namespace Data_Access_Layer.DTOs;

public sealed class TeacherWriteDTO
{
    public int? CenterId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
