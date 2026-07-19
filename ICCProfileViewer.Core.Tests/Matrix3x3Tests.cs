using ICCProfileViewer.Core.Colorimetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Core.Tests;

[TestClass]
public sealed class Matrix3x3Tests
{
    private const double Tolerance = 1e-12;

    [TestMethod]
    public void Transform_MultipliesMatrixByXyzColumnVector()
    {
        var matrix = new Matrix3x3(
            1, 2, 3,
            4, 5, 6,
            7, 8, 9);

        var result = matrix.Transform(new XyzColor(2, 3, 4));

        Assert.AreEqual(20, result.X, Tolerance);
        Assert.AreEqual(47, result.Y, Tolerance);
        Assert.AreEqual(74, result.Z, Tolerance);
    }

    [TestMethod]
    public void TryInvert_RoundTripsXyzValue()
    {
        var matrix = new Matrix3x3(
            3, 0, 2,
            2, 0, -2,
            0, 1, 1);
        var original = new XyzColor(0.25, 0.5, 0.75);

        Assert.IsTrue(matrix.TryInvert(out var inverse));
        var roundTrip = inverse.Transform(matrix.Transform(original));

        Assert.AreEqual(original.X, roundTrip.X, Tolerance);
        Assert.AreEqual(original.Y, roundTrip.Y, Tolerance);
        Assert.AreEqual(original.Z, roundTrip.Z, Tolerance);
    }

    [TestMethod]
    public void TryInvert_IsStableForVerySmallFiniteScale()
    {
        var matrix = new Matrix3x3(
            1e-300, 0, 0,
            0, 2e-300, 0,
            0, 0, 4e-300);

        Assert.IsTrue(matrix.TryInvert(out var inverse));
        var result = inverse.Transform(matrix.Transform(new XyzColor(1, 2, 3)));

        Assert.AreEqual(1, result.X, Tolerance);
        Assert.AreEqual(2, result.Y, Tolerance);
        Assert.AreEqual(3, result.Z, Tolerance);
    }

    [TestMethod]
    public void TryInvert_RejectsSingularAndNonFiniteMatrices()
    {
        var singular = new Matrix3x3(
            1, 2, 3,
            2, 4, 6,
            3, 6, 9);
        var nonFinite = new Matrix3x3(
            double.NaN, 0, 0,
            0, 1, 0,
            0, 0, 1);

        Assert.IsFalse(singular.TryInvert(out _));
        Assert.IsFalse(nonFinite.TryInvert(out _));
    }
}
