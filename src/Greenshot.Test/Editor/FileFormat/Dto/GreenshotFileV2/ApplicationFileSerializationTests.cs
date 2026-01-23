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
using Greenshot.Editor.FileFormat;
using Greenshot.Editor.FileFormat.Dto;
using MessagePack;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

public class ApplicationFileSerializationTests
{
    /// <summary>
    /// Tests the serialization and deserialization process of an <see cref="GreenshotFile"/> object.
    /// </summary>
    /// <remarks>This test verifies that an <see cref="GreenshotFile"/> object can be correctly converted to
    /// its DTO representation, serialized, deserialized and then converted back to <see cref="GreenshotFile"/>.</remarks>
    [Fact]
    public void SerializeDeserialize_ApplicationFile()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var lineContainer = new LineContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        var rectangleContainer = new RectangleContainer(surface)
        {
            Left = 30,
            Top = 40,
            Width = 200,
            Height = 80
        };
        var domainList = new DrawableContainerList { lineContainer, rectangleContainer };
        var image = new Bitmap(10, 10);
        var domain = new GreenshotFile
        {
            ContainerList = domainList,
            Image = image,
            MetaInformation = new GreenshotFileMetaInformation
            {
                SchemaVersion = GreenshotFileVersionHandler.CurrentSchemaVersion
            }
        };

        // Act
        var dto = ConvertDomainToDto.ToDto(domain);
        var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = MessagePackSerializer.Deserialize<GreenshotFileDto>(serialized);
        var result = ConvertDtoToDomain.ToDomain(deserializedDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(domain.MetaInformation.SchemaVersion, result.MetaInformation.SchemaVersion);
        Assert.NotNull(result.Image);
        Assert.NotNull(result.ContainerList);
        Assert.Equal(2, result.ContainerList.Count);
        Assert.IsType<LineContainer>(result.ContainerList[0]);
        Assert.IsType<RectangleContainer>(result.ContainerList[1]);
        Assert.Equal(lineContainer.Top, result.ContainerList[0].Top);
        Assert.Equal(lineContainer.Left, result.ContainerList[0].Left);
        Assert.Equal(lineContainer.Width, result.ContainerList[0].Width);
        Assert.Equal(lineContainer.Height, result.ContainerList[0].Height);
        Assert.Equal(rectangleContainer.Top, result.ContainerList[1].Top);
        Assert.Equal(rectangleContainer.Left, result.ContainerList[1].Left);
        Assert.Equal(rectangleContainer.Width, result.ContainerList[1].Width);
        Assert.Equal(rectangleContainer.Height, result.ContainerList[1].Height);
    }
}
