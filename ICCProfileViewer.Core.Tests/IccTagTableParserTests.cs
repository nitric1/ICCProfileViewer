using System;
using System.Buffers.Binary;
using System.IO;
using ICCProfileViewer.Core.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Core.Tests;

[TestClass]
public sealed class IccTagTableParserTests
{
    [TestMethod]
    public void Parse_ReturnsSignatureTypeOffsetAndSize()
    {
        var profile = CreateProfile();

        var tags = IccTagTableParser.Parse(profile);

        Assert.HasCount(1, tags);
        Assert.AreEqual("wtpt", tags[0].Signature);
        Assert.AreEqual("XYZ", tags[0].TypeSignature);
        Assert.AreEqual(144u, tags[0].Offset);
        Assert.AreEqual(16u, tags[0].Size);
    }

    [TestMethod]
    public void Parse_RejectsMissingProfileSignature()
    {
        var profile = CreateProfile();
        profile[36] = 0;

        var exception = Assert.ThrowsExactly<InvalidDataException>(() =>
            IccTagTableParser.Parse(profile));

        StringAssert.Contains(exception.Message, "'acsp'");
    }

    [TestMethod]
    public void Parse_RejectsDeclaredSizeMismatch()
    {
        var profile = CreateProfile();
        WriteUInt32(profile, 0, (uint)profile.Length + 4);

        var exception = Assert.ThrowsExactly<InvalidDataException>(() =>
            IccTagTableParser.Parse(profile));

        StringAssert.Contains(exception.Message, "declares");
    }

    [TestMethod]
    public void Parse_RejectsTagOutsideProfile()
    {
        var profile = CreateProfile();
        WriteUInt32(profile, 140, 32);

        var exception = Assert.ThrowsExactly<InvalidDataException>(() =>
            IccTagTableParser.Parse(profile));

        StringAssert.Contains(exception.Message, "past the end");
    }

    private static byte[] CreateProfile()
    {
        var profile = new byte[160];
        WriteUInt32(profile, 0, (uint)profile.Length);
        WriteSignature(profile, 36, "acsp");
        WriteUInt32(profile, 128, 1);
        WriteSignature(profile, 132, "wtpt");
        WriteUInt32(profile, 136, 144);
        WriteUInt32(profile, 140, 16);
        WriteSignature(profile, 144, "XYZ ");
        return profile;
    }

    private static void WriteSignature(Span<byte> destination, int offset, string signature)
    {
        for (var index = 0; index < signature.Length; index++)
        {
            destination[offset + index] = (byte)signature[index];
        }
    }

    private static void WriteUInt32(Span<byte> destination, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(offset, sizeof(uint)), value);
}
