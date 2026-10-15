/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 */
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.V1.Legacy;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Tests.Editor.FileFormatHandlers;

[Collection(TestCollections.FileFormat)]
public class SaveGreenshotSurfaceRoundTripTests
{
    [Fact]
    public void SaveExtensions_IncludeCurrentAndLegacyFormats()
    {
        var handler = new GreenshotFileFormatHandler();

        Assert.Contains(".gsa", handler.SupportedExtensions[FileFormatHandlerActions.SaveToFile]);
        Assert.Contains(".greenshot", handler.SupportedExtensions[FileFormatHandlerActions.SaveToFile]);
    }

    [Fact]
    public void SaveWithGsaExtension_WritesCurrentArchiveFormat()
    {
        using var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        surface.Image = new Bitmap(8, 8);
        using var output = new MemoryStream();

        Assert.True(new GreenshotFileFormatHandler().TrySaveToStream((Bitmap)surface.Image, output, ".gsa", surface));

        output.Position = 0;
        using var archive = new ZipArchive(output, ZipArchiveMode.Read, true);
        Assert.NotNull(archive.GetEntry("meta.json"));
        Assert.NotNull(archive.GetEntry("content.json"));
    }

    [Fact]
    public void SaveAndLoadGreenshotFile_RoundTripsSurfaceImageAndRectangle()
    {
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        using (surface)
        {
            var sourceImage = new Bitmap(120, 80);
            using (var graphics = Graphics.FromImage(sourceImage))
            {
                graphics.Clear(Color.CornflowerBlue);
            }

            surface.Image = sourceImage;
            var rectangle = new RectangleContainer(surface)
            {
                Left = 17,
                Top = 12,
                Width = 53,
                Height = 31
            };
            surface.Elements.Add(rectangle);

            var filePath = Path.Combine(Path.GetTempPath(), $"greenshot_v1_roundtrip_{Guid.NewGuid():N}.greenshot");
            try
            {
                var handler = new GreenshotFileFormatHandler();
                using (var output = File.Create(filePath))
                {
                    Assert.True(handler.TrySaveToStream((Bitmap)surface.Image, output, ".greenshot", surface));
                }

                using (var savedFile = File.OpenRead(filePath))
                {
                    savedFile.Seek(-14, SeekOrigin.End);
                    using var reader = new BinaryReader(savedFile, Encoding.ASCII, true);
                    Assert.Equal("Greenshot01.04", Encoding.ASCII.GetString(reader.ReadBytes(14)));
                }

                using var loadedSurface = handler.LoadGreenshotSurface(filePath);
                Assert.Equal(surface.Image.Size, loadedSurface.Image.Size);
                Assert.Equal(Color.CornflowerBlue.ToArgb(), ((Bitmap)loadedSurface.Image).GetPixel(0, 0).ToArgb());

                var loadedRectangle = Assert.IsType<RectangleContainer>(Assert.Single(loadedSurface.Elements));
                Assert.Equal(rectangle.Left, loadedRectangle.Left);
                Assert.Equal(rectangle.Top, loadedRectangle.Top);
                Assert.Equal(rectangle.Width, loadedRectangle.Width);
                Assert.Equal(rectangle.Height, loadedRectangle.Height);

                var sourceFields = rectangle.GetFields().OrderBy(field => field.FieldType.Name).ToArray();
                var loadedFields = loadedRectangle.GetFields().OrderBy(field => field.FieldType.Name).ToArray();
                Assert.Equal(sourceFields.Length, loadedFields.Length);
                for (var index = 0; index < sourceFields.Length; index++)
                {
                    Assert.Equal(sourceFields[index].FieldType.Name, loadedFields[index].FieldType.Name);
                    if (sourceFields[index].Value is Color sourceColor && loadedFields[index].Value is Color loadedColor)
                    {
                        Assert.Equal(sourceColor.ToArgb(), loadedColor.ToArgb());
                    }
                    else
                    {
                        Assert.Equal(sourceFields[index].Value, loadedFields[index].Value);
                    }
                }
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
    }

    [Theory]
    [InlineData(FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT, 1, false)]
    [InlineData(FilterContainer.PreparedFilter.AREA_HIGHLIGHT, 2, true)]
    [InlineData(FilterContainer.PreparedFilter.GRAYSCALE, 1, true)]
    [InlineData(FilterContainer.PreparedFilter.MAGNIFICATION, 1, false)]
    public void SaveGreenshotFile_PreservesHighlightFilterInversion(FilterContainer.PreparedFilter preset, int filterCount, bool expectedInvert)
    {
        using var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        surface.Image = new Bitmap(120, 80);
        var highlight = new HighlightContainer(surface)
        {
            Left = 17,
            Top = 12,
            Width = 53,
            Height = 31
        };
        highlight.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, preset);
        surface.Elements.Add(highlight);
        Assert.Equal(filterCount, highlight.Filters.Count);
        Assert.All(highlight.Filters, filter => Assert.Equal(expectedInvert, filter.Invert));

        using var output = new MemoryStream();
        Assert.True(new GreenshotFileFormatHandler().TrySaveToStream((Bitmap)surface.Image, output, ".greenshot", surface));

        output.Seek(-22, SeekOrigin.End);
        using var reader = new BinaryReader(output, Encoding.ASCII, true);
        var payloadLength = reader.ReadInt64();
        Assert.Equal("Greenshot01.04", Encoding.ASCII.GetString(reader.ReadBytes(14)));
        output.Seek(-(payloadLength + 22), SeekOrigin.End);
        var savedHighlight = Assert.IsType<LegacyHighlightContainer>(Assert.Single(LegacyNrbfReader.ReadContainerList(output)));
        Assert.Equal(filterCount, savedHighlight.Children.Count);
        Assert.All(savedHighlight.Children, filter => Assert.Equal(expectedInvert, Assert.IsAssignableFrom<LegacyAbstractFilter>(filter).Invert));
    }

    [Fact]
    public void SaveAndLoadGsaFile_RoundTripsCutMarkContainer()
    {
        using var surface = (Surface)SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        surface.Image = new Bitmap(100, 100);
        var settings = new TornEdgeEffect
        {
            GenerateShadow = false,
            ToothHeight = 8,
            HorizontalToothRange = 16,
            VerticalToothRange = 18,
            Darkness = 0.45f,
            BackgroundColor = Color.FromArgb(64, 10, 20, 30)
        };

        Assert.True(surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Wave, settings));
        var cutMark = Assert.Single(surface.Elements.OfType<CutMarkContainer>());

        using var loadedSurface = SaveAndLoadFile(surface, ".gsa");

        var loadedCutMark = Assert.Single(loadedSurface.Elements.OfType<CutMarkContainer>());
        Assert.Equal(cutMark.Seed, loadedCutMark.Seed);
        Assert.Equal(cutMark.IsHorizontal, loadedCutMark.IsHorizontal);
        Assert.Equal(cutMark.Left, loadedCutMark.Left);
        Assert.Equal(cutMark.Top, loadedCutMark.Top);
        Assert.Equal(cutMark.Width, loadedCutMark.Width);
        Assert.Equal(cutMark.Height, loadedCutMark.Height);
        Assert.Equal(cutMark.ToothHeight, loadedCutMark.ToothHeight);
        Assert.Equal(cutMark.ToothRange, loadedCutMark.ToothRange);
        Assert.Equal(cutMark.GetFieldValue(FieldType.CUT_MARK_STYLE), loadedCutMark.GetFieldValue(FieldType.CUT_MARK_STYLE));
        AssertTornEdgeSettingsEqual(cutMark.GetSettings(), loadedCutMark.GetSettings());
        Assert.Equal(
            ((Color)cutMark.GetFieldValue(FieldType.FILL_COLOR)).ToArgb(),
            ((Color)loadedCutMark.GetFieldValue(FieldType.FILL_COLOR)).ToArgb());
    }

    [Fact]
    public void SaveAndLoadGsaFile_RoundTripsTornEdgeContainer()
    {
        using var surface = (Surface)SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        surface.Image = new Bitmap(100, 100);
        var settings = new TornEdgeEffect
        {
            GenerateShadow = false,
            ToothHeight = 9,
            HorizontalToothRange = 15,
            VerticalToothRange = 17,
            Edges = [true, false, true, false],
            BackgroundColor = Color.FromArgb(64, 40, 50, 60)
        };
        var tornEdges = surface.AddTornEdges(settings);

        using var loadedSurface = SaveAndLoadFile(surface, ".gsa");

        var loadedTornEdges = Assert.Single(loadedSurface.Elements.OfType<TornEdgeContainer>());
        Assert.Equal(tornEdges.Seed, loadedTornEdges.Seed);
        Assert.Equal(tornEdges.Left, loadedTornEdges.Left);
        Assert.Equal(tornEdges.Top, loadedTornEdges.Top);
        Assert.Equal(tornEdges.Width, loadedTornEdges.Width);
        Assert.Equal(tornEdges.Height, loadedTornEdges.Height);
        Assert.Equal(tornEdges.ToothHeight, loadedTornEdges.ToothHeight);
        Assert.Equal(tornEdges.ToothRange, loadedTornEdges.ToothRange);
        Assert.Equal(tornEdges.Style, loadedTornEdges.Style);
        Assert.Equal(tornEdges.Edges, loadedTornEdges.Edges);
        Assert.Equal(tornEdges.Margins, loadedTornEdges.Margins);
        AssertTornEdgeSettingsEqual(tornEdges.GetSettings(), loadedTornEdges.GetSettings());
        Assert.Equal(
            ((Color)tornEdges.GetFieldValue(FieldType.FILL_COLOR)).ToArgb(),
            ((Color)loadedTornEdges.GetFieldValue(FieldType.FILL_COLOR)).ToArgb());
    }

    [Fact]
    public void SaveAndLoadGreenshotFile_RoundTripsCutMarkContainer()
    {
        using var surface = (Surface)SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        surface.Image = new Bitmap(100, 100);
        var settings = new TornEdgeEffect
        {
            GenerateShadow = false,
            ToothHeight = 8,
            HorizontalToothRange = 16,
            VerticalToothRange = 18,
            Darkness = 0.45f,
            BackgroundColor = Color.FromArgb(64, 10, 20, 30)
        };
        Assert.True(surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Wave, settings));
        var cutMark = Assert.Single(surface.Elements.OfType<CutMarkContainer>());

        using var loadedSurface = SaveAndLoadFile(surface, ".greenshot");

        var loadedCutMark = Assert.Single(loadedSurface.Elements.OfType<CutMarkContainer>());
        Assert.Equal(cutMark.Seed, loadedCutMark.Seed);
        Assert.Equal(cutMark.IsHorizontal, loadedCutMark.IsHorizontal);
        Assert.Equal(cutMark.Left, loadedCutMark.Left);
        Assert.Equal(cutMark.Top, loadedCutMark.Top);
        Assert.Equal(cutMark.Width, loadedCutMark.Width);
        Assert.Equal(cutMark.Height, loadedCutMark.Height);
        Assert.Equal(cutMark.ToothHeight, loadedCutMark.ToothHeight);
        Assert.Equal(cutMark.ToothRange, loadedCutMark.ToothRange);
        Assert.Equal(cutMark.GetFieldValue(FieldType.CUT_MARK_STYLE), loadedCutMark.GetFieldValue(FieldType.CUT_MARK_STYLE));
        AssertTornEdgeSettingsEqual(cutMark.GetSettings(), loadedCutMark.GetSettings());
        Assert.Equal(
            ((Color)cutMark.GetFieldValue(FieldType.FILL_COLOR)).ToArgb(),
            ((Color)loadedCutMark.GetFieldValue(FieldType.FILL_COLOR)).ToArgb());
    }

    [Fact]
    public void SaveAndLoadGreenshotFile_RoundTripsTornEdgeContainer()
    {
        using var surface = (Surface)SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        surface.Image = new Bitmap(100, 100);
        var settings = new TornEdgeEffect
        {
            GenerateShadow = false,
            ToothHeight = 9,
            HorizontalToothRange = 15,
            VerticalToothRange = 17,
            Edges = [true, false, true, false],
            BackgroundColor = Color.FromArgb(64, 40, 50, 60)
        };
        var tornEdges = surface.AddTornEdges(settings);

        using var loadedSurface = SaveAndLoadFile(surface, ".greenshot");

        var loadedTornEdges = Assert.Single(loadedSurface.Elements.OfType<TornEdgeContainer>());
        Assert.Equal(tornEdges.Seed, loadedTornEdges.Seed);
        Assert.Equal(tornEdges.Left, loadedTornEdges.Left);
        Assert.Equal(tornEdges.Top, loadedTornEdges.Top);
        Assert.Equal(tornEdges.Width, loadedTornEdges.Width);
        Assert.Equal(tornEdges.Height, loadedTornEdges.Height);
        Assert.Equal(tornEdges.ToothHeight, loadedTornEdges.ToothHeight);
        Assert.Equal(tornEdges.ToothRange, loadedTornEdges.ToothRange);
        Assert.Equal(tornEdges.Style, loadedTornEdges.Style);
        Assert.Equal(tornEdges.Edges, loadedTornEdges.Edges);
        Assert.Equal(tornEdges.Margins, loadedTornEdges.Margins);
        AssertTornEdgeSettingsEqual(tornEdges.GetSettings(), loadedTornEdges.GetSettings());
        Assert.Equal(
            ((Color)tornEdges.GetFieldValue(FieldType.FILL_COLOR)).ToArgb(),
            ((Color)loadedTornEdges.GetFieldValue(FieldType.FILL_COLOR)).ToArgb());
    }

    private static ISurface SaveAndLoadFile(ISurface surface, string extension)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"greenshot_container_roundtrip_{Guid.NewGuid():N}{extension}");
        try
        {
            var handler = new GreenshotFileFormatHandler();
            using (var output = File.Create(filePath))
            {
                Assert.True(handler.TrySaveToStream((Bitmap)surface.Image, output, extension, surface));
            }

            return handler.LoadGreenshotSurface(filePath);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static void AssertTornEdgeSettingsEqual(TornEdgeEffect expected, TornEdgeEffect actual)
    {
        Assert.Equal(expected.Darkness, actual.Darkness);
        Assert.Equal(expected.ShadowSize, actual.ShadowSize);
        Assert.Equal(expected.ShadowOffset.X, actual.ShadowOffset.X);
        Assert.Equal(expected.ShadowOffset.Y, actual.ShadowOffset.Y);
        Assert.Equal(expected.ToothHeight, actual.ToothHeight);
        Assert.Equal(expected.HorizontalToothRange, actual.HorizontalToothRange);
        Assert.Equal(expected.VerticalToothRange, actual.VerticalToothRange);
        Assert.Equal(expected.GenerateShadow, actual.GenerateShadow);
        Assert.Equal(expected.BackgroundColor.ToArgb(), actual.BackgroundColor.ToArgb());
        Assert.Equal(expected.Edges, actual.Edges);
        Assert.Equal(expected.Seed, actual.Seed);
    }
}
