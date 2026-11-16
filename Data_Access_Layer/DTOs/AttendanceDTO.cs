namespace Data_Access_Layer.DTOs
{
    public class AttendanceDTO
    {
        public int AttendanceId { get; set; }

        public string? StudentName { get; set; }
        public int CenterId { get; set; }

        public string? CenterName { get; set; }

       
        public int HalaqaId { get; set; }

        public int StudentId { get; set; }

        public DateOnly AttendanceDate { get; set; }

        public short Status { get; set; }

        public string? Notes { get; set; }

        public int RecordedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }



        public AttendanceDTO(int attendanceId, int centerId, int halaqaId, int studentId, DateOnly attendanceDate, 
            short status, string? notes, int recordedByUserId, DateTime createdAt , string fullName , string centerName)
        {
            AttendanceId = attendanceId;
            CenterId = centerId;
            CenterName = centerName;
            HalaqaId = halaqaId;
            StudentId = studentId;
            AttendanceDate = attendanceDate;
            Status = status;
            Notes = notes;
            RecordedByUserId = recordedByUserId;
            CreatedAt = createdAt;
            StudentName = fullName;
        }


        public AttendanceDTO() { }


    }
}