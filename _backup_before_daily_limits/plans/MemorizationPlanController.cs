using Business_Layer;
using Data_Access_Layer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace QuranCenterManager_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin,Teacher")]
    public class MemorizationPlanController : ControllerBase
    {
        private readonly MemorizationPlanBusinessLayer _memorizationPlanBusinessLayer;

        public MemorizationPlanController(MemorizationPlanBusinessLayer memorizationPlanBusinessLayer)
        {
            _memorizationPlanBusinessLayer = memorizationPlanBusinessLayer;
        }

        [HttpGet("All", Name = "GetAllMemorizationPlans")]
        public async Task<IActionResult> GetAll()
        {
            if (!TryGetPlanScope(out var centerId, out var teacherId, out _))
                return Forbid();

            return Ok(await _memorizationPlanBusinessLayer.GetAllAsync(centerId, teacherId));
        }

        [HttpGet("{id:int}", Name = "GetMemorizationPlanById")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id < 1)
                return BadRequest("Invalid plan ID.");

            if (!TryGetPlanScope(out var centerId, out var teacherId, out _))
                return Forbid();

            var plan = await _memorizationPlanBusinessLayer.GetByIdAsync(id, centerId, teacherId);
            return plan == null ? NotFound() : Ok(plan);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MemorizationPlanWriteDTO? input)
        {
            if (!TryGetPlanScope(out var centerId, out var teacherId, out var userId))
                return Forbid();

            if (!IsValidInput(input))
                return BadRequest("تحقق من الطالب ونطاق السور والهدف وأيام الخطة.");

            try
            {
                var plan = await _memorizationPlanBusinessLayer.CreateAsync(
                    input!,
                    centerId,
                    teacherId,
                    userId);

                if (plan == null)
                    return BadRequest("الطالب غير نشط أو لا يتبع نطاق حسابك.");

                return CreatedAtAction(nameof(GetById), new { id = plan.MemorizationPlanId }, plan);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while creating the memorization plan.");
            }
        }

        [HttpPut("{id:int}/status", Name = "UpdateMemorizationPlanStatus")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] MemorizationPlanStatusDTO? input)
        {
            if (id < 1 || input == null)
                return BadRequest("Invalid plan status.");

            if (!TryGetPlanScope(out var centerId, out var teacherId, out _))
                return Forbid();

            try
            {
                var plan = await _memorizationPlanBusinessLayer.UpdateStatusAsync(
                    id,
                    input.IsActive,
                    centerId,
                    teacherId);

                return plan == null ? NotFound() : Ok(plan);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while updating the memorization plan.");
            }
        }

        private static bool IsValidInput(MemorizationPlanWriteDTO? input)
        {
            return input != null
                && input.StudentId > 0
                && input.Type >= 0
                && input.Type <= 1
                && input.FromSurah >= 1
                && input.FromSurah <= input.ToSurah
                && input.ToSurah <= 114
                && input.PagesPerSession > 0
                && input.TargetRounds > 0
                && input.StartDate != default
                && input.DaysOfWeek != null
                && input.DaysOfWeek.Length is >= 1 and <= 7
                && input.DaysOfWeek.All(day => day >= 0 && day <= 6)
                && input.DaysOfWeek.Distinct().Count() == input.DaysOfWeek.Length;
        }

        private bool TryGetPlanScope(
            out int? centerId,
            out int? teacherId,
            out int userId)
        {
            centerId = null;
            teacherId = null;
            userId = 0;

            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId)
                || userId < 1)
            {
                return false;
            }

            if (User.IsInRole("PlatformAdmin") || User.IsInRole("Admin"))
                return true;

            if (User.IsInRole("CenterAdmin"))
            {
                if (!int.TryParse(User.FindFirst("centerId")?.Value, out var parsedCenterId)
                    || parsedCenterId < 1)
                    return false;

                centerId = parsedCenterId;
                return true;
            }

            if (User.IsInRole("Teacher"))
            {
                if (!int.TryParse(User.FindFirst("teacherId")?.Value, out var parsedTeacherId)
                    || parsedTeacherId < 1)
                    return false;

                teacherId = parsedTeacherId;
                return true;
            }

            return false;
        }
    }
}
