using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

internal static class HistoricalAuditFingerprint
{
    public static string Create(HistoricalInvoiceAuditItemResponse item)
    {
        var evidence = new
        {
            item.InvoiceNbr, item.InvoiceDate, item.CurrentStatus, item.CustomerName,
            item.AmountPayable, item.TaxableAmount, item.TaxTotal, item.CgstAmount,
            item.SgstAmount, item.IgstAmount, item.InvoiceLineCount,
            item.LinkedVoucherCount, item.LinkedArReceiptCount,
            item.HasGstStagingDocument, item.GstStagingStatus, item.PlaceOfSupply,
            item.CompletionEvidenceClassification, item.CompletionEvidenceReasons,
            item.CustomerGstin, item.IsRecipientRegistered, item.SupplierStateCode,
            item.CustomerStateCode, item.IsTaxApplicable, item.SupplyType,
            item.GstReturnCategory, item.GstTaxType, item.GstClassificationValid,
            item.GstClassificationErrors, item.Lines, item.HasOldGoldTransaction,
            item.OldGoldTransactions
        };
        return Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evidence))));
    }
}
