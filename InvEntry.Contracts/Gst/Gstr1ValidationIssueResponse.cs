namespace InvEntry.Contracts.Gst;

public sealed class Gstr1ValidationIssueResponse
{
    /// <summary>
    /// Error, Warning or Information.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Stable validation rule code.
    /// Example: GST-DOC-001, GST-LINE-001.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable validation message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// GST staging document key, when the issue belongs
    /// to a particular document.
    /// </summary>
    public long? DocumentGkey { get; set; }

    /// <summary>
    /// Original source transaction key.
    /// </summary>
    public int? SourceGkey { get; set; }

    /// <summary>
    /// Invoice/document number for display.
    /// </summary>
    public string? DocumentNbr { get; set; }

    /// <summary>
    /// GST document line number when applicable.
    /// </summary>
    public int? LineNbr { get; set; }

    /// <summary>
    /// HSN code when applicable.
    /// </summary>
    public string? HsnCode { get; set; }

    /// <summary>
    /// Field associated with the problem.
    /// Example: RecipientGstin, HsnCode, TaxableValue.
    /// </summary>
    public string? FieldName { get; set; }
}