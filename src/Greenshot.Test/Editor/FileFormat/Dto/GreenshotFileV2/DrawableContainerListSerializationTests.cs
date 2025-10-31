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
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using MessagePack;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

public class DrawableContainerListSerializationTests
{
    /// <summary>
    /// Tests the serialization and deserialization process of an <see cref="DrawableContainerList"/> object.
    /// </summary>
    /// <remarks>This test verifies that an <see cref="DrawableContainerList"/> object can be correctly converted to
    /// its DTO representation, serialized, deserialized and then converted back to <see cref="DrawableContainerList"/>.</remarks>
    [Fact]
    public void SerializeDeserialize_DrawableContainerList()
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

        // Act
        var dto = ConvertDomainToDto.ToDto(domainList);
        var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = MessagePackSerializer.Deserialize<DrawableContainerListDto>(serialized);
        var result = ConvertDtoToDomain.ToDomain(deserializedDto) as DrawableContainerList;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<LineContainer>(result[0]);
        Assert.IsType<RectangleContainer>(result[1]);

        Assert.Equal(lineContainer.Top, result[0].Top);
        Assert.Equal(lineContainer.Left, result[0].Left);
        Assert.Equal(lineContainer.Width, result[0].Width);
        Assert.Equal(lineContainer.Height, result[0].Height);

        Assert.Equal(rectangleContainer.Top, result[1].Top);
        Assert.Equal(rectangleContainer.Left, result[1].Left);
        Assert.Equal(rectangleContainer.Width, result[1].Width);
        Assert.Equal(rectangleContainer.Height, result[1].Height);
    }

    /// <summary>
    /// Tests the serialization and deserialization process of an <see cref="DrawableContainerList"/> object that contains all possible drawable container.
    /// </summary>
    /// <remarks>It only checks the types to proof <see cref="UnionAttribute"/> of <see cref="DrawableContainerDto"/></remarks>
    [Fact]
    public void SerializeDeserialize_DrawableContainerList_with_all_Container()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var metafilePath = Path.Combine("TestData", "Images", "Logo_G_with_Border.emf");
        using var metafileStream = File.OpenRead(metafilePath);

        var svgFilePath = Path.Combine("TestData", "Images", "Logo_G_with_Border.svg");
        using var svgStream = File.OpenRead(svgFilePath);


        var domainList = new DrawableContainerList
        {
            new LineContainer(surface),
            new RectangleContainer(surface),
            new ImageContainer(surface),
            new IconContainer(surface),
            new TextContainer(surface),
            new SpeechbubbleContainer(surface),
            new ArrowContainer(surface),
            new CursorContainer(surface),
            new EllipseContainer(surface),
            new FreehandContainer(surface),
            new HighlightContainer(surface),
            new MetafileContainer(metafileStream, surface),
            new ObfuscateContainer(surface),
            new StepLabelContainer(surface),
            new SvgContainer(svgStream, surface)
        };

        // Act
        var dto = ConvertDomainToDto.ToDto(domainList);
        var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = MessagePackSerializer.Deserialize<DrawableContainerListDto>(serialized);
        var result = ConvertDtoToDomain.ToDomain(deserializedDto) as DrawableContainerList;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(domainList.Count, result.Count);
        Assert.IsType<LineContainer>(result[0]);
        Assert.IsType<RectangleContainer>(result[1]);
        Assert.IsType<ImageContainer>(result[2]);
        Assert.IsType<IconContainer>(result[3]);
        Assert.IsType<TextContainer>(result[4]);
        Assert.IsType<SpeechbubbleContainer>(result[5]);
        Assert.IsType<ArrowContainer>(result[6]);
        Assert.IsType<CursorContainer>(result[7]);
        Assert.IsType<EllipseContainer>(result[8]);
        Assert.IsType<FreehandContainer>(result[9]);
        Assert.IsType<HighlightContainer>(result[10]);
        Assert.IsType<MetafileContainer>(result[11]);
        Assert.IsType<ObfuscateContainer>(result[12]);
        Assert.IsType<StepLabelContainer>(result[13]);
        Assert.IsType<SvgContainer>(result[14]);
    }

    /// <summary>
    /// Tests the serialization and deserialization of a DrawableContainerList with two HighlightContainers using different highlight colors.
    /// This is to test issue #500.
    /// </summary>
    [Fact]
    public void SerializeDeserialize_DrawableContainerList_with_two_different_HighlightContainer()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var highlight1 = new HighlightContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        var highlight2 = new HighlightContainer(surface)
        {
            Left = 30,
            Top = 40,
            Width = 120,
            Height = 60
        };
        var color1Yellow = Color.Yellow;
        var color2Lime = Color.Lime;
        // Set different highlight colors
        ((HighlightFilter)highlight1.Children[0]).SetFieldValue(FieldType.FILL_COLOR, color1Yellow);
        ((HighlightFilter)highlight2.Children[0]).SetFieldValue(FieldType.FILL_COLOR, color2Lime);

        var domainList = new DrawableContainerList { highlight1, highlight2 };

        // Act
        var dto = ConvertDomainToDto.ToDto(domainList);
        var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = MessagePackSerializer.Deserialize<DrawableContainerListDto>(serialized);
        var result = ConvertDtoToDomain.ToDomain(deserializedDto) as DrawableContainerList;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.IsType<HighlightContainer>(result[0]);
        Assert.IsType<HighlightContainer>(result[1]);

        var resultcontainer1 = (HighlightContainer)result[0];
        var resultcontainer2 = (HighlightContainer)result[1];

        var color1 = ((HighlightFilter)resultcontainer1.Children[0]).GetFieldValue(FieldType.FILL_COLOR);
        var color2 = ((HighlightFilter)resultcontainer2.Children[0]).GetFieldValue(FieldType.FILL_COLOR);

        Assert.NotNull(color1);
        Assert.IsType<Color>(color1);
        Assert.True(DtoHelper.CompareColorValue(color1Yellow, (Color)color1),
            $"The color values are different. expected:{DtoHelper.ArgbString(color1Yellow)} result:{DtoHelper.ArgbString((Color)color1)}");


        Assert.NotNull(color2);
        Assert.IsType<Color>(color2);
        Assert.True(DtoHelper.CompareColorValue(color2Lime, (Color)color2),
            $"The color values are different. expected:{DtoHelper.ArgbString(color2Lime)} result:{DtoHelper.ArgbString((Color)color2)}");

    }
}
