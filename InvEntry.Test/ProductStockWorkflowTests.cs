using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using InvEntry.Models;
using InvEntry.ViewModels;

namespace InvEntry.Test;

[TestFixture]
public class ProductStockWorkflowTests
{
    [Test]
    public void View_UsesSelectionAndWeighingTabs()
    {
        var document = XDocument.Load(FindViewPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var workflowTabControl = document.Descendants(presentation + "TabControl").First();
        var tabs = workflowTabControl.Elements(presentation + "TabItem").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(tabs, Has.Count.EqualTo(2));
            Assert.That((string?)tabs[0].Attribute("Header"), Does.Contain("GRN SELECTION"));
            Assert.That((string?)tabs[1].Attribute("Header"), Does.Contain("WEIGHING & TAGGING"));
            Assert.That(document.ToString(), Does.Contain("ContinueToWeighingCommand"));
        });
    }

    [Test]
    public void View_HidesPendingRowsAndShowsReadOnlyCompletedItems()
    {
        string xaml = File.ReadAllText(FindViewPath());

        Assert.Multiple(() =>
        {
            Assert.That(xaml, Does.Not.Contain("ItemsSource=\"{Binding GrnLineList}\""));
            Assert.That(xaml, Does.Contain("ItemsSource=\"{Binding CompletedItems}\""));
            Assert.That(xaml, Does.Contain("AllowEditing=\"False\""));
        });
    }

    [Test]
    public void View_ExposesOnlyExplicitPrintTagInvocation()
    {
        string xaml = File.ReadAllText(FindViewPath());

        Assert.Multiple(() =>
        {
            Assert.That(CountOccurrences(xaml, "PrintTagCommand"), Is.EqualTo(1));
            Assert.That(xaml, Does.Contain("CalculateCurrentWeightsCommand"));
            Assert.That(xaml, Does.Not.Contain("ShowingEditor"));
        });
    }

    [Test]
    public void WeightCalculation_OnlyUpdatesRoundedNetWeight()
    {
        var viewModel = (ProductStockEntryViewModel)
            RuntimeHelpers.GetUninitializedObject(typeof(ProductStockEntryViewModel));
        var line = new GrnLine
        {
            ProductStockGkey = 2608,
            ProductSku = "GBL2-0176",
            GrossWeight = 10.5555m,
            StoneWeight = 0.1111m,
            IsPrinted = false
        };
        var method = typeof(ProductStockEntryViewModel).GetMethod(
            "EvaluateGrnLine", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        method!.Invoke(viewModel, new object[] { line });

        Assert.Multiple(() =>
        {
            Assert.That(line.NetWeight, Is.EqualTo(10.444m));
            Assert.That(line.ProductStockGkey, Is.EqualTo(2608));
            Assert.That(line.ProductSku, Is.EqualTo("GBL2-0176"));
            Assert.That(line.IsPrinted, Is.False);
        });
    }

    private static int CountOccurrences(string source, string value) =>
        (source.Length - source.Replace(value, string.Empty).Length) / value.Length;

    private static string FindViewPath()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName, "InvEntry", "Views", "ProductStockEntryView.xaml");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("ProductStockEntryView.xaml was not found.");
    }
}