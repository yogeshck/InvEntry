using DataAccess.Models;

namespace DataAccess.Services;

public interface IGstr1StagingService
{
    string GetSupplierGstin();

    Gstr1StagePreparation PrepareInvoice(
        InvoiceHeader invoice,
        IReadOnlyCollection<InvoiceLine> lines);

    Gstr1StageResult StageInvoice(
        InvoiceHeader invoice,
        IReadOnlyCollection<InvoiceLine> lines);
}

public enum Gstr1StageOutcome
{
    Staged,
    AlreadyStaged
}

public sealed class Gstr1StageResult
{
    public Gstr1StageOutcome Outcome { get; init; }

    public int SourceGkey { get; init; }

    public string DocumentNbr { get; init; } = string.Empty;

    public string SupplierGstin { get; init; } = string.Empty;
}

public sealed class Gstr1StagePreparation
{
    public int SourceGkey { get; init; }

    public string DocumentNbr { get; init; } = string.Empty;

    public string SupplierGstin { get; init; } = string.Empty;

    public string ReturnCategory { get; init; } = string.Empty;

    public string? Gstr1Table { get; init; }

    public bool IsReportable { get; init; }

    public int LineCount { get; init; }
}