using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ICCProfileViewer.App.Services;

public interface IProfileFileSource
{
    string DisplayName { get; }

    Task<Stream> OpenReadAsync(CancellationToken cancellationToken);
}
