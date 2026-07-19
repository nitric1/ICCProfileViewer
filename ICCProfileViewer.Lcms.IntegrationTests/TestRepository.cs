using System;
using System.IO;

namespace ICCProfileViewer.Lcms.IntegrationTests;

internal static class TestRepository
{
    public static string Root { get; } = FindRoot();

    public static string NativeLibraryPath => Path.Combine(
        Root,
        "Artifacts",
        "native",
        "win-x64",
        "Release",
        "lcms2.dll");

    public static string IntegrationTestHostOutputDirectory => Path.Combine(
        Root,
        "ICCProfileViewer.Lcms.IntegrationTestHost",
        "bin",
        BuildConfiguration,
        "net10.0");

    private static string BuildConfiguration
    {
        get
        {
            var targetFrameworkDirectory = new DirectoryInfo(
                AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
            return targetFrameworkDirectory.Parent?.Name
                ?? throw new DirectoryNotFoundException(
                    $"Could not determine the build configuration from '{AppContext.BaseDirectory}'.");
        }
    }

    public static string ProfilePath(string fileName) => Path.Combine(
        Root,
        "External",
        "Little-CMS",
        "testbed",
        fileName);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ICCProfileViewer.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find the repository root from '{AppContext.BaseDirectory}'.");
    }
}
