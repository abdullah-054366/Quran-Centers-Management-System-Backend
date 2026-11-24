namespace Data_Access_Layer.DTOs;

public sealed class CenterNameDTO
{
    public string CenterName { get; set; } = string.Empty;
}

public sealed class CenterDeletionSummaryDTO
{
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public int Students { get; set; }
    public int Teachers { get; set; }
    public int Halaqat { get; set; }
    public int Users { get; set; }
    public int AttendanceRecords { get; set; }
    public int MemorizationRecords { get; set; }
    public int Plans { get; set; }
    public bool CanDelete { get; set; }
}

/// <summary>ما حُذف فعلاً عند محو مركز بكل بياناته. عملية لا رجعة فيها.</summary>
public sealed class CenterPurgeResultDTO
{
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public int AttendanceRecords { get; set; }
    public int MemorizationRecords { get; set; }
    public int Plans { get; set; }
    public int HalaqaStudents { get; set; }
    public int Halaqat { get; set; }
    public int Students { get; set; }
    public int Teachers { get; set; }
    public int UsersDeleted { get; set; }

    /// <summary>حسابات بقيت لأنها أنشأت سجلات في مركز آخر: فُصلت عن المركز وعُطِّلت.</summary>
    public int UsersDetached { get; set; }
}

public sealed class CenterHalaqaSummaryDTO
{
    public int HalaqaId { get; set; }
    public string HalaqaName { get; set; } = string.Empty;
    public string? TeacherName { get; set; }
    public int StudentCount { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CenterAdminUserDTO
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CenterOverviewDTO
{
    public CenterDTO Center { get; set; } = new();
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int ActiveTeachers { get; set; }
    public int ActiveHalaqat { get; set; }
    public int MonthAttendanceRecords { get; set; }
    public int MonthAttendedRecords { get; set; }
    public int MonthMemorizationRecords { get; set; }
    public DateOnly? LastActivityDate { get; set; }
    public List<CenterHalaqaSummaryDTO> Halaqat { get; set; } = new();
    public List<CenterAdminUserDTO> Admins { get; set; } = new();
}

public sealed class PlatformMonthPointDTO
{
    public string Month { get; set; } = string.Empty;
    public int Centers { get; set; }
    public int Students { get; set; }
}

public sealed class PlatformCenterActivityDTO
{
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ActiveStudents { get; set; }
    public int MonthActivity { get; set; }
    public DateOnly? LastActivityDate { get; set; }
    public int? DaysSinceActivity { get; set; }
}

public sealed class PlatformOverviewDTO
{
    public int TotalCenters { get; set; }
    public int ActiveCenters { get; set; }
    public int TotalUsers { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalHalaqat { get; set; }
    public int MonthAttendanceRecords { get; set; }
    public int MonthAttendedRecords { get; set; }
    public List<PlatformMonthPointDTO> Growth { get; set; } = new();
    public List<PlatformCenterActivityDTO> TopCenters { get; set; } = new();
    public List<PlatformCenterActivityDTO> IdleCenters { get; set; } = new();
}
