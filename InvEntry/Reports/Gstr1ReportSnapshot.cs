using System;
using System.Collections.Generic;

namespace InvEntry.Reports;

public sealed class Gstr1ReportSnapshot
{
    public string CompanyName { get; init; } = string.Empty;
    public string Branch { get; init; } = string.Empty;
    public string SupplierGstin { get; init; } = string.Empty;
    public string FinancialYear { get; init; } = string.Empty;
    public string ReturnMonth { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public int DocumentCount { get; init; }
    public decimal InvoiceValue { get; init; }
    public decimal TaxableValue { get; init; }
    public decimal CgstAmount { get; init; }
    public decimal SgstAmount { get; init; }
    public decimal IgstAmount { get; init; }
    public decimal CessAmount { get; init; }
    public IReadOnlyList<Gstr1ReportRow> Rows { get; init; } = [];
}

public sealed class Gstr1ReportRow
{
    public string RowType { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Gstr1Table { get; init; } = string.Empty;
    public int? DocumentCount { get; init; }
    public string DocumentNbr { get; init; } = string.Empty;
    public DateTime? DocumentDate { get; init; }
    public string Recipient { get; init; } = string.Empty;
    public decimal InvoiceValue { get; init; }
    public decimal TaxableValue { get; init; }
    public decimal CgstAmount { get; init; }
    public decimal SgstAmount { get; init; }
    public decimal IgstAmount { get; init; }
    public decimal CessAmount { get; init; }
}
