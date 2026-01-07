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
public class ConvertSpeechbubbleContainerToDtoTests
{
    [Fact]
    public void ConvertDomainToDto_SpeechbubbleContainer_Returns_SpeechbubbleContainerDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var speechbubbleContainer = new SpeechbubbleContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Text = "Hello, greenshot!",
            StoredTargetGripperLocation = new Point(30, 40)
        };
        // see SpeechbubbleContainer.InitializeFields() for defaults
        var defaultLineThickness = 2;
        var defaultLineColor = Color.Blue;
        var defaultFillColor = Color.White;
        var defaultShadow = false;
        var defaultFontItalic = false;
        var defaultFontBold = true;
        var defaultFontFamily = FontFamily.GenericSansSerif.Name;
        var defaultFontSize = 20f;
        var defaultTextHorizontalAlignment = StringAlignment.Center;
        var defaultTextVerticalAlignment = StringAlignment.Center;

        // Act
        var result = ConvertDomainToDto.ToDto(speechbubbleContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultFillColor = DtoHelper.GetFieldValue(result, FieldType.FILL_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);
        var resultFontItalic = DtoHelper.GetFieldValue(result, FieldType.FONT_ITALIC);
        var resultFontBold = DtoHelper.GetFieldValue(result, FieldType.FONT_BOLD);
        var resultFontFamily = DtoHelper.GetFieldValue(result, FieldType.FONT_FAMILY);
        var resultFontSize = DtoHelper.GetFieldValue(result, FieldType.FONT_SIZE);
        var resultTextHorizontalAlignment = DtoHelper.GetFieldValue(result, FieldType.TEXT_HORIZONTAL_ALIGNMENT);
        var resultTextVerticalAlignment = DtoHelper.GetFieldValue(result, FieldType.TEXT_VERTICAL_ALIGNMENT);

        Assert.NotNull(result);
        Assert.Equal(speechbubbleContainer.Left, result.Left);
        Assert.Equal(speechbubbleContainer.Top, result.Top);
        Assert.Equal(speechbubbleContainer.Width, result.Width);
        Assert.Equal(speechbubbleContainer.Height, result.Height);
        Assert.Equal(speechbubbleContainer.Text, ((SpeechbubbleContainerDto)result).Text);
        Assert.Equal(speechbubbleContainer.StoredTargetGripperLocation.X, ((SpeechbubbleContainerDto)result).StoredTargetGripperLocation.X);
        Assert.Equal(speechbubbleContainer.StoredTargetGripperLocation.Y, ((SpeechbubbleContainerDto)result).StoredTargetGripperLocation.Y);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(defaultLineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(defaultLineColor, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(defaultLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(defaultFillColor, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(defaultFillColor)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(defaultShadow, (bool)resultShadow);

        Assert.NotNull(resultFontItalic);
        Assert.IsType<bool>(resultFontItalic);
        Assert.Equal(defaultFontItalic, (bool)resultFontItalic);

        Assert.NotNull(resultFontBold);
        Assert.IsType<bool>(resultFontBold);
        Assert.Equal(defaultFontBold, (bool)resultFontBold);

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
    public void ConvertDomainToDto_SpeechbubbleContainer_with_Field_Values_Returns_SpeechbubbleContainerDto_with_same_Values()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var colorWhite = Color.White;
        var fontFamily = FontFamily.GenericSansSerif.Name;
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var speechbubbleContainer = new SpeechbubbleContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Text = "Hello, greenshot!",
            StoredTargetGripperLocation = new Point(30, 40)
        };
        speechbubbleContainer.SetFieldValue(FieldType.LINE_THICKNESS, 2);
        speechbubbleContainer.SetFieldValue(FieldType.LINE_COLOR, colorBlue);
        speechbubbleContainer.SetFieldValue(FieldType.FILL_COLOR, colorWhite);
        speechbubbleContainer.SetFieldValue(FieldType.SHADOW, false);
        speechbubbleContainer.SetFieldValue(FieldType.FONT_ITALIC, false);
        speechbubbleContainer.SetFieldValue(FieldType.FONT_BOLD, true);
        speechbubbleContainer.SetFieldValue(FieldType.FONT_FAMILY, fontFamily);
        speechbubbleContainer.SetFieldValue(FieldType.FONT_SIZE, 20f);
        speechbubbleContainer.SetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT, StringAlignment.Center);
        speechbubbleContainer.SetFieldValue(FieldType.TEXT_VERTICAL_ALIGNMENT, StringAlignment.Center);

        // Act
        var result = ConvertDomainToDto.ToDto(speechbubbleContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultFillColor = DtoHelper.GetFieldValue(result, FieldType.FILL_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);
        var resultFontItalic = DtoHelper.GetFieldValue(result, FieldType.FONT_ITALIC);
        var resultFontBold = DtoHelper.GetFieldValue(result, FieldType.FONT_BOLD);
        var resultFontFamily = DtoHelper.GetFieldValue(result, FieldType.FONT_FAMILY);
        var resultFontSize = DtoHelper.GetFieldValue(result, FieldType.FONT_SIZE);
        var resultTextHorizontalAlignment = DtoHelper.GetFieldValue(result, FieldType.TEXT_HORIZONTAL_ALIGNMENT);
        var resultTextVerticalAlignment = DtoHelper.GetFieldValue(result, FieldType.TEXT_VERTICAL_ALIGNMENT);

        Assert.NotNull(result);
        Assert.Equal(speechbubbleContainer.Left, result.Left);
        Assert.Equal(speechbubbleContainer.Top, result.Top);
        Assert.Equal(speechbubbleContainer.Width, result.Width);
        Assert.Equal(speechbubbleContainer.Height, result.Height);
        Assert.Equal(speechbubbleContainer.Text, ((SpeechbubbleContainerDto)result).Text);
        Assert.Equal(speechbubbleContainer.StoredTargetGripperLocation.X, ((SpeechbubbleContainerDto)result).StoredTargetGripperLocation.X);
        Assert.Equal(speechbubbleContainer.StoredTargetGripperLocation.Y, ((SpeechbubbleContainerDto)result).StoredTargetGripperLocation.Y);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(2,resultLineThickness);
        
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
