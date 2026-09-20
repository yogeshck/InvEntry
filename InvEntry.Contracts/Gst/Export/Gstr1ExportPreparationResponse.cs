using InvEntry.Contracts.Gst;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1ExportPreparationResponse
{
    public string SupplierGstin { get; set; } = string.Empty;

    public string ReturnPeriod { get; set; } = string.Empty;

    public Gstr1ValidationResponse Validation { get; set; } = new();

    public Gstr1B2csSummaryResponse B2cs { get; set; } = new();

    public Gstr1HsnSummaryResponse Hsn { get; set; } = new();

    public Gstr1DocumentsIssuedResponse DocumentsIssued { get; set; } = new();
}
