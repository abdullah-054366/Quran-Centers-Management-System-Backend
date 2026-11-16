namespace Data_Access_Layer.DTOs
{
    public class AttendanceHalaqaDTO
    {
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
    }

    public class AttendanceStudentDTO
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
    }
}
