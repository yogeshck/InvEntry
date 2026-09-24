using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace InvEntry.Controls;

public enum PixelPerfectImageMode
{
    Fit,
    ActualPixels
}

/// <summary>
/// Draws a bitmap without changing its aspect ratio. Fit mode uses discrete
/// integer (or reciprocal-integer) scales so printer-dot-sized features are
/// not distorted independently by the WPF layout system.
/// </summary>
public sealed class PixelPerfectImage : FrameworkElement
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(BitmapSource), typeof(PixelPerfectImage),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(PixelPerfectImageMode), typeof(PixelPerfectImage),
        new FrameworkPropertyMetadata(PixelPerfectImageMode.Fit, FrameworkPropertyMetadataOptions.AffectsRender));

    public BitmapSource Source
    {
        get => (BitmapSource)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public PixelPerfectImageMode Mode
    {
        get => (PixelPerfectImageMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public PixelPerfectImage()
    {
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (Source is null || Source.PixelWidth <= 0 || Source.PixelHeight <= 0)
            return;

        double scale = Mode == PixelPerfectImageMode.ActualPixels
            ? 1d
            : CalculateFitScale(ActualWidth, ActualHeight, Source.PixelWidth, Source.PixelHeight);
        double width = Source.PixelWidth * scale;
        double height = Source.PixelHeight * scale;
        double x = Math.Round((ActualWidth - width) / 2d);
        double y = Math.Round((ActualHeight - height) / 2d);

        drawingContext.PushClip(new RectangleGeometry(new Rect(RenderSize)));
        drawingContext.DrawImage(Source, new Rect(x, y, width, height));
        drawingContext.Pop();
    }

    public static double CalculateFitScale(
        double availableWidth,
        double availableHeight,
        double bitmapWidth,
        double bitmapHeight)
    {
        if (availableWidth <= 0d || availableHeight <= 0d || bitmapWidth <= 0d || bitmapHeight <= 0d)
            return 1d;

        double availableScale = Math.Min(availableWidth / bitmapWidth, availableHeight / bitmapHeight);
        if (availableScale >= 1d)
            return Math.Max(1d, Math.Floor(availableScale));

        return 1d / Math.Ceiling(1d / availableScale);
    }
}
