using Business_Layer;
using Microsoft.AspNetCore.Authorization;
using Data_Access_Layer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace QuranCenterManager_API.Controllers
{


    [ApiController]
    [Route("api/Centers")]
    [Authorize]
    public class CenterController : Controller
    {


       
        private readonly CenterBusinessLayer _business;
        private readonly AuditBusinessLayer _audit;

        public CenterController(CenterBusinessLayer business, AuditBusinessLayer audit)
        {
            _business = business;
            _audit = audit;
        }

        [HttpGet("All", Name = "GetAllCenters")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult> GetAllCenters()
        {
            var CentersList = await _business.GetAllCenters();
            if (CentersList == null)
            {
                return BadRequest();
            }
            return Ok(CentersList);
        }


        [HttpGet("{id}", Name = "GetCenterById")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]

        public async Task<ActionResult> GetCenterById(int id)
        {
            if (id < 1)
                return BadRequest("Invalid center ID.");

            var Center = await _business.GetCenterById(id);
            if (Center == null)
            {
                return NotFound();
            }

            if (User.IsInRole("CenterAdmin")
                && (!int.TryParse(User.FindFirst("centerId")?.Value, out var centerId)
                    || Center.CenterId != centerId))
            {
                return NotFound();
            }

            return Ok(Center);
        }


        [HttpPost]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult<CenterDTO>> CreateCenter([FromBody] CenterWriteDTO input)
        {
            if (input is null
                || string.IsNullOrWhiteSpace(input.CenterName)
                || input.CenterName.Trim().Length > 150
                || string.IsNullOrWhiteSpace(input.CenterCode))
            {
                return BadRequest("اسم المركز وكود انضمام من 4 إلى 10 أحرف وأرقام مطلوبة.");
            }

            var centerCode = input.CenterCode.Trim().ToUpperInvariant();
            if (centerCode.Length < 4 || centerCode.Length > 10
                || centerCode.Any(character => !char.IsAsciiLetterOrDigit(character)))
            {
                return BadRequest("كود المركز يجب أن يتكون من 4 إلى 10 أحرف إنجليزية أو أرقام.");
            }

            if (await _business.CenterCodeExists(centerCode))
                return Conflict("كود المركز مستخدم مسبقًا.");

            input.CenterCode = centerCode;

            try
            {
                var center = await _business.AddCenter(input);
                await this.LogAuditAsync(_audit, "center.create", "Center", center.CenterId, center.CenterName,
                    $"الكود: {center.CenterCode}");
                return CreatedAtAction(nameof(GetCenterById), new { id = center.CenterId }, center);
            }
            catch (DbUpdateException)
            {
                return Conflict("تعذّرت إضافة المركز. تحقق من أن كود الانضمام غير مستخدم.");
            }
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult<CenterDTO>> UpdateCenterStatus(
            int id,
            [FromBody] CenterStatusDTO input)
        {
            if (id < 1)
                return BadRequest("Invalid center ID.");

            var center = await _business.UpdateCenterStatus(id, input.IsActive);
            if (center is null)
                return NotFound();

            await this.LogAuditAsync(_audit, input.IsActive ? "center.activate" : "center.suspend",
                "Center", center.CenterId, center.CenterName);
            return Ok(center);
        }

        [HttpPut("{id:int}/code")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult<CenterDTO>> UpdateCenterCodeEnabled(
            int id,
            [FromBody] CenterCodeEnabledDTO input)
        {
            if (id < 1)
                return BadRequest("Invalid center ID.");

            var center = await _business.UpdateCenterCodeEnabled(id, input.CodeEnabled);
            if (center is null)
                return NotFound();

            await this.LogAuditAsync(_audit, input.CodeEnabled ? "center.code_enable" : "center.code_disable",
                "Center", center.CenterId, center.CenterName);
            return Ok(center);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult<CenterDTO>> UpdateCenter(int id, [FromBody] CenterNameDTO input)
        {
            if (id < 1)
                return BadRequest("Invalid center ID.");

            if (input is null
                || string.IsNullOrWhiteSpace(input.CenterName)
                || input.CenterName.Trim().Length > 150)
            {
                return BadRequest("اسم المركز مطلوب ولا يتجاوز 150 حرفاً.");
            }

            var before = await _business.GetCenterById(id);
            if (before is null)
                return NotFound();

            var center = await _business.UpdateCenterName(id, input.CenterName);
            if (center is null)
                return NotFound();

            await this.LogAuditAsync(_audit, "center.rename", "Center", center.CenterId, center.CenterName,
                $"الاسم السابق: {before.CenterName}");
            return Ok(center);
        }

        [HttpPost("{id:int}/regenerate-code")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult<CenterDTO>> RegenerateCode(int id)
        {
            if (id < 1)
                return BadRequest("Invalid center ID.");

            try
            {
                var before = await _business.GetCenterById(id);
                if (before is null)
                    return NotFound();

                var center = await _business.RegenerateCenterCode(id);
                if (center is null)
                    return NotFound();

                await this.LogAuditAsync(_audit, "center.code_regenerate", "Center", center.CenterId,
                    center.CenterName, $"الكود السابق: {before.CenterCode}");
                return Ok(center);
            }
            catch (DbUpdateException)
            {
                return Conflict("تعذّر توليد كود جديد. حاول مرة أخرى.");
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("{id:int}/deletion-summary")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult<CenterDeletionSummaryDTO>> GetDeletionSummary(int id)
        {
            if (id < 1)
                return BadRequest("Invalid center ID.");

            var summary = await _business.GetDeletionSummary(id);
            return summary is null ? NotFound() : Ok(summary);
        }

        [HttpGet("{id:int}/overview")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult<CenterOverviewDTO>> GetCenterOverview(int id)
        {
            if (id < 1)
                return BadRequest("Invalid center ID.");

            var overview = await _business.GetCenterOverview(id);
            return overview is null ? NotFound() : Ok(overview);
        }

        [HttpGet("Overview")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult<PlatformOverviewDTO>> GetPlatformOverview()
        {
            return Ok(await _business.GetPlatformOverview());
        }

        // الحذف النهائي لا يسمح به إلا لمركز فارغ تماماً؛ غير ذلك يُوقَف المركز (أرشفة).
        [HttpDelete("{id:int}", Name = "DeleteCenter")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult> DeleteCenter(int id)
        {
            if (id < 1)
                return BadRequest("Invalid center ID.");

            var summary = await _business.GetDeletionSummary(id);
            if (summary is null)
                return NotFound();

            if (!summary.CanDelete)
            {
                return Conflict("لا يمكن حذف مركز يحتوي بيانات (طلاب أو مدرسين أو حلقات أو سجلات). أوقف المركز بدلاً من حذفه.");
            }

            try
            {
                if (!await _business.DeleteCenter(id))
                    return NotFound();
            }
            catch (DbUpdateException)
            {
                return Conflict("لا يمكن حذف المركز لارتباطه ببيانات أخرى. أوقفه بدلاً من حذفه.");
            }

            await this.LogAuditAsync(_audit, "center.delete", "Center", id, summary.CenterName);
            return Ok();
        }

        [HttpGet("Count", Name = "GetCenterCount")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult> getCenterCount()
        {
            var count = await _business.getCenterCount();
            return Ok(count);
        }



        [HttpGet("ActiveCount", Name = "GetActiveCentersCount")]
        [Authorize(Roles = "PlatformAdmin,Admin")]

        public async Task<ActionResult> getActiveCentersCount()
        {
            var count = await _business.getActiveCentersCount();
            return Ok(count);
        }


    }
}
