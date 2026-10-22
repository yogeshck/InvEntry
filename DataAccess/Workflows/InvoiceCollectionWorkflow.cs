using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Services;
using InvEntry.Contracts.Invoices;
using Microsoft.Extensions.Options;

namespace DataAccess.Workflows;

public interface IInvoiceCollectionWorkflow
{
    Task<SaveInvoiceCollectionResponse> SaveAsync(
        SaveInvoiceCollectionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Saves the receipts collected later on a credit invoice (outstanding-invoice screen)
/// as one unit: a voucher and an AR receipt per line, the invoice's received amount and
/// balance, and the FinanceTracker collection events. Either all of it is saved or none.
/// </summary>
public sealed class InvoiceCollectionWorkflow : IInvoiceCollectionWorkflow
{
    private readonly IRepositoryBase<InvoiceHeader> _invoices;
    private readonly IRepositoryBase<InvoiceArReceipt> _receipts;
    private readonly IRepositoryBase<Voucher> _vouchers;
    private readonly IRepositoryBase<VoucherType> _voucherTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinanceSyncService? _financeSyncService;
    private readonly FinanceSyncOptions _financeSyncOptions;

    public InvoiceCollectionWorkflow(
        IRepositoryBase<InvoiceHeader> invoices,
        IRepositoryBase<InvoiceArReceipt> receipts,
        IRepositoryBase<Voucher> vouchers,
        IRepositoryBase<VoucherType> voucherTypes,
        IUnitOfWork unitOfWork,
        FinanceSyncService? financeSyncService = null,
        IOptions<FinanceSyncOptions>? financeSyncOptions = null)
    {
        _invoices = invoices;
        _receipts = receipts;
        _vouchers = vouchers;
        _voucherTypes = voucherTypes;
        _unitOfWork = unitOfWork;
        _financeSyncService = financeSyncService;
        _financeSyncOptions = financeSyncOptions?.Value ?? new FinanceSyncOptions { Enabled = false };
    }

    public async Task<SaveInvoiceCollectionResponse> SaveAsync(
        SaveInvoiceCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Lines ??= [];

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Invoice and estimate gkeys overlap, so match the number as well.
            var invoice = _invoices.Get(x => x.Gkey == request.InvoiceGkey && x.InvNbr == request.InvNbr)
                ?? throw new KeyNotFoundException($"Invoice {request.InvNbr} was not found.");

            if (!InvoiceStatus.IsFinalised(invoice.Status?.Trim()))
                throw new InvalidOperationException($"Invoice {invoice.InvNbr} is not finalised.");

            var balance = invoice.InvBalance.GetValueOrDefault();
            var error = InvoiceCollectionRules.Validate(balance, request.Lines);
            if (error is not null)
                throw new InvalidOperationException(error);

            var now = DateTime.Now;
            var receivedOn = request.ReceivedOn == default ? now.Date : request.ReceivedOn.Date;

            var receiptSeq = _receipts
                .GetList(x => x.InvoiceGkey == invoice.Gkey && x.InvoiceNbr == invoice.InvNbr)
                .Select(x => x.SeqNbr)
                .DefaultIfEmpty(0)
                .Max();

            // Same daily sequence as vouchers posted one at a time (VoucherController.Post).
            var today = now.Date;
            var tomorrow = today.AddDays(1);
            var voucherSeq = _vouchers
                .GetList(x => x.VoucherDate >= today && x.VoucherDate < tomorrow)
                .Select(x => x.SeqNbr.GetValueOrDefault())
                .DefaultIfEmpty(0)
                .Max();

            var running = balance;
            var vouchers = new List<Voucher>();

            foreach (var line in request.Lines)
            {
                var voucherTypeName = InvoiceCollectionRules.VoucherType(line.PaymentType);
                var voucherType = _voucherTypes.Get(x => x.DocumentType == voucherTypeName)
                    ?? throw new InvalidOperationException(
                        $"Payment type '{line.PaymentType}' has no voucher type configured.");

                voucherType.LastUsedNumber = voucherType.LastUsedNumber.GetValueOrDefault() + 1;
                _voucherTypes.Update(voucherType);

                var voucherNbr = $"{voucherType.DocNbrPrefix}{voucherType.LastUsedNumber.Value:D4}";
                var mode = InvoiceCollectionRules.VoucherMode(line.PaymentType);
                var reference = string.IsNullOrWhiteSpace(line.Reference) ? null : line.Reference.Trim();

                var voucher = new Voucher
                {
                    SeqNbr = ++voucherSeq,
                    CustomerGkey = invoice.CustGkey,
                    VoucherDate = now,
                    TransType = "Receipt",
                    VoucherType = voucherTypeName,
                    Mode = mode,
                    TransDate = receivedOn,
                    VoucherNbr = voucherNbr,
                    RefDocNbr = invoice.InvNbr,
                    RefDocDate = invoice.InvDate,
                    RefDocGkey = invoice.Gkey,
                    TransDesc = reference is null
                        ? $"{voucherTypeName}-Receipt-{mode}"
                        : $"{voucherTypeName}-Receipt-{mode} / {reference}",
                    TransAmount = line.Amount
                };
                _vouchers.Add(voucher);
                vouchers.Add(voucher);

                _receipts.Add(new InvoiceArReceipt
                {
                    SeqNbr = ++receiptSeq,
                    CustGkey = invoice.CustGkey,
                    InvoiceGkey = invoice.Gkey,
                    InvoiceNbr = invoice.InvNbr,
                    InvoiceReceivableAmount = running,
                    BalBeforeAdj = running,
                    AdjustedAmount = line.Amount,
                    BalanceAfterAdj = running - line.Amount,
                    TransactionType = line.PaymentType.Trim(),
                    ModeOfReceipt = InvoiceCollectionRules.ModeOfReceipt(line.PaymentType),
                    InternalVoucherNbr = voucherNbr,
                    InternalVoucherDate = now,
                    ExternalTransactionId = reference,
                    ExternalTransactionDate = reference is null ? null : receivedOn,
                    InvoiceReceiptNbr = invoice.InvNbr?.Replace("B", "R"),
                    Status = "Adj"
                });

                running -= line.Amount;
            }

            var total = request.Lines.Sum(x => x.Amount);
            invoice.InvBalance = running;
            invoice.RecdAmount = invoice.RecdAmount.GetValueOrDefault() + total;
            invoice.ModifiedOn = now;
            _invoices.Update(invoice);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Each line reaches FinanceTracker on the day it was received.
            if (_financeSyncOptions.Enabled && _financeSyncService is not null)
            {
                foreach (var voucher in vouchers)
                {
                    await _financeSyncService.QueueCollectionSyncAsync(voucher.Gkey,
                        _financeSyncOptions.OrgGkey, _financeSyncOptions.LocationGkey, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);

            return new SaveInvoiceCollectionResponse
            {
                InvNbr = invoice.InvNbr ?? string.Empty,
                RecdAmount = invoice.RecdAmount.GetValueOrDefault(),
                InvBalance = running,
                VoucherNumbers = vouchers.Select(x => x.VoucherNbr ?? string.Empty).ToList()
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
