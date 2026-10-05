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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.FileFormat.Legacy;
using Xunit;

namespace Greenshot.Tests.FileFormat
{
    /// <summary>
    /// Tests that the elements of old files, read without BinaryFormatter and mapped to the editor containers, are the same
    /// as the containers BinaryFormatter created from the same files before. And that what the editor saves today can be read back.
    /// </summary>
    public class LegacyElementMapperTests
    {
        private static readonly string TestDataDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "LegacyFormat");

        public LegacyElementMapperTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        public static IEnumerable<object[]> GreenshotFiles() => Directory.GetFiles(Path.Combine(TestDataDirectory, "Greenshotfile"), "*.greenshot", SearchOption.AllDirectories)
            .Select(file => new object[] { file.Substring(TestDataDirectory.Length + 1) });

        public static IEnumerable<object[]> TemplateFiles() => Directory.GetFiles(Path.Combine(TestDataDirectory, "GreenshotTemplate"), "*.gst", SearchOption.AllDirectories)
            .Select(file => new object[] { file.Substring(TestDataDirectory.Length + 1) });

        private static Surface CreateSurface() => new Surface(new Bitmap(800, 400));

        /// <summary>
        /// The old way: BinaryFormatter with the allow list, as the clipboard still does it
        /// </summary>
        private static IDrawableContainerList ReadWithBinaryFormatter(byte[] payload, Surface surface)
        {
            var elements = DrawableContainerClipboard.Deserialize(payload);
            Assert.NotNull(elements);
            elements.Parent = surface;
            return elements;
        }

        private static byte[] ReadPayload(string relativePath)
        {
            var bytes = File.ReadAllBytes(Path.Combine(TestDataDirectory, relativePath));
            if (!relativePath.EndsWith(".greenshot"))
            {
                return bytes;
            }

            using var stream = new MemoryStream(bytes);
            Assert.True(LegacyGreenshotFile.TryLocate(stream, out var file));
            var payload = new byte[file.ElementsLength];
            Array.Copy(bytes, file.ElementsOffset, payload, 0, payload.Length);
            return payload;
        }

        [Theory]
        [MemberData(nameof(GreenshotFiles))]
        [MemberData(nameof(TemplateFiles))]
        public void LoadElements_OldFiles_SameAsBinaryFormatter(string relativePath)
        {
            var payload = ReadPayload(relativePath);
            using var expectedSurface = CreateSurface();
            var expected = ReadWithBinaryFormatter(payload, expectedSurface);
            using var surface = CreateSurface();

            surface.LoadElementsFromStream(new MemoryStream(payload));

            AssertSameElements(expected, surface.Elements);
            Assert.Equal(expectedSurface.CounterStart, surface.CounterStart);
        }

        [Fact]
        public void LoadSurface_GreenshotFile_ImageAndElements()
        {
            var handler = new Greenshot.Editor.FileFormatHandlers.GreenshotFileFormatHandler();
            using var stream = File.OpenRead(Path.Combine(TestDataDirectory, "Greenshotfile", "File_Version_1.03", "Surface_with_14_different_DrawableContainer.greenshot"));

            using var surface = (Surface)handler.LoadSurface(stream);

            Assert.NotNull(surface.Image);
            Assert.Equal(14, surface.Elements.Count);
        }

        /// <summary>
        /// The editor still saves with BinaryFormatter, what it saves today must be readable by the frozen reader.
        /// When this fails, a change to the editor classes changed the file format: the reader (or the mapper) needs to know it.
        /// </summary>
        [Fact]
        public void SaveAndLoad_AllElementTypes_RoundTrip()
        {
            using var surface = CreateSurface();
            var elements = new List<DrawableContainer>
            {
                new RectangleContainer(surface) { Left = 10, Top = 20, Width = 30, Height = 40 },
                new EllipseContainer(surface) { Left = 50, Top = 60, Width = 70, Height = 80 },
                new LineContainer(surface) { Left = 1, Top = 2, Width = 300, Height = 0 },
                new ArrowContainer(surface) { Left = 5, Top = 6, Width = 100, Height = 50 },
                new TextContainer(surface) { Left = 100, Top = 100, Width = 200, Height = 50, Text = "Text" },
                new HighlightContainer(surface) { Left = 300, Top = 10, Width = 50, Height = 50 },
                new ObfuscateContainer(surface) { Left = 300, Top = 100, Width = 50, Height = 50 },
                new StepLabelContainer(surface) { Left = 400, Top = 10, Width = 30, Height = 30 },
                new StepLabelContainer(surface) { Left = 450, Top = 10, Width = 30, Height = 30 },
                new ImageContainer(surface) { Image = new Bitmap(20, 10), Left = 500, Top = 300, Width = 20, Height = 10 },
                new EmojiContainer(surface, "\U0001F49A") { Left = 600, Top = 300 }
            };
            var speechbubble = new SpeechbubbleContainer(surface) { Left = 200, Top = 200, Width = 150, Height = 80, Text = "Bubble" };
            speechbubble.RestoreTargetGripper(new Dapplo.Windows.Common.Structs.NativePoint(100, 300));
            elements.Add(speechbubble);
            var freehand = new FreehandContainer(surface);
            freehand.RestoreCapturePoints(new[] { new Point(10, 10), new Point(20, 15), new Point(30, 30) });
            elements.Add(freehand);

            elements[0].SetFieldValue(FieldType.LINE_COLOR, Color.FromArgb(255, 1, 2, 3));
            elements[0].SetFieldValue(FieldType.FILL_COLOR, Color.Transparent);
            elements[4].SetFieldValue(FieldType.FONT_SIZE, 23f);
            elements[5].SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, FilterContainer.PreparedFilter.MAGNIFICATION);
            elements[6].SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, FilterContainer.PreparedFilter.PIXELIZE);
            var elementList = new DrawableContainerList(surface.ID);
            elementList.AddRange(elements);
            surface.AddElements(elementList, false);
            surface.CounterStart = 5;

            using var stream = new MemoryStream();
            surface.SaveElementsToStream(stream);
            stream.Position = 0;
            using var loadedSurface = CreateSurface();
            loadedSurface.LoadElementsFromStream(stream);

            AssertSameElements(surface.Elements, loadedSurface.Elements);
            Assert.Equal(5, loadedSurface.CounterStart);
        }

        private static void AssertSameElements(IEnumerable<IDrawableContainer> expected, IEnumerable<IDrawableContainer> actual)
        {
            var expectedList = expected.ToList();
            var actualList = actual.ToList();
            Assert.Equal(expectedList.Select(e => e.GetType()), actualList.Select(e => e.GetType()));
            for (int i = 0; i < expectedList.Count; i++)
            {
                AssertSameElement(expectedList[i], actualList[i]);
            }
        }

        private static void AssertSameElement(IDrawableContainer expected, IDrawableContainer actual)
        {
            string name = expected.GetType().Name;
            Assert.True(expected.Bounds == actual.Bounds, $"{name}: bounds {expected.Bounds} != {actual.Bounds}");
            AssertSameFields(name, (IFieldHolder)expected, (IFieldHolder)actual);

            var expectedFilters = ((DrawableContainer)expected).Filters.ToList();
            var actualFilters = ((DrawableContainer)actual).Filters.ToList();
            Assert.Equal(expectedFilters.Select(f => f.GetType()), actualFilters.Select(f => f.GetType()));
            for (int i = 0; i < expectedFilters.Count; i++)
            {
                Assert.Equal(expectedFilters[i].Invert, actualFilters[i].Invert);
                AssertSameFields($"{name} {expectedFilters[i].GetType().Name}", expectedFilters[i], actualFilters[i]);
            }

            switch (expected)
            {
                case SpeechbubbleContainer expectedSpeechbubble:
                    var actualSpeechbubble = (SpeechbubbleContainer)actual;
                    Assert.Equal(expectedSpeechbubble.Text, actualSpeechbubble.Text);
                    Assert.Equal(expectedSpeechbubble.TargetAdorner?.Location, actualSpeechbubble.TargetAdorner?.Location);
                    break;
                case TextContainer expectedText:
                    Assert.Equal(expectedText.Text, ((TextContainer)actual).Text);
                    break;
                case StepLabelContainer expectedStepLabel:
                    Assert.Equal(expectedStepLabel.Number, ((StepLabelContainer)actual).Number);
                    break;
                case ImageContainer expectedImage:
                    Assert.Equal(expectedImage.Image.Size, ((ImageContainer)actual).Image.Size);
                    break;
                case IconContainer expectedIcon:
                    Assert.Equal(expectedIcon.Icon.Size, ((IconContainer)actual).Icon.Size);
                    break;
                case EmojiContainer expectedEmoji:
                    Assert.Equal(expectedEmoji.Emoji, ((EmojiContainer)actual).Emoji);
                    break;
                case CursorContainer expectedCursor:
                    Assert.Equal(expectedCursor.Cursor.Size, ((CursorContainer)actual).Cursor.Size);
                    Assert.Equal(expectedCursor.Cursor.HotSpot, ((CursorContainer)actual).Cursor.HotSpot);
                    break;
            }

            // Includes the shape of a freehand line and the shadow
            Assert.True(expected.DrawingBounds == actual.DrawingBounds, $"{name}: drawing bounds {expected.DrawingBounds} != {actual.DrawingBounds}");
        }

        /// <summary>
        /// Every field of the expected element must have the same value in the actual one
        /// </summary>
        private static void AssertSameFields(string name, IFieldHolder expected, IFieldHolder actual)
        {
            foreach (var expectedField in expected.GetFields())
            {
                Assert.True(actual.HasField(expectedField.FieldType), $"{name}: field {expectedField.FieldType} is missing");
                object expectedValue = expectedField.Value;
                object actualValue = actual.GetField(expectedField.FieldType).Value;
                if (expectedValue is Color expectedColor && actualValue is Color actualColor)
                {
                    Assert.True(expectedColor.ToArgb() == actualColor.ToArgb(), $"{name}: {expectedField.FieldType} {expectedColor} != {actualColor}");
                    continue;
                }

                Assert.True(Equals(expectedValue, actualValue), $"{name}: {expectedField.FieldType} {expectedValue} ({expectedValue?.GetType().Name}) != {actualValue} ({actualValue?.GetType().Name})");
            }
        }
    }
}
