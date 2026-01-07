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
public class ConvertDtoToSpeechbubbleContainerTests
{
    [Fact]
    public void ConvertDtoToDomain_SpeechbubbleContainerDto_Returns_SpeechbubbleContainer()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var colorWhite = Color.White;
        var fontFamily = FontFamily.GenericSansSerif.Name;
        var dto = new SpeechbubbleContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Text = "Hello, greenshot!",
            StoredTargetGripperLocation = new PointDto { X = 30, Y = 40 },
            Fields = [ new FieldDto
                {
                    FieldTypeName = nameof(FieldType.LINE_THICKNESS),
                    Scope = nameof(SpeechbubbleContainer),
                    Value = new IntFieldValueDto
                    {
                        Value = 2 // default in SpeechbubbleContainer is 2
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.LINE_COLOR),
                    Scope = nameof(SpeechbubbleContainer),
                    Value = new ColorFieldValueDto
                    {
                        Value = colorBlue // default in SpeechbubbleContainer is Blue
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FILL_COLOR),
                    Scope = nameof(SpeechbubbleContainer),
                    Value = new ColorFieldValueDto
                    {
                        Value = colorWhite // default in SpeechbubbleContainer is White
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.SHADOW),
                    Scope = nameof(SpeechbubbleContainer),
                    Value = new BoolFieldValueDto
                    {
                        Value = false // default in SpeechbubbleContainer is true
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FONT_ITALIC),
                    Scope = nameof(SpeechbubbleContainer),
                    Value = new BoolFieldValueDto
                    {
                        Value = false // default in SpeechbubbleContainer is false
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FONT_BOLD),
                    Scope = nameof(SpeechbubbleContainer),
                    Value = new BoolFieldValueDto
                    {
                        Value = true // default in SpeechbubbleContainer is true
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FONT_FAMILY),
                    Scope = nameof(SpeechbubbleContainer),
                    Value = new StringFieldValueDto
                    {
                        Value = fontFamily // default in SpeechbubbleContainer is "Microsoft Sans Serif"
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.FONT_SIZE),
                    Scope = nameof(SpeechbubbleContainer),
                    Value = new SingleFieldValueDto
                    {
                        Value = 20f // default in SpeechbubbleContainer is 20f
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.TEXT_HORIZONTAL_ALIGNMENT),
                    Scope = nameof(SpeechbubbleContainer),
                    Value = new StringAlignmentFieldValueDto
                    {
                        Value = StringAlignment.Center // default in SpeechbubbleContainer is Center
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.TEXT_VERTICAL_ALIGNMENT),
                    Scope = nameof(SpeechbubbleContainer),
                    Value = new StringAlignmentFieldValueDto
                    {
                        Value = StringAlignment.Center // default in SpeechbubbleContainer is Center
                    }
                }
            ]
        };

        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null) as SpeechbubbleContainer;

        // Assert
        var resultLineThickness = result.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = result.GetFieldValue(FieldType.LINE_COLOR);
        var resultFillColor = result.GetFieldValue(FieldType.FILL_COLOR);
        var resultShadow = result.GetFieldValue(FieldType.SHADOW);
        var resultFontItalic = result.GetFieldValue(FieldType.FONT_ITALIC);
        var resultFontBold = result.GetFieldValue(FieldType.FONT_BOLD);
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
        Assert.Equal(dto.StoredTargetGripperLocation.X, result.StoredTargetGripperLocation.X);
        Assert.Equal(dto.StoredTargetGripperLocation.Y, result.StoredTargetGripperLocation.Y);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(2, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(colorBlue, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorBlue)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");
        
        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(colorWhite, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorWhite)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.False((bool)resultShadow);
        
        Assert.NotNull(resultFontItalic);
        Assert.IsType<bool>(resultFontItalic);
        Assert.False((bool)resultFontItalic);
        
        Assert.NotNull(resultFontBold);
        Assert.IsType<bool>(resultFontBold);
        Assert.True((bool)resultFontBold);
        
        Assert.NotNull(resultFontFamily);
        Assert.IsType<string>(resultFontFamily);
        Assert.Equal(fontFamily, resultFontFamily);
        
        Assert.NotNull(resultFontSize);
        Assert.IsType<float>(resultFontSize);
        Assert.Equal(20f, resultFontSize);
        
        Assert.NotNull(resultTextHorizontalAlignment);
        Assert.IsType<StringAlignment>(resultTextHorizontalAlignment);
        Assert.Equal(StringAlignment.Center, resultTextHorizontalAlignment);
        
        Assert.NotNull(resultTextVerticalAlignment);
        Assert.IsType<StringAlignment>(resultTextVerticalAlignment);
        Assert.Equal(StringAlignment.Center, resultTextVerticalAlignment);
    }
}
