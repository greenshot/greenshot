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
using System.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.FileFormat.Dto.Fields;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;

[Collection("DefaultCollection")]
public class ConvertDtoToEllipseContainerTests
{
    [Fact]
    public void ConvertDtoToDomain_EllipseContainerDto_Returns_EllipseContainer()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var colorGreen = Color.Green;
        var dto = new EllipseContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Fields = [ new FieldDto
                {
                    FieldTypeName = nameof(FieldType.LINE_THICKNESS),
                    Scope = nameof(EllipseContainer),
                    Value = new IntFieldValueDto
                    {
                        Value = 3 // default in EllipseContainer is 2
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.LINE_COLOR),
                    Scope = nameof(EllipseContainer),
                    Value = new ColorFieldValueDto
                    {
                        Value = colorBlue // default in EllipseContainer is Red
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FILL_COLOR),
                    Scope = nameof(EllipseContainer),
                    Value = new ColorFieldValueDto
                    {
                        Value = colorGreen // default in EllipseContainer is Transparent
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.SHADOW),
                    Scope = nameof(EllipseContainer),
                    Value = new BoolFieldValueDto
                    {
                        Value = false // default in EllipseContainer is true
                    }
                }
            ]
        };

        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null);

        // Assert
        var resultLineThickness = result.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = result.GetFieldValue(FieldType.LINE_COLOR);
        var resultFillColor = result.GetFieldValue(FieldType.FILL_COLOR);
        var resultShadow = result.GetFieldValue(FieldType.SHADOW);

        Assert.NotNull(result);
        Assert.Equal(dto.Left, result.Left);
        Assert.Equal(dto.Top, result.Top);
        Assert.Equal(dto.Width, result.Width);
        Assert.Equal(dto.Height, result.Height);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(3, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(colorBlue, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorBlue)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");
        
        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(colorGreen, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorGreen)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.False((bool)resultShadow); 
    }
}
