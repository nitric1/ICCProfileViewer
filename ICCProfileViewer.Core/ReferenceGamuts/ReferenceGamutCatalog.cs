using System;
using System.Collections.Generic;
using ICCProfileViewer.Core.Colorimetry;

namespace ICCProfileViewer.Core.ReferenceGamuts;

public static class ReferenceGamutCatalog
{
    public static ReferenceGamut Srgb { get; } = new(
        ReferenceGamutId.Srgb,
        "sRGB",
        new XyChromaticity(0.6400, 0.3300),
        new XyChromaticity(0.3000, 0.6000),
        new XyChromaticity(0.1500, 0.0600),
        new XyChromaticity(0.3127, 0.3290),
        "D65",
        "IEC 61966-2-1 sRGB transfer function",
        new Uri("https://registry.color.org/rgb-registry/srgb"));

    public static ReferenceGamut DisplayP3 { get; } = new(
        ReferenceGamutId.DisplayP3,
        "Display P3",
        new XyChromaticity(0.6800, 0.3200),
        new XyChromaticity(0.2650, 0.6900),
        new XyChromaticity(0.1500, 0.0600),
        new XyChromaticity(0.3127, 0.3290),
        "D65",
        "IEC 61966-2-1 sRGB transfer function",
        new Uri("https://registry.color.org/rgb-registry/displayp3"));

    public static ReferenceGamut DciP3 { get; } = new(
        ReferenceGamutId.DciP3,
        "DCI-P3 (DCI white)",
        new XyChromaticity(0.6800, 0.3200),
        new XyChromaticity(0.2650, 0.6900),
        new XyChromaticity(0.1500, 0.0600),
        new XyChromaticity(0.3140, 0.3510),
        "DCI",
        "Power function, gamma 2.6",
        new Uri("https://registry.color.org/rgb-registry/dcip3"));

    public static ReferenceGamut AdobeRgb1998 { get; } = new(
        ReferenceGamutId.AdobeRgb1998,
        "Adobe RGB (1998)",
        new XyChromaticity(0.6400, 0.3300),
        new XyChromaticity(0.2100, 0.7100),
        new XyChromaticity(0.1500, 0.0600),
        new XyChromaticity(0.3127, 0.3290),
        "D65",
        "Power function, gamma 563/256",
        new Uri("https://www.adobe.com/digitalimag/pdfs/AdobeRGB1998.pdf"));

    public static ReferenceGamut Bt2020 { get; } = new(
        ReferenceGamutId.Bt2020,
        "BT.2020",
        new XyChromaticity(0.7080, 0.2920),
        new XyChromaticity(0.1700, 0.7970),
        new XyChromaticity(0.1310, 0.0460),
        new XyChromaticity(0.3127, 0.3290),
        "D65",
        "ITU-R BT.2020 transfer function",
        new Uri("https://registry.color.org/rgb-registry/bt2020"));

    public static IReadOnlyList<ReferenceGamut> All { get; } =
        Array.AsReadOnly([Srgb, DisplayP3, DciP3, AdobeRgb1998, Bt2020]);

    public static ReferenceGamut Get(ReferenceGamutId id) => id switch
    {
        ReferenceGamutId.Srgb => Srgb,
        ReferenceGamutId.DisplayP3 => DisplayP3,
        ReferenceGamutId.DciP3 => DciP3,
        ReferenceGamutId.AdobeRgb1998 => AdobeRgb1998,
        ReferenceGamutId.Bt2020 => Bt2020,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown reference gamut."),
    };
}
