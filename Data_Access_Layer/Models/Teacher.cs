using System;
using System.Collections.Generic;

namespace Data_Access_Layer.Models;

public partial class Teacher
{
    public int TeacherId { get; set; }

    public int UserId { get; set; }

    public int CenterId { get; set; }

    public bool IsActive { get; set; }

    public DateOnly JoinedAt { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual ICollection<Halaqat> Halaqats { get; set; } = new List<Halaqat>();

    public virtual ICollection<MemorizationRecord> MemorizationRecords { get; set; } = new List<MemorizationRecord>();

    public virtual User User { get; set; } = null!;
}
