using System;
using System.Collections.Generic;

namespace Data_Access_Layer.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public int UserId { get; set; }

    public int CenterId { get; set; }

    public bool IsActive { get; set; }

    public DateOnly JoinedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Center Center { get; set; } = null!;

    public virtual HalaqaStudent? HalaqaStudent { get; set; }

    public virtual ICollection<MemorizationRecord> MemorizationRecords { get; set; } = new List<MemorizationRecord>();

    public virtual User User { get; set; } = null!;
}
