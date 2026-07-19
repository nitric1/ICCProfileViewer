namespace ICCProfileViewer.Core.Profiles;

public sealed record IccTagInfo(
    string Signature,
    string TypeSignature,
    uint Offset,
    uint Size);
