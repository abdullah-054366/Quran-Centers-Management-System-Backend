using Data_Access_Layer;
using Data_Access_Layer.DTOs;

namespace Business_Layer
{
    public class MemorizationPlanBusinessLayer
    {
        private readonly MemorizationPlanData _memorizationPlanData;

        public MemorizationPlanBusinessLayer(MemorizationPlanData memorizationPlanData)
        {
            _memorizationPlanData = memorizationPlanData;
        }

        public async Task<List<MemorizationPlanDTO>> GetAllAsync(
            int? centerId = null,
            int? teacherId = null,
            int? studentId = null)
        {
            return await _memorizationPlanData.GetAllAsync(centerId, teacherId, studentId);
        }

        public async Task<MemorizationPlanDTO?> GetByIdAsync(
            int planId,
            int? centerId = null,
            int? teacherId = null,
            int? studentId = null)
        {
            return await _memorizationPlanData.GetByIdAsync(planId, centerId, teacherId, studentId);
        }

        public async Task<MemorizationPlanDTO?> CreateAsync(
            MemorizationPlanWriteDTO input,
            int? centerId,
            int? teacherId,
            int userId)
        {
            return await _memorizationPlanData.CreateAsync(input, centerId, teacherId, userId);
        }

        public async Task<bool> HasOverlappingActivePlanAsync(MemorizationPlanWriteDTO input, int? excludePlanId = null)
        {
            return await _memorizationPlanData.HasOverlappingActivePlanAsync(input, excludePlanId);
        }

        public async Task<MemorizationPlanDTO?> UpdateAsync(
            int planId,
            MemorizationPlanWriteDTO input,
            int? centerId,
            int? teacherId,
            int? studentId = null)
        {
            return await _memorizationPlanData.UpdateAsync(planId, input, centerId, teacherId, studentId);
        }

        public async Task<MemorizationPlanDTO?> UpdateStatusAsync(
            int planId,
            bool isActive,
            int? centerId,
            int? teacherId,
            int? studentId = null)
        {
            return await _memorizationPlanData.UpdateStatusAsync(planId, isActive, centerId, teacherId, studentId);
        }
    }
}
