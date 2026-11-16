using System;
using System.Collections.Generic;

namespace Data_Access_Layer.Models;

public partial class Halaqat
{
    public int HalaqaId { get; set; }

    public int CenterId { get; set; }

    public int? TeacherId { get; set; }

    public string HalaqaName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Center Center { get; set; } = null!;

    public virtual ICollection<HalaqaStudent> HalaqaStudents { get; set; } = new List<HalaqaStudent>();

    public virtual Teacher? Teacher { get; set; }
}
