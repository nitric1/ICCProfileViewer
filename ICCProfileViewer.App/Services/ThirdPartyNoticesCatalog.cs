using System;
using System.IO;
using System.Reflection;
using System.Text;
using ICCProfileViewer.Core.Colorimetry;

namespace ICCProfileViewer.App.Services;

public static class ThirdPartyNoticesCatalog
{
    private const string SoftwareNoticesResourceName =
        "ICCProfileViewer.App.ThirdPartyNotices.SoftwareNotices.txt";
    private const string CieDatasetNoticeResourceName =
        "ICCProfileViewer.Core.Colorimetry.Data.CIE_cc_1931_2deg.NOTICE.md";
    private const string SkiaSharpNoticesResourceName =
        "ICCProfileViewer.App.ThirdPartyNotices.SkiaSharpNativeAssets.txt";
    private const string AvaloniaAngleLicenseResourceName =
        "ICCProfileViewer.App.ThirdPartyNotices.AvaloniaAngleNativeAssets.txt";

    private static readonly Lazy<string> NoticeText = new(Load);

    public static string Text => NoticeText.Value;

    private static string Load()
    {
        var appAssembly = typeof(ThirdPartyNoticesCatalog).Assembly;
        var coreAssembly = typeof(SpectralLocusCatalog).Assembly;
        var sections = new[]
        {
            ReadResource(appAssembly, SoftwareNoticesResourceName),
            ReadResource(coreAssembly, CieDatasetNoticeResourceName),
            ReadResource(appAssembly, SkiaSharpNoticesResourceName),
            ReadResource(appAssembly, AvaloniaAngleLicenseResourceName),
        };

        return string.Join(
            $"{Environment.NewLine}{Environment.NewLine}" +
            new string('=', 78) +
            $"{Environment.NewLine}{Environment.NewLine}",
            sections);
    }

    private static string ReadResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException(
                $"Embedded third-party notice '{resourceName}' was not found.");
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd().Trim();
    }
}
