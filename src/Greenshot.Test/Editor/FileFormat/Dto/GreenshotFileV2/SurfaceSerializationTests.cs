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
using Greenshot.Editor.FileFormat;
using Greenshot.Editor.FileFormat.Dto;
using MessagePack;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

[Collection("DefaultCollection")]
public class SurfaceSerializationTests
{
    /// <summary>
    /// Tests the serialization and deserialization process of a plain Surface without any elements.
    /// </summary>
    [Fact]
    public void SerializeDeserialize_PlainSurface()
    {
        // Arrange
        var image = new Bitmap(200, 150);
        var surface = new Surface(image);

        // Act
        var greenshotFile = GreenshotFileVersionHandler.CreateGreenshotFile(surface);
        var dto = ConvertDomainToDto.ToDto(greenshotFile);
        var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = MessagePackSerializer.Deserialize<GreenshotFileDto>(serialized);
        var deserializedGreenshotFile = ConvertDtoToDomain.ToDomain(deserializedDto) as GreenshotFile;
        var resultSurface = GreenshotFileVersionHandler.CreateSurface(deserializedGreenshotFile);

        // Assert
        Assert.NotNull(resultSurface);
        Assert.Equal(image.Width, resultSurface.Image.Width);
        Assert.Equal(image.Height, resultSurface.Image.Height);
        Assert.Empty(resultSurface.Elements);

    }

    /// <summary>
    /// Tests the serialization and deserialization process of a surface with two StepLabelContainer, where the counter starts at 3.
    /// </summary>
    [Fact]
    public void SerializeDeserialize_SurfaceWith2StepLabelWichStartsAt3()
    {
        // Arrange
        var image = new Bitmap(100, 100);
        var surface = new Surface(image);
        surface.CounterStart = 3;
        var stepLabel1 = new StepLabelContainer(surface)
        {
            Left = 5,
            Top = 10,
            Width = 50,
            Height = 25
        };
        var stepLabel2 = new StepLabelContainer(surface)
        {
            Left = 15,
            Top = 20,
            Width = 60,
            Height = 35
        };
        surface.AddElement(stepLabel1, false, false);
        surface.AddElement(stepLabel2, false, false);

        // Act
        var greenshotfile = GreenshotFileVersionHandler.CreateGreenshotFile(surface);
        var dto= ConvertDomainToDto.ToDto(greenshotfile);
        var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = MessagePackSerializer.Deserialize<GreenshotFileDto>(serialized);
        var deserializedGreenshotFile = ConvertDtoToDomain.ToDomain(deserializedDto) as GreenshotFile;

        var resultSurface = GreenshotFileVersionHandler.CreateSurface(deserializedGreenshotFile);

        // Assert
        Assert.NotNull(resultSurface);
        var resultContainerList = resultSurface.Elements;

        Assert.NotNull(resultContainerList);
        Assert.Equal(2, resultContainerList.Count);
        Assert.IsType<StepLabelContainer>(resultContainerList[0]);   
        Assert.IsType<StepLabelContainer>(resultContainerList[1]);

        var resultStepLabel1 = resultContainerList[0] as StepLabelContainer;
        var resultStepLabel2 = resultContainerList[1] as StepLabelContainer;
        Assert.NotNull(resultStepLabel1);
        Assert.NotNull(resultStepLabel2);
        Assert.Equal(stepLabel1.Left, resultStepLabel1.Left);
        Assert.Equal(stepLabel1.Top, resultStepLabel1.Top);
        Assert.Equal(stepLabel1.Width, resultStepLabel1.Width);
        Assert.Equal(stepLabel1.Height, resultStepLabel1.Height);
        Assert.Equal(stepLabel2.Left, resultStepLabel2.Left);
        Assert.Equal(stepLabel2.Top, resultStepLabel2.Top);
        Assert.Equal(stepLabel2.Width, resultStepLabel2.Width);
        Assert.Equal(stepLabel2.Height, resultStepLabel2.Height);

        // both StepLabels should have the same CounterStart, which is 3
        Assert.Equal(3, resultStepLabel1.CounterStart);
        Assert.Equal(3, resultStepLabel2.CounterStart);

        // The StepLabels should have the correct numbers, which are 3 and 4
        Assert.Equal(3, resultStepLabel1.Number);
        Assert.Equal(4, resultStepLabel2.Number);
    }
}