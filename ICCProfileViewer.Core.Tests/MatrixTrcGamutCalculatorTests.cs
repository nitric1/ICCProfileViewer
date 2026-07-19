using ICCProfileViewer.Core.Colorimetry;
using ICCProfileViewer.Core.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Core.Tests;

[TestClass]
public sealed class MatrixTrcGamutCalculatorTests
{
    private static readonly Matrix3x3 BradfordD65ToD50 = new(
        1.0479298, 0.0229468, -0.0501922,
        0.0296278, 0.9904345, -0.0170738,
        -0.0092430, 0.0150552, 0.7518743);

    [TestMethod]
    public void FromChromaticAdaptationTag_RestoresSrgbDeviceChromaticities()
    {
        var colorTags = new IccColorTagData(
            new XyzColor(0.436065673828125, 0.2224884033203125, 0.013916015625),
            new XyzColor(0.3851470947265625, 0.7168731689453125, 0.097076416015625),
            new XyzColor(0.14306640625, 0.06060791015625, 0.7140960693359375),
            new XyzColor(0.95047, 1, 1.08883),
            null,
            BradfordD65ToD50);

        var gamut = MatrixTrcGamutCalculator.FromChromaticAdaptationTag(colorTags);

        Assert.IsNotNull(gamut);
        Assert.AreEqual(
            GamutCalculationMethod.ChromaticAdaptationTagInverse,
            gamut.CalculationMethod);
        Assert.AreEqual(GamutBoundaryAccuracy.Exact, gamut.Accuracy);
        StringAssert.Contains(gamut.AdaptationDescription, "inverse ICC chad matrix");
        AssertXy(gamut.Red.Xy, 0.6400, 0.3300);
        AssertXy(gamut.Green.Xy, 0.3000, 0.6000);
        AssertXy(gamut.Blue.Xy, 0.1500, 0.0600);
        AssertXy(gamut.WhitePoint.Xy, 0.3127, 0.3290);
    }

    [TestMethod]
    public void FromChromaticAdaptationTag_ReturnsNullForMissingOrSingularMatrix()
    {
        var missing = new IccColorTagData(null, null, null, null, null, null);
        var singular = new IccColorTagData(
            new XyzColor(1, 0, 0),
            new XyzColor(0, 1, 0),
            new XyzColor(0, 0, 1),
            new XyzColor(1, 1, 1),
            null,
            new Matrix3x3(
                1, 2, 3,
                2, 4, 6,
                3, 6, 9));

        Assert.IsNull(MatrixTrcGamutCalculator.FromChromaticAdaptationTag(missing));
        Assert.IsNull(MatrixTrcGamutCalculator.FromChromaticAdaptationTag(singular));
    }

    [TestMethod]
    public void FromChromaticAdaptationTag_ReturnsNullForInvalidColorant()
    {
        var colorTags = new IccColorTagData(
            new XyzColor(double.NaN, 0, 0),
            new XyzColor(0, 1, 0),
            new XyzColor(0, 0, 1),
            new XyzColor(1, 1, 1),
            null,
            new Matrix3x3(
                1, 0, 0,
                0, 1, 0,
                0, 0, 1));

        Assert.IsNull(MatrixTrcGamutCalculator.FromChromaticAdaptationTag(colorTags));
    }

    private static void AssertXy(
        XyChromaticity actual,
        double expectedX,
        double expectedY)
    {
        Assert.AreEqual(expectedX, actual.X, 0.0002);
        Assert.AreEqual(expectedY, actual.Y, 0.0002);
    }
}
