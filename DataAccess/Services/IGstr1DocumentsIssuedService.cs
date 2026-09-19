using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IGstr1DocumentsIssuedService
{
    Task<Gstr1DocumentsIssuedResponse> GetAsync(
        string supplierGstin,
        string returnPeriod,
        CancellationToken cancellationToken = default);
}
