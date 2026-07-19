namespace ICCProfileViewer.Core.Colorimetry;

public readonly record struct SpectralLocusPoint(
    int WavelengthNanometers,
    XyChromaticity Xy,
    UvPrimeChromaticity UvPrime);
