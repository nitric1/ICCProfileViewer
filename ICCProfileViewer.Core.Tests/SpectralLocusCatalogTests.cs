using System;
using System.Security.Cryptography;
using ICCProfileViewer.Core.Colorimetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Core.Tests;

[TestClass]
public sealed class SpectralLocusCatalogTests
{
    private const double Tolerance = 1e-12;
    private const string ResourceName =
        "ICCProfileViewer.Core.Colorimetry.Data.CIE_cc_1931_2deg.csv";

    [TestMethod]
    public void Cie1931TwoDegree_CoversOfficialRangeAtOneNanometerIntervals()
    {
        var points = SpectralLocusCatalog.Cie1931TwoDegree;

        Assert.AreEqual(471, points.Count);
        for (var index = 0; index < points.Count; index++)
        {
            Assert.AreEqual(360 + index, points[index].WavelengthNanometers);
        }
    }

    [TestMethod]
    public void Get_ReturnsPublishedSampleRows()
    {
        AssertPoint(360, 0.17556, 0.00529);
        AssertPoint(449, 0.15763, 0.01684);
        AssertPoint(550, 0.30160, 0.69231);
        AssertPoint(830, 0.73469, 0.26531);
    }

    [TestMethod]
    public void Get_RejectsWavelengthOutsidePublishedRange()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => SpectralLocusCatalog.Get(359));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => SpectralLocusCatalog.Get(831));
    }

    [TestMethod]
    public void AllPoints_HaveFiniteCoordinatesAndRoundTripThroughUvPrime()
    {
        foreach (var point in SpectralLocusCatalog.Cie1931TwoDegree)
        {
            Assert.IsTrue(double.IsFinite(point.Xy.X), point.WavelengthNanometers.ToString());
            Assert.IsTrue(double.IsFinite(point.Xy.Y), point.WavelengthNanometers.ToString());
            Assert.IsTrue(double.IsFinite(point.UvPrime.UPrime), point.WavelengthNanometers.ToString());
            Assert.IsTrue(double.IsFinite(point.UvPrime.VPrime), point.WavelengthNanometers.ToString());

            var roundTrip = ChromaticityConverter.ToXy(point.UvPrime);
            Assert.IsNotNull(roundTrip, point.WavelengthNanometers.ToString());
            Assert.AreEqual(point.Xy.X, roundTrip.Value.X, Tolerance);
            Assert.AreEqual(point.Xy.Y, roundTrip.Value.Y, Tolerance);
        }
    }

    [TestMethod]
    public void EmbeddedDataset_HasPublishedSha256()
    {
        using var stream = typeof(SpectralLocusCatalog).Assembly
            .GetManifestResourceStream(ResourceName);

        Assert.IsNotNull(stream);
        var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        Assert.AreEqual(SpectralLocusCatalog.DatasetSha256, actual);
    }

    private static void AssertPoint(int wavelength, double x, double y)
    {
        var point = SpectralLocusCatalog.Get(wavelength);

        Assert.AreEqual(wavelength, point.WavelengthNanometers);
        Assert.AreEqual(x, point.Xy.X, Tolerance);
        Assert.AreEqual(y, point.Xy.Y, Tolerance);
    }
}
