using DevExpress.XtraReports.UI;

namespace InvEntry.Reports;

public partial class Gstr1MonthlyReport : XtraReport
{
    public Gstr1MonthlyReport()
    {
        InitializeComponent();
    }

    public Gstr1MonthlyReport(Gstr1ReportSnapshot snapshot)
        : this()
    {
        ApplySnapshot(snapshot);
    }

    private void ApplySnapshot(Gstr1ReportSnapshot snapshot)
    {
        DisplayName = $"GSTR-1 {snapshot.ReturnMonth}";
        DataSource = snapshot.Rows;

        companyNameLabel.Text = snapshot.CompanyName;
        branchLabel.Text = $"Branch / Location: {snapshot.Branch}";
        gstinLabel.Text = $"GSTIN: {snapshot.SupplierGstin}";
        financialYearLabel.Text = $"Financial Year: {snapshot.FinancialYear}";
        returnMonthLabel.Text = $"Return Month: {snapshot.ReturnMonth}";
        scopeLabel.Text = $"Scope: {snapshot.Scope}";
        overallTotalsLabel.Text =
            $"Overall totals - Documents: {snapshot.DocumentCount:N0}   Invoice: {snapshot.InvoiceValue:N2}   " +
            $"Taxable: {snapshot.TaxableValue:N2}   CGST: {snapshot.CgstAmount:N2}   " +
            $"SGST: {snapshot.SgstAmount:N2}   IGST: {snapshot.IgstAmount:N2}   Cess: {snapshot.CessAmount:N2}";
        reportTotalsLabel.Text =
            $"TOTAL  Documents {snapshot.DocumentCount:N0} | Invoice {snapshot.InvoiceValue:N2} | " +
            $"Taxable {snapshot.TaxableValue:N2} | CGST {snapshot.CgstAmount:N2} | " +
            $"SGST {snapshot.SgstAmount:N2} | IGST {snapshot.IgstAmount:N2} | Cess {snapshot.CessAmount:N2}";
        generatedAtLabel.Text = $"Generated: {snapshot.GeneratedAt:dd-MMM-yyyy HH:mm:ss}";
    }
}