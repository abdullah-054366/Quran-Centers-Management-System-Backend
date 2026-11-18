using System;

namespace Data_Access_Layer.Models;

/// <summary>إعدادات المركز الإضافية (صف واحد لكل مركز، وينشأ عند أول حفظ).</summary>
public partial class CenterSetting
{
    public int CenterId { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    /// <summary>أيام الحلقات الافتراضية: أرقام 0=الأحد ... 6=السبت مفصولة بفاصلة.</summary>
    public string DefaultDays { get; set; } = "0,1,2,3,4";
    public string DefaultStartTime { get; set; } = "16:00";
    public string DefaultEndTime { get; set; } = "18:00";
    public int LateAfterMinutes { get; set; } = 10;
    public bool ExcusedCountsAsAttended { get; set; }
    public DateTime UpdatedAt { get; set; }
}
