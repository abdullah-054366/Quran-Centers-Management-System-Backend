using Business_Layer;
using Data_Access_Layer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace QuranCenterManager_API.Controllers
{
    [ApiController]
    [Route("api/Teachers")]
    public class TeacherController : ControllerBase
    {
        private readonly TeacherBusinessLayer _business;

        public TeacherController(TeacherBusinessLayer business)
        {
            _business = business;
        }

        [HttpGet("All", Name = "GetAllTeachers")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]
        public async Task<ActionResult<List<TeacherDTO>>> GetAllTeachers()
        {
            if (!TryGetCenterScope(out var centerId))
                return Forbid();

            var teachers = await _business.GetAllTeachers(centerId);
            return Ok(teachers);
        }

        [HttpGet("{id:int}", Name = "GetTeacherById")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]
        public async Task<ActionResult<TeacherDTO>> GetTeacherById(int id)
        {
            if (id < 1)
                return BadRequest("Invalid teacher ID.");

            if (!TryGetCenterScope(out var centerId))
                return Forbid();

            var teacher = await _business.GetTeacherById(id, centerId);
            if (teacher is null)
                return NotFound();

            return Ok(teacher);
        }

        [HttpPost]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]
        public async Task<ActionResult<TeacherDTO>> CreateTeacher([FromBody] TeacherWriteDTO input)
        {
            if (!TryGetCenterScope(out var centerScope))
                return Forbid();

            if (input is null
                || (!centerScope.HasValue && (!input.CenterId.HasValue || input.CenterId.Value < 1))
                || string.IsNullOrWhiteSpace(input.FirstName)
                || string.IsNullOrWhiteSpace(input.LastName)
                || string.IsNullOrWhiteSpace(input.Email)
                || string.IsNullOrWhiteSpace(input.Password)
                || input.Password.Length < 8)
            {
                return BadRequest("الاسم والبريد والمركز وكلمة مرور من 8 أحرف على الأقل مطلوبة.");
            }

            // لا يسمح لمدير المركز باختيار مركز مختلف عن مركزه.
            if (centerScope.HasValue)
                input.CenterId = centerScope;

            var email = input.Email.Trim().ToLowerInvariant();
            if (!IsValidEmail(email))
                return BadRequest("أدخل بريدًا إلكترونيًا صحيحًا.");

            if (input.FirstName.Trim().Length > 100 || input.LastName.Trim().Length > 100
                || email.Length > 255 || (input.PhoneNumber?.Length ?? 0) > 20)
            {
                return BadRequest("تجاوز أحد الحقول الحد المسموح لطوله.");
            }

            if (await _business.EmailExists(email))
                return Conflict("البريد الإلكتروني مستخدم مسبقًا.");

            input.Email = email;

            try
            {
                var teacher = await _business.CreateTeacher(input);
                if (teacher is null)
                    return Conflict("لم يتم العثور على دور Teacher في قاعدة البيانات.");

                return CreatedAtAction(
                    nameof(GetTeacherById),
                    new { id = teacher.TeacherId },
                    teacher);
            }
            catch (DbUpdateException)
            {
                return Conflict("تعذر إنشاء المدرس. تحقق من البريد والمركز المرتبطين به.");
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]
        public async Task<ActionResult<TeacherDTO>> UpdateTeacher(int id, [FromBody] TeacherWriteDTO input)
        {
            if (id < 1)
                return BadRequest("Invalid teacher ID.");

            if (!TryGetCenterScope(out var centerId))
                return Forbid();

            if (input is null
                || string.IsNullOrWhiteSpace(input.FirstName)
                || string.IsNullOrWhiteSpace(input.LastName)
                || string.IsNullOrWhiteSpace(input.Email)
                || (input.CenterId.HasValue && input.CenterId.Value < 1))
            {
                return BadRequest("الاسم والبريد الإلكتروني مطلوبان.");
            }

            var email = input.Email.Trim().ToLowerInvariant();
            if (!IsValidEmail(email))
                return BadRequest("أدخل بريدًا إلكترونيًا صحيحًا.");

            if (input.FirstName.Trim().Length > 100 || input.LastName.Trim().Length > 100
                || email.Length > 255 || (input.PhoneNumber?.Length ?? 0) > 20)
            {
                return BadRequest("تجاوز أحد الحقول الحد المسموح لطوله.");
            }

            var existingTeacher = await _business.GetTeacherById(id, centerId);
            if (existingTeacher is null)
                return NotFound();

            if (await _business.EmailExists(email, existingTeacher.UserId))
                return Conflict("البريد الإلكتروني مستخدم مسبقًا.");

            input.Email = email;
            if (centerId.HasValue)
                input.CenterId = null;

            try
            {
                var teacher = await _business.UpdateTeacher(id, input, centerId);
                if (teacher is null)
                    return NotFound();

                return Ok(teacher);
            }
            catch (DbUpdateException)
            {
                return Conflict("تعذر تحديث بيانات المدرس. تحقق من البريد أو المركز.");
            }
        }

        [HttpDelete("{id:int}", Name = "DeleteTeacher")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            if (id < 1)
                return BadRequest("Invalid teacher ID.");

            if (!TryGetCenterScope(out var centerId))
                return Forbid();

            try
            {
                var deleted = await _business.DeleteTeacher(id, centerId);
                if (!deleted)
                    return NotFound();

                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict("لا يمكن حذف المدرس لارتباطه بحلقات أو سجلات محفوظة.");
            }
        }

        private static bool IsValidEmail(string email)
        {
            return new EmailAddressAttribute().IsValid(email);
        }

        private bool TryGetCenterScope(out int? centerId)
        {
            centerId = null;
            if (User.IsInRole("PlatformAdmin") || User.IsInRole("Admin"))
                return true;

            if (!User.IsInRole("CenterAdmin")
                || !int.TryParse(User.FindFirst("centerId")?.Value, out var parsedCenterId)
                || parsedCenterId < 1)
            {
                return false;
            }

            centerId = parsedCenterId;
            return true;
        }
    }
}
