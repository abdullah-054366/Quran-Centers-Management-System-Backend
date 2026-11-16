using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Data_Access_Layer.Models;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public int? CenterId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    [JsonIgnore]
    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public DateTime? RefreshTokenRevokedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Center? Center { get; set; }

    public virtual ICollection<MemorizationRecord> MemorizationRecords { get; set; } = new List<MemorizationRecord>();

    public virtual Role Role { get; set; } = null!;

    public virtual Student? Student { get; set; }

    public virtual Teacher? Teacher { get; set; }
}



