using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICCProfileViewer.Core.Colorimetry;
using ICCProfileViewer.Core.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Lcms.IntegrationTests;

[TestClass]
public sealed class LcmsTransformServiceTests
{
    private static readonly RgbColor[] PrimariesAndWhite =
    [
        new(1, 0, 0),
        new(0, 1, 0),
        new(0, 0, 1),
        new(1, 1, 1),
    ];

    private readonly LcmsTransformService service = new();

    [TestMethod]
    public async Task TransformRgbToXyzAsync_ReturnsAbsoluteColorimetricPcsValues()
    {
        await using var stream = File.OpenRead(TestRepository.ProfilePath("test5.icc"));

        var results = await service.TransformRgbToXyzAsync(
            stream,
            "test5.icc",
            PrimariesAndWhite,
            IccRenderingIntent.AbsoluteColorimetric,
            CancellationToken.None);

        Assert.HasCount(4, results);
        AssertXyz(results[0], 0.43606567, 0.22248841, 0.01391602);
        AssertXyz(results[1], 0.38514709, 0.71687320, 0.09707642);
        AssertXyz(results[2], 0.14306641, 0.06060791, 0.71409608);
        AssertXyz(results[3], 0.96427918, 0.99996948, 0.82508849);
    }

    [TestMethod]
    public async Task TransformRgbToXyzAsync_RejectsNonRgbProfile()
    {
        await using var stream = File.OpenRead(TestRepository.ProfilePath("test1.icc"));

        var exception = await Assert.ThrowsExactlyAsync<LcmsTransformException>(() =>
            service.TransformRgbToXyzAsync(
                stream,
                "test1.icc",
                PrimariesAndWhite,
                IccRenderingIntent.AbsoluteColorimetric,
                CancellationToken.None));

        Assert.IsInstanceOfType<NotSupportedException>(exception.InnerException);
    }

    [TestMethod]
    public async Task TransformRgbToXyzAsync_RepeatedCreationAndDisposalRemainsStable()
    {
        for (var iteration = 0; iteration < 100; iteration++)
        {
            await using var stream = File.OpenRead(TestRepository.ProfilePath("test5.icc"));
            var results = await service.TransformRgbToXyzAsync(
                stream,
                "test5.icc",
                PrimariesAndWhite.AsSpan(0, 1).ToArray(),
                IccRenderingIntent.AbsoluteColorimetric,
                CancellationToken.None);

            AssertXyz(results[0], 0.43606567, 0.22248841, 0.01391602);
        }
    }

    private static void AssertXyz(
        XyzColor actual,
        double expectedX,
        double expectedY,
        double expectedZ)
    {
        Assert.AreEqual(expectedX, actual.X, 0.0000001);
        Assert.AreEqual(expectedY, actual.Y, 0.0000001);
        Assert.AreEqual(expectedZ, actual.Z, 0.0000001);
    }
}
