namespace ICCProfileViewer.Core.Colorimetry;

public sealed record GamutBoundary(
    ChromaticityPoint Red,
    ChromaticityPoint Green,
    ChromaticityPoint Blue,
    ChromaticityPoint WhitePoint,
    GamutCalculationMethod CalculationMethod,
    GamutBoundaryAccuracy Accuracy,
    string AdaptationDescription);
