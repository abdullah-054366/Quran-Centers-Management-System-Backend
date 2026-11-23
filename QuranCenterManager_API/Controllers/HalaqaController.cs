using Business_Layer;
using Data_Access_Layer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace QuranCenterManager_API.Controllers;

[ApiController]
[Route("api/Halaqat")]
[Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin,Teacher")]
public class HalaqaController : ControllerBase
{
    private readonly HalaqaBusinessLayer _business;

    public HalaqaController(HalaqaBusinessLayer business)
    {
        _business = business;
    }

    [HttpGet("All", Name = "GetAllHalaqat")]
    public async Task<ActionResult<List<HalaqaDTO>>> GetAll()
    {
        if (!TryGetScope(out var centerId, out var teacherId))
            return Forbid();

        return Ok(await _business.GetAllAsync(centerId, teacherId));
    }

    [HttpGet("{id:int}", Name = "GetHalaqaById")]
    public async Task<ActionResult<HalaqaDTO>> GetById(int id)
    {
        if (id < 1)
            return BadRequest("Invalid halaqa ID.");

        if (!TryGetScope(out var centerId, out var teacherId))
            return Forbid();

        var halaqa = await _business.GetByIdAsync(id, centerId, teacherId);
        if (halaqa is null)
            return NotFound();

        return Ok(halaqa);
    }

    [HttpGet("{id:int}/Students", Name = "GetHalaqaStudents")]
    public async Task<ActionResult<List<HalaqaStudentDTO>>> GetStudents(int id)
    {
        if (id < 1)
            return BadRequest("Invalid halaqa ID.");

        if (!TryGetScope(out var centerScope, out var teacherScope))
            return Forbid();

        if (await _business.GetByIdAsync(id, centerScope, teacherScope) is null)
            return NotFound();

        return Ok(await _business.GetStudentsAsync(id, centerScope));
    }

    [HttpGet("{id:int}/AvailableStudents", Name = "GetAvailableHalaqaStudents")]
    public async Task<ActionResult<List<StudentDTO>>> GetAvailableStudents(int id)
    {
        if (id < 1)
            return BadRequest("Invalid halaqa ID.");

        if (!TryGetScope(out var centerScope, out var teacherScope))
            return Forbid();

        if (await _business.GetByIdAsync(id, centerScope, teacherScope) is null)
            return NotFound();

        return Ok(await _business.GetAvailableStudentsAsync(id, centerScope));
    }

    [HttpPost("{id:int}/Students", Name = "AssignStudentsToHalaqa")]
    public async Task<ActionResult<List<HalaqaStudentDTO>>> AssignStudents(
        int id,
        [FromBody] HalaqaStudentAssignmentDTO? input)
    {
        if (id < 1
            || input?.StudentIds == null
            || input.StudentIds.Length == 0
            || input.StudentIds.Any(studentId => studentId < 1)
            || input.StudentIds.Distinct().Count() != input.StudentIds.Length)
        {
            return BadRequest("اختر طالباً واحداً على الأقل.");
        }

        if (!TryGetScope(out var centerScope, out var teacherScope))
            return Forbid();

        var halaqa = await _business.GetByIdAsync(id, centerScope, teacherScope);
        if (halaqa is null)
            return NotFound();
        if (!halaqa.IsActive)
            return BadRequest("لا يمكن إضافة طلاب إلى حلقة موقوفة.");

        try
        {
            var assigned = await _business.AssignStudentsAsync(id, input.StudentIds, centerScope);
            if (!assigned)
                return Conflict("تأكد أن الطلاب نشطون في مركز الحلقة وغير مسجلين في حلقة أخرى.");

            return Ok(await _business.GetStudentsAsync(id, centerScope));
        }
        catch (DbUpdateException)
        {
            return Conflict("تعذّر إسناد الطلاب. قد يكون أحدهم أُسند إلى حلقة أخرى للتو.");
        }
    }

    [HttpDelete("{id:int}/Students/{studentId:int}", Name = "RemoveStudentFromHalaqa")]
    public async Task<IActionResult> RemoveStudent(int id, int studentId)
    {
        if (id < 1 || studentId < 1)
            return BadRequest("Invalid halaqa or student ID.");

        if (!TryGetScope(out var centerScope, out var teacherScope))
            return Forbid();

        if (await _business.GetByIdAsync(id, centerScope, teacherScope) is null)
            return NotFound();

        var removed = await _business.RemoveStudentAsync(id, studentId, centerScope);
        return removed ? NoContent() : NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]
    public async Task<ActionResult<HalaqaDTO>> Create([FromBody] HalaqaWriteDTO input)
    {
        if (!TryGetCenterScope(out var centerScope))
            return Forbid();

        if (!IsValidInput(input, requireCenter: !centerScope.HasValue))
            return BadRequest("اسم الحلقة ومركز صالح مطلوبان، ووصف الحلقة لا يتجاوز 500 حرف.");

        if (centerScope.HasValue)
            input.CenterId = centerScope;

        var centerId = input.CenterId!.Value;
        if (!await _business.CanAssignAsync(centerId, input.TeacherId))
            return BadRequest("المركز غير نشط أو المدرس غير نشط أو لا يتبع المركز المحدد.");

        try
        {
            var created = await _business.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = created.HalaqaId }, created);
        }
        catch (DbUpdateException)
        {
            return Conflict("تعذّرت إضافة الحلقة بسبب ارتباط غير صالح.");
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]
    public async Task<ActionResult<HalaqaDTO>> Update(int id, [FromBody] HalaqaWriteDTO input)
    {
        if (id < 1)
            return BadRequest("Invalid halaqa ID.");

        if (!TryGetCenterScope(out var centerScope))
            return Forbid();

        if (!IsValidInput(input, requireCenter: false))
            return BadRequest("اسم الحلقة مطلوب، ووصف الحلقة لا يتجاوز 500 حرف.");

        var existing = await _business.GetByIdAsync(id, centerScope);
        if (existing is null)
            return NotFound();

        var centerId = centerScope ?? input.CenterId ?? existing.CenterId;
        if (centerScope.HasValue)
            centerId = centerScope.Value;

        var keepCurrentCenter = existing.CenterId == centerId;
        var keepCurrentTeacher = existing.TeacherId == input.TeacherId
            ? existing.TeacherId
            : null;

        if (!await _business.CanAssignAsync(
                centerId,
                input.TeacherId,
                keepCurrentCenter,
                keepCurrentTeacher))
            return BadRequest("المركز غير نشط أو المدرس غير نشط أو لا يتبع المركز المحدد.");

        input.CenterId = centerId;

        try
        {
            var updated = await _business.UpdateAsync(id, input, centerScope);
            if (updated is null)
                return NotFound();

            return Ok(updated);
        }
        catch (DbUpdateException)
        {
            return Conflict("تعذّر تحديث بيانات الحلقة.");
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id < 1)
            return BadRequest("Invalid halaqa ID.");

        if (!TryGetCenterScope(out var centerScope))
            return Forbid();

        try
        {
            var deleted = await _business.DeleteAsync(id, centerScope);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict("لا يمكن حذف الحلقة لارتباطها بطلاب أو سجلات حضور محفوظة.");
        }
    }

    private static bool IsValidInput(HalaqaWriteDTO? input, bool requireCenter)
    {
        return input is not null
            && !string.IsNullOrWhiteSpace(input.HalaqaName)
            && input.HalaqaName.Trim().Length <= 150
            && (string.IsNullOrWhiteSpace(input.Description) || input.Description.Trim().Length <= 500)
            && (!input.TeacherId.HasValue || input.TeacherId.Value > 0)
            && (!input.CenterId.HasValue || input.CenterId.Value > 0)
            && (!requireCenter || input.CenterId.HasValue);
    }

    /// <summary>
    /// نطاق القارئ: مسؤول المنصة يرى الكل، ومدير المركز مركزه،
    /// والمعلم حلقاته هو فقط (teacherId من التوكن).
    /// </summary>
    private bool TryGetScope(out int? centerId, out int? teacherId)
    {
        centerId = null;
        teacherId = null;

        if (User.IsInRole("PlatformAdmin") || User.IsInRole("Admin"))
            return true;

        if (!int.TryParse(User.FindFirst("centerId")?.Value, out var parsedCenterId) || parsedCenterId < 1)
            return false;

        centerId = parsedCenterId;

        if (User.IsInRole("CenterAdmin"))
            return true;

        if (User.IsInRole("Teacher")
            && int.TryParse(User.FindFirst("teacherId")?.Value, out var parsedTeacherId)
            && parsedTeacherId > 0)
        {
            teacherId = parsedTeacherId;
            return true;
        }

        return false;
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
