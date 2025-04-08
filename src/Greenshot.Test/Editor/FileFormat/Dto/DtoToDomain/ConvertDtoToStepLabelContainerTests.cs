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
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Base.Interfaces.Drawing;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;

[Collection("DefaultCollection")]
public class ConvertDtoToStepLabelContainerTests
{
    [Fact]
    public void ConvertDtoToDomain_StepLabelContainerDto_Returns_StepLabelContainer()
    {
        // Arrange
        var dto = new StepLabelContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Number = 2,
            CounterStart = 1
        };
        // see StepLabelContainer.InitializeFields() for defaults
        var defaultFillColor = System.Drawing.Color.DarkRed;
        var defaultLineColor = System.Drawing.Color.White;
        var defaultFlags = FieldFlag.COUNTER;

        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null);

        // Assert
        var resultFillColor = result.GetFieldValue(Greenshot.Editor.Drawing.Fields.FieldType.FILL_COLOR);
        var resultLineColor = result.GetFieldValue(Greenshot.Editor.Drawing.Fields.FieldType.LINE_COLOR);
        var resultFlags = result.GetFieldValue(Greenshot.Editor.Drawing.Fields.FieldType.FLAGS);

        Assert.NotNull(result);
        Assert.Equal(dto.Left, result.Left);
        Assert.Equal(dto.Top, result.Top);
        Assert.Equal(dto.Width, result.Width);
        Assert.Equal(dto.Height, result.Height);
        Assert.Equal(dto.Number, result.Number);
        Assert.Equal(dto.CounterStart, result.CounterStart);

        Assert.NotNull(resultFillColor);
        Assert.IsType<System.Drawing.Color>(resultFillColor);
        Assert.Equal(defaultFillColor.ToArgb(), ((System.Drawing.Color)resultFillColor).ToArgb());

        Assert.NotNull(resultLineColor);
        Assert.IsType<System.Drawing.Color>(resultLineColor);
        Assert.Equal(defaultLineColor.ToArgb(), ((System.Drawing.Color)resultLineColor).ToArgb());

        Assert.NotNull(resultFlags);
        Assert.IsType<FieldFlag>(resultFlags);
        Assert.Equal(defaultFlags, (FieldFlag)resultFlags);
    }
}
