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
using System.Collections.Generic;
using System.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.FileFormat.Dto.Fields;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;

[Collection("DefaultCollection")]
public class ConvertDtoToFreehandContainerTests
{
    [Fact]
    public void ConvertDtoToDomain_FreehandContainerDto_Returns_FreehandContainer()
    {
        // Arrange
        var colorRed = System.Drawing.Color.Red;
        var dto = new FreehandContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            CapturePoints = [new PointDto { X = 10, Y = 20 }, new PointDto { X = 30, Y = 40 }],
            Fields = [ new FieldDto
                {
                    FieldTypeName = nameof(FieldType.LINE_THICKNESS),
                    Scope = nameof(FreehandContainer),
                    Value = new IntFieldValueDto
                    {
                        Value = 2 // default in FreehandContainer is 3
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.LINE_COLOR),
                    Scope = nameof(FreehandContainer),
                    Value = new ColorFieldValueDto
                    {
                        Value = colorRed // default in FreehandContainer is Red
                    }
                }]
        };

        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null);

        // Assert
        var resultLineThickness = result.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = result.GetFieldValue(FieldType.LINE_COLOR);

        Assert.NotNull(result);
        Assert.IsType<FreehandContainer>(result);
        Assert.Equal(dto.Left, result.Left);
        Assert.Equal(dto.Top, result.Top);
        Assert.Equal(dto.Width, result.Width);
        Assert.Equal(dto.Height, result.Height);
        Assert.NotNull(result.CapturePoints);
        Assert.Equal(dto.CapturePoints.Count, result.CapturePoints.Count);
        Assert.Equal(dto.CapturePoints[0].X, result.CapturePoints[0].X);
        Assert.Equal(dto.CapturePoints[0].Y, result.CapturePoints[0].Y);
        Assert.Equal(dto.CapturePoints[1].X, result.CapturePoints[1].X);
        Assert.Equal(dto.CapturePoints[1].Y, result.CapturePoints[1].Y);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(2, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(colorRed, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorRed)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

    }
}
