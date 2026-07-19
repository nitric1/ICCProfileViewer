namespace ICCProfileViewer.Core.Colorimetry;

public readonly record struct Matrix3x3(
    double M11,
    double M12,
    double M13,
    double M21,
    double M22,
    double M23,
    double M31,
    double M32,
    double M33);
