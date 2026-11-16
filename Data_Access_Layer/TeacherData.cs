using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;

namespace Data_Access_Layer
{
    public class TeacherData
    {
        private readonly HalaqaDbContext _context;

        public TeacherData(HalaqaDbContext context)
        {
            _context = context;
        }

        public async Task<List<TeacherDTO>> GetAllTeachers(int? centerId = null)
        {
            return await (
                from teacher in _context.Teachers.AsNoTracking()
                join user in _context.Users.AsNoTracking() on teacher.UserId equals user.UserId
                join center in _context.Centers.AsNoTracking() on teacher.CenterId equals center.CenterId
                where !centerId.HasValue || teacher.CenterId == centerId.Value
                orderby user.FirstName
                select new TeacherDTO(
                    teacher.TeacherId,
                    teacher.UserId,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber,
                    teacher.IsActive,
                    teacher.JoinedAt,
                    center.CenterName,
                    center.CenterCode,
                    center.CenterId

                    )
                { CenterId = teacher.CenterId }
            ).ToListAsync();
        }

        public async Task<TeacherDTO?> GetTeacherById(int id, int? centerId = null)
        {
            return await (
                from teacher in _context.Teachers.AsNoTracking()
                join user in _context.Users.AsNoTracking() on teacher.UserId equals user.UserId
                join center in _context.Centers.AsNoTracking() on teacher.CenterId equals center.CenterId
                where teacher.TeacherId == id && (!centerId.HasValue || teacher.CenterId == centerId.Value)
                select new TeacherDTO(
                    teacher.TeacherId,
                    teacher.UserId,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber,
                    teacher.IsActive,
                    teacher.JoinedAt,
                    center.CenterName,
                    center.CenterCode,
                    center.CenterId


                    )
                { CenterId = teacher.CenterId }
            ).FirstOrDefaultAsync();
        }

        public Task<bool> EmailExists(string email, int? exceptUserId = null)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var users = _context.Users.AsNoTracking().AsQueryable();

            if (exceptUserId.HasValue)
                users = users.Where(user => user.UserId != exceptUserId.Value);

            return users.AnyAsync(user => user.Email.ToLower() == normalizedEmail);
        }

        public async Task<TeacherDTO?> CreateTeacher(TeacherWriteDTO input)
        {
            var roleId = await _context.Roles
                .Where(role => role.RoleName.ToLower() == "teacher")
                .Select(role => (int?)role.RoleId)
                .FirstOrDefaultAsync();

            if (!roleId.HasValue)
                return null;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var centerId = input.CenterId!.Value;
            var user = new User
            {
                RoleId = roleId.Value,
                CenterId = centerId,
                FirstName = input.FirstName.Trim(),
                LastName = input.LastName.Trim(),
                Email = input.Email.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password!),
                PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim(),
                IsActive = input.IsActive
            };

            var teacher = new Teacher
            {
                User = user,
                CenterId = centerId,
                IsActive = input.IsActive,
                JoinedAt = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToTeacherDto(teacher, user);
        }

        public async Task<TeacherDTO?> UpdateTeacher(int teacherId, TeacherWriteDTO input, int? centerId = null)
        {
            var teacher = await _context.Teachers
                .Include(item => item.User)
                .FirstOrDefaultAsync(item => item.TeacherId == teacherId
                    && (!centerId.HasValue || item.CenterId == centerId.Value));

            if (teacher == null)
                return null;

            var user = teacher.User;
            var normalizedEmail = input.Email.Trim().ToLowerInvariant();
            var emailChanged = !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
            user.FirstName = input.FirstName.Trim();
            user.LastName = input.LastName.Trim();
            user.Email = normalizedEmail;
            user.PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim();
            user.IsActive = input.IsActive;
            teacher.IsActive = input.IsActive;

            if (emailChanged || !input.IsActive)
            {
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                user.RefreshTokenRevokedAt = DateTime.UtcNow;
            }

            if (input.CenterId.HasValue)
            {
                teacher.CenterId = input.CenterId.Value;
                user.CenterId = input.CenterId.Value;
            }

            await _context.SaveChangesAsync();
            return ToTeacherDto(teacher, user);
        }

        public async Task<bool> DeleteTeacher(int teacherId, int? centerId = null)
        {
            var teacher = await _context.Teachers
                .Include(item => item.User)
                .FirstOrDefaultAsync(item => item.TeacherId == teacherId
                    && (!centerId.HasValue || item.CenterId == centerId.Value));

            if (teacher == null)
                return false;

            teacher.User.IsActive = false;
            teacher.User.RefreshTokenHash = null;
            teacher.User.RefreshTokenExpiresAt = null;
            teacher.User.RefreshTokenRevokedAt = DateTime.UtcNow;

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();
            return true;
        }

        private static TeacherDTO ToTeacherDto(Teacher teacher, User user)
        {
            return new TeacherDTO(
                teacher.TeacherId,
                teacher.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                teacher.IsActive,
                teacher.JoinedAt)
            { CenterId = teacher.CenterId };
        }
    }
}
