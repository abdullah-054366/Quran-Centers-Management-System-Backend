using System;
using System.Collections.Generic;

namespace Data_Access_Layer.Models;

public partial class HalaqaStudent
{
    public int HalaqaStudentId { get; set; }

    public int CenterId { get; set; }

    public int HalaqaId { get; set; }

    public int StudentId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual Halaqat Halaqa { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
