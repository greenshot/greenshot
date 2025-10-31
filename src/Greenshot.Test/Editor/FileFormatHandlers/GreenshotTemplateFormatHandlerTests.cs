/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Drawing;
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Test.Editor.FileFormatHandlers;

/// <summary>
/// Tests for the <see cref="GreenshotTemplateFormatHandler"/> class.
/// </summary>
[Collection("DefaultCollection")]
public class GreenshotTemplateFormatHandlerTests
{
    /// <summary>
    /// Verifies that calling <see cref="GreenshotTemplateFormatHandler.SaveTemplateToFile"/>
    /// with a surface containing two arrow containers results in a new file being created.
    /// The test also ensures cleanup by deleting the temporary file.
    /// </summary>
    [Fact]
    public void SaveTemplateToFile_ShouldSaveAndCreateFile()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        surface.AddElement(new ArrowContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        });
        surface.AddElement(new ArrowContainer(surface)
        {
            Left = 110,
            Top = 120,
            Width = 200,
            Height = 150
        });
        var handler = new GreenshotTemplateFormatHandler();
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.gst");

        try
        {
            // Act
            handler.SaveTemplateToFile(tempFile, surface);

            // Assert
            Assert.True(File.Exists(tempFile));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Verifies that a template saved with <see cref="GreenshotTemplateFormatHandler.SaveTemplateToFile"/>
    /// can be successfully loaded using <see cref="GreenshotTemplateFormatHandler.LoadTemplateFromFile"/>.
    /// The test checks if the loaded surface contains the same elements as the original surface.
    /// </summary>
    [Fact]
    public void LoadTemplateFromFile_FromSavedTemplate_ShouldLoadSurfaceWithElements()
    {
        // Arrange
        var originalSurface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var lineColor = Color.Red;
        var arrow1 = new ArrowContainer(originalSurface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        arrow1.SetFieldValue(FieldType.LINE_THICKNESS, 3);
        arrow1.SetFieldValue(FieldType.LINE_COLOR, lineColor);
        arrow1.SetFieldValue(FieldType.ARROWHEADS, ArrowContainer.ArrowHeadCombination.BOTH);

        var arrow2 = new ArrowContainer(originalSurface)
        {
            Left = 110,
            Top = 120,
            Width = 200,
            Height = 150
        };
        arrow2.SetFieldValue(FieldType.LINE_THICKNESS, 5);
        arrow2.SetFieldValue(FieldType.LINE_COLOR, lineColor);
        arrow2.SetFieldValue(FieldType.SHADOW, false);

        originalSurface.AddElement(arrow1);
        originalSurface.AddElement(arrow2);
        var handler = new GreenshotTemplateFormatHandler();
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.gst");

        try
        {
            handler.SaveTemplateToFile(tempFile, originalSurface);

            var newSurface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();

            // Act
            handler.LoadTemplateFromFile(tempFile, newSurface);

            // Assert
            Assert.Equal(2, newSurface.Elements.Count);
            Assert.All(newSurface.Elements, e => Assert.IsType<ArrowContainer>(e));

            var loadedArrow1 = (ArrowContainer)newSurface.Elements[0];
            Assert.NotNull(loadedArrow1);
            Assert.Equal(10, loadedArrow1.Left);
            Assert.Equal(20, loadedArrow1.Top);
            Assert.Equal(100, loadedArrow1.Width);
            Assert.Equal(50, loadedArrow1.Height);
            Assert.Equal(3, loadedArrow1.GetFieldValue(FieldType.LINE_THICKNESS));
            var resultLineColor1 = loadedArrow1.GetFieldValue(FieldType.LINE_COLOR);
            Assert.True(DtoHelper.CompareColorValue(lineColor, (Color)resultLineColor1),
            $"The color values are different. expected:{DtoHelper.ArgbString(lineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor1)}");

            Assert.Equal(ArrowContainer.ArrowHeadCombination.BOTH, loadedArrow1.GetFieldValue(FieldType.ARROWHEADS));

            var loadedArrow2 = (ArrowContainer)newSurface.Elements[1];
            Assert.NotNull(loadedArrow2);
            Assert.Equal(110, loadedArrow2.Left);
            Assert.Equal(120, loadedArrow2.Top);
            Assert.Equal(200, loadedArrow2.Width);
            Assert.Equal(150, loadedArrow2.Height);
            Assert.Equal(5, loadedArrow2.GetFieldValue(FieldType.LINE_THICKNESS));
            var resultLineColor2 = loadedArrow1.GetFieldValue(FieldType.LINE_COLOR);
            Assert.True(DtoHelper.CompareColorValue(lineColor, (Color)resultLineColor2),
            $"The color values are different. expected:{DtoHelper.ArgbString(lineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor2)}");
            Assert.False((bool)loadedArrow2.GetFieldValue(FieldType.SHADOW));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
