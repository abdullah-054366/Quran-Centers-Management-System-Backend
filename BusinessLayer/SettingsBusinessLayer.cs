using System.Text.RegularExpressions;
using Data_Access_Layer;
using Data_Access_Layer.DTOs;

namespace Business_Layer
{
    public class SettingsBusinessLayer
    {
        private readonly SettingsData _data;
        private readonly CenterData _centers;

        public SettingsBusinessLayer(SettingsData data, CenterData centers)
        {
            _data = data;
            _centers = centers;
        }

        public Task<ProfileDTO?> GetProfile(int userId) => _data.GetProfile(userId);

        /// <returns>رسالة خطأ، أو null عند النجاح.</returns>
        public static string? ValidateProfile(ProfileUpdateDTO? i)
        {
            if (i is null) return "البيانات مطلوبة.";
            if (string.IsNullOrWhiteSpace(i.FirstName) || i.FirstName.Trim().Length > 50) return "الاسم الأول مطلوب (حتى 50 حرفاً).";
            if (string.IsNullOrWhiteSpace(i.LastName) || i.LastName.Trim().Length > 50) return "اسم العائلة مطلوب (حتى 50 حرفاً).";
            if (!string.IsNullOrWhiteSpace(i.PhoneNumber) && !Regex.IsMatch(i.PhoneNumber.Trim(), @"^[0-9+\- ]{7,20}$"))
                return "رقم الجوال غير صحيح.";
            return null;
        }

        public Task<ProfileDTO?> UpdateProfile(int userId, ProfileUpdateDTO i) =>
            _data.UpdateProfile(userId, i.FirstName!.Trim(), i.LastName!.Trim(), i.PhoneNumber?.Trim());

        public static string? ValidatePassword(ChangePasswordDTO? i)
        {
            if (i is null || string.IsNullOrEmpty(i.CurrentPassword)) return "كلمة المرور الحالية مطلوبة.";
            if (string.IsNullOrEmpty(i.NewPassword) || i.NewPassword.Length < 8) return "كلمة المرور الجديدة يجب ألا تقل عن 8 أحرف.";
            if (i.NewPassword.Length > 100) return "كلمة المرور الجديدة طويلة جداً.";
            if (i.NewPassword == i.CurrentPassword) return "كلمة المرور الجديدة يجب أن تختلف عن الحالية.";
            return null;
        }

        public Task<PasswordChangeResult> ChangePassword(int userId, ChangePasswordDTO i) =>
            _data.ChangePassword(userId, i.CurrentPassword!, i.NewPassword!);

        public Task<bool> LogoutAll(int userId) => _data.LogoutAll(userId);

        public Task<CenterSettingsDTO?> GetCenterSettings(int centerId) => _data.GetCenterSettings(centerId);

        public static string? ValidateCenterSettings(CenterSettingsUpdateDTO? i)
        {
            if (i is null) return "البيانات مطلوبة.";
            if (i.CenterName is not null && (i.CenterName.Trim().Length is < 2 or > 150)) return "اسم المركز بين 2 و150 حرفاً.";
            if (!string.IsNullOrWhiteSpace(i.Phone) && !Regex.IsMatch(i.Phone.Trim(), @"^[0-9+\- ]{7,20}$")) return "رقم هاتف المركز غير صحيح.";
            if (i.Address is not null && i.Address.Trim().Length > 250) return "العنوان طويل جداً.";
            if (i.LateAfterMinutes is < 0 or > 120) return "حد التأخر بين 0 و120 دقيقة.";

            if (i.DefaultDays is not null)
            {
                var parts = i.DefaultDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 0 || parts.Length > 7 || parts.Any(p => !int.TryParse(p, out var d) || d is < 0 or > 6) || parts.Distinct().Count() != parts.Length)
                    return "أيام الحلقات غير صحيحة.";
            }

            var timeOk = new Regex(@"^([01]\d|2[0-3]):[0-5]\d$");
            if (i.DefaultStartTime is not null && !timeOk.IsMatch(i.DefaultStartTime)) return "وقت البداية غير صحيح (HH:mm).";
            if (i.DefaultEndTime is not null && !timeOk.IsMatch(i.DefaultEndTime)) return "وقت النهاية غير صحيح (HH:mm).";
            if (i.DefaultStartTime is not null && i.DefaultEndTime is not null
                && string.CompareOrdinal(i.DefaultEndTime, i.DefaultStartTime) <= 0)
                return "وقت النهاية يجب أن يكون بعد وقت البداية.";
            return null;
        }

        public Task<CenterSettingsDTO?> UpdateCenterSettings(int centerId, CenterSettingsUpdateDTO i) =>
            _data.UpdateCenterSettings(centerId, i);

        public Task<bool> SetSelfRegistration(int centerId, bool enabled) => _data.SetSelfRegistration(centerId, enabled);

        public Task<CenterDTO?> RegenerateCode(int centerId) => _centers.RegenerateCenterCode(centerId);

        public Task<SystemStatusDTO> GetSystemStatus(string env) => _data.GetSystemStatus(env);
    }
}
