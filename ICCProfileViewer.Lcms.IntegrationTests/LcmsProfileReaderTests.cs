using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICCProfileViewer.Core.Colorimetry;
using ICCProfileViewer.Core.Profiles;
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
        Assert.IsNotNull(profile.Gamut);
        Assert.AreEqual(
            GamutCalculationMethod.MatrixTrcDeviceTransform,
            profile.Gamut.CalculationMethod);
        Assert.AreEqual(GamutBoundaryAccuracy.Exact, profile.Gamut.Accuracy);
        StringAssert.Contains(profile.Gamut.AdaptationDescription, "adaptation state 0");
        AssertXy(profile.Gamut.Red.Xy, 0.6400, 0.3300);
        AssertXy(profile.Gamut.Green.Xy, 0.3000, 0.6000);
        AssertXy(profile.Gamut.Blue.Xy, 0.1500, 0.0600);
        AssertXy(profile.Gamut.WhitePoint.Xy, 0.3127, 0.3290);

        Assert.IsTrue(profile.ColorTags.RedColorant.HasValue);
        var pcsRed = ChromaticityConverter.ToXy(
            profile.ColorTags.RedColorant.GetValueOrDefault());
        Assert.IsNotNull(pcsRed);
        Assert.IsGreaterThan(
            0.001,
            System.Math.Abs(pcsRed.Value.X - profile.Gamut.Red.Xy.X),
            "The device-side primary must not be a naive xy conversion of the D50 PCS colorant.");
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
        Assert.IsNull(profile.Gamut);
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
    public async Task ReadAsync_WithChadMatrixProfile_MatchesDirectInverseAdaptation()
    {
        await using var stream = File.OpenRead(TestRepository.ProfilePath("crayons.icc"));

        var profile = await reader.ReadAsync(stream, "crayons.icc", CancellationToken.None);
        var directlyCalculated =
            MatrixTrcGamutCalculator.FromChromaticAdaptationTag(profile.ColorTags);

        Assert.IsTrue(profile.IsMatrixShaper);
        Assert.IsNotNull(profile.ColorTags.ChromaticAdaptationMatrix);
        Assert.IsNotNull(profile.Gamut);
        Assert.IsNotNull(directlyCalculated);
        AssertPointMatches(profile.Gamut.Red, directlyCalculated.Red);
        AssertPointMatches(profile.Gamut.Green, directlyCalculated.Green);
        AssertPointMatches(profile.Gamut.Blue, directlyCalculated.Blue);
        AssertPointMatches(profile.Gamut.WhitePoint, directlyCalculated.WhitePoint);
    }

    [TestMethod]
    public async Task ReadAsync_AllowsNullPaddingAfterDeclaredProfileSize()
    {
        var profileBytes = await File.ReadAllBytesAsync(TestRepository.ProfilePath("test5.icc"));
        var paddedProfileBytes = new byte[profileBytes.Length + 3];
        profileBytes.CopyTo(paddedProfileBytes, 0);
        await using var stream = new MemoryStream(paddedProfileBytes, writable: false);

        var profile = await reader.ReadAsync(stream, "padded-test5.icc", CancellationToken.None);

        Assert.AreEqual("padded-test5.icc", profile.DisplayName);
        Assert.AreEqual(paddedProfileBytes.Length, profile.SizeInBytes);
        Assert.AreEqual(2.1, profile.Version, 0.001);
        Assert.AreEqual("RGB", profile.DataColorSpace);
        Assert.AreEqual(17, profile.TagCount);
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

    private static void AssertXy(
        XyChromaticity actual,
        double expectedX,
        double expectedY)
    {
        Assert.AreEqual(expectedX, actual.X, 0.0001);
        Assert.AreEqual(expectedY, actual.Y, 0.0001);
    }

    private static void AssertPointMatches(
        ChromaticityPoint transformed,
        ChromaticityPoint directlyCalculated)
    {
        Assert.AreEqual(transformed.Xy.X, directlyCalculated.Xy.X, 0.0001);
        Assert.AreEqual(transformed.Xy.Y, directlyCalculated.Xy.Y, 0.0001);
        Assert.AreEqual(transformed.UvPrime.UPrime, directlyCalculated.UvPrime.UPrime, 0.0001);
        Assert.AreEqual(transformed.UvPrime.VPrime, directlyCalculated.UvPrime.VPrime, 0.0001);
    }
}
