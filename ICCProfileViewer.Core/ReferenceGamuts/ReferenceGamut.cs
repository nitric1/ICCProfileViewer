using System;
using ICCProfileViewer.Core.Colorimetry;

namespace ICCProfileViewer.Core.ReferenceGamuts;

public sealed record ReferenceGamut(
    ReferenceGamutId Id,
    string Name,
    XyChromaticity Red,
    XyChromaticity Green,
    XyChromaticity Blue,
    XyChromaticity WhitePoint,
    string WhitePointName,
    string TransferFunction,
    Uri Source);
