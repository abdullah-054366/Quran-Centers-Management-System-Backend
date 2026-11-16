using Business_Layer;
using Data_Access_Layer;
using Data_Access_Layer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace QuranCenterManager_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemorizationController : ControllerBase
    {
        private readonly MemorizationBusinessLayer _memorizationBusinessLayer;
        private readonly ILogger<MemorizationController> _logger;

        public MemorizationController(
            MemorizationBusinessLayer memorizationBusinessLayer,
            ILogger<MemorizationController> logger)
        {
            _memorizationBusinessLayer = memorizationBusinessLayer;
            _logger = logger;
        }

        [HttpGet("Students", Name = "GetMemorizationStudents")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetStudents()
        {
            if (!TryGetTeacherScope(out var teacherId, out _))
                return Forbid();

            try
            {
                return Ok(await _memorizationBusinessLayer.GetStudentsForTeacher(teacherId));
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving memorization students.");
            }
        }

        [HttpGet("All", Name = "GetAllMemorizationRecords")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin,Teacher,Student")]
        public async Task<IActionResult> GetAllRecords(
            [FromQuery] int? studentId,
            [FromQuery(Name = "from")] DateOnly? fromDate,
            [FromQuery(Name = "to")] DateOnly? toDate)
        {
            if (!TryGetMemorizationScope(out var centerId, out var teacherId, out var scopedStudentId))
                return Forbid();

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
                return BadRequest("تاريخ البداية بعد تاريخ النهاية.");

            // الطالب لا يرى إلا سجلاته، ولا يُسمح له بتغيير ذلك عبر الاستعلام.
            var effectiveStudentId = scopedStudentId ?? studentId;
            try
            {
                return Ok(await _memorizationBusinessLayer.GetMemorizationRecords(
                    centerId,
                    teacherId,
                    effectiveStudentId,
                    fromDate,
                    toDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve memorization records");
                return StatusCode(500, "تعذر تحميل سجلات الحفظ والمراجعة.");
            }
        }

        [HttpGet("DailyUsage", Name = "GetMemorizationDailyUsage")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetDailyUsage([FromQuery] int studentId, [FromQuery] DateOnly? date)
        {
            if (!TryGetTeacherScope(out var teacherId, out _))
                return Forbid();

            if (studentId <= 0)
                return BadRequest("معرّف الطالب غير صالح.");

            try
            {
                var usage = await _memorizationBusinessLayer.GetDailyUsage(
                    teacherId,
                    studentId,
                    date ?? MemorizationRules.TodayInRiyadh());

                if (usage == null)
                    return NotFound("الطالب لا يتبع حلقة نشطة مرتبطة بحسابك.");

                return Ok(usage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read memorization daily usage for student {StudentId}", studentId);
                return StatusCode(500, "تعذر قراءة سجلات اليوم، حاول مرة أخرى.");
            }
        }

        [HttpPost("Bulk", Name = "SaveMemorizationRecords")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> SaveBulk([FromBody] List<MemorizationRecordWriteDTO>? records)
        {
            if (!TryGetTeacherScope(out var teacherId, out var userId))
                return Forbid();

            if (records == null || records.Count == 0)
                return BadRequest("أضف سجلاً واحداً على الأقل.");

            var today = MemorizationRules.TodayInRiyadh();

            foreach (var record in records)
            {
                if (record.StudentId <= 0)
                    return BadRequest("الطالب غير محدد.");

                if (record.Type != MemorizationRules.TypeMemorization && record.Type != MemorizationRules.TypeReview)
                    return BadRequest("نوع السجل غير صالح.");

                if (record.SurahNumber < 1 || record.SurahNumber > 114)
                    return BadRequest("رقم السورة غير صالح.");

                if (record.FromAyah < 1 || record.ToAyah < record.FromAyah)
                    return BadRequest("نطاق الآيات غير صالح: يجب أن تكون آية النهاية بعد آية البداية أو مثلها.");

                if (record.ToAyah > MemorizationRules.GetAyahCount(record.SurahNumber))
                    return BadRequest($"رقم الآية خارج عدد آيات السورة ({MemorizationRules.GetAyahCount(record.SurahNumber)} آية).");

                if (record.RecordDate == default)
                    return BadRequest("التاريخ مطلوب.");

                if (record.RecordDate > today)
                    return BadRequest("لا يمكن تسجيل حفظ أو مراجعة بتاريخ مستقبلي.");

                if ((record.Notes?.Length ?? 0) > 1000)
                    return BadRequest("الملاحظات طويلة جداً (الحد 1000 حرف).");
            }

            var hasDuplicates = records
                .GroupBy(record => new
                {
                    record.StudentId,
                    record.RecordDate,
                    record.Type,
                    record.SurahNumber,
                    record.FromAyah,
                    record.ToAyah
                })
                .Any(group => group.Count() > 1);

            if (hasDuplicates)
                return BadRequest("يوجد سجل مكرر داخل الجلسة نفسها.");

            // جلسة واحدة لكل (طالب، تاريخ، نوع). الجلسة التي تضم سوراً متعددة تُحسب سجلاً واحداً في الحد اليومي.
            var generatedSessions = new Dictionary<(int, DateOnly, short), Guid>();
            var sessionOwners = new Dictionary<Guid, (int, DateOnly, short)>();

            foreach (var record in records)
            {
                var key = (record.StudentId, record.RecordDate, record.Type);

                if (!record.SessionId.HasValue || record.SessionId.Value == Guid.Empty)
                {
                    if (!generatedSessions.TryGetValue(key, out var generated))
                    {
                        generated = Guid.NewGuid();
                        generatedSessions[key] = generated;
                    }

                    record.SessionId = generated;
                }

                if (sessionOwners.TryGetValue(record.SessionId.Value, out var owner))
                {
                    if (owner != key)
                        return BadRequest("معرّف الجلسة مستخدم لطالب أو نوع أو تاريخ مختلف.");
                }
                else
                {
                    sessionOwners[record.SessionId.Value] = key;
                }
            }

            try
            {
                var result = await _memorizationBusinessLayer.SaveMemorizationRecords(
                    records,
                    teacherId,
                    userId);

                return result.Status switch
                {
                    MemorizationSaveStatus.Saved => Ok(new { savedCount = result.SavedCount }),
                    MemorizationSaveStatus.LimitReached => Conflict(result.Message),
                    MemorizationSaveStatus.DuplicateSession => Conflict(result.Message),
                    _ => BadRequest(result.Message)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save memorization records. TeacherId={TeacherId}", teacherId);
                return StatusCode(500, "تعذر حفظ السجل بسبب خطأ في الخادم. حاول مرة أخرى، وإن تكرر الخطأ راجع المسؤول.");
            }
        }

        private bool TryGetTeacherScope(out int teacherId, out int userId)
        {
            teacherId = 0;
            userId = 0;

            return int.TryParse(User.FindFirst("teacherId")?.Value, out teacherId)
                && teacherId > 0
                && int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId)
                && userId > 0;
        }

        private bool TryGetMemorizationScope(
            out int? centerId,
            out int? teacherId,
            out int? studentId)
        {
            centerId = null;
            teacherId = null;
            studentId = null;

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
