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
    public class AttendanceController : ControllerBase
    {
        private readonly AttendanceBusinessLayer _attendanceBusinessLayer;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            AttendanceBusinessLayer attendanceBusinessLayer,
            ILogger<AttendanceController> logger)
        {
            _attendanceBusinessLayer = attendanceBusinessLayer;
            _logger = logger;
        }

        [HttpGet("All", Name = "GetAllAttendances")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin,Teacher,Student")]
        public async Task<IActionResult> GetAllAttendances()
        {
            if (!TryGetAttendanceScope(out var centerId, out var teacherId, out var studentId, out _))
                return Forbid();

            try
            {
                var attendances = await _attendanceBusinessLayer.GetAllAttendancesAsync(
                    centerId,
                    teacherId,
                    studentId);
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attendance request failed: {Path}", Request.Path);
                return StatusCode(500, "An error occurred while retrieving attendances.");
            }
        }

        [HttpGet("Halaqas", Name = "GetAttendanceHalaqas")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin,Teacher")]
        public async Task<IActionResult> GetHalaqas()
        {
            if (!TryGetAttendanceScope(out var centerId, out var teacherId, out var studentId, out _))
                return Forbid();

            try
            {
                return Ok(await _attendanceBusinessLayer.GetHalaqasAsync(
                    centerId,
                    teacherId,
                    studentId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attendance request failed: {Path}", Request.Path);
                return StatusCode(500, "An error occurred while retrieving halaqas.");
            }
        }

        [HttpGet("Halaqas/{halaqaId:int}/Students", Name = "GetAttendanceStudents")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin,Teacher")]
        public async Task<IActionResult> GetStudentsByHalaqa(int halaqaId)
        {
            if (halaqaId <= 0) return BadRequest("Invalid halaqa id.");
            if (!TryGetAttendanceScope(out var centerId, out var teacherId, out _, out _))
                return Forbid();

            try
            {
                return Ok(await _attendanceBusinessLayer.GetStudentsByHalaqaAsync(
                    halaqaId,
                    centerId,
                    teacherId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attendance request failed: {Path}", Request.Path);
                return StatusCode(500, "An error occurred while retrieving halaqa students.");
            }
        }

        [HttpPost("Bulk", Name = "SaveAttendanceBulk")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin,Teacher")]
        public async Task<IActionResult> SaveAttendanceBulk([FromBody] List<AttendanceDTO>? attendances)
        {
            if (!TryGetAttendanceScope(out var centerId, out var teacherId, out _, out var userId))
                return Forbid();

            if (attendances == null || attendances.Count == 0)
                return BadRequest("At least one attendance record is required.");

            if (centerId.HasValue)
            {
                foreach (var attendance in attendances)
                    attendance.CenterId = centerId.Value;
            }

            if (attendances.Any(item => item.CenterId <= 0
                || item.HalaqaId <= 0
                || item.StudentId <= 0
                || item.AttendanceDate == default
                || item.Status < 1
                || item.Status > 4))
            {
                return BadRequest("Attendance data contains an invalid field.");
            }

            if (attendances.Any(item => item.AttendanceDate > MemorizationRules.TodayInRiyadh()))
                return BadRequest("لا يمكن تسجيل الحضور بتاريخ مستقبلي.");

            var hasDuplicateStudents = attendances
                .GroupBy(item => new { item.HalaqaId, item.StudentId, item.AttendanceDate })
                .Any(group => group.Count() > 1);
            if (hasDuplicateStudents)
                return BadRequest("A student can only have one record per halaqa and date.");

            try
            {
                var saved = await _attendanceBusinessLayer.SaveAttendancesAsync(
                    attendances,
                    centerId,
                    teacherId,
                    userId);

                if (!saved)
                    return BadRequest("إحدى الحلقات أو عضويات الطلاب لا تتبع نطاق حسابك.");

                return Ok(new { savedCount = attendances.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attendance request failed: {Path}", Request.Path);
                return StatusCode(500, "An error occurred while saving attendance.");
            }
        }

        private bool TryGetAttendanceScope(
            out int? centerId,
            out int? teacherId,
            out int? studentId,
            out int? userId)
        {
            centerId = null;
            teacherId = null;
            studentId = null;
            userId = null;

            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedUserId)
                || parsedUserId < 1)
            {
                return false;
            }

            userId = parsedUserId;

            if (User.IsInRole("PlatformAdmin") || User.IsInRole("Admin"))
                return true;

            if (User.IsInRole("CenterAdmin"))
            {
                if (!int.TryParse(User.FindFirst("centerId")?.Value, out var parsedCenterId)
                    || parsedCenterId < 1)
                {
                    return false;
                }

                centerId = parsedCenterId;
                return true;
            }

            if (User.IsInRole("Teacher"))
            {
                if (!int.TryParse(User.FindFirst("teacherId")?.Value, out var parsedTeacherId)
                    || parsedTeacherId < 1)
                {
                    return false;
                }

                teacherId = parsedTeacherId;
                return true;
            }

            if (User.IsInRole("Student"))
            {
                if (!int.TryParse(User.FindFirst("studentId")?.Value, out var parsedStudentId)
                    || parsedStudentId < 1)
                {
                    return false;
                }

                studentId = parsedStudentId;
                return true;
            }

            return false;
        }
    }
}
