using Business_Layer;
using Data_Access_Layer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace QuranCenterManager_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemorizationPlanController : ControllerBase
    {
        private readonly MemorizationPlanBusinessLayer _memorizationPlanBusinessLayer;
        private readonly ILogger<MemorizationPlanController> _logger;

        public MemorizationPlanController(
            MemorizationPlanBusinessLayer memorizationPlanBusinessLayer,
            ILogger<MemorizationPlanController> logger)
        {
            _memorizationPlanBusinessLayer = memorizationPlanBusinessLayer;
            _logger = logger;
        }

        // القراءة: الإدارة والمعلم، والطالب لخططه فقط. الإنشاء والإيقاف للإدارة والمعلم.
        private const string StaffRoles = "PlatformAdmin,Admin,CenterAdmin,Teacher";
        private const string ReaderRoles = StaffRoles + ",Student";

        [HttpGet("All", Name = "GetAllMemorizationPlans")]
        [Authorize(Roles = ReaderRoles)]
        public async Task<IActionResult> GetAll()
        {
            if (!TryGetPlanScope(out var centerId, out var teacherId, out _, out var studentId))
                return Forbid();

            try
            {
                return Ok(await _memorizationPlanBusinessLayer.GetAllAsync(centerId, teacherId, studentId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load memorization plans");
                return StatusCode(500, "تعذر تحميل الخطط بسبب خطأ في الخادم.");
            }
        }

        [HttpGet("{id:int}", Name = "GetMemorizationPlanById")]
        [Authorize(Roles = ReaderRoles)]
        public async Task<IActionResult> GetById(int id)
        {
            if (id < 1)
                return BadRequest("رقم الخطة غير صالح.");

            if (!TryGetPlanScope(out var centerId, out var teacherId, out _, out var studentId))
                return Forbid();

            var plan = await _memorizationPlanBusinessLayer.GetByIdAsync(id, centerId, teacherId, studentId);
            return plan == null ? NotFound() : Ok(plan);
        }

        [HttpPost]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> Create([FromBody] MemorizationPlanWriteDTO? input)
        {
            if (!TryGetPlanScope(out var centerId, out var teacherId, out var userId, out _))
                return Forbid();

            if (!IsValidInput(input))
                return BadRequest("تحقق من الطالب ونطاق السور والهدف (صفحات الجلسة من 1 إلى 604) وأيام الخطة.");

            try
            {
                if (await _memorizationPlanBusinessLayer.HasOverlappingActivePlanAsync(input!))
                    return Conflict("لدى الطالب خطة نشطة من نفس النوع يتقاطع نطاقها مع هذه الخطة. أوقف الخطة القديمة أو غيّر النطاق.");

                var plan = await _memorizationPlanBusinessLayer.CreateAsync(
                    input!,
                    centerId,
                    teacherId,
                    userId);

                if (plan == null)
                    return BadRequest("الطالب غير نشط أو لا يتبع نطاق حسابك.");

                return CreatedAtAction(nameof(GetById), new { id = plan.MemorizationPlanId }, plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create memorization plan");
                return StatusCode(500, "تعذر إنشاء الخطة بسبب خطأ في الخادم.");
            }
        }

        [HttpPut("{id:int}/status", Name = "UpdateMemorizationPlanStatus")]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] MemorizationPlanStatusDTO? input)
        {
            if (id < 1 || input == null)
                return BadRequest("بيانات حالة الخطة غير صالحة.");

            if (!TryGetPlanScope(out var centerId, out var teacherId, out _, out _))
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update memorization plan status");
                return StatusCode(500, "تعذر تحديث الخطة بسبب خطأ في الخادم.");
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
                && input.PagesPerSession is >= 1 and <= 604
                && input.TargetRounds is >= 1 and <= 1000
                && input.StartDate != default
                && input.DaysOfWeek != null
                && input.DaysOfWeek.Length is >= 1 and <= 7
                && input.DaysOfWeek.All(day => day >= 0 && day <= 6)
                && input.DaysOfWeek.Distinct().Count() == input.DaysOfWeek.Length;
        }

        private bool TryGetPlanScope(
            out int? centerId,
            out int? teacherId,
            out int userId,
            out int? studentId)
        {
            centerId = null;
            teacherId = null;
            studentId = null;
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

            if (User.IsInRole("Student"))
            {
                if (!int.TryParse(User.FindFirst("studentId")?.Value, out var parsedStudentId)
                    || parsedStudentId < 1)
                    return false;

                studentId = parsedStudentId;
                return true;
            }

            return false;
        }
    }
}
