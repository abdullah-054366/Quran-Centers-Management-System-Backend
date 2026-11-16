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
    private readonly UserBusinessLayer _business;
    public UserController(UserBusinessLayer business)
    {
        _business = business;
    }

    [HttpGet]
    [Authorize(Roles = "PlatformAdmin,Admin")]
    public async Task<IActionResult> GetAllUsers()
    {

        var users = await _business.GetAllUsers();

        return Ok(users ?? new List<UserDTO>());
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
    [Authorize(Roles = "PlatformAdmin,Admin")]
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
            return BadRequest("First name, last name, email, a valid role, and a password of at least 8 characters are required.");
        }

        var email = input.Email.Trim().ToLowerInvariant();
        if (email.Length > 255 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return BadRequest("A valid email address is required.");

        if (input.FirstName.Trim().Length > 100 || input.LastName.Trim().Length > 100
            || (input.PhoneNumber?.Length ?? 0) > 20)
        {
            return BadRequest("Name or phone number exceeds the allowed length.");
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

            return created
                ? StatusCode(StatusCodes.Status201Created, new { message = "تم إنشاء الحساب بنجاح." })
                : StatusCode(StatusCodes.Status500InternalServerError, "تعذر إنشاء الحساب.");
        }
        catch (DbUpdateException)
        {
            return Conflict("تعذر إنشاء الحساب. قد يكون البريد مستخدمًا أو توجد قيمة مرتبطة غير صحيحة.");
        }
    }

    [HttpPut("{userId:int}/password")]
    [Authorize(Roles = "PlatformAdmin,Admin")]
    public async Task<IActionResult> ResetPassword(int userId, [FromBody] ResetPasswordRequest request)
    {
        if (userId < 1)
            return BadRequest("Invalid user ID.");

        if (request is null || string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest("A new password of at least 8 characters is required.");

        var updated = await _business.UpdatePassword(userId, request.NewPassword);
        return updated
            ? Ok(new { message = "تمت إعادة تعيين كلمة المرور. سجّل الدخول مجددًا على الأجهزة الأخرى." })
            : NotFound("User not found.");
    }

    [HttpPut("{userId:int}")]
    [Authorize(Roles = "PlatformAdmin,Admin")]
    public async Task<IActionResult> UpdateUser(
    int userId,
    [FromBody] UserDTO input)
    {
        if (userId < 1
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
            var userUpdate = await _business.Find(userId);

            if (userUpdate is null)
                return NotFound("User not found.");

            userUpdate.RoleId = input.RoleId;
            userUpdate.CenterId = input.CenterId;
            userUpdate.FirstName = input.FirstName;
            userUpdate.LastName = input.LastName;
            userUpdate.Email = normalizedEmail;
            userUpdate.PhoneNumber = input.PhoneNumber;
            userUpdate.IsActive = input.IsActive;
            userUpdate.Password = input.Password;
        

            var saved = await userUpdate.Save();

            if (!saved)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Could not update user.");

            return Ok();
        }
        catch (DbUpdateException)
        {
            return Conflict(
                "Could not update user. Email may already exist or a related ID is invalid.");
        }
    }
    [HttpDelete("{userId:int}")]
    [Authorize(Roles = "PlatformAdmin,Admin")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        if (userId < 1) 
            return BadRequest("Invalid user ID.");
        try 
        {
            return await _business.DeleteUser(userId) ? NoContent() : NotFound(); 
        }
        catch (DbUpdateException) 
        {
            return Conflict("لا يمكن حذف هذا المستخدم لأنه مرتبط بسجلات أخرى."); 
        }
    }



    [HttpGet("Count", Name = "GetUserCount")]
    [Authorize(Roles = "PlatformAdmin,Admin")]
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

