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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertTextContainerToDtoTests
{
    [Fact]
    public void ConvertDomainToDto_TextContainer_Returns_TextContainerDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var textContainer = new TextContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Text = "Hello, greenshot!"
        };

        // see TextContainer.InitializeFields() for defaults
        var defaultLineThickness = 2;
        var defaultLineColor = Color.Red;
        var defaultShadow = true;
        var defaultFontItalic = false;
        var defaultFontBold = false;
        var defaultFillColor = Color.Transparent;
        var defaultFontFamily = FontFamily.GenericSansSerif.Name;
        var defaultFontSize = 11f;
        var defaultTextHorizontalAlignment = StringAlignment.Center;
        var defaultTextVerticalAlignment = StringAlignment.Center;

        // Act
        var result = ConvertDomainToDto.ToDto(textContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);
        var resultFontItalic = DtoHelper.GetFieldValue(result, FieldType.FONT_ITALIC);
        var resultFontBold = DtoHelper.GetFieldValue(result, FieldType.FONT_BOLD);
        var resultFillColor = DtoHelper.GetFieldValue(result, FieldType.FILL_COLOR);
        var resultFontFamily = DtoHelper.GetFieldValue(result, FieldType.FONT_FAMILY);
        var resultFontSize = DtoHelper.GetFieldValue(result, FieldType.FONT_SIZE);
        var resultTextHorizontalAlignment = DtoHelper.GetFieldValue(result, FieldType.TEXT_HORIZONTAL_ALIGNMENT);
        var resultTextVerticalAlignment = DtoHelper.GetFieldValue(result, FieldType.TEXT_VERTICAL_ALIGNMENT);

        Assert.NotNull(result);
        Assert.Equal(textContainer.Left, result.Left);
        Assert.Equal(textContainer.Top, result.Top);
        Assert.Equal(textContainer.Width, result.Width);
        Assert.Equal(textContainer.Height, result.Height);
        Assert.Equal(textContainer.Text, ((TextContainerDto)result).Text);

        //Test Workaround also
        Assert.Equal(textContainer.Text, textContainer.Text);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(defaultLineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(defaultLineColor, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(defaultLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(defaultShadow, (bool)resultShadow);

        Assert.NotNull(resultFontItalic);
        Assert.IsType<bool>(resultFontItalic);
        Assert.Equal(defaultFontItalic, (bool)resultFontItalic);

        Assert.NotNull(resultFontBold);
        Assert.IsType<bool>(resultFontBold);
        Assert.Equal(defaultFontBold, (bool)resultFontBold);

        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(defaultFillColor, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(defaultFillColor)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultFontFamily);
        Assert.IsType<string>(resultFontFamily);
        Assert.Equal(defaultFontFamily, resultFontFamily);

        Assert.NotNull(resultFontSize);
        Assert.IsType<float>(resultFontSize);
        Assert.Equal(defaultFontSize, resultFontSize);

        Assert.NotNull(resultTextHorizontalAlignment);
        Assert.IsType<StringAlignment>(resultTextHorizontalAlignment);
        Assert.Equal(defaultTextHorizontalAlignment, resultTextHorizontalAlignment);

        Assert.NotNull(resultTextVerticalAlignment);
        Assert.IsType<StringAlignment>(resultTextVerticalAlignment);
        Assert.Equal(defaultTextVerticalAlignment, resultTextVerticalAlignment);
    }

    [Fact]
    public void ConvertDomainToDto_TextContainer_with_Field_Values_Returns_TextContainerDto_with_same_Values()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var colorGreen = Color.Green;
        var fontFamilyName = "Arial";
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var textContainer = new TextContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Text = "Hello, greenshot!"
        };
        textContainer.SetFieldValue(FieldType.LINE_THICKNESS, 3);
        textContainer.SetFieldValue(FieldType.LINE_COLOR, colorBlue);
        textContainer.SetFieldValue(FieldType.SHADOW, false);
        textContainer.SetFieldValue(FieldType.FONT_ITALIC, true);
        textContainer.SetFieldValue(FieldType.FONT_BOLD, true);
        textContainer.SetFieldValue(FieldType.FILL_COLOR, colorGreen);
        textContainer.SetFieldValue(FieldType.FONT_FAMILY, fontFamilyName);
        textContainer.SetFieldValue(FieldType.FONT_SIZE, 12f);
        textContainer.SetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT, StringAlignment.Far);
        textContainer.SetFieldValue(FieldType.TEXT_VERTICAL_ALIGNMENT, StringAlignment.Far);

        // Act
        var result = ConvertDomainToDto.ToDto(textContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);
        var resultFontItalic = DtoHelper.GetFieldValue(result, FieldType.FONT_ITALIC);
        var resultFontBold = DtoHelper.GetFieldValue(result, FieldType.FONT_BOLD);
        var resultFillColor = DtoHelper.GetFieldValue(result, FieldType.FILL_COLOR);
        var resultFontFamily = DtoHelper.GetFieldValue(result, FieldType.FONT_FAMILY);
        var resultFontSize = DtoHelper.GetFieldValue(result, FieldType.FONT_SIZE);
        var resultTextHorizontalAlignment = DtoHelper.GetFieldValue(result, FieldType.TEXT_HORIZONTAL_ALIGNMENT);
        var resultTextVerticalAlignment = DtoHelper.GetFieldValue(result, FieldType.TEXT_VERTICAL_ALIGNMENT);

        Assert.NotNull(result);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(3, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(colorBlue, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorBlue)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

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
        Assert.True(DtoHelper.CompareColorValue(colorGreen, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorGreen)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultFontFamily);
        Assert.IsType<string>(resultFontFamily);
        Assert.Equal(fontFamilyName, resultFontFamily);

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
