namespace Data_Access_Layer.DTOs;

public sealed class HalaqaDTO
{
    public int HalaqaId { get; set; }
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string HalaqaName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StudentCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
