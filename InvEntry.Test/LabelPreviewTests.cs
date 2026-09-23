using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using InvEntry.Models;
using InvEntry.Services.Printing;
using InvEntry.Utils;
using InvEntry.ViewModels;
using ZXing;
using ZXing.Common;

namespace InvEntry.Test;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public class LabelPreviewTests
{
    private static readonly LabelPrintRequest Request = new(
        "GBL2-0176", "Gold Bangle", 12.5m, 10.444m, 0.111m, "916", "MATHA");

    [Test]
    public void Renderer_UsesProductionZplAndCurrentValues()
    {
        var renderer = new ZplLabelPreviewRenderer();
        var result = renderer.Render(Request);
        string productionZpl = BarCodePrint.CreateZpl(
            Request.ProductCode, Request.ProductName, Request.VaPercent,
            Request.ProductWeight, Request.StoneWeight,
            Request.ProductPurity, Request.CompanyName);

        Assert.Multiple(() =>
        {
            Assert.That(result.Zpl, Is.EqualTo(productionZpl));
            Assert.That(result.Zpl, Does.Contain("^PW700"));
            Assert.That(result.Zpl, Does.Contain("^LL250"));
            Assert.That(result.Zpl, Does.Contain("^BCN,40,N,N,N"));
            Assert.That(result.Zpl, Does.Contain("GBL2-0176"));
            Assert.That(result.Zpl, Does.Contain("Gwt: 10.444"));
            Assert.That(result.Zpl, Does.Contain("Stone: 0.111"));
            Assert.That(result.Image.PixelWidth, Is.EqualTo(700));
            Assert.That(result.Image.PixelHeight, Is.EqualTo(250));
        });
    }

    [Test]
    public void RenderedBarcode_DecodesAsExistingCode128Value()
    {
        var image = new ZplLabelPreviewRenderer().Render(Request).Image;
        const int width = 245;
        const int height = 48;
        const int scale = 4;
        const int padding = 40;
        int stride = width * 4;
        var pixels = new byte[stride * height];
        image.CopyPixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

        int scaledWidth = width * scale + padding * 2;
        int scaledHeight = height * scale + padding * 2;
        var scaled = Enumerable.Repeat((byte)255, scaledWidth * scaledHeight * 4).ToArray();
        for (int sourceY = 0; sourceY < height; sourceY++)
        for (int sourceX = 0; sourceX < width; sourceX++)
        for (int offsetY = 0; offsetY < scale; offsetY++)
        for (int offsetX = 0; offsetX < scale; offsetX++)
        {
            int sourceOffset = sourceY * stride + sourceX * 4;
            int targetX = padding + sourceX * scale + offsetX;
            int targetY = padding + sourceY * scale + offsetY;
            int targetOffset = (targetY * scaledWidth + targetX) * 4;
            Array.Copy(pixels, sourceOffset, scaled, targetOffset, 4);
        }

        var source = new RGBLuminanceSource(
            scaled, scaledWidth, scaledHeight, RGBLuminanceSource.BitmapFormat.BGRA32);
        var reader = new BarcodeReaderGeneric
        {
            Options = new DecodingOptions
            {
                PossibleFormats = new[] { BarcodeFormat.CODE_128 },
                TryHarder = true
            }
        };

        var decoded = reader.Decode(source);
        Assert.That(decoded?.Text, Is.EqualTo(Request.ProductCode));
    }

    [Test]
    public void WeightChange_InvalidatesPreviewWithoutChangingStockIdentity()
    {
        var viewModel = (ProductStockEntryViewModel)
            RuntimeHelpers.GetUninitializedObject(typeof(ProductStockEntryViewModel));
        var line = new GrnLine
        {
            ProductStockGkey = 2608,
            ProductSku = "GBL2-0176",
            GrossWeight = 10m,
            StoneWeight = 1m
        };
        viewModel.SelectedGrnLine = line;
        viewModel.LabelPreviewZpl = "old preview";
        viewModel.HasLabelPreview = true;
        var command = typeof(ProductStockEntryViewModel).GetMethod(
            "CalculateCurrentWeights", BindingFlags.Instance | BindingFlags.NonPublic);

        command!.Invoke(viewModel, null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasLabelPreview, Is.False);
            Assert.That(viewModel.LabelPreviewZpl, Is.Null);
            Assert.That(line.NetWeight, Is.EqualTo(9m));
            Assert.That(line.ProductStockGkey, Is.EqualTo(2608));
            Assert.That(line.ProductSku, Is.EqualTo("GBL2-0176"));
            Assert.That(line.IsPrinted, Is.False);
        });
    }

    [Test]
    public void PreviewUi_KeepsPreviewAndPrintAsSeparateExplicitCommands()
    {
        string xaml = File.ReadAllText(FindViewPath());

        Assert.Multiple(() =>
        {
            Assert.That(xaml, Does.Contain("Command=\"{Binding PreviewTagCommand}\""));
            Assert.That(xaml, Does.Contain("Command=\"{Binding PrintTagCommand}\""));
            Assert.That(xaml, Does.Contain("Header=\"Visual Preview\""));
            Assert.That(xaml, Does.Contain("Header=\"ZPL Text\""));
        });
    }

    [Test]
    public void PreviewCommand_HasNoPrinterOrFinalizationCalls()
    {
        string source = File.ReadAllText(FindViewModelPath());
        int start = source.IndexOf("private async Task PreviewTagAsync", StringComparison.Ordinal);
        int end = source.IndexOf("private void InvalidateLabelPreview", start, StringComparison.Ordinal);
        string previewMethod = source[start..end];

        Assert.Multiple(() =>
        {
            Assert.That(previewMethod, Does.Not.Contain("PrintAsync"));
            Assert.That(previewMethod, Does.Not.Contain("ReserveProductSku"));
            Assert.That(previewMethod, Does.Not.Contain("UpdateProductStock"));
            Assert.That(previewMethod, Does.Not.Contain("CreateGrnLine"));
            Assert.That(previewMethod, Does.Not.Contain("GrnLineList.Remove"));
            Assert.That(previewMethod, Does.Not.Contain("SelectNextPrintableLine"));
        });
    }

    private static string FindViewModelPath() =>
        FindRepositoryFile("InvEntry", "ViewModels", "ProductStockEntryViewModel.cs");
    private static string FindViewPath()
    {
        return FindRepositoryFile("InvEntry", "Views", "ProductStockEntryView.xaml");
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Repository file was not found: {string.Join('/', segments)}");
    }
}