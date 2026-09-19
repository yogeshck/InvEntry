using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Gst.Core.Classification;
using InvEntry.Gst.Core.Models;

namespace DataAccess.Services;

public sealed class Gstr1StagingService
    : IGstr1StagingService
{
    private readonly IRepositoryBase<GstGstr1Document>
        _gstGstr1DocumentRepository;

    private readonly IRepositoryBase<OrgThisCompanyView>
        _companyRepository;

    private readonly IRepositoryBase<OrgCustomer>
        _customerRepository;

    private readonly IGstClassificationService
        _gstClassificationService;

    public Gstr1StagingService(
        IRepositoryBase<GstGstr1Document> gstGstr1DocumentRepository,
        IRepositoryBase<OrgThisCompanyView> companyRepository,
        IRepositoryBase<OrgCustomer> customerRepository,
        IGstClassificationService gstClassificationService)
    {
        _gstGstr1DocumentRepository =
            gstGstr1DocumentRepository;

        _companyRepository =
            companyRepository;

        _customerRepository =
            customerRepository;

        _gstClassificationService =
            gstClassificationService;
    }

    public string GetSupplierGstin()
    {
        var company =
            GetCurrentCompany();

        return company.GstNbr!
            .Trim()
            .ToUpperInvariant();
    }

    public Gstr1StageResult StageInvoice(
        InvoiceHeader invoice,
        IReadOnlyCollection<InvoiceLine> lines)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(lines);

        var supplierGstin =
            GetSupplierGstin();

        var existing =
            _gstGstr1DocumentRepository.Get(
                x =>
                    x.SourceGkey == invoice.Gkey &&
                    x.DocumentType == "SalesInvoice" &&
                    x.SupplierGstin == supplierGstin);

        if (existing != null)
        {
            return new Gstr1StageResult
            {
                Outcome =
                    Gstr1StageOutcome.AlreadyStaged,

                SourceGkey =
                    invoice.Gkey,

                DocumentNbr =
                    invoice.InvNbr ??
                    string.Empty,

                SupplierGstin =
                    supplierGstin
            };
        }

        var document =
            PrepareDocument(
                invoice,
                lines);

        _gstGstr1DocumentRepository.Add(
            document);

        return new Gstr1StageResult
        {
            Outcome =
                Gstr1StageOutcome.Staged,

            SourceGkey =
                document.SourceGkey,

            DocumentNbr =
                document.DocumentNbr,

            SupplierGstin =
                document.SupplierGstin
        };
    }

    private OrgCustomer GetInvoiceCustomer(
        InvoiceHeader invoice)
    {
        if (!invoice.CustGkey.HasValue ||
            invoice.CustGkey.Value <= 0)
        {
            throw new InvalidOperationException(
                "Invoice customer is required for GSTR-1 staging.");
        }

        var customer =
            _customerRepository.Get(
                x =>
                    x.Gkey ==
                    invoice.CustGkey.Value);

        if (customer is null)
        {
            throw new InvalidOperationException(
                $"Customer GKey {invoice.CustGkey.Value} was not found.");
        }

        return customer;
    }

    private OrgThisCompanyView GetCurrentCompany()
    {
        var company =
            _companyRepository.Get(
                x => x.ThisCompany == true);

        if (company is null)
        {
            throw new InvalidOperationException(
                "Current company configuration was not found.");
        }

        if (string.IsNullOrWhiteSpace(
                company.GstNbr))
        {
            throw new InvalidOperationException(
                "GSTIN is not configured for the current company.");
        }

        if (string.IsNullOrWhiteSpace(
                company.GstCode))
        {
            throw new InvalidOperationException(
                "GST state code is not configured for the current company.");
        }

        return company;
    }

    private GstGstr1Document PrepareDocument(
    InvoiceHeader invoice,
    IReadOnlyCollection<InvoiceLine> lines)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(lines);

        if (invoice.Gkey <= 0)
        {
            throw new InvalidOperationException(
                "A valid invoice GKey is required for GSTR-1 staging.");
        }

        if (string.IsNullOrWhiteSpace(invoice.InvNbr))
        {
            throw new InvalidOperationException(
                "Invoice number is required for GSTR-1 staging.");
        }

        if (!invoice.InvDate.HasValue)
        {
            throw new InvalidOperationException(
                "Invoice date is required for GSTR-1 staging.");
        }

        if (string.IsNullOrWhiteSpace(invoice.GstLocBuyer))
        {
            throw new InvalidOperationException(
                "Place of Supply is required for GSTR-1 staging.");
        }

        if (lines.Count == 0)
        {
            throw new InvalidOperationException(
                $"Final invoice {invoice.InvNbr} cannot be staged " +
                "for GSTR-1 without invoice lines.");
        }

        var company =
            GetCurrentCompany();

        var customer =
            GetInvoiceCustomer(invoice);

        var supplierGstin =
            company.GstNbr!
                .Trim()
                .ToUpperInvariant();

        var supplierStateCode =
            company.GstCode!
                .Trim();

        var recipientGstin =
            string.IsNullOrWhiteSpace(customer.GstinNbr)
                ? null
                : customer.GstinNbr
                    .Trim()
                    .ToUpperInvariant();

        var recipientStateCode =
            string.IsNullOrWhiteSpace(customer.GstStateCode)
                ? null
                : customer.GstStateCode.Trim();

        var placeOfSupply =
            invoice.GstLocBuyer.Trim();

        // =========================================================
        // GST REPORTING VALUES
        //
        // IMPORTANT:
        // These values are derived from SALE LINES specifically for
        // GST/GSTR-1 reporting.
        //
        // Do NOT use InvoiceHeader.InvTaxableAmount or
        // GrossRcbAmount here because those fields may contain
        // old-metal / settlement adjustments.
        //
        // This does NOT modify the original invoice.
        // =========================================================

        var gstTaxableValue =
            lines.Sum(x =>
                x.InvlTaxableAmount.GetValueOrDefault());

        var gstCgstAmount =
            lines.Sum(x =>
                x.InvlCgstAmount.GetValueOrDefault());

        var gstSgstAmount =
            lines.Sum(x =>
                x.InvlSgstAmount.GetValueOrDefault());

        var gstIgstAmount =
            lines.Sum(x =>
                x.InvlIgstAmount.GetValueOrDefault());

        var gstCessAmount = 0M;

        // For GSTR-1 staging this represents the value of the
        // outward supply, independently of customer settlement.
        var gstInvoiceValue =
            gstTaxableValue +
            gstCgstAmount +
            gstSgstAmount +
            gstIgstAmount +
            gstCessAmount;

        var classificationRequest =
            new GstClassificationRequest
            {
                DocumentType =
                    GstDocumentType.SalesInvoice,

                DocumentNumber =
                    invoice.InvNbr,

                DocumentDate =
                    invoice.InvDate.Value,

                SupplierGstin =
                    supplierGstin,

                SupplierStateCode =
                    supplierStateCode,

                RecipientGstin =
                    recipientGstin,

                RecipientStateCode =
                    recipientStateCode,

                PlaceOfSupplyCode =
                    placeOfSupply,

                TaxableValue =
                    gstTaxableValue,

                InvoiceValue =
                    gstInvoiceValue,

                CgstAmount =
                    gstCgstAmount,

                SgstAmount =
                    gstSgstAmount,

                IgstAmount =
                    gstIgstAmount,

                CessAmount =
                    gstCessAmount,

                TaxTreatment =
                    invoice.IsTaxApplicable
                        ? GstTaxTreatment.Taxable
                        : GstTaxTreatment.NonGst,

                IsExport = false,
                IsSez = false,
                IsReverseCharge = false,
                IsDeemedExport = false,
                IsEcommerceSupply = false,
                IsAmendment = false
            };

        var classification =
            _gstClassificationService.Classify(
                classificationRequest);

        if (!classification.IsValid)
        {
            throw new InvalidOperationException(
                $"GSTR-1 classification failed for invoice " +
                $"{invoice.InvNbr}: " +
                string.Join(
                    "; ",
                    classification.Errors));
        }

        var document =
            new GstGstr1Document
            {
                SourceGkey = invoice.Gkey,

                DocumentType =
                    GstDocumentType.SalesInvoice.ToString(),

                DocumentNbr =
                    invoice.InvNbr,

                DocumentDate =
                    DateOnly.FromDateTime(
                        invoice.InvDate.Value),

                SupplierGstin =
                    supplierGstin,

                ReturnPeriod =
                    invoice.InvDate.Value.ToString(
                        "yyyyMM",
                        System.Globalization.CultureInfo.InvariantCulture),

                RecipientGstin =
                    recipientGstin,

                RecipientStateCode =
                    recipientStateCode,

                IsRecipientRegistered =
                    classification.IsRecipientRegistered,

                PlaceOfSupplyCode =
                    placeOfSupply,

                SupplyType =
                    classification.SupplyType.ToString(),

                TaxType =
                    classification.TaxType.ToString(),

                ReturnCategory =
                    classification.ReturnCategory.ToString(),

                Gstr1Table =
                    classification.Gstr1Table,

                IsReportable =
                    classification.IsReportable,

                InvoiceValue =
    gstInvoiceValue,

                TaxableValue =
    gstTaxableValue,

                CgstAmount =
    gstCgstAmount,

                SgstAmount =
    gstSgstAmount,

                IgstAmount =
    gstIgstAmount,

                CessAmount =
    gstCessAmount,

                IsReverseCharge = false,
                IsSez = false,
                IsDeemedExport = false,
                IsEcommerceSupply = false,

                EcommerceOperatorGstin = null,

                IsAmendment = false,

                OriginalDocumentNbr = null,
                OriginalDocumentDate = null,

                Status = "PENDING",
                CreatedOn = DateTime.Now
            };

        var usedLineNumbers =
            new HashSet<int>();

        var nextLineNumber = 1;

        foreach (var line in lines)
        {
            var lineNumber =
                line.InvLineNbr.GetValueOrDefault();

            if (lineNumber <= 0 ||
                !usedLineNumbers.Add(lineNumber))
            {
                while (
                    usedLineNumbers.Contains(
                        nextLineNumber))
                {
                    nextLineNumber++;
                }

                lineNumber =
                    nextLineNumber;

                usedLineNumbers.Add(
                    lineNumber);
            }

            var cgstRate =
                line.InvlCgstPercent.GetValueOrDefault();

            var sgstRate =
                line.InvlSgstPercent.GetValueOrDefault();

            var igstRate =
                line.InvlIgstPercent.GetValueOrDefault();

            var gstRate =
                igstRate > 0M
                    ? igstRate
                    : cgstRate + sgstRate;

            var description =
                !string.IsNullOrWhiteSpace(
                    line.ProductDesc)
                    ? line.ProductDesc.Trim()

                    : !string.IsNullOrWhiteSpace(
                        line.ProductName)
                        ? line.ProductName.Trim()

                        : !string.IsNullOrWhiteSpace(
                            line.ItemNotes)
                            ? line.ItemNotes.Trim()
                            : null;

            document.GstGstr1DocumentLines.Add(
                new GstGstr1DocumentLine
                {
                    SourceLineGkey =
                        line.Gkey > 0
                            ? line.Gkey
                            : null,

                    LineNbr = lineNumber,

                    HsnCode =
                        string.IsNullOrWhiteSpace(
                            line.HsnCode)
                            ? null
                            : line.HsnCode.Trim(),

                    Description =
                        description,

                    Quantity =
                        Convert.ToDecimal(
                            line.ProdQty),

                    TaxableValue =
                        line.InvlTaxableAmount
                            .GetValueOrDefault(),

                    GstRate = gstRate,
                    CgstRate = cgstRate,
                    SgstRate = sgstRate,
                    IgstRate = igstRate,

                    CgstAmount =
                        line.InvlCgstAmount
                            .GetValueOrDefault(),

                    SgstAmount =
                        line.InvlSgstAmount
                            .GetValueOrDefault(),

                    IgstAmount =
                        line.InvlIgstAmount
                            .GetValueOrDefault(),

                    CessAmount = 0M
                });
        }

        return document;
    }

    public Gstr1StagePreparation PrepareInvoice(
    InvoiceHeader invoice,
    IReadOnlyCollection<InvoiceLine> lines)
    {
        var document =
            PrepareDocument(
                invoice,
                lines);

        return new Gstr1StagePreparation
        {
            SourceGkey =
                document.SourceGkey,

            DocumentNbr =
                document.DocumentNbr,

            SupplierGstin =
                document.SupplierGstin,

            ReturnCategory =
                document.ReturnCategory,

            Gstr1Table =
                document.Gstr1Table,

            IsReportable =
                document.IsReportable,

            LineCount =
                document.GstGstr1DocumentLines.Count
        };
    }

}