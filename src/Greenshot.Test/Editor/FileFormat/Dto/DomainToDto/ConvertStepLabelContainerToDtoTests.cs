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
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertStepLabelContainerToDtoTests
{
    [Fact]
    public void ConvertDomainToDto_StepLabelContainer_Returns_StepLabelContainerDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var stepLabelContainer = new StepLabelContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Number = 2,
            CounterStart = 1
        };
        // see StepLabelContainer.InitializeFields() for defaults
        var defaultFillColor = Color.DarkRed;
        var defaultLineColor = Color.White;
        var defaultFlags = FieldFlag.COUNTER;

        // Act
        var result = ConvertDomainToDto.ToDto(stepLabelContainer);

        // Assert
        var resultFillColor = DtoHelper.GetFieldValue(result, FieldType.FILL_COLOR);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultFlags = DtoHelper.GetFieldValue(result, FieldType.FLAGS);

        Assert.NotNull(result);
        Assert.Equal(stepLabelContainer.Left, result.Left);
        Assert.Equal(stepLabelContainer.Top, result.Top);
        Assert.Equal(stepLabelContainer.Width, result.Width);
        Assert.Equal(stepLabelContainer.Height, result.Height);
        Assert.Equal(stepLabelContainer.Number, ((StepLabelContainerDto)result).Number);
        Assert.Equal(stepLabelContainer.CounterStart, ((StepLabelContainerDto)result).CounterStart);

        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(defaultFillColor, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(defaultFillColor)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(defaultLineColor, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(defaultLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultFlags);
        Assert.IsType<FieldFlag>(resultFlags);
        Assert.Equal(defaultFlags, (FieldFlag)resultFlags);
    }

    [Fact]
    public void ConvertDomainToDto_StepLabelContainer_with_Field_Values_Returns_StepLabelContainerDto_with_same_Values()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var colorGreen = Color.Green;
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var stepLabelContainer = new StepLabelContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Number = 5,
            CounterStart = 3
        };
        stepLabelContainer.SetFieldValue(FieldType.FILL_COLOR, colorBlue);
        stepLabelContainer.SetFieldValue(FieldType.LINE_COLOR, colorGreen);
        // stays the same as default
        stepLabelContainer.SetFieldValue(FieldType.FLAGS , FieldFlag.COUNTER);

        // Act
        var result = ConvertDomainToDto.ToDto(stepLabelContainer);

        // Assert
        var resultFillColor = DtoHelper.GetFieldValue(result, FieldType.FILL_COLOR);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultFlags = DtoHelper.GetFieldValue(result, FieldType.FLAGS);

        Assert.NotNull(result);
        Assert.Equal(stepLabelContainer.Left, result.Left);
        Assert.Equal(stepLabelContainer.Top, result.Top);
        Assert.Equal(stepLabelContainer.Width, result.Width);
        Assert.Equal(stepLabelContainer.Height, result.Height);
        Assert.Equal(stepLabelContainer.Number, ((StepLabelContainerDto)result).Number);
        Assert.Equal(stepLabelContainer.CounterStart, ((StepLabelContainerDto)result).CounterStart);

        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(colorBlue, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorBlue)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(colorGreen, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorGreen)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultFlags);
        Assert.IsType<FieldFlag>(resultFlags);
        Assert.Equal(FieldFlag.COUNTER, (FieldFlag)resultFlags);
    }
}
