using Data_Access_Layer;
using Data_Access_Layer.DTOs;

namespace Business_Layer;

public sealed class AuditBusinessLayer
{
    private readonly AuditData _data;

    public AuditBusinessLayer(AuditData data)
    {
        _data = data;
    }

    public Task LogAsync(AuditEntryDTO entry) => _data.LogAsync(entry);

    public Task<AuditPageDTO> QueryAsync(
        string? action, string? entityType, string? search,
        DateOnly? fromDate, DateOnly? toDate, int page, int pageSize)
        => _data.QueryAsync(action, entityType, search, fromDate, toDate, page, pageSize);
}
