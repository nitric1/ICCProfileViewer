using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICCProfileViewer.Core.Colorimetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Lcms.IntegrationTests;

[TestClass]
public sealed class LcmsProfileReaderTests
{
    private readonly LcmsProfileReader reader = new();

    [TestMethod]
    public async Task ReadAsync_ReadsVersion2MatrixProfileFromMemory()
    {
        await using var stream = File.OpenRead(TestRepository.ProfilePath("test5.icc"));

        var profile = await reader.ReadAsync(stream, "test5.icc", CancellationToken.None);

        Assert.AreEqual("test5.icc", profile.DisplayName);
        Assert.AreEqual(stream.Length, profile.SizeInBytes);
        Assert.AreEqual(2.1, profile.Version, 0.001);
        Assert.AreEqual(0x02100000u, profile.EncodedVersion);
        Assert.AreEqual("Display", profile.ProfileClass);
        Assert.AreEqual("RGB", profile.DataColorSpace);
        Assert.AreEqual("XYZ", profile.ProfileConnectionSpace);
        Assert.AreEqual("Perceptual", profile.RenderingIntent);
        Assert.AreEqual("Test profile, not suitable for real use", profile.Description);
        Assert.AreEqual(17, profile.TagCount);
        Assert.HasCount(17, profile.Tags);
        Assert.AreEqual("cprt", profile.Tags[0].Signature);
        Assert.AreEqual("text", profile.Tags[0].TypeSignature);
        Assert.AreEqual(336u, profile.Tags[0].Offset);
        Assert.AreEqual(35u, profile.Tags[0].Size);
        Assert.IsTrue(profile.IsMatrixShaper);
        Assert.IsNotNull(profile.CreationDate);
        AssertXyz(profile.ColorTags.RedColorant, 0.436065673828125, 0.2224884033203125, 0.013916015625);
        AssertXyz(profile.ColorTags.GreenColorant, 0.3851470947265625, 0.7168731689453125, 0.097076416015625);
        AssertXyz(profile.ColorTags.BlueColorant, 0.14306640625, 0.06060791015625, 0.7140960693359375);
        AssertXyz(profile.ColorTags.MediaWhitePoint, 0.9504547119140625, 1, 1.08905029296875);
        AssertXyz(profile.ColorTags.MediaBlackPoint, 0, 0, 0);
        Assert.IsNull(profile.ColorTags.ChromaticAdaptationMatrix);
    }

    [TestMethod]
    public async Task ReadAsync_ReadsVersion4LutProfileFromMemory()
    {
        await using var stream = File.OpenRead(TestRepository.ProfilePath("test4.icc"));

        var profile = await reader.ReadAsync(stream, "test4.icc", CancellationToken.None);

        Assert.AreEqual(4.2, profile.Version, 0.001);
        Assert.AreEqual(0x04200000u, profile.EncodedVersion);
        Assert.AreEqual("ColorSpace", profile.ProfileClass);
        Assert.AreEqual("RGB", profile.DataColorSpace);
        Assert.AreEqual("Lab", profile.ProfileConnectionSpace);
        Assert.AreEqual(11, profile.TagCount);
        Assert.IsFalse(profile.IsMatrixShaper);
        Assert.IsNull(profile.ColorTags.RedColorant);
        Assert.IsNotNull(profile.ColorTags.ChromaticAdaptationMatrix);
        Assert.AreEqual(
            1.0480194091796875,
            profile.ColorTags.ChromaticAdaptationMatrix.Value.M11,
            0.0000001);
        Assert.AreEqual(
            0.75213623046875,
            profile.ColorTags.ChromaticAdaptationMatrix.Value.M33,
            0.0000001);
    }

    [TestMethod]
    public async Task ReadAsync_WrapsInvalidProfileError()
    {
        await using var stream = File.OpenRead(TestRepository.ProfilePath("bad.icc"));

        var exception = await Assert.ThrowsExactlyAsync<LcmsProfileReadException>(
            () => reader.ReadAsync(stream, "bad.icc", CancellationToken.None));

        Assert.AreEqual("bad.icc", exception.DisplayName);
        Assert.IsNotNull(exception.InnerException);
        Assert.IsEmpty(exception.NativeErrors);
        Assert.IsInstanceOfType<InvalidDataException>(exception.InnerException);
    }

    private static void AssertXyz(
        XyzColor? actual,
        double expectedX,
        double expectedY,
        double expectedZ)
    {
        Assert.IsNotNull(actual);
        Assert.AreEqual(expectedX, actual.Value.X, 0.0000001);
        Assert.AreEqual(expectedY, actual.Value.Y, 0.0000001);
        Assert.AreEqual(expectedZ, actual.Value.Z, 0.0000001);
    }
}
