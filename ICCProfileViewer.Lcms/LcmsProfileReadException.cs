using System;

namespace ICCProfileViewer.Lcms;

public sealed class LcmsProfileReadException : Exception
{
    public LcmsProfileReadException(string displayName, Exception innerException)
        : base($"Little CMS could not read ICC profile '{displayName}'.", innerException)
    {
        DisplayName = displayName;
    }

    public string DisplayName { get; }
}
