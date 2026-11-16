namespace Data_Access_Layer.DTOs;

public sealed class HalaqaStudentDTO
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateOnly StartDate { get; set; }
}

public sealed class HalaqaStudentAssignmentDTO
{
    public int[]? StudentIds { get; set; }
}
