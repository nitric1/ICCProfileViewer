using System;
using ICCProfileViewer.Core.Colorimetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Core.Tests;

[TestClass]
public sealed class ChromaticityDiagramRasterizerTests
{
    [TestMethod]
    [DataRow(ChromaticityDiagramType.Cie1931Xy)]
    [DataRow(ChromaticityDiagramType.Cie1976UvPrime)]
    public void Rasterize_CreatesOpaqueLocusOnTransparentBackground(
        ChromaticityDiagramType diagramType)
    {
        var raster = ChromaticityDiagramRasterizer.Rasterize(diagramType, 160, 160);

        Assert.AreEqual(160, raster.Width);
        Assert.AreEqual(160, raster.Height);
        Assert.AreEqual(640, raster.Stride);
        Assert.AreEqual(160 * 160 * 4, raster.BgraPixels.Length);

        var pixels = raster.BgraPixels.Span;
        var transparentCount = 0;
        var opaqueCount = 0;
        for (var offset = 3; offset < pixels.Length; offset += 4)
        {
            if (pixels[offset] == 0)
            {
                transparentCount++;
            }
            else if (pixels[offset] == 255)
            {
                opaqueCount++;
            }
        }

        Assert.IsGreaterThan(1000, transparentCount);
        Assert.IsGreaterThan(1000, opaqueCount);
        Assert.AreEqual(160 * 160, transparentCount + opaqueCount);
    }

    [TestMethod]
    public void Rasterize_BothDiagramsAgreeNearD65()
    {
        var xyRaster = ChromaticityDiagramRasterizer.Rasterize(
            ChromaticityDiagramType.Cie1931Xy,
            800,
            900);
        var uvRaster = ChromaticityDiagramRasterizer.Rasterize(
            ChromaticityDiagramType.Cie1976UvPrime,
            700,
            650);
        var xyColor = GetPixelAt(
            xyRaster,
            ChromaticityDiagramCoordinateSystem.GetCoordinate(
                ChromaticityDiagramType.Cie1931Xy,
                new XyChromaticity(0.3127, 0.3290)),
            ChromaticityDiagramCoordinateSystem.GetBounds(
                ChromaticityDiagramType.Cie1931Xy));
        var uvColor = GetPixelAt(
            uvRaster,
            ChromaticityDiagramCoordinateSystem.GetCoordinate(
                ChromaticityDiagramType.Cie1976UvPrime,
                new XyChromaticity(0.3127, 0.3290)),
            ChromaticityDiagramCoordinateSystem.GetBounds(
                ChromaticityDiagramType.Cie1976UvPrime));

        for (var index = 0; index < 3; index++)
        {
            Assert.IsLessThanOrEqualTo(2, Math.Abs(xyColor[index] - uvColor[index]));
        }

        Assert.IsGreaterThan((byte)245, xyColor[0]);
        Assert.IsGreaterThan((byte)245, xyColor[1]);
        Assert.AreEqual(255, xyColor[3]);
    }

    [TestMethod]
    public void Rasterize_RejectsInvalidSize()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ChromaticityDiagramRasterizer.Rasterize(
                ChromaticityDiagramType.Cie1931Xy,
                0,
                10));
    }

    private static byte[] GetPixelAt(
        ChromaticityDiagramRaster raster,
        DiagramCoordinate coordinate,
        DiagramBounds bounds)
    {
        var pixelX = Math.Clamp(
            (int)((coordinate.Horizontal - bounds.MinimumHorizontal) / bounds.Width * raster.Width),
            0,
            raster.Width - 1);
        var pixelY = Math.Clamp(
            (int)((bounds.MaximumVertical - coordinate.Vertical) / bounds.Height * raster.Height),
            0,
            raster.Height - 1);
        var offset = pixelY * raster.Stride + pixelX * 4;
        return raster.BgraPixels.Slice(offset, 4).ToArray();
    }
}
