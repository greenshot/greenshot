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
using System.Collections.Generic;
using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertFreehandContainerToDtoTests
{
    [Fact]
    public void ConvertDomainToDto_FreehandContainer_Returns_FreehandContainerDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var freehandContainer = new FreehandContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            CapturePoints = [new Point(10, 20), new Point(30, 40)]
        };
        // see FreehandContainer.InitializeFields() for defaults
        var defaultLineThickness = 3;
        var defaultLineColor = Color.Red;

        // Act
        var result = ConvertDomainToDto.ToDto(freehandContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);

        Assert.NotNull(result);
        Assert.Equal(freehandContainer.Left, result.Left);
        Assert.Equal(freehandContainer.Top, result.Top);
        Assert.Equal(freehandContainer.Width, result.Width);
        Assert.Equal(freehandContainer.Height, result.Height);
        Assert.NotNull(result.CapturePoints);
        Assert.Equal(freehandContainer.CapturePoints.Count, result.CapturePoints.Count);
        Assert.Equal(freehandContainer.CapturePoints[0].X, result.CapturePoints[0].X);
        Assert.Equal(freehandContainer.CapturePoints[0].Y, result.CapturePoints[0].Y);
        Assert.Equal(freehandContainer.CapturePoints[1].X, result.CapturePoints[1].X);
        Assert.Equal(freehandContainer.CapturePoints[1].Y, result.CapturePoints[1].Y);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(defaultLineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(defaultLineColor, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(defaultLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");
    }

    [Fact]
    public void ConvertDomainToDto_FreehandContainer_with_Field_Values_Returns_FreehandContainerDto_with_same_Values()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var freehandContainer = new FreehandContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            CapturePoints = [new Point(10, 20), new Point(30, 40)]
        };
        freehandContainer.SetFieldValue(FieldType.LINE_THICKNESS, 5);
        freehandContainer.SetFieldValue(FieldType.LINE_COLOR, colorBlue);

        // Act
        var result = ConvertDomainToDto.ToDto(freehandContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);

        Assert.NotNull(result);
        Assert.Equal(freehandContainer.Left, result.Left);
        Assert.Equal(freehandContainer.Top, result.Top);
        Assert.Equal(freehandContainer.Width, result.Width);
        Assert.Equal(freehandContainer.Height, result.Height);
        Assert.NotNull(result.CapturePoints);
        Assert.Equal(freehandContainer.CapturePoints.Count, result.CapturePoints.Count);
        Assert.Equal(freehandContainer.CapturePoints[0].X, result.CapturePoints[0].X);
        Assert.Equal(freehandContainer.CapturePoints[0].Y, result.CapturePoints[0].Y);
        Assert.Equal(freehandContainer.CapturePoints[1].X, result.CapturePoints[1].X);
        Assert.Equal(freehandContainer.CapturePoints[1].Y, result.CapturePoints[1].Y);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(5, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(colorBlue, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorBlue)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");
    }
}
