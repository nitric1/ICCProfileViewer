using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICCProfileViewer.Core.Colorimetry;

namespace ICCProfileViewer.Core.Profiles;

public interface IIccProfileTransformService
{
    Task<IReadOnlyList<XyzColor>> TransformRgbToXyzAsync(
        Stream profileStream,
        string displayName,
        IReadOnlyList<RgbColor> colors,
        IccRenderingIntent renderingIntent,
        CancellationToken cancellationToken);
}
