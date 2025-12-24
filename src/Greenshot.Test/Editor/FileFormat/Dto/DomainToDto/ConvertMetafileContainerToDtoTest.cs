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
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertMetafileContainerToDtoTest
{
    [Fact]
    public void ConvertDomainToDto_MetafileContainer_Returns_MetafileContainerDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var metafilePath = Path.Combine("TestData", "Images", "Logo_G_with_Border.emf");
        using var metafileStream = File.OpenRead(metafilePath);

        var metafileContainer = new MetafileContainer(metafileStream, surface);

        // Act
        var result = ConvertDomainToDto.ToDto(metafileContainer);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(metafileContainer.Left, result.Left);
        Assert.Equal(metafileContainer.Top, result.Top);
        Assert.Equal(metafileContainer.Width, result.Width);
        Assert.Equal(metafileContainer.Height, result.Height);

        // Ensure the metafile was serialized
        Assert.NotNull(result.MetafileData);
        Assert.True(result.MetafileData.Length > 0); 
    }
}
