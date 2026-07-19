using System;

namespace ICCProfileViewer.Core.Colorimetry;

public sealed class DiagramPlotLayout
{
    private DiagramPlotLayout(DiagramBounds dataBounds, DiagramRect plotArea, double scale)
    {
        DataBounds = dataBounds;
        PlotArea = plotArea;
        Scale = scale;
    }

    public DiagramBounds DataBounds { get; }

    public DiagramRect PlotArea { get; }

    public double Scale { get; }

    public static DiagramPlotLayout Create(
        DiagramBounds dataBounds,
        double viewportWidth,
        double viewportHeight,
        double padding)
    {
        if (!double.IsFinite(viewportWidth) || viewportWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportWidth), viewportWidth, "Viewport width must be finite and positive.");
        }

        if (!double.IsFinite(viewportHeight) || viewportHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportHeight), viewportHeight, "Viewport height must be finite and positive.");
        }

        if (!double.IsFinite(padding) || padding < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(padding), padding, "Padding must be finite and non-negative.");
        }

        var availableWidth = viewportWidth - 2 * padding;
        var availableHeight = viewportHeight - 2 * padding;
        if (availableWidth <= 0 || availableHeight <= 0)
        {
            throw new ArgumentException("The viewport must be larger than twice the requested padding.");
        }

        var scale = Math.Min(
            availableWidth / dataBounds.Width,
            availableHeight / dataBounds.Height);
        var plotWidth = dataBounds.Width * scale;
        var plotHeight = dataBounds.Height * scale;
        var plotArea = new DiagramRect(
            (viewportWidth - plotWidth) / 2,
            (viewportHeight - plotHeight) / 2,
            plotWidth,
            plotHeight);

        return new DiagramPlotLayout(dataBounds, plotArea, scale);
    }

    public DiagramCoordinate Project(DiagramCoordinate dataCoordinate)
    {
        if (!double.IsFinite(dataCoordinate.Horizontal) || !double.IsFinite(dataCoordinate.Vertical))
        {
            throw new ArgumentException("The data coordinate must be finite.", nameof(dataCoordinate));
        }

        return new DiagramCoordinate(
            PlotArea.X + (dataCoordinate.Horizontal - DataBounds.MinimumHorizontal) * Scale,
            PlotArea.Y + (DataBounds.MaximumVertical - dataCoordinate.Vertical) * Scale);
    }

    public bool TryUnproject(DiagramCoordinate viewportCoordinate, out DiagramCoordinate dataCoordinate)
    {
        if (!PlotArea.Contains(viewportCoordinate))
        {
            dataCoordinate = default;
            return false;
        }

        dataCoordinate = new DiagramCoordinate(
            DataBounds.MinimumHorizontal + (viewportCoordinate.Horizontal - PlotArea.X) / Scale,
            DataBounds.MaximumVertical - (viewportCoordinate.Vertical - PlotArea.Y) / Scale);
        return true;
    }
}
