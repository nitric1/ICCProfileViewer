using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ICCProfileViewer.Core.Colorimetry;

public static class SpectralLocusCatalog
{
    public const int FirstWavelengthNanometers = 360;
    public const int LastWavelengthNanometers = 830;
    public const int WavelengthStepNanometers = 1;
    public const string DatasetDoi = "10.25039/CIE.DS.mifmy4x4";
    public const string DatasetLicense = "CC BY-SA 4.0";
    public const string DatasetSha256 =
        "5a3f0582ea0907867c7a2718051bbdc04f39e758d8c09e628930efc62386e399";

    internal const string ResourceName =
        "ICCProfileViewer.Core.Colorimetry.Data.CIE_cc_1931_2deg.csv";

    private const int ExpectedPointCount =
        (LastWavelengthNanometers - FirstWavelengthNanometers) /
        WavelengthStepNanometers + 1;

    public static IReadOnlyList<SpectralLocusPoint> Cie1931TwoDegree { get; } = Load();

    public static SpectralLocusPoint Get(int wavelengthNanometers)
    {
        if (wavelengthNanometers < FirstWavelengthNanometers ||
            wavelengthNanometers > LastWavelengthNanometers ||
            (wavelengthNanometers - FirstWavelengthNanometers) % WavelengthStepNanometers != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(wavelengthNanometers),
                wavelengthNanometers,
                $"Wavelength must be between {FirstWavelengthNanometers} and " +
                $"{LastWavelengthNanometers} nm in {WavelengthStepNanometers} nm steps.");
        }

        var index =
            (wavelengthNanometers - FirstWavelengthNanometers) /
            WavelengthStepNanometers;
        return Cie1931TwoDegree[index];
    }

    private static IReadOnlyList<SpectralLocusPoint> Load()
    {
        using var stream = typeof(SpectralLocusCatalog).Assembly
            .GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException(
                $"Embedded spectral-locus dataset '{ResourceName}' was not found.");
        using var reader = new StreamReader(
            stream,
            Encoding.ASCII,
            detectEncodingFromByteOrderMarks: true);

        var points = new List<SpectralLocusPoint>(ExpectedPointCount);
        var expectedWavelength = FirstWavelengthNanometers;

        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0)
            {
                continue;
            }

            points.Add(ParsePoint(line, expectedWavelength));
            expectedWavelength += WavelengthStepNanometers;
        }

        if (points.Count != ExpectedPointCount)
        {
            throw new InvalidDataException(
                $"The spectral-locus dataset contains {points.Count} points; " +
                $"expected {ExpectedPointCount}.");
        }

        return points.AsReadOnly();
    }

    private static SpectralLocusPoint ParsePoint(string line, int expectedWavelength)
    {
        var fields = line.Split(',');
        if (fields.Length != 4 ||
            !int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var wavelength) ||
            !double.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            throw new InvalidDataException(
                $"The spectral-locus dataset contains an invalid row for {expectedWavelength} nm.");
        }

        if (wavelength != expectedWavelength)
        {
            throw new InvalidDataException(
                $"The spectral-locus dataset contains {wavelength} nm where " +
                $"{expectedWavelength} nm was expected.");
        }

        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z) ||
            x < 0 || y < 0 || z < 0 ||
            Math.Abs(x + y + z - 1) > 0.00001)
        {
            throw new InvalidDataException(
                $"The spectral-locus dataset contains invalid chromaticity coordinates " +
                $"for {wavelength} nm.");
        }

        var xy = new XyChromaticity(x, y);
        var uvPrime = ChromaticityConverter.ToUvPrime(xy)
            ?? throw new InvalidDataException(
                $"The spectral-locus coordinate for {wavelength} nm cannot be converted to u'v'.");

        return new SpectralLocusPoint(wavelength, xy, uvPrime);
    }
}
