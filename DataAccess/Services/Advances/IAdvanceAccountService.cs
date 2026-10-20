using DataAccess.Models;
using InvEntry.Contracts.Advances;
using InvEntry.Contracts.Invoices;

namespace DataAccess.Services.Advances;

public interface IAdvanceAccountService
{
    /// <summary>
    /// Records money received into an advance / RD account: the VOUCHER, the
    /// ADVANCE_LEDGER RECEIPT row and the account's cached totals, in one
    /// transaction. A retry with the same ClientRequestId returns the original
    /// receipt.
    /// </summary>
    Task<AdvanceReceiptResponse> CreateReceiptAsync(
        AdvanceReceiptRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active accounts of the customer with a balance, for the billing settlement screen.
    /// </summary>
    Task<IReadOnlyList<AdvanceAccountSummary>> GetAvailableAccountsAsync(
        int customerGkey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the "Advance Adj" / "RD Adj" settlement lines of an invoice
    /// before anything is written. Throws <see cref="AdvanceAccountBusinessException"/>.
    /// </summary>
    Task ValidateInvoiceAdjustmentsAsync(
        int? customerGkey,
        IEnumerable<InvoiceSettlementSaveModel> adjustmentLines,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Debits each advance account used by the invoice's adjustment lines and adds
    /// an ADJUSTMENT ledger row (SOURCE_REF INVOICE-&lt;gkey&gt;-ADJ-&lt;account&gt;).
    /// Must run inside the caller's invoice transaction; the ledger rows are saved
    /// by the caller's SaveChanges. Fails when a balance is insufficient.
    /// </summary>
    Task<IReadOnlyList<AdvanceLedger>> ApplyInvoiceAdjustmentsAsync(
        InvoiceHeader invoice,
        IEnumerable<InvoiceSettlementSaveModel> adjustmentLines,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A business rule on an advance account was not met (insufficient balance,
/// closed account, account of another customer, ...). Safe to show to the user.
/// </summary>
public sealed class AdvanceAccountBusinessException : InvalidOperationException
{
    public AdvanceAccountBusinessException(string message)
        : base(message)
    {
    }
}
