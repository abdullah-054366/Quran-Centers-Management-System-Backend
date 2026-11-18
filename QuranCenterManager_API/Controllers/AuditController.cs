using Business_Layer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuranCenterManager_API.Controllers;

[ApiController]
[Route("api/Audit")]
[Authorize(Roles = "PlatformAdmin,Admin")]
public class AuditController : ControllerBase
{
    private readonly AuditBusinessLayer _business;

    public AuditController(AuditBusinessLayer business)
    {
        _business = business;
    }

    [HttpGet(Name = "GetAuditLogs")]
    public async Task<IActionResult> Get(
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] string? search,
        [FromQuery(Name = "from")] DateOnly? fromDate,
        [FromQuery(Name = "to")] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        return Ok(await _business.QueryAsync(action, entityType, search, fromDate, toDate, page, pageSize));
    }
}
