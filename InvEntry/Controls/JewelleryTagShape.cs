using System;
using System.Windows;
using System.Windows.Media;

namespace InvEntry.Controls;

/// <summary>
/// Preview-only outline of the 100 x 13 mm BAR-Coding TECH jewellery tag.
/// Dimensions are in millimetres and scale uniformly. This control never
/// participates in physical printing.
/// </summary>
public sealed class JewelleryTagShape : FrameworkElement
{
    public const double OverallLengthMm = 100d;
    public const double MaximumHeightMm = 13d;
    public const double PrintableBodyLengthMm = 55d;
    public const double AttachmentTailLengthMm = 45d;
    public const double FoldLineMm = 27.5d;
    public const double AttachmentTailHeightMm = 3d;

    public JewelleryTagShape()
    {
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        MinHeight = 52d;
    }

    public static double CalculateUniformScale(double availableWidth, double availableHeight) =>
        Math.Max(0d, Math.Min(availableWidth / OverallLengthMm, availableHeight / MaximumHeightMm));

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsInfinity(availableSize.Width) ? 800d : availableSize.Width;
        double height = double.IsInfinity(availableSize.Height)
            ? width * MaximumHeightMm / OverallLengthMm
            : availableSize.Height;
        double scale = CalculateUniformScale(width, height);
        return new Size(OverallLengthMm * scale, MaximumHeightMm * scale);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        double scale = CalculateUniformScale(ActualWidth, ActualHeight);
        if (scale <= 0d) return;

        double width = OverallLengthMm * scale;
        double height = MaximumHeightMm * scale;
        double originX = (ActualWidth - width) / 2d;
        double originY = (ActualHeight - height) / 2d;
        double bodyWidth = PrintableBodyLengthMm * scale;
        double tailHeight = AttachmentTailHeightMm * scale;
        double tailY = originY + (height - tailHeight) / 2d;
        double radius = Math.Min(2d * scale, height / 2d);
        var outlinePen = new Pen(new SolidColorBrush(Color.FromRgb(100, 116, 139)), 1d);
        var foldPen = new Pen(new SolidColorBrush(Color.FromRgb(148, 163, 184)), 1d) { DashStyle = DashStyles.Dash };

        drawingContext.DrawRoundedRectangle(Brushes.White, outlinePen,
            new Rect(originX, originY, bodyWidth, height), radius, radius);
        drawingContext.DrawRoundedRectangle(Brushes.White, outlinePen,
            new Rect(originX + bodyWidth - 1d, tailY, AttachmentTailLengthMm * scale + 1d, tailHeight),
            tailHeight / 2d, tailHeight / 2d);
        drawingContext.DrawLine(foldPen,
            new Point(originX + FoldLineMm * scale, originY + 1d),
            new Point(originX + FoldLineMm * scale, originY + height - 1d));
    }
}