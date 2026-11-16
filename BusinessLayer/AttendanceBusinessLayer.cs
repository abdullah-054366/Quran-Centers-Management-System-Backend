using Data_Access_Layer;
using Data_Access_Layer.DTOs;

namespace Business_Layer
{
    public class AttendanceBusinessLayer
    {
        private readonly AttendanceData _attendanceData;

        public AttendanceBusinessLayer(AttendanceData attendanceData)
        {
            _attendanceData = attendanceData;
        }

        public async Task<List<AttendanceDTO>> GetAllAttendancesAsync(
            int? centerId = null,
            int? teacherId = null,
            int? studentId = null,
            int? halaqaId = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            return await _attendanceData.GetAllAttendancesAsync(centerId, teacherId, studentId, halaqaId, fromDate, toDate);
        }

        public async Task<List<AttendanceHalaqaDTO>> GetHalaqasAsync(
            int? centerId = null,
            int? teacherId = null,
            int? studentId = null)
        {
            return await _attendanceData.GetHalaqasAsync(centerId, teacherId, studentId);
        }

        public async Task<List<AttendanceStudentDTO>> GetStudentsByHalaqaAsync(
            int halaqaId,
            int? centerId = null,
            int? teacherId = null)
        {
            return await _attendanceData.GetStudentsByHalaqaAsync(halaqaId, centerId, teacherId);
        }

        public async Task<bool> SaveAttendancesAsync(
            List<AttendanceDTO> attendances,
            int? centerScope = null,
            int? teacherId = null,
            int? recorderUserId = null)
        {
            return await _attendanceData.SaveAttendancesAsync(
                attendances,
                centerScope,
                teacherId,
                recorderUserId);
        }
    }
}
