using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Lcms.IntegrationTests;

[TestClass]
public sealed class TestAssemblySetup
{
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("The MVP Little CMS integration tests currently target Windows x64 only.");
        }

        Assert.IsTrue(
            File.Exists(TestRepository.NativeLibraryPath),
            $"Little CMS was not found at '{TestRepository.NativeLibraryPath}'. " +
            "Run build-lcms.cmd Release x64 from a Visual Studio Developer shell first.");

        Environment.SetEnvironmentVariable(
            NativeLibraryBootstrapper.LibraryPathEnvironmentVariable,
            TestRepository.NativeLibraryPath);
    }
}
