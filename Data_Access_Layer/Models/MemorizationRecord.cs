using System;
using System.Collections.Generic;

namespace Data_Access_Layer.Models;

public partial class MemorizationRecord
{
    public int MemorizationRecordId { get; set; }

    public int CenterId { get; set; }

    public int StudentId { get; set; }

    public int TeacherId { get; set; }

    public short Type { get; set; }

    public short SurahNumber { get; set; }

    public short FromAyah { get; set; }

    public short ToAyah { get; set; }

    public DateOnly RecordDate { get; set; }

    public string? Notes { get; set; }

    public int CreatedByUserId { get; set; }

    public short Source { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? SessionId { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
