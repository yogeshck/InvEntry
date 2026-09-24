using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.OneD;

namespace InvEntry.Controls;

/// <summary>
/// Composes the authoritative 700 x 250 ZPL render into a preview-only
/// 100 x 13 mm jewellery-tag layout. It does not participate in printing.
/// </summary>
public sealed class JewelleryTagPreview : FrameworkElement
{
    public const double CanvasWidth = 1000d;
    public const double CanvasHeight = 130d;
    public const double BodyWidth = 550d;
    public const double FoldX = 275d;
    public const double TailStartX = 550d;
    public const double ContentLeftGutter = 12d;
    public const double ContentTop = 10d;
    public const int LeftSourceX = 0;
    public const int RightSourceX = 250;
    public const int SectionSourceWidth = 250;
    public const int SectionSourceHeight = 110;

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(BitmapSource), typeof(JewelleryTagPreview),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ZplProperty = DependencyProperty.Register(
        nameof(Zpl), typeof(string), typeof(JewelleryTagPreview),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(PixelPerfectImageMode), typeof(JewelleryTagPreview),
        new FrameworkPropertyMetadata(PixelPerfectImageMode.Fit, FrameworkPropertyMetadataOptions.AffectsRender));

    public BitmapSource? Source
    {
        get => (BitmapSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string? Zpl
    {
        get => (string?)GetValue(ZplProperty);
        set => SetValue(ZplProperty, value);
    }

    public PixelPerfectImageMode Mode
    {
        get => (PixelPerfectImageMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public JewelleryTagPreview()
    {
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Mode == PixelPerfectImageMode.ActualPixels)
            return new Size(CanvasWidth, CanvasHeight);

        double width = double.IsInfinity(availableSize.Width) ? CanvasWidth : availableSize.Width;
        double height = double.IsInfinity(availableSize.Height) ? CanvasHeight : availableSize.Height;
        double scale = CalculateScale(width, height, Mode);
        return new Size(CanvasWidth * scale, CanvasHeight * scale);
    }

    public static double CalculateScale(double availableWidth, double availableHeight, PixelPerfectImageMode mode) =>
        mode == PixelPerfectImageMode.ActualPixels
            ? 1d
            : PixelPerfectImage.CalculateFitScale(
                availableWidth, availableHeight, CanvasWidth, CanvasHeight);

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        double scale = CalculateScale(ActualWidth, ActualHeight, Mode);
        double width = CanvasWidth * scale;
        double height = CanvasHeight * scale;
        double originX = Math.Round((ActualWidth - width) / 2d);
        double originY = Math.Round((ActualHeight - height) / 2d);

        drawing.PushTransform(new TranslateTransform(originX, originY));
        drawing.PushTransform(new ScaleTransform(scale, scale));
        DrawTag(drawing);
        drawing.Pop();
        drawing.Pop();
    }

    private void DrawTag(DrawingContext drawing)
    {
        const double tailHeight = 30d;
        var outlinePen = new Pen(new SolidColorBrush(Color.FromRgb(100, 116, 139)), 1d);
        var foldPen = new Pen(new SolidColorBrush(Color.FromRgb(100, 116, 139)), 1d) { DashStyle = DashStyles.Dash };

        drawing.DrawRoundedRectangle(Brushes.White, outlinePen,
            new Rect(0, 0, BodyWidth, CanvasHeight), 20d, 20d);
        drawing.DrawRoundedRectangle(Brushes.White, outlinePen,
            new Rect(BodyWidth - 1d, (CanvasHeight - tailHeight) / 2d, CanvasWidth - BodyWidth + 1d, tailHeight),
            tailHeight / 2d, tailHeight / 2d);

        if (Source is not null && Source.PixelWidth >= 500 && Source.PixelHeight >= SectionSourceHeight)
        {
            drawing.PushClip(new RectangleGeometry(new Rect(1, 1, BodyWidth - 2d, CanvasHeight - 2d), 19d, 19d));
            var left = new CroppedBitmap(Source,
                new Int32Rect(LeftSourceX, 0, SectionSourceWidth, SectionSourceHeight));
            var right = new CroppedBitmap(Source,
                new Int32Rect(RightSourceX, 0, SectionSourceWidth, SectionSourceHeight));
            drawing.DrawImage(left, new Rect(ContentLeftGutter, ContentTop, SectionSourceWidth, SectionSourceHeight));
            drawing.DrawImage(right, new Rect(FoldX + ContentLeftGutter, ContentTop, SectionSourceWidth, SectionSourceHeight));
            drawing.Pop();
        }

        // The gutter keeps content clear of the fold; draw the fold over white only.
        drawing.DrawLine(foldPen, new Point(FoldX, 2d), new Point(FoldX, CanvasHeight - 2d));

        PhysicalTagOverflow overflow = DetectOverflow(Zpl);
        if (overflow != PhysicalTagOverflow.None)
            DrawOverflow(drawing, overflow, FoldX);
    }

    private static void DrawOverflow(DrawingContext drawing, PhysicalTagOverflow overflow, double FoldX)
    {
        var brush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        if ((overflow & PhysicalTagOverflow.Left) != 0)
            drawing.DrawGeometry(brush, null, Triangle(FoldX - 18d));
        if ((overflow & PhysicalTagOverflow.Right) != 0)
            drawing.DrawGeometry(brush, null, Triangle(FoldX * 2d - 18d));

        static Geometry Triangle(double x)
        {
            var geometry = new StreamGeometry();
            using StreamGeometryContext context = geometry.Open();
            context.BeginFigure(new Point(x, 4d), true, true);
            context.LineTo(new Point(x + 14d, 4d), true, false);
            context.LineTo(new Point(x + 14d, 18d), true, false);
            geometry.Freeze();
            return geometry;
        }
    }

    public static PhysicalTagOverflow DetectOverflow(string? zpl)
    {
        if (string.IsNullOrWhiteSpace(zpl)) return PhysicalTagOverflow.None;

        int x = 0, y = 0, fontHeight = 20, moduleWidth = 1, barcodeHeight = 40;
        bool barcodeField = false;
        PhysicalTagOverflow result = PhysicalTagOverflow.None;
        foreach (string command in zpl.Split('^', StringSplitOptions.RemoveEmptyEntries))
        {
            if (command.StartsWith("FO", StringComparison.Ordinal))
            {
                string[] values = command[2..].Split(',', 2);
                _ = int.TryParse(values.ElementAtOrDefault(0), out x);
                _ = int.TryParse(values.ElementAtOrDefault(1), out y);
            }
            else if (command.StartsWith("BY", StringComparison.Ordinal))
            {
                string[] values = command[2..].Split(',');
                if (int.TryParse(values.ElementAtOrDefault(0), out int width)) moduleWidth = Math.Max(1, width);
                if (int.TryParse(values.ElementAtOrDefault(2), out int height)) barcodeHeight = height;
            }
            else if (command.StartsWith("BC", StringComparison.Ordinal))
            {
                string[] values = command[2..].Split(',');
                if (int.TryParse(values.ElementAtOrDefault(1), out int height)) barcodeHeight = height;
                barcodeField = true;
            }
            else if (command.StartsWith("A0", StringComparison.Ordinal))
            {
                string[] values = command[2..].Split(',');
                if (int.TryParse(values.ElementAtOrDefault(1), out int height)) fontHeight = height;
                barcodeField = false;
            }
            else if (command.StartsWith("FD", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(command[2..]))
            {
                string value = command[2..];
                double elementWidth = barcodeField ? BarcodeWidth(value, moduleWidth) : TextWidth(value, fontHeight);
                int sectionStart = x < RightSourceX ? LeftSourceX : RightSourceX;
                bool overflow = x < sectionStart || x + elementWidth > sectionStart + SectionSourceWidth ||
                                y < 0 || y + (barcodeField ? barcodeHeight : fontHeight) > SectionSourceHeight;
                if (overflow)
                    result |= sectionStart == LeftSourceX ? PhysicalTagOverflow.Left : PhysicalTagOverflow.Right;
                barcodeField = false;
            }
        }
        return result;
    }

    private static int BarcodeWidth(string value, int moduleWidth)
    {
        var hints = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.MARGIN] = 0,
            [EncodeHintType.PURE_BARCODE] = true
        };
        BitMatrix matrix = new Code128Writer().encode(value, BarcodeFormat.CODE_128, 0, 1, hints);
        return matrix.Width * moduleWidth;
    }

    private static double TextWidth(string value, int fontHeight) => new FormattedText(
        value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
        new Typeface("Segoe UI"), fontHeight, Brushes.Black, 1d).WidthIncludingTrailingWhitespace;
}

[Flags]
public enum PhysicalTagOverflow
{
    None = 0,
    Left = 1,
    Right = 2
}