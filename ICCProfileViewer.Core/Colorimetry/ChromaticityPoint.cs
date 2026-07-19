using System;

namespace ICCProfileViewer.Core.Colorimetry;

public sealed record ChromaticityPoint
{
    private ChromaticityPoint(
        string label,
        ChromaticityPointRole role,
        XyzColor xyz,
        XyChromaticity xy,
        UvPrimeChromaticity uvPrime)
    {
        Label = label;
        Role = role;
        Xyz = xyz;
        Xy = xy;
        UvPrime = uvPrime;
    }

    public string Label { get; }

    public ChromaticityPointRole Role { get; }

    public XyzColor Xyz { get; }

    public XyChromaticity Xy { get; }

    public UvPrimeChromaticity UvPrime { get; }

    public static ChromaticityPoint FromXyz(
        string label,
        ChromaticityPointRole role,
        XyzColor xyz)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        var xy = ChromaticityConverter.ToXy(xyz)
            ?? throw new ArgumentException(
                "XYZ must contain finite values with a defined chromaticity.",
                nameof(xyz));
        var uvPrime = ChromaticityConverter.ToUvPrime(xyz)
            ?? throw new ArgumentException(
                "XYZ must contain finite values with a defined u'v' chromaticity.",
                nameof(xyz));

        return new ChromaticityPoint(label, role, xyz, xy, uvPrime);
    }
}
