using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DataAccess.Services;

public sealed record FinanceSyncDispatchRequest(
    IReadOnlyList<long> EventIds, int OrgGkey, int LocationGkey, string Actor);
public sealed record FinanceSyncDispatchAccepted(Guid JobId, string Status,
    int RequestedCount);
public sealed record FinanceSyncDispatchJobDto(Guid JobId, string Status,
    int Requested, int Claimed, int Completed, int Sent, int NotSent,
    DateTime CreatedOn, DateTime? StartedOn, DateTime? CompletedOn,
    string Actor, string? SafeError);

internal sealed class FinanceSyncDispatchJobState
{
    public required Guid JobId { get; init; }
    public required long[] EventIds { get; init; }
    public required string Actor { get; init; }
    public string Status { get; set; } = "QUEUED";
    public int Claimed { get; set; }
    public int Completed { get; set; }
    public int Sent { get; set; }
    public int NotSent { get; set; }
    public DateTime CreatedOn { get; init; } = DateTime.UtcNow;
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public string? SafeError { get; set; }
    public FinanceSyncDispatchJobDto Snapshot() => new(JobId, Status,
        EventIds.Length, Claimed, Completed, Sent, NotSent, CreatedOn,
        StartedOn, CompletedOn, Actor, SafeError);
}

public sealed class FinanceSyncDispatchJobQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateBounded<Guid>(
        new BoundedChannelOptions(20) { FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true, SingleWriter = false });
    private readonly ConcurrentDictionary<Guid, FinanceSyncDispatchJobState> _jobs = [];
    private readonly ConcurrentDictionary<long, Guid> _activeEvents = [];

    public FinanceSyncDispatchAccepted Enqueue(IReadOnlyCollection<long> ids, string actor)
    {
        var distinct = ids.Distinct().ToArray();
        if (distinct.Length is < 1 or > 100)
            throw new ArgumentException("Select between 1 and 100 eligible events.");
        if (distinct.Any(id => _activeEvents.ContainsKey(id)))
            throw new InvalidOperationException("One or more selected events already have an active dispatch request.");
        var job = new FinanceSyncDispatchJobState
        {
            JobId = Guid.NewGuid(), EventIds = distinct, Actor = actor
        };
        foreach (var id in distinct)
            if (!_activeEvents.TryAdd(id, job.JobId))
            {
                Release(job);
                throw new InvalidOperationException("One or more selected events already have an active dispatch request.");
            }
        _jobs[job.JobId] = job;
        if (!_queue.Writer.TryWrite(job.JobId))
        {
            _jobs.TryRemove(job.JobId, out _);
            Release(job);
            throw new InvalidOperationException("The dispatch queue is busy. Try again shortly.");
        }
        return new(job.JobId, job.Status, distinct.Length);
    }

    public FinanceSyncDispatchJobDto? Find(Guid id) =>
        _jobs.TryGetValue(id, out var job) ? job.Snapshot() : null;
    internal IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken token) =>
        _queue.Reader.ReadAllAsync(token);
    internal bool TryGet(Guid id, out FinanceSyncDispatchJobState state) =>
        _jobs.TryGetValue(id, out state!);
    internal void Release(FinanceSyncDispatchJobState job)
    {
        foreach (var id in job.EventIds)
            _activeEvents.TryRemove(new KeyValuePair<long, Guid>(id, job.JobId));
    }
}

public sealed class FinanceSyncManualDispatchWorker : BackgroundService
{
    private readonly FinanceSyncDispatchJobQueue _jobs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FinanceSyncManualDispatchWorker> _logger;
    public FinanceSyncManualDispatchWorker(FinanceSyncDispatchJobQueue jobs,
        IServiceScopeFactory scopeFactory, ILogger<FinanceSyncManualDispatchWorker> logger) =>
        (_jobs, _scopeFactory, _logger) = (jobs, scopeFactory, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in _jobs.ReadAllAsync(stoppingToken))
        {
            if (!_jobs.TryGet(jobId, out var job)) continue;
            job.Status = "RUNNING";
            job.StartedOn = DateTime.UtcNow;
            _logger.LogInformation("Finance sync manual job {JobId} started by {Actor}; events {EventIds}",
                job.JobId, job.Actor, job.EventIds);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<FinanceSyncService>();
                var result = await service.DispatchSelectedAsync(job.EventIds, progress =>
                {
                    job.Claimed = progress.Completed + progress.Remaining;
                    job.Completed = progress.Completed;
                    job.Sent = progress.Sent;
                    job.NotSent = progress.Completed - progress.Sent;
                    return Task.CompletedTask;
                }, stoppingToken);
                job.Claimed = result.Claimed;
                job.Completed = result.Claimed;
                job.Sent = result.Sent;
                job.NotSent = result.NotSent + (result.Requested - result.Claimed);
                job.Status = "COMPLETED";
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                job.Status = "CANCELLED";
            }
            catch (Exception ex)
            {
                job.Status = "FAILED";
                job.SafeError = "The dispatch job could not be completed. Review server logs and event status before retrying.";
                _logger.LogError(ex, "Finance sync manual job {JobId} failed", job.JobId);
            }
            finally
            {
                job.CompletedOn = DateTime.UtcNow;
                _jobs.Release(job);
                _logger.LogInformation("Finance sync manual job {JobId} ended with {Status}; sent {Sent}; not sent {NotSent}",
                    job.JobId, job.Status, job.Sent, job.NotSent);
            }
        }
    }
}
