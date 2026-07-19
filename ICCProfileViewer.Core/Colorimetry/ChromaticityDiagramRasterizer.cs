using System;
using System.Collections.Generic;

namespace ICCProfileViewer.Core.Colorimetry;

public static class ChromaticityDiagramRasterizer
{
    public static ChromaticityDiagramRaster Rasterize(
        ChromaticityDiagramType diagramType,
        int width,
        int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Raster width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Raster height must be positive.");
        }

        var byteCount = checked(width * height * 4);
        var pixels = new byte[byteCount];
        var bounds = ChromaticityDiagramCoordinateSystem.GetBounds(diagramType);
        var polygon = CreateSpectralLocusPolygon(diagramType);
        var intersections = new List<double>(8);

        for (var pixelY = 0; pixelY < height; pixelY++)
        {
            var dataY = bounds.MaximumVertical -
                (pixelY + 0.5) / height * bounds.Height;
            FindScanlineIntersections(polygon, dataY, intersections);

            for (var intersectionIndex = 0;
                 intersectionIndex + 1 < intersections.Count;
                 intersectionIndex += 2)
            {
                var minimumX = Math.Max(bounds.MinimumHorizontal, intersections[intersectionIndex]);
                var maximumX = Math.Min(bounds.MaximumHorizontal, intersections[intersectionIndex + 1]);
                var firstPixelX = Math.Max(
                    0,
                    (int)Math.Ceiling((minimumX - bounds.MinimumHorizontal) / bounds.Width * width - 0.5));
                var lastPixelX = Math.Min(
                    width - 1,
                    (int)Math.Floor((maximumX - bounds.MinimumHorizontal) / bounds.Width * width - 0.5));

                for (var pixelX = firstPixelX; pixelX <= lastPixelX; pixelX++)
                {
                    var dataX = bounds.MinimumHorizontal +
                        (pixelX + 0.5) / width * bounds.Width;
                    var coordinate = new DiagramCoordinate(dataX, dataY);
                    if (!TryGetXy(diagramType, coordinate, out var xy) ||
                        !TryConvertToDisplayRgb(xy, out var red, out var green, out var blue))
                    {
                        continue;
                    }

                    var offset = (pixelY * width + pixelX) * 4;
                    pixels[offset] = blue;
                    pixels[offset + 1] = green;
                    pixels[offset + 2] = red;
                    pixels[offset + 3] = 255;
                }
            }
        }

        return new ChromaticityDiagramRaster(width, height, pixels);
    }

    private static IReadOnlyList<DiagramCoordinate> CreateSpectralLocusPolygon(
        ChromaticityDiagramType diagramType)
    {
        var points = new DiagramCoordinate[SpectralLocusCatalog.Cie1931TwoDegree.Count];
        for (var index = 0; index < points.Length; index++)
        {
            var locusPoint = SpectralLocusCatalog.Cie1931TwoDegree[index];
            points[index] = diagramType switch
            {
                ChromaticityDiagramType.Cie1931Xy =>
                    new DiagramCoordinate(locusPoint.Xy.X, locusPoint.Xy.Y),
                ChromaticityDiagramType.Cie1976UvPrime =>
                    new DiagramCoordinate(locusPoint.UvPrime.UPrime, locusPoint.UvPrime.VPrime),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(diagramType),
                    diagramType,
                    "Unknown diagram type."),
            };
        }

        return points;
    }

    private static void FindScanlineIntersections(
        IReadOnlyList<DiagramCoordinate> polygon,
        double scanlineY,
        List<double> intersections)
    {
        intersections.Clear();
        var previous = polygon[^1];
        foreach (var current in polygon)
        {
            if ((current.Vertical > scanlineY) != (previous.Vertical > scanlineY))
            {
                intersections.Add(
                    previous.Horizontal +
                    (scanlineY - previous.Vertical) *
                    (current.Horizontal - previous.Horizontal) /
                    (current.Vertical - previous.Vertical));
            }

            previous = current;
        }

        intersections.Sort();
    }

    private static bool TryGetXy(
        ChromaticityDiagramType diagramType,
        DiagramCoordinate coordinate,
        out XyChromaticity xy)
    {
        switch (diagramType)
        {
            case ChromaticityDiagramType.Cie1931Xy:
                xy = new XyChromaticity(coordinate.Horizontal, coordinate.Vertical);
                return true;
            case ChromaticityDiagramType.Cie1976UvPrime:
                var converted = ChromaticityConverter.ToXy(
                    new UvPrimeChromaticity(coordinate.Horizontal, coordinate.Vertical));
                if (converted is null)
                {
                    xy = default;
                    return false;
                }

                xy = converted.Value;
                return true;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(diagramType),
                    diagramType,
                    "Unknown diagram type.");
        }
    }

    private static bool TryConvertToDisplayRgb(
        XyChromaticity xy,
        out byte red,
        out byte green,
        out byte blue)
    {
        red = green = blue = 0;
        if (!double.IsFinite(xy.X) ||
            !double.IsFinite(xy.Y) ||
            xy.Y <= 0 ||
            xy.X < 0 ||
            xy.X + xy.Y > 1)
        {
            return false;
        }

        var x = xy.X / xy.Y;
        const double y = 1;
        var z = (1 - xy.X - xy.Y) / xy.Y;
        var linearRed = 3.2404542 * x - 1.5371385 * y - 0.4985314 * z;
        var linearGreen = -0.9692660 * x + 1.8760108 * y + 0.0415560 * z;
        var linearBlue = 0.0556434 * x - 0.2040259 * y + 1.0572252 * z;
        linearRed = Math.Max(0, linearRed);
        linearGreen = Math.Max(0, linearGreen);
        linearBlue = Math.Max(0, linearBlue);

        var maximum = Math.Max(linearRed, Math.Max(linearGreen, linearBlue));
        if (!double.IsFinite(maximum) || maximum <= 0)
        {
            return false;
        }

        red = EncodeSrgb(linearRed / maximum);
        green = EncodeSrgb(linearGreen / maximum);
        blue = EncodeSrgb(linearBlue / maximum);
        return true;
    }

    private static byte EncodeSrgb(double linear)
    {
        var encoded = linear <= 0.0031308
            ? 12.92 * linear
            : 1.055 * Math.Pow(linear, 1 / 2.4) - 0.055;
        return (byte)Math.Round(Math.Clamp(encoded, 0, 1) * 255);
    }
}
