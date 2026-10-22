using InvEntry.Contracts.Gst;
using System;
using System.Collections.Generic;

namespace InvEntry.ViewModels;

/// <summary>
/// Validation issue as displayed on the GSTR-1 screen, with whether the
/// operator can correct it from the line correction popup.
/// </summary>
public sealed class Gstr1ValidationIssueRow
{
    // Line-level issues on the GST reporting fields the popup can edit.
    private static readonly HashSet<string> CorrectableCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "GST-LINE-002", // HSN missing
            "GST-LINE-005", // UQC missing
            "GST-LINE-006", // GST quantity missing
            "GST-LINE-007", // source UOM missing
            "GST-LINE-008"  // UOM / UQC mismatch
        };

    public Gstr1ValidationIssueRow(Gstr1ValidationIssueResponse issue)
    {
        Issue = issue;
    }

    public Gstr1ValidationIssueResponse Issue { get; }

    public bool IsCorrectable => IsCorrectableIssue(Issue);

    public bool IsError =>
        string.Equals(Issue.Severity, "Error", StringComparison.OrdinalIgnoreCase);

    public string DisplayText =>
        $"{Issue.Severity} / {Issue.Code}: {Issue.Message} {Issue.DocumentNbr}".TrimEnd();

    public static bool IsCorrectableIssue(Gstr1ValidationIssueResponse? issue) =>
        issue is not null &&
        issue.DocumentGkey is > 0 &&
        issue.LineNbr is not null &&
        CorrectableCodes.Contains(issue.Code);
}
