using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
namespace Data_Access_Layer
{
    public partial class UserData
    {
        private readonly HalaqaDbContext _context;

        public UserData(HalaqaDbContext context)
        {
            _context = context;
        }

        public async Task<User?> FindUserForAuthentication(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            return await _context.Users
                .Include(user => user.Role)
                .Include(user => user.Teacher)
                .Include(user => user.Student)
                .FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail);
        }

        public async Task SaveAuthenticationChanges()
        {
            await _context.SaveChangesAsync();
        }

        public Task<bool> EmailExists(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return _context.Users.AsNoTracking()
                .AnyAsync(user => user.Email.ToLower() == normalizedEmail);
        }

        public async Task<List<UserDTO>> GetAllUsers()
        {
            return await (
                from user in _context.Users.AsNoTracking()
                join role in _context.Roles.AsNoTracking()
                    on user.RoleId equals role.RoleId
                join center in _context.Centers.AsNoTracking()
                    on user.CenterId equals center.CenterId into centers
                from center in centers.DefaultIfEmpty()
                orderby user.FirstName, user.LastName
                select new UserDTO
                {
                    UserId = user.UserId,
                    RoleId = user.RoleId,
                    CenterId = user.CenterId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    IsActive = user.IsActive,
                    RoleName = role.RoleName,
                    CenterName = center != null ? center.CenterName : null,
                    CreatedAt = user.CreatedAt
                }
            ).ToListAsync();
        }

        public async Task<List<RoleDTO>> GetRoles()
        {
            return await _context.Roles.AsNoTracking()
                .OrderBy(role => role.RoleId)
                .Select(role => new RoleDTO { RoleId = role.RoleId, RoleName = role.RoleName })
                .ToListAsync();
        }

        public Task<string?> GetRoleNameById(int roleId)
        {
            return _context.Roles.AsNoTracking()
                .Where(role => role.RoleId == roleId)
                .Select(role => (string?)role.RoleName)
                .FirstOrDefaultAsync();
        }

        public Task<string?> GetUserRoleName(int userId)
        {
            return _context.Users.AsNoTracking()
                .Where(user => user.UserId == userId)
                .Select(user => (string?)user.Role.RoleName)
                .FirstOrDefaultAsync();
        }

        public Task<bool> CenterIsActive(int centerId)
        {
            return _context.Centers.AsNoTracking()
                .AnyAsync(center => center.CenterId == centerId && center.IsActive);
        }

        public async Task<bool> SetActiveAsync(int userId, bool isActive)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsActive = isActive;
            if (!isActive)
            {
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                user.RefreshTokenRevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserDTO?> GetUserById(int userId)
        {
            return await (
                from user in _context.Users.AsNoTracking()
                join role in _context.Roles.AsNoTracking()
                on user.RoleId equals role.RoleId
                where user.UserId == userId
                select new UserDTO
                {
                    UserId = user.UserId,
                    RoleId = user.RoleId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    IsActive = user.IsActive
                }
            ).FirstOrDefaultAsync();
        }

        public async Task<UserDTO> FindUser(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return null;
                    
                return new UserDTO
                {
                    UserId = user.UserId,
                    RoleId = user.RoleId,
                    CenterId = user.CenterId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    IsActive = user.IsActive
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> AddUser(UserDTO input)
        {
            var user = new User
            {
                RoleId = input.RoleId,
                CenterId = input.CenterId,
                FirstName = input.FirstName.Trim(),
                LastName = input.LastName.Trim(),
                Email = input.Email.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password
                    ?? throw new ArgumentException("A password is required to create a user.", nameof(input))),
                PhoneNumber = input.PhoneNumber,
                IsActive = input.IsActive
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            input.UserId = user.UserId;
            return true;
        }

        // مركز يقبل التسجيل برمزه: نشط والكود مفعّل.
        public async Task<int?> FindCenterIdForRegistration(string centerCode)
        {
            var code = centerCode.Trim().ToUpperInvariant();
            return await _context.Centers.AsNoTracking()
                .Where(center => center.CenterCode.ToUpper() == code
                    && center.IsActive
                    && center.CodeEnabled)
                .Select(center => (int?)center.CenterId)
                .FirstOrDefaultAsync();
        }

        // ينشئ حساب طالب وسجل الطالب معاً في عملية حفظ واحدة،
        // ويُسنده إلى الحلقة التي اختارها عند التسجيل إن وُجدت.
        public async Task<bool> RegisterStudentAsync(UserDTO input, int centerId, int? halaqaId = null)
        {
            var roleId = await _context.Roles.AsNoTracking()
                .Where(role => role.RoleName == "Student")
                .Select(role => (int?)role.RoleId)
                .FirstOrDefaultAsync();

            if (!roleId.HasValue)
                return false;

            var user = new User
            {
                RoleId = roleId.Value,
                CenterId = centerId,
                FirstName = input.FirstName.Trim(),
                LastName = input.LastName.Trim(),
                Email = input.Email.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password
                    ?? throw new ArgumentException("A password is required.", nameof(input))),
                PhoneNumber = input.PhoneNumber,
                IsActive = true,
                Student = new Student
                {
                    CenterId = centerId,
                    IsActive = true,
                    JoinedAt = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3))
                }
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (halaqaId.HasValue)
            {
                _context.HalaqaStudents.Add(new HalaqaStudent
                {
                    CenterId = centerId,
                    HalaqaId = halaqaId.Value,
                    StudentId = user.Student!.StudentId,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3))
                });
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> UpdateUser(UserDTO input)
        {
            // لا نبتلع الاستثناءات هنا: DbUpdateException يُعالَج في الكنترولر ويرجع 409 برسالة مفهومة.
            var user = await _context.Users.FindAsync(input.UserId);

            if (user == null)
                return false;

            user.RoleId = input.RoleId;
            user.CenterId = input.CenterId;
            user.FirstName = input.FirstName.Trim();
            user.LastName = input.LastName.Trim();
            user.Email = input.Email.Trim().ToLowerInvariant();
            user.PhoneNumber = input.PhoneNumber;
            user.IsActive = input.IsActive;

            // تعطيل الحساب أو تغيير دوره ينهي جلسة التجديد فوراً.
            if (!user.IsActive)
            {
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                user.RefreshTokenRevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user is null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePassword(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            user.RefreshTokenRevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<int> getCountUsers()
        {
            try
            {
                return await _context.Users.CountAsync();
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, rethrow it, return a default value, etc.)
                throw new Exception("An error occurred while counting the centers.", ex);
            }
        }


    }
}


