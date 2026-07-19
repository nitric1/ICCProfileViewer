using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Lcms.IntegrationTests;

[TestClass]
public sealed class NativeLibraryBootstrapperTests
{
    [TestMethod]
    public void Initialize_LoadsExplicitLittleCmsBuild()
    {
        var runtime = NativeLibraryBootstrapper.Initialize();

        Assert.AreEqual(2190, runtime.EncodedVersion);
        Assert.AreEqual("2.19", runtime.Version);
        Assert.AreEqual("ExplicitPath", runtime.LibrarySource);
        Assert.AreEqual(
            Path.GetFullPath(TestRepository.NativeLibraryPath),
            runtime.LibraryPath);
    }
}
