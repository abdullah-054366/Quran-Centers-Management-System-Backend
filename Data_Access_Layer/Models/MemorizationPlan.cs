namespace Data_Access_Layer.Models;

public partial class MemorizationPlan
{
    public int MemorizationPlanId { get; set; }

    public int CenterId { get; set; }

    public int StudentId { get; set; }

    public short Type { get; set; }

    public short FromSurah { get; set; }

    public short ToSurah { get; set; }

    public short PagesPerSession { get; set; }

    public short TargetRounds { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>آخر يوم في مدّة الخطة. null = خطة مفتوحة النهاية يحكمها PagesPerSession.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>true = مقدار الجلسة مشتقّ من النطاق والمدّة، وPagesPerSession تقريب للعرض فقط.</summary>
    public bool DeriveAmount { get; set; }

    public int[] DaysOfWeek { get; set; } = Array.Empty<int>();

    public bool IsActive { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;
}
