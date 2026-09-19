using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IGstr1ValidationService
{
    Task<Gstr1ValidationResponse> ValidateAsync(
        string supplierGstin,
        string returnPeriod,
        CancellationToken cancellationToken = default);
}