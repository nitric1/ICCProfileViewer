using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Lcms.IntegrationTests;

[TestClass]
public sealed class NativeLibraryIsolationTests
{
    private const string HostAssemblyName = "ICCProfileViewer.Lcms.IntegrationTestHost.dll";

    [TestMethod]
    public async Task Host_LoadsLibraryFromExplicitPath()
    {
        using var host = CreateIsolatedHost(copyAppLocalLibrary: false);

        var result = await RunHostAsync(host.HostAssemblyPath, startInfo =>
            startInfo.Environment[NativeLibraryBootstrapper.LibraryPathEnvironmentVariable] =
                TestRepository.NativeLibraryPath);

        Assert.AreEqual(0, result.ExitCode, result.StandardError);
        Assert.IsTrue(result.Response.Success);
        Assert.AreEqual("ExplicitPath", result.Response.LibrarySource);
        Assert.AreEqual(
            Path.GetFullPath(TestRepository.NativeLibraryPath),
            result.Response.LibraryPath);
    }

    [TestMethod]
    public async Task Host_LoadsAppLocalLibraryWithoutExplicitPath()
    {
        using var host = CreateIsolatedHost(copyAppLocalLibrary: true);

        var result = await RunHostAsync(host.HostAssemblyPath, startInfo =>
            startInfo.Environment.Remove(NativeLibraryBootstrapper.LibraryPathEnvironmentVariable));

        Assert.AreEqual(0, result.ExitCode, result.StandardError);
        Assert.IsTrue(result.Response.Success);
        Assert.AreEqual("AppLocal", result.Response.LibrarySource);
        Assert.AreEqual(
            Path.Combine(host.DirectoryPath, "lcms2.dll"),
            result.Response.LibraryPath);
    }

    [TestMethod]
    public async Task Host_UsesOperatingSystemSearchPathAsFallback()
    {
        using var host = CreateIsolatedHost(copyAppLocalLibrary: false);
        using var nativeDirectory = TemporaryDirectory.Create();
        File.Copy(
            TestRepository.NativeLibraryPath,
            Path.Combine(nativeDirectory.DirectoryPath, "lcms2.dll"));

        var result = await RunHostAsync(host.HostAssemblyPath, startInfo =>
        {
            startInfo.Environment.Remove(NativeLibraryBootstrapper.LibraryPathEnvironmentVariable);
            var pathKey = startInfo.Environment.Keys.FirstOrDefault(key =>
                string.Equals(key, "PATH", StringComparison.OrdinalIgnoreCase)) ?? "PATH";
            var existingPath = startInfo.Environment.TryGetValue(pathKey, out var value) ? value : null;
            startInfo.Environment[pathKey] = string.IsNullOrEmpty(existingPath)
                ? nativeDirectory.DirectoryPath
                : nativeDirectory.DirectoryPath + Path.PathSeparator + existingPath;
        });

        Assert.AreEqual(0, result.ExitCode, result.StandardError);
        Assert.IsTrue(result.Response.Success);
        Assert.AreEqual("OperatingSystem", result.Response.LibrarySource);
        Assert.IsNull(result.Response.LibraryPath);
    }

    [TestMethod]
    public async Task Host_ReportsActionableErrorForMissingExplicitLibrary()
    {
        using var host = CreateIsolatedHost(copyAppLocalLibrary: false);
        var missingPath = Path.Combine(host.DirectoryPath, "missing-lcms2.dll");

        var result = await RunHostAsync(host.HostAssemblyPath, startInfo =>
            startInfo.Environment[NativeLibraryBootstrapper.LibraryPathEnvironmentVariable] = missingPath);

        Assert.AreEqual(2, result.ExitCode);
        Assert.IsFalse(result.Response.Success);
        Assert.IsNull(result.Response.LibrarySource);
        StringAssert.Contains(
            result.Response.ErrorMessage,
            NativeLibraryBootstrapper.LibraryPathEnvironmentVariable);
        StringAssert.Contains(result.Response.ErrorMessage, missingPath);
        StringAssert.Contains(result.Response.ErrorMessage, "build-lcms.cmd");
    }

    private static TemporaryHost CreateIsolatedHost(bool copyAppLocalLibrary)
    {
        var temporaryDirectory = TemporaryDirectory.Create();
        foreach (var sourcePath in Directory.EnumerateFiles(TestRepository.IntegrationTestHostOutputDirectory))
        {
            File.Copy(sourcePath, Path.Combine(temporaryDirectory.DirectoryPath, Path.GetFileName(sourcePath)));
        }

        if (copyAppLocalLibrary)
        {
            File.Copy(
                TestRepository.NativeLibraryPath,
                Path.Combine(temporaryDirectory.DirectoryPath, "lcms2.dll"));
        }

        return new TemporaryHost(
            temporaryDirectory,
            Path.Combine(temporaryDirectory.DirectoryPath, HostAssemblyName));
    }

    private static async Task<HostResult> RunHostAsync(
        string hostAssemblyPath,
        Action<ProcessStartInfo> configure)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(hostAssemblyPath);
        configure(startInfo);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the native-library integration test host.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await process.WaitForExitAsync(timeout.Token);
        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        var response = JsonSerializer.Deserialize<HostResponse>(standardOutput)
            ?? throw new InvalidDataException($"The test host returned invalid JSON: {standardOutput}");

        return new HostResult(process.ExitCode, response, standardError);
    }

    private sealed record HostResponse(
        bool Success,
        int? EncodedVersion,
        string? Version,
        string? LibrarySource,
        string? LibraryPath,
        string? ErrorMessage);

    private sealed record HostResult(
        int ExitCode,
        HostResponse Response,
        string StandardError);

    private sealed class TemporaryHost : IDisposable
    {
        private readonly TemporaryDirectory directory;

        public TemporaryHost(TemporaryDirectory directory, string hostAssemblyPath)
        {
            this.directory = directory;
            HostAssemblyPath = hostAssemblyPath;
        }

        public string DirectoryPath => directory.DirectoryPath;

        public string HostAssemblyPath { get; }

        public void Dispose() => directory.Dispose();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; }

        public static TemporaryDirectory Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "ICCProfileViewer-tests");
            var directoryPath = Path.Combine(root, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directoryPath);
            return new TemporaryDirectory(directoryPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
