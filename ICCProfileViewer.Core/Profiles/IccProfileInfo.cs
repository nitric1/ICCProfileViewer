using System;
using System.Collections.Generic;

namespace ICCProfileViewer.Core.Profiles;

public sealed record IccProfileInfo(
    string DisplayName,
    long SizeInBytes,
    double Version,
    uint EncodedVersion,
    string ProfileClass,
    string DataColorSpace,
    string ProfileConnectionSpace,
    DateTime? CreationDate,
    string RenderingIntent,
    string? Description,
    string? Manufacturer,
    string? Model,
    string? Copyright,
    string? HeaderManufacturerSignature,
    string? HeaderModelSignature,
    int TagCount,
    bool IsMatrixShaper,
    IccColorTagData ColorTags,
    IReadOnlyList<IccTagInfo> Tags);
