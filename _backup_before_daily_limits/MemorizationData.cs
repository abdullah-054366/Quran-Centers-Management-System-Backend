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
            int? studentId = null)
        {
            return await _context.MemorizationRecords
                .AsNoTracking()
                .Where(record => (!centerId.HasValue || record.CenterId == centerId.Value)
                    && (!teacherId.HasValue || record.TeacherId == teacherId.Value)
                    && (!studentId.HasValue || record.StudentId == studentId.Value))
                .OrderByDescending(record => record.RecordDate)
                .ThenByDescending(record => record.CreatedAt)
                .Take(200)
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
                    Notes = record.Notes
                })
                .ToListAsync();
        }

        public async Task<bool> SaveMemorizationRecords(
            List<MemorizationRecordWriteDTO> records,
            int teacherId,
            int userId)
        {
            var teacher = await _context.Teachers
                .AsNoTracking()
                .Where(item => item.TeacherId == teacherId
                    && item.UserId == userId
                    && item.IsActive)
                .Select(item => new { item.TeacherId, item.CenterId })
                .FirstOrDefaultAsync();

            if (teacher == null)
                return false;

            var studentIds = records.Select(record => record.StudentId).Distinct().ToList();
            var allowedStudentIds = await _context.HalaqaStudents
                .AsNoTracking()
                .Where(membership => studentIds.Contains(membership.StudentId)
                    && membership.EndDate == null
                    && membership.CenterId == teacher.CenterId
                    && membership.Student.IsActive
                    && membership.Student.CenterId == teacher.CenterId
                    && membership.Halaqa.IsActive
                    && membership.Halaqa.CenterId == teacher.CenterId
                    && membership.Halaqa.TeacherId == teacherId)
                .Select(membership => membership.StudentId)
                .Distinct()
                .ToListAsync();

            if (studentIds.Any(studentId => !allowedStudentIds.Contains(studentId)))
                return false;

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
                Source = 0
            }).ToList();

            await _context.MemorizationRecords.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
