using Business_Layer;
using Data_Access_Layer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace QuranCenterManager_API.Controllers;

[Authorize]
[ApiController]
[Route("api/Users")]
public class UserController : ControllerBase
{
    private const string AdminRoles = "PlatformAdmin,Admin";

    private readonly UserBusinessLayer _business;
    private readonly AuditBusinessLayer _audit;

    public UserController(UserBusinessLayer business, AuditBusinessLayer audit)
    {
        _business = business;
        _audit = audit;
    }

    private bool IsPlatformAdmin => User.IsInRole("PlatformAdmin");

    // حسابات PlatformAdmin لا يغيّرها إلا PlatformAdmin، منعاً لتصعيد الصلاحيات.
    private bool CanTouchRole(string? roleName)
        => !string.Equals(roleName, "PlatformAdmin", StringComparison.Ordinal) || IsPlatformAdmin;

    private static string FullName(string first, string last) => $"{first} {last}".Trim();

    [HttpGet]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _business.GetAllUsers();
        return Ok(users ?? new List<UserDTO>());
    }

    [HttpGet("Roles")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> GetRoles()
    {
        return Ok(await _business.GetRoles());
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetUserById(int userId)
    {
        if (userId < 1)
            return BadRequest("Invalid user ID.");

        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!User.IsInRole("Admin") && !User.IsInRole("PlatformAdmin")
            && callerId != userId.ToString())
            return Forbid();

        var user = await _business.GetUserById(userId);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> CreateUser([FromBody] UserDTO input)
    {
        if (input is null
            || string.IsNullOrWhiteSpace(input.FirstName)
            || string.IsNullOrWhiteSpace(input.LastName)
            || string.IsNullOrWhiteSpace(input.Email)
            || string.IsNullOrWhiteSpace(input.Password)
            || input.Password.Length < 8
            || input.RoleId < 1)
        {
            return BadRequest("الاسم الأول والأخير والبريد والدور وكلمة مرور من 8 أحرف على الأقل مطلوبة.");
        }

        var email = input.Email.Trim().ToLowerInvariant();
        if (email.Length > 255 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return BadRequest("أدخل بريداً إلكترونياً صحيحاً.");

        if (input.FirstName.Trim().Length > 100 || input.LastName.Trim().Length > 100
            || (input.PhoneNumber?.Length ?? 0) > 20)
        {
            return BadRequest("الاسم أو رقم الهاتف يتجاوز الطول المسموح.");
        }

        var roleName = await _business.GetRoleNameById(input.RoleId);
        if (roleName is null)
            return BadRequest("الدور غير صالح.");

        // المدرس والطالب يُنشآن من صفحتيهما لأن لهما سجلاً إضافياً مرتبطاً بالحساب.
        if (roleName is "Teacher" or "Student")
            return BadRequest("أنشئ المدرسين من صفحة المدرسين، والطلاب من التسجيل.");

        if (!CanTouchRole(roleName))
            return Forbid();

        if (roleName == "CenterAdmin")
        {
            if (!input.CenterId.HasValue || !await _business.CenterIsActive(input.CenterId.Value))
                return BadRequest("اختر مركزاً نشطاً لمدير المركز.");
        }
        else
        {
            input.CenterId = null;
        }

        try
        {
            if (await _business.EmailExists(email))
                return Conflict("البريد الإلكتروني مستخدم مسبقًا.");

            var created = await _business.CreateUser(new UserDTO
            {
                RoleId = input.RoleId,
                CenterId = input.CenterId,
                FirstName = input.FirstName.Trim(),
                LastName = input.LastName.Trim(),
                Email = email,
                PhoneNumber = input.PhoneNumber?.Trim(),
                Password = input.Password,
                IsActive = input.IsActive
            });

            if (!created)
                return StatusCode(StatusCodes.Status500InternalServerError, "تعذر إنشاء الحساب.");

            await this.LogAuditAsync(_audit, "user.create", "User", null,
                FullName(input.FirstName, input.LastName), $"الدور: {roleName}، البريد: {email}");

            return StatusCode(StatusCodes.Status201Created, new { message = "تم إنشاء الحساب بنجاح." });
        }
        catch (DbUpdateException)
        {
            return Conflict("تعذر إنشاء الحساب. قد يكون البريد مستخدمًا أو توجد قيمة مرتبطة غير صحيحة.");
        }
    }

    [HttpPut("{userId:int}/password")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> ResetPassword(int userId, [FromBody] ResetPasswordRequest request)
    {
        if (userId < 1)
            return BadRequest("Invalid user ID.");

        if (request is null || string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest("كلمة المرور الجديدة يجب ألا تقل عن 8 أحرف.");

        var target = await _business.Find(userId);
        if (target is null)
            return NotFound("المستخدم غير موجود.");

        var targetRole = await _business.GetUserRoleName(userId);
        if (!CanTouchRole(targetRole))
            return Forbid();

        var updated = await _business.UpdatePassword(userId, request.NewPassword);
        if (!updated)
            return NotFound("المستخدم غير موجود.");

        await this.LogAuditAsync(_audit, "user.password_reset", "User", userId,
            FullName(target.FirstName, target.LastName), $"الدور: {targetRole}");

        return Ok(new { message = "تمت إعادة تعيين كلمة المرور. سجّل الدخول مجددًا على الأجهزة الأخرى." });
    }

    [HttpPut("{userId:int}/status")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> UpdateStatus(int userId, [FromBody] UserStatusDTO input)
    {
        if (userId < 1 || input is null)
            return BadRequest("Invalid user ID.");

        if (this.GetUserId() == userId)
            return BadRequest("لا يمكنك تعطيل حسابك بنفسك.");

        var target = await _business.Find(userId);
        if (target is null)
            return NotFound("المستخدم غير موجود.");

        var targetRole = await _business.GetUserRoleName(userId);
        if (!CanTouchRole(targetRole))
            return Forbid();

        if (!await _business.SetActiveAsync(userId, input.IsActive))
            return NotFound("المستخدم غير موجود.");

        await this.LogAuditAsync(_audit, input.IsActive ? "user.enable" : "user.disable", "User", userId,
            FullName(target.FirstName, target.LastName), $"الدور: {targetRole}");

        return Ok(new { userId, isActive = input.IsActive });
    }

    [HttpPut("{userId:int}")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserDTO input)
    {
        if (userId < 1
            || input is null
            || string.IsNullOrWhiteSpace(input.FirstName)
            || string.IsNullOrWhiteSpace(input.LastName)
            || string.IsNullOrWhiteSpace(input.Email)
            || input.RoleId < 1)
        {
            return BadRequest("بيانات المستخدم غير صالحة.");
        }

        var normalizedEmail = input.Email.Trim().ToLowerInvariant();
        if (normalizedEmail.Length > 255
            || input.FirstName.Trim().Length > 100
            || input.LastName.Trim().Length > 100
            || (input.PhoneNumber?.Length ?? 0) > 20
            || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(normalizedEmail))
        {
            return BadRequest("أدخل بريداً إلكترونياً صحيحاً، وتأكد من أطوال الحقول.");
        }

        try
        {
            var existing = await _business.Find(userId);
            if (existing is null)
                return NotFound("المستخدم غير موجود.");

            var currentRole = await _business.GetUserRoleName(userId);
            var newRole = await _business.GetRoleNameById(input.RoleId);
            if (newRole is null)
                return BadRequest("الدور غير صالح.");

            if (!CanTouchRole(currentRole) || !CanTouchRole(newRole))
                return Forbid();

            if (this.GetUserId() == userId && (input.RoleId != existing.RoleId || !input.IsActive))
                return BadRequest("لا يمكنك تغيير دورك أو تعطيل حسابك بنفسك.");

            if (newRole == "CenterAdmin")
            {
                if (!input.CenterId.HasValue)
                    return BadRequest("اختر مركزاً لمدير المركز.");
            }

            var dto = new UserDTO
            {
                UserId = userId,
                RoleId = input.RoleId,
                CenterId = input.CenterId,
                FirstName = input.FirstName,
                LastName = input.LastName,
                Email = normalizedEmail,
                PhoneNumber = input.PhoneNumber,
                IsActive = input.IsActive
            };

            if (!await _business.UpdateUserAsync(dto))
                return StatusCode(StatusCodes.Status500InternalServerError, "تعذر تحديث المستخدم.");

            var changes = new List<string>();
            if (currentRole != newRole) changes.Add($"الدور: {currentRole} ← {newRole}");
            if (existing.IsActive != input.IsActive) changes.Add(input.IsActive ? "تفعيل" : "تعطيل");
            if (existing.CenterId != input.CenterId) changes.Add("تغيير المركز");
            if (!string.Equals(existing.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)) changes.Add("تغيير البريد");

            await this.LogAuditAsync(_audit, "user.update", "User", userId,
                FullName(input.FirstName, input.LastName),
                changes.Count > 0 ? string.Join("، ", changes) : "تعديل بيانات");

            return Ok();
        }
        catch (DbUpdateException)
        {
            return Conflict("تعذر تحديث المستخدم. قد يكون البريد مستخدمًا أو توجد قيمة مرتبطة غير صحيحة.");
        }
    }

    [HttpDelete("{userId:int}")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        if (userId < 1)
            return BadRequest("Invalid user ID.");

        if (this.GetUserId() == userId)
            return BadRequest("لا يمكنك حذف حسابك بنفسك.");

        var target = await _business.Find(userId);
        if (target is null)
            return NotFound();

        var targetRole = await _business.GetUserRoleName(userId);
        if (!CanTouchRole(targetRole))
            return Forbid();

        try
        {
            if (!await _business.DeleteUser(userId))
                return NotFound();

            await this.LogAuditAsync(_audit, "user.delete", "User", userId,
                FullName(target.FirstName, target.LastName), $"الدور: {targetRole}، البريد: {target.Email}");

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict("لا يمكن حذف هذا المستخدم لأنه مرتبط بسجلات أخرى. عطّل حسابه بدلاً من حذفه.");
        }
    }

    [HttpGet("Count", Name = "GetUserCount")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> GetUserCount()
    {
        var count = await _business.getCountUsers();
        return Ok(new { Count = count });
    }
}

public sealed class ResetPasswordRequest
{
    public string? NewPassword { get; set; }
}
