using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ICCProfileViewer.Core.Colorimetry;
using lcmsNET;

namespace ICCProfileViewer.Lcms;

internal static class LcmsRgbTransform
{
    public static IReadOnlyList<XyzColor> ToXyz(
        Context context,
        Profile inputProfile,
        IReadOnlyList<RgbColor> colors,
        Intent intent,
        double? adaptationState = null)
    {
        if (adaptationState is { } state)
        {
            context.AdaptationState = state;
        }

        using var outputProfile = Profile.CreateXYZ(context);
        using var transform = Transform.Create(
            context,
            inputProfile,
            Cms.TYPE_RGB_DBL,
            outputProfile,
            Cms.TYPE_XYZ_DBL,
            intent,
            CmsFlags.None);

        var inputValues = new double[colors.Count * 3];
        for (var index = 0; index < colors.Count; index++)
        {
            var color = colors[index];
            var offset = index * 3;
            inputValues[offset] = color.Red;
            inputValues[offset + 1] = color.Green;
            inputValues[offset + 2] = color.Blue;
        }

        var outputValues = new double[inputValues.Length];
        transform.DoTransform(
            MemoryMarshal.AsBytes(inputValues.AsSpan()),
            MemoryMarshal.AsBytes(outputValues.AsSpan()),
            colors.Count);

        var results = new XyzColor[colors.Count];
        for (var index = 0; index < results.Length; index++)
        {
            var offset = index * 3;
            results[index] = new XyzColor(
                outputValues[offset],
                outputValues[offset + 1],
                outputValues[offset + 2]);
        }

        return results;
    }
}
