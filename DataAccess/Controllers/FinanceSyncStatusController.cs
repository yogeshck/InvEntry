using DataAccess.Services;
using InvEntry.Contracts.FinanceSync;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

/// <summary>
/// Finance sync status for the InvEntry desktop app: what has not reached
/// FinanceTracker, why, and a way to send it again.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class FinanceSyncStatusController : ControllerBase
{
    private readonly FinanceSyncStatusService _status;
    private readonly ILogger<FinanceSyncStatusController> _logger;

    public FinanceSyncStatusController(FinanceSyncStatusService status,
        ILogger<FinanceSyncStatusController> logger)
    {
        _status = status;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken token) =>
        Ok(await _status.GetAsync(token));

    [HttpPost("send")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> Send([FromBody] FinanceSyncSendRequestDto request,
        CancellationToken token)
    {
        var actor = $"inventry:{Clean(request.Actor)}";
        var result = await _status.SendAsync(request.EventIds ?? [], actor, token);
        _logger.LogInformation("Finance sync send from {Actor}: {Requested} events, job {JobId}",
            actor, result.Requested, result.JobId);
        return Ok(result);
    }

    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";
        value = value.Trim();
        return value.Length <= 80 ? value : value[..80];
    }
}
