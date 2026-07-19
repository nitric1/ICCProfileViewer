using System;

namespace ICCProfileViewer.Lcms;

public sealed class LcmsNativeLibraryException : Exception
{
    public LcmsNativeLibraryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LcmsNativeLibraryException(string message)
        : base(message)
    {
    }
}
