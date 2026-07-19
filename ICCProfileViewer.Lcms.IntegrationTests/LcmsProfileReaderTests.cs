using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        Assert.IsTrue(profile.IsMatrixShaper);
        Assert.IsNotNull(profile.CreationDate);
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
    }

    [TestMethod]
    public async Task ReadAsync_WrapsInvalidProfileError()
    {
        await using var stream = File.OpenRead(TestRepository.ProfilePath("bad.icc"));

        var exception = await Assert.ThrowsExactlyAsync<LcmsProfileReadException>(
            () => reader.ReadAsync(stream, "bad.icc", CancellationToken.None));

        Assert.AreEqual("bad.icc", exception.DisplayName);
        Assert.IsNotNull(exception.InnerException);
    }
}
