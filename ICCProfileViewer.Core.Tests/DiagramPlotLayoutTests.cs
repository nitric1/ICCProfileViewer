using System;
using ICCProfileViewer.Core.Colorimetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.Core.Tests;

[TestClass]
public sealed class DiagramPlotLayoutTests
{
    [TestMethod]
    public void Create_PreservesDataScaleAndCentersPlot()
    {
        var bounds = ChromaticityDiagramCoordinateSystem.GetBounds(
            ChromaticityDiagramType.Cie1931Xy);

        var layout = DiagramPlotLayout.Create(bounds, 1000, 700, 50);

        Assert.AreEqual(layout.PlotArea.Width / bounds.Width, layout.Scale, 0.0000001);
        Assert.AreEqual(layout.PlotArea.Height / bounds.Height, layout.Scale, 0.0000001);
        Assert.AreEqual(233.3333333, layout.PlotArea.X, 0.0000001);
        Assert.AreEqual(50, layout.PlotArea.Y, 0.0000001);
        Assert.AreEqual(533.3333333, layout.PlotArea.Width, 0.0000001);
        Assert.AreEqual(600, layout.PlotArea.Height, 0.0000001);
    }

    [TestMethod]
    public void ProjectAndUnproject_RoundTripWithInvertedVerticalAxis()
    {
        var layout = DiagramPlotLayout.Create(
            ChromaticityDiagramCoordinateSystem.GetBounds(
                ChromaticityDiagramType.Cie1976UvPrime),
            800,
            800,
            40);
        var source = new DiagramCoordinate(0.1978, 0.4683);

        var viewport = layout.Project(source);
        var success = layout.TryUnproject(viewport, out var result);

        Assert.IsTrue(success);
        Assert.AreEqual(source.Horizontal, result.Horizontal, 0.0000001);
        Assert.AreEqual(source.Vertical, result.Vertical, 0.0000001);
        Assert.IsLessThan(
            layout.Project(new DiagramCoordinate(0, 0.1)).Vertical,
            layout.Project(new DiagramCoordinate(0, 0.6)).Vertical);
    }

    [TestMethod]
    public void TryUnproject_RejectsLetterboxArea()
    {
        var layout = DiagramPlotLayout.Create(
            ChromaticityDiagramCoordinateSystem.GetBounds(
                ChromaticityDiagramType.Cie1931Xy),
            1000,
            700,
            50);

        Assert.IsFalse(layout.TryUnproject(new DiagramCoordinate(100, 350), out _));
    }

    [TestMethod]
    public void Create_RejectsViewportConsumedByPadding()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            DiagramPlotLayout.Create(
                ChromaticityDiagramCoordinateSystem.GetBounds(
                    ChromaticityDiagramType.Cie1931Xy),
                100,
                100,
                50));
    }

    [TestMethod]
    public void CoordinateSystem_ConvertsTheSameXyForBothDiagrams()
    {
        var xy = new XyChromaticity(0.3127, 0.3290);

        var xyCoordinate = ChromaticityDiagramCoordinateSystem.GetCoordinate(
            ChromaticityDiagramType.Cie1931Xy,
            xy);
        var uvCoordinate = ChromaticityDiagramCoordinateSystem.GetCoordinate(
            ChromaticityDiagramType.Cie1976UvPrime,
            xy);

        Assert.AreEqual(0.3127, xyCoordinate.Horizontal, 0.0000001);
        Assert.AreEqual(0.3290, xyCoordinate.Vertical, 0.0000001);
        Assert.AreEqual(0.1978300066, uvCoordinate.Horizontal, 0.0000001);
        Assert.AreEqual(0.4683199949, uvCoordinate.Vertical, 0.0000001);
    }
}
