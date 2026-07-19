using System;

namespace ICCProfileViewer.Core.Profiles;

public static class IccSignature
{
    public static string Format(uint signature)
    {
        Span<char> characters = stackalloc char[4];
        for (var index = 0; index < characters.Length; index++)
        {
            var shift = 24 - index * 8;
            var value = (byte)(signature >> shift);
            characters[index] = value is >= 0x20 and <= 0x7e ? (char)value : '?';
        }

        return new string(characters).TrimEnd();
    }
}
