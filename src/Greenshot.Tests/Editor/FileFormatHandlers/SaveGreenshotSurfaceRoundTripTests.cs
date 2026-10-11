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
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
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
}
