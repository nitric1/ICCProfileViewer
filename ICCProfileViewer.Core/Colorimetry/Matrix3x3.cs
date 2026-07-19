using System;

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
    double M33)
{
    private const double DeterminantTolerance = 1e-12;

    public XyzColor Transform(XyzColor value) => new(
        M11 * value.X + M12 * value.Y + M13 * value.Z,
        M21 * value.X + M22 * value.Y + M23 * value.Z,
        M31 * value.X + M32 * value.Y + M33 * value.Z);

    public bool TryInvert(out Matrix3x3 inverse)
    {
        var scale = MaximumAbsoluteComponent();
        if (!double.IsFinite(scale) || scale == 0)
        {
            inverse = default;
            return false;
        }

        var a = M11 / scale;
        var b = M12 / scale;
        var c = M13 / scale;
        var d = M21 / scale;
        var e = M22 / scale;
        var f = M23 / scale;
        var g = M31 / scale;
        var h = M32 / scale;
        var i = M33 / scale;

        var determinant =
            a * (e * i - f * h) -
            b * (d * i - f * g) +
            c * (d * h - e * g);
        if (!double.IsFinite(determinant) || Math.Abs(determinant) <= DeterminantTolerance)
        {
            inverse = default;
            return false;
        }

        var inverseScale = 1 / (determinant * scale);
        inverse = new Matrix3x3(
            (e * i - f * h) * inverseScale,
            (c * h - b * i) * inverseScale,
            (b * f - c * e) * inverseScale,
            (f * g - d * i) * inverseScale,
            (a * i - c * g) * inverseScale,
            (c * d - a * f) * inverseScale,
            (d * h - e * g) * inverseScale,
            (b * g - a * h) * inverseScale,
            (a * e - b * d) * inverseScale);

        return inverse.HasOnlyFiniteComponents();
    }

    private double MaximumAbsoluteComponent()
    {
        var maximum = 0d;
        maximum = Math.Max(maximum, Math.Abs(M11));
        maximum = Math.Max(maximum, Math.Abs(M12));
        maximum = Math.Max(maximum, Math.Abs(M13));
        maximum = Math.Max(maximum, Math.Abs(M21));
        maximum = Math.Max(maximum, Math.Abs(M22));
        maximum = Math.Max(maximum, Math.Abs(M23));
        maximum = Math.Max(maximum, Math.Abs(M31));
        maximum = Math.Max(maximum, Math.Abs(M32));
        return Math.Max(maximum, Math.Abs(M33));
    }

    private bool HasOnlyFiniteComponents() =>
        double.IsFinite(M11) &&
        double.IsFinite(M12) &&
        double.IsFinite(M13) &&
        double.IsFinite(M21) &&
        double.IsFinite(M22) &&
        double.IsFinite(M23) &&
        double.IsFinite(M31) &&
        double.IsFinite(M32) &&
        double.IsFinite(M33);
}
