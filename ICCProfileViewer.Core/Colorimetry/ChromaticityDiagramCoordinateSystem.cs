using System;

namespace ICCProfileViewer.Core.Colorimetry;

public static class ChromaticityDiagramCoordinateSystem
{
    public static DiagramBounds GetBounds(ChromaticityDiagramType diagramType) => diagramType switch
    {
        ChromaticityDiagramType.Cie1931Xy => new DiagramBounds(0, 0.8, 0, 0.9),
        ChromaticityDiagramType.Cie1976UvPrime => new DiagramBounds(0, 0.7, 0, 0.65),
        _ => throw new ArgumentOutOfRangeException(nameof(diagramType), diagramType, "Unknown diagram type."),
    };

    public static DiagramCoordinate GetCoordinate(
        ChromaticityDiagramType diagramType,
        XyChromaticity xy) => diagramType switch
    {
        ChromaticityDiagramType.Cie1931Xy => new DiagramCoordinate(xy.X, xy.Y),
        ChromaticityDiagramType.Cie1976UvPrime => ToCoordinate(
            ChromaticityConverter.ToUvPrime(xy)
            ?? throw new ArgumentException("The xy value does not define a finite u'v' coordinate.", nameof(xy))),
        _ => throw new ArgumentOutOfRangeException(nameof(diagramType), diagramType, "Unknown diagram type."),
    };

    public static DiagramCoordinate GetCoordinate(
        ChromaticityDiagramType diagramType,
        ChromaticityPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        return diagramType switch
        {
            ChromaticityDiagramType.Cie1931Xy => ToCoordinate(point.Xy),
            ChromaticityDiagramType.Cie1976UvPrime => ToCoordinate(point.UvPrime),
            _ => throw new ArgumentOutOfRangeException(nameof(diagramType), diagramType, "Unknown diagram type."),
        };
    }

    private static DiagramCoordinate ToCoordinate(XyChromaticity value) =>
        new(value.X, value.Y);

    private static DiagramCoordinate ToCoordinate(UvPrimeChromaticity value) =>
        new(value.UPrime, value.VPrime);
}
