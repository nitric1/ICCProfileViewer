using System;
using ICCProfileViewer.Core.Colorimetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Core.Tests;

[TestClass]
public sealed class ChromaticityConverterTests
{
    private const double Tolerance = 1e-7;

    [TestMethod]
    public void ToXy_WithD65White_ReturnsCie1931Coordinates()
    {
        var result = ChromaticityConverter.ToXy(new XyzColor(0.95047, 1, 1.08883));

        Assert.IsNotNull(result);
        Assert.AreEqual(0.3127266, result.Value.X, Tolerance);
        Assert.AreEqual(0.3290231, result.Value.Y, Tolerance);
    }

    [TestMethod]
    public void ToUvPrime_WithD65White_ReturnsCie1976Coordinates()
    {
        var result = ChromaticityConverter.ToUvPrime(new XyzColor(0.95047, 1, 1.08883));

        Assert.IsNotNull(result);
        Assert.AreEqual(0.1978398, result.Value.UPrime, Tolerance);
        Assert.AreEqual(0.4683363, result.Value.VPrime, Tolerance);
    }

    [TestMethod]
    public void XyAndUvPrimeConversions_RoundTripD65White()
    {
        var xy = new XyChromaticity(0.3127, 0.3290);

        var uvPrime = ChromaticityConverter.ToUvPrime(xy);
        Assert.IsNotNull(uvPrime);
        var roundTrip = ChromaticityConverter.ToXy(uvPrime.Value);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(xy.X, roundTrip.Value.X, Tolerance);
        Assert.AreEqual(xy.Y, roundTrip.Value.Y, Tolerance);
    }

    [TestMethod]
    public void XyzConversions_AreInvariantToVerySmallScale()
    {
        var reference = ChromaticityConverter.ToXy(new XyzColor(0.95047, 1, 1.08883));
        var scaled = ChromaticityConverter.ToXy(new XyzColor(0.95047e-300, 1e-300, 1.08883e-300));

        Assert.IsNotNull(reference);
        Assert.IsNotNull(scaled);
        Assert.AreEqual(reference.Value.X, scaled.Value.X, Tolerance);
        Assert.AreEqual(reference.Value.Y, scaled.Value.Y, Tolerance);
    }

    [TestMethod]
    public void XyzConversions_WithUndefinedDenominators_ReturnNull()
    {
        Assert.IsNull(ChromaticityConverter.ToXy(new XyzColor(0, 0, 0)));
        Assert.IsNull(ChromaticityConverter.ToXy(new XyzColor(1, -1, 0)));
        Assert.IsNull(ChromaticityConverter.ToUvPrime(new XyzColor(1, -1d / 15d, 0)));
    }

    [TestMethod]
    public void ChromaticityConversions_WithUndefinedDenominators_ReturnNull()
    {
        Assert.IsNull(ChromaticityConverter.ToUvPrime(new XyChromaticity(1.5, 0)));
        Assert.IsNull(ChromaticityConverter.ToXy(new UvPrimeChromaticity(2d / 3d, 1)));
    }

    [TestMethod]
    public void Conversions_WithNonFiniteComponents_ReturnNull()
    {
        Assert.IsNull(ChromaticityConverter.ToXy(new XyzColor(double.NaN, 1, 1)));
        Assert.IsNull(ChromaticityConverter.ToUvPrime(new XyzColor(1, double.PositiveInfinity, 1)));
        Assert.IsNull(ChromaticityConverter.ToUvPrime(new XyChromaticity(0.3, double.NaN)));
        Assert.IsNull(ChromaticityConverter.ToXy(new UvPrimeChromaticity(double.NegativeInfinity, 0.4)));
    }

    [TestMethod]
    public void ChromaticityConversions_WithOverflowingDenominators_ReturnNull()
    {
        Assert.IsNull(ChromaticityConverter.ToUvPrime(new XyChromaticity(double.MaxValue, 0)));
        Assert.IsNull(ChromaticityConverter.ToXy(new UvPrimeChromaticity(double.MaxValue, 0)));
    }
}
