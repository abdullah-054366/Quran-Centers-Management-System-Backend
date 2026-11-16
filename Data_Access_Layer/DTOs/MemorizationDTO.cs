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
        /// <summary>يجمع سجلات سور الجلسة الواحدة؛ يُولَّد في الخادم إن لم يُرسل.</summary>
        public Guid? SessionId { get; set; }
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
        public Guid? SessionId { get; set; }
    }

    public enum MemorizationSaveStatus
    {
        Saved,
        NotAllowed,
        LimitReached,
        DuplicateSession
    }

    public class MemorizationSaveResult
    {
        public MemorizationSaveStatus Status { get; set; }
        public string? Message { get; set; }
        public int SavedCount { get; set; }
    }

    public class MemorizationDailyUsageDTO
    {
        public int StudentId { get; set; }
        public string Date { get; set; } = string.Empty;
        public int MemorizationCount { get; set; }
        public int ReviewCount { get; set; }
        public int MemorizationLimit { get; set; }
        public int ReviewLimit { get; set; }
    }
}
