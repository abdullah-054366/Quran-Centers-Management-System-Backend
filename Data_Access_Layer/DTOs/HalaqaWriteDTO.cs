namespace Data_Access_Layer.DTOs;

public sealed class HalaqaWriteDTO
{
    public int? CenterId { get; set; }
    public int? TeacherId { get; set; }
    public string HalaqaName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
