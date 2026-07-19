using System;

namespace ICCProfileViewer.Core.Colorimetry;

public static class ChromaticityConverter
{
    private const double DenominatorTolerance = 1e-12;

    public static XyChromaticity? ToXy(XyzColor color)
    {
        if (!TryNormalize(color, out var x, out var y, out var z))
        {
            return null;
        }

        var denominator = x + y + z;
        if (IsUndefinedDenominator(denominator))
        {
            return null;
        }

        return CreateXy(x / denominator, y / denominator);
    }

    public static UvPrimeChromaticity? ToUvPrime(XyzColor color)
    {
        if (!TryNormalize(color, out var x, out var y, out var z))
        {
            return null;
        }

        var denominator = x + 15 * y + 3 * z;
        if (IsUndefinedDenominator(denominator))
        {
            return null;
        }

        return CreateUvPrime(4 * x / denominator, 9 * y / denominator);
    }

    public static UvPrimeChromaticity? ToUvPrime(XyChromaticity chromaticity)
    {
        if (!AreFinite(chromaticity.X, chromaticity.Y))
        {
            return null;
        }

        var denominator = -2 * chromaticity.X + 12 * chromaticity.Y + 3;
        if (IsUndefinedDenominator(denominator))
        {
            return null;
        }

        return CreateUvPrime(
            4 * chromaticity.X / denominator,
            9 * chromaticity.Y / denominator);
    }

    public static XyChromaticity? ToXy(UvPrimeChromaticity chromaticity)
    {
        if (!AreFinite(chromaticity.UPrime, chromaticity.VPrime))
        {
            return null;
        }

        var denominator = 6 * chromaticity.UPrime - 16 * chromaticity.VPrime + 12;
        if (IsUndefinedDenominator(denominator))
        {
            return null;
        }

        return CreateXy(
            9 * chromaticity.UPrime / denominator,
            4 * chromaticity.VPrime / denominator);
    }

    private static bool TryNormalize(
        XyzColor color,
        out double x,
        out double y,
        out double z)
    {
        x = color.X;
        y = color.Y;
        z = color.Z;
        if (!AreFinite(x, y, z))
        {
            return false;
        }

        var scale = Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z)));
        if (scale == 0)
        {
            return false;
        }

        x /= scale;
        y /= scale;
        z /= scale;
        return true;
    }

    private static XyChromaticity? CreateXy(double x, double y) =>
        AreFinite(x, y) ? new XyChromaticity(x, y) : null;

    private static UvPrimeChromaticity? CreateUvPrime(double uPrime, double vPrime) =>
        AreFinite(uPrime, vPrime) ? new UvPrimeChromaticity(uPrime, vPrime) : null;

    private static bool IsUndefinedDenominator(double value) =>
        !double.IsFinite(value) || Math.Abs(value) <= DenominatorTolerance;

    private static bool AreFinite(double first, double second) =>
        double.IsFinite(first) && double.IsFinite(second);

    private static bool AreFinite(double first, double second, double third) =>
        AreFinite(first, second) && double.IsFinite(third);
}
