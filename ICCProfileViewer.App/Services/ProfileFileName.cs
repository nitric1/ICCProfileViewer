using System;
using System.IO;

namespace ICCProfileViewer.App.Services;

public static class ProfileFileName
{
    public static bool HasSupportedExtension(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var extension = Path.GetExtension(fileName);
        return string.Equals(extension, ".icc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".icm", StringComparison.OrdinalIgnoreCase);
    }
}
