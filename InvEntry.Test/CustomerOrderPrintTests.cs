using System.Reflection;
using System.Runtime.CompilerServices;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraReports.UI;
using InvEntry.Models;
using InvEntry.Reports;
using InvEntry.ViewModels;
using Microsoft.Extensions.Configuration;

namespace InvEntry.Test;

public sealed class CustomerOrderPrintTests
{
    [Test]
    public void Report_UsesCustomerOrderSourcesAndOrderNumberParameter()
    {
        using var report = new CustomerOrderPrint();
        var dataSource = GetPrivateField<SqlDataSource>(report, "sqlDataSource1");
        var queries = dataSource.Queries.OfType<CustomSqlQuery>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(report.Parameters
                    .Cast<DevExpress.XtraReports.Parameters.Parameter>()
                    .Select(x => x.Name),
                Is.EqualTo(new[] { "pOrderNbr" }));
            Assert.That(queries.Select(x => x.Name), Is.EquivalentTo(new[]
            {
                "CUSTOMER_ORDER_HEADER",
                "CUSTOMER_ORDER_LINES",
                "CUSTOMER_ORDER_OLD_METAL",
                "CUSTOMER_ORDER_RECEIPTS",
                "CUSTOMER_ORDER_SETTLEMENT",
                "ORG_CUSTOMER_ADDRESS_VIEW",
                "ORG_THIS_COMPANY_VIEW",
                "CompBankDet"
            }));
            Assert.That(queries.Select(x => x.Sql),
                Has.None.Contains("INVOICE_"));
            Assert.That(GetPrivateField<DetailReportBand>(report, "DetailReport").DataMember,
                Is.EqualTo("CUSTOMER_ORDER_LINES"));
            Assert.That(GetPrivateField<DetailReportBand>(report, "DetailReport1").Visible,
                Is.False);
            Assert.That(GetPrivateField<CalculatedField>(report, "calculatedField1").Expression,
                Is.Empty);
            Assert.That(GetPrivateField<XRLabel>(report, "xrLabel20")
                    .ExpressionBindings.Single(x => x.PropertyName == "Text").Expression,
                Is.EqualTo("[calculatedField1]"));
            Assert.That(GetPrivateField<XRTableRow>(report, "subTotalRow").Visible,
                Is.False);
            Assert.That(GetPrivateField<XRTableRow>(report, "xrTableRow12").Visible,
                Is.True);
            Assert.That(GetPrivateField<XRLabel>(report, "xrLabel1").Visible,
                Is.False);
            Assert.That(GetPrivateField<XRLabel>(report, "xrLabel2").Visible,
                Is.False);
        });
    }

    [Test]
    public void SettlementQueries_UseCustomerOrderRecordsAndAcceptedAmounts()
    {
        using var report = new CustomerOrderPrint();
        var queries = GetPrivateField<SqlDataSource>(report, "sqlDataSource1")
            .Queries.OfType<CustomSqlQuery>().ToDictionary(x => x.Name, x => x.Sql);

        Assert.Multiple(() =>
        {
            Assert.That(queries["CUSTOMER_ORDER_OLD_METAL"], Does.Contain("OMT.DOC_REF_GKEY"));
            Assert.That(queries["CUSTOMER_ORDER_OLD_METAL"], Does.Contain("OMT.DOC_REF_NBR = @paramOrderNbr"));
            Assert.That(queries["CUSTOMER_ORDER_OLD_METAL"], Does.Contain("'CUSTOMER ORDER'"));
            Assert.That(queries["CUSTOMER_ORDER_OLD_METAL"], Does.Contain("FINAL_PURCHASE_PRICE"));
            Assert.That(queries["CUSTOMER_ORDER_RECEIPTS"], Does.Contain("V.REF_DOC_GKEY"));
            Assert.That(queries["CUSTOMER_ORDER_RECEIPTS"], Does.Contain("V.REF_DOC_NBR = @paramOrderNbr"));
            Assert.That(queries["CUSTOMER_ORDER_RECEIPTS"], Does.Contain("V.TRANS_TYPE"));
            Assert.That(queries["CUSTOMER_ORDER_RECEIPTS"], Does.Contain("'RECEIPT'"));
            Assert.That(queries["CUSTOMER_ORDER_RECEIPTS"], Does.Contain("'ADVANCE RECEIPT'"));
            Assert.That(queries["CUSTOMER_ORDER_SETTLEMENT"], Does.Contain("sum(OMT.FINAL_PURCHASE_PRICE)"));
            Assert.That(queries["CUSTOMER_ORDER_SETTLEMENT"], Does.Contain("sum(V.TRANS_AMOUNT)"));
            Assert.That(queries["CUSTOMER_ORDER_SETTLEMENT"], Does.Contain("as BALANCE_AMOUNT"));
            Assert.That(queries.Values, Has.None.Contains("INVOICE_"));
        });
    }

    [TestCase(1000, new double[0], new double[0], 1000, TestName = "Balance_NoOptionalDeductions")]
    [TestCase(1000, new[] { 200d }, new double[0], 800, TestName = "Balance_OneOldMetal")]
    [TestCase(1000, new[] { 100d, 150d }, new double[0], 750, TestName = "Balance_MultipleOldMetal")]
    [TestCase(1000, new double[0], new[] { 300d }, 700, TestName = "Balance_OneReceipt")]
    [TestCase(1000, new double[0], new[] { 100d, 200d }, 700, TestName = "Balance_MultipleReceipts")]
    [TestCase(1000, new[] { 250d }, new[] { 300d }, 450, TestName = "Balance_OldMetalAndReceipt")]
    [TestCase(100, new[] { 75d }, new[] { 50d }, -25, TestName = "Balance_IsNotClamped")]
    public void BalanceCalculation_SumsAllQualifyingRows(
        double total,
        double[] oldMetal,
        double[] receipts,
        double expected)
    {
        var method = typeof(CustomerOrderPrint).GetMethod(
            "CalculateBalance",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new AssertionException("Customer Order balance calculation was not found.");

        var actual = method.Invoke(null, new object[]
        {
            (decimal)total,
            oldMetal.Select(x => (decimal?)x),
            receipts.Select(x => (decimal?)x)
        });

        Assert.That(actual, Is.EqualTo((decimal)expected));
    }

    [Test]
    public void OptionalSections_AreDataBoundAndGrowByDetailRow()
    {
        using var report = new CustomerOrderPrint();
        var detailReports = report.Bands.OfType<DetailReportBand>().ToDictionary(x => x.Name);

        Assert.Multiple(() =>
        {
            Assert.That(detailReports["OldMetalDetailReport"].DataMember,
                Is.EqualTo("CUSTOMER_ORDER_OLD_METAL"));
            Assert.That(detailReports["OldMetalDetailReport"].Bands.OfType<DetailBand>().Single().Name,
                Is.EqualTo("OldMetalDetail"));
            Assert.That(detailReports["ReceiptDetailReport"].DataMember,
                Is.EqualTo("CUSTOMER_ORDER_RECEIPTS"));
            Assert.That(detailReports["ReceiptDetailReport"].Bands.OfType<DetailBand>().Single().Name,
                Is.EqualTo("ReceiptDetail"));
            Assert.That(detailReports["SettlementSummaryDetailReport"].DataMember,
                Is.EqualTo("CUSTOMER_ORDER_SETTLEMENT"));
        });
    }

    [Test]
    public void HeaderAndCategoryLayout_PreserveReadableWidthsAndAlignedBindings()
    {
        using var report = new CustomerOrderPrint();
        var caption = GetPrivateField<XRTableCell>(report, "invoiceDateCaption");
        var details = GetPrivateField<XRTableCell>(report, "invoiceDate");
        var category = GetPrivateField<XRTableCell>(report, "hsnCode");
        var categoryHeader = GetPrivateField<XRTableCell>(report, "hsnCaption");
        var product = GetPrivateField<XRTableCell>(report, "productName");
        var productHeader = GetPrivateField<XRTableCell>(report, "productNameCaption");
        var detailsExpression = details.ExpressionBindings
            .Single(x => x.PropertyName == "Text").Expression;

        Assert.Multiple(() =>
        {
            Assert.That(caption.Text, Is.EqualTo("ORDER DETAILS:"));
            Assert.That(caption.WordWrap, Is.False);
            Assert.That(detailsExpression, Does.Contain("'Order Date: '"));
        Assert.That(detailsExpression, Does.Contain("Expected: "));
            Assert.That(detailsExpression, Does.Contain("Iif(IsNull([CUSTOMER_ORDER_HEADER].[DELIVERY_DATE]), ''"));
            Assert.That(detailsExpression, Does.Contain("'     Status: '"));
            Assert.That(category.Weight, Is.GreaterThan(0.6D));
            Assert.That(categoryHeader.Weight, Is.GreaterThan(0.6D));
            Assert.That(product.Weight + category.Weight, Is.EqualTo(2.0633685835066903D).Within(0.000001D));
            Assert.That(productHeader.Weight + categoryHeader.Weight, Is.EqualTo(1.9969851504697629D).Within(0.000001D));
        });
    }

    [Test]
    public void Factory_PreparesOrderReportWithTrimmedParameterAndConfiguredConnection()
    {
        var factory = new ReportFactoryService(
            new ConfigurationBuilder().Build(),
            null!);
        var method = typeof(ReportFactoryService).GetMethod(
            "PrepareCustomerOrderReport",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new AssertionException("Customer Order report preparation was not found.");

        using var report = (CustomerOrderPrint)(method.Invoke(factory, new object[] { "  CO-42  " })
            ?? throw new AssertionException("Customer Order report was null."));
        var dataSource = GetPrivateField<SqlDataSource>(report, "sqlDataSource1");

        Assert.Multiple(() =>
        {
            Assert.That(report.Parameters["pOrderNbr"].Value, Is.EqualTo("CO-42"));
            Assert.That(dataSource.ConnectionName, Is.EqualTo("ReportDBCon01"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Factory_RejectsBlankOrderNumber(string? orderNbr)
    {
        var factory = new ReportFactoryService(
            new ConfigurationBuilder().Build(),
            null!);

        Assert.That(
            () => factory.CreateCustomerOrderReport(orderNbr!),
            Throws.InstanceOf<ArgumentException>());
    }

    [TestCase(false, false, 0, null, false, TestName = "PrintDisabled_ForNewOrder")]
    [TestCase(true, false, 42, "CO-42", true, TestName = "PrintEnabled_ForLoadedOrder")]
    [TestCase(true, true, 42, "CO-42", false, TestName = "PrintDisabled_WhilePersistedOrderIsBeingEdited")]
    [TestCase(false, false, 0, "", false, TestName = "PrintDisabled_AfterResetState")]
    public void PrintEligibility_RequiresUnmodifiedPersistedOrder(
        bool isExistingOrder,
        bool isEditMode,
        int gkey,
        string? orderNbr,
        bool expected)
    {
        var viewModel = (CustomerOrderViewModel)
            RuntimeHelpers.GetUninitializedObject(typeof(CustomerOrderViewModel));

        SetPrivateField(viewModel, "_header", new CustomerOrder
        {
            GKey = gkey,
            OrderNbr = orderNbr
        });
        SetPrivateField(viewModel, "_isExistingOrder", isExistingOrder);
        SetPrivateField(viewModel, "_isEditMode", isEditMode);
        SetPrivateField(viewModel, "_isPreparingCustomerOrderReport", false);

        var method = typeof(CustomerOrderViewModel).GetMethod(
            "CanPrintCustomerOrder",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new AssertionException("Customer Order print eligibility was not found.");

        Assert.That(method.Invoke(viewModel, null), Is.EqualTo(expected));
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new AssertionException($"Field {fieldName} was not found.");

        return (T)(field.GetValue(instance)
            ?? throw new AssertionException($"Field {fieldName} was null."));
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var field = instance.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new AssertionException($"Field {fieldName} was not found.");

        field.SetValue(instance, value);
    }
}
