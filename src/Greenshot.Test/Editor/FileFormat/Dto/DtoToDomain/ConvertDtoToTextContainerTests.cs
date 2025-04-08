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
public class ConvertDtoToTextContainerTests
{
    [Fact]
    public void ConvertDtoToDomain_TextContainerDto_Returns_TextContainer()
    {
        // Arrange
        var colorRed = Color.Red;
        var colorTransparent = Color.Transparent;
        var fontFamilyName = FontFamily.GenericSansSerif.Name;
        var dto = new TextContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Text = "Hello, greenshot!",
            Fields = [
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.LINE_THICKNESS),
                    Scope = nameof(TextContainer),
                    Value = new IntFieldValueDto
                    {
                        Value = 3 // default in TextContainer is 2
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.LINE_COLOR),
                    Scope = nameof(TextContainer),
                    Value = new ColorFieldValueDto
                    {
                        Value = colorRed // default in TextContainer is Red
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.SHADOW),
                    Scope = nameof(TextContainer),
                    Value = new BoolFieldValueDto
                    {
                        Value = false // default in TextContainer is true
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FONT_ITALIC),
                    Scope = nameof(TextContainer),
                    Value = new BoolFieldValueDto
                    {
                        Value = true // default in TextContainer is false
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FONT_BOLD),
                    Scope = nameof(TextContainer),
                    Value = new BoolFieldValueDto
                    {
                        Value = true // default in TextContainer is false
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FILL_COLOR),
                    Scope = nameof(TextContainer),
                    Value = new ColorFieldValueDto
                    {
                        Value = colorTransparent // default in TextContainer is Transparent
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FONT_FAMILY),
                    Scope = nameof(TextContainer),
                    Value = new StringFieldValueDto
                    {
                        Value = "Arial" // default in TextContainer is GenericSansSerif.Name
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FONT_SIZE),
                    Scope = nameof(TextContainer),
                    Value = new SingleFieldValueDto
                    {
                        Value = 12f // default in TextContainer is 11f
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.TEXT_HORIZONTAL_ALIGNMENT),
                    Scope = nameof(TextContainer),
                    Value = new StringAlignmentFieldValueDto
                    {
                        Value = StringAlignment.Far // default in TextContainer is StringAlignment.Center
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.TEXT_VERTICAL_ALIGNMENT),
                    Scope = nameof(TextContainer),
                    Value = new StringAlignmentFieldValueDto
                    {
                        Value = StringAlignment.Far // default in TextContainer is StringAlignment.Center
                    }
                }
            ]
        };

        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null) as TextContainer;

        // Assert
        var resultLineThickness = result.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = result.GetFieldValue(FieldType.LINE_COLOR);
        var resultShadow = result.GetFieldValue(FieldType.SHADOW);
        var resultFontItalic = result.GetFieldValue(FieldType.FONT_ITALIC);
        var resultFontBold = result.GetFieldValue(FieldType.FONT_BOLD);
        var resultFillColor = result.GetFieldValue(FieldType.FILL_COLOR);
        var resultFontFamily = result.GetFieldValue(FieldType.FONT_FAMILY);
        var resultFontSize = result.GetFieldValue(FieldType.FONT_SIZE);
        var resultTextHorizontalAlignment = result.GetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT);
        var resultTextVerticalAlignment = result.GetFieldValue(FieldType.TEXT_VERTICAL_ALIGNMENT);

        Assert.NotNull(result);
        Assert.Equal(dto.Left, result.Left);
        Assert.Equal(dto.Top, result.Top);
        Assert.Equal(dto.Width, result.Width);
        Assert.Equal(dto.Height, result.Height);
        Assert.Equal(dto.Text, result.Text);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(3, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(colorRed, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorRed)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.False((bool)resultShadow);

        Assert.NotNull(resultFontItalic);
        Assert.IsType<bool>(resultFontItalic);
        Assert.True((bool)resultFontItalic);

        Assert.NotNull(resultFontBold);
        Assert.IsType<bool>(resultFontBold);
        Assert.True((bool)resultFontBold);

        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(colorTransparent, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorTransparent)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultFontFamily);
        Assert.IsType<string>(resultFontFamily);
        Assert.Equal("Arial", resultFontFamily);

        Assert.NotNull(resultFontSize);
        Assert.IsType<float>(resultFontSize);
        Assert.Equal(12f, resultFontSize);

        Assert.NotNull(resultTextHorizontalAlignment);
        Assert.IsType<StringAlignment>(resultTextHorizontalAlignment);
        Assert.Equal(StringAlignment.Far, resultTextHorizontalAlignment);

        Assert.NotNull(resultTextVerticalAlignment);
        Assert.IsType<StringAlignment>(resultTextVerticalAlignment);
        Assert.Equal(StringAlignment.Far, resultTextVerticalAlignment);
    }
}
