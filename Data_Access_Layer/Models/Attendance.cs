using System;
using System.Collections.Generic;

namespace Data_Access_Layer.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int CenterId { get; set; }

    public int HalaqaId { get; set; }

    public int StudentId { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public short Status { get; set; }

    public string? Notes { get; set; }

    public int RecordedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual Halaqat Halaqa { get; set; } = null!;

    public virtual User RecordedByUser { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
