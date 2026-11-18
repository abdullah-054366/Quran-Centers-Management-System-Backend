namespace Data_Access_Layer.DTOs;

public sealed class AuditEntryDTO
{
    public int? ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? Details { get; set; }
}

public sealed class AuditLogDTO
{
    public long AuditLogId { get; set; }
    public int? ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AuditPageDTO
{
    public List<AuditLogDTO> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
