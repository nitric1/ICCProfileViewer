using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ICCProfileViewer.Lcms;

internal static class ProfileStreamLoader
{
    public const int MaximumProfileSizeInBytes = 64 * 1024 * 1024;

    public static async Task<byte[]> ReadAllAsync(
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
}
