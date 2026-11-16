namespace Data_Access_Layer.DTOs
{
    public class MemorizationStudentDTO
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string HalaqaName { get; set; } = string.Empty;
    }

    public class MemorizationRecordWriteDTO
    {
        public int StudentId { get; set; }
        public short Type { get; set; }
        public short SurahNumber { get; set; }
        public short FromAyah { get; set; }
        public short ToAyah { get; set; }
        public DateOnly RecordDate { get; set; }
        public string? Notes { get; set; }
    }

    public class MemorizationRecordDTO
    {
        public int MemorizationRecordId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public short Type { get; set; }
        public short SurahNumber { get; set; }
        public short FromAyah { get; set; }
        public short ToAyah { get; set; }
        public DateOnly RecordDate { get; set; }
        public string? Notes { get; set; }
    }
}
