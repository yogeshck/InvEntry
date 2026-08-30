using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Contracts.Invoices;

namespace DataAccess.Workflows;

public sealed class InvoiceWorkflow : IInvoiceWorkflow
{
    private readonly IRepositoryBase<InvoiceHeader> _invoiceRepository;
    private readonly IRepositoryBase<InvoiceLine> _lineRepository;
    private readonly IRepositoryBase<InvoiceArReceipt> _receiptRepository;
    private readonly IRepositoryBase<OldMetalTransaction> _oldMetalRepository;
    private readonly IRepositoryBase<VoucherType> _voucherTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceWorkflow(
        IRepositoryBase<InvoiceHeader> invoiceRepository,
        IRepositoryBase<InvoiceLine> lineRepository,
        IRepositoryBase<InvoiceArReceipt> receiptRepository,
        IRepositoryBase<OldMetalTransaction> oldMetalRepository,
        IRepositoryBase<VoucherType> voucherTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _lineRepository = lineRepository;
        _receiptRepository = receiptRepository;
        _oldMetalRepository = oldMetalRepository;
        _voucherTypeRepository = voucherTypeRepository;
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

            SyncReceipts(
                invoice,
                request.Receipts ??
                    new List<InvoiceReceiptSaveModel>());

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

        var receipts =
            _receiptRepository
                .GetList(
                    x => x.InvoiceGkey == invoiceGkey)
                .OrderBy(x => x.SeqNbr)
                .ToList();

        var response = new InvoiceEditResponse
        {
            Header = MapHeaderToContract(invoice),

            Lines = lines
                .Select(MapLineToContract)
                .ToList(),

            OldMetalTransactions = oldMetal
                .Select(MapOldMetalToContract)  
                .ToList(),

            Receipts = receipts
                .Select(MapReceiptToContract)
                .ToList()
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
        int invoiceGkey,
        CancellationToken cancellationToken = default)
    {
        if (invoiceGkey <= 0)
            throw new ArgumentException(
                "A valid invoice GKey is required.",
                nameof(invoiceGkey));

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
            // PAYMENT / CREDIT VALIDATION
            // ---------------------------------------------------------
            // We will strengthen this next.
            //
            // For now do not allow an invalid negative payable/balance.
            // ---------------------------------------------------------

            if (invoice.AmountPayable < 0)
            {
                throw new InvalidOperationException(
                    "Invoice amount payable cannot be negative.");
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

            var receipts =
                _receiptRepository
                    .GetList(
                        x => x.InvoiceGkey == invoiceGkey)
                    .ToList();

            foreach (var receipt in receipts)
            {
                receipt.InvoiceNbr =
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