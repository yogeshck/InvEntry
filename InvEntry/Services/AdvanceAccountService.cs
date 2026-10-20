using InvEntry.Contracts.Advances;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.Services;

/// <summary>
/// Customer advance / RD accounts. Balances are kept by the API; the client no
/// longer updates LEDGERS_HEADER for advance or RD money.
/// </summary>
public interface IAdvanceAccountService
{
    /// <summary>
    /// Records an advance / RD receipt (voucher + ledger + balance) on the server.
    /// Reuse the same request (same ClientRequestId) when retrying after a failure.
    /// </summary>
    Task<AdvanceReceiptResponse> CreateReceipt(AdvanceReceiptRequest request);

    /// <summary>Active advance / RD accounts with a balance, for invoice settlement.</summary>
    Task<IReadOnlyList<AdvanceAccountSummary>> GetAvailableAccounts(int customerGkey);

    /// <summary>Receipt print data, as it stood right after the receipt.</summary>
    Task<AdvanceReceiptPrintDto> GetReceiptPrint(int voucherGkey);
}

public class AdvanceAccountService : IAdvanceAccountService
{
    private readonly IMijmsApiService _mijmsApiService;

    public AdvanceAccountService(IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public Task<AdvanceReceiptResponse> CreateReceipt(AdvanceReceiptRequest request)
    {
        return _mijmsApiService.Post<AdvanceReceiptRequest, AdvanceReceiptResponse>(
            "api/AdvanceAccount/receipt", request);
    }

    public async Task<IReadOnlyList<AdvanceAccountSummary>> GetAvailableAccounts(int customerGkey)
    {
        return await _mijmsApiService.GetResponse<List<AdvanceAccountSummary>>(
            $"api/AdvanceAccount/customer/{customerGkey}/available");
    }

    public Task<AdvanceReceiptPrintDto> GetReceiptPrint(int voucherGkey)
    {
        return _mijmsApiService.GetResponse<AdvanceReceiptPrintDto>(
            $"api/AdvanceAccount/receipt/{voucherGkey}/print");
    }
}
