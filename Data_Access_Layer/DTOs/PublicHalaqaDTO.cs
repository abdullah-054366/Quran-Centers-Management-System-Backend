namespace Data_Access_Layer.DTOs;

/// <summary>
/// حلقة كما تُعرض للطالب أثناء التسجيل الذاتي (بلا بيانات حساسة).
/// </summary>
public sealed class PublicHalaqaDTO
{
    public int HalaqaId { get; set; }
    public string HalaqaName { get; set; } = string.Empty;
    public string? TeacherName { get; set; }
    public string? Description { get; set; }
    public int StudentCount { get; set; }
}
