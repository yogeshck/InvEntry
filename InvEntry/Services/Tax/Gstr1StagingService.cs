using InvEntry.Models;
using InvEntry.GST;
using System;

public class Gstr1StagingService
{
    private readonly IGstClassificationService
        _gstClassificationService;

    private readonly IRepository<GstGstr1Document>  _documentRepository;

    public Gstr1StagingService(
        IGstClassificationService gstClassificationService,
        IRepository<GstGstr1Document> documentRepository)
    {
        _gstClassificationService =
            gstClassificationService;

        _documentRepository =
            documentRepository;
    }

    public void StageSalesInvoice(
        InvoiceHeader invoice,
        IReadOnlyCollection<InvoiceLine> lines,
        OrgThisCompanyView company,
        Customer customer)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(company);
        ArgumentNullException.ThrowIfNull(customer);

        if (invoice.Gkey <= 0)
            throw new InvalidOperationException(
                "A valid invoice GKey is required for GSTR-1 staging.");

        if (string.IsNullOrWhiteSpace(invoice.InvNbr))
            throw new InvalidOperationException(
                "Invoice number is required for GSTR-1 staging.");

        if (!invoice.InvDate.HasValue)
            throw new InvalidOperationException(
                "Invoice date is required for GSTR-1 staging.");

        if (string.IsNullOrWhiteSpace(company.GstNbr))
            throw new InvalidOperationException(
                "Company GSTIN is required for GSTR-1 staging.");

        if (string.IsNullOrWhiteSpace(company.GstCode))
            throw new InvalidOperationException(
                "Company GST state code is required for GSTR-1 staging.");

        /*
         * Your backend InvoiceHeader uses GstLocBuyer/GstLocSeller,
         * whereas WPF currently uses PlaceOfSupply/PlaceOfSeller.
         *
         * For GST classification:
         *   Supplier state = Company.GstCode
         *   POS            = Invoice.GstLocBuyer
         */
        var placeOfSupply =
            invoice.GstLocBuyer;

        if (string.IsNullOrWhiteSpace(placeOfSupply))
            throw new InvalidOperationException(
                "Place of Supply is required for GSTR-1 staging.");

        // =====================================================
        // CLASSIFY FROM AUTHORITATIVE BACKEND DATA
        // =====================================================

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
                    company.GstNbr,

                SupplierStateCode =
                    company.GstCode,

                RecipientGstin =
                    customer.GstinNbr,

                RecipientStateCode =
                    customer.GstStateCode,

                PlaceOfSupplyCode =
                    placeOfSupply,

                TaxableValue =
                    invoice.InvTaxableAmount
                        .GetValueOrDefault(),

                InvoiceValue =
                    invoice.GrossRcbAmount
                        .GetValueOrDefault(),

                CgstAmount =
                    invoice.CgstAmount
                        .GetValueOrDefault(),

                SgstAmount =
                    invoice.SgstAmount
                        .GetValueOrDefault(),

                IgstAmount =
                    invoice.IgstAmount
                        .GetValueOrDefault(),

                CessAmount = 0M,

                TaxTreatment =
                    invoice.IsTaxApplicable
                        ? GstTaxTreatment.Taxable
                        : GstTaxTreatment.NonGst
            };

        var classification =
            _gstClassificationService.Classify(
                classificationRequest);

        if (!classification.IsValid)
        {
            var errors =
                string.Join(
                    "; ",
                    classification.Errors);

            throw new InvalidOperationException(
                $"GSTR-1 classification failed for invoice " +
                $"{invoice.InvNbr}: {errors}");
        }

        // =====================================================
        // IDEMPOTENCY
        // =====================================================

        var supplierGstin =
            company.GstNbr.Trim().ToUpperInvariant();

        var existing =
            _documentRepository.Get(
                x =>
                    x.SourceGkey == invoice.Gkey &&
                    x.DocumentType == "SalesInvoice" &&
                    x.SupplierGstin == supplierGstin);

        if (existing != null)
            return;

        // =====================================================
        // HEADER SNAPSHOT
        // =====================================================

        var document =
            new GstGstr1Document
            {
                SourceGkey =
                    invoice.Gkey,

                DocumentType =
                    "SalesInvoice",

                DocumentNbr =
                    invoice.InvNbr,

                DocumentDate =
                    invoice.InvDate.Value,

                SupplierGstin =
                    supplierGstin,

                ReturnPeriod =
                    invoice.InvDate.Value
                        .ToString("yyyyMM"),

                RecipientGstin =
                    string.IsNullOrWhiteSpace(customer.GstinNbr)
                        ? null
                        : customer.GstinNbr.Trim()
                            .ToUpperInvariant(),

                RecipientStateCode =
                    customer.GstStateCode,

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
                    invoice.GrossRcbAmount
                        .GetValueOrDefault(),

                TaxableValue =
                    invoice.InvTaxableAmount
                        .GetValueOrDefault(),

                CgstAmount =
                    invoice.CgstAmount
                        .GetValueOrDefault(),

                SgstAmount =
                    invoice.SgstAmount
                        .GetValueOrDefault(),

                IgstAmount =
                    invoice.IgstAmount
                        .GetValueOrDefault(),

                CessAmount = 0M,

                IsReverseCharge = false,
                IsSez = false,
                IsDeemedExport = false,
                IsEcommerceSupply = false,
                IsAmendment = false,

                Status =
                    "PENDING",

                CreatedOn =
                    DateTime.Now
            };

        // =====================================================
        // LINE SNAPSHOT
        // =====================================================

        foreach (var line in
                 lines.OrderBy(x => x.InvLineNbr))
        {
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

            document.Lines.Add(
                new GstGstr1DocumentLine
                {
                    SourceLineGkey =
                        line.Gkey,

                    LineNbr =
                        line.InvLineNbr
                            ?? line.Gkey,

                    HsnCode =
                        line.HsnCode,

                    Description =
                        line.ProductDesc
                        ?? line.ProductName
                        ?? line.ItemNotes,

                    Quantity =
                        line.ProdQty,

                    TaxableValue =
                        line.InvlTaxableAmount
                            .GetValueOrDefault(),

                    GstRate =
                        gstRate,

                    CgstRate =
                        cgstRate,

                    SgstRate =
                        sgstRate,

                    IgstRate =
                        igstRate,

                    CgstAmount =
                        line.InvlCgstAmount
                            .GetValueOrDefault(),

                    SgstAmount =
                        line.InvlSgstAmount
                            .GetValueOrDefault(),

                    IgstAmount =
                        line.InvlIgstAmount
                            .GetValueOrDefault(),

                    CessAmount =
                        0M
                });
        }

        // Adding the parent should add Lines through EF navigation.
        _documentRepository.Add(document);
    }
}