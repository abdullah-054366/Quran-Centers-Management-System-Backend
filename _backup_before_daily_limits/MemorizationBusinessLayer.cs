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
            int? studentId = null)
        {
            return await _memorizationData.GetMemorizationRecords(centerId, teacherId, studentId);
        }

        public async Task<bool> SaveMemorizationRecords(
            List<MemorizationRecordWriteDTO> records,
            int teacherId,
            int userId)
        {
            return await _memorizationData.SaveMemorizationRecords(records, teacherId, userId);
        }
    }
}
