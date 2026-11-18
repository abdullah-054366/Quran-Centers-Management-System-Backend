using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;


namespace Data_Access_Layer
{
    public class CenterData
    {

        private readonly HalaqaDbContext _context;

        public CenterData(HalaqaDbContext context)
        {
            _context = context;
        }


        public  async Task<List<CenterDTO>> GetAllCenters()
        {
            try
            {
                return await (from c in _context.Centers.AsNoTracking()
                              orderby c.CenterName
                              select new CenterDTO(
                                  c.CenterId, c.CenterName, c.CenterCode,
                                  c.CodeEnabled, c.IsActive, c.CreatedAt))
               .ToListAsync();
            }
            catch
            {
                throw new Exception("An error occurred while retrieving all centers.");
            }
        }



        public async Task<CenterDTO?> GetCenterById(int id)
        {
            try
            {
                return await (from c in _context.Centers.AsNoTracking()
                              orderby c.CenterName
                              where c.CenterId == id
                              select new CenterDTO(
                                  c.CenterId, c.CenterName, c.CenterCode,
                                  c.CodeEnabled, c.IsActive, c.CreatedAt))
               .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, rethrow it, return a default value, etc.)
                throw new Exception("An error occurred while retrieving the center by ID.", ex);
            }
        }





        public Task<bool> CenterCodeExists(string centerCode)
        {
            var normalizedCode = centerCode.Trim().ToUpperInvariant();
            return _context.Centers.AnyAsync(center => center.CenterCode.ToUpper() == normalizedCode);
        }

        public async Task<CenterDTO> AddCenter(CenterWriteDTO input)
        {
            var center = new Center
            {
                CenterName = input.CenterName.Trim(),
                CenterCode = input.CenterCode.Trim().ToUpperInvariant(),
                CodeEnabled = true,
                IsActive = true
            };

            _context.Centers.Add(center);
            await _context.SaveChangesAsync();

            return ToCenterDTO(center);
        }

        public async Task<CenterDTO?> UpdateCenterStatus(int centerId, bool isActive)
        {
            var center = await _context.Centers.FindAsync(centerId);
            if (center is null)
                return null;

            center.IsActive = isActive;
            await _context.SaveChangesAsync();
            return ToCenterDTO(center);
        }

        public async Task<CenterDTO?> UpdateCenterCodeEnabled(int centerId, bool codeEnabled)
        {
            var center = await _context.Centers.FindAsync(centerId);
            if (center is null)
                return null;

            center.CodeEnabled = codeEnabled;
            await _context.SaveChangesAsync();
            return ToCenterDTO(center);
        }

        private static CenterDTO ToCenterDTO(Center center)
        {
            return new CenterDTO(
                center.CenterId,
                center.CenterName,
                center.CenterCode,
                center.CodeEnabled,
                center.IsActive,
                center.CreatedAt);
        }

        public async Task<bool> DeleteCenter(int centerId)
        {
            var center = await _context.Centers.FindAsync(centerId);
            if (center == null)
            {
                return false; // Center not found
            }
            _context.Centers.Remove(center);
            await _context.SaveChangesAsync();
            return true; // Center deleted successfully
        }




        private static DateOnly TodayLocal() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

        public async Task<CenterDTO?> UpdateCenterName(int centerId, string centerName)
        {
            var center = await _context.Centers.FindAsync(centerId);
            if (center is null)
                return null;

            center.CenterName = centerName.Trim();
            await _context.SaveChangesAsync();
            return ToCenterDTO(center);
        }

        private static readonly char[] CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

        public async Task<CenterDTO?> RegenerateCenterCode(int centerId)
        {
            var center = await _context.Centers.FindAsync(centerId);
            if (center is null)
                return null;

            for (var attempt = 0; attempt < 20; attempt++)
            {
                var code = new string(Enumerable.Range(0, 6)
                    .Select(_ => CodeAlphabet[System.Security.Cryptography.RandomNumberGenerator.GetInt32(CodeAlphabet.Length)])
                    .ToArray());

                if (await CenterCodeExists(code))
                    continue;

                center.CenterCode = code;
                await _context.SaveChangesAsync();
                return ToCenterDTO(center);
            }

            throw new InvalidOperationException("تعذّر توليد كود فريد.");
        }

        // جدول الخطط قد لا يكون أُنشئ بعد في قاعدة بيانات قديمة.
        private async Task<int> CountPlansSafe(int centerId)
        {
            try
            {
                return await _context.MemorizationPlans.CountAsync(item => item.CenterId == centerId);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<CenterDeletionSummaryDTO?> GetDeletionSummary(int centerId)
        {
            var center = await _context.Centers.AsNoTracking()
                .Where(item => item.CenterId == centerId)
                .Select(item => new { item.CenterId, item.CenterName })
                .FirstOrDefaultAsync();

            if (center is null)
                return null;

            var summary = new CenterDeletionSummaryDTO
            {
                CenterId = center.CenterId,
                CenterName = center.CenterName,
                Students = await _context.Students.CountAsync(item => item.CenterId == centerId),
                Teachers = await _context.Teachers.CountAsync(item => item.CenterId == centerId),
                Halaqat = await _context.Halaqats.CountAsync(item => item.CenterId == centerId),
                Users = await _context.Users.CountAsync(item => item.CenterId == centerId),
                AttendanceRecords = await _context.Attendances.CountAsync(item => item.CenterId == centerId),
                MemorizationRecords = await _context.MemorizationRecords.CountAsync(item => item.CenterId == centerId),
                Plans = await CountPlansSafe(centerId)
            };

            summary.CanDelete = summary.Students + summary.Teachers + summary.Halaqat + summary.Users
                + summary.AttendanceRecords + summary.MemorizationRecords + summary.Plans == 0;
            return summary;
        }

        public async Task<CenterOverviewDTO?> GetCenterOverview(int centerId)
        {
            var center = await GetCenterById(centerId);
            if (center is null)
                return null;

            var today = TodayLocal();
            var monthStart = new DateOnly(today.Year, today.Month, 1);

            var overview = new CenterOverviewDTO
            {
                Center = center,
                TotalStudents = await _context.Students.CountAsync(item => item.CenterId == centerId),
                ActiveStudents = await _context.Students.CountAsync(item => item.CenterId == centerId && item.IsActive),
                ActiveTeachers = await _context.Teachers.CountAsync(item => item.CenterId == centerId && item.IsActive),
                ActiveHalaqat = await _context.Halaqats.CountAsync(item => item.CenterId == centerId && item.IsActive),
                MonthAttendanceRecords = await _context.Attendances.CountAsync(item =>
                    item.CenterId == centerId && item.AttendanceDate >= monthStart),
                MonthAttendedRecords = await _context.Attendances.CountAsync(item =>
                    item.CenterId == centerId && item.AttendanceDate >= monthStart
                    && (item.Status == 1 || item.Status == 3)),
                MonthMemorizationRecords = await _context.MemorizationRecords.CountAsync(item =>
                    item.CenterId == centerId && item.RecordDate >= monthStart)
            };

            var lastAttendance = await _context.Attendances.AsNoTracking()
                .Where(item => item.CenterId == centerId)
                .MaxAsync(item => (DateOnly?)item.AttendanceDate);
            var lastRecord = await _context.MemorizationRecords.AsNoTracking()
                .Where(item => item.CenterId == centerId)
                .MaxAsync(item => (DateOnly?)item.RecordDate);

            overview.LastActivityDate = lastAttendance.HasValue && lastRecord.HasValue
                ? (lastAttendance > lastRecord ? lastAttendance : lastRecord)
                : lastAttendance ?? lastRecord;

            overview.Halaqat = await (
                from halaqa in _context.Halaqats.AsNoTracking()
                where halaqa.CenterId == centerId
                orderby halaqa.HalaqaName
                select new CenterHalaqaSummaryDTO
                {
                    HalaqaId = halaqa.HalaqaId,
                    HalaqaName = halaqa.HalaqaName,
                    IsActive = halaqa.IsActive,
                    TeacherName = (
                        from teacher in _context.Teachers.AsNoTracking()
                        join user in _context.Users.AsNoTracking() on teacher.UserId equals user.UserId
                        where teacher.TeacherId == halaqa.TeacherId
                        select user.FirstName + " " + user.LastName
                    ).FirstOrDefault(),
                    StudentCount = _context.HalaqaStudents.Count(membership =>
                        membership.HalaqaId == halaqa.HalaqaId && membership.EndDate == null)
                }).ToListAsync();

            overview.Admins = await (
                from user in _context.Users.AsNoTracking()
                join role in _context.Roles.AsNoTracking() on user.RoleId equals role.RoleId
                where user.CenterId == centerId && role.RoleName == "CenterAdmin"
                orderby user.FirstName, user.LastName
                select new CenterAdminUserDTO
                {
                    UserId = user.UserId,
                    FullName = user.FirstName + " " + user.LastName,
                    Email = user.Email,
                    IsActive = user.IsActive
                }).ToListAsync();

            return overview;
        }

        public async Task<PlatformOverviewDTO> GetPlatformOverview()
        {
            var today = TodayLocal();
            var monthStart = new DateOnly(today.Year, today.Month, 1);

            var overview = new PlatformOverviewDTO
            {
                TotalCenters = await _context.Centers.CountAsync(),
                ActiveCenters = await _context.Centers.CountAsync(item => item.IsActive),
                TotalUsers = await _context.Users.CountAsync(),
                TotalStudents = await _context.Students.CountAsync(),
                ActiveStudents = await _context.Students.CountAsync(item => item.IsActive),
                TotalTeachers = await _context.Teachers.CountAsync(),
                TotalHalaqat = await _context.Halaqats.CountAsync(),
                MonthAttendanceRecords = await _context.Attendances.CountAsync(item => item.AttendanceDate >= monthStart),
                MonthAttendedRecords = await _context.Attendances.CountAsync(item =>
                    item.AttendanceDate >= monthStart && (item.Status == 1 || item.Status == 3))
            };

            // النمو التراكمي لآخر 6 أشهر.
            var centerDates = (await _context.Centers.AsNoTracking()
                .Select(item => item.CreatedAt).ToListAsync())
                .Select(DateOnly.FromDateTime).ToList();
            var studentDates = await _context.Students.AsNoTracking()
                .Select(item => item.JoinedAt).ToListAsync();

            for (var i = 5; i >= 0; i--)
            {
                var month = monthStart.AddMonths(-i);
                var end = month.AddMonths(1);
                overview.Growth.Add(new PlatformMonthPointDTO
                {
                    Month = $"{month.Year}-{month.Month:00}",
                    Centers = centerDates.Count(date => date < end),
                    Students = studentDates.Count(date => date < end)
                });
            }

            // نشاط كل مركز.
            var attendance = await _context.Attendances.AsNoTracking()
                .GroupBy(item => item.CenterId)
                .Select(group => new
                {
                    CenterId = group.Key,
                    Last = group.Max(item => item.AttendanceDate),
                    Month = group.Count(item => item.AttendanceDate >= monthStart)
                }).ToListAsync();

            var records = await _context.MemorizationRecords.AsNoTracking()
                .GroupBy(item => item.CenterId)
                .Select(group => new
                {
                    CenterId = group.Key,
                    Last = group.Max(item => item.RecordDate),
                    Month = group.Count(item => item.RecordDate >= monthStart)
                }).ToListAsync();

            var studentCounts = await _context.Students.AsNoTracking()
                .Where(item => item.IsActive)
                .GroupBy(item => item.CenterId)
                .Select(group => new { CenterId = group.Key, Count = group.Count() })
                .ToListAsync();

            var centers = await _context.Centers.AsNoTracking()
                .Select(item => new { item.CenterId, item.CenterName, item.IsActive })
                .ToListAsync();

            var activity = centers.Select(center =>
            {
                var att = attendance.FirstOrDefault(item => item.CenterId == center.CenterId);
                var rec = records.FirstOrDefault(item => item.CenterId == center.CenterId);

                DateOnly? last = null;
                if (att is not null) last = att.Last;
                if (rec is not null && (!last.HasValue || rec.Last > last.Value)) last = rec.Last;

                return new PlatformCenterActivityDTO
                {
                    CenterId = center.CenterId,
                    CenterName = center.CenterName,
                    IsActive = center.IsActive,
                    ActiveStudents = studentCounts.FirstOrDefault(item => item.CenterId == center.CenterId)?.Count ?? 0,
                    MonthActivity = (att?.Month ?? 0) + (rec?.Month ?? 0),
                    LastActivityDate = last,
                    DaysSinceActivity = last.HasValue ? today.DayNumber - last.Value.DayNumber : null
                };
            }).ToList();

            overview.TopCenters = activity
                .Where(item => item.MonthActivity > 0)
                .OrderByDescending(item => item.MonthActivity)
                .Take(5)
                .ToList();

            overview.IdleCenters = activity
                .Where(item => item.IsActive && (!item.DaysSinceActivity.HasValue || item.DaysSinceActivity.Value >= 14))
                .OrderByDescending(item => item.DaysSinceActivity ?? int.MaxValue)
                .Take(10)
                .ToList();

            return overview;
        }

        public async Task<int> getCountCenters()
        {
            try
            {
                return await _context.Centers.CountAsync();
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, rethrow it, return a default value, etc.)
                throw new Exception("An error occurred while counting the centers.", ex);
            }
        }



        public async Task<int> getIctiveCentersCount()
        {
            try
            {
                return await _context.Centers.CountAsync(c => c.IsActive);
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, rethrow it, return a default value, etc.)
                throw new Exception("An error occurred while counting the active centers.", ex);
            }
        }

    }
}
