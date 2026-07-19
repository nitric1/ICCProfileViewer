using System;

namespace ICCProfileViewer.Core.Colorimetry;

public readonly record struct DiagramRect
{
    public DiagramRect(double x, double y, double width, double height)
    {
        if (!double.IsFinite(x) ||
            !double.IsFinite(y) ||
            !double.IsFinite(width) ||
            !double.IsFinite(height) ||
            width <= 0 ||
            height <= 0)
        {
            throw new ArgumentException("A diagram rectangle must be finite and have a positive size.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    public double Right => X + Width;

    public double Bottom => Y + Height;

    public bool Contains(DiagramCoordinate coordinate) =>
        double.IsFinite(coordinate.Horizontal) &&
        double.IsFinite(coordinate.Vertical) &&
        coordinate.Horizontal >= X &&
        coordinate.Horizontal <= Right &&
        coordinate.Vertical >= Y &&
        coordinate.Vertical <= Bottom;
}
