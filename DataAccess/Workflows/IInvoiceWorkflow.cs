using InvEntry.Contracts.Invoices;

namespace DataAccess.Workflows;

public interface IInvoiceWorkflow
{
    Task<SaveInvoiceResponse> SaveDraftAsync(
        SaveInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<InvoiceEditResponse> GetForEditAsync(
        int invoiceGkey,
        CancellationToken cancellationToken = default);

    Task<FinaliseInvoiceResponse> FinaliseAsync(
        int invoiceGkey,
        CancellationToken cancellationToken = default);

    Task CancelDraftAsync(
        int invoiceGkey,
        CancellationToken cancellationToken = default);
}