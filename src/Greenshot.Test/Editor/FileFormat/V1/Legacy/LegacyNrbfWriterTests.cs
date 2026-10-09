/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Formats.Nrbf;
using System.IO;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.V1.Legacy;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.V1.Legacy;

[Collection("DefaultCollection")]
public class LegacyNrbfWriterTests
{
    [Fact]
    public void WriteEditorFileWithRectangle_CreatesManualCompatibilityArtifact()
    {
        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "V1WriterManualTest-NonDefaultFields.greenshot");
        using var screenshot = new Bitmap(320, 200);
        using (var graphics = Graphics.FromImage(screenshot))
        {
            graphics.Clear(Color.LightBlue);
            graphics.FillRectangle(Brushes.DarkBlue, 32, 24, 128, 96);
        }

        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            var containers = new LegacyDrawableContainerList
            {
                new LegacyRectangleContainer
                {
                    Left = 48,
                    Top = 40,
                    Width = 140,
                    Height = 90,
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 7),
                        CreateField("LINE_COLOR", Color.Magenta),
                        CreateField("FILL_COLOR", Color.LightGreen),
                        CreateField("SHADOW", false)
                    ]
                }
            };
            LegacyNrbfWriter.WriteEditorFile(output, screenshot, containers);
        }

        Console.WriteLine($"Manual compatibility file created: {outputPath}");
        Assert.True(File.Exists(outputPath));

        using var stream = File.OpenRead(outputPath);
        using (var image = Image.FromStream(stream, true, true))
        {
            Assert.Equal(new Size(320, 200), image.Size);
        }

        stream.Seek(-22, SeekOrigin.End);
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, true);
        var payloadLength = reader.ReadInt64();
        Assert.InRange(payloadLength, 1, stream.Length - 22);
        stream.Seek(-14, SeekOrigin.End);
        Assert.Equal("Greenshot01.04", System.Text.Encoding.ASCII.GetString(reader.ReadBytes(14)));
        stream.Seek(-(payloadLength + 22), SeekOrigin.End);
        var readContainers = LegacyNrbfReader.ReadContainerList(stream);
        var rectangle = Assert.IsType<LegacyRectangleContainer>(Assert.Single(readContainers));
        Assert.Equal(48, rectangle.Left);
        Assert.Equal(40, rectangle.Top);
        Assert.Equal(140, rectangle.Width);
        Assert.Equal(90, rectangle.Height);
        Assert.Equal(7, rectangle.Fields[0].Value);
        Assert.Equal(Color.Magenta, rectangle.Fields[1].Value);
        Assert.Equal(Color.LightGreen, rectangle.Fields[2].Value);
        Assert.Equal(false, rectangle.Fields[3].Value);
    }

    [Fact]
    public void WriteEditorFileWithArrowContainer_CreatesManualCompatibilityArtifact()
    {
        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "V1WriterManualArrowContainer.greenshot");
        using var screenshot = new Bitmap(320, 200);
        using (var graphics = Graphics.FromImage(screenshot))
        {
            graphics.Clear(Color.WhiteSmoke);
        }

        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            var containers = new LegacyDrawableContainerList
            {
                new LegacyArrowContainer
                {
                    Left = 60,
                    Top = 50,
                    Width = 120,
                    Height = 70,
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 4),
                        CreateField("LINE_COLOR", Color.Red),
                        CreateField("ARROWHEADS", ArrowContainer.ArrowHeadCombination.BOTH),
                        CreateField("SHADOW", true)
                    ]
                }
            };
            LegacyNrbfWriter.WriteEditorFile(output, screenshot, containers);
        }

        Console.WriteLine($"Manual arrow-compatibility file created: {outputPath}");
        Assert.True(File.Exists(outputPath));

        using var stream = File.OpenRead(outputPath);
        using var image = Image.FromStream(stream, true, true);
        Assert.Equal(new Size(320, 200), image.Size);

        stream.Seek(-22, SeekOrigin.End);
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, true);
        var payloadLength = reader.ReadInt64();
        Assert.InRange(payloadLength, 1, stream.Length - 22);
        stream.Seek(-(payloadLength + 22), SeekOrigin.End);
        var readContainers = LegacyNrbfReader.ReadContainerList(stream);
        var arrow = Assert.IsType<LegacyArrowContainer>(Assert.Single(readContainers));
        Assert.Equal(60, arrow.Left);
        Assert.Equal(50, arrow.Top);
        Assert.Equal(120, arrow.Width);
        Assert.Equal(70, arrow.Height);
        Assert.Equal(4, arrow.Fields[0].Value);
        Assert.Equal(Color.Red, arrow.Fields[1].Value);
        Assert.Equal(ArrowContainer.ArrowHeadCombination.BOTH, arrow.Fields[2].Value);
        Assert.Equal(true, arrow.Fields[3].Value);
    }

    [Fact]
    public void WriteEditorFileWithAllSupportedContainers_CreatesManualCompatibilityArtifact()
    {
        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "V1WriterManualAllSupportedContainers.greenshot");
        var testDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Images");
        var backgroundImagePath = Path.Combine(testDataDirectory, "Screenshot_background_800x400.png");
        var sourceImagePath = Path.Combine(testDataDirectory, "Logo_G_with_Border.png");
        var sourceIconPath = Path.Combine(testDataDirectory, "Greenshot.ico");
        var svgPath = Path.Combine(testDataDirectory, "Logo_G_with_Border.svg");
        var metafilePath = Path.Combine(testDataDirectory, "Logo_G_with_Border.emf");
        using var backgroundImage = Image.FromFile(backgroundImagePath);
        using var expectedBackground = new Bitmap(backgroundImage);
        using var screenshot = new Bitmap(backgroundImage);
        using var sourceImage = Image.FromFile(sourceImagePath);
        using var sourceIcon = new Icon(sourceIconPath);
        using var cursorColorLayer = new Bitmap(16, 16);
        using var cursorMaskLayer = new Bitmap(16, 16);
        using (var cursorGraphics = Graphics.FromImage(cursorColorLayer))
        {
            cursorGraphics.Clear(Color.Transparent);
            cursorGraphics.FillPolygon(Brushes.White,
            [
                new Point(1, 1), new Point(1, 13), new Point(4, 10),
                new Point(7, 15), new Point(9, 14), new Point(6, 9), new Point(12, 9)
            ]);
            cursorGraphics.FillPolygon(Brushes.Black,
            [
                new Point(2, 3), new Point(2, 11), new Point(4, 8),
                new Point(7, 13), new Point(8, 13), new Point(5, 8), new Point(10, 8)
            ]);
        }
        using (var maskGraphics = Graphics.FromImage(cursorMaskLayer))
        {
            maskGraphics.Clear(Color.Transparent);
            maskGraphics.FillPolygon(Brushes.White,
            [
                new Point(1, 1), new Point(1, 13), new Point(4, 10),
                new Point(7, 15), new Point(9, 14), new Point(6, 9), new Point(12, 9)
            ]);
        }
        var svgBytes = File.ReadAllBytes(svgPath);
        var metafileBytes = File.ReadAllBytes(metafilePath);
        using var svgContent = new MemoryStream(svgBytes, false);
        using var metafileContent = new MemoryStream(metafileBytes, false);

        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            var containers = new LegacyDrawableContainerList
            {
                new LegacyRectangleContainer
                {
                    Left = 20,
                    Top = 25,
                    Width = 130,
                    Height = 90,
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 4),
                        CreateField("LINE_COLOR", Color.DarkOrange),
                        CreateField("FILL_COLOR", Color.PeachPuff),
                        CreateField("SHADOW", false)
                    ]
                },
                new LegacyEllipseContainer
                {
                    Left = 180,
                    Top = 25,
                    Width = 130,
                    Height = 90,
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 4),
                        CreateField("LINE_COLOR", Color.DarkGreen),
                        CreateField("FILL_COLOR", Color.LightGreen),
                        CreateField("SHADOW", false)
                    ]
                },
                new LegacyLineContainer
                {
                    Left = 30,
                    Top = 160,
                    Width = 120,
                    Height = 70,
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 6),
                        CreateField("LINE_COLOR", Color.DarkBlue),
                        CreateField("SHADOW", false)
                    ]
                },
                new LegacyArrowContainer
                {
                    Left = 190,
                    Top = 160,
                    Width = 150,
                    Height = 70,
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 5),
                        CreateField("LINE_COLOR", Color.Red),
                        CreateField("ARROWHEADS", ArrowContainer.ArrowHeadCombination.BOTH),
                        CreateField("SHADOW", false)
                    ]
                },
                new LegacyFreehandContainer
                {
                    Left = 360,
                    Top = 35,
                    Width = 120,
                    Height = 80,
                    CapturePoints =
                    [
                        new Point(0, 0),
                        new Point(35, 30),
                        new Point(70, 5),
                        new Point(110, 65)
                    ],
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 5),
                        CreateField("LINE_COLOR", Color.DarkViolet)
                    ]
                },
                new LegacySpeechbubbleContainer
                {
                    Left = 420,
                    Top = 25,
                    Width = 190,
                    Height = 95,
                    Text = "Point on screen",
                    StoredTargetGripperLocation = new Point(510, 155),
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 4, "SpeechbubbleContainer"),
                        CreateField("LINE_COLOR", Color.Blue, "SpeechbubbleContainer"),
                        CreateField("FILL_COLOR", Color.LightYellow, "SpeechbubbleContainer"),
                        CreateField("SHADOW", false, "SpeechbubbleContainer"),
                        CreateField("FONT_FAMILY", "Arial", "SpeechbubbleContainer"),
                        CreateField("FONT_SIZE", 16f, "SpeechbubbleContainer"),
                        CreateField("FONT_BOLD", true, "SpeechbubbleContainer"),
                        CreateField("FONT_ITALIC", false, "SpeechbubbleContainer"),
                        CreateField("TEXT_HORIZONTAL_ALIGNMENT", StringAlignment.Center, "SpeechbubbleContainer"),
                        CreateField("TEXT_VERTICAL_ALIGNMENT", StringAlignment.Center, "SpeechbubbleContainer")
                    ]
                },
                new LegacyTextContainer
                {
                    Left = 380,
                    Top = 140,
                    Width = 230,
                    Height = 130,
                    Text = "Arial text",
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 5, "TextContainer"),
                        CreateField("LINE_COLOR", Color.Purple, "TextContainer"),
                        CreateField("FILL_COLOR", Color.Transparent, "TextContainer"),
                        CreateField("SHADOW", false, "TextContainer"),
                        CreateField("FONT_FAMILY", "Arial", "TextContainer"),
                        CreateField("FONT_SIZE", 18f, "TextContainer"),
                        CreateField("FONT_BOLD", false, "TextContainer"),
                        CreateField("FONT_ITALIC", false, "TextContainer"),
                        CreateField("TEXT_HORIZONTAL_ALIGNMENT", StringAlignment.Center, "TextContainer"),
                        CreateField("TEXT_VERTICAL_ALIGNMENT", StringAlignment.Center, "TextContainer")
                    ]
                },
                new LegacyImageContainer
                {
                    Left = 25,
                    Top = 275,
                    Width = 120,
                    Height = 70,
                    Image = sourceImage,
                    Fields =
                    [
                        CreateField("SHADOW", false, "ImageContainer")
                    ]
                },
                new LegacyIconContainer
                {
                    Left = 190,
                    Top = 275,
                    Width = 64,
                    Height = 64,
                    Icon = sourceIcon
                },
                new LegacyHighlightContainer
                {
                    Left = 30,
                    Top = 130,
                    Width = 140,
                    Height = 90,
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 1, "HighlightContainer"),
                        CreateField("LINE_COLOR", Color.Lime, "HighlightContainer"),
                        CreateField("SHADOW", false, "HighlightContainer"),
                        CreateField("PREPARED_FILTER_HIGHLIGHT", FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT, "HighlightContainer")
                    ],
                    Children =
                    [
                        new LegacyHighlightFilter
                        {
                            Fields = [CreateField("FILL_COLOR", Color.Red, "HighlightFilter")]
                        }
                    ]
                },
                new LegacyObfuscateContainer
                {
                    Left = 190,
                    Top = 130,
                    Width = 150,
                    Height = 90,
                    Fields =
                    [
                        CreateField("LINE_THICKNESS", 1, "ObfuscateContainer"),
                        CreateField("LINE_COLOR", Color.Lime, "ObfuscateContainer"),
                        CreateField("SHADOW", false, "ObfuscateContainer"),
                        CreateField("PREPARED_FILTER_OBFUSCATE", FilterContainer.PreparedFilter.BLUR, "ObfuscateContainer")
                    ],
                    Children =
                    [
                        new LegacyBlurFilter
                        {
                            Fields =
                            [
                                CreateField("BLUR_RADIUS", 12, "BlurFilter"),
                                CreateField("PREVIEW_QUALITY", 1d, "BlurFilter")
                            ]
                        }
                    ]
                },
                new LegacyStepLabelContainer
                {
                    Left = 270,
                    Top = 275,
                    Width = 40,
                    Height = 40,
                    Number = 1,
                    CounterStart = 16,
                    Fields =
                    [
                        CreateField("FILL_COLOR", Color.DarkRed, "StepLabelContainer"),
                        CreateField("LINE_COLOR", Color.White, "StepLabelContainer"),
                        CreateField("FLAGS", FieldFlag.COUNTER, "StepLabelContainer")
                    ]
                },
                new LegacyStepLabelContainer
                {
                    Left = 330, Top = 275, Width = 40, Height = 40, Number = 2, CounterStart = 16,
                    Fields =
                    [
                        CreateField("FILL_COLOR", Color.DarkRed, "StepLabelContainer"),
                        CreateField("LINE_COLOR", Color.White, "StepLabelContainer"),
                        CreateField("FLAGS", FieldFlag.COUNTER, "StepLabelContainer")
                    ]
                },
                new LegacyStepLabelContainer
                {
                    Left = 390, Top = 275, Width = 40, Height = 40, Number = 3, CounterStart = 16,
                    Fields =
                    [
                        CreateField("FILL_COLOR", Color.DarkRed, "StepLabelContainer"),
                        CreateField("LINE_COLOR", Color.White, "StepLabelContainer"),
                        CreateField("FLAGS", FieldFlag.COUNTER, "StepLabelContainer")
                    ]
                },
                new LegacyStepLabelContainer
                {
                    Left = 450, Top = 275, Width = 40, Height = 40, Number = 4, CounterStart = 16,
                    Fields =
                    [
                        CreateField("FILL_COLOR", Color.DarkRed, "StepLabelContainer"),
                        CreateField("LINE_COLOR", Color.White, "StepLabelContainer"),
                        CreateField("FLAGS", FieldFlag.COUNTER, "StepLabelContainer")
                    ]
                },
                new LegacyStepLabelContainer
                {
                    Left = 510, Top = 275, Width = 40, Height = 40, Number = 5, CounterStart = 16,
                    Fields =
                    [
                        CreateField("FILL_COLOR", Color.DarkRed, "StepLabelContainer"),
                        CreateField("LINE_COLOR", Color.White, "StepLabelContainer"),
                        CreateField("FLAGS", FieldFlag.COUNTER, "StepLabelContainer")
                    ]
                },
                new LegacyStepLabelContainer
                {
                    Left = 570, Top = 275, Width = 40, Height = 40, Number = 6, CounterStart = 16,
                    Fields =
                    [
                        CreateField("FILL_COLOR", Color.DarkRed, "StepLabelContainer"),
                        CreateField("LINE_COLOR", Color.White, "StepLabelContainer"),
                        CreateField("FLAGS", FieldFlag.COUNTER, "StepLabelContainer")
                    ]
                },
                new LegacySvgContainer
                {
                    Left = 640,
                    Top = 40,
                    Width = 120,
                    Height = 100,
                    SvgContent = svgContent,
                    Fields = [CreateField("SHADOW", false, "SvgContainer")]
                },
                new LegacyMetafileContainer
                {
                    Left = 640,
                    Top = 160,
                    Width = 120,
                    Height = 100,
                    MetafileContent = metafileContent,
                    Fields = [CreateField("SHADOW", false, "MetafileContainer")]
                },
                new LegacyEmojiContainer
                {
                    Left = 640,
                    Top = 320,
                    Width = 64,
                    Height = 64,
                    Emoji = "😀",
                    Fields = [CreateField("SHADOW", false, "EmojiContainer")]
                },
                new LegacyCursorContainer
                {
                    Left = 720,
                    Top = 320,
                    Width = 64,
                    Height = 64,
                    savedCursor = new LegacyCaptureCursorSerializationWrapper
                    {
                        ColorLayer = cursorColorLayer,
                        MaskLayer = cursorMaskLayer,
                        SizeWidth = 16,
                        SizeHeight = 16,
                        HotspotX = 3,
                        HotspotY = 4
                    },
                    Fields = [CreateField("SHADOW", false, "CursorContainer")]
                }
            };
            LegacyNrbfWriter.WriteEditorFile(output, screenshot, containers);
        }

        Console.WriteLine($"Manual all-supported-containers compatibility file created: {outputPath}");
        Assert.True(File.Exists(outputPath));

        using var stream = File.OpenRead(outputPath);
        using var image = Image.FromStream(stream, true, true);
        using var renderedBackground = new Bitmap(image);
        Assert.Equal(new Size(800, 400), image.Size);
        Assert.Equal(expectedBackground.GetPixel(0, 0), renderedBackground.GetPixel(0, 0));

        stream.Seek(-22, SeekOrigin.End);
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, true);
        var payloadLength = reader.ReadInt64();
        Assert.InRange(payloadLength, 1, stream.Length - 22);
        stream.Seek(-(payloadLength + 22), SeekOrigin.End);
        var readContainers = LegacyNrbfReader.ReadContainerList(stream);
        Assert.Equal(21, readContainers.Count);
        var rectangle = Assert.IsType<LegacyRectangleContainer>(readContainers[0]);
        Assert.Equal((20, 25, 130, 90), (rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height));
        Assert.Equal(4, rectangle.Fields[0].Value);
        Assert.Equal(Color.DarkOrange, rectangle.Fields[1].Value);

        var ellipse = Assert.IsType<LegacyEllipseContainer>(readContainers[1]);
        Assert.Equal((180, 25, 130, 90), (ellipse.Left, ellipse.Top, ellipse.Width, ellipse.Height));
        Assert.Equal(Color.DarkGreen, ellipse.Fields[1].Value);

        var line = Assert.IsType<LegacyLineContainer>(readContainers[2]);
        Assert.Equal((30, 160, 120, 70), (line.Left, line.Top, line.Width, line.Height));
        Assert.Equal(Color.DarkBlue, line.Fields[1].Value);

        var arrow = Assert.IsType<LegacyArrowContainer>(readContainers[3]);
        Assert.Equal((190, 160, 150, 70), (arrow.Left, arrow.Top, arrow.Width, arrow.Height));
        Assert.Equal(Color.Red, arrow.Fields[1].Value);
        Assert.Equal(ArrowContainer.ArrowHeadCombination.BOTH, arrow.Fields[2].Value);

        var freehand = Assert.IsType<LegacyFreehandContainer>(readContainers[4]);
        Assert.Equal((360, 35, 120, 80), (freehand.Left, freehand.Top, freehand.Width, freehand.Height));
        Assert.Equal(
            new[] { new Point(0, 0), new Point(35, 30), new Point(70, 5), new Point(110, 65) },
            freehand.CapturePoints);
        Assert.Equal(5, freehand.Fields[0].Value);
        Assert.Equal(Color.DarkViolet, freehand.Fields[1].Value);

        var speechbubble = Assert.IsType<LegacySpeechbubbleContainer>(readContainers[5]);
        Assert.Equal((420, 25, 190, 95), (speechbubble.Left, speechbubble.Top, speechbubble.Width, speechbubble.Height));
        Assert.Equal("Point on screen", speechbubble.Text);
        Assert.Equal(new Point(510, 155), speechbubble.StoredTargetGripperLocation);
        Assert.Equal(4, speechbubble.Fields[0].Value);
        Assert.Equal(Color.Blue, speechbubble.Fields[1].Value);

        var text = Assert.IsType<LegacyTextContainer>(readContainers[6]);
        Assert.Equal((380, 140, 230, 130), (text.Left, text.Top, text.Width, text.Height));
        Assert.Equal("Arial text", text.Text);
        Assert.Equal(5, text.Fields[0].Value);
        Assert.Equal(Color.Purple, text.Fields[1].Value);
        Assert.Equal("Arial", text.Fields[4].Value);
        Assert.Equal(18f, text.Fields[5].Value);

        var imageContainer = Assert.IsType<LegacyImageContainer>(readContainers[7]);
        Assert.Equal((25, 275, 120, 70), (imageContainer.Left, imageContainer.Top, imageContainer.Width, imageContainer.Height));
        Assert.NotNull(imageContainer.Image);
        Assert.Equal(sourceImage.Size, imageContainer.Image.Size);
        Assert.Single(imageContainer.Fields);

        var iconContainer = Assert.IsType<LegacyIconContainer>(readContainers[8]);
        Assert.Equal((190, 275, 64, 64), (iconContainer.Left, iconContainer.Top, iconContainer.Width, iconContainer.Height));
        Assert.NotNull(iconContainer.Icon);
        Assert.Equal(sourceIcon.Size, iconContainer.Icon.Size);

        var highlightContainer = Assert.IsType<LegacyHighlightContainer>(readContainers[9]);
        Assert.Equal((30, 130, 140, 90), (highlightContainer.Left, highlightContainer.Top, highlightContainer.Width, highlightContainer.Height));
        Assert.Equal(FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT, highlightContainer.Fields[3].Value);
        var highlightFilter = Assert.IsType<LegacyHighlightFilter>(Assert.Single(highlightContainer.Children));
        Assert.Equal(Color.Red, highlightFilter.Fields[0].Value);

        var obfuscateContainer = Assert.IsType<LegacyObfuscateContainer>(readContainers[10]);
        Assert.Equal((190, 130, 150, 90), (obfuscateContainer.Left, obfuscateContainer.Top, obfuscateContainer.Width, obfuscateContainer.Height));
        Assert.Equal(FilterContainer.PreparedFilter.BLUR, obfuscateContainer.Fields[3].Value);
        var blurFilter = Assert.IsType<LegacyBlurFilter>(Assert.Single(obfuscateContainer.Children));
        Assert.Equal(12, blurFilter.Fields[0].Value);
        Assert.Equal(1d, blurFilter.Fields[1].Value);

        var stepLabelPositions = new[] { 270, 330, 390, 450, 510, 570 };
        for (var index = 0; index < stepLabelPositions.Length; index++)
        {
            var stepLabel = Assert.IsType<LegacyStepLabelContainer>(readContainers[11 + index]);
            Assert.Equal((stepLabelPositions[index], 275, 40, 40), (stepLabel.Left, stepLabel.Top, stepLabel.Width, stepLabel.Height));
            Assert.Equal(index + 1, stepLabel.Number);
            Assert.Equal(16, stepLabel.CounterStart);
            Assert.Equal(Color.DarkRed, stepLabel.Fields[0].Value);
            Assert.Equal(Color.White, stepLabel.Fields[1].Value);
            Assert.Contains(stepLabel.Fields, field => field.FieldType.Name == "LINE_THICKNESS" && Equals(field.Value, 0));
            Assert.Contains(stepLabel.Fields, field => field.FieldType.Name == "SHADOW" && Equals(field.Value, false));
        }

        var svg = Assert.IsType<LegacySvgContainer>(readContainers[17]);
        Assert.Equal((640, 40, 120, 100), (svg.Left, svg.Top, svg.Width, svg.Height));
        Assert.Equal(svgBytes, svg.SvgContent.ToArray());

        var metafile = Assert.IsType<LegacyMetafileContainer>(readContainers[18]);
        Assert.Equal((640, 160, 120, 100), (metafile.Left, metafile.Top, metafile.Width, metafile.Height));
        Assert.NotNull(metafile.MetafileContent);

        var emoji = Assert.IsType<LegacyEmojiContainer>(readContainers[19]);
        Assert.Equal((640, 320, 64, 64), (emoji.Left, emoji.Top, emoji.Width, emoji.Height));
        Assert.Equal("😀", emoji.Emoji);

        var cursor = Assert.IsType<LegacyCursorContainer>(readContainers[20]);
        Assert.Equal((720, 320, 64, 64), (cursor.Left, cursor.Top, cursor.Width, cursor.Height));
        Assert.Equal(16, cursor.savedCursor.SizeWidth);
        Assert.Equal(16, cursor.savedCursor.SizeHeight);
        Assert.Equal((3, 4), (cursor.savedCursor.HotspotX, cursor.savedCursor.HotspotY));
        Assert.Equal(Color.White.ToArgb(), cursor.savedCursor.ColorLayer.GetPixel(1, 3).ToArgb());
        Assert.Equal(Color.Black.ToArgb(), cursor.savedCursor.ColorLayer.GetPixel(3, 5).ToArgb());
        Assert.Equal(0, cursor.savedCursor.ColorLayer.GetPixel(15, 15).A);
        Assert.Equal(0, cursor.savedCursor.MaskLayer.GetPixel(15, 15).A);
    }

    [Fact]
    public void WriteContainerList_RoundTripsImageAndIconContainers()
    {
        var testDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Images");
        using var image = Image.FromFile(Path.Combine(testDataDirectory, "Logo_G_with_Border.png"));
        using var icon = new Icon(Path.Combine(testDataDirectory, "Greenshot.ico"));
        var containers = new LegacyDrawableContainerList
        {
            new LegacyImageContainer
            {
                Left = 10,
                Top = 20,
                Width = 90,
                Height = 60,
                Image = image
            },
            new LegacyIconContainer
            {
                Left = 120,
                Top = 30,
                Width = 48,
                Height = 48,
                Icon = icon
            }
        };
        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);

        stream.Position = 0;
        var result = LegacyNrbfReader.ReadContainerList(stream);
        var imageContainer = Assert.IsType<LegacyImageContainer>(result[0]);
        Assert.Equal(image.Size, imageContainer.Image.Size);
        var iconContainer = Assert.IsType<LegacyIconContainer>(result[1]);
        Assert.Equal(icon.Size, iconContainer.Icon.Size);
        var expectedIconSizes = new[] { new Size(256, 256), new Size(48, 48), new Size(32, 32), new Size(24, 24), new Size(16, 16) };
        Assert.Equal(expectedIconSizes, GetIconSizes(icon));
        Assert.Equal(expectedIconSizes, GetIconSizes(iconContainer.Icon));
    }

    [Fact]
    public void WriteEditorFileWithSvgContainer_CreatesManualCompatibilityArtifact()
    {
        var testDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Images");
        var svgPath = Path.Combine(testDataDirectory, "Logo_G_with_Border.svg");
        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "V1WriterManualSvgContainer.greenshot");
        var svgBytes = File.ReadAllBytes(svgPath);
        using var svgContent = new MemoryStream(svgBytes, false);
        var containers = new LegacyDrawableContainerList
        {
            new LegacySvgContainer
            {
                Left = 300,
                Top = 200,
                Width = 120,
                Height = 100,
                RotationAngle = 180,
                SvgContent = svgContent,
                Fields =
                [
                    CreateField("SHADOW", false, "SvgContainer")
                ]
            }
        };
        using var screenshot = new Bitmap(800, 400);
        using (var graphics = Graphics.FromImage(screenshot))
        {
            graphics.Clear(Color.White);
        }

        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            LegacyNrbfWriter.WriteEditorFile(output, screenshot, containers);
        }

        Console.WriteLine($"Manual SVG compatibility file created: {outputPath}");
        Assert.True(File.Exists(outputPath));

        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);
        stream.Position = 0;
        var result = Assert.IsType<LegacySvgContainer>(Assert.Single(LegacyNrbfReader.ReadContainerList(stream)));
        Assert.Equal((300, 200, 120, 100), (result.Left, result.Top, result.Width, result.Height));
        Assert.Equal(180, result.RotationAngle);
        Assert.Equal(svgBytes, result.SvgContent.ToArray());
        Assert.Equal(false, Assert.Single(result.Fields).Value);
    }

    [Fact]
    public void WriteEditorFileWithMetafileContainer_CreatesManualCompatibilityArtifact()
    {
        var testDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Images");
        var metafilePath = Path.Combine(testDataDirectory, "Logo_G_with_Border.emf");
        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "V1WriterManualMetafileContainer.greenshot");
        var metafileBytes = File.ReadAllBytes(metafilePath);
        using var sourceMetafile = Image.FromFile(metafilePath);
        using var metafileContent = new MemoryStream(metafileBytes, false);
        var containers = new LegacyDrawableContainerList
        {
            new LegacyMetafileContainer
            {
                Left = 300,
                Top = 200,
                Width = 120,
                Height = 100,
                RotationAngle = 180,
                MetafileContent = metafileContent,
                Fields =
                [
                    CreateField("SHADOW", false, "MetafileContainer")
                ]
            }
        };
        using var screenshot = new Bitmap(800, 400);
        using (var graphics = Graphics.FromImage(screenshot))
        {
            graphics.Clear(Color.White);
        }

        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            LegacyNrbfWriter.WriteEditorFile(output, screenshot, containers);
        }

        Console.WriteLine($"Manual metafile compatibility file created: {outputPath}");
        Assert.True(File.Exists(outputPath));

        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);
        stream.Position = 0;
        var result = Assert.IsType<LegacyMetafileContainer>(Assert.Single(LegacyNrbfReader.ReadContainerList(stream)));
        Assert.Equal((300, 200, 120, 100), (result.Left, result.Top, result.Width, result.Height));
        Assert.Equal(180, result.RotationAngle);
        Assert.NotNull(result.MetafileContent);
        using var renderedMetafile = Image.FromStream(result.MetafileContent);
        Assert.Equal(sourceMetafile.Size, renderedMetafile.Size);
        Assert.Equal(false, Assert.Single(result.Fields).Value);
    }

    [Fact]
    public void WriteEditorFileWithEmojiContainer_CreatesManualCompatibilityArtifact()
    {
        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "V1WriterManualEmojiContainer.greenshot");
        const string emoji = "😀";
        var containers = new LegacyDrawableContainerList
        {
            new LegacyEmojiContainer
            {
                Left = 300,
                Top = 200,
                Width = 120,
                Height = 100,
                Emoji = emoji,
                Fields =
                [
                    CreateField("SHADOW", false, "EmojiContainer")
                ]
            }
        };
        using var screenshot = new Bitmap(800, 400);
        using (var graphics = Graphics.FromImage(screenshot))
        {
            graphics.Clear(Color.White);
        }

        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            LegacyNrbfWriter.WriteEditorFile(output, screenshot, containers);
        }

        Console.WriteLine($"Manual emoji compatibility file created: {outputPath}");
        Assert.True(File.Exists(outputPath));

        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);
        stream.Position = 0;
        var result = Assert.IsType<LegacyEmojiContainer>(Assert.Single(LegacyNrbfReader.ReadContainerList(stream)));
        Assert.Equal((300, 200, 120, 100), (result.Left, result.Top, result.Width, result.Height));
        Assert.Equal(0, result.RotationAngle);
        Assert.Equal(emoji, result.Emoji);
        Assert.Equal(false, Assert.Single(result.Fields).Value);
    }

    [Fact]
    public void WriteEditorFileWithCursorContainer_CreatesManualCompatibilityArtifact()
    {
        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "V1WriterManualCursorContainer.greenshot");
        using var colorLayer = new Bitmap(16, 16);
        using var maskLayer = new Bitmap(16, 16);
        using (var graphics = Graphics.FromImage(colorLayer))
        {
            graphics.Clear(Color.Transparent);
            graphics.FillPolygon(Brushes.White,
            [
                new Point(1, 1), new Point(1, 13), new Point(4, 10),
                new Point(7, 15), new Point(9, 14), new Point(6, 9), new Point(12, 9)
            ]);
            graphics.FillPolygon(Brushes.Black,
            [
                new Point(2, 3), new Point(2, 11), new Point(4, 8),
                new Point(7, 13), new Point(8, 13), new Point(5, 8), new Point(10, 8)
            ]);
        }
        using (var graphics = Graphics.FromImage(maskLayer))
        {
            graphics.Clear(Color.Transparent);
            graphics.FillPolygon(Brushes.White,
            [
                new Point(1, 1), new Point(1, 13), new Point(4, 10),
                new Point(7, 15), new Point(9, 14), new Point(6, 9), new Point(12, 9)
            ]);
        }

        var containers = new LegacyDrawableContainerList
        {
            new LegacyCursorContainer
            {
                Left = 600,
                Top = 100,
                Width = 64,
                Height = 64,
                savedCursor = new LegacyCaptureCursorSerializationWrapper
                {
                    ColorLayer = colorLayer,
                    MaskLayer = maskLayer,
                    SizeWidth = 16,
                    SizeHeight = 16,
                    HotspotX = 5,
                    HotspotY = 6
                },
                Fields =
                [
                    CreateField("SHADOW", false, "CursorContainer")
                ]
            }
        };
        using var screenshot = new Bitmap(800, 400);
        using (var graphics = Graphics.FromImage(screenshot))
        {
            graphics.Clear(Color.White);
        }

        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            LegacyNrbfWriter.WriteEditorFile(output, screenshot, containers);
        }

        Console.WriteLine($"Manual cursor compatibility file created: {outputPath}");
        Assert.True(File.Exists(outputPath));

        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);
        stream.Position = 0;
        var result = Assert.IsType<LegacyCursorContainer>(Assert.Single(LegacyNrbfReader.ReadContainerList(stream)));
        Assert.Equal((600, 100, 64, 64), (result.Left, result.Top, result.Width, result.Height));
        Assert.Equal(16, result.savedCursor.SizeWidth);
        Assert.Equal(16, result.savedCursor.SizeHeight);
        Assert.Equal((5, 6), (result.savedCursor.HotspotX, result.savedCursor.HotspotY));
        Assert.Equal(Color.White.ToArgb(), result.savedCursor.ColorLayer.GetPixel(1, 3).ToArgb());
        Assert.Equal(Color.Black.ToArgb(), result.savedCursor.ColorLayer.GetPixel(3, 5).ToArgb());
        Assert.Equal(0, result.savedCursor.ColorLayer.GetPixel(15, 15).A);
        Assert.Equal(0, result.savedCursor.MaskLayer.GetPixel(15, 15).A);
    }

    [Fact]
    public void WriteEditorFileWithEveryFilterType_CreatesDedicatedCompatibilityArtifact()
    {
        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
        Directory.CreateDirectory(outputDirectory);
        var mainFilterContainers = new LegacyDrawableContainerList
        {
            CreateFilterTestContainer(
                new LegacyHighlightContainer { Left = 22, Top = 50, Width = 260, Height = 164 },
                "PREPARED_FILTER_HIGHLIGHT", FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT, "HighlightContainer",
                new LegacyHighlightFilter { Fields = [CreateField("FILL_COLOR", Color.FromArgb(160, Color.Yellow), "HighlightFilter")] }),
            CreateFilterTestContainer(
                new LegacyHighlightContainer { Left = 22, Top = 260, Width = 260, Height = 164 },
                "PREPARED_FILTER_HIGHLIGHT", FilterContainer.PreparedFilter.MAGNIFICATION, "HighlightContainer",
                new LegacyMagnifierFilter { Fields = [CreateField("MAGNIFICATION_FACTOR", 3, "MagnifierFilter")] }),
            CreateFilterTestContainer(
                new LegacyObfuscateContainer { Left = 316, Top = 50, Width = 260, Height = 164 },
                "PREPARED_FILTER_OBFUSCATE", FilterContainer.PreparedFilter.BLUR, "ObfuscateContainer",
                new LegacyBlurFilter
                {
                    Fields =
                    [
                        CreateField("BLUR_RADIUS", 12, "BlurFilter"),
                        CreateField("PREVIEW_QUALITY", 1d, "BlurFilter")
                    ]
                }),
            CreateFilterTestContainer(
                new LegacyObfuscateContainer { Left = 316, Top = 260, Width = 260, Height = 164 },
                "PREPARED_FILTER_OBFUSCATE", FilterContainer.PreparedFilter.PIXELIZE, "ObfuscateContainer",
                new LegacyPixelizationFilter { Fields = [CreateField("PIXEL_SIZE", 16, "PixelizationFilter")] })
        };
        var brightnessContainer = new LegacyDrawableContainerList
        {
            CreateFilterTestContainer(
                new LegacyHighlightContainer { Left = 80, Top = 100, Width = 360, Height = 200 },
                "PREPARED_FILTER_HIGHLIGHT", FilterContainer.PreparedFilter.AREA_HIGHLIGHT, "HighlightContainer",
                new LegacyBrightnessFilter { Fields = [CreateField("BRIGHTNESS", 0.35d, "BrightnessFilter")] })
        };
        var grayscaleContainer = new LegacyDrawableContainerList
        {
            CreateFilterTestContainer(
                new LegacyHighlightContainer { Left = 80, Top = 100, Width = 360, Height = 200 },
                "PREPARED_FILTER_HIGHLIGHT", FilterContainer.PreparedFilter.GRAYSCALE, "HighlightContainer",
                new LegacyGrayscaleFilter())
        };

        WriteFilterTestArtifact(
            Path.Combine(outputDirectory, "V1WriterManualFilterTypes.greenshot"),
            mainFilterContainers,
            ["Highlight", "Magnifier", "Blur", "Pixelization"],
            new Size(620, 455));
        WriteFilterTestArtifact(
            Path.Combine(outputDirectory, "V1WriterManualBrightnessFilter.greenshot"),
            brightnessContainer,
            ["Brightness"],
            new Size(520, 360));
        WriteFilterTestArtifact(
            Path.Combine(outputDirectory, "V1WriterManualGrayscaleFilter.greenshot"),
            grayscaleContainer,
            ["Grayscale"],
            new Size(520, 360));
    }

    private static void WriteFilterTestArtifact(string outputPath, LegacyDrawableContainerList containers, string[] filterLabels, Size screenshotSize)
    {
        using var screenshot = new Bitmap(screenshotSize.Width, screenshotSize.Height);
        using (var graphics = Graphics.FromImage(screenshot))
        using (var labelFont = new Font("Arial", 14, FontStyle.Bold))
        using (var sampleFont = new Font("Arial", 12, FontStyle.Bold))
        using (var labelBrush = new SolidBrush(Color.Black))
        using (var sampleBrush = new SolidBrush(Color.White))
        {
            graphics.Clear(Color.White);
            for (var index = 0; index < containers.Count; index++)
            {
                var container = containers[index];
                var panel = new Rectangle(container.Left, container.Top, container.Width, container.Height);
                graphics.DrawString(filterLabels[index], labelFont, labelBrush, panel.Left, panel.Top - 26);
                DrawFilterTestPattern(graphics, panel, sampleFont, sampleBrush);
            }
        }

        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            LegacyNrbfWriter.WriteEditorFile(output, screenshot, containers);
        }

        Console.WriteLine($"Manual filter compatibility file created: {outputPath}");
        Assert.True(File.Exists(outputPath));

        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);
        stream.Position = 0;
        var roundTripped = LegacyNrbfReader.ReadContainerList(stream);
        Assert.Equal(containers.Count, roundTripped.Count);
        for (var index = 0; index < containers.Count; index++)
        {
            Assert.IsType(containers[index].Children[0].GetType(), Assert.Single(roundTripped[index].Children));
        }
    }

    [Fact]
    public void WriteEditorFileWithEllipseAndLine_CreatesManualCompatibilityArtifact()
    {
        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "V1WriterManualBasicShapes.greenshot");
        using var screenshot = new Bitmap(320, 200);
        using (var graphics = Graphics.FromImage(screenshot))
        {
            graphics.Clear(Color.White);
        }

        var containers = new LegacyDrawableContainerList
        {
            new LegacyEllipseContainer
            {
                Left = 32,
                Top = 30,
                Width = 112,
                Height = 78,
                Fields =
                [
                    CreateField("LINE_THICKNESS", 4),
                    CreateField("LINE_COLOR", Color.Blue),
                    CreateField("FILL_COLOR", Color.LightYellow),
                    CreateField("SHADOW", false)
                ]
            },
            new LegacyLineContainer
            {
                Left = 190,
                Top = 42,
                Width = 82,
                Height = 74,
                Fields =
                [
                    CreateField("LINE_THICKNESS", 6),
                    CreateField("LINE_COLOR", Color.DarkGreen),
                    CreateField("SHADOW", false)
                ]
            }
        };
        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            LegacyNrbfWriter.WriteEditorFile(output, screenshot, containers);
        }

        using var stream = File.OpenRead(outputPath);
        stream.Seek(-22, SeekOrigin.End);
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, true);
        var payloadLength = reader.ReadInt64();
        Assert.InRange(payloadLength, 1, stream.Length - 22);
        stream.Seek(-(payloadLength + 22), SeekOrigin.End);
        var readContainers = LegacyNrbfReader.ReadContainerList(stream);
        Assert.IsType<LegacyEllipseContainer>(readContainers[0]);
        Assert.IsType<LegacyLineContainer>(readContainers[1]);
        Console.WriteLine($"Manual basic-shapes compatibility file created: {outputPath}");
    }

    [Fact]
    public void WriteEmptyContainerList_ProducesSafeReadableV104PayloadAndLeavesStreamOpen()
    {
        using var stream = new MemoryStream();

        LegacyNrbfWriter.WriteEmptyContainerList(stream);

        Assert.True(stream.CanWrite);
        Assert.Equal(stream.Length, stream.Position);
        stream.Position = 0;
        var root = Assert.IsAssignableFrom<ClassRecord>(NrbfDecoder.Decode(stream, leaveOpen: true));
        Assert.Equal("Greenshot.Editor.Drawing.DrawableContainerList", root.TypeName.FullName);
        Assert.Contains(", Greenshot.Editor, Version=1.4.0.0,", root.TypeName.AssemblyQualifiedName);
        Assert.Equal(0, Convert.ToInt32(root.GetRawValue("List`1+_size")));
        var items = Assert.IsAssignableFrom<SerializationRecord>(root.GetRawValue("List`1+_items"));
        Assert.Equal("Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]", items.TypeName.FullName);

        stream.Position = 0;
        Assert.Empty(LegacyNrbfReader.ReadContainerList(stream));
    }

    [Fact]
    public void WriteEmptyContainerList_RejectsNullDestination()
    {
        Assert.Throws<ArgumentNullException>(() => LegacyNrbfWriter.WriteEmptyContainerList(null));
    }

    [Fact]
    public void WriteEmptyContainerList_RejectsNonWritableDestination()
    {
        using var stream = new MemoryStream(new byte[1], false);

        Assert.Throws<ArgumentException>(() => LegacyNrbfWriter.WriteEmptyContainerList(stream));
    }

    [Fact]
    public void WriteContainerList_RejectsUnsupportedContainer()
    {
        using var stream = new MemoryStream();
        var containers = new LegacyDrawableContainerList { new LegacyDrawableContainer() };

        Assert.Throws<NotSupportedException>(() => LegacyNrbfWriter.WriteContainerList(stream, containers));
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void WriteContainerList_RoundTripsRectangleCoordinates()
    {
        var containers = new LegacyDrawableContainerList
        {
            new LegacyRectangleContainer
            {
                Left = -20,
                Top = 42,
                Width = 200,
                Height = 90,
                Fields =
                [
                    CreateField("LINE_THICKNESS", 2),
                    CreateField("LINE_COLOR", Color.Red),
                    CreateField("FILL_COLOR", Color.Transparent),
                    CreateField("SHADOW", true)
                ]
            }
        };
        using var stream = new MemoryStream();

        LegacyNrbfWriter.WriteContainerList(stream, containers);

        stream.Position = 0;
        var result = Assert.Single(LegacyNrbfReader.ReadContainerList(stream));
        Assert.IsType<LegacyRectangleContainer>(result);
        Assert.Equal(-20, result.Left);
        Assert.Equal(42, result.Top);
        Assert.Equal(200, result.Width);
        Assert.Equal(90, result.Height);
        Assert.Equal(4, result.Fields.Count);
        Assert.Equal("LINE_THICKNESS", result.Fields[0].FieldType.Name);
        Assert.Equal(2, result.Fields[0].Value);
        Assert.Equal("LINE_COLOR", result.Fields[1].FieldType.Name);
        Assert.Equal(Color.Red, result.Fields[1].Value);
        Assert.Equal("FILL_COLOR", result.Fields[2].FieldType.Name);
        Assert.Equal(Color.Transparent, result.Fields[2].Value);
        Assert.Equal("SHADOW", result.Fields[3].FieldType.Name);
        Assert.Equal(true, result.Fields[3].Value);
    }

    [Fact]
    public void WriteContainerList_RoundTripsEllipseAndLineContainers()
    {
        var containers = new LegacyDrawableContainerList
        {
            new LegacyEllipseContainer
            {
                Left = 10,
                Top = 20,
                Width = 30,
                Height = 40,
                Fields =
                [
                    CreateField("LINE_THICKNESS", 3),
                    CreateField("LINE_COLOR", Color.Blue),
                    CreateField("FILL_COLOR", Color.Yellow),
                    CreateField("SHADOW", false)
                ]
            },
            new LegacyLineContainer
            {
                Left = 50,
                Top = 60,
                Width = 70,
                Height = 80,
                Fields =
                [
                    CreateField("LINE_THICKNESS", 5),
                    CreateField("LINE_COLOR", Color.Green),
                    CreateField("SHADOW", false)
                ]
            }
        };
        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);

        stream.Position = 0;
        var result = LegacyNrbfReader.ReadContainerList(stream);

        var ellipse = Assert.IsType<LegacyEllipseContainer>(result[0]);
        Assert.Equal((10, 20, 30, 40), (ellipse.Left, ellipse.Top, ellipse.Width, ellipse.Height));
        Assert.Equal(Color.Yellow, ellipse.Fields[2].Value);
        var line = Assert.IsType<LegacyLineContainer>(result[1]);
        Assert.Equal((50, 60, 70, 80), (line.Left, line.Top, line.Width, line.Height));
        Assert.Equal(Color.Green, line.Fields[1].Value);
    }

    [Fact]
    public void WriteContainerList_RoundTripsFreehandContainerCapturePoints()
    {
        var expectedPoints = new[] { new Point(2, 3), new Point(20, 35), new Point(45, 12), new Point(70, 60) };
        var containers = new LegacyDrawableContainerList
        {
            new LegacyFreehandContainer
            {
                Left = 15,
                Top = 25,
                Width = 80,
                Height = 70,
                CapturePoints = [.. expectedPoints],
                Fields =
                [
                    CreateField("LINE_THICKNESS", 7),
                    CreateField("LINE_COLOR", Color.Crimson)
                ]
            }
        };
        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);

        stream.Position = 0;
        var freehand = Assert.IsType<LegacyFreehandContainer>(Assert.Single(LegacyNrbfReader.ReadContainerList(stream)));
        Assert.Equal(expectedPoints, freehand.CapturePoints);
        Assert.Equal(7, freehand.Fields[0].Value);
        Assert.Equal(Color.Crimson, freehand.Fields[1].Value);
    }

    [Fact]
    public void WriteContainerList_RoundTripsSpeechbubbleContainer()
    {
        var containers = new LegacyDrawableContainerList
        {
            new LegacySpeechbubbleContainer
            {
                Left = 100,
                Top = 110,
                Width = 150,
                Height = 80,
                Text = "A note",
                StoredTargetGripperLocation = new Point(75, 220),
                Fields =
                [
                    CreateField("LINE_THICKNESS", 3, "SpeechbubbleContainer"),
                    CreateField("LINE_COLOR", Color.Blue, "SpeechbubbleContainer"),
                    CreateField("FILL_COLOR", Color.White, "SpeechbubbleContainer")
                ]
            }
        };
        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);

        stream.Position = 0;
        var speechbubble = Assert.IsType<LegacySpeechbubbleContainer>(Assert.Single(LegacyNrbfReader.ReadContainerList(stream)));
        Assert.Equal("A note", speechbubble.Text);
        Assert.Equal(new Point(75, 220), speechbubble.StoredTargetGripperLocation);
        Assert.Equal(Color.Blue, speechbubble.Fields[1].Value);
    }

    [Fact]
    public void WriteContainerList_RoundTripsStepLabelContainer()
    {
        var containers = new LegacyDrawableContainerList
        {
            new LegacyStepLabelContainer
            {
                Left = 42,
                Top = 53,
                Width = 30,
                Height = 30,
                Number = 6,
                CounterStart = 4,
                Fields =
                [
                    CreateField("FILL_COLOR", Color.DarkRed, "StepLabelContainer"),
                    CreateField("LINE_COLOR", Color.White, "StepLabelContainer"),
                    CreateField("FLAGS", FieldFlag.COUNTER, "StepLabelContainer")
                ]
            }
        };
        using var stream = new MemoryStream();

        LegacyNrbfWriter.WriteContainerList(stream, containers);

        stream.Position = 0;
        var stepLabel = Assert.IsType<LegacyStepLabelContainer>(Assert.Single(LegacyNrbfReader.ReadContainerList(stream)));
        Assert.Equal((42, 53, 30, 30), (stepLabel.Left, stepLabel.Top, stepLabel.Width, stepLabel.Height));
        Assert.Equal(6, stepLabel.Number);
        Assert.Equal(4, stepLabel.CounterStart);
        Assert.Equal(Color.DarkRed, stepLabel.Fields[0].Value);
        Assert.Equal(Color.White, stepLabel.Fields[1].Value);
        Assert.Equal(FieldFlag.COUNTER, stepLabel.Fields[2].Value);
        Assert.Contains(stepLabel.Fields, field => field.FieldType.Name == "LINE_THICKNESS" && Equals(field.Value, 0));
        Assert.Contains(stepLabel.Fields, field => field.FieldType.Name == "SHADOW" && Equals(field.Value, false));
    }

    [Fact]
    public void WriteContainerList_RoundTripsHighlightAndObfuscateContainersWithFilters()
    {
        var containers = new LegacyDrawableContainerList
        {
            new LegacyHighlightContainer
            {
                Left = 30,
                Top = 40,
                Width = 150,
                Height = 75,
                Fields =
                [
                    CreateField("PREPARED_FILTER_HIGHLIGHT", FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT, "HighlightContainer")
                ],
                Children =
                [
                    new LegacyHighlightFilter
                    {
                        Fields = [CreateField("FILL_COLOR", Color.Yellow, "HighlightFilter")]
                    }
                ]
            },
            new LegacyObfuscateContainer
            {
                Left = 210,
                Top = 40,
                Width = 150,
                Height = 75,
                Fields =
                [
                    CreateField("PREPARED_FILTER_OBFUSCATE", FilterContainer.PreparedFilter.BLUR, "ObfuscateContainer")
                ],
                Children =
                [
                    new LegacyBlurFilter
                    {
                        Fields =
                        [
                            CreateField("BLUR_RADIUS", 12, "BlurFilter"),
                            CreateField("PREVIEW_QUALITY", 1d, "BlurFilter")
                        ]
                    }
                ]
            }
        };
        using var stream = new MemoryStream();

        LegacyNrbfWriter.WriteContainerList(stream, containers);

        stream.Position = 0;
        var result = LegacyNrbfReader.ReadContainerList(stream);
        var highlight = Assert.IsType<LegacyHighlightContainer>(result[0]);
        Assert.Equal((30, 40, 150, 75), (highlight.Left, highlight.Top, highlight.Width, highlight.Height));
        Assert.Equal(FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT, highlight.Fields[0].Value);
        Assert.Equal(Color.Yellow, Assert.Single(Assert.IsType<LegacyHighlightFilter>(Assert.Single(highlight.Children)).Fields).Value);

        var obfuscate = Assert.IsType<LegacyObfuscateContainer>(result[1]);
        Assert.Equal((210, 40, 150, 75), (obfuscate.Left, obfuscate.Top, obfuscate.Width, obfuscate.Height));
        Assert.Equal(FilterContainer.PreparedFilter.BLUR, obfuscate.Fields[0].Value);
        var blur = Assert.IsType<LegacyBlurFilter>(Assert.Single(obfuscate.Children));
        Assert.Equal(12, blur.Fields[0].Value);
        Assert.Equal(1d, blur.Fields[1].Value);
    }

    [Fact]
    public void WriteContainerList_UsesTypedInterfaceArraysForNestedLists()
    {
        var containers = new LegacyDrawableContainerList
        {
            new LegacyRectangleContainer
            {
                Fields =
                [
                    CreateField("LINE_THICKNESS", 2),
                    CreateField("LINE_COLOR", Color.Red)
                ]
            }
        };
        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);
        stream.Position = 0;

        var root = Assert.IsAssignableFrom<ClassRecord>(NrbfDecoder.Decode(stream, leaveOpen: true));
        var containersArray = Assert.IsAssignableFrom<ArrayRecord>(root.GetRawValue("List`1+_items"));
        var container = Assert.IsAssignableFrom<ClassRecord>(containersArray.GetArray(typeof(Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]), true).GetValue(0));
        var children = Assert.IsAssignableFrom<ClassRecord>(container.GetRawValue("Children"));
        var childrenArray = Assert.IsAssignableFrom<ArrayRecord>(children.GetRawValue("_items"));
        var fields = Assert.IsAssignableFrom<ClassRecord>(container.GetRawValue("AbstractFieldHolder+fields"));
        var fieldsArray = Assert.IsAssignableFrom<ArrayRecord>(fields.GetRawValue("_items"));

        Assert.Equal("Greenshot.Base.Interfaces.Drawing.IFieldHolder[]", childrenArray.TypeName.FullName);
        Assert.Equal("Greenshot.Base.Interfaces.Drawing.IField[]", fieldsArray.TypeName.FullName);
    }

    [Fact]
    public void WriteContainerList_RoundTripsArrowAndTextContainers()
    {
        var containers = new LegacyDrawableContainerList
        {
            new LegacyArrowContainer
            {
                Left = 12,
                Top = 14,
                Width = 120,
                Height = 60,
                Fields =
                [
                    CreateField("LINE_THICKNESS", 3),
                    CreateField("LINE_COLOR", Color.DeepPink),
                    CreateField("ARROWHEADS", ArrowContainer.ArrowHeadCombination.BOTH),
                    CreateField("SHADOW", true)
                ]
            },
            new LegacyTextContainer
            {
                Left = 40,
                Top = 50,
                Width = 150,
                Height = 80,
                Text = "hello",
                Fields =
                [
                    CreateField("LINE_THICKNESS", 2),
                    CreateField("LINE_COLOR", Color.DarkSlateBlue),
                    CreateField("FONT_FAMILY", "Arial"),
                    CreateField("FONT_SIZE", 11.5f),
                    CreateField("TEXT_HORIZONTAL_ALIGNMENT", StringAlignment.Center),
                    CreateField("TEXT_VERTICAL_ALIGNMENT", StringAlignment.Center)
                ]
            }
        };

        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);

        stream.Position = 0;
        var result = LegacyNrbfReader.ReadContainerList(stream);

        var arrow = Assert.IsType<LegacyArrowContainer>(result[0]);
        Assert.Equal(12, arrow.Left);
        Assert.Equal(14, arrow.Top);
        Assert.Equal(120, arrow.Width);
        Assert.Equal(60, arrow.Height);
        Assert.Equal(ArrowContainer.ArrowHeadCombination.BOTH, arrow.Fields[2].Value);

        var text = Assert.IsType<LegacyTextContainer>(result[1]);
        Assert.Equal(40, text.Left);
        Assert.Equal(50, text.Top);
        Assert.Equal(150, text.Width);
        Assert.Equal(80, text.Height);
        Assert.Equal("hello", text.Text);
        Assert.Equal("Arial", text.Fields[2].Value);
        Assert.Equal(StringAlignment.Center, text.Fields[4].Value);
    }

    [Fact]
    public void WriteContainerList_RoundTripsSupportedEnumFieldValues()
    {
        var containers = new LegacyDrawableContainerList
        {
            new LegacyRectangleContainer
            {
                Fields =
                [
                    CreateField("FLAGS", FieldFlag.CONFIRMABLE),
                    CreateField("PREPARED_FILTER", FilterContainer.PreparedFilter.GRAYSCALE),
                    CreateField("ARROW_HEAD", ArrowContainer.ArrowHeadCombination.BOTH),
                    CreateField("SCALE", 1.25f),
                    CreateField("OPACITY", 0.625d)
                ]
            }
        };
        using var stream = new MemoryStream();
        LegacyNrbfWriter.WriteContainerList(stream, containers);

        stream.Position = 0;
        var rectangle = Assert.IsType<LegacyRectangleContainer>(Assert.Single(LegacyNrbfReader.ReadContainerList(stream)));

        Assert.Equal(FieldFlag.CONFIRMABLE, rectangle.Fields[0].Value);
        Assert.Equal(FilterContainer.PreparedFilter.GRAYSCALE, rectangle.Fields[1].Value);
        Assert.Equal(ArrowContainer.ArrowHeadCombination.BOTH, rectangle.Fields[2].Value);
        Assert.Equal(1.25f, rectangle.Fields[3].Value);
        Assert.Equal(0.625d, rectangle.Fields[4].Value);
    }

    private static LegacyDrawableContainer CreateFilterTestContainer(
        LegacyDrawableContainer container,
        string presetFieldName,
        FilterContainer.PreparedFilter preset,
        string scope,
        LegacyFieldHolder filter)
    {
        container.Fields =
        [
            CreateField("LINE_THICKNESS", 2, scope),
            CreateField("LINE_COLOR", Color.Lime, scope),
            CreateField("SHADOW", false, scope),
            CreateField(presetFieldName, preset, scope)
        ];
        container.Children = [filter];
        return container;
    }

    private static void DrawFilterTestPattern(Graphics graphics, Rectangle bounds, Font sampleFont, Brush sampleBrush)
    {
        var colors = new[]
        {
            Color.DarkRed, Color.Orange, Color.Gold, Color.LimeGreen,
            Color.DeepSkyBlue, Color.RoyalBlue, Color.MediumOrchid, Color.HotPink
        };
        var tileWidth = (float)bounds.Width / colors.Length;
        var tileHeight = (float)bounds.Height / 5;
        for (var row = 0; row < 5; row++)
        {
            for (var column = 0; column < colors.Length; column++)
            {
                using var brush = new SolidBrush(colors[(column + row * 3) % colors.Length]);
                graphics.FillRectangle(brush, bounds.Left + column * tileWidth, bounds.Top + row * tileHeight, tileWidth + 1, tileHeight + 1);
            }
        }

        using var shadowBrush = new SolidBrush(Color.Black);
        graphics.DrawString("Aa 0123 Greenshot", sampleFont, shadowBrush, bounds.Left + 13, bounds.Top + 63);
        graphics.DrawString("Aa 0123 Greenshot", sampleFont, sampleBrush, bounds.Left + 11, bounds.Top + 61);
        using var pen = new Pen(Color.White, 3);
        graphics.DrawLine(pen, bounds.Left + 8, bounds.Top + 146, bounds.Left + 235, bounds.Top + 14);
    }

    private static LegacyField CreateField(string name, object value, string scope = "RectangleContainer")
    {
        var field = new LegacyField
        {
            FieldType = new LegacyFieldType { Name = name },
            Scope = scope
        };
        field.ResetValue(value);
        return field;
    }

    private static Size[] GetIconSizes(Icon icon)
    {
        using var stream = new MemoryStream();
        icon.Save(stream);
        var data = stream.ToArray();
        var count = BitConverter.ToUInt16(data, 4);
        var sizes = new Size[count];
        for (var index = 0; index < count; index++)
        {
            var offset = 6 + index * 16;
            var width = data[offset] == 0 ? 256 : data[offset];
            var height = data[offset + 1] == 0 ? 256 : data[offset + 1];
            sizes[index] = new Size(width, height);
        }

        return sizes;
    }
}
