using System.Diagnostics;
using System.Reflection;
using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;

namespace Data_Access_Layer
{
    public enum PasswordChangeResult { Ok, WrongCurrent, NotFound }

    public class SettingsData
    {
        private readonly HalaqaDbContext _context;

        public SettingsData(HalaqaDbContext context)
        {
            _context = context;
        }

        // ───────── الحساب ─────────
        public Task<ProfileDTO?> GetProfile(int userId)
        {
            return _context.Users.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new ProfileDTO
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    RoleName = u.Role.RoleName,
                    CenterId = u.CenterId,
                    CenterName = u.Center != null ? u.Center.CenterName : null
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ProfileDTO?> UpdateProfile(int userId, string firstName, string lastName, string? phone)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null) return null;

            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone;
            await _context.SaveChangesAsync();
            return await GetProfile(userId);
        }

        public async Task<PasswordChangeResult> ChangePassword(int userId, string current, string next)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null) return PasswordChangeResult.NotFound;

            if (string.IsNullOrWhiteSpace(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(current, user.PasswordHash))
                return PasswordChangeResult.WrongCurrent;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(next);
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            user.RefreshTokenRevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return PasswordChangeResult.Ok;
        }

        public async Task<bool> LogoutAll(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null) return false;

            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            user.RefreshTokenRevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // ───────── إعدادات المركز ─────────
        public async Task<CenterSettingsDTO?> GetCenterSettings(int centerId)
        {
            var center = await _context.Centers.AsNoTracking().FirstOrDefaultAsync(c => c.CenterId == centerId);
            if (center is null) return null;

            var s = await _context.CenterSettings.AsNoTracking().FirstOrDefaultAsync(x => x.CenterId == centerId)
                    ?? new CenterSetting { CenterId = centerId };

            return new CenterSettingsDTO
            {
                CenterId = center.CenterId,
                CenterName = center.CenterName,
                CenterCode = center.CenterCode,
                CodeEnabled = center.CodeEnabled,
                Phone = s.Phone,
                Address = s.Address,
                DefaultDays = s.DefaultDays,
                DefaultStartTime = s.DefaultStartTime,
                DefaultEndTime = s.DefaultEndTime,
                LateAfterMinutes = s.LateAfterMinutes,
                ExcusedCountsAsAttended = s.ExcusedCountsAsAttended
            };
        }

        public async Task<CenterSettingsDTO?> UpdateCenterSettings(int centerId, CenterSettingsUpdateDTO input)
        {
            var center = await _context.Centers.FirstOrDefaultAsync(c => c.CenterId == centerId);
            if (center is null) return null;

            if (!string.IsNullOrWhiteSpace(input.CenterName))
                center.CenterName = input.CenterName.Trim();

            var s = await _context.CenterSettings.FirstOrDefaultAsync(x => x.CenterId == centerId);
            if (s is null)
            {
                s = new CenterSetting { CenterId = centerId };
                _context.CenterSettings.Add(s);
            }

            s.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
            s.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
            s.DefaultDays = input.DefaultDays ?? s.DefaultDays;
            s.DefaultStartTime = input.DefaultStartTime ?? s.DefaultStartTime;
            s.DefaultEndTime = input.DefaultEndTime ?? s.DefaultEndTime;
            s.LateAfterMinutes = input.LateAfterMinutes;
            s.ExcusedCountsAsAttended = input.ExcusedCountsAsAttended;
            s.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _context.SaveChangesAsync();
            return await GetCenterSettings(centerId);
        }

        public async Task<bool> SetSelfRegistration(int centerId, bool enabled)
        {
            var center = await _context.Centers.FirstOrDefaultAsync(c => c.CenterId == centerId);
            if (center is null) return false;
            center.CodeEnabled = enabled;
            await _context.SaveChangesAsync();
            return true;
        }

        // ───────── حالة النظام ─────────
        public async Task<SystemStatusDTO> GetSystemStatus(string environmentName)
        {
            var status = new SystemStatusDTO
            {
                Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0",
                Environment = environmentName,
                ServerTimeUtc = DateTime.UtcNow,
                StartedAtUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime()
            };

            try
            {
                status.DatabaseOk = await _context.Database.CanConnectAsync();
                if (!status.DatabaseOk) return status;

                status.Users = await _context.Users.CountAsync();
                status.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
                status.Centers = await _context.Centers.CountAsync();
                status.ActiveCenters = await _context.Centers.CountAsync(c => c.IsActive);
                status.Teachers = await _context.Teachers.CountAsync();
                status.Students = await _context.Students.CountAsync();
                status.Halaqat = await _context.Halaqats.CountAsync();
                status.MemorizationRecords = await _context.MemorizationRecords.CountAsync();
                status.AttendanceRecords = await _context.Attendances.CountAsync();

                try
                {
                    status.AuditEntries = await _context.AuditLogs.CountAsync();
                    status.LastAuditAt = await _context.AuditLogs.MaxAsync(a => (DateTime?)a.CreatedAt);
                }
                catch { /* جدول التدقيق قد لا يكون منشأً بعد */ }

                try
                {
                    status.DatabaseSizeBytes = await _context.Database
                        .SqlQueryRaw<long>("SELECT pg_database_size(current_database()) AS \"Value\"")
                        .FirstOrDefaultAsync();
                }
                catch { /* ليست PostgreSQL أو لا صلاحية */ }
            }
            catch
            {
                status.DatabaseOk = false;
            }

            return status;
        }
    }
}
