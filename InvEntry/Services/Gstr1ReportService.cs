using InvEntry.Contracts.Gst;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IGstr1ReportService
{
    Task<Gstr1ReturnSummaryResponse> GetSummaryAsync(
        string supplierGstin,
        string returnPeriod);

    Task<IReadOnlyList<Gstr1DocumentResponse>> GetDocumentsAsync(
        string supplierGstin,
        string returnPeriod);

    Task<IReadOnlyList<Gstr1DocumentLineResponse>> GetDocumentLinesAsync(
        long documentGkey,
        string supplierGstin,
        string returnPeriod);
}

public sealed class Gstr1ReportService : IGstr1ReportService
{
    private readonly IMijmsApiService _mijmsApiService;

    public Gstr1ReportService(
        IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public Task<Gstr1ReturnSummaryResponse> GetSummaryAsync(
        string supplierGstin,
        string returnPeriod)
    {
        Validate(supplierGstin, returnPeriod);

        return _mijmsApiService
            .GetResponse<Gstr1ReturnSummaryResponse>(
                $"api/gstr1/summary" +
                $"?supplierGstin={Uri.EscapeDataString(supplierGstin)}" +
                $"&returnPeriod={Uri.EscapeDataString(returnPeriod)}");
    }

    public async Task<IReadOnlyList<Gstr1DocumentResponse>> GetDocumentsAsync(
        string supplierGstin,
        string returnPeriod)
    {
        Validate(supplierGstin, returnPeriod);

        var result =
            await _mijmsApiService
                .GetResponse<List<Gstr1DocumentResponse>>(
                    $"api/gstr1/documents" +
                    $"?supplierGstin={Uri.EscapeDataString(supplierGstin)}" +
                    $"&returnPeriod={Uri.EscapeDataString(returnPeriod)}");

        return result;
    }

    public async Task<IReadOnlyList<Gstr1DocumentLineResponse>> GetDocumentLinesAsync(
        long documentGkey,
        string supplierGstin,
        string returnPeriod)
    {
        if (documentGkey <= 0)
            throw new ArgumentOutOfRangeException(nameof(documentGkey));

        Validate(supplierGstin, returnPeriod);

        var result =
            await _mijmsApiService
                .GetResponse<List<Gstr1DocumentLineResponse>>(
                    $"api/gstr1/documents/{documentGkey}/lines" +
                    $"?supplierGstin={Uri.EscapeDataString(supplierGstin)}" +
                    $"&returnPeriod={Uri.EscapeDataString(returnPeriod)}");

        return result;
    }

    private static void Validate(
        string supplierGstin,
        string returnPeriod)
    {
        if (string.IsNullOrWhiteSpace(supplierGstin))
            throw new ArgumentException(
                "Supplier GSTIN is required.",
                nameof(supplierGstin));

        if (string.IsNullOrWhiteSpace(returnPeriod))
            throw new ArgumentException(
                "Return period is required.",
                nameof(returnPeriod));
    }
}