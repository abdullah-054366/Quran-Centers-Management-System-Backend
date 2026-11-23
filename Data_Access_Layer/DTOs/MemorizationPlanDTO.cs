namespace Data_Access_Layer.DTOs
{
    public class MemorizationPlanDTO
    {
        public int MemorizationPlanId { get; set; }
        public int CenterId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public short Type { get; set; }
        public short FromSurah { get; set; }
        public short ToSurah { get; set; }
        public short PagesPerSession { get; set; }
        public short TargetRounds { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool DeriveAmount { get; set; }
        public int[] DaysOfWeek { get; set; } = Array.Empty<int>();
        public bool IsActive { get; set; }
        public int CompletedSessions { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MemorizationPlanWriteDTO
    {
        public int StudentId { get; set; }
        public short Type { get; set; }
        public short FromSurah { get; set; }
        public short ToSurah { get; set; }
        public short PagesPerSession { get; set; }
        public short TargetRounds { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool DeriveAmount { get; set; }
        public int[] DaysOfWeek { get; set; } = Array.Empty<int>();
    }

    public class MemorizationPlanStatusDTO
    {
        public bool IsActive { get; set; }
    }
}
