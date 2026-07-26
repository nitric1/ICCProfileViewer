using ICCProfileViewer.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.App.Tests;

[TestClass]
public sealed class ThirdPartyNoticesCatalogTests
{
    [TestMethod]
    public void Text_ContainsBundledSoftwareAndDatasetNotices()
    {
        var text = ThirdPartyNoticesCatalog.Text;

        StringAssert.Contains(text, "Avalonia UI 12.1.0");
        StringAssert.Contains(text, "Little CMS 2.19.1");
        StringAssert.Contains(text, "lcmsNET 1.2.1");
        StringAssert.Contains(text, "Microsoft .NET 10 runtime");
        StringAssert.Contains(text, "10.25039/CIE.DS.mifmy4x4");
        StringAssert.Contains(text, "CC BY-SA 4.0");
        StringAssert.Contains(
            text,
            "SkiaSharp and HarfBuzzSharp incorporate third party material");
        StringAssert.Contains(text, "Copyright 2018 The ANGLE Project Authors");
        Assert.IsGreaterThan(100_000, text.Length);
    }
}
