using Business_Layer;
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

        public MemorizationController(MemorizationBusinessLayer memorizationBusinessLayer)
        {
            _memorizationBusinessLayer = memorizationBusinessLayer;
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
        public async Task<IActionResult> GetAllRecords()
        {
            if (!TryGetMemorizationScope(out var centerId, out var teacherId, out var studentId))
                return Forbid();

            try
            {
                return Ok(await _memorizationBusinessLayer.GetMemorizationRecords(
                    centerId,
                    teacherId,
                    studentId));
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving memorization records.");
            }
        }

        [HttpPost("Bulk", Name = "SaveMemorizationRecords")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> SaveBulk([FromBody] List<MemorizationRecordWriteDTO>? records)
        {
            if (!TryGetTeacherScope(out var teacherId, out var userId))
                return Forbid();

            if (records == null || records.Count == 0)
                return BadRequest("At least one memorization record is required.");

            if (records.Any(record => record.StudentId <= 0
                || record.Type < 0
                || record.Type > 1
                || record.SurahNumber < 1
                || record.SurahNumber > 114
                || record.FromAyah < 1
                || record.ToAyah < record.FromAyah
                || record.RecordDate == default
                || (record.Notes?.Length ?? 0) > 1000))
            {
                return BadRequest("Memorization data contains an invalid field.");
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
                return BadRequest("A memorization record is duplicated in this session.");

            try
            {
                var saved = await _memorizationBusinessLayer.SaveMemorizationRecords(
                    records,
                    teacherId,
                    userId);

                if (!saved)
                    return BadRequest("الطالب لا يتبع حلقة نشطة مرتبطة بحسابك.");

                return Ok(new { savedCount = records.Count });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while saving memorization records.");
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
