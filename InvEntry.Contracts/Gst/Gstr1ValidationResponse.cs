using System.Collections.Generic;
using System.Linq;

namespace InvEntry.Contracts.Gst;

public sealed class Gstr1ValidationResponse
{
    public string SupplierGstin { get; set; } = string.Empty;

    public string ReturnPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Number of staged documents examined.
    /// </summary>
    public int DocumentCount { get; set; }

    /// <summary>
    /// Number of reportable staged documents examined.
    /// </summary>
    public int ReportableDocumentCount { get; set; }

    public List<Gstr1ValidationIssueResponse> Issues { get; set; } = [];

    public int ErrorCount =>
        Issues.Count(x =>
            string.Equals(
                x.Severity,
                "Error",
                System.StringComparison.OrdinalIgnoreCase));

    public int WarningCount =>
        Issues.Count(x =>
            string.Equals(
                x.Severity,
                "Warning",
                System.StringComparison.OrdinalIgnoreCase));

    public int InformationCount =>
        Issues.Count(x =>
            string.Equals(
                x.Severity,
                "Information",
                System.StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Export must not proceed when blocking validation
    /// errors are present.
    /// </summary>
    public bool IsValid => ErrorCount == 0;

    public bool IsExportReady => ErrorCount == 0;
}