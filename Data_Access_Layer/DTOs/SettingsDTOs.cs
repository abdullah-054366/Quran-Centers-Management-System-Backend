namespace Data_Access_Layer.DTOs;

public sealed class ProfileDTO
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string RoleName { get; set; } = "";
    public int? CenterId { get; set; }
    public string? CenterName { get; set; }
}

public sealed class ProfileUpdateDTO
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}

public sealed class ChangePasswordDTO
{
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}

public sealed class CenterSettingsDTO
{
    public int CenterId { get; set; }
    public string CenterName { get; set; } = "";
    public string CenterCode { get; set; } = "";
    /// <summary>true = التسجيل الذاتي للطلاب برمز المركز مفعّل.</summary>
    public bool CodeEnabled { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string DefaultDays { get; set; } = "0,1,2,3,4";
    public string DefaultStartTime { get; set; } = "16:00";
    public string DefaultEndTime { get; set; } = "18:00";
    public int LateAfterMinutes { get; set; } = 10;
    public bool ExcusedCountsAsAttended { get; set; }
}

public sealed class CenterSettingsUpdateDTO
{
    public string? CenterName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? DefaultDays { get; set; }
    public string? DefaultStartTime { get; set; }
    public string? DefaultEndTime { get; set; }
    public int LateAfterMinutes { get; set; }
    public bool ExcusedCountsAsAttended { get; set; }
}

public sealed class SystemStatusDTO
{
    public bool DatabaseOk { get; set; }
    public long? DatabaseSizeBytes { get; set; }
    public string Version { get; set; } = "";
    public string Environment { get; set; } = "";
    public DateTime ServerTimeUtc { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public int Users { get; set; }
    public int ActiveUsers { get; set; }
    public int Centers { get; set; }
    public int ActiveCenters { get; set; }
    public int Teachers { get; set; }
    public int Students { get; set; }
    public int Halaqat { get; set; }
    public int MemorizationRecords { get; set; }
    public int AttendanceRecords { get; set; }
    public int AuditEntries { get; set; }
    public DateTime? LastAuditAt { get; set; }
}
