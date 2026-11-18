using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;

namespace Data_Access_Layer;

public sealed class AuditData
{
    private readonly HalaqaDbContext _context;

    public AuditData(HalaqaDbContext context)
    {
        _context = context;
    }

    private static string? Cut(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        value = value.Trim();
        return value.Length <= max ? value : value[..max];
    }

    // فشل التدقيق لا يجب أن يكسر العملية الأصلية.
    public async Task LogAsync(AuditEntryDTO entry)
    {
        var log = new AuditLog
        {
            ActorUserId = entry.ActorUserId,
            ActorName = Cut(entry.ActorName, 200) ?? string.Empty,
            ActorRole = Cut(entry.ActorRole, 50) ?? string.Empty,
            Action = Cut(entry.Action, 60) ?? "unknown",
            EntityType = Cut(entry.EntityType, 40) ?? "unknown",
            EntityId = Cut(entry.EntityId, 40),
            EntityName = Cut(entry.EntityName, 200),
            Details = Cut(entry.Details, 1000)
        };

        try
        {
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch
        {
            _context.Entry(log).State = EntityState.Detached;
        }
    }

    public async Task<AuditPageDTO> QueryAsync(
        string? action,
        string? entityType,
        string? search,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(log => log.Action == action);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(log => log.EntityType == entityType);

        if (fromDate.HasValue)
        {
            var start = fromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(log => log.CreatedAt >= start);
        }

        if (toDate.HasValue)
        {
            var end = toDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(log => log.CreatedAt < end);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(log =>
                log.ActorName.ToLower().Contains(term)
                || (log.EntityName != null && log.EntityName.ToLower().Contains(term))
                || (log.Details != null && log.Details.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.AuditLogId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new AuditLogDTO
            {
                AuditLogId = log.AuditLogId,
                ActorUserId = log.ActorUserId,
                ActorName = log.ActorName,
                ActorRole = log.ActorRole,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                EntityName = log.EntityName,
                Details = log.Details,
                CreatedAt = log.CreatedAt
            })
            .ToListAsync();

        return new AuditPageDTO { Items = items, Total = total, Page = page, PageSize = pageSize };
    }
}
