namespace Data_Access_Layer.Models;

public partial class AuditLog
{
    public long AuditLogId { get; set; }

    public int? ActorUserId { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public string ActorRole { get; set; } = string.Empty;

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? EntityName { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }
}
