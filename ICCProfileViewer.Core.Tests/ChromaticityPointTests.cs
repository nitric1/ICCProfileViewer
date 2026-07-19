using System;
using ICCProfileViewer.Core.Colorimetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Core.Tests;

[TestClass]
public sealed class ChromaticityPointTests
{
    [TestMethod]
    public void FromXyz_CreatesBothDiagramCoordinates()
    {
        var point = ChromaticityPoint.FromXyz(
            "White",
            ChromaticityPointRole.WhitePoint,
            new XyzColor(0.95047, 1, 1.08883));

        Assert.AreEqual("White", point.Label);
        Assert.AreEqual(ChromaticityPointRole.WhitePoint, point.Role);
        Assert.AreEqual(0.3127266, point.Xy.X, 0.0000001);
        Assert.AreEqual(0.3290231, point.Xy.Y, 0.0000001);
        Assert.AreEqual(0.1978398, point.UvPrime.UPrime, 0.0000001);
        Assert.AreEqual(0.4683363, point.UvPrime.VPrime, 0.0000001);
    }

    [TestMethod]
    public void FromXyz_RejectsUndefinedChromaticity()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            ChromaticityPoint.FromXyz(
                "Black",
                ChromaticityPointRole.WhitePoint,
                new XyzColor(0, 0, 0)));
    }
}
