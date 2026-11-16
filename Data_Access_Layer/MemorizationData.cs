using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;

namespace Data_Access_Layer
{
    public class MemorizationData
    {
        private readonly HalaqaDbContext _context;

        public MemorizationData(HalaqaDbContext context)
        {
            _context = context;
        }

        public async Task<List<MemorizationStudentDTO>> GetStudentsForTeacher(int teacherId)
        {
            var teacherCenterId = await _context.Teachers
                .AsNoTracking()
                .Where(teacher => teacher.TeacherId == teacherId && teacher.IsActive)
                .Select(teacher => (int?)teacher.CenterId)
                .FirstOrDefaultAsync();

            if (!teacherCenterId.HasValue)
                return new List<MemorizationStudentDTO>();

            return await _context.HalaqaStudents
                .AsNoTracking()
                .Where(membership => membership.EndDate == null
                    && membership.CenterId == teacherCenterId.Value
                    && membership.Student.IsActive
                    && membership.Student.CenterId == teacherCenterId.Value
                    && membership.Halaqa.IsActive
                    && membership.Halaqa.CenterId == teacherCenterId.Value
                    && membership.Halaqa.TeacherId == teacherId)
                .Select(membership => new MemorizationStudentDTO
                {
                    StudentId = membership.StudentId,
                    StudentName = membership.Student.User.FirstName + " " + membership.Student.User.LastName,
                    HalaqaName = membership.Halaqa.HalaqaName
                })
                .OrderBy(student => student.StudentName)
                .ToListAsync();
        }

        public async Task<List<MemorizationRecordDTO>> GetMemorizationRecords(
            int? centerId = null,
            int? teacherId = null,
            int? studentId = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            // بلا فلاتر تاريخ: آخر 200 سجل فقط. مع فلاتر التاريخ: حد أمان أكبر.
            var hasFilters = fromDate.HasValue || toDate.HasValue;
            var limit = hasFilters ? 5000 : 200;

            return await _context.MemorizationRecords
                .AsNoTracking()
                .Where(record => (!centerId.HasValue || record.CenterId == centerId.Value)
                    && (!teacherId.HasValue || record.TeacherId == teacherId.Value)
                    && (!studentId.HasValue || record.StudentId == studentId.Value)
                    && (!fromDate.HasValue || record.RecordDate >= fromDate.Value)
                    && (!toDate.HasValue || record.RecordDate <= toDate.Value))
                .OrderByDescending(record => record.RecordDate)
                .ThenByDescending(record => record.CreatedAt)
                .Take(limit)
                .Select(record => new MemorizationRecordDTO
                {
                    MemorizationRecordId = record.MemorizationRecordId,
                    StudentId = record.StudentId,
                    StudentName = record.Student.User.FirstName + " " + record.Student.User.LastName,
                    TeacherName = record.Teacher.User.FirstName + " " + record.Teacher.User.LastName,
                    Type = record.Type,
                    SurahNumber = record.SurahNumber,
                    FromAyah = record.FromAyah,
                    ToAyah = record.ToAyah,
                    RecordDate = record.RecordDate,
                    Notes = record.Notes,
                    SessionId = record.SessionId
                })
                .ToListAsync();
        }

        private const int StudentLockNamespace = 7301;

        // عدد الجلسات = جلسات SessionId المختلفة + كل سجل قديم بلا SessionId (يُحسب جلسة مستقلة).
        private static int CountSessions(IEnumerable<Guid?> sessionIds)
        {
            var list = sessionIds.ToList();
            return list.Where(id => id.HasValue).Select(id => id!.Value).Distinct().Count()
                + list.Count(id => !id.HasValue);
        }

        private async Task<List<int>> GetAllowedStudentIds(int teacherId, int centerId, List<int> studentIds)
        {
            return await _context.HalaqaStudents
                .AsNoTracking()
                .Where(membership => studentIds.Contains(membership.StudentId)
                    && membership.EndDate == null
                    && membership.CenterId == centerId
                    && membership.Student.IsActive
                    && membership.Student.CenterId == centerId
                    && membership.Halaqa.IsActive
                    && membership.Halaqa.CenterId == centerId
                    && membership.Halaqa.TeacherId == teacherId)
                .Select(membership => membership.StudentId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<MemorizationDailyUsageDTO?> GetDailyUsage(int teacherId, int studentId, DateOnly date)
        {
            var centerId = await _context.Teachers
                .AsNoTracking()
                .Where(teacher => teacher.TeacherId == teacherId && teacher.IsActive)
                .Select(teacher => (int?)teacher.CenterId)
                .FirstOrDefaultAsync();

            if (!centerId.HasValue)
                return null;

            var allowed = await GetAllowedStudentIds(teacherId, centerId.Value, new List<int> { studentId });
            if (!allowed.Contains(studentId))
                return null;

            var rows = await _context.MemorizationRecords
                .AsNoTracking()
                .Where(record => record.StudentId == studentId && record.RecordDate == date)
                .Select(record => new { record.Type, record.SessionId })
                .ToListAsync();

            return new MemorizationDailyUsageDTO
            {
                StudentId = studentId,
                Date = date.ToString("yyyy-MM-dd"),
                MemorizationCount = CountSessions(rows.Where(r => r.Type == MemorizationRules.TypeMemorization).Select(r => r.SessionId)),
                ReviewCount = CountSessions(rows.Where(r => r.Type == MemorizationRules.TypeReview).Select(r => r.SessionId)),
                MemorizationLimit = MemorizationRules.MaxMemorizationPerDay,
                ReviewLimit = MemorizationRules.MaxReviewPerDay
            };
        }

        public async Task<MemorizationSaveResult> SaveMemorizationRecords(
            List<MemorizationRecordWriteDTO> records,
            int teacherId,
            int userId)
        {
            var notAllowed = new MemorizationSaveResult
            {
                Status = MemorizationSaveStatus.NotAllowed,
                Message = "الطالب لا يتبع حلقة نشطة مرتبطة بحسابك."
            };

            var teacher = await _context.Teachers
                .AsNoTracking()
                .Where(item => item.TeacherId == teacherId
                    && item.UserId == userId
                    && item.IsActive)
                .Select(item => new { item.TeacherId, item.CenterId })
                .FirstOrDefaultAsync();

            if (teacher == null)
                return notAllowed;

            var studentIds = records.Select(record => record.StudentId).Distinct().OrderBy(id => id).ToList();
            var allowedStudentIds = await GetAllowedStudentIds(teacherId, teacher.CenterId, studentIds);

            if (studentIds.Any(studentId => !allowedStudentIds.Contains(studentId)))
                return notAllowed;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // قفل لكل طالب حتى نهاية المعاملة: يمنع طلبين متزامنين من تجاوز الحد معاً.
            // الترتيب التصاعدي يمنع التعارض (deadlock) بين طلبين على طلاب متشابهين.
            foreach (var studentId in studentIds)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock({0}, {1})", StudentLockNamespace, studentId);
            }

            // منع إعادة إرسال جلسة محفوظة (نقر مزدوج أو تكرار الطلب).
            var incomingSessionIds = records
                .Where(record => record.SessionId.HasValue)
                .Select(record => record.SessionId!.Value)
                .Distinct()
                .ToList();

            if (incomingSessionIds.Count > 0)
            {
                var alreadySaved = await _context.MemorizationRecords
                    .AsNoTracking()
                    .AnyAsync(record => record.SessionId != null && incomingSessionIds.Contains(record.SessionId.Value));

                if (alreadySaved)
                {
                    return new MemorizationSaveResult
                    {
                        Status = MemorizationSaveStatus.DuplicateSession,
                        Message = "تم تسجيل هذه الجلسة مسبقاً."
                    };
                }
            }

            // فحص الحد اليومي لكل (طالب، تاريخ، نوع) مقابل ما هو محفوظ فعلاً في قاعدة البيانات.
            var groups = records
                .GroupBy(record => new { record.StudentId, record.RecordDate, record.Type })
                .ToList();

            foreach (var group in groups)
            {
                var key = group.Key;
                var existing = await _context.MemorizationRecords
                    .AsNoTracking()
                    .Where(record => record.StudentId == key.StudentId
                        && record.RecordDate == key.RecordDate
                        && record.Type == key.Type)
                    .Select(record => record.SessionId)
                    .ToListAsync();

                var incomingSessions = CountSessions(group.Select(record => record.SessionId));

                if (CountSessions(existing) + incomingSessions > MemorizationRules.MaxPerDay(key.Type))
                {
                    return new MemorizationSaveResult
                    {
                        Status = MemorizationSaveStatus.LimitReached,
                        Message = MemorizationRules.LimitMessage(key.Type)
                    };
                }
            }

            var entities = records.Select(record => new MemorizationRecord
            {
                CenterId = teacher.CenterId,
                StudentId = record.StudentId,
                TeacherId = teacher.TeacherId,
                Type = record.Type,
                SurahNumber = record.SurahNumber,
                FromAyah = record.FromAyah,
                ToAyah = record.ToAyah,
                RecordDate = record.RecordDate,
                Notes = record.Notes,
                CreatedByUserId = userId,
                Source = MemorizationRules.SourceManual,
                SessionId = record.SessionId
            }).ToList();

            await _context.MemorizationRecords.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new MemorizationSaveResult
            {
                Status = MemorizationSaveStatus.Saved,
                SavedCount = entities.Count
            };
        }
    }
}
