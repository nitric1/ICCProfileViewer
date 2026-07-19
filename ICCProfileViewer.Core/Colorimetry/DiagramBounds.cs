using System;

namespace ICCProfileViewer.Core.Colorimetry;

public readonly record struct DiagramBounds
{
    public DiagramBounds(double minimumHorizontal, double maximumHorizontal, double minimumVertical, double maximumVertical)
    {
        if (!double.IsFinite(minimumHorizontal) ||
            !double.IsFinite(maximumHorizontal) ||
            !double.IsFinite(minimumVertical) ||
            !double.IsFinite(maximumVertical) ||
            minimumHorizontal >= maximumHorizontal ||
            minimumVertical >= maximumVertical)
        {
            throw new ArgumentException("Diagram bounds must be finite and have positive extents.");
        }

        MinimumHorizontal = minimumHorizontal;
        MaximumHorizontal = maximumHorizontal;
        MinimumVertical = minimumVertical;
        MaximumVertical = maximumVertical;
    }

    public double MinimumHorizontal { get; }

    public double MaximumHorizontal { get; }

    public double MinimumVertical { get; }

    public double MaximumVertical { get; }

    public double Width => MaximumHorizontal - MinimumHorizontal;

    public double Height => MaximumVertical - MinimumVertical;

    public bool Contains(DiagramCoordinate coordinate) =>
        double.IsFinite(coordinate.Horizontal) &&
        double.IsFinite(coordinate.Vertical) &&
        coordinate.Horizontal >= MinimumHorizontal &&
        coordinate.Horizontal <= MaximumHorizontal &&
        coordinate.Vertical >= MinimumVertical &&
        coordinate.Vertical <= MaximumVertical;
}
