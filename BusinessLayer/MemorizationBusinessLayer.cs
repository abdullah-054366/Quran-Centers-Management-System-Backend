using Data_Access_Layer;
using Data_Access_Layer.DTOs;

namespace Business_Layer
{
    public class MemorizationBusinessLayer
    {
        private readonly MemorizationData _memorizationData;

        public MemorizationBusinessLayer(MemorizationData memorizationData)
        {
            _memorizationData = memorizationData;
        }

        public async Task<List<MemorizationStudentDTO>> GetStudentsForTeacher(int teacherId)
        {
            return await _memorizationData.GetStudentsForTeacher(teacherId);
        }

        public async Task<List<MemorizationRecordDTO>> GetMemorizationRecords(
            int? centerId = null,
            int? teacherId = null,
            int? studentId = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            return await _memorizationData.GetMemorizationRecords(centerId, teacherId, studentId, fromDate, toDate);
        }

        public async Task<MemorizationSaveResult> SaveMemorizationRecords(
            List<MemorizationRecordWriteDTO> records,
            int teacherId,
            int userId)
        {
            return await _memorizationData.SaveMemorizationRecords(records, teacherId, userId);
        }

        public async Task<MemorizationDailyUsageDTO?> GetDailyUsage(int teacherId, int studentId, DateOnly date)
        {
            return await _memorizationData.GetDailyUsage(teacherId, studentId, date);
        }
    }
}
