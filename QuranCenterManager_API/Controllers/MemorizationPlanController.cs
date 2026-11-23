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

        // الإدارة والمعلم ضمن نطاقهم، والطالب لخططه هو فقط (قراءة وإنشاء وإيقاف).
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
        [Authorize(Roles = ReaderRoles)]
        public async Task<IActionResult> Create([FromBody] MemorizationPlanWriteDTO? input)
        {
            if (!TryGetPlanScope(out var centerId, out var teacherId, out var userId, out var ownStudentId))
                return Forbid();

            // الطالب ينشئ لنفسه فقط، مهما أُرسل في الطلب.
            if (ownStudentId.HasValue && input != null)
                input.StudentId = ownStudentId.Value;

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
        [Authorize(Roles = ReaderRoles)]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] MemorizationPlanStatusDTO? input)
        {
            if (id < 1 || input == null)
                return BadRequest("بيانات حالة الخطة غير صالحة.");

            if (!TryGetPlanScope(out var centerId, out var teacherId, out _, out var studentId))
                return Forbid();

            try
            {
                var plan = await _memorizationPlanBusinessLayer.UpdateStatusAsync(
                    id,
                    input.IsActive,
                    centerId,
                    teacherId,
                    studentId);

                return plan == null ? NotFound() : Ok(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update memorization plan status");
                return StatusCode(500, "تعذر تحديث الخطة بسبب خطأ في الخادم.");
            }
        }

        [HttpPut("{id:int}", Name = "UpdateMemorizationPlan")]
        [Authorize(Roles = ReaderRoles)]
        public async Task<IActionResult> Update(int id, [FromBody] MemorizationPlanWriteDTO? input)
        {
            if (id < 1)
                return BadRequest("رقم الخطة غير صالح.");

            if (!TryGetPlanScope(out var centerId, out var teacherId, out _, out var ownStudentId))
                return Forbid();

            var current = await _memorizationPlanBusinessLayer.GetByIdAsync(id, centerId, teacherId, ownStudentId);

            if (current == null)
                return NotFound();

            // الطالب والنوع يُؤخذان من الخطة نفسها لا من الطلب: تغييرهما يجعلها خطة أخرى.
            if (input != null)
            {
                input.StudentId = current.StudentId;
                input.Type = current.Type;
            }

            if (!IsValidInput(input))
                return BadRequest("تحقق من نطاق السور والهدف (صفحات الجلسة من 1 إلى 604) وأيام الخطة ومدّتها.");

            try
            {
                if (await _memorizationPlanBusinessLayer.HasOverlappingActivePlanAsync(input!, id))
                    return Conflict("لدى الطالب خطة نشطة أخرى من نفس النوع يتقاطع نطاقها مع هذا النطاق.");

                var plan = await _memorizationPlanBusinessLayer.UpdateAsync(
                    id,
                    input!,
                    centerId,
                    teacherId,
                    ownStudentId);

                return plan == null ? NotFound() : Ok(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update memorization plan");
                return StatusCode(500, "تعذر تعديل الخطة بسبب خطأ في الخادم.");
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
                && (input.EndDate is null || input.EndDate >= input.StartDate)
                && input.DaysOfWeek != null
                && input.DaysOfWeek.Length is >= 1 and <= 7
                && input.DaysOfWeek.All(day => day >= 0 && day <= 6)
                && input.DaysOfWeek.Distinct().Count() == input.DaysOfWeek.Length
                && HasPlanDayInSpan(input);
        }

        // خطة محدّدة المدّة بلا يوم واحد من أيامها داخل المدّة = خطة بلا جلسات.
        // أيام الأسبوع تتكرر كل سبعة أيام، ففحص أول سبعة أيام من المدّة يكفي ويمنع
        // حلقة طويلة لو جاء تاريخ نهاية بعيد.
        private static bool HasPlanDayInSpan(MemorizationPlanWriteDTO input)
        {
            if (input.EndDate is null)
                return true;

            var days = input.DaysOfWeek.ToHashSet();
            var limit = input.StartDate.AddDays(6);

            for (var day = input.StartDate; day <= input.EndDate.Value && day <= limit; day = day.AddDays(1))
            {
                if (days.Contains((int)day.DayOfWeek))
                    return true;
            }

            return false;
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
