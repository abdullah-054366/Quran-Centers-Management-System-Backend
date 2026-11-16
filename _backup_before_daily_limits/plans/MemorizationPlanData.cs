using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;

namespace Data_Access_Layer
{
    public class MemorizationPlanData
    {
        private readonly HalaqaDbContext _context;

        public MemorizationPlanData(HalaqaDbContext context)
        {
            _context = context;
        }

        public async Task<List<MemorizationPlanDTO>> GetAllAsync(
            int? centerId = null,
            int? teacherId = null)
        {
            return await ProjectPlans(GetScopedPlans(centerId, teacherId))
                .OrderByDescending(plan => plan.CreatedAt)
                .ToListAsync();
        }

        public async Task<MemorizationPlanDTO?> GetByIdAsync(
            int planId,
            int? centerId = null,
            int? teacherId = null)
        {
            return await ProjectPlans(GetScopedPlans(centerId, teacherId))
                .FirstOrDefaultAsync(plan => plan.MemorizationPlanId == planId);
        }

        public async Task<MemorizationPlanDTO?> CreateAsync(
            MemorizationPlanWriteDTO input,
            int? centerScope,
            int? teacherId,
            int userId)
        {
            var student = await _context.Students
                .AsNoTracking()
                .Where(item => item.StudentId == input.StudentId && item.IsActive)
                .Select(item => new { item.StudentId, item.CenterId })
                .FirstOrDefaultAsync();

            if (student == null
                || (centerScope.HasValue && student.CenterId != centerScope.Value)
                || !await _context.Centers.AnyAsync(center =>
                    center.CenterId == student.CenterId && center.IsActive)
                || !await _context.Users.AnyAsync(user => user.UserId == userId && user.IsActive))
            {
                return null;
            }

            if (teacherId.HasValue
                && !await HasActiveTeacherAssignment(student.StudentId, student.CenterId, teacherId.Value))
            {
                return null;
            }

            var plan = new MemorizationPlan
            {
                CenterId = student.CenterId,
                StudentId = student.StudentId,
                Type = input.Type,
                FromSurah = input.FromSurah,
                ToSurah = input.ToSurah,
                PagesPerSession = input.PagesPerSession,
                TargetRounds = input.TargetRounds,
                StartDate = input.StartDate,
                DaysOfWeek = input.DaysOfWeek.Distinct().OrderBy(day => day).ToArray(),
                IsActive = true,
                CreatedByUserId = userId
            };

            await _context.MemorizationPlans.AddAsync(plan);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(plan.MemorizationPlanId, centerScope, teacherId);
        }

        public async Task<MemorizationPlanDTO?> UpdateStatusAsync(
            int planId,
            bool isActive,
            int? centerId,
            int? teacherId)
        {
            var plan = await GetScopedPlans(centerId, teacherId, asNoTracking: false)
                .FirstOrDefaultAsync(item => item.MemorizationPlanId == planId);

            if (plan == null)
                return null;

            plan.IsActive = isActive;
            plan.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(planId, centerId, teacherId);
        }

        private IQueryable<MemorizationPlan> GetScopedPlans(
            int? centerId,
            int? teacherId,
            bool asNoTracking = true)
        {
            var plans = asNoTracking
                ? _context.MemorizationPlans.AsNoTracking()
                : _context.MemorizationPlans.AsQueryable();

            if (centerId.HasValue)
                plans = plans.Where(plan => plan.CenterId == centerId.Value);

            if (teacherId.HasValue)
            {
                plans = plans.Where(plan => _context.HalaqaStudents.Any(membership =>
                    membership.StudentId == plan.StudentId
                    && membership.CenterId == plan.CenterId
                    && membership.EndDate == null
                    && _context.Halaqats.Any(halaqa =>
                        halaqa.HalaqaId == membership.HalaqaId
                        && halaqa.CenterId == plan.CenterId
                        && halaqa.IsActive
                        && halaqa.TeacherId == teacherId.Value)));
            }

            return plans;
        }

        private IQueryable<MemorizationPlanDTO> ProjectPlans(IQueryable<MemorizationPlan> plans)
        {
            return plans.Select(plan => new MemorizationPlanDTO
            {
                MemorizationPlanId = plan.MemorizationPlanId,
                CenterId = plan.CenterId,
                StudentId = plan.StudentId,
                StudentName = plan.Student.User.FirstName + " " + plan.Student.User.LastName,
                Type = plan.Type,
                FromSurah = plan.FromSurah,
                ToSurah = plan.ToSurah,
                PagesPerSession = plan.PagesPerSession,
                TargetRounds = plan.TargetRounds,
                StartDate = plan.StartDate,
                DaysOfWeek = plan.DaysOfWeek,
                IsActive = plan.IsActive,
                CompletedSessions = _context.MemorizationRecords
                    .Where(record => record.StudentId == plan.StudentId
                        && record.RecordDate >= plan.StartDate
                        && record.Type == plan.Type
                        && record.SurahNumber >= plan.FromSurah
                        && record.SurahNumber <= plan.ToSurah)
                    .Select(record => record.RecordDate)
                    .Distinct()
                    .Count(),
                CreatedAt = plan.CreatedAt
            });
        }

        private async Task<bool> HasActiveTeacherAssignment(
            int studentId,
            int centerId,
            int teacherId)
        {
            return await _context.HalaqaStudents.AnyAsync(membership =>
                membership.StudentId == studentId
                && membership.CenterId == centerId
                && membership.EndDate == null
                && _context.Halaqats.Any(halaqa =>
                    halaqa.HalaqaId == membership.HalaqaId
                    && halaqa.CenterId == centerId
                    && halaqa.IsActive
                    && halaqa.TeacherId == teacherId));
        }
    }
}
