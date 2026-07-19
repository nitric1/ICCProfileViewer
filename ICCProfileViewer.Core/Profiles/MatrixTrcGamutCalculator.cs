using ICCProfileViewer.Core.Colorimetry;

namespace ICCProfileViewer.Core.Profiles;

public static class MatrixTrcGamutCalculator
{
    private static readonly XyzColor IccD50 = new(0.9642, 1, 0.8249);

    public static GamutBoundary? FromChromaticAdaptationTag(IccColorTagData colorTags)
    {
        if (colorTags.RedColorant is not { } red ||
            colorTags.GreenColorant is not { } green ||
            colorTags.BlueColorant is not { } blue ||
            colorTags.ChromaticAdaptationMatrix is not { } adaptation ||
            !adaptation.TryInvert(out var inverseAdaptation))
        {
            return null;
        }

        var deviceRed = inverseAdaptation.Transform(red);
        var deviceGreen = inverseAdaptation.Transform(green);
        var deviceBlue = inverseAdaptation.Transform(blue);
        var deviceWhitePoint = inverseAdaptation.Transform(IccD50);
        if (!HasDefinedChromaticity(deviceRed) ||
            !HasDefinedChromaticity(deviceGreen) ||
            !HasDefinedChromaticity(deviceBlue) ||
            !HasDefinedChromaticity(deviceWhitePoint))
        {
            return null;
        }

        return new GamutBoundary(
            ChromaticityPoint.FromXyz(
                "R",
                ChromaticityPointRole.RedPrimary,
                deviceRed),
            ChromaticityPoint.FromXyz(
                "G",
                ChromaticityPointRole.GreenPrimary,
                deviceGreen),
            ChromaticityPoint.FromXyz(
                "B",
                ChromaticityPointRole.BluePrimary,
                deviceBlue),
            ChromaticityPoint.FromXyz(
                "White",
                ChromaticityPointRole.WhitePoint,
                deviceWhitePoint),
            GamutCalculationMethod.ChromaticAdaptationTagInverse,
            GamutBoundaryAccuracy.Exact,
            "Device-side chromaticities restored by applying the inverse ICC chad matrix " +
            "to the D50 PCS colorants and reference white.");
    }

    private static bool HasDefinedChromaticity(XyzColor value) =>
        ChromaticityConverter.ToXy(value) is not null &&
        ChromaticityConverter.ToUvPrime(value) is not null;
}
