namespace StudentApi.DTOs.Auth
{
    public class RegisterStudentRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public string? CenterCode { get; set; }

        /// <summary>الحلقة التي اختارها الطالب من حلقات المركز.</summary>
        public int? HalaqaId { get; set; }
    }
}
