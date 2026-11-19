using Data_Access_Layer;
using Data_Access_Layer.DTOs;

namespace Business_Layer;

public sealed class HalaqaBusinessLayer
{
    private readonly HalaqaData _halaqaData;

    public HalaqaBusinessLayer(HalaqaData halaqaData)
    {
        _halaqaData = halaqaData;
    }

    public Task<List<HalaqaDTO>> GetAllAsync(int? centerId = null)
    {
        return _halaqaData.GetAllAsync(centerId);
    }

    public Task<HalaqaDTO?> GetByIdAsync(int halaqaId, int? centerId = null)
    {
        return _halaqaData.GetByIdAsync(halaqaId, centerId);
    }

    public Task<List<HalaqaStudentDTO>> GetStudentsAsync(int halaqaId, int? centerId = null)
    {
        return _halaqaData.GetStudentsAsync(halaqaId, centerId);
    }

    public Task<List<PublicHalaqaDTO>> GetForRegistrationAsync(int centerId)
    {
        return _halaqaData.GetForRegistrationAsync(centerId);
    }

    public Task<bool> IsOpenForRegistrationAsync(int halaqaId, int centerId)
    {
        return _halaqaData.IsOpenForRegistrationAsync(halaqaId, centerId);
    }

    public Task<List<StudentDTO>> GetAvailableStudentsAsync(int halaqaId, int? centerId = null)
    {
        return _halaqaData.GetAvailableStudentsAsync(halaqaId, centerId);
    }

    public Task<bool> AssignStudentsAsync(
        int halaqaId,
        IEnumerable<int> studentIds,
        int? centerId = null)
    {
        return _halaqaData.AssignStudentsAsync(halaqaId, studentIds, centerId);
    }

    public Task<bool> RemoveStudentAsync(int halaqaId, int studentId, int? centerId = null)
    {
        return _halaqaData.RemoveStudentAsync(halaqaId, studentId, centerId);
    }

    public Task<bool> CanAssignAsync(
        int centerId,
        int? teacherId,
        bool allowInactiveCenter = false,
        int? allowedInactiveTeacherId = null)
    {
        return _halaqaData.CanAssignAsync(centerId, teacherId, allowInactiveCenter, allowedInactiveTeacherId);
    }

    public Task<HalaqaDTO> CreateAsync(HalaqaWriteDTO input)
    {
        return _halaqaData.CreateAsync(input);
    }

    public Task<HalaqaDTO?> UpdateAsync(int halaqaId, HalaqaWriteDTO input, int? centerScope = null)
    {
        return _halaqaData.UpdateAsync(halaqaId, input, centerScope);
    }

    public Task<bool> DeleteAsync(int halaqaId, int? centerScope = null)
    {
        return _halaqaData.DeleteAsync(halaqaId, centerScope);
    }
}
