using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ICCProfileViewer.Core.Profiles;

public interface IIccProfileReader
{
    Task<IccProfileInfo> ReadAsync(
        Stream profileStream,
        string displayName,
        CancellationToken cancellationToken);
}
