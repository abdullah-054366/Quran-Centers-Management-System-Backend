using System;
using System.Collections.Generic;

namespace Data_Access_Layer.Models;

public partial class Center
{
    public int CenterId { get; set; }

    public string CenterName { get; set; } = null!;

    public string CenterCode { get; set; } = null!;

    public bool CodeEnabled { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<HalaqaStudent> HalaqaStudents { get; set; } = new List<HalaqaStudent>();

    public virtual ICollection<Halaqat> Halaqats { get; set; } = new List<Halaqat>();

    public virtual ICollection<MemorizationRecord> MemorizationRecords { get; set; } = new List<MemorizationRecord>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
