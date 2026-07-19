using System;
using System.Collections.Generic;

namespace ICCProfileViewer.Lcms;

public sealed class LcmsTransformException : Exception
{
    public LcmsTransformException(
        string displayName,
        IReadOnlyList<LcmsError> nativeErrors,
        Exception innerException)
        : base(CreateMessage(displayName, nativeErrors), innerException)
    {
        DisplayName = displayName;
        NativeErrors = nativeErrors;
    }

    public string DisplayName { get; }

    public IReadOnlyList<LcmsError> NativeErrors { get; }

    private static string CreateMessage(string displayName, IReadOnlyList<LcmsError> nativeErrors)
    {
        var details = nativeErrors.Count == 0
            ? null
            : $" Little CMS reported: {nativeErrors[^1].Message}";
        return $"Little CMS could not transform RGB values from ICC profile '{displayName}'.{details}";
    }
}
