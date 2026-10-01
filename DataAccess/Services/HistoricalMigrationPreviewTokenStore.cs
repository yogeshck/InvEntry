using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace DataAccess.Services;

public interface IHistoricalMigrationPreviewTokenStore
{
    HistoricalMigrationPreviewToken Issue(string administratorIdentity, string supplierGstin,
        DateTime fromDate, DateTime toDate, int excludedCount,
        IReadOnlyCollection<HistoricalMigrationTokenCandidate> candidates);
    bool TryAcquire(string token, string administratorIdentity, out HistoricalMigrationPreviewToken? preview, out string error);
    void Retire(string token);
    void Release(string token);
}

public sealed record HistoricalMigrationTokenCandidate(string InvoiceNumber, DateTime InvoiceDate, string SourceFingerprint);

public sealed class HistoricalMigrationPreviewToken
{
    private int _inUse;
    public required string Token { get; init; }
    public required string AdministratorIdentity { get; init; }
    public required string SupplierGstin { get; init; }
    public required DateTime FromDate { get; init; }
    public required DateTime ToDate { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required int ExcludedCount { get; init; }
    public required IReadOnlyList<HistoricalMigrationTokenCandidate> Candidates { get; init; }
    public bool TryAcquire() => Interlocked.CompareExchange(ref _inUse, 1, 0) == 0;
    public void Release() => Interlocked.Exchange(ref _inUse, 0);
}

public sealed class HistoricalMigrationPreviewTokenStore : IHistoricalMigrationPreviewTokenStore
{
    private readonly TimeSpan _lifetime;
    private readonly TimeProvider _clock;
    private readonly ConcurrentDictionary<string, HistoricalMigrationPreviewToken> _tokens = new(StringComparer.Ordinal);

    public HistoricalMigrationPreviewTokenStore(TimeProvider? clock = null, TimeSpan? lifetime = null)
    { _clock = clock ?? TimeProvider.System; _lifetime = lifetime ?? TimeSpan.FromMinutes(20); }

    public HistoricalMigrationPreviewToken Issue(string administratorIdentity, string supplierGstin,
        DateTime fromDate, DateTime toDate, int excludedCount,
        IReadOnlyCollection<HistoricalMigrationTokenCandidate> candidates)
    {
        var now = _clock.GetUtcNow();
        var value = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var preview = new HistoricalMigrationPreviewToken
        {
            Token = value, AdministratorIdentity = administratorIdentity,
            SupplierGstin = supplierGstin, FromDate = fromDate.Date, ToDate = toDate.Date,
            GeneratedAt = now, ExpiresAt = now.Add(_lifetime), ExcludedCount = excludedCount,
            Candidates = candidates.ToList().AsReadOnly()
        };
        _tokens[value] = preview;
        return preview;
    }

    public bool TryAcquire(string token, string administratorIdentity,
        out HistoricalMigrationPreviewToken? preview, out string error)
    {
        preview = null;
        if (string.IsNullOrWhiteSpace(token) || !_tokens.TryGetValue(token, out var found))
        { error = "The preview token is invalid. Run Preview again."; return false; }
        if (found.ExpiresAt <= _clock.GetUtcNow())
        { _tokens.TryRemove(token, out _); error = "The preview token has expired. Run Preview again."; return false; }
        if (!string.Equals(found.AdministratorIdentity, administratorIdentity, StringComparison.Ordinal))
        { error = "The preview token belongs to a different administrator session."; return false; }
        if (!found.TryAcquire())
        { error = "This preview token is already being processed."; return false; }
        preview = found; error = string.Empty; return true;
    }

    public void Retire(string token) => _tokens.TryRemove(token, out _);
    public void Release(string token) { if (_tokens.TryGetValue(token, out var value)) value.Release(); }
}
