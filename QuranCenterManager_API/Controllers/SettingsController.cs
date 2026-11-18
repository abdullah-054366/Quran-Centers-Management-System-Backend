using System.Security.Claims;
using Business_Layer;
using Data_Access_Layer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace QuranCenterManager_API.Controllers
{
    [ApiController]
    [Route("api/Settings")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private const string CenterRoles = "PlatformAdmin,Admin,CenterAdmin";

        private readonly SettingsBusinessLayer _business;
        private readonly AuditBusinessLayer _audit;
        private readonly IWebHostEnvironment _env;

        public SettingsController(SettingsBusinessLayer business, AuditBusinessLayer audit, IWebHostEnvironment env)
        {
            _business = business;
            _audit = audit;
            _env = env;
        }

        /// <summary>مسؤول المركز: مركزه فقط. مدير النظام: المركز المحدد في centerId.</summary>
        private bool TryResolveCenter(int? requestedCenterId, out int centerId)
        {
            centerId = 0;
            if (User.IsInRole("CenterAdmin"))
                return int.TryParse(User.FindFirstValue("centerId"), out centerId) && centerId > 0;

            if (requestedCenterId is > 0)
            {
                centerId = requestedCenterId.Value;
                return true;
            }
            return false;
        }

        [HttpGet("center")]
        [Authorize(Roles = CenterRoles)]
        public async Task<ActionResult<CenterSettingsDTO>> GetCenter([FromQuery] int? centerId)
        {
            if (!TryResolveCenter(centerId, out var id)) return BadRequest("حدد المركز.");
            var settings = await _business.GetCenterSettings(id);
            return settings is null ? NotFound() : Ok(settings);
        }

        [HttpPut("center")]
        [Authorize(Roles = CenterRoles)]
        public async Task<ActionResult<CenterSettingsDTO>> UpdateCenter([FromQuery] int? centerId, [FromBody] CenterSettingsUpdateDTO? input)
        {
            if (!TryResolveCenter(centerId, out var id)) return BadRequest("حدد المركز.");

            var error = SettingsBusinessLayer.ValidateCenterSettings(input);
            if (error is not null) return BadRequest(error);

            var settings = await _business.UpdateCenterSettings(id, input!);
            if (settings is null) return NotFound();

            await this.LogAuditAsync(_audit, "center.update_settings", "Center", id, settings.CenterName);
            return Ok(settings);
        }

        [HttpPut("center/self-registration")]
        [Authorize(Roles = CenterRoles)]
        public async Task<IActionResult> SetSelfRegistration([FromQuery] int? centerId, [FromBody] CenterCodeEnabledDTO? input)
        {
            if (input is null) return BadRequest("البيانات مطلوبة.");
            if (!TryResolveCenter(centerId, out var id)) return BadRequest("حدد المركز.");
            if (!await _business.SetSelfRegistration(id, input.CodeEnabled)) return NotFound();

            await this.LogAuditAsync(_audit, input.CodeEnabled ? "center.code_enable" : "center.code_disable", "Center", id, null);
            return Ok(new { codeEnabled = input.CodeEnabled });
        }

        [HttpPost("center/regenerate-code")]
        [Authorize(Roles = CenterRoles)]
        public async Task<IActionResult> RegenerateCode([FromQuery] int? centerId)
        {
            if (!TryResolveCenter(centerId, out var id)) return BadRequest("حدد المركز.");

            try
            {
                var before = await _business.GetCenterSettings(id);
                if (before is null) return NotFound();

                var center = await _business.RegenerateCode(id);
                if (center is null) return NotFound();

                await this.LogAuditAsync(_audit, "center.code_regenerate", "Center", id, center.CenterName,
                    $"الكود السابق: {before.CenterCode}");
                return Ok(new { centerCode = center.CenterCode });
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

        [HttpGet("system")]
        [Authorize(Roles = "PlatformAdmin,Admin")]
        public async Task<ActionResult<SystemStatusDTO>> GetSystemStatus()
        {
            return Ok(await _business.GetSystemStatus(_env.EnvironmentName));
        }
    }
}
