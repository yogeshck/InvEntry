namespace DataAccess.Services;

/// <summary>
/// Last cycles of the automatic finance sync, so the status screens can tell
/// users whether it is running. Kept in memory: a restart starts it afresh.
/// </summary>
public sealed class FinanceSyncHeartbeat
{
    /// <summary>A cycle runs every 30 seconds; none for this long means it has stopped.</summary>
    public static readonly TimeSpan StoppedAfter = TimeSpan.FromMinutes(3);

    private readonly object _gate = new();
    private DateTime? _lastCompletedUtc;
    private DateTime? _lastFailedUtc;
    private string? _lastError;

    public void Completed()
    {
        lock (_gate)
        {
            _lastCompletedUtc = DateTime.UtcNow;
            _lastError = null;
        }
    }

    public void Failed(Exception exception)
    {
        lock (_gate)
        {
            _lastFailedUtc = DateTime.UtcNow;
            _lastError = exception.InnerException?.Message ?? exception.Message;
        }
    }

    public (DateTime? LastCompletedUtc, DateTime? LastFailedUtc, string? LastError) Snapshot()
    {
        lock (_gate)
            return (_lastCompletedUtc, _lastFailedUtc, _lastError);
    }
}
