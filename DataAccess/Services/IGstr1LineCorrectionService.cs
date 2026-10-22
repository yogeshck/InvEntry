using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IGstr1LineCorrectionService
{
    /// <summary>
    /// Returns null when the document/line is not in the requested return scope.
    /// </summary>
    Task<Gstr1LineCorrectionResponse?> GetAsync(
        long documentGkey,
        int lineNbr,
        string supplierGstin,
        string returnPeriod,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns null when the document/line is not in the requested return scope.
    /// Throws <see cref="ArgumentException"/> when the correction is invalid.
    /// </summary>
    Task<Gstr1LineCorrectionResponse?> SaveAsync(
        long documentGkey,
        int lineNbr,
        Gstr1LineCorrectionRequest request,
        CancellationToken cancellationToken = default);
}
