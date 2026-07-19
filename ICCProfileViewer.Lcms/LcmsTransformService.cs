using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ICCProfileViewer.Core.Colorimetry;
using ICCProfileViewer.Core.Profiles;
using lcmsNET;

namespace ICCProfileViewer.Lcms;

public sealed class LcmsTransformService : IIccProfileTransformService
{
    public async Task<IReadOnlyList<XyzColor>> TransformRgbToXyzAsync(
        Stream profileStream,
        string displayName,
        IReadOnlyList<RgbColor> colors,
        IccRenderingIntent renderingIntent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profileStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(colors);

        if (!profileStream.CanRead)
        {
            throw new ArgumentException("The ICC profile stream must be readable.", nameof(profileStream));
        }

        ValidateColors(colors);
        var profileBytes = await ProfileStreamLoader
            .ReadAllAsync(profileStream, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            IccTagTableParser.Parse(profileBytes);
        }
        catch (Exception exception)
        {
            throw new LcmsTransformException(displayName, Array.Empty<LcmsError>(), exception);
        }

        NativeLibraryBootstrapper.Initialize();

        using var operationContext = new LcmsOperationContext();
        try
        {
            using var inputProfile = Profile.Open(operationContext.Context, profileBytes);
            if (inputProfile.ColorSpace != ColorSpaceSignature.RgbData)
            {
                throw new NotSupportedException(
                    $"RGB-to-XYZ transform requires an RGB profile, but '{displayName}' uses {inputProfile.ColorSpace}.");
            }

            using var outputProfile = Profile.CreateXYZ(operationContext.Context);
            using var transform = Transform.Create(
                operationContext.Context,
                inputProfile,
                Cms.TYPE_RGB_DBL,
                outputProfile,
                Cms.TYPE_XYZ_DBL,
                MapIntent(renderingIntent),
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
        catch (LcmsNativeLibraryException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new LcmsTransformException(
                displayName,
                operationContext.GetErrors(),
                exception);
        }
    }

    private static void ValidateColors(IReadOnlyList<RgbColor> colors)
    {
        for (var index = 0; index < colors.Count; index++)
        {
            var color = colors[index];
            if (!IsUnitValue(color.Red) || !IsUnitValue(color.Green) || !IsUnitValue(color.Blue))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(colors),
                    $"RGB value at index {index} must contain finite components between 0 and 1.");
            }
        }
    }

    private static bool IsUnitValue(double value) =>
        double.IsFinite(value) && value is >= 0 and <= 1;

    private static Intent MapIntent(IccRenderingIntent renderingIntent) => renderingIntent switch
    {
        IccRenderingIntent.Perceptual => Intent.Perceptual,
        IccRenderingIntent.RelativeColorimetric => Intent.RelativeColorimetric,
        IccRenderingIntent.Saturation => Intent.Saturation,
        IccRenderingIntent.AbsoluteColorimetric => Intent.AbsoluteColorimetric,
        _ => throw new ArgumentOutOfRangeException(nameof(renderingIntent), renderingIntent, null),
    };
}
