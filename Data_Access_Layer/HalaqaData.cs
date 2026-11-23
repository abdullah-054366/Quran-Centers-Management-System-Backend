using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;

namespace Data_Access_Layer;

public sealed class HalaqaData
{
    private readonly HalaqaDbContext _context;

    public HalaqaData(HalaqaDbContext context)
    {
        _context = context;
    }

    public Task<List<HalaqaDTO>> GetAllAsync(int? centerId = null, int? teacherId = null)
    {
        return GetHalaqaQuery()
            .Where(halaqa => (!centerId.HasValue || halaqa.CenterId == centerId.Value)
                && (!teacherId.HasValue || halaqa.TeacherId == teacherId.Value))
            .OrderBy(halaqa => halaqa.CenterName)
            .ThenBy(halaqa => halaqa.HalaqaName)
            .ToListAsync();
    }

    public Task<HalaqaDTO?> GetByIdAsync(int halaqaId, int? centerId = null, int? teacherId = null)
    {
        return GetHalaqaQuery()
            .Where(halaqa => halaqa.HalaqaId == halaqaId
                && (!centerId.HasValue || halaqa.CenterId == centerId.Value)
                && (!teacherId.HasValue || halaqa.TeacherId == teacherId.Value))
            .FirstOrDefaultAsync();
    }

    public Task<List<HalaqaStudentDTO>> GetStudentsAsync(int halaqaId, int? centerId = null)
    {
        return (from membership in _context.HalaqaStudents.AsNoTracking()
                join student in _context.Students.AsNoTracking()
                    on membership.StudentId equals student.StudentId
                join user in _context.Users.AsNoTracking()
                    on student.UserId equals user.UserId
                where membership.HalaqaId == halaqaId
                    && membership.EndDate == null
                    && (!centerId.HasValue || membership.CenterId == centerId.Value)
                orderby user.FirstName, user.LastName
                select new HalaqaStudentDTO
                {
                    StudentId = student.StudentId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    StartDate = membership.StartDate
                }).ToListAsync();
    }

    /// <summary>
    /// الحلقات النشطة في مركز، كما تُعرض للطالب أثناء التسجيل الذاتي.
    /// </summary>
    public async Task<List<PublicHalaqaDTO>> GetForRegistrationAsync(int centerId)
    {
        return await (from halaqa in _context.Halaqats.AsNoTracking()
                      where halaqa.CenterId == centerId && halaqa.IsActive
                      orderby halaqa.HalaqaName
                      select new PublicHalaqaDTO
                      {
                          HalaqaId = halaqa.HalaqaId,
                          HalaqaName = halaqa.HalaqaName,
                          Description = halaqa.Description,
                          TeacherName = (
                              from teacher in _context.Teachers.AsNoTracking()
                              join user in _context.Users.AsNoTracking() on teacher.UserId equals user.UserId
                              where teacher.TeacherId == halaqa.TeacherId
                              select user.FirstName + " " + user.LastName
                          ).FirstOrDefault(),
                          StudentCount = _context.HalaqaStudents.Count(membership =>
                              membership.HalaqaId == halaqa.HalaqaId && membership.EndDate == null)
                      }).ToListAsync();
    }

    /// <summary>يتحقق أن الحلقة نشطة وتتبع هذا المركز (يُستخدم في التسجيل الذاتي).</summary>
    public Task<bool> IsOpenForRegistrationAsync(int halaqaId, int centerId)
    {
        return _context.Halaqats.AsNoTracking()
            .AnyAsync(halaqa => halaqa.HalaqaId == halaqaId
                && halaqa.CenterId == centerId
                && halaqa.IsActive);
    }

    public async Task<List<StudentDTO>> GetAvailableStudentsAsync(int halaqaId, int? centerId = null)
    {
        var halaqa = await _context.Halaqats.AsNoTracking()
            .Where(item => item.HalaqaId == halaqaId
                && (!centerId.HasValue || item.CenterId == centerId.Value))
            .Select(item => new { item.CenterId, item.IsActive })
            .FirstOrDefaultAsync();

        if (halaqa is null || !halaqa.IsActive)
            return new List<StudentDTO>();

        return await (from student in _context.Students.AsNoTracking()
                      join user in _context.Users.AsNoTracking()
                          on student.UserId equals user.UserId
                      where student.CenterId == halaqa.CenterId
                          && student.IsActive
                          && !_context.HalaqaStudents.Any(membership =>
                              membership.StudentId == student.StudentId
                              && membership.EndDate == null)
                      orderby user.FirstName, user.LastName
                      select new StudentDTO(
                          student.StudentId,
                          student.UserId,
                          user.FirstName,
                          user.LastName,
                          user.Email,
                          user.PhoneNumber,
                          student.IsActive,
                          student.JoinedAt))
            .ToListAsync();
    }

    public async Task<bool> AssignStudentsAsync(
        int halaqaId,
        IEnumerable<int> studentIds,
        int? centerId = null)
    {
        var uniqueStudentIds = studentIds.Distinct().ToArray();
        if (uniqueStudentIds.Length == 0)
            return false;

        var halaqa = await _context.Halaqats.FirstOrDefaultAsync(item =>
            item.HalaqaId == halaqaId
            && item.IsActive
            && (!centerId.HasValue || item.CenterId == centerId.Value));

        if (halaqa is null)
            return false;

        var eligibleStudentIds = await _context.Students
            .Where(student => uniqueStudentIds.Contains(student.StudentId)
                && student.CenterId == halaqa.CenterId
                && student.IsActive
                && !_context.HalaqaStudents.Any(membership =>
                    membership.StudentId == student.StudentId
                    && membership.EndDate == null))
            .Select(student => student.StudentId)
            .ToListAsync();

        if (eligibleStudentIds.Count != uniqueStudentIds.Length)
            return false;

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var memberships = uniqueStudentIds.Select(studentId => new HalaqaStudent
        {
            CenterId = halaqa.CenterId,
            HalaqaId = halaqa.HalaqaId,
            StudentId = studentId,
            StartDate = startDate
        });

        await _context.HalaqaStudents.AddRangeAsync(memberships);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveStudentAsync(
        int halaqaId,
        int studentId,
        int? centerId = null)
    {
        var membership = await _context.HalaqaStudents.FirstOrDefaultAsync(item =>
            item.HalaqaId == halaqaId
            && item.StudentId == studentId
            && item.EndDate == null
            && (!centerId.HasValue || item.CenterId == centerId.Value));

        if (membership is null)
            return false;

        membership.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CanAssignAsync(
        int centerId,
        int? teacherId,
        bool allowInactiveCenter = false,
        int? allowedInactiveTeacherId = null)
    {
        var centerExists = await _context.Centers
            .AnyAsync(center => center.CenterId == centerId
                && (center.IsActive || allowInactiveCenter));
        if (!centerExists)
            return false;

        if (!teacherId.HasValue)
            return true;

        return await _context.Teachers.AnyAsync(teacher =>
            teacher.TeacherId == teacherId.Value
            && teacher.CenterId == centerId
            && (teacher.IsActive || teacher.TeacherId == allowedInactiveTeacherId));
    }

    public async Task<HalaqaDTO> CreateAsync(HalaqaWriteDTO input)
    {
        var halaqa = new Halaqat
        {
            CenterId = input.CenterId!.Value,
            TeacherId = input.TeacherId,
            HalaqaName = input.HalaqaName.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            IsActive = input.IsActive
        };

        _context.Halaqats.Add(halaqa);
        await _context.SaveChangesAsync();
        return (await GetByIdAsync(halaqa.HalaqaId))!;
    }

    public async Task<HalaqaDTO?> UpdateAsync(int halaqaId, HalaqaWriteDTO input, int? centerScope = null)
    {
        var halaqa = await _context.Halaqats.FirstOrDefaultAsync(item =>
            item.HalaqaId == halaqaId
            && (!centerScope.HasValue || item.CenterId == centerScope.Value));

        if (halaqa is null)
            return null;

        halaqa.CenterId = input.CenterId!.Value;
        halaqa.TeacherId = input.TeacherId;
        halaqa.HalaqaName = input.HalaqaName.Trim();
        halaqa.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        halaqa.IsActive = input.IsActive;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(halaqaId, centerScope);
    }

    public async Task<bool> DeleteAsync(int halaqaId, int? centerScope = null)
    {
        var halaqa = await _context.Halaqats.FirstOrDefaultAsync(item =>
            item.HalaqaId == halaqaId
            && (!centerScope.HasValue || item.CenterId == centerScope.Value));

        if (halaqa is null)
            return false;

        _context.Halaqats.Remove(halaqa);
        await _context.SaveChangesAsync();
        return true;
    }

    private IQueryable<HalaqaDTO> GetHalaqaQuery()
    {
        return from halaqa in _context.Halaqats.AsNoTracking()
               join center in _context.Centers.AsNoTracking()
                   on halaqa.CenterId equals center.CenterId
               select new HalaqaDTO
               {
                   HalaqaId = halaqa.HalaqaId,
                   CenterId = halaqa.CenterId,
                   CenterName = center.CenterName,
                   TeacherId = halaqa.TeacherId,
                   TeacherName = (
                       from teacher in _context.Teachers.AsNoTracking()
                       join user in _context.Users.AsNoTracking() on teacher.UserId equals user.UserId
                       where teacher.TeacherId == halaqa.TeacherId
                       select user.FirstName + " " + user.LastName
                   ).FirstOrDefault(),
                   HalaqaName = halaqa.HalaqaName,
                   Description = halaqa.Description,
                   StudentCount = _context.HalaqaStudents.Count(student =>
                       student.HalaqaId == halaqa.HalaqaId && student.EndDate == null),
                   IsActive = halaqa.IsActive,
                   CreatedAt = halaqa.CreatedAt
               };
    }
}
