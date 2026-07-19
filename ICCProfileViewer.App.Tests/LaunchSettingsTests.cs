using System;
using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.App.Tests;

[TestClass]
public sealed class LaunchSettingsTests
{
    [TestMethod]
    public void VisualStudioProfile_ResolvesLittleCmsPathFromRepositoryRoot()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectDirectory = Path.Combine(repositoryRoot, "ICCProfileViewer.App");
        var launchSettingsPath = Path.Combine(projectDirectory, "Properties", "launchSettings.json");
        using var document = JsonDocument.Parse(File.ReadAllText(launchSettingsPath));
        var profile = document.RootElement
            .GetProperty("profiles")
            .GetProperty("ICCProfileViewer.App (Local LittleCMS)");
        var workingDirectory = profile.GetProperty("workingDirectory").GetString();
        var nativePath = profile
            .GetProperty("environmentVariables")
            .GetProperty("ICC_PROFILE_VIEWER_LCMS_PATH")
            .GetString();

        Assert.AreEqual("Project", profile.GetProperty("commandName").GetString());
        Assert.IsNotNull(workingDirectory);
        Assert.IsNotNull(nativePath);
        Assert.AreEqual(
            Path.GetFullPath(repositoryRoot),
            Path.GetFullPath(Path.Combine(projectDirectory, workingDirectory)));
        Assert.AreEqual(
            Path.GetFullPath(Path.Combine(
                repositoryRoot,
                "Artifacts",
                "native",
                "win-x64",
                "Release",
                "lcms2.dll")),
            Path.GetFullPath(Path.Combine(repositoryRoot, nativePath)));
    }

    private static string FindRepositoryRoot()
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
