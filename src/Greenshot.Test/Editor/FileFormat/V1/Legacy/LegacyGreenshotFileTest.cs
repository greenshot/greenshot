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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Editor.FileFormat.V1.Legacy;
using Greenshot.Test.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.V1.Legacy;

[Collection("DefaultCollection")]
public class LegacyGreenshotFileTest
{
    public static IEnumerable<object[]> TestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "Surface_with_Image_800x400.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "LineContainer_lt_200_200_w_400.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "RectangleContainer_lt_100_200_wh_150_80.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "ArrowContainer_lt_100_200_wh_400_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "EllipseContainer_lt_200_200_wh_400_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "HighlightContainer_TextFilter_lt_310_70_wh_195_60.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "TextContainer_lt_300_200_wh_300_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "IconContainer_lt_400_200_wh_32_32.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "ImageContainer_lt_300_200_wh_100_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "ObfuscateContainer_BlurFilter_lt_130_70_wh_180_70.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "SpeechbubbleContainer_lt_200_200_wh_150_80.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "StepLabelContainer_lt_200_200_lt_500_300.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "FreehandContainer_with_4_points.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "Surface_with_11_different_DrawableContainer.greenshot")];

        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "Surface_with_Image_800x400.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "LineContainer_lt_200_200_w_400.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "RectangleContainer_lt_100_200_wh_150_80.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "ArrowContainer_lt_100_200_wh_400_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "EllipseContainer_lt_200_200_wh_400_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "HighlightContainer_TextFilter_lt_310_70_wh_195_60.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "TextContainer_lt_300_200_wh_300_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "IconContainer_lt_400_200_wh_32_32.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "ImageContainer_lt_300_200_wh_100_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "MetafileContainer_lt_300_200_wh_120_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "ObfuscateContainer_BlurFilter_lt_130_70_wh_180_70.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "SpeechbubbleContainer_lt_200_200_wh_150_80.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "StepLabelContainer_lt_200_200_lt_500_300.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "SvgContainer_lt_300_200_wh_120_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "FreehandContainer_with_4_points.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "Surface_with_14_different_DrawableContainer.greenshot")];

        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.04", "Surface_with_11_different_DrawableContainer.greenshot")];
    }

    /// <summary>
    /// This is more or less a test while develop.
    /// A very simple test that loads a legacy Greenshot file and check if the image and container list are not null and the deserialization throws no exception.
    /// A more detailed test is done in the <see cref="LoadGreenshotSurfaceTests"/>.
    /// </summary>
    /// <param name="filePath"></param>
    [Theory]
    [MemberData(nameof(TestData))]
    public void LoadLegacyGreenshotFileTest(string filePath)
    {
        // Arrange
        var surfaceFileStream = File.OpenRead(filePath);

        // Act
        // We create a copy of the bitmap, so everything else can be disposed
        surfaceFileStream.Position = 0;
        using Image tmpImage = Image.FromStream(surfaceFileStream, true, true);
        var fileImage = ImageHelper.Clone(tmpImage);

        var containerList = LegacyFileHelper.GetContainerListFromGreenshotfile(surfaceFileStream);

        // Assert
        Assert.NotNull(fileImage);
        Assert.NotNull(containerList);
    }

}