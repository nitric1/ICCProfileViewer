using ICCProfileViewer.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.App.Tests;

[TestClass]
public sealed class ProfileFileNameTests
{
    [TestMethod]
    public void HasSupportedExtension_AcceptsIccAndIcmCaseInsensitively()
    {
        Assert.IsTrue(ProfileFileName.HasSupportedExtension("display.icc"));
        Assert.IsTrue(ProfileFileName.HasSupportedExtension("display.ICM"));
    }

    [TestMethod]
    public void HasSupportedExtension_RejectsOtherOrMisleadingExtensions()
    {
        Assert.IsFalse(ProfileFileName.HasSupportedExtension("display.icc.txt"));
        Assert.IsFalse(ProfileFileName.HasSupportedExtension("display.png"));
        Assert.IsFalse(ProfileFileName.HasSupportedExtension("display"));
    }
}
