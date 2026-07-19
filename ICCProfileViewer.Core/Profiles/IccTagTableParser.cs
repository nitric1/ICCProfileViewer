using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace ICCProfileViewer.Core.Profiles;

public static class IccTagTableParser
{
    public const int HeaderSizeInBytes = 128;
    public const int MaximumTagCount = 100;

    private const int ProfileSignatureOffset = 36;
    private const uint ProfileSignature = 0x61637370;
    private const int TagCountSizeInBytes = 4;
    private const int TagEntrySizeInBytes = 12;
    private const int MinimumTagDataSizeInBytes = 8;

    public static IReadOnlyList<IccTagInfo> Parse(ReadOnlySpan<byte> profileData)
    {
        if (profileData.Length < HeaderSizeInBytes + TagCountSizeInBytes)
        {
            throw new InvalidDataException("The ICC profile is too small to contain a header and tag table.");
        }

        var declaredSize = ReadUInt32(profileData, 0);
        if (declaredSize < HeaderSizeInBytes + TagCountSizeInBytes)
        {
            throw new InvalidDataException(
                $"The ICC profile declares only {declaredSize} bytes, which is too small for a header and tag table.");
        }

        if (declaredSize > profileData.Length)
        {
            throw new InvalidDataException(
                $"The ICC profile declares {declaredSize} bytes but contains {profileData.Length} bytes.");
        }

        // Some installed profiles contain non-profile padding after the byte count declared
        // by the ICC header. Keep the declared size as the validation boundary so tags cannot
        // address those trailing bytes.
        profileData = profileData[..checked((int)declaredSize)];

        if (ReadUInt32(profileData, ProfileSignatureOffset) != ProfileSignature)
        {
            throw new InvalidDataException("The ICC profile header does not contain the required 'acsp' signature.");
        }

        var tagCount = ReadUInt32(profileData, HeaderSizeInBytes);
        if (tagCount > MaximumTagCount)
        {
            throw new InvalidDataException(
                $"The ICC profile contains {tagCount} tags; the supported maximum is {MaximumTagCount}.");
        }

        var tagDirectoryEnd = checked(
            HeaderSizeInBytes + TagCountSizeInBytes + (int)tagCount * TagEntrySizeInBytes);
        if (tagDirectoryEnd > profileData.Length)
        {
            throw new InvalidDataException("The ICC tag table extends past the end of the profile.");
        }

        var tags = new List<IccTagInfo>((int)tagCount);
        var signatures = new HashSet<uint>();
        for (var index = 0; index < tagCount; index++)
        {
            var entryOffset = HeaderSizeInBytes + TagCountSizeInBytes + (int)index * TagEntrySizeInBytes;
            var signature = ReadUInt32(profileData, entryOffset);
            var dataOffset = ReadUInt32(profileData, entryOffset + 4);
            var dataSize = ReadUInt32(profileData, entryOffset + 8);

            if (!signatures.Add(signature))
            {
                throw new InvalidDataException(
                    $"The ICC tag table contains duplicate '{IccSignature.Format(signature)}' entries.");
            }

            if (dataOffset % 4 != 0)
            {
                throw new InvalidDataException(
                    $"ICC tag '{IccSignature.Format(signature)}' has an offset that is not four-byte aligned.");
            }

            if (dataSize < MinimumTagDataSizeInBytes || dataOffset < tagDirectoryEnd)
            {
                throw new InvalidDataException(
                    $"ICC tag '{IccSignature.Format(signature)}' has an invalid data range.");
            }

            var dataEnd = (ulong)dataOffset + dataSize;
            if (dataEnd > (ulong)profileData.Length)
            {
                throw new InvalidDataException(
                    $"ICC tag '{IccSignature.Format(signature)}' extends past the end of the profile.");
            }

            var typeSignature = ReadUInt32(profileData, checked((int)dataOffset));
            tags.Add(new IccTagInfo(
                IccSignature.Format(signature),
                IccSignature.Format(typeSignature),
                dataOffset,
                dataSize));
        }

        return tags;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, sizeof(uint)));
}
