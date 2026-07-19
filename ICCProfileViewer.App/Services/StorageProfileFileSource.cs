using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace ICCProfileViewer.App.Services;

internal sealed class StorageProfileFileSource : IProfileFileSource
{
    private readonly IStorageFile storageFile;

    public StorageProfileFileSource(IStorageFile storageFile)
    {
        this.storageFile = storageFile;
    }

    public string DisplayName => storageFile.Name;

    public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = await storageFile.OpenReadAsync();
        if (cancellationToken.IsCancellationRequested)
        {
            await stream.DisposeAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        return stream;
    }
}
