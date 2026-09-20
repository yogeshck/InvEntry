using InvEntry.Contracts.Gst;
using InvEntry.Contracts.Gst.Export;

namespace DataAccess.Services;

public sealed class Gstr1ExportPreparationService : IGstr1ExportPreparationService
{
    private readonly IGstr1ValidationService _validationService;
    private readonly IGstr1B2csSummaryService _b2csSummaryService;
    private readonly IGstr1B2bSummaryService _b2bSummaryService;
    private readonly IGstr1HsnSummaryService _hsnSummaryService;
    private readonly IGstr1DocumentsIssuedService _documentsIssuedService;

    public Gstr1ExportPreparationService(
        IGstr1ValidationService validationService,
        IGstr1B2csSummaryService b2csSummaryService,
        IGstr1B2bSummaryService b2bSummaryService,
        IGstr1HsnSummaryService hsnSummaryService,
        IGstr1DocumentsIssuedService documentsIssuedService)
    {
        _validationService = validationService;
        _b2csSummaryService = b2csSummaryService;
        _b2bSummaryService = b2bSummaryService;
        _hsnSummaryService = hsnSummaryService;
        _documentsIssuedService = documentsIssuedService;
    }

    public async Task<Gstr1ExportPreparationResponse> PrepareAsync(
        string supplierGstin,
        string returnPeriod,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validationService.ValidateAsync(
            supplierGstin,
            returnPeriod,
            cancellationToken);

        if (!validation.IsExportReady)
        {
            throw new InvalidOperationException(
                $"GSTR-1 export preparation refused because validation returned {validation.ErrorCount} error(s).");
        }

        var b2cs = await _b2csSummaryService.GetSummaryAsync(
            new Gstr1B2csSummaryQuery
            {
                SupplierGstin = supplierGstin,
                ReturnPeriod = returnPeriod
            },
            cancellationToken);

        var b2b = await _b2bSummaryService.GetSummaryAsync(
            new Gstr1B2bSummaryQuery
            {
                SupplierGstin = supplierGstin,
                ReturnPeriod = returnPeriod
            },
            cancellationToken);

        var hsn = await _hsnSummaryService.GetSummaryAsync(
            new Gstr1HsnSummaryQuery
            {
                SupplierGstin = supplierGstin,
                ReturnPeriod = returnPeriod
            },
            cancellationToken);

        var documentsIssued = await _documentsIssuedService.GetAsync(
            supplierGstin,
            returnPeriod,
            cancellationToken);


        return new Gstr1ExportPreparationResponse
        {
            SupplierGstin = validation.SupplierGstin,
            ReturnPeriod = validation.ReturnPeriod,
            Validation = validation,
            B2cs = b2cs,
            B2b = b2b,
            Hsn = hsn,
            DocumentsIssued = documentsIssued
        };
    }
}
