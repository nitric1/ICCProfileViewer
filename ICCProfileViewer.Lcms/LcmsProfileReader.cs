using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICCProfileViewer.Core.Colorimetry;
using ICCProfileViewer.Core.Profiles;
using lcmsNET;

namespace ICCProfileViewer.Lcms;

public sealed class LcmsProfileReader : IIccProfileReader
{
    public const int MaximumProfileSizeInBytes = ProfileStreamLoader.MaximumProfileSizeInBytes;

    public async Task<IccProfileInfo> ReadAsync(
        Stream profileStream,
        string displayName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profileStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (!profileStream.CanRead)
        {
            throw new ArgumentException("The ICC profile stream must be readable.", nameof(profileStream));
        }

        var profileBytes = await ProfileStreamLoader
            .ReadAllAsync(profileStream, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<IccTagInfo> tags;
        try
        {
            tags = IccTagTableParser.Parse(profileBytes);
        }
        catch (Exception exception)
        {
            throw new LcmsProfileReadException(displayName, Array.Empty<LcmsError>(), exception);
        }

        NativeLibraryBootstrapper.Initialize();

        using var operationContext = new LcmsOperationContext();
        try
        {
            using var profile = Profile.Open(operationContext.Context, profileBytes);
            var creationDate = profile.GetHeaderCreationDateTime(out var date)
                ? date
                : (DateTime?)null;
            if (profile.TagCount != tags.Count)
            {
                throw new InvalidDataException(
                    $"The raw ICC tag count ({tags.Count}) does not match Little CMS ({profile.TagCount}).");
            }

            return new IccProfileInfo(
                displayName,
                profileBytes.LongLength,
                profile.Version,
                profile.EncodedICCVersion,
                profile.DeviceClass.ToString(),
                IccSignature.Format((uint)profile.ColorSpace),
                IccSignature.Format((uint)profile.PCS),
                creationDate,
                profile.HeaderRenderingIntent.ToString(),
                ReadProfileInfo(profile, InfoType.Description),
                ReadProfileInfo(profile, InfoType.Manufacturer),
                ReadProfileInfo(profile, InfoType.Model),
                ReadProfileInfo(profile, InfoType.Copyright),
                FormatOptionalSignature(profile.HeaderManufacturer),
                FormatOptionalSignature(profile.HeaderModel),
                profile.TagCount,
                profile.IsMatrixShaper,
                ReadColorTags(profile),
                tags);
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
            throw new LcmsProfileReadException(
                displayName,
                operationContext.GetErrors(),
                exception);
        }
    }

    private static IccColorTagData ReadColorTags(Profile profile)
    {
        return new IccColorTagData(
            ReadXyzTag(profile, TagSignature.RedColorant),
            ReadXyzTag(profile, TagSignature.GreenColorant),
            ReadXyzTag(profile, TagSignature.BlueColorant),
            ReadXyzTag(profile, TagSignature.MediaWhitePoint),
            ReadXyzTag(profile, TagSignature.MediaBlackPoint),
            ReadChromaticAdaptationMatrix(profile));
    }

    private static XyzColor? ReadXyzTag(Profile profile, TagSignature tagSignature)
    {
        if (!profile.HasTag(tagSignature))
        {
            return null;
        }

        var value = profile.ReadTag<CIEXYZ>(tagSignature);
        return new XyzColor(value.X, value.Y, value.Z);
    }

    private static Matrix3x3? ReadChromaticAdaptationMatrix(Profile profile)
    {
        if (!profile.HasTag(TagSignature.ChromaticAdaptation))
        {
            return null;
        }

        var value = profile.ReadTag<CIEXYZTRIPLE>(TagSignature.ChromaticAdaptation);
        return new Matrix3x3(
            value.Red.X,
            value.Red.Y,
            value.Red.Z,
            value.Green.X,
            value.Green.Y,
            value.Green.Z,
            value.Blue.X,
            value.Blue.Y,
            value.Blue.Z);
    }

    private static string? ReadProfileInfo(Profile profile, InfoType infoType)
    {
        var value = profile.GetProfileInfo(infoType, "en", "US");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? FormatOptionalSignature(uint signature)
    {
        return signature == 0 ? null : IccSignature.Format(signature);
    }
}
