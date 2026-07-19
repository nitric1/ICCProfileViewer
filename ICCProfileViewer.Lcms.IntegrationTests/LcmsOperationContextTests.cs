using System;
using System.IO;
using lcmsNET;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Lcms.IntegrationTests;

[TestClass]
public sealed class LcmsOperationContextTests
{
    [TestMethod]
    public void ErrorHandler_RemainsAliveAndCapturesNativeDiagnostic()
    {
        NativeLibraryBootstrapper.Initialize();
        using var context = new LcmsOperationContext();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var profileBytes = File.ReadAllBytes(TestRepository.ProfilePath("bad.icc"));

        Assert.ThrowsExactly<LcmsNETException>(() =>
            Profile.Open(context.Context, profileBytes));

        var errors = context.GetErrors();
        Assert.HasCount(1, errors);
        Assert.AreEqual(11, errors[0].Code);
        StringAssert.Contains(errors[0].Message, "invalid signature");
    }
}
