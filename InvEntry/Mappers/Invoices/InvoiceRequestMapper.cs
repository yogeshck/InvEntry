using InvEntry.Contracts.Invoices;
using InvEntry.Models;
using InvEntry.Models.Settlements;
using InvEntry.ViewModels.Invoices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InvEntry.Mappers.Invoices;

public static class InvoiceRequestMapper
{
    public static SaveInvoiceRequest ToDraftSaveRequest(
        InvoiceHeader source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new SaveInvoiceRequest
        {
            Header = MapHeader(source),

            Lines = source.Lines?
                .Select(MapLine)
                .ToList()
                ?? new List<InvoiceLineSaveModel>(),

            OldMetalTransactions = source.OldMetalTransactions?
                .Select(MapOldMetal)
                .ToList()
                ?? new List<InvoiceOldMetalSaveModel>(),

            // Draft invoices do NOT contain payment/settlement data.
            // Payment information is supplied separately during Finalise.
            Receipts = new List<InvoiceReceiptSaveModel>()

            /*            Receipts = source.ReceiptLines?
                            .Select(MapReceipt)
                            .ToList()
                            ?? new List<InvoiceReceiptSaveModel>()*/
        };
    }

    public static FinaliseInvoiceRequest ToFinaliseRequest(
    int invoiceGkey,
    InvoiceSettlementViewModel settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        return new FinaliseInvoiceRequest
        {
            InvoiceGkey = invoiceGkey,


            DiscountAmount =
            settlement.DiscountAmount,

            CreditAmount =
                settlement.IsReceivable && settlement.UseCredit
                    ? settlement.CreditAmount
                    : 0M,

            Receipts = settlement.Receipts
                .Where(x => x.Amount > 0M)
                .Select(x => MapSettlementLine(
                    x,
                    InvoiceSettlementType.Receipt))
                .ToList(),

            Refunds = settlement.Refunds
                .Where(x => x.Amount > 0M)
                .Select(x => MapSettlementLine(
                    x,
                    InvoiceSettlementType.Refund))
                .ToList()
        };
    }

    private static InvoiceSettlementSaveModel MapSettlementLine(
        InvoiceSettlementLine source,
        string settlementType)
    {
        return new InvoiceSettlementSaveModel
        {
            SettlementType = settlementType,

            PaymentMode =
                source.PaymentMode?.Trim() ?? string.Empty,

            Amount = source.Amount,

            TransactionId =
                NullIfWhiteSpace(source.TransactionId),

            TransactionDate =
                source.TransactionDate,

            InstrumentNumber =
                NullIfWhiteSpace(source.InstrumentNumber),

            InstrumentDate =
                source.InstrumentDate,

            BankName =
                NullIfWhiteSpace(source.BankName),

            CompanyBankAccountNbr =
                NullIfWhiteSpace(source.CompanyBankAccountNbr),

            OtherReference =
                NullIfWhiteSpace(source.OtherReference)
        };
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    // =========================================================
    // HEADER
    // =========================================================

    private static InvoiceHeaderSaveModel MapHeader(
        InvoiceHeader source)
    {
        return new InvoiceHeaderSaveModel
        {
            Gkey = source.GKey,

            InvNbr = source.InvNbr,
            InvDate = source.InvDate,

            CustMobile = source.CustMobile,
            CustGkey = source.CustGkey,

            PlaceOfSeller = source.PlaceOfSeller,
            PlaceOfSupply = source.PlaceOfSupply,

            PaymentDueDate = source.PaymentDueDate,

            InvlTaxableAmount = source.InvlTaxableAmount,

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

            GstLocSeller = source.GstLocSeller,
            GstLocBuyer = source.GstLocBuyer,

            SalesPerson = source.SalesPerson,

            Status = string.IsNullOrWhiteSpace(
                    source.Status)
            ? InvoiceStatus.Draft
            : source.Status
        
        };
    }

    // =========================================================
    // INVOICE LINE
    // =========================================================

    private static InvoiceLineSaveModel MapLine(
        InvoiceLine source)
    {
        return new InvoiceLineSaveModel
        {
            Gkey = source.GKey,

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
            ItemPacked = source.ItemPacked,

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

    // =========================================================
    // OLD METAL
    // =========================================================

    private static InvoiceOldMetalSaveModel MapOldMetal(
        OldMetalTransaction source)
    {
        return new InvoiceOldMetalSaveModel
        {
            Gkey = source.GKey,

            TransNbr = source.TransNbr,
            TransDate = source.TransDate,
            TransType = source.TransType,

            DocRefGkey = source.DocRefGkey,
            DocRefNbr = source.DocRefNbr,
            DocRefDate = source.DocRefDate,

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

            Remarks = source.Remarks,

            DocRefType = source.DocRefType
        };
    }

    // =========================================================
    // RECEIPT
    // =========================================================

    private static InvoiceReceiptSaveModel MapReceipt(
        InvoiceArReceipt source)
    {
        return new InvoiceReceiptSaveModel
        {
            Gkey = source.GKey,

            CustGkey = source.CustGkey,

            InvoiceGkey = source.InvoiceGkey,
            InvoiceNbr = source.InvoiceNbr,

            InvoiceReceivableAmount =
                source.InvoiceReceivableAmount,

            BalanceAfterAdj =
                source.BalanceAfterAdj,

            SeqNbr = source.SeqNbr,

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

            BankName = source.BankName,

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

            Status = source.Status,

            InvoiceReceiptNbr =
                source.InvoiceReceiptNbr
        };
    }
}