using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Workflows;

public interface IOldMetalTransferPostingService
{
    void Post(StockTransferHeader header);
}

public sealed class OldMetalTransferPostingService
    : IOldMetalTransferPostingService
{
    private const string DocumentType = "OM Transfer";
    private const string LegacyDocumentType = "Stock Transfer";
    private const int LegacyRemarksLength = 50;

    private readonly MijmsContext _context;

    public OldMetalTransferPostingService(MijmsContext context)
    {
        _context = context;
    }

    public void Post(StockTransferHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);

        var customer = _context.OrgCustomers
            .SingleOrDefault(customer =>
                customer.MobileNbr == header.ToReferenceValue);

        if (customer is null)
        {
            throw new InvalidOperationException(
                "Destination reference does not resolve to a valid " +
                "customer required for Old Metal transfer posting.");
        }

        ValidateLines(header.Lines);

        var voucherType = _context.VoucherTypes
            .FromSqlInterpolated(
                $"SELECT * FROM VOUCHER_TYPES WITH (UPDLOCK, HOLDLOCK) WHERE DOCUMENT_TYPE = {DocumentType}")
            .SingleOrDefault();

        if (voucherType is null ||
            voucherType.IsActive != true ||
            string.IsNullOrWhiteSpace(voucherType.DocNbrPrefix) ||
            voucherType.DocNbrLength.GetValueOrDefault() <= 0)
        {
            throw new InvalidOperationException(
                "OM Transfer VoucherType configuration is missing, " +
                "inactive, or incomplete.");
        }

        var documentNumberLength =
            voucherType.DocNbrLength.GetValueOrDefault();

        voucherType.LastUsedNumber =
            voucherType.LastUsedNumber.GetValueOrDefault() + 1;

        var transactionNumber =
            $"{voucherType.DocNbrPrefix}" +
            $"{voucherType.LastUsedNumber.Value.ToString($"D{documentNumberLength}")}";

        foreach (var line in header.Lines)
        {
            _context.OldMetalTransactions.Add(
                new OldMetalTransaction
                {
                    TransNbr = transactionNumber,
                    TransDate = header.TransferDate,
                    TransType = DocumentType,
                    DocRefGkey = header.Gkey,
                    DocRefNbr = header.TransferNbr,
                    DocRefDate = header.TransferDate,
                    DocRefType = LegacyDocumentType,
                    CustGkey = customer.Gkey,
                    CustMobile = customer.MobileNbr,
                    ProductGkey = line.ProductGkey,
                    ProductId = line.ProductId,
                    ProductCategory = line.ProductCategory,
                    Metal = line.Metal,
                    Purity = line.Purity,
                    TransactedRate = line.TransactedRate,
                    Uom = line.Uom,
                    GrossWeight = line.GrossWeight,
                    StoneWeight = line.StoneWeight,
                    NetWeight = line.NetWeight,
                    TotalProposedPrice = line.TransferValue,
                    FinalPurchasePrice = line.TransferValue,
                    Remarks = line.Notes
                });
        }
    }

    private static void ValidateLines(
        IEnumerable<StockTransferLine> lines)
    {
        foreach (var line in lines)
        {
            if (!line.ProductGkey.HasValue ||
                string.IsNullOrWhiteSpace(line.ProductId) ||
                string.IsNullOrWhiteSpace(line.Metal) ||
                string.IsNullOrWhiteSpace(line.Purity) ||
                string.IsNullOrWhiteSpace(line.Uom))
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Old Metal product details " +
                    "are incomplete.");
            }

            if (line.GrossWeight <= 0M ||
                line.NetWeight <= 0M ||
                line.StoneWeight < 0M ||
                line.StoneWeight > line.GrossWeight)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Old Metal weights are invalid.");
            }

            if (line.TransactedRate.GetValueOrDefault() <= 0M)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Old Metal transacted rate " +
                    "must be greater than zero.");
            }

            if (line.TransferValue.GetValueOrDefault() <= 0M)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Old Metal transfer value " +
                    "must be greater than zero.");
            }

            if (line.Notes?.Length > LegacyRemarksLength)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Notes cannot exceed " +
                    $"{LegacyRemarksLength} characters for Old Metal " +
                    "compatibility posting.");
            }
        }
    }
}