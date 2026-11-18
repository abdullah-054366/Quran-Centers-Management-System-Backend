using Business_Layer;
using Data_Access_Layer;
using Data_Access_Layer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace QuranCenterManager_API.Controllers
{
    /// <summary>إعدادات حساب المستخدم الحالي (كل الأدوار).</summary>
    [ApiController]
    [Route("api/Account")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly SettingsBusinessLayer _business;
        private readonly AuditBusinessLayer _audit;

        public AccountController(SettingsBusinessLayer business, AuditBusinessLayer audit)
        {
            _business = business;
            _audit = audit;
        }

        [HttpGet("me")]
        public async Task<ActionResult<ProfileDTO>> GetMe()
        {
            var userId = this.GetUserId();
            if (userId is null) return Unauthorized();
            var profile = await _business.GetProfile(userId.Value);
            return profile is null ? NotFound() : Ok(profile);
        }

        [HttpPut("me")]
        public async Task<ActionResult<ProfileDTO>> UpdateMe([FromBody] ProfileUpdateDTO? input)
        {
            var userId = this.GetUserId();
            if (userId is null) return Unauthorized();

            var error = SettingsBusinessLayer.ValidateProfile(input);
            if (error is not null) return BadRequest(error);

            var profile = await _business.UpdateProfile(userId.Value, input!);
            if (profile is null) return NotFound();

            await this.LogAuditAsync(_audit, "account.update_profile", "User", userId, $"{profile.FirstName} {profile.LastName}");
            return Ok(profile);
        }

        [HttpPost("change-password")]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO? input)
        {
            var userId = this.GetUserId();
            if (userId is null) return Unauthorized();

            var error = SettingsBusinessLayer.ValidatePassword(input);
            if (error is not null) return BadRequest(error);

            var result = await _business.ChangePassword(userId.Value, input!);
            if (result == PasswordChangeResult.NotFound) return NotFound();
            if (result == PasswordChangeResult.WrongCurrent) return BadRequest("كلمة المرور الحالية غير صحيحة.");

            await this.LogAuditAsync(_audit, "account.change_password", "User", userId, null);
            return Ok("تم تغيير كلمة المرور. سجّل الدخول من جديد.");
        }

        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = this.GetUserId();
            if (userId is null) return Unauthorized();
            if (!await _business.LogoutAll(userId.Value)) return NotFound();

            await this.LogAuditAsync(_audit, "account.logout_all", "User", userId, null);
            return Ok("تم إنهاء جميع الجلسات. قد تبقى الجلسة الحالية صالحة حتى انتهاء رمز الدخول (30 دقيقة).");
        }
    }
}
