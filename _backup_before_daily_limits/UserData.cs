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
                orderby user.FirstName
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
            ).ToListAsync();
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

        public async Task<bool> UpdateUser(UserDTO input )
        {
            try
            {
                var user = await _context.Users.FindAsync(input.UserId);

                if (user == null)
                    return false;

                user.RoleId = input.RoleId;
                user.CenterId = input.CenterId;
                user.FirstName = input.FirstName.Trim();
                user.LastName = input.LastName.Trim();
                user.Email = input.Email.Trim();
                user.PhoneNumber = input.PhoneNumber;
                user.IsActive = input.IsActive;

                // إذا لم تُرسل كلمة مرور جديدة، تبقى القديمة كما هي.
                //if (!string.IsNullOrWhiteSpace(passwordHash))
                //    user.PasswordHash = passwordHash;

                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUser(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user is null)
                    return false;

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }   
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


