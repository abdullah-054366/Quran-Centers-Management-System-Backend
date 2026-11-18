using System.Security.Claims;
using Business_Layer;
using Data_Access_Layer.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace QuranCenterManager_API.Controllers;

public static class AuditExtensions
{
    public static int? GetUserId(this ControllerBase controller)
    {
        return int.TryParse(controller.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }

    public static Task LogAuditAsync(
        this ControllerBase controller,
        AuditBusinessLayer audit,
        string action,
        string entityType,
        object? entityId,
        string? entityName,
        string? details = null)
    {
        return audit.LogAsync(new AuditEntryDTO
        {
            ActorUserId = controller.GetUserId(),
            ActorName = controller.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            ActorRole = controller.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            Action = action,
            EntityType = entityType,
            EntityId = entityId?.ToString(),
            EntityName = entityName,
            Details = details
        });
    }
}
