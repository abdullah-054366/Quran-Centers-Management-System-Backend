// Data_Access_Layer/DTOs/StudentDTO.cs
namespace Data_Access_Layer.DTOs
{
    public class StudentDTO
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateOnly JoinedAt { get; set; }

        public StudentDTO() { }

        public StudentDTO(int studentId, int userId, string firstName, string? lastName,
                          string email, string? phoneNumber, bool isActive, DateOnly joinedAt)
        {
            StudentId = studentId;
            UserId = userId;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            IsActive = isActive;
            JoinedAt = joinedAt;
        }
    }
}