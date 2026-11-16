

namespace Data_Access_Layer.DTOs
{
    public class TeacherDTO
    {
        public int TeacherId { get; set; }
        public int UserId { get; set; }
        public int CenterId { get; set; }
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; }
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateOnly JoinedAt { get; set; }

        public string? CenterName { get; set; }

        public string? CenterCode { get; set; }

        public TeacherDTO(int teacherId, int userId, string firstName, string? lastName, string email, string? phoneNumber, bool isActive, DateOnly joinedAt, string? centerName = null, string? centerCode = null, int centerId = 0)
        {
            TeacherId = teacherId;
            UserId = userId;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            IsActive = isActive;
            JoinedAt = joinedAt;
            CenterName = centerName;
            CenterCode = centerCode;
            CenterId = centerId;
        }

        public TeacherDTO() { }

      
    }
}
