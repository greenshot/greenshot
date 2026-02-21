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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Test.Editor.FileFormatHandlers;

/// <summary>
/// Contains unit tests for loading and validating various container types and surfaces from Greenshot files using the
/// <see cref="GreenshotFileFormatHandler.LoadGreenshotSurface"/>.
/// </summary>
/// <remarks>Every test methode uses a .greenshot file from the TestData folder, which is expected to be present.
/// For every test method there is a .greenshot file in every supported Greenshot file version.
/// </remarks>
[Collection("DefaultCollection")]
public class LoadGreenshotSurfaceTests
{
    private readonly GreenshotFileFormatHandler _greenshotFileFormatHandler = new();

    public static IEnumerable<object[]> ImageSurfaceTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "Surface_with_Image_800x400.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "Surface_with_Image_800x400.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "Surface_with_Image_800x400.gsa")];
    }

    [Theory]
    [MemberData(nameof(ImageSurfaceTestData))]
    public void LoadImageSurfaceFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(0, resultElementList.Count);
    }

    public static IEnumerable<object[]> RectangleContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "RectangleContainer_lt_100_200_wh_150_80.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "RectangleContainer_lt_100_200_wh_150_80.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "RectangleContainer_lt_100_200_wh_150_80.gsa")];
    }

    [Theory]
    [MemberData(nameof(RectangleContainerTestData))]
    public void LoadRectangleContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        var rectangleInTestfile = new Rectangle(100, 200, 150, 80);
        var rectangleLineThickness = 2;
        var rectangleLineColor = Color.Red;
        var rectangleFillColor = Color.Transparent;
        var rectangleShadow = true;

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<RectangleContainer>(resultFirstElement);
        var resultRectangleContainer = (RectangleContainer)resultFirstElement;

        Assert.Equal(rectangleInTestfile.Top, resultRectangleContainer.Top);
        Assert.Equal(rectangleInTestfile.Left, resultRectangleContainer.Left);
        Assert.Equal(rectangleInTestfile.Width, resultRectangleContainer.Width);
        Assert.Equal(rectangleInTestfile.Height, resultRectangleContainer.Height);

        var resultAdorerList = resultRectangleContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 4 Adorners for corners + 4 Adorners for the sides
        Assert.Equal(8, resultAdorerList.Count);

        var resultLineThickness = resultRectangleContainer.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = resultRectangleContainer.GetFieldValue(FieldType.LINE_COLOR);
        var resultFillColor = resultRectangleContainer.GetFieldValue(FieldType.FILL_COLOR);
        var resultShadow = resultRectangleContainer.GetFieldValue(FieldType.SHADOW);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(rectangleLineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(rectangleLineColor, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(rectangleLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(rectangleFillColor, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(rectangleFillColor)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(rectangleShadow, resultShadow);
    }

    public static IEnumerable<object[]> LineContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "LineContainer_lt_200_200_w_400.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "LineContainer_lt_200_200_w_400.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "LineContainer_lt_200_200_w_400.gsa")];
    }

    [Theory]
    [MemberData(nameof(LineContainerTestData))]
    public void LoadLineContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        var linePosInTestfile = new Point(200, 200);
        var lineWidthInTestfile = 400;
        var lineThickness = 4;
        var lineColor = Color.Blue;
        var lineShadow = false;

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<LineContainer>(resultFirstElement);
        var resultLineContainer = (LineContainer)resultFirstElement;

        Assert.Equal(linePosInTestfile.X, resultLineContainer.Left);
        Assert.Equal(linePosInTestfile.Y, resultLineContainer.Top);
        Assert.Equal(lineWidthInTestfile, resultLineContainer.Width);

        // LineContainer has no height, so it is set to -1
        Assert.Equal(-1, resultLineContainer.Height);

        var resultAdorerList = resultLineContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 2 Adorners for start and end
        Assert.Equal(2, resultAdorerList.Count);

        var resultLineThickness = resultLineContainer.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = resultLineContainer.GetFieldValue(FieldType.LINE_COLOR);
        var resultShadow = resultLineContainer.GetFieldValue(FieldType.SHADOW);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(lineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(lineColor, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(lineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(lineShadow, resultShadow);
    }

    public static IEnumerable<object[]> ArrowContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "ArrowContainer_lt_100_200_wh_400_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "ArrowContainer_lt_100_200_wh_400_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "ArrowContainer_lt_100_200_wh_400_100.gsa")];
    }

    [Theory]
    [MemberData(nameof(ArrowContainerTestData))]
    public void LoadArrowContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        var arrowPosInTestfile = new Point(100, 200);
        var arrowWidthInTestfile = 400;
        var arrowHeightInTestfile = 100;
        var lineThickness = 3;
        var lineColor = Color.Red;
        var lineShadow = true;

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<ArrowContainer>(resultFirstElement);
        var resultArrowContainer = (ArrowContainer)resultFirstElement;

        Assert.Equal(arrowPosInTestfile.X, resultArrowContainer.Left);
        Assert.Equal(arrowPosInTestfile.Y, resultArrowContainer.Top);
        Assert.Equal(arrowWidthInTestfile, resultArrowContainer.Width);
        Assert.Equal(arrowHeightInTestfile, resultArrowContainer.Height);

        var resultAdorerList = resultArrowContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 2 Adorners for start and end
        Assert.Equal(2, resultAdorerList.Count);

        var resultLineThickness = resultArrowContainer.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = resultArrowContainer.GetFieldValue(FieldType.LINE_COLOR);
        var resultShadow = resultArrowContainer.GetFieldValue(FieldType.SHADOW);
        var resultArrowheads = resultArrowContainer.GetFieldValue(FieldType.ARROWHEADS);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(lineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(lineColor, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(lineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(lineShadow, resultShadow);

        Assert.NotNull(resultArrowheads);
        Assert.IsType<ArrowContainer.ArrowHeadCombination>(resultArrowheads);
        Assert.Equal(ArrowContainer.ArrowHeadCombination.BOTH, resultArrowheads);
    }

    public static IEnumerable<object[]> EllipseContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "EllipseContainer_lt_200_200_wh_400_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "EllipseContainer_lt_200_200_wh_400_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "EllipseContainer_lt_200_200_wh_400_100.gsa")];
    }

    [Theory]
    [MemberData(nameof(EllipseContainerTestData))]
    public void LoadEllipseContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        var ellipseRectangleInTestfile = new Rectangle(200, 200, 400, 100);
        var lineThickness = 6;
        var lineColor = Color.FromArgb(255,0,255,0);
        var fillColor = Color.Blue;
        var lineShadow = false;

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<EllipseContainer>(resultFirstElement);
        var resultEllipseContainer = (EllipseContainer)resultFirstElement;

        Assert.Equal(ellipseRectangleInTestfile.Top, resultEllipseContainer.Top);
        Assert.Equal(ellipseRectangleInTestfile.Left, resultEllipseContainer.Left);
        Assert.Equal(ellipseRectangleInTestfile.Width, resultEllipseContainer.Width);
        Assert.Equal(ellipseRectangleInTestfile.Height, resultEllipseContainer.Height);

        var resultAdorerList = resultEllipseContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 4 Adorners for corners + 4 Adorners for the sides
        Assert.Equal(8, resultAdorerList.Count);

        var resultLineThickness = resultEllipseContainer.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = resultEllipseContainer.GetFieldValue(FieldType.LINE_COLOR);
        var resultFillColor = resultEllipseContainer.GetFieldValue(FieldType.FILL_COLOR);
        var resultShadow = resultEllipseContainer.GetFieldValue(FieldType.SHADOW);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(lineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(lineColor, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(lineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(fillColor, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(fillColor)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(lineShadow, resultShadow);
    }

    public static IEnumerable<object[]> FreehandContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "FreehandContainer_with_4_points.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "FreehandContainer_with_4_points.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "FreehandContainer_with_4_points.gsa")];
    }

    [Theory]
    [MemberData(nameof(FreehandContainerTestData))]
    public void LoadFreehandContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        // The Rectangle of the FreehandContainer is inherited from the Parent-Surface.Image, not from DrawingBounds.
        var freehandRectangleInTestfile = new Rectangle(0, 0, 800,400);
        var expectedCapturePoints = new List<Point>
        {
            new Point(240, 171),
            new Point(246, 170),
            new Point(246, 170),
            new Point(252, 170)
        };
        var lineThickness = 3;
        var lineColor = Color.Red;

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<FreehandContainer>(resultFirstElement);

        Assert.Equal(freehandRectangleInTestfile.Top, ((FreehandContainer)resultFirstElement).Top);
        Assert.Equal(freehandRectangleInTestfile.Left, ((FreehandContainer)resultFirstElement).Left);
        Assert.Equal(freehandRectangleInTestfile.Width, ((FreehandContainer)resultFirstElement).Width);
        Assert.Equal(freehandRectangleInTestfile.Height, ((FreehandContainer)resultFirstElement).Height);

        var resultPoints = ((FreehandContainer)resultFirstElement).CapturePoints;
        Assert.NotNull(resultPoints);
        Assert.Equal(expectedCapturePoints.Count, resultPoints.Count);
        for (int i = 0; i < expectedCapturePoints.Count; i++)
        {
            Assert.Equal(expectedCapturePoints[i], resultPoints[i]);
        }

        var resultLineThickness = ((FreehandContainer)resultFirstElement).GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = ((FreehandContainer)resultFirstElement).GetFieldValue(FieldType.LINE_COLOR);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(lineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(lineColor, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(lineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

    }

    public static IEnumerable<object[]> TextContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "TextContainer_lt_300_200_wh_300_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "TextContainer_lt_300_200_wh_300_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "TextContainer_lt_300_200_wh_300_100.gsa")];
    }

    [Theory]
    [MemberData(nameof(TextContainerTestData))]
    public void LoadTextContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        var textContainerRectInTestfile = new Rectangle(300, 200, 300, 100);
        var expectedText = "Hello Greenshot"; 
        var expectedLineThickness = 4;
        var expectedLineColor = Color.Red;
        var expectedFillColor = Color.FromArgb(255,204,255,229);
        var expectedShadow = true;
        var expectedFontFamily = "Arial";
        var expectedFontSize = 30f;
        var expectedFontBold = true;
        var expectedFontItalic = false;
        var expectedHorizontalAlignment = StringAlignment.Far;
        var expectedVerticalAlignment = StringAlignment.Far;

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<TextContainer>(resultFirstElement);
        var textContainer = (TextContainer)resultFirstElement;

        Assert.Equal(textContainerRectInTestfile.Top, textContainer.Top);
        Assert.Equal(textContainerRectInTestfile.Left, textContainer.Left);
        Assert.Equal(textContainerRectInTestfile.Width, textContainer.Width);
        Assert.Equal(textContainerRectInTestfile.Height, textContainer.Height);

        var resultAdorerList = textContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 4 Adorners for corners + 4 Adorners for the sides
        Assert.Equal(8, resultAdorerList.Count);

        Assert.Equal(expectedText, textContainer.Text);

        var resultLineThickness = textContainer.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = textContainer.GetFieldValue(FieldType.LINE_COLOR);
        var resultFillColor = textContainer.GetFieldValue(FieldType.FILL_COLOR);
        var resultShadow = textContainer.GetFieldValue(FieldType.SHADOW);
        var resultFontFamily = textContainer.GetFieldValue(FieldType.FONT_FAMILY);
        var resultFontSize = textContainer.GetFieldValue(FieldType.FONT_SIZE);
        var resultFontBold = textContainer.GetFieldValue(FieldType.FONT_BOLD);
        var resultFontItalic = textContainer.GetFieldValue(FieldType.FONT_ITALIC);
        var resultHorizontalAlignment = textContainer.GetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT);
        var resultVerticalAlignment = textContainer.GetFieldValue(FieldType.TEXT_VERTICAL_ALIGNMENT);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(expectedLineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(expectedLineColor, (Color)resultLineColor),
            $"The line color values are different. expected:{DtoHelper.ArgbString(expectedLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(expectedFillColor, (Color)resultFillColor),
            $"The fill color values are different. expected:{DtoHelper.ArgbString(expectedFillColor)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(expectedShadow, resultShadow);

        Assert.NotNull(resultFontFamily);
        Assert.IsType<string>(resultFontFamily);
        Assert.Equal(expectedFontFamily, resultFontFamily);

        Assert.NotNull(resultFontSize);
        Assert.IsType<float>(resultFontSize);
        Assert.Equal(expectedFontSize, resultFontSize);

        Assert.NotNull(resultFontBold);
        Assert.IsType<bool>(resultFontBold);
        Assert.Equal(expectedFontBold, resultFontBold);

        Assert.NotNull(resultFontItalic);
        Assert.IsType<bool>(resultFontItalic);
        Assert.Equal(expectedFontItalic, resultFontItalic);

        Assert.NotNull(resultHorizontalAlignment);
        Assert.IsType<StringAlignment>(resultHorizontalAlignment);
        Assert.Equal(expectedHorizontalAlignment, resultHorizontalAlignment);

        Assert.NotNull(resultVerticalAlignment);
        Assert.IsType<StringAlignment>(resultVerticalAlignment);
        Assert.Equal(expectedVerticalAlignment, resultVerticalAlignment);
    }

    public static IEnumerable<object[]> SpeechbubbleContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "SpeechbubbleContainer_lt_200_200_wh_150_80.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "SpeechbubbleContainer_lt_200_200_wh_150_80.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "SpeechbubbleContainer_lt_200_200_wh_150_80.gsa")];
    }

    [Theory]
    [MemberData(nameof(SpeechbubbleContainerTestData))]
    public void LoadSpeechbubbleContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        var speechbubbleRectInTestfile = new Rectangle(200, 200, 150, 80);
        var expectedText = "Point on 100x300";
        var expectedLineThickness = 3;
        var expectedLineColor = Color.Blue;
        var expectedFillColor = Color.White;
        var expectedShadow = true;
        var expectedFontFamily = "Arial";
        var expectedFontSize = 20f;
        var expectedFontBold = false;
        var expectedFontItalic = false;
        var expectedHorizontalAlignment = StringAlignment.Center;
        var expectedVerticalAlignment = StringAlignment.Center;
        var expectedTargetPoint = new Point(100, 300);

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<SpeechbubbleContainer>(resultFirstElement);
        var speechbubbleContainer = (SpeechbubbleContainer)resultFirstElement;

        Assert.Equal(speechbubbleRectInTestfile.Top, speechbubbleContainer.Top);
        Assert.Equal(speechbubbleRectInTestfile.Left, speechbubbleContainer.Left);
        Assert.Equal(speechbubbleRectInTestfile.Width, speechbubbleContainer.Width);
        Assert.Equal(speechbubbleRectInTestfile.Height, speechbubbleContainer.Height);

        Assert.Equal(expectedText, speechbubbleContainer.Text);
        Assert.Equal(expectedTargetPoint, speechbubbleContainer.StoredTargetGripperLocation);

        var resultAdorerList = speechbubbleContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 4 Adorners for corners + 4 Adorners for the sides + 1 Target Adorner
        Assert.Equal(9, resultAdorerList.Count);

        Assert.Equal(expectedTargetPoint.X, speechbubbleContainer.TargetAdorner.Location.X);
        Assert.Equal(expectedTargetPoint.Y, speechbubbleContainer.TargetAdorner.Location.Y);

        var resultLineThickness = speechbubbleContainer.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = speechbubbleContainer.GetFieldValue(FieldType.LINE_COLOR);
        var resultFillColor = speechbubbleContainer.GetFieldValue(FieldType.FILL_COLOR);
        var resultShadow = speechbubbleContainer.GetFieldValue(FieldType.SHADOW);
        var resultFontFamily = speechbubbleContainer.GetFieldValue(FieldType.FONT_FAMILY);
        var resultFontSize = speechbubbleContainer.GetFieldValue(FieldType.FONT_SIZE);
        var resultFontBold = speechbubbleContainer.GetFieldValue(FieldType.FONT_BOLD);
        var resultFontItalic = speechbubbleContainer.GetFieldValue(FieldType.FONT_ITALIC);
        var resultHorizontalAlignment = speechbubbleContainer.GetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT);
        var resultVerticalAlignment = speechbubbleContainer.GetFieldValue(FieldType.TEXT_VERTICAL_ALIGNMENT);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(expectedLineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(expectedLineColor, (Color)resultLineColor),
            $"The line color values are different. expected:{DtoHelper.ArgbString(expectedLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(expectedFillColor, (Color)resultFillColor),
            $"The fill color values are different. expected:{DtoHelper.ArgbString(expectedFillColor)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(expectedShadow, resultShadow);

        Assert.NotNull(resultFontFamily);
        Assert.IsType<string>(resultFontFamily);
        Assert.Equal(expectedFontFamily, resultFontFamily);

        Assert.NotNull(resultFontSize);
        Assert.IsType<float>(resultFontSize);
        Assert.Equal(expectedFontSize, resultFontSize);

        Assert.NotNull(resultFontBold);
        Assert.IsType<bool>(resultFontBold);
        Assert.Equal(expectedFontBold, resultFontBold);

        Assert.NotNull(resultFontItalic);
        Assert.IsType<bool>(resultFontItalic);
        Assert.Equal(expectedFontItalic, resultFontItalic);

        Assert.NotNull(resultHorizontalAlignment);
        Assert.IsType<StringAlignment>(resultHorizontalAlignment);
        Assert.Equal(expectedHorizontalAlignment, resultHorizontalAlignment);

        Assert.NotNull(resultVerticalAlignment);
        Assert.IsType<StringAlignment>(resultVerticalAlignment);
        Assert.Equal(expectedVerticalAlignment, resultVerticalAlignment);
    }

    public static IEnumerable<object[]> HighlightContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "HighlightContainer_TextFilter_lt_310_70_wh_195_60.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "HighlightContainer_TextFilter_lt_310_70_wh_195_60.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "HighlightContainer_TextFilter_lt_310_70_wh_195_60.gsa")];
    }

    [Theory]
    [MemberData(nameof(HighlightContainerTestData))]
    public void LoadHighlightContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400); 
        var highlightRectInTestfile = new Rectangle(310, 70, 195, 60);
        var expectedPreparedFilter = FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT;

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<HighlightContainer>(resultFirstElement);
        var highlightContainer = (HighlightContainer)resultFirstElement;

        Assert.Equal(highlightRectInTestfile.Top, highlightContainer.Top);
        Assert.Equal(highlightRectInTestfile.Left, highlightContainer.Left);
        Assert.Equal(highlightRectInTestfile.Width, highlightContainer.Width);
        Assert.Equal(highlightRectInTestfile.Height, highlightContainer.Height);

        var resultAdorerList = highlightContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 4 Adorners for corners + 4 Adorners for the sides
        Assert.Equal(8, resultAdorerList.Count);

        var resultPreparedFilter = highlightContainer.GetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT);

        Assert.NotNull(resultPreparedFilter);
        Assert.IsType<FilterContainer.PreparedFilter>(resultPreparedFilter);
        Assert.Equal(expectedPreparedFilter, resultPreparedFilter);

        // Check the actual filter applied based on the prepared filter
        Assert.NotNull(highlightContainer.Filters);
        Assert.Equal(1, highlightContainer.Filters.Count);
        Assert.IsType<HighlightFilter>(highlightContainer.Filters[0]);
    }

    public static IEnumerable<object[]> ObfuscateContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "ObfuscateContainer_BlurFilter_lt_130_70_wh_180_70.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "ObfuscateContainer_BlurFilter_lt_130_70_wh_180_70.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "ObfuscateContainer_BlurFilter_lt_130_70_wh_180_70.gsa")];
    }

    [Theory]
    [MemberData(nameof(ObfuscateContainerTestData))]
    public void LoadObfuscateContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400); 
        var obfuscateRectInTestfile = new Rectangle(130, 70, 180, 70);
        var expectedPreparedFilter = FilterContainer.PreparedFilter.BLUR;
        var expectedLineThickness = 1;
        var expectedLineColor = Color.FromArgb(255,0,255,0);

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<ObfuscateContainer>(resultFirstElement);
        var obfuscateContainer = (ObfuscateContainer)resultFirstElement;

        Assert.Equal(obfuscateRectInTestfile.Top, obfuscateContainer.Top);
        Assert.Equal(obfuscateRectInTestfile.Left, obfuscateContainer.Left);
        Assert.Equal(obfuscateRectInTestfile.Width, obfuscateContainer.Width);
        Assert.Equal(obfuscateRectInTestfile.Height, obfuscateContainer.Height);

        var resultAdorerList = obfuscateContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 4 Adorners for corners + 4 Adorners for the sides
        Assert.Equal(8, resultAdorerList.Count);

        var resultLineThickness = obfuscateContainer.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = obfuscateContainer.GetFieldValue(FieldType.LINE_COLOR);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(expectedLineThickness, resultLineThickness);

        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(expectedLineColor, (Color)resultLineColor),
            $"The line color values are different. expected:{DtoHelper.ArgbString(expectedLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        var resultPreparedFilter = obfuscateContainer.GetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE);

        Assert.NotNull(resultPreparedFilter);
        Assert.IsType<FilterContainer.PreparedFilter>(resultPreparedFilter);
        Assert.Equal(expectedPreparedFilter, resultPreparedFilter);

        // Check the actual filter applied based on the prepared filter
        Assert.NotNull(obfuscateContainer.Filters);
        Assert.Equal(1, obfuscateContainer.Filters.Count);
        Assert.IsType<BlurFilter>(obfuscateContainer.Filters[0]);
    }

    public static IEnumerable<object[]> IconContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "IconContainer_lt_400_200_wh_32_32.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "IconContainer_lt_400_200_wh_32_32.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "IconContainer_lt_400_200_wh_32_32.gsa")];
    }

    [Theory]
    [MemberData(nameof(IconContainerTestData))]
    public void LoadIconContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        var iconRectInTestfile = new Rectangle(400, 200, 32, 32);

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<IconContainer>(resultFirstElement);
        var iconContainer = (IconContainer)resultFirstElement;

        Assert.Equal(iconRectInTestfile.Top, iconContainer.Top);
        Assert.Equal(iconRectInTestfile.Left, iconContainer.Left);
        Assert.Equal(iconRectInTestfile.Width, iconContainer.Width);
        Assert.Equal(iconRectInTestfile.Height, iconContainer.Height);

        var resultAdorerList = iconContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 4 Adorners for corners + 4 Adorners for the sides
        Assert.Equal(8, resultAdorerList.Count);

        Assert.NotNull(iconContainer.Icon);
        Assert.Equal(iconRectInTestfile.Size, iconContainer.Icon.Size);
    }

    public static IEnumerable<object[]> StepLabelContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "StepLabelContainer_lt_200_200_lt_500_300.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "StepLabelContainer_lt_200_200_lt_500_300.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.04", "StepLabelContainer_lt_200_200_lt_500_300.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "StepLabelContainer_lt_200_200_lt_500_300.gsa")];
    }

    [Theory]
    [MemberData(nameof(StepLabelContainerTestData))]
    public void LoadStepLabelContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        var stepLabel1Pos = new Point(200, 200);
        var stepLabel2Pos = new Point(500, 300);
        var stepLabelSize = new Size(30, 30);
        var expectedFillColor = Color.Blue;
        var expectedLineColor = Color.White;
        var expectedShadow = false;
        var expectedLineThickness = 0;

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(2, resultElementList.Count);

        // Assertions for the first StepLabelContainer
        var resultFirstElement = resultElementList[0];
        Assert.NotNull(resultFirstElement);
        Assert.IsType<StepLabelContainer>(resultFirstElement);
        var stepLabelContainer1 = (StepLabelContainer)resultFirstElement;

        Assert.Equal(stepLabel1Pos.X, stepLabelContainer1.Left);
        Assert.Equal(stepLabel1Pos.Y, stepLabelContainer1.Top);
        Assert.Equal(stepLabelSize.Width, stepLabelContainer1.Width);
        Assert.Equal(stepLabelSize.Height, stepLabelContainer1.Height);
        Assert.Equal(1, stepLabelContainer1.Number);

        var resultFillColor1 = stepLabelContainer1.GetFieldValue(FieldType.FILL_COLOR);
        var resultLineColor1 = stepLabelContainer1.GetFieldValue(FieldType.LINE_COLOR);

        Assert.NotNull(resultFillColor1);
        Assert.IsType<Color>(resultFillColor1);
        Assert.True(DtoHelper.CompareColorValue(expectedFillColor, (Color)resultFillColor1),
            $"The fill color values for StepLabelContainer 1 are different. expected:{DtoHelper.ArgbString(expectedFillColor)} result:{DtoHelper.ArgbString((Color)resultFillColor1)}");

        Assert.NotNull(resultLineColor1);
        Assert.IsType<Color>(resultLineColor1);
        Assert.True(DtoHelper.CompareColorValue(expectedLineColor, (Color)resultLineColor1),
            $"The line color values for StepLabelContainer 1 are different. expected:{DtoHelper.ArgbString(expectedLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor1)}");

        var resultShadow1 = stepLabelContainer1.GetFieldValue(FieldType.SHADOW);
        Assert.NotNull(resultShadow1);
        Assert.IsType<bool>(resultShadow1);
        Assert.Equal(expectedShadow, resultShadow1);

        var resultLineThickness1 = stepLabelContainer1.GetFieldValue(FieldType.LINE_THICKNESS);
        Assert.NotNull(resultLineThickness1);
        Assert.IsType<int>(resultLineThickness1);
        Assert.Equal(expectedLineThickness, resultLineThickness1);

        // Assertions for the second StepLabelContainer
        var resultSecondElement = resultElementList[1];
        Assert.NotNull(resultSecondElement);
        Assert.IsType<StepLabelContainer>(resultSecondElement);
        var stepLabelContainer2 = (StepLabelContainer)resultSecondElement;

        Assert.Equal(stepLabel2Pos.X, stepLabelContainer2.Left);
        Assert.Equal(stepLabel2Pos.Y, stepLabelContainer2.Top);
        Assert.Equal(stepLabelSize.Width, stepLabelContainer2.Width);
        Assert.Equal(stepLabelSize.Height, stepLabelContainer2.Height);
        Assert.Equal(2, stepLabelContainer2.Number);

        var resultFillColor2 = stepLabelContainer2.GetFieldValue(FieldType.FILL_COLOR);
        var resultLineColor2 = stepLabelContainer2.GetFieldValue(FieldType.LINE_COLOR);

        Assert.NotNull(resultFillColor2);
        Assert.IsType<Color>(resultFillColor2);
        Assert.True(DtoHelper.CompareColorValue(expectedFillColor, (Color)resultFillColor2),
            $"The fill color values for StepLabelContainer 2 are different. expected:{DtoHelper.ArgbString(expectedFillColor)} result:{DtoHelper.ArgbString((Color)resultFillColor2)}");

        Assert.NotNull(resultLineColor2);
        Assert.IsType<Color>(resultLineColor2);
        Assert.True(DtoHelper.CompareColorValue(expectedLineColor, (Color)resultLineColor2),
            $"The line color values for StepLabelContainer 2 are different. expected:{DtoHelper.ArgbString(expectedLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor2)}");

        var resultShadow2 = stepLabelContainer2.GetFieldValue(FieldType.SHADOW);
        Assert.NotNull(resultShadow2);
        Assert.IsType<bool>(resultShadow2);
        Assert.Equal(expectedShadow, resultShadow2);

        var resultLineThickness2 = stepLabelContainer2.GetFieldValue(FieldType.LINE_THICKNESS);
        Assert.NotNull(resultLineThickness2);
        Assert.IsType<int>(resultLineThickness2);
        Assert.Equal(expectedLineThickness, resultLineThickness2);
    }

    public static IEnumerable<object[]> ImageContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "ImageContainer_lt_300_200_wh_100_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "ImageContainer_lt_300_200_wh_100_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "ImageContainer_lt_300_200_wh_100_100.gsa")];
    }

    [Theory]
    [MemberData(nameof(ImageContainerTestData))]
    public void LoadImageContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        // The ImageContainer has been scaled down from (256, 256) to (100, 100)
        // TODO CHR: there is a Bug, if I open this file File_Version_1.03 manually, the size of the container is (256,256) instead of (100,100),
        // it happens in BitmapContainer_OnFieldChanged() when the shadow field is changed during TranferFieldValues() in ConvertDtoToDomain()
        // i guess its a side effect with some newly merged changes from main branch, because i dont saw this last week, or it has to do sth. with changed
        // local default values of my shadow field 
        // The strange thing is, this test here dont fail. I investigate later
        var containerImageSize = new Size(256, 256);
        var imageRectInTestfile = new Rectangle(300, 200, 100, 100);

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<ImageContainer>(resultFirstElement);
        var imageContainer = (ImageContainer)resultFirstElement;

        Assert.Equal(imageRectInTestfile.Top, imageContainer.Top);
        Assert.Equal(imageRectInTestfile.Left, imageContainer.Left);
        Assert.Equal(imageRectInTestfile.Width, imageContainer.Width);
        Assert.Equal(imageRectInTestfile.Height, imageContainer.Height);

        var resultAdorerList = imageContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 4 Adorners for corners + 4 Adorners for the sides
        Assert.Equal(8, resultAdorerList.Count);

        Assert.NotNull(imageContainer.Image);
        Assert.Equal(containerImageSize, imageContainer.Image.Size);
    }

    public static IEnumerable<object[]> SvgContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "SvgContainer_lt_300_200_wh_120_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "SvgContainer_lt_300_200_wh_120_100.gsa")];
    }

    [Theory]
    [MemberData(nameof(SvgContainerTestData))]
    public void LoadSvgContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        var svgRectInTestfile = new Rectangle(300, 200, 120, 100);

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<SvgContainer>(resultFirstElement);
        var svgContainer = (SvgContainer)resultFirstElement;

        Assert.Equal(svgRectInTestfile.Top, svgContainer.Top);
        Assert.Equal(svgRectInTestfile.Left, svgContainer.Left);
        Assert.Equal(svgRectInTestfile.Width, svgContainer.Width);
        Assert.Equal(svgRectInTestfile.Height, svgContainer.Height);

        var resultAdorerList = svgContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 4 Adorners for corners
        Assert.Equal(4, resultAdorerList.Count);

        Assert.NotNull(svgContainer.SvgContent);
        Assert.True( svgContainer.SvgContent.Length > 0);
    }

    public static IEnumerable<object[]> EmojiContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.04", "EmojiContainer_lt_100_200_wh_64_64.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "EmojiContainer_lt_100_200_wh_64_64.gsa")];
    }

    [Theory]
    [MemberData(nameof(EmojiContainerTestData))]
    public void LoadEmojiContainerFromGreenshotFile(string filePath )
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);

        var EmojiPos = new Point(100, 200);
        var EmojiSize = new Size(64, 64);
        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        var resultFirstElement = resultElementList[0];
        Assert.NotNull(resultFirstElement);
        Assert.IsType<EmojiContainer>(resultFirstElement);

        Assert.Equal(EmojiPos.X, ((EmojiContainer)resultFirstElement).Left);
        Assert.Equal(EmojiPos.Y, ((EmojiContainer)resultFirstElement).Top);

        Assert.Equal(EmojiSize.Width, ((EmojiContainer)resultFirstElement).Width);
        Assert.Equal(EmojiSize.Height, ((EmojiContainer)resultFirstElement).Height);

    }

    public static IEnumerable<object[]> MetafileContainerTestData()
    {
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "MetafileContainer_lt_300_200_wh_120_100.greenshot")];
        yield return [Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "MetafileContainer_lt_300_200_wh_120_100.gsa")];
    }

    [Theory]
    [MemberData(nameof(MetafileContainerTestData))]
    public void LoadMetafileContainerFromGreenshotFile(string filePath)
    {
        // Arrange
        var imageSizeInTestfile = new Size(800, 400);
        var metafileRectInTestfile = new Rectangle(300, 200, 120, 100);

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);
        var resultElementList = resultSurface.Elements;
        var resultFirstElement = resultSurface.Elements.FirstOrDefault();

        Assert.Equal(imageSizeInTestfile, resultSurface.Image.Size);

        Assert.NotNull(resultElementList);
        Assert.Equal(1, resultElementList.Count);

        Assert.NotNull(resultFirstElement);
        Assert.IsType<MetafileContainer>(resultFirstElement);
        var metafileContainer = (MetafileContainer)resultFirstElement;

        Assert.Equal(metafileRectInTestfile.Top, metafileContainer.Top);
        Assert.Equal(metafileRectInTestfile.Left, metafileContainer.Left);
        Assert.Equal(metafileRectInTestfile.Width, metafileContainer.Width);
        Assert.Equal(metafileRectInTestfile.Height, metafileContainer.Height);

        var resultAdorerList = metafileContainer.Adorners;
        Assert.NotNull(resultAdorerList);
        // 4 Adorners for corners
        Assert.Equal(4, resultAdorerList.Count);

        Assert.NotNull(metafileContainer.MetafileContent);
        Assert.True(metafileContainer.MetafileContent.Length > 0);
    }

    /// <summary>
    /// Tests to load a Greenshot surface from a file created with version 01.02, ensuring that the surface
    /// contains the expected number and types of drawable containers.
    /// </summary>
    /// <remarks>The test file contains all possible container types for version 01.02 </remarks>
    [Fact]
    public void LoadFromV0102GreenshotFileWithDifferentContainer()
    {
        // Arrange
        string filePath = Path.Combine("TestData", "Greenshotfile", "File_Version_1.02", "Surface_with_11_different_DrawableContainer.greenshot");

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);

        var resultElementList = resultSurface.Elements;

        Assert.NotNull(resultElementList);
        Assert.Equal(11, resultElementList.Count);

        Assert.IsType<RectangleContainer>(resultElementList[0]);
        Assert.IsType<EllipseContainer>(resultElementList[1]);
        Assert.IsType<LineContainer>(resultElementList[2]);
        Assert.IsType<ArrowContainer>(resultElementList[3]);
        Assert.IsType<FreehandContainer>(resultElementList[4]);
        Assert.IsType<TextContainer>(resultElementList[5]);
        Assert.IsType<SpeechbubbleContainer>(resultElementList[6]);
        Assert.IsType<StepLabelContainer>(resultElementList[7]);
        Assert.IsType<HighlightContainer>(resultElementList[8]);
        Assert.IsType<ObfuscateContainer>(resultElementList[9]);
        Assert.IsType<ImageContainer>(resultElementList[10]);
    }

    /// <summary>
    /// Tests to load a Greenshot surface from a file created with version 01.03, ensuring that the surface
    /// contains the expected number and types of drawable containers.
    /// </summary>
    /// <remarks>The test file contains all possible container types for version 01.03 </remarks>
    [Fact]
    public void LoadFromV0103GreenshotFileWithDifferentContainer()
    {
        // Arrange
        string filePath = Path.Combine("TestData", "Greenshotfile", "File_Version_1.03", "Surface_with_14_different_DrawableContainer.greenshot");

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);

        var resultElementList = resultSurface.Elements;

        Assert.NotNull(resultElementList);
        Assert.Equal(14, resultElementList.Count);

        Assert.IsType<RectangleContainer>(resultElementList[0]);
        Assert.IsType<EllipseContainer>(resultElementList[1]);
        Assert.IsType<LineContainer>(resultElementList[2]);
        Assert.IsType<ArrowContainer>(resultElementList[3]);
        Assert.IsType<FreehandContainer>(resultElementList[4]);
        Assert.IsType<TextContainer>(resultElementList[5]);
        Assert.IsType<SpeechbubbleContainer>(resultElementList[6]);
        Assert.IsType<StepLabelContainer>(resultElementList[7]);
        Assert.IsType<HighlightContainer>(resultElementList[8]);
        Assert.IsType<ObfuscateContainer>(resultElementList[9]);
        Assert.IsType<ImageContainer>(resultElementList[10]);
        Assert.IsType<SvgContainer>(resultElementList[11]);
        Assert.IsType<MetafileContainer>(resultElementList[12]);
        Assert.IsType<IconContainer>(resultElementList[13]); 
    }

    /// <summary>
    /// Tests to load a Greenshot surface from a file created with version 01.04, ensuring that the surface
    /// contains the expected number and types of drawable containers.
    /// </summary>
    /// <remarks>The test file contains all possible container types for version 01.04 </remarks>
    [Fact]
    public void LoadFromV0104GreenshotFileWithDifferentContainer()
    {
        // Arrange
        string filePath = Path.Combine("TestData", "Greenshotfile", "File_Version_1.04", "Surface_with_11_different_DrawableContainer.greenshot");

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);

        var resultElementList = resultSurface.Elements;

        Assert.NotNull(resultElementList);
        Assert.Equal(11, resultElementList.Count);

        Assert.IsType<ArrowContainer>(resultElementList[0]);
        Assert.IsType<EllipseContainer>(resultElementList[1]);
        Assert.IsType<FreehandContainer>(resultElementList[2]);
        Assert.IsType<HighlightContainer>(resultElementList[3]);
        Assert.IsType<IconContainer>(resultElementList[4]);
        Assert.IsType<LineContainer>(resultElementList[5]);
        Assert.IsType<ObfuscateContainer>(resultElementList[6]);
        Assert.IsType<RectangleContainer>(resultElementList[7]);
        Assert.IsType<SpeechbubbleContainer>(resultElementList[8]);
        Assert.IsType<SvgContainer>(resultElementList[9]);
        Assert.IsType<TextContainer>(resultElementList[10]);
    }

    /// <summary>
    /// Tests to load a Greenshot surface from a file created with version 02.01, ensuring that the surface
    /// contains the expected number and types of drawable containers.
    /// </summary>
    /// <remarks>The test file contains all possible container types for version 02.01 </remarks>
    [Fact]
    public void LoadFromV0201GreenshotFileWithDifferentContainer()
    {
        // Arrange
        string filePath = Path.Combine("TestData", "Greenshotfile", "File_Version_2.01", "Surface_with_14_different_DrawableContainer.gsa");

        // Act
        var resultSurface = _greenshotFileFormatHandler.LoadGreenshotSurface(filePath);

        // Assert
        Assert.NotNull(resultSurface);

        var resultElementList = resultSurface.Elements;

        Assert.NotNull(resultElementList);
        Assert.Equal(14, resultElementList.Count);

        Assert.IsType<RectangleContainer>(resultElementList[0]);
        Assert.IsType<EllipseContainer>(resultElementList[1]);
        Assert.IsType<LineContainer>(resultElementList[2]);
        Assert.IsType<ArrowContainer>(resultElementList[3]);
        Assert.IsType<FreehandContainer>(resultElementList[4]);
        Assert.IsType<TextContainer>(resultElementList[5]);
        Assert.IsType<SpeechbubbleContainer>(resultElementList[6]);
        Assert.IsType<StepLabelContainer>(resultElementList[7]);
        Assert.IsType<HighlightContainer>(resultElementList[8]);
        Assert.IsType<ObfuscateContainer>(resultElementList[9]);
        Assert.IsType<ImageContainer>(resultElementList[10]);
        Assert.IsType<SvgContainer>(resultElementList[11]);
        Assert.IsType<MetafileContainer>(resultElementList[12]);
        Assert.IsType<IconContainer>(resultElementList[13]);
    }
}