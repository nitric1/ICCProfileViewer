using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICCProfileViewer.Core.Profiles;
using lcmsNET;

namespace ICCProfileViewer.Lcms;

public sealed class LcmsProfileReader : IIccProfileReader
{
    public const int MaximumProfileSizeInBytes = 64 * 1024 * 1024;

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

        var profileBytes = await ReadProfileBytesAsync(profileStream, cancellationToken).ConfigureAwait(false);
        NativeLibraryBootstrapper.Initialize();

        try
        {
            using var profile = Profile.Open(profileBytes);
            var creationDate = profile.GetHeaderCreationDateTime(out var date)
                ? date
                : (DateTime?)null;

            return new IccProfileInfo(
                displayName,
                profileBytes.LongLength,
                profile.Version,
                profile.EncodedICCVersion,
                profile.DeviceClass.ToString(),
                FormatSignature((uint)profile.ColorSpace),
                FormatSignature((uint)profile.PCS),
                creationDate,
                profile.HeaderRenderingIntent.ToString(),
                ReadProfileInfo(profile, InfoType.Description),
                ReadProfileInfo(profile, InfoType.Manufacturer),
                ReadProfileInfo(profile, InfoType.Model),
                ReadProfileInfo(profile, InfoType.Copyright),
                FormatOptionalSignature(profile.HeaderManufacturer),
                FormatOptionalSignature(profile.HeaderModel),
                profile.TagCount,
                profile.IsMatrixShaper);
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
            throw new LcmsProfileReadException(displayName, exception);
        }
    }

    private static async Task<byte[]> ReadProfileBytesAsync(
        Stream profileStream,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];

        while (true)
        {
            var bytesRead = await profileStream
                .ReadAsync(chunk.AsMemory(), cancellationToken)
                .ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            if (buffer.Length + bytesRead > MaximumProfileSizeInBytes)
            {
                throw new InvalidDataException(
                    $"ICC profiles larger than {MaximumProfileSizeInBytes / (1024 * 1024)} MiB are not supported.");
            }

            await buffer
                .WriteAsync(chunk.AsMemory(0, bytesRead), cancellationToken)
                .ConfigureAwait(false);
        }

        return buffer.ToArray();
    }

    private static string? ReadProfileInfo(Profile profile, InfoType infoType)
    {
        var value = profile.GetProfileInfo(infoType, "en", "US");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? FormatOptionalSignature(uint signature)
    {
        return signature == 0 ? null : FormatSignature(signature);
    }

    private static string FormatSignature(uint signature)
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
