using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Inventory;
using InvEntry.Contracts.Invoices;
using DataAccess.Inventory.ProductStock;

namespace DataAccess.Workflows;

public sealed class InvoiceWorkflow : IInvoiceWorkflow
{
    private readonly IRepositoryBase<InvoiceHeader> _invoiceRepository;
    private readonly IRepositoryBase<InvoiceLine> _lineRepository;
    private readonly IRepositoryBase<InvoiceArReceipt> _receiptRepository;
    private readonly IRepositoryBase<OldMetalTransaction> _oldMetalRepository;
    private readonly IRepositoryBase<VoucherType> _voucherTypeRepository;
    private readonly IRepositoryBase<Voucher> _voucherRepository;
    //private readonly IRepositoryBase<ProductStock> _productStockRepository;
    //private readonly IRepositoryBase<ProductStockSummary> _productStockSummaryRepository;
    //private readonly IRepositoryBase<ProductTransaction> _productTransactionRepository;
    private readonly IStockMovementService _stockMovementService;
    private readonly IUnitOfWork _unitOfWork;

    //IRepositoryBase<ProductStock> productStockRepository,
    //IRepositoryBase<ProductStockSummary> productStockSummaryRepository,
    //IRepositoryBase<ProductTransaction> productTransactionRepository,

    public InvoiceWorkflow(
        IRepositoryBase<InvoiceHeader> invoiceRepository,
        IRepositoryBase<InvoiceLine> lineRepository,
        IRepositoryBase<InvoiceArReceipt> receiptRepository,
        IRepositoryBase<OldMetalTransaction> oldMetalRepository,
        IRepositoryBase<VoucherType> voucherTypeRepository,
        IRepositoryBase<Voucher> voucherRepository,

        IStockMovementService stockMovementService,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _lineRepository = lineRepository;
        _receiptRepository = receiptRepository;
        _oldMetalRepository = oldMetalRepository;
        _voucherTypeRepository = voucherTypeRepository;
        _voucherRepository = voucherRepository;

      //  _productStockRepository = productStockRepository;
      //  _productStockSummaryRepository = productStockSummaryRepository;
      //  _productTransactionRepository = productTransactionRepository;

        _stockMovementService =  stockMovementService;

        _unitOfWork = unitOfWork;
    }

    // =========================================================
    // SAVE DRAFT
    // =========================================================

    public async Task<SaveInvoiceResponse> SaveDraftAsync(
        SaveInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Header);

        if (request.Lines == null ||
            request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Invoice must contain at least one line.");
        }

        var isNew =
            request.Header.Gkey <= 0;

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

        try
        {
            InvoiceHeader invoice;

            if (isNew)
            {
                invoice = await CreateDraftHeaderAsync(
                    request.Header,
                    cancellationToken);
            }
            else
            {
                invoice = UpdateDraftHeader(
                    request.Header);
            }

            /*
             * A new Invoice needs its database-generated Gkey
             * before children can be attached.
             */
            if (isNew)
            {
                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }

            SyncLines(
                invoice,
                request.Lines);

            SyncOldMetal(
                invoice,
                request.OldMetalTransactions ??
                    new List<InvoiceOldMetalSaveModel>());

/*            SyncReceipts(
                invoice,
                request.Receipts ??
                    new List<InvoiceReceiptSaveModel>());*/

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new SaveInvoiceResponse
            {
                Gkey = invoice.Gkey,

                /*
                 * Draft deliberately has no final invoice number.
                 */
                InvNbr = invoice.InvNbr ??
                         string.Empty,

                IsNew = isNew,

                Status = invoice.Status
            };
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private void PostInvoiceStock(
        InvoiceHeader invoice,
        IReadOnlyCollection<InvoiceLine> lines)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvNbr))
        {
            throw new InvalidOperationException(
                "Official invoice number must be generated before stock is posted.");
        }

        if (lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Invoice contains no lines to post to stock.");
        }

        var requests = lines
            .Select(line =>
                new StockMovementRequest
                {
                    // ---------------------------------------------
                    // Source document
                    // ---------------------------------------------

                    DocumentGkey =
                        invoice.Gkey,

                    DocumentLineGkey =
                        line.Gkey,

                    DocumentNumber =
                        invoice.InvNbr,

                    DocumentDate =
                        invoice.InvDate ?? DateTime.Now,

                    DocumentType =
                        "SALE_INVOICE",


                    // ---------------------------------------------
                    // Product
                    // ---------------------------------------------

                    ProductGkey =
                        line.ProductGkey.GetValueOrDefault(),

                    ProductSku =
                        string.IsNullOrWhiteSpace(line.ProductSku)
                            ? null
                            : line.ProductSku,

                    ProductCategory =
                        line.ProdCategory,


                    // ---------------------------------------------
                    // Movement
                    // ---------------------------------------------

                    Direction =
                        StockMovementDirection.Out,

                    Purpose =
                        StockMovementPurpose.Sale,


                    // ---------------------------------------------
                    // Quantity / Weight
                    // ---------------------------------------------

                    Quantity =
                        line.ProdQty,

                    GrossWeight =
                        line.ProdGrossWeight.GetValueOrDefault(),

                    StoneWeight =
                        line.ProdStoneWeight.GetValueOrDefault(),

                    NetWeight =
                        line.ProdNetWeight.GetValueOrDefault(),


                    // ---------------------------------------------
                    // Commercial
                    // ---------------------------------------------

                    UnitPrice =
                        line.InvlBilledPrice,

                    TransactionValue =
                        line.InvlPayableAmt,


                    // ---------------------------------------------
                    // Reference
                    // ---------------------------------------------

                    Reason =
                        "Invoice Sale",

                    Notes =
                        $"Invoice {invoice.InvNbr}"
                })
            .ToList();


        _stockMovementService.PostMovements(
            requests);
    }

    private void CreateSettlementRecords(
        InvoiceHeader invoice,
        FinaliseInvoiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvNbr))
        {
            throw new InvalidOperationException(
                "Official invoice number must be generated before settlement records are created.");
        }

        var sequenceNumber = 1;

        var invoiceReceivableAmount =
            invoice.AmountPayable.GetValueOrDefault();

        var runningBalance =
            invoiceReceivableAmount;


        // =========================================================
        // RECEIPTS / ADJUSTMENTS
        // Customer -> Shop
        // =========================================================

        foreach (var settlement in
                 request.Receipts ??
                 new List<InvoiceSettlementSaveModel>())
        {
            var balanceBefore =
                runningBalance;

            runningBalance -=
                settlement.Amount;

            // Avoid tiny decimal/rounding residue.
            if (Math.Abs(runningBalance) <= 0.01M)
            {
                runningBalance = 0M;
            }

            var voucher =
                CreateSettlementVoucher(
                    invoice,
                    settlement,
                    sequenceNumber,
                    isRefund: false);

            _voucherRepository.Add(voucher);

            var receipt =
                CreateInvoiceArReceipt(
                    invoice,
                    settlement,
                    sequenceNumber,
                    balanceBefore,
                    runningBalance);

            _receiptRepository.Add(receipt);

            sequenceNumber++;
        }


        // =========================================================
        // CREDIT
        //
        // Credit is NOT an adjustment/receipt.
        // It represents the amount still outstanding.
        //
        // Therefore DO NOT subtract CreditAmount from runningBalance.
        // =========================================================

        if (request.CreditAmount > 0M)
        {
            var creditSettlement =
                new InvoiceSettlementSaveModel
                {
                    SettlementType =
                        InvoiceSettlementType.Receipt,

                    PaymentMode =
                        "Credit",

                    Amount =
                        request.CreditAmount,

                    TransactionDate =
                        DateTime.Now
                };

            var voucher =
                CreateCreditVoucher(
                    invoice,
                    creditSettlement,
                    sequenceNumber);

            _voucherRepository.Add(voucher);

            var creditReceipt =
                CreateCreditArReceipt(
                    invoice,
                    creditSettlement,
                    sequenceNumber,
                    runningBalance);

            _receiptRepository.Add(creditReceipt);

            sequenceNumber++;
        }


        // =========================================================
        // REFUNDS
        // Shop -> Customer
        // =========================================================

        foreach (var settlement in
                 request.Refunds ??
                 new List<InvoiceSettlementSaveModel>())
        {
            var voucher =
                CreateSettlementVoucher(
                    invoice,
                    settlement,
                    sequenceNumber,
                    isRefund: true);

            _voucherRepository.Add(voucher);

            var refundReceipt =
                CreateRefundArReceipt(
                    invoice,
                    settlement,
                    sequenceNumber);

            _receiptRepository.Add(refundReceipt);

            sequenceNumber++;
        }


        // =========================================================
        // FINAL HEADER RECEIPT / BALANCE
        // =========================================================

        var totalCashReceived =
            request.Receipts?
                .Where(x =>
                    !IsAdjustmentMode(x.PaymentMode))
                .Sum(x => x.Amount)
            ?? 0M;

        invoice.RecdAmount =
            totalCashReceived;

        invoice.InvBalance =
            runningBalance;
    }


    private static Voucher CreateSettlementVoucher(
    InvoiceHeader invoice,
    InvoiceSettlementSaveModel settlement,
    int sequenceNumber,
    bool isRefund)
    {
        var transactionDate =
            settlement.TransactionDate ??
            DateTime.Now;

        var isAdjustment =
            IsAdjustmentMode(
                settlement.PaymentMode);

        return new Voucher
        {
            SeqNbr =
                sequenceNumber,

            CustomerGkey =
                invoice.CustGkey,

            TransType =
                isRefund
                    ? "Payment"
                    : isAdjustment
                        ? "Journal"
                        : "Receipt",

            VoucherType =
                isRefund
                    ? "Refund"
                    : settlement.PaymentMode,

            Mode =
                settlement.PaymentMode,

            TransAmount =
                settlement.Amount,

            VoucherNbr =
                invoice.InvNbr,

            VoucherDate =
                invoice.InvDate,

            RefDocGkey =
                invoice.Gkey,

            RefDocNbr =
                invoice.InvNbr,

            RefDocDate =
                invoice.InvDate,

            TransDesc =
                BuildSettlementDescription(
                    settlement,
                    isRefund),

            TransDate =
                transactionDate
        };
    }


    private static string BuildSettlementDescription(
    InvoiceSettlementSaveModel settlement,
    bool isRefund)
    {
        var parts = new List<string>
    {
        isRefund
            ? "Invoice Refund"
            : "Invoice Receipt",

        settlement.PaymentMode
    };

        if (!string.IsNullOrWhiteSpace(
                settlement.TransactionId))
        {
            parts.Add(
                settlement.TransactionId);
        }

        if (!string.IsNullOrWhiteSpace(
                settlement.InstrumentNumber))
        {
            parts.Add(
                settlement.InstrumentNumber);
        }

        if (!string.IsNullOrWhiteSpace(
                settlement.BankName))
        {
            parts.Add(
                settlement.BankName);
        }

        if (!string.IsNullOrWhiteSpace(
                settlement.OtherReference))
        {
            parts.Add(
                settlement.OtherReference);
        }

        return string.Join(
            " / ",
            parts.Where(
                x => !string.IsNullOrWhiteSpace(x)));
    }

    private static InvoiceArReceipt CreateInvoiceArReceipt(
        InvoiceHeader invoice,
        InvoiceSettlementSaveModel settlement,
        int sequenceNumber,
        decimal balanceBefore,
        decimal balanceAfter)
    {
        return new InvoiceArReceipt
        {
            SeqNbr =
                sequenceNumber,

            CustGkey =
                invoice.CustGkey,

            InvoiceGkey =
                invoice.Gkey,

            InvoiceNbr =
                invoice.InvNbr,

            InvoiceReceivableAmount =
                invoice.AmountPayable,

            BalBeforeAdj =
                balanceBefore,

            AdjustedAmount =
                settlement.Amount,

            BalanceAfterAdj =
                balanceAfter,

            TransactionType =
                settlement.PaymentMode,

            ModeOfReceipt =
                settlement.PaymentMode,

            InternalVoucherNbr =
                invoice.InvNbr,

            InternalVoucherDate =
                invoice.InvDate,

            InvoiceReceiptNbr =
                GenerateReceiptReference(
                    invoice.InvNbr),

            Status =
                balanceAfter <= 0.01M
                    ? "Adj"
                    : "Partial",

            BankName =
                settlement.BankName,

            ExternalTransactionId =
                settlement.TransactionId,

            ExternalTransactionDate =
                settlement.TransactionDate ??
                DateTime.Now,

            OtherReference =
                GetSettlementReference(
                    settlement),

            CompanyBankAccountNbr =
                settlement.CompanyBankAccountNbr
        };
    }

    private static string GenerateReceiptReference(
    string? invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return string.Empty;

        if (invoiceNumber.StartsWith(
                "B",
                StringComparison.OrdinalIgnoreCase))
        {
            return "R" +
                   invoiceNumber.Substring(1);
        }

        return $"R-{invoiceNumber}";
    }

    private static Voucher CreateCreditVoucher(
    InvoiceHeader invoice,
    InvoiceSettlementSaveModel settlement,
    int sequenceNumber)
    {
        return new Voucher
        {
            SeqNbr =
                sequenceNumber,

            CustomerGkey =
                invoice.CustGkey,

            TransType =
                "Journal",

            VoucherType =
                "Credit",

            Mode =
                "Credit",

            TransAmount =
                settlement.Amount,

            VoucherNbr =
                invoice.InvNbr,

            VoucherDate =
                invoice.InvDate,

            RefDocGkey =
                invoice.Gkey,

            RefDocNbr =
                invoice.InvNbr,

            RefDocDate =
                invoice.InvDate,

            TransDesc =
                "Invoice Credit / Customer Receivable",

            TransDate =
                settlement.TransactionDate ??
                DateTime.Now
        };
    }

    private static InvoiceArReceipt CreateCreditArReceipt(
        InvoiceHeader invoice,
        InvoiceSettlementSaveModel settlement,
        int sequenceNumber,
        decimal outstandingBalance)
    {
        return new InvoiceArReceipt
        {
            SeqNbr =
                sequenceNumber,

            CustGkey =
                invoice.CustGkey,

            InvoiceGkey =
                invoice.Gkey,

            InvoiceNbr =
                invoice.InvNbr,

            InvoiceReceivableAmount =
                invoice.AmountPayable,

            TransactionType =
                "Credit",

            ModeOfReceipt =
                "Credit",

            // Credit is the outstanding balance,
            // NOT money received.
            BalBeforeAdj =
                outstandingBalance,

            AdjustedAmount =
                0M,

            BalanceAfterAdj =
                outstandingBalance,

            InternalVoucherNbr =
                invoice.InvNbr,

            InternalVoucherDate =
                invoice.InvDate,

            InvoiceReceiptNbr =
                GenerateReceiptReference(
                    invoice.InvNbr),

            Status =
                "Outstanding",

            ExternalTransactionDate =
                settlement.TransactionDate ??
                DateTime.Now
        };
    }

    private static InvoiceArReceipt CreateRefundArReceipt(
    InvoiceHeader invoice,
    InvoiceSettlementSaveModel settlement,
    int sequenceNumber)
    {
        return new InvoiceArReceipt
        {
            SeqNbr =
                sequenceNumber,

            CustGkey =
                invoice.CustGkey,

            InvoiceGkey =
                invoice.Gkey,

            InvoiceNbr =
                invoice.InvNbr,

            InvoiceReceivableAmount =
                invoice.AmountPayable,

            TransactionType =
                "Refund",

            ModeOfReceipt =
                settlement.PaymentMode,

            AdjustedAmount =
                settlement.Amount,

            InternalVoucherNbr =
                invoice.InvNbr,

            InternalVoucherDate =
                invoice.InvDate,

            InvoiceReceiptNbr =
                GenerateReceiptReference(
                    invoice.InvNbr),

            Status =
                "Refund",

            BankName =
                settlement.BankName,

            ExternalTransactionId =
                settlement.TransactionId,

            ExternalTransactionDate =
                settlement.TransactionDate ??
                DateTime.Now,

            OtherReference =
                GetSettlementReference(
                    settlement),

            CompanyBankAccountNbr =
                settlement.CompanyBankAccountNbr
        };
    }

    private static string? GetSettlementReference(
        InvoiceSettlementSaveModel settlement)
    {
        if (!string.IsNullOrWhiteSpace(settlement.InstrumentNumber))
            return settlement.InstrumentNumber;

        return string.IsNullOrWhiteSpace(settlement.OtherReference)
            ? null
            : settlement.OtherReference;
    }

    public Task<InvoiceEditResponse> GetForEditAsync(
    int invoiceGkey,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (invoiceGkey <= 0)
            throw new ArgumentException(
                "A valid invoice GKey is required.",
                nameof(invoiceGkey));

        var invoice =
            _invoiceRepository.Get(
                x => x.Gkey == invoiceGkey);

        if (invoice == null)
        {
            throw new KeyNotFoundException(
                $"Invoice GKey {invoiceGkey} was not found.");
        }

        /*
         * For the current Edit Invoice workflow,
         * only DRAFT invoices are editable.
         */
        if (!InvoiceStatus.IsDraft(invoice.Status))
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceGkey} cannot be edited because " +
                $"its status is '{invoice.Status}'.");
        }

        var lines =
            _lineRepository
                .GetList(
                    x => x.InvoiceHdrGkey == invoiceGkey)
                .OrderBy(x => x.InvLineNbr)
                .ToList();

        var oldMetal =
            _oldMetalRepository
                .GetList(
                    x => x.DocRefGkey == invoiceGkey)
                .OrderBy(x => x.Gkey)
                .ToList();

/*        var receipts =
            _receiptRepository
                .GetList(
                    x => x.InvoiceGkey == invoiceGkey)
                .OrderBy(x => x.SeqNbr)
                .ToList();*/

        var response = new InvoiceEditResponse
        {
            Header = MapHeaderToContract(invoice),

            Lines = lines
                .Select(MapLineToContract)
                .ToList(),

            OldMetalTransactions = oldMetal
                .Select(MapOldMetalToContract)  
                .ToList(),

            Receipts =
                new List<InvoiceReceiptSaveModel>()

        };

        return Task.FromResult(response);
    }

    // =========================================================
    // CREATE DRAFT HEADER
    // =========================================================

    private Task<InvoiceHeader> CreateDraftHeaderAsync(
        InvoiceHeaderSaveModel source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var invoice =
            new InvoiceHeader();

        MapHeader(
            source,
            invoice);

        /*
         * These values MUST be controlled by the backend.
         *
         * Never trust Status or InvNbr sent by WPF.
         */
        invoice.InvNbr = null;
        invoice.Status = InvoiceStatus.Draft;

        invoice.FinalisedOn = null;

        invoice.CreatedOn =
            DateTime.Now;

        invoice.ModifiedOn =
            null;

        _invoiceRepository.Add(
            invoice);

        return Task.FromResult(invoice);
    }

    // =========================================================
    // UPDATE EXISTING DRAFT
    // =========================================================

    private InvoiceHeader UpdateDraftHeader(
        InvoiceHeaderSaveModel source)
    {
        var invoice =
            _invoiceRepository.Get(
                x => x.Gkey == source.Gkey);

        if (invoice == null)
        {
            throw new InvalidOperationException(
                $"Invoice draft GKey {source.Gkey} was not found.");
        }

        /*
         * FINAL and CANCELLED documents are immutable through
         * SaveDraft.
         */
        if (!InvoiceStatus.IsDraft(
                invoice.Status))
        {
            throw new InvalidOperationException(
                $"Invoice cannot be modified because its " +
                $"current status is '{invoice.Status}'.");
        }

        MapHeader(
            source,
            invoice);

        /*
         * Protect workflow-controlled fields.
         */
        invoice.Status =
            InvoiceStatus.Draft;

        invoice.ModifiedOn =
            DateTime.Now;

        _invoiceRepository.Update(
            invoice);

        return invoice;
    }

    // =========================================================
    // SYNC LINES
    // =========================================================

    private void SyncLines(
        InvoiceHeader invoice,
        IReadOnlyCollection<InvoiceLineSaveModel> incomingLines)
    {
        var existingLines =
            _lineRepository
                .GetList(
                    x => x.InvoiceHdrGkey ==
                         invoice.Gkey)
                .ToList();

        var incomingExistingKeys =
            incomingLines
                .Where(x => x.Gkey > 0)
                .Select(x => x.Gkey)
                .ToHashSet();

        // -----------------------------------------------------
        // Delete lines removed from the Draft
        // -----------------------------------------------------

        foreach (var existing in existingLines)
        {
            if (!incomingExistingKeys.Contains(
                    existing.Gkey))
            {
                _lineRepository.Remove(
                    existing);
            }
        }

        // -----------------------------------------------------
        // Add/update current lines
        // -----------------------------------------------------

        var lineNumber = 1;

        foreach (var source in incomingLines)
        {
            InvoiceLine line;

            if (source.Gkey > 0)
            {
                line =
                    existingLines.FirstOrDefault(
                        x => x.Gkey ==
                             source.Gkey)
                    ?? throw new InvalidOperationException(
                        $"Invoice line GKey {source.Gkey} " +
                        $"does not belong to Draft " +
                        $"{invoice.Gkey}.");

                MapLine(
                    source,
                    line);

                line.ModifiedOn =
                    DateTime.Now;

                _lineRepository.Update(
                    line);
            }
            else
            {
                line =
                    new InvoiceLine();

                MapLine(
                    source,
                    line);

                line.CreatedOn =
                    DateTime.Now;

                line.ModifiedOn =
                    null;

                _lineRepository.Add(
                    line);
            }

            /*
             * Do NOT trust these references from WPF.
             */
            line.InvoiceHdrGkey =
                invoice.Gkey;

            line.InvoiceId =
                null;

            line.InvLineNbr =
                lineNumber++;

            line.TenantGkey =
                invoice.TenantGkey ??
                source.TenantGkey;
        }
    }

    // =========================================================
    // SYNC OLD METAL
    // =========================================================

    private void SyncOldMetal(
        InvoiceHeader invoice,
        IReadOnlyCollection<InvoiceOldMetalSaveModel> incomingItems)
    {
        var existingItems =
            _oldMetalRepository
                .GetList(
                    x => x.DocRefGkey ==
                         invoice.Gkey)
                .ToList();

        var incomingExistingKeys =
            incomingItems
                .Where(x => x.Gkey > 0)
                .Select(x => x.Gkey)
                .ToHashSet();

        foreach (var existing in existingItems)
        {
            if (!incomingExistingKeys.Contains(
                    existing.Gkey))
            {
                _oldMetalRepository.Remove(
                    existing);
            }
        }

        foreach (var source in incomingItems)
        {
            OldMetalTransaction item;

            if (source.Gkey > 0)
            {
                item =
                    existingItems.FirstOrDefault(
                        x => x.Gkey ==
                             source.Gkey)
                    ?? throw new InvalidOperationException(
                        $"Old metal transaction GKey " +
                        $"{source.Gkey} does not belong to " +
                        $"Draft {invoice.Gkey}.");

                MapOldMetal(
                    source,
                    item);

                _oldMetalRepository.Update(
                    item);
            }
            else
            {
                item =
                    new OldMetalTransaction();

                MapOldMetal(
                    source,
                    item);

                _oldMetalRepository.Add(
                    item);
            }

            /*
             * Draft is linked by Gkey.
             * Final invoice number does not exist yet.
             */
            item.DocRefGkey =
                invoice.Gkey;

            item.DocRefNbr =
                null;

            item.DocRefDate =
                invoice.InvDate;

            item.DocRefType =
                "Sale Invoice";

            item.CustGkey =
                invoice.CustGkey;

            item.CustMobile =
                invoice.CustMobile;
        }
    }

    // =========================================================
    // SYNC RECEIPTS / PAYMENT DETAILS
    // =========================================================

    private void SyncReceipts(
        InvoiceHeader invoice,
        IReadOnlyCollection<InvoiceReceiptSaveModel> incomingReceipts)
    {
        var existingReceipts =
            _receiptRepository
                .GetList(
                    x => x.InvoiceGkey ==
                         invoice.Gkey)
                .ToList();

        var incomingExistingKeys =
            incomingReceipts
                .Where(x => x.Gkey > 0)
                .Select(x => x.Gkey)
                .ToHashSet();

        foreach (var existing in existingReceipts)
        {
            if (!incomingExistingKeys.Contains(
                    existing.Gkey))
            {
                _receiptRepository.Remove(
                    existing);
            }
        }

        var sequenceNumber = 1;

        foreach (var source in incomingReceipts)
        {
            InvoiceArReceipt receipt;

            if (source.Gkey > 0)
            {
                receipt =
                    existingReceipts.FirstOrDefault(
                        x => x.Gkey ==
                             source.Gkey)
                    ?? throw new InvalidOperationException(
                        $"Invoice receipt GKey " +
                        $"{source.Gkey} does not belong to " +
                        $"Draft {invoice.Gkey}.");

                MapReceipt(
                    source,
                    receipt);

                _receiptRepository.Update(
                    receipt);
            }
            else
            {
                receipt =
                    new InvoiceArReceipt();

                MapReceipt(
                    source,
                    receipt);

                _receiptRepository.Add(
                    receipt);
            }

            receipt.InvoiceGkey =
                invoice.Gkey;

            /*
             * No final InvoiceNbr during Draft.
             */
            receipt.InvoiceNbr =
                null;

            receipt.CustGkey =
                invoice.CustGkey;

            receipt.SeqNbr =
                sequenceNumber++;
        }
    }

    // =========================================================
    // HEADER MAPPING
    // =========================================================

    private static void MapHeader(
        InvoiceHeaderSaveModel source,
        InvoiceHeader destination)
    {
        destination.InvDate =
            source.InvDate;

        destination.PaymentMode =
            source.PaymentMode;

        destination.CustGkey =
            source.CustGkey;

        destination.CustMobile =
            source.CustMobile;

        destination.InvRefund =
            source.InvRefund;

        /*
         * Contract:
         * InvlTaxableAmount
         *
         * EF:
         * InvTaxableAmount
         */
        destination.InvTaxableAmount =
            source.InvlTaxableAmount;

        destination.IsTaxApplicable =
            source.IsTaxApplicable;

        destination.OldGoldAmount =
            source.OldGoldAmount;

        destination.OldSilverAmount =
            source.OldSilverAmount;

        destination.TaxType =
            source.TaxType;

        destination.GstLocSeller =
            source.GstLocSeller;

        destination.GstLocBuyer =
            source.GstLocBuyer;

        destination.CgstPercent =
            source.CgstPercent;

        destination.CgstAmount =
            source.CgstAmount;

        destination.SgstPercent =
            source.SgstPercent;

        destination.SgstAmount =
            source.SgstAmount;

        destination.IgstPercent =
            source.IgstPercent;

        destination.IgstAmount =
            source.IgstAmount;

        destination.AmountPayable =
            source.AmountPayable;

        destination.DiscountPercent =
            source.DiscountPercent;

        destination.DiscountAmount =
            source.DiscountAmount;

        destination.AdvanceAdj =
            source.AdvanceAdj;

        destination.PaymentDueDate =
            source.PaymentDueDate;

        destination.RdAmountAdj =
            source.RdAmountAdj;

        destination.RecdAmount =
            source.RecdAmount;

        destination.InvBalance =
            source.InvBalance;

        destination.RoundOff =
            source.RoundOff;

        destination.InvNotes =
            source.InvNotes;

        destination.TenantGkey =
            source.TenantGkey;

        destination.GrossRcbAmount =
            source.GrossRcbAmount;

        destination.InvlTaxTotal =
            source.InvlTaxTotal;

        destination.SalesPerson =
            source.SalesPerson;

        /*
         * Do NOT map:
         *
         * Gkey
         * InvNbr
         * Status
         * CreatedOn
         * ModifiedOn
         *
         * These are controlled by backend workflow.
         *
         * PlaceOfSeller / PlaceOfSupply are also currently not
         * present in your EF InvoiceHeader model.
         */
    }

    // =========================================================
    // LINE MAPPING
    // =========================================================

    private static void MapLine(
        InvoiceLineSaveModel source,
        InvoiceLine destination)
    {
        destination.HsnCode =
            source.HsnCode;

        destination.InvNote =
            source.InvNote;

        destination.InvlBilledPrice =
            source.InvlBilledPrice;

        destination.InvlGrossAmt =
            source.InvlGrossAmt;

        destination.InvlMakingCharges =
            source.InvlMakingCharges;

        destination.InvlOtherCharges =
            source.InvlOtherCharges;

        destination.InvlPayableAmt =
            source.InvlPayableAmt;

        destination.InvlStoneAmount =
            source.InvlStoneAmount;

        destination.InvlTaxableAmount =
            source.InvlTaxableAmount;

        destination.InvlWastageAmt =
            source.InvlWastageAmt;

        destination.IsTaxable =
            source.IsTaxable;

        destination.ItemNotes =
            source.ItemNotes;

        /*
         * Contract/WPF currently int?.
         * EF currently bool?.
         */
        destination.ItemPacked =
            source.ItemPacked.HasValue
                ? source.ItemPacked.Value != 0
                : null;

        destination.ProdCategory =
            source.ProdCategory;

        destination.ProdGrossWeight =
            source.ProdGrossWeight;

        destination.ProdNetWeight =
            source.ProdNetWeight;

        destination.ProdQty =
            source.ProdQty ?? 0;

        destination.ProdStoneWeight =
            source.ProdStoneWeight;

        destination.ProductDesc =
            source.ProductDesc;

        destination.ProductGkey =
            source.ProductGkey;

        destination.ProductName =
            source.ProductName;

        destination.ProdPackCode =
            source.ProdPackCode;

        destination.ProductPurity =
            source.ProductPurity;

        destination.TaxAmount =
            source.TaxAmount;

        destination.TaxPercent =
            source.TaxPercent;

        destination.TaxType =
            source.TaxType;

        destination.VaAmount =
            source.VaAmount;

        destination.VaPercent =
            source.VaPercent;

        destination.InvlCgstPercent =
            source.InvlCgstPercent;

        destination.InvlCgstAmount =
            source.InvlCgstAmount;

        destination.InvlIgstPercent =
            source.InvlIgstPercent;

        destination.InvlIgstAmount =
            source.InvlIgstAmount;

        destination.InvlTotal =
            source.InvlTotal;

        destination.InvlSgstAmount =
            source.InvlSgstAmount;

        destination.InvlSgstPercent =
            source.InvlSgstPercent;

        destination.ProductId =
            source.ProductId;

        destination.Metal =
            source.Metal;

        destination.ProductSku =
            source.ProductSku;
    }

    // =========================================================
    // OLD METAL MAPPING
    // =========================================================

    private static void MapOldMetal(
        InvoiceOldMetalSaveModel source,
        OldMetalTransaction destination)
    {
        destination.TransNbr =
            source.TransNbr;

        destination.TransDate =
            source.TransDate;

        destination.TransType =
            source.TransType;

        destination.ProductGkey =
            source.ProductGkey;

        destination.ProductId =
            source.ProductId;

        destination.ProductCategory =
            source.ProductCategory;

        destination.Metal =
            source.Metal;

        destination.Purity =
            source.Purity;

        destination.TransactedRate =
            source.TransactedRate;

        destination.Uom =
            source.Uom;

        destination.GrossWeight =
            source.GrossWeight;

        destination.StoneWeight =
            source.StoneWeight;

        destination.WastagePercent =
            source.WastagePercent;

        destination.WastageWeight =
            source.WastageWeight;

        destination.NetWeight =
            source.NetWeight;

        destination.TotalProposedPrice =
            source.TotalProposedPrice;

        destination.FinalPurchasePrice =
            source.FinalPurchasePrice;

        destination.Remarks =
            source.Remarks;
    }

    // =========================================================
    // RECEIPT MAPPING
    // =========================================================

    private static void MapReceipt(
        InvoiceReceiptSaveModel source,
        InvoiceArReceipt destination)
    {
        destination.InvoiceReceivableAmount =
            source.InvoiceReceivableAmount;

        destination.BalanceAfterAdj =
            source.BalanceAfterAdj;

        destination.TransactionType =
            source.TransactionType;

        destination.ModeOfReceipt =
            source.ModeOfReceipt;

        destination.BalBeforeAdj =
            source.BalBeforeAdj;

        destination.AdjustedAmount =
            source.AdjustedAmount;

        destination.InternalVoucherNbr =
            source.InternalVoucherNbr;

        destination.InternalVoucherDate =
            source.InternalVoucherDate;

        destination.ExternalTransactionId =
            source.ExternalTransactionId;

        destination.ExternalTransactionDate =
            source.ExternalTransactionDate;

        destination.BankName =
            source.BankName;

        destination.OtherReference =
            source.OtherReference;

        destination.SenderBankAccountNbr =
            source.SenderBankAccountNbr;

        destination.SenderBankGkey =
            source.SenderBankGkey;

        destination.SenderBankBranch =
            source.SenderBankBranch;

        destination.SenderBankIfscCode =
            source.SenderBankIfscCode;

        destination.CompanyBankAccountNbr =
            source.CompanyBankAccountNbr;

        destination.Status =
            source.Status;

        destination.InvoiceReceiptNbr =
            source.InvoiceReceiptNbr;
    }

    // =========================================================
    // FINALISE - NEXT STAGE
    // =========================================================

    public async Task<FinaliseInvoiceResponse> FinaliseAsync(
        FinaliseInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(request);

        var invoiceGkey = request.InvoiceGkey;

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var invoice =
                _invoiceRepository.Get(
                    x => x.Gkey == invoiceGkey);

            if (invoice == null)
                throw new KeyNotFoundException(
                    $"Invoice GKey {invoiceGkey} was not found.");

            if (InvoiceStatus.IsFinal(invoice.Status))
            {
                return new FinaliseInvoiceResponse
                {
                    Gkey = invoice.Gkey,
                    InvNbr = invoice.InvNbr ?? string.Empty,
                    Status = invoice.Status,
                    FinalisedOn = invoice.FinalisedOn
                };
            }

            if (!InvoiceStatus.IsDraft(invoice.Status))
            {
                throw new InvalidOperationException(
                    $"Invoice cannot be finalised because its status is '{invoice.Status}'.");
            }

            var lines =
                _lineRepository
                    .GetList(
                        x => x.InvoiceHdrGkey == invoiceGkey)
                    .ToList();

            if (lines.Count == 0)
            {
                throw new InvalidOperationException(
                    "Invoice must contain at least one line before finalisation.");
            }


            // ---------------------------------------------------------
            // SETTLEMENT VALIDATION
            // ---------------------------------------------------------

            const decimal settlementTolerance = 0.01M;

            // =========================================================
            // DISCOUNT
            // =========================================================
            //
            // AmountPayable stored on the Draft already includes the
            // Draft's existing DiscountAmount.
            //
            // Recover the pre-discount settlement amount first, then
            // apply the discount submitted during Finalisation.
            //
            // Example:
            //
            // Draft:
            //     AmountPayable   = 9,500
            //     DiscountAmount  =   500
            //
            // Pre-discount amount = 10,000
            //
            // Settlement changes discount to 750:
            //
            //     Final payable   = 10,000 - 750
            //                     =  9,250
            //
            // The backend remains authoritative. We do NOT accept a
            // payable/net amount calculated by WPF.
            //

            var existingDiscount =
                invoice.DiscountAmount.GetValueOrDefault();

            var requestedDiscount =
                request.DiscountAmount;


            // ---------------------------------------------------------
            // Validate discount
            // ---------------------------------------------------------

            if (requestedDiscount < 0M)
            {
                throw new InvalidOperationException(
                    "Discount amount cannot be negative.");
            }


            // Recover the amount before the Draft discount was applied.
            var preDiscountSettlementAmount =
                invoice.AmountPayable.GetValueOrDefault()
                + existingDiscount;


            // A discount is meaningful only against a positive
            // receivable. Do not allow it to manufacture/increase
            // a refund to the customer.
            if (requestedDiscount > 0M &&
                preDiscountSettlementAmount <= settlementTolerance)
            {
                throw new InvalidOperationException(
                    "Discount cannot be applied because there is no " +
                    "positive invoice amount available for discount.");
            }


            // Discount cannot exceed the amount that the customer
            // would otherwise owe.
            if (requestedDiscount >
                preDiscountSettlementAmount + settlementTolerance)
            {
                throw new InvalidOperationException(
                    $"Discount amount ₹{requestedDiscount:N2} exceeds " +
                    $"the amount available for discount " +
                    $"₹{preDiscountSettlementAmount:N2}.");
            }


            // ---------------------------------------------------------
            // AUTHORITATIVE FINAL AMOUNT
            // ---------------------------------------------------------

            var netSettlementAmount =
                preDiscountSettlementAmount
                - requestedDiscount;


            // Normalise tiny rounding differences.
            if (Math.Abs(netSettlementAmount) <= settlementTolerance)
            {
                netSettlementAmount = 0M;
            }


            // Persist the final commercial values on the Invoice.
            invoice.DiscountAmount =
                requestedDiscount;

            invoice.AmountPayable =
                netSettlementAmount;


            // Draft has no received amount. The actual settlement
            // records below represent the final receipts/refunds.
            invoice.RecdAmount = 0M;


            var receiptAmount =
                request.Receipts?
                    .Where(x => x.Amount > 0M)
                    .Sum(x => x.Amount)
                ?? 0M;

            var refundAmount =
                request.Refunds?
                    .Where(x => x.Amount > 0M)
                    .Sum(x => x.Amount)
                ?? 0M;

            var creditAmount =
                request.CreditAmount;


            // ---------------------------------------------------------
            // BASIC AMOUNT VALIDATION
            // ---------------------------------------------------------

            if (request.Receipts?.Any(x => x.Amount <= 0M) == true)
            {
                throw new InvalidOperationException(
                    "Receipt amount must be greater than zero.");
            }

            if (request.Refunds?.Any(x => x.Amount <= 0M) == true)
            {
                throw new InvalidOperationException(
                    "Refund amount must be greater than zero.");
            }

            if (creditAmount < 0M)
            {
                throw new InvalidOperationException(
                    "Credit amount cannot be negative.");
            }


            // ---------------------------------------------------------
            // SETTLEMENT TYPE VALIDATION
            // ---------------------------------------------------------

            if (request.Receipts?.Any(
                    x => !string.Equals(
                        x.SettlementType,
                        InvoiceSettlementType.Receipt,
                        StringComparison.OrdinalIgnoreCase)) == true)
            {
                throw new InvalidOperationException(
                    "Invalid settlement type found in receipt entries.");
            }

            if (request.Refunds?.Any(
                    x => !string.Equals(
                        x.SettlementType,
                        InvoiceSettlementType.Refund,
                        StringComparison.OrdinalIgnoreCase)) == true)
            {
                throw new InvalidOperationException(
                    "Invalid settlement type found in refund entries.");
            }


            // ---------------------------------------------------------
            // RECEIVABLE
            // Customer owes shop
            // ---------------------------------------------------------

            if (netSettlementAmount > settlementTolerance)
            {
                if (refundAmount > settlementTolerance)
                {
                    throw new InvalidOperationException(
                        "Refunds are not allowed when the customer owes the shop.");
                }

                var settledAmount =
                    receiptAmount + creditAmount;

                var difference =
                    netSettlementAmount - settledAmount;

                if (Math.Abs(difference) > settlementTolerance)
                {
                    throw new InvalidOperationException(
                        $"Settlement does not match the invoice amount. " +
                        $"Amount payable: {netSettlementAmount:N2}, " +
                        $"Receipts: {receiptAmount:N2}, " +
                        $"Credit: {creditAmount:N2}, " +
                        $"Difference: {difference:N2}.");
                }
            }


            // ---------------------------------------------------------
            // REFUND
            // Shop owes customer
            // ---------------------------------------------------------

            else if (netSettlementAmount < -settlementTolerance)
            {
                var refundPayable =
                    Math.Abs(netSettlementAmount);

                // Explicit user action is mandatory.
                if (refundAmount <= settlementTolerance)
                {
                    throw new InvalidOperationException(
                        $"A refund of ₹{refundPayable:N2} is due to the customer. " +
                        "Enter the refund details before finalising the invoice.");
                }

                if (receiptAmount > settlementTolerance)
                {
                    throw new InvalidOperationException(
                        "Receipts are not allowed when a refund is due to the customer.");
                }

                if (creditAmount > settlementTolerance)
                {
                    throw new InvalidOperationException(
                        "Credit is not allowed when a refund is due to the customer.");
                }

                var difference =
                    refundPayable - refundAmount;

                if (Math.Abs(difference) > settlementTolerance)
                {
                    throw new InvalidOperationException(
                        $"Refund is incomplete. " +
                        $"Refund required: ₹{refundPayable:N2}, " +
                        $"Refund entered: ₹{refundAmount:N2}, " +
                        $"Balance: ₹{difference:N2}.");
                }
            }


            // ---------------------------------------------------------
            // FULLY ADJUSTED
            // ---------------------------------------------------------

            else
            {
                if (receiptAmount > settlementTolerance ||
                    refundAmount > settlementTolerance ||
                    creditAmount > settlementTolerance)
                {
                    throw new InvalidOperationException(
                        "No receipt, refund or credit is allowed because " +
                        "the invoice is fully adjusted.");
                }
            }


            // ---------------------------------------------------------
            // GENERATE OFFICIAL INVOICE NUMBER
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(invoice.InvNbr))
            {
                invoice.InvNbr =
                    await GenerateInvoiceNumberAsync(
                        cancellationToken);
            }

            // ---------------------------------------------------------
            // SAFETY - DO NOT CREATE SETTLEMENT TWICE
            // ---------------------------------------------------------

            var existingSettlementRecords =
                _receiptRepository
                    .GetList(
                        x => x.InvoiceGkey == invoice.Gkey)
                    .ToList();

            if (existingSettlementRecords.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Invoice {invoice.Gkey} already contains settlement records " +
                    "and cannot be finalised again.");
            }

            // -----------
            // STOCK UPDATE
            // -----------
            PostInvoiceStock(
                        invoice,
                        lines);

            // ---------------------------------------------------------
            // CREATE SETTLEMENT RECORDS
            //
            // Creates:
            //   Receipt -> Voucher + InvoiceArReceipt
            //   Credit  -> Voucher + InvoiceArReceipt
            //   Refund  -> Voucher + InvoiceArReceipt
            //
            // Everything is still inside the current DB transaction.
            // ---------------------------------------------------------

            CreateSettlementRecords(
                invoice,
                request);

            // ---------------------------------------------------------
            // FINAL STATUS
            // ---------------------------------------------------------

            invoice.Status =
                InvoiceStatus.Final;

            invoice.FinalisedOn =
                DateTime.Now;

            // ---------------------------------------------------------
            // CHILD DOCUMENT REFERENCES
            // ---------------------------------------------------------

            foreach (var line in lines)
            {
                line.InvoiceId =
                    invoice.InvNbr;
            }

            var oldMetalTransactions =
                _oldMetalRepository
                    .GetList(
                        x => x.DocRefGkey == invoiceGkey)
                    .ToList();

            foreach (var oldMetal in oldMetalTransactions)
            {
                oldMetal.DocRefNbr =
                    invoice.InvNbr;
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new FinaliseInvoiceResponse
            {
                Gkey = invoice.Gkey,
                InvNbr = invoice.InvNbr ?? string.Empty,
                Status = invoice.Status,
                FinalisedOn = invoice.FinalisedOn
            };
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static bool IsCashReceiptMode(
    string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return false;

        return !IsAdjustmentMode(mode) &&
               !IsCreditMode(mode);
    }

    private static bool IsAdjustmentMode(
        string? mode)
    {
        return
            string.Equals(
                mode,
                "Advance Adj",
                StringComparison.OrdinalIgnoreCase) ||

            string.Equals(
                mode,
                "RD Adj",
                StringComparison.OrdinalIgnoreCase); 

    }

    private static bool IsCreditMode(
        string? mode)
    {
        return string.Equals(
            mode,
            "Credit",
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GenerateInvoiceNumberAsync(
    CancellationToken cancellationToken)
    {
        var voucherType =
            _voucherTypeRepository.Get(
                x => x.DocumentType == "Sale Invoice");

        if (voucherType == null)
        {
            throw new InvalidOperationException(
                "Invoice voucher type configuration was not found.");
        }

        var nextNumber =
            voucherType.LastUsedNumber + 1;

        var numberLength =
            voucherType.DocNbrLength ?? 0;

        var prefix =
            voucherType.DocNbrPrefix ?? string.Empty;

        var numericPart =
            nextNumber
                .ToString()
                .PadLeft(
                    numberLength,
                    '0');

        voucherType.LastUsedNumber =
            nextNumber;

        return prefix + numericPart;
    }


    // =========================================================
    // CANCEL - NEXT STAGE
    // =========================================================

    public Task CancelDraftAsync(
        int invoiceGkey,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "Draft cancellation will be implemented after " +
            "Draft save/reload is verified.");
    }

    private static InvoiceHeaderSaveModel MapHeaderToContract(
    InvoiceHeader source)
    {
        return new InvoiceHeaderSaveModel
        {
            Gkey = source.Gkey,
            InvNbr = source.InvNbr,
            InvDate = source.InvDate,

            CustMobile = source.CustMobile,

            PaymentDueDate = source.PaymentDueDate,

            InvlTaxableAmount = source.InvTaxableAmount,

            AdvanceAdj = source.AdvanceAdj,
            RdAmountAdj = source.RdAmountAdj,

            OldGoldAmount = source.OldGoldAmount,
            OldSilverAmount = source.OldSilverAmount,

            DiscountPercent = source.DiscountPercent,
            DiscountAmount = source.DiscountAmount,

            RoundOff = source.RoundOff,

            AmountPayable = source.AmountPayable,
            RecdAmount = source.RecdAmount,

            InvBalance = source.InvBalance,
            InvRefund = source.InvRefund,

            InvNotes = source.InvNotes,

            IsTaxApplicable = source.IsTaxApplicable,
            TaxType = source.TaxType,

            CgstPercent = source.CgstPercent,
            SgstPercent = source.SgstPercent,
            IgstPercent = source.IgstPercent,

            CgstAmount = source.CgstAmount,
            SgstAmount = source.SgstAmount,
            IgstAmount = source.IgstAmount,

            PaymentMode = source.PaymentMode,

            GrossRcbAmount = source.GrossRcbAmount,
            InvlTaxTotal = source.InvlTaxTotal,

            TenantGkey = source.TenantGkey,
            CustGkey = source.CustGkey,

            GstLocSeller = source.GstLocSeller,
            GstLocBuyer = source.GstLocBuyer,

            SalesPerson = source.SalesPerson,

            Status = source.Status
        };
    }

    private static InvoiceLineSaveModel MapLineToContract(
    InvoiceLine source)
    {
        return new InvoiceLineSaveModel
        {
            Gkey = source.Gkey,

            HsnCode = source.HsnCode,
            InvLineNbr = source.InvLineNbr,
            InvNote = source.InvNote,

            InvlBilledPrice = source.InvlBilledPrice,
            InvlGrossAmt = source.InvlGrossAmt,
            InvlMakingCharges = source.InvlMakingCharges,
            InvlOtherCharges = source.InvlOtherCharges,
            InvlPayableAmt = source.InvlPayableAmt,
            InvlStoneAmount = source.InvlStoneAmount,
            InvlTaxableAmount = source.InvlTaxableAmount,
            InvlWastageAmt = source.InvlWastageAmt,

            IsTaxable = source.IsTaxable,

            ItemNotes = source.ItemNotes,

            ItemPacked = source.ItemPacked.HasValue
                ? source.ItemPacked.Value ? 1 : 0
                : null,

            ProdCategory = source.ProdCategory,
            ProdGrossWeight = source.ProdGrossWeight,
            ProdNetWeight = source.ProdNetWeight,

            ProdQty = source.ProdQty,

            ProdStoneWeight = source.ProdStoneWeight,

            ProductDesc = source.ProductDesc,
            ProductGkey = source.ProductGkey,
            ProductName = source.ProductName,
            ProdPackCode = source.ProdPackCode,
            ProductPurity = source.ProductPurity,

            TaxAmount = source.TaxAmount,
            TaxPercent = source.TaxPercent,
            TaxType = source.TaxType,

            VaAmount = source.VaAmount,
            VaPercent = source.VaPercent,

            InvoiceHdrGkey = source.InvoiceHdrGkey,
            InvoiceId = source.InvoiceId,

            TenantGkey = source.TenantGkey,

            InvlCgstPercent = source.InvlCgstPercent,
            InvlCgstAmount = source.InvlCgstAmount,

            InvlIgstPercent = source.InvlIgstPercent,
            InvlIgstAmount = source.InvlIgstAmount,

            InvlTotal = source.InvlTotal,

            InvlSgstAmount = source.InvlSgstAmount,
            InvlSgstPercent = source.InvlSgstPercent,

            ProductId = source.ProductId,
            Metal = source.Metal,
            ProductSku = source.ProductSku
        };
    }

    private static InvoiceOldMetalSaveModel MapOldMetalToContract(
    OldMetalTransaction source)
    {
        return new InvoiceOldMetalSaveModel
        {
            Gkey = source.Gkey,

            TransNbr = source.TransNbr,
            TransDate = source.TransDate,
            TransType = source.TransType,

            DocRefGkey = source.DocRefGkey,
            DocRefNbr = source.DocRefNbr,
            DocRefDate = source.DocRefDate,
            DocRefType = source.DocRefType,

            CustGkey = source.CustGkey,
            CustMobile = source.CustMobile,

            ProductGkey = source.ProductGkey,
            ProductId = source.ProductId,
            ProductCategory = source.ProductCategory,

            Metal = source.Metal,
            Purity = source.Purity,

            TransactedRate = source.TransactedRate,
            Uom = source.Uom,

            GrossWeight = source.GrossWeight,
            StoneWeight = source.StoneWeight,

            WastagePercent = source.WastagePercent,
            WastageWeight = source.WastageWeight,

            NetWeight = source.NetWeight,

            TotalProposedPrice = source.TotalProposedPrice,
            FinalPurchasePrice = source.FinalPurchasePrice,

            Remarks = source.Remarks
        };
    }

    private static InvoiceReceiptSaveModel MapReceiptToContract(
    InvoiceArReceipt source)
    {
        return new InvoiceReceiptSaveModel
        {
            Gkey = source.Gkey,

            InvoiceGkey = source.InvoiceGkey,
            InvoiceNbr = source.InvoiceNbr,

            CustGkey = source.CustGkey,

            InvoiceReceivableAmount =
                source.InvoiceReceivableAmount,

            BalanceAfterAdj =
                source.BalanceAfterAdj,

            TransactionType =
                source.TransactionType,

            ModeOfReceipt =
                source.ModeOfReceipt,

            BalBeforeAdj =
                source.BalBeforeAdj,

            AdjustedAmount =
                source.AdjustedAmount,

            InternalVoucherNbr =
                source.InternalVoucherNbr,

            InternalVoucherDate =
                source.InternalVoucherDate,

            ExternalTransactionId =
                source.ExternalTransactionId,

            ExternalTransactionDate =
                source.ExternalTransactionDate,

            BankName =
                source.BankName,

            OtherReference =
                source.OtherReference,

            SenderBankAccountNbr =
                source.SenderBankAccountNbr,

            SenderBankGkey =
                source.SenderBankGkey,

            SenderBankBranch =
                source.SenderBankBranch,

            SenderBankIfscCode =
                source.SenderBankIfscCode,

            CompanyBankAccountNbr =
                source.CompanyBankAccountNbr,

            Status =
                source.Status,

            InvoiceReceiptNbr =
                source.InvoiceReceiptNbr,

            SeqNbr =
                source.SeqNbr
        };
    }


}