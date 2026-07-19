using ICCProfileViewer.Core.Colorimetry;

namespace ICCProfileViewer.Core.Profiles;

public sealed record IccColorTagData(
    XyzColor? RedColorant,
    XyzColor? GreenColorant,
    XyzColor? BlueColorant,
    XyzColor? MediaWhitePoint,
    XyzColor? MediaBlackPoint,
    Matrix3x3? ChromaticAdaptationMatrix);
