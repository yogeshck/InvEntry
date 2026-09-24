using System;
using System.Collections.Generic;
using System.Linq;
using InvEntry.Utils;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.OneD;

namespace InvEntry.Services.Printing;

public sealed record LabelPreviewResult(string Zpl, BitmapSource Image);

public interface ILabelPreviewRenderer
{
    LabelPreviewResult Render(LabelPrintRequest request);
}

public sealed partial class ZplLabelPreviewRenderer : ILabelPreviewRenderer
{
    // WPF drawing coordinates are device-independent pixels. Rendering at 96 DPI
    // preserves the required one-ZPL-dot-to-one-bitmap-pixel mapping.
    private const double WpfRasterDpi = 96d;

    public LabelPreviewResult Render(LabelPrintRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string zpl = BarCodePrint.CreateZpl(
            request.ProductCode,
            request.ProductName,
            request.VaPercent,
            request.ProductWeight,
            request.StoneWeight,
            request.ProductPurity,
            request.CompanyName);

        return new LabelPreviewResult(zpl, RenderZpl(zpl));
    }

    internal static BitmapSource RenderZpl(string zpl)
    {
        int width = ReadInt(zpl, PwRegex(), 700);
        int height = ReadInt(zpl, LlRegex(), 250);
        var visual = new DrawingVisual();
        RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased);

        using (DrawingContext drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

            int x = 0;
            int y = 0;
            int fontHeight = 20;
            int fontWidth = 20;
            int moduleWidth = 1;
            int barcodeHeight = 40;
            bool barcodeField = false;

            foreach (string command in zpl.Split('^', StringSplitOptions.RemoveEmptyEntries))
            {
                if (command.StartsWith("FO", StringComparison.Ordinal))
                {
                    string[] coordinates = command[2..].Split(',', 2);
                    _ = int.TryParse(coordinates.ElementAtOrDefault(0), out x);
                    _ = int.TryParse(coordinates.ElementAtOrDefault(1), out y);
                }
                else if (command.StartsWith("BY", StringComparison.Ordinal))
                {
                    string[] values = command[2..].Split(',');
                    if (int.TryParse(values.ElementAtOrDefault(0), out int parsedWidth))
                        moduleWidth = Math.Max(1, parsedWidth);
                    if (int.TryParse(values.ElementAtOrDefault(2), out int parsedHeight))
                        barcodeHeight = parsedHeight;
                }
                else if (command.StartsWith("BC", StringComparison.Ordinal))
                {
                    string[] values = command[2..].Split(',');
                    if (int.TryParse(values.ElementAtOrDefault(1), out int parsedHeight))
                        barcodeHeight = parsedHeight;
                    barcodeField = true;
                }
                else if (command.StartsWith("A0", StringComparison.Ordinal))
                {
                    string[] values = command[2..].Split(',');
                    if (int.TryParse(values.ElementAtOrDefault(1), out int parsedHeight))
                        fontHeight = parsedHeight;
                    if (int.TryParse(values.ElementAtOrDefault(2), out int parsedWidth))
                        fontWidth = parsedWidth;
                    barcodeField = false;
                }
                else if (command.StartsWith("FD", StringComparison.Ordinal))
                {
                    string value = command[2..];
                    if (barcodeField)
                    {
                        DrawCode128(drawing, value, x, y, barcodeHeight, moduleWidth);
                        barcodeField = false;
                    }
                    else if (!string.IsNullOrWhiteSpace(value))
                    {
                        var text = new FormattedText(
                            value,
                            CultureInfo.InvariantCulture,
                            FlowDirection.LeftToRight,
                            // This is a local approximation only. Zebra font 0 remains
                            // authoritative for native physical-printer output.
                            new Typeface("Segoe UI"),
                            fontHeight,
                            Brushes.Black,
                            1d)
                        {
                            MaxTextWidth = Math.Max(fontWidth, width - x)
                        };
                        drawing.DrawText(text, new Point(x, y));
                    }
                }
            }
        }

        var bitmap = new RenderTargetBitmap(width, height, WpfRasterDpi, WpfRasterDpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void DrawCode128(
        DrawingContext drawing,
        string value,
        int x,
        int y,
        int height,
        int moduleWidth)
    {
        var hints = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.MARGIN] = 0,
            [EncodeHintType.PURE_BARCODE] = true
        };
        BitMatrix matrix = new Code128Writer().encode(
            value,
            BarcodeFormat.CODE_128,
            0,
            Math.Max(1, height),
            hints);

        for (int column = 0; column < matrix.Width; column++)
        {
            if (matrix[column, 0])
            {
                drawing.DrawRectangle(
                    Brushes.Black,
                    null,
                    new Rect(x + column * moduleWidth, y, moduleWidth, height));
            }
        }
    }

    private static int ReadInt(string value, Regex regex, int fallback) =>
        int.TryParse(regex.Match(value).Groups[1].Value, out int parsed) ? parsed : fallback;

    [GeneratedRegex(@"\^PW(\d+)")]
    private static partial Regex PwRegex();

    [GeneratedRegex(@"\^LL(\d+)")]
    private static partial Regex LlRegex();
}