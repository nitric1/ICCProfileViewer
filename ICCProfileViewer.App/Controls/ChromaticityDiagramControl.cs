using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using ICCProfileViewer.Core.Colorimetry;
using ICCProfileViewer.Core.ReferenceGamuts;

namespace ICCProfileViewer.App.Controls;

public sealed class ChromaticityDiagramControl : Control
{
    public static readonly StyledProperty<ChromaticityDiagramType> DiagramTypeProperty =
        AvaloniaProperty.Register<ChromaticityDiagramControl, ChromaticityDiagramType>(
            nameof(DiagramType));

    public static readonly StyledProperty<GamutBoundary?> ProfileGamutProperty =
        AvaloniaProperty.Register<ChromaticityDiagramControl, GamutBoundary?>(
            nameof(ProfileGamut));

    public static readonly StyledProperty<bool> ShowSrgbProperty =
        AvaloniaProperty.Register<ChromaticityDiagramControl, bool>(nameof(ShowSrgb), true);

    public static readonly StyledProperty<bool> ShowDisplayP3Property =
        AvaloniaProperty.Register<ChromaticityDiagramControl, bool>(nameof(ShowDisplayP3));

    public static readonly StyledProperty<bool> ShowDciP3Property =
        AvaloniaProperty.Register<ChromaticityDiagramControl, bool>(nameof(ShowDciP3));

    public static readonly StyledProperty<bool> ShowAdobeRgbProperty =
        AvaloniaProperty.Register<ChromaticityDiagramControl, bool>(nameof(ShowAdobeRgb));

    public static readonly StyledProperty<bool> ShowBt2020Property =
        AvaloniaProperty.Register<ChromaticityDiagramControl, bool>(nameof(ShowBt2020));

    public static readonly StyledProperty<bool> ShowWhitePointsProperty =
        AvaloniaProperty.Register<ChromaticityDiagramControl, bool>(nameof(ShowWhitePoints), true);

    private const double PlotPadding = 46;
    private const int RasterQuantum = 64;
    private const int MaximumRasterDimension = 512;
    private const int MaximumCachedRasters = 4;

    private static readonly OverlayStyle SrgbStyle = new(
        ReferenceGamutCatalog.Srgb,
        Color.Parse("#0078D4"),
        new DashStyle([8, 4], 0));

    private static readonly OverlayStyle DisplayP3Style = new(
        ReferenceGamutCatalog.DisplayP3,
        Color.Parse("#00A36C"),
        new DashStyle([3, 3], 0));

    private static readonly OverlayStyle DciP3Style = new(
        ReferenceGamutCatalog.DciP3,
        Color.Parse("#E67E22"),
        new DashStyle([10, 3, 2, 3], 0));

    private static readonly OverlayStyle AdobeRgbStyle = new(
        ReferenceGamutCatalog.AdobeRgb1998,
        Color.Parse("#8E44AD"),
        new DashStyle([2, 3], 0));

    private static readonly OverlayStyle Bt2020Style = new(
        ReferenceGamutCatalog.Bt2020,
        Color.Parse("#D81B60"),
        new DashStyle([12, 4], 0));

    private readonly Dictionary<RasterCacheKey, WriteableBitmap> backgroundCache = new();
    private DiagramCoordinate? hoverDataCoordinate;
    private Point hoverViewportPoint;

    static ChromaticityDiagramControl()
    {
        AffectsRender<ChromaticityDiagramControl>(
            DiagramTypeProperty,
            ProfileGamutProperty,
            ShowSrgbProperty,
            ShowDisplayP3Property,
            ShowDciP3Property,
            ShowAdobeRgbProperty,
            ShowBt2020Property,
            ShowWhitePointsProperty);
    }

    public ChromaticityDiagramType DiagramType
    {
        get => GetValue(DiagramTypeProperty);
        set => SetValue(DiagramTypeProperty, value);
    }

    public GamutBoundary? ProfileGamut
    {
        get => GetValue(ProfileGamutProperty);
        set => SetValue(ProfileGamutProperty, value);
    }

    public bool ShowSrgb
    {
        get => GetValue(ShowSrgbProperty);
        set => SetValue(ShowSrgbProperty, value);
    }

    public bool ShowDisplayP3
    {
        get => GetValue(ShowDisplayP3Property);
        set => SetValue(ShowDisplayP3Property, value);
    }

    public bool ShowDciP3
    {
        get => GetValue(ShowDciP3Property);
        set => SetValue(ShowDciP3Property, value);
    }

    public bool ShowAdobeRgb
    {
        get => GetValue(ShowAdobeRgbProperty);
        set => SetValue(ShowAdobeRgbProperty, value);
    }

    public bool ShowBt2020
    {
        get => GetValue(ShowBt2020Property);
        set => SetValue(ShowBt2020Property, value);
    }

    public bool ShowWhitePoints
    {
        get => GetValue(ShowWhitePointsProperty);
        set => SetValue(ShowWhitePointsProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bounds.Width <= PlotPadding * 2 || Bounds.Height <= PlotPadding * 2)
        {
            return;
        }

        var dataBounds = ChromaticityDiagramCoordinateSystem.GetBounds(DiagramType);
        var layout = DiagramPlotLayout.Create(
            dataBounds,
            Bounds.Width,
            Bounds.Height,
            PlotPadding);
        var theme = CreateTheme();
        var plotRect = ToRect(layout.PlotArea);

        context.FillRectangle(theme.PlotBackground, plotRect);
        DrawBackground(context, layout, plotRect);
        DrawGridAndAxes(context, layout, theme);
        DrawSpectralLocus(context, layout, theme);
        DrawReferenceGamuts(context, layout, theme);
        DrawProfileGamut(context, layout, theme);
        DrawHoverTooltip(context, layout, theme);
    }

    protected override void OnPointerMoved(PointerEventArgs eventArgs)
    {
        base.OnPointerMoved(eventArgs);
        hoverViewportPoint = eventArgs.GetPosition(this);
        if (!TryCreateLayout(out var layout) ||
            !layout.TryUnproject(
                new DiagramCoordinate(hoverViewportPoint.X, hoverViewportPoint.Y),
                out var coordinate))
        {
            hoverDataCoordinate = null;
        }
        else
        {
            hoverDataCoordinate = coordinate;
        }

        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs eventArgs)
    {
        base.OnPointerExited(eventArgs);
        hoverDataCoordinate = null;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs eventArgs)
    {
        base.OnPointerPressed(eventArgs);
        if (eventArgs.ClickCount != 2 || hoverDataCoordinate is not { } coordinate)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            _ = clipboard.SetTextAsync(FormatCoordinate(coordinate));
            eventArgs.Handled = true;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs eventArgs)
    {
        foreach (var bitmap in backgroundCache.Values)
        {
            bitmap.Dispose();
        }

        backgroundCache.Clear();
        base.OnDetachedFromVisualTree(eventArgs);
    }

    private void DrawBackground(
        DrawingContext context,
        DiagramPlotLayout layout,
        Rect plotRect)
    {
        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var pixelWidth = QuantizeRasterDimension(layout.PlotArea.Width * scaling);
        var aspectRatio = layout.DataBounds.Height / layout.DataBounds.Width;
        var pixelHeight = Math.Max(1, (int)Math.Round(pixelWidth * aspectRatio));
        if (pixelHeight > MaximumRasterDimension)
        {
            pixelHeight = MaximumRasterDimension;
            pixelWidth = Math.Max(1, (int)Math.Round(pixelHeight / aspectRatio));
        }

        var key = new RasterCacheKey(DiagramType, pixelWidth, pixelHeight);
        if (!backgroundCache.TryGetValue(key, out var bitmap))
        {
            bitmap = CreateBackgroundBitmap(key);
            if (backgroundCache.Count >= MaximumCachedRasters)
            {
                using var enumerator = backgroundCache.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    enumerator.Current.Value.Dispose();
                    backgroundCache.Remove(enumerator.Current.Key);
                }
            }

            backgroundCache.Add(key, bitmap);
        }

        context.DrawImage(
            bitmap,
            new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height),
            plotRect);
    }

    private static int QuantizeRasterDimension(double requestedPixels)
    {
        var quantized = (int)Math.Ceiling(requestedPixels / RasterQuantum) * RasterQuantum;
        return Math.Clamp(quantized, RasterQuantum, MaximumRasterDimension);
    }

    private static WriteableBitmap CreateBackgroundBitmap(RasterCacheKey key)
    {
        var raster = ChromaticityDiagramRasterizer.Rasterize(
            key.DiagramType,
            key.PixelWidth,
            key.PixelHeight);
        var bitmap = new WriteableBitmap(
            new PixelSize(key.PixelWidth, key.PixelHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        var source = raster.BgraPixels.ToArray();

        using var framebuffer = bitmap.Lock();
        for (var row = 0; row < key.PixelHeight; row++)
        {
            Marshal.Copy(
                source,
                row * raster.Stride,
                IntPtr.Add(framebuffer.Address, row * framebuffer.RowBytes),
                raster.Stride);
        }

        return bitmap;
    }

    private void DrawGridAndAxes(
        DrawingContext context,
        DiagramPlotLayout layout,
        DiagramTheme theme)
    {
        var plotArea = layout.PlotArea;
        const double tickInterval = 0.1;
        var firstHorizontalTick = Math.Ceiling(layout.DataBounds.MinimumHorizontal / tickInterval) * tickInterval;
        for (var value = firstHorizontalTick;
             value <= layout.DataBounds.MaximumHorizontal + 0.00001;
             value += tickInterval)
        {
            var point = layout.Project(new DiagramCoordinate(value, layout.DataBounds.MinimumVertical));
            context.DrawLine(
                theme.GridPen,
                new Point(point.Horizontal, plotArea.Y),
                new Point(point.Horizontal, plotArea.Bottom));
            DrawText(
                context,
                value.ToString("0.0", CultureInfo.InvariantCulture),
                new Point(point.Horizontal - 10, plotArea.Bottom + 6),
                10,
                theme.SecondaryText);
        }

        var firstVerticalTick = Math.Ceiling(layout.DataBounds.MinimumVertical / tickInterval) * tickInterval;
        for (var value = firstVerticalTick;
             value <= layout.DataBounds.MaximumVertical + 0.00001;
             value += tickInterval)
        {
            var point = layout.Project(new DiagramCoordinate(layout.DataBounds.MinimumHorizontal, value));
            context.DrawLine(
                theme.GridPen,
                new Point(plotArea.X, point.Vertical),
                new Point(plotArea.Right, point.Vertical));
            DrawText(
                context,
                value.ToString("0.0", CultureInfo.InvariantCulture),
                new Point(plotArea.X - 30, point.Vertical - 7),
                10,
                theme.SecondaryText);
        }

        context.DrawRectangle(null, theme.AxisPen, ToRect(plotArea));
        var horizontalLabel = DiagramType == ChromaticityDiagramType.Cie1931Xy ? "x" : "u'";
        var verticalLabel = DiagramType == ChromaticityDiagramType.Cie1931Xy ? "y" : "v'";
        DrawText(
            context,
            horizontalLabel,
            new Point(plotArea.Right + 12, plotArea.Bottom - 8),
            12,
            theme.PrimaryText);
        DrawText(
            context,
            verticalLabel,
            new Point(plotArea.X - 4, plotArea.Y - 24),
            12,
            theme.PrimaryText);
    }

    private void DrawSpectralLocus(
        DrawingContext context,
        DiagramPlotLayout layout,
        DiagramTheme theme)
    {
        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            var first = true;
            foreach (var locusPoint in SpectralLocusCatalog.Cie1931TwoDegree)
            {
                var coordinate = DiagramType == ChromaticityDiagramType.Cie1931Xy
                    ? new DiagramCoordinate(locusPoint.Xy.X, locusPoint.Xy.Y)
                    : new DiagramCoordinate(locusPoint.UvPrime.UPrime, locusPoint.UvPrime.VPrime);
                var point = ToPoint(layout.Project(coordinate));
                if (first)
                {
                    geometryContext.BeginFigure(point, false);
                    first = false;
                }
                else
                {
                    geometryContext.LineTo(point, true);
                }
            }

            geometryContext.EndFigure(true);
        }

        context.DrawGeometry(null, theme.SpectralLocusPen, geometry);
    }

    private void DrawReferenceGamuts(
        DrawingContext context,
        DiagramPlotLayout layout,
        DiagramTheme theme)
    {
        if (ShowSrgb)
        {
            DrawReferenceGamut(context, layout, SrgbStyle, theme);
        }

        if (ShowDisplayP3)
        {
            DrawReferenceGamut(context, layout, DisplayP3Style, theme);
        }

        if (ShowDciP3)
        {
            DrawReferenceGamut(context, layout, DciP3Style, theme);
        }

        if (ShowAdobeRgb)
        {
            DrawReferenceGamut(context, layout, AdobeRgbStyle, theme);
        }

        if (ShowBt2020)
        {
            DrawReferenceGamut(context, layout, Bt2020Style, theme);
        }
    }

    private void DrawReferenceGamut(
        DrawingContext context,
        DiagramPlotLayout layout,
        OverlayStyle style,
        DiagramTheme theme)
    {
        var brush = new SolidColorBrush(style.Color);
        var pen = new Pen(brush, 1.8, style.DashStyle);
        DrawTriangle(
            context,
            layout,
            style.Gamut.Red,
            style.Gamut.Green,
            style.Gamut.Blue,
            pen);
        DrawPoint(context, layout, style.Gamut.Red, brush, theme.PointOutlinePen, 3);
        DrawPoint(context, layout, style.Gamut.Green, brush, theme.PointOutlinePen, 3);
        DrawPoint(context, layout, style.Gamut.Blue, brush, theme.PointOutlinePen, 3);
        if (ShowWhitePoints)
        {
            DrawWhitePoint(context, layout, style.Gamut.WhitePoint, brush, theme);
        }
    }

    private void DrawProfileGamut(
        DrawingContext context,
        DiagramPlotLayout layout,
        DiagramTheme theme)
    {
        if (ProfileGamut is not { } gamut)
        {
            return;
        }

        DrawTriangle(
            context,
            layout,
            gamut.Red.Xy,
            gamut.Green.Xy,
            gamut.Blue.Xy,
            theme.ProfilePen);
        DrawPoint(context, layout, gamut.Red.Xy, theme.ProfileBrush, theme.PointOutlinePen, 4.5);
        DrawPoint(context, layout, gamut.Green.Xy, theme.ProfileBrush, theme.PointOutlinePen, 4.5);
        DrawPoint(context, layout, gamut.Blue.Xy, theme.ProfileBrush, theme.PointOutlinePen, 4.5);
        if (ShowWhitePoints)
        {
            DrawWhitePoint(context, layout, gamut.WhitePoint.Xy, theme.ProfileBrush, theme);
        }
    }

    private void DrawTriangle(
        DrawingContext context,
        DiagramPlotLayout layout,
        XyChromaticity red,
        XyChromaticity green,
        XyChromaticity blue,
        IPen pen)
    {
        var redPoint = ToPoint(layout.Project(GetCoordinate(red)));
        var greenPoint = ToPoint(layout.Project(GetCoordinate(green)));
        var bluePoint = ToPoint(layout.Project(GetCoordinate(blue)));
        context.DrawLine(pen, redPoint, greenPoint);
        context.DrawLine(pen, greenPoint, bluePoint);
        context.DrawLine(pen, bluePoint, redPoint);
    }

    private void DrawPoint(
        DrawingContext context,
        DiagramPlotLayout layout,
        XyChromaticity chromaticity,
        IBrush fill,
        IPen outline,
        double radius)
    {
        var point = ToPoint(layout.Project(GetCoordinate(chromaticity)));
        context.DrawEllipse(fill, outline, point, radius, radius);
    }

    private void DrawWhitePoint(
        DrawingContext context,
        DiagramPlotLayout layout,
        XyChromaticity chromaticity,
        IBrush outline,
        DiagramTheme theme)
    {
        var point = ToPoint(layout.Project(GetCoordinate(chromaticity)));
        context.DrawEllipse(theme.WhitePointFill, new Pen(outline, 2), point, 5, 5);
    }

    private void DrawHoverTooltip(
        DrawingContext context,
        DiagramPlotLayout layout,
        DiagramTheme theme)
    {
        if (hoverDataCoordinate is not { } coordinate || !layout.DataBounds.Contains(coordinate))
        {
            return;
        }

        var text = new FormattedText(
            $"{FormatCoordinate(coordinate)}\nDouble-click to copy",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            11,
            theme.TooltipText);
        var width = text.Width + 16;
        var height = text.Height + 12;
        var x = Math.Min(hoverViewportPoint.X + 14, Bounds.Width - width - 6);
        var y = Math.Min(hoverViewportPoint.Y + 14, Bounds.Height - height - 6);
        x = Math.Max(6, x);
        y = Math.Max(6, y);
        var rect = new Rect(x, y, width, height);
        context.DrawRectangle(theme.TooltipBackground, theme.TooltipBorderPen, rect, 5, 5);
        context.DrawText(text, new Point(x + 8, y + 6));
    }

    private DiagramCoordinate GetCoordinate(XyChromaticity xy) =>
        ChromaticityDiagramCoordinateSystem.GetCoordinate(DiagramType, xy);

    private string FormatCoordinate(DiagramCoordinate coordinate) => DiagramType switch
    {
        ChromaticityDiagramType.Cie1931Xy =>
            FormattableString.Invariant($"x {coordinate.Horizontal:0.####}, y {coordinate.Vertical:0.####}"),
        ChromaticityDiagramType.Cie1976UvPrime =>
            FormattableString.Invariant($"u' {coordinate.Horizontal:0.####}, v' {coordinate.Vertical:0.####}"),
        _ => throw new ArgumentOutOfRangeException(nameof(DiagramType), DiagramType, "Unknown diagram type."),
    };

    private bool TryCreateLayout(out DiagramPlotLayout layout)
    {
        if (Bounds.Width <= PlotPadding * 2 || Bounds.Height <= PlotPadding * 2)
        {
            layout = null!;
            return false;
        }

        layout = DiagramPlotLayout.Create(
            ChromaticityDiagramCoordinateSystem.GetBounds(DiagramType),
            Bounds.Width,
            Bounds.Height,
            PlotPadding);
        return true;
    }

    private DiagramTheme CreateTheme()
    {
        var dark = ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
        var primaryText = new SolidColorBrush(dark ? Color.Parse("#F4F4F4") : Color.Parse("#202020"));
        var secondaryText = new SolidColorBrush(dark ? Color.Parse("#C8C8C8") : Color.Parse("#555555"));
        var axisBrush = new SolidColorBrush(dark ? Color.Parse("#D0D0D0") : Color.Parse("#454545"));
        var gridBrush = new SolidColorBrush(dark ? Color.Parse("#55FFFFFF") : Color.Parse("#35000000"));
        var plotBackground = new SolidColorBrush(dark ? Color.Parse("#202124") : Color.Parse("#F4F4F4"));
        var profileBrush = new SolidColorBrush(dark ? Color.Parse("#FFE45C") : Color.Parse("#B42318"));
        var pointOutline = new SolidColorBrush(dark ? Colors.Black : Colors.White);
        var tooltipBackground = new SolidColorBrush(dark ? Color.Parse("#F02B2B2B") : Color.Parse("#F0FFFFFF"));

        return new DiagramTheme(
            primaryText,
            secondaryText,
            plotBackground,
            new Pen(gridBrush, 1),
            new Pen(axisBrush, 1.2),
            new Pen(axisBrush, 1.6),
            profileBrush,
            new Pen(profileBrush, 3),
            new Pen(pointOutline, 1),
            new SolidColorBrush(Colors.White),
            tooltipBackground,
            primaryText,
            new Pen(axisBrush, 1));
    }

    private static void DrawText(
        DrawingContext context,
        string value,
        Point origin,
        double fontSize,
        IBrush brush)
    {
        context.DrawText(
            new FormattedText(
                value,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                fontSize,
                brush),
            origin);
    }

    private static Point ToPoint(DiagramCoordinate coordinate) =>
        new(coordinate.Horizontal, coordinate.Vertical);

    private static Rect ToRect(DiagramRect rect) =>
        new(rect.X, rect.Y, rect.Width, rect.Height);

    private readonly record struct RasterCacheKey(
        ChromaticityDiagramType DiagramType,
        int PixelWidth,
        int PixelHeight);

    private sealed record OverlayStyle(
        ReferenceGamut Gamut,
        Color Color,
        IDashStyle DashStyle);

    private sealed record DiagramTheme(
        IBrush PrimaryText,
        IBrush SecondaryText,
        IBrush PlotBackground,
        IPen GridPen,
        IPen AxisPen,
        IPen SpectralLocusPen,
        IBrush ProfileBrush,
        IPen ProfilePen,
        IPen PointOutlinePen,
        IBrush WhitePointFill,
        IBrush TooltipBackground,
        IBrush TooltipText,
        IPen TooltipBorderPen);
}
