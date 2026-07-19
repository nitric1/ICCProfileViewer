using System;
using ICCProfileViewer.Core.Colorimetry;
using ICCProfileViewer.Core.ReferenceGamuts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Core.Tests;

[TestClass]
public sealed class ReferenceGamutCatalogTests
{
    [TestMethod]
    public void All_ContainsFiveGamutsInUiOrder()
    {
        Assert.AreEqual(5, ReferenceGamutCatalog.All.Count);
        Assert.AreSame(ReferenceGamutCatalog.Srgb, ReferenceGamutCatalog.All[0]);
        Assert.AreSame(ReferenceGamutCatalog.DisplayP3, ReferenceGamutCatalog.All[1]);
        Assert.AreSame(ReferenceGamutCatalog.DciP3, ReferenceGamutCatalog.All[2]);
        Assert.AreSame(ReferenceGamutCatalog.AdobeRgb1998, ReferenceGamutCatalog.All[3]);
        Assert.AreSame(ReferenceGamutCatalog.Bt2020, ReferenceGamutCatalog.All[4]);
        Assert.AreEqual(ReferenceGamutId.Srgb, ReferenceGamutCatalog.All[0].Id);
        Assert.AreEqual(ReferenceGamutId.DisplayP3, ReferenceGamutCatalog.All[1].Id);
        Assert.AreEqual(ReferenceGamutId.DciP3, ReferenceGamutCatalog.All[2].Id);
        Assert.AreEqual(ReferenceGamutId.AdobeRgb1998, ReferenceGamutCatalog.All[3].Id);
        Assert.AreEqual(ReferenceGamutId.Bt2020, ReferenceGamutCatalog.All[4].Id);
    }

    [TestMethod]
    public void Metadata_HasStableNamesWhitePointsTransferFunctionsAndSources()
    {
        AssertMetadata(
            ReferenceGamutCatalog.Srgb,
            "sRGB",
            "D65",
            "IEC 61966-2-1 sRGB transfer function",
            "https://registry.color.org/rgb-registry/srgb");
        AssertMetadata(
            ReferenceGamutCatalog.DisplayP3,
            "Display P3",
            "D65",
            "IEC 61966-2-1 sRGB transfer function",
            "https://registry.color.org/rgb-registry/displayp3");
        AssertMetadata(
            ReferenceGamutCatalog.DciP3,
            "DCI-P3 (DCI white)",
            "DCI",
            "Power function, gamma 2.6",
            "https://registry.color.org/rgb-registry/dcip3");
        AssertMetadata(
            ReferenceGamutCatalog.AdobeRgb1998,
            "Adobe RGB (1998)",
            "D65",
            "Power function, gamma 563/256",
            "https://www.adobe.com/digitalimag/pdfs/AdobeRGB1998.pdf");
        AssertMetadata(
            ReferenceGamutCatalog.Bt2020,
            "BT.2020",
            "D65",
            "ITU-R BT.2020 transfer function",
            "https://registry.color.org/rgb-registry/bt2020");
    }

    [TestMethod]
    public void Srgb_HasPublishedChromaticities()
    {
        AssertChromaticities(
            ReferenceGamutCatalog.Srgb,
            new XyChromaticity(0.6400, 0.3300),
            new XyChromaticity(0.3000, 0.6000),
            new XyChromaticity(0.1500, 0.0600),
            new XyChromaticity(0.3127, 0.3290));
    }

    [TestMethod]
    public void DisplayP3_HasPublishedChromaticities()
    {
        AssertChromaticities(
            ReferenceGamutCatalog.DisplayP3,
            new XyChromaticity(0.6800, 0.3200),
            new XyChromaticity(0.2650, 0.6900),
            new XyChromaticity(0.1500, 0.0600),
            new XyChromaticity(0.3127, 0.3290));
    }

    [TestMethod]
    public void DciP3_HasPublishedChromaticities()
    {
        AssertChromaticities(
            ReferenceGamutCatalog.DciP3,
            new XyChromaticity(0.6800, 0.3200),
            new XyChromaticity(0.2650, 0.6900),
            new XyChromaticity(0.1500, 0.0600),
            new XyChromaticity(0.3140, 0.3510));
    }

    [TestMethod]
    public void AdobeRgb1998_HasPublishedChromaticities()
    {
        AssertChromaticities(
            ReferenceGamutCatalog.AdobeRgb1998,
            new XyChromaticity(0.6400, 0.3300),
            new XyChromaticity(0.2100, 0.7100),
            new XyChromaticity(0.1500, 0.0600),
            new XyChromaticity(0.3127, 0.3290));
    }

    [TestMethod]
    public void Bt2020_HasPublishedChromaticities()
    {
        AssertChromaticities(
            ReferenceGamutCatalog.Bt2020,
            new XyChromaticity(0.7080, 0.2920),
            new XyChromaticity(0.1700, 0.7970),
            new XyChromaticity(0.1310, 0.0460),
            new XyChromaticity(0.3127, 0.3290));
    }

    [TestMethod]
    public void P3Definitions_SharePrimariesButUseDistinctWhitePointsAndNames()
    {
        var displayP3 = ReferenceGamutCatalog.DisplayP3;
        var dciP3 = ReferenceGamutCatalog.DciP3;

        Assert.AreEqual(displayP3.Red, dciP3.Red);
        Assert.AreEqual(displayP3.Green, dciP3.Green);
        Assert.AreEqual(displayP3.Blue, dciP3.Blue);
        Assert.AreNotEqual(displayP3.WhitePoint, dciP3.WhitePoint);
        Assert.AreEqual("Display P3", displayP3.Name);
        Assert.AreEqual("DCI-P3 (DCI white)", dciP3.Name);
        Assert.AreEqual("D65", displayP3.WhitePointName);
        Assert.AreEqual("DCI", dciP3.WhitePointName);
    }

    [TestMethod]
    public void AllChromaticities_CanBeConvertedToUvPrime()
    {
        foreach (var gamut in ReferenceGamutCatalog.All)
        {
            Assert.IsNotNull(ChromaticityConverter.ToUvPrime(gamut.Red), gamut.Name);
            Assert.IsNotNull(ChromaticityConverter.ToUvPrime(gamut.Green), gamut.Name);
            Assert.IsNotNull(ChromaticityConverter.ToUvPrime(gamut.Blue), gamut.Name);
            Assert.IsNotNull(ChromaticityConverter.ToUvPrime(gamut.WhitePoint), gamut.Name);
        }
    }

    [TestMethod]
    public void AllSources_AreAbsoluteHttpsUris()
    {
        foreach (var gamut in ReferenceGamutCatalog.All)
        {
            Assert.IsTrue(gamut.Source.IsAbsoluteUri, gamut.Name);
            Assert.AreEqual(Uri.UriSchemeHttps, gamut.Source.Scheme, true, gamut.Name);
        }
    }

    [TestMethod]
    public void Get_ReturnsCatalogInstanceAndRejectsUnknownId()
    {
        Assert.AreSame(
            ReferenceGamutCatalog.AdobeRgb1998,
            ReferenceGamutCatalog.Get(ReferenceGamutId.AdobeRgb1998));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => ReferenceGamutCatalog.Get((ReferenceGamutId)int.MaxValue));
    }

    private static void AssertChromaticities(
        ReferenceGamut actual,
        XyChromaticity red,
        XyChromaticity green,
        XyChromaticity blue,
        XyChromaticity whitePoint)
    {
        Assert.AreEqual(red, actual.Red);
        Assert.AreEqual(green, actual.Green);
        Assert.AreEqual(blue, actual.Blue);
        Assert.AreEqual(whitePoint, actual.WhitePoint);
    }

    private static void AssertMetadata(
        ReferenceGamut actual,
        string name,
        string whitePointName,
        string transferFunction,
        string source)
    {
        Assert.AreEqual(name, actual.Name);
        Assert.AreEqual(whitePointName, actual.WhitePointName);
        Assert.AreEqual(transferFunction, actual.TransferFunction);
        Assert.AreEqual(source, actual.Source.AbsoluteUri);
    }
}
