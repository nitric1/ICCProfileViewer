using System;

namespace ICCProfileViewer.Core.Colorimetry;

public sealed class ChromaticityDiagramRaster
{
    internal ChromaticityDiagramRaster(int width, int height, byte[] bgraPixels)
    {
        Width = width;
        Height = height;
        BgraPixels = bgraPixels;
    }

    public int Width { get; }

    public int Height { get; }

    public ReadOnlyMemory<byte> BgraPixels { get; }

    public int Stride => checked(Width * 4);
}
