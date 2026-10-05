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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Greenshot.FileFormat.Legacy;
using Xunit;
#if NETFRAMEWORK
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace Greenshot.Tests.FileFormat
{
    /// <summary>
    /// Tests for the BinaryFormatter-free reader of the .greenshot / .gst files written by Greenshot 1.2, 1.3 and 1.4.
    /// The test files were created by Christian Schulz for PR #638, one file per element type and version.
    /// </summary>
    public class LegacyElementReaderTests
    {
        private static readonly string TestDataDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "LegacyFormat");

        public static IEnumerable<object[]> GreenshotFiles() => Directory.GetFiles(Path.Combine(TestDataDirectory, "Greenshotfile"), "*.greenshot", SearchOption.AllDirectories)
            .Select(file => new object[] { RelativePath(file) });

        public static IEnumerable<object[]> TemplateFiles() => Directory.GetFiles(Path.Combine(TestDataDirectory, "GreenshotTemplate"), "*.gst", SearchOption.AllDirectories)
            .Select(file => new object[] { RelativePath(file) });

        private static string RelativePath(string file) => file.Substring(TestDataDirectory.Length + 1);

        private static IReadOnlyList<LegacyElement> ReadGreenshotFile(string relativePath, out LegacyGreenshotFile file)
        {
            using var stream = File.OpenRead(Path.Combine(TestDataDirectory, relativePath));
            Assert.True(LegacyGreenshotFile.TryLocate(stream, out file), $"{relativePath} has no .greenshot marker");
            return file.ReadElements(stream);
        }

        private static IReadOnlyList<LegacyElement> ReadTemplate(string relativePath)
        {
            using var stream = File.OpenRead(Path.Combine(TestDataDirectory, relativePath));
            return LegacyElementReader.Read(stream);
        }

        /// <summary>
        /// The file names say what is in the file, e.g. "RectangleContainer_lt_100_200_wh_150_80": one rectangle at 100,200 with size 150x80
        /// </summary>
        private static void AssertMatchesFileName(string relativePath, IReadOnlyList<LegacyElement> elements)
        {
            string name = Path.GetFileNameWithoutExtension(relativePath);
            Assert.All(elements, element => Assert.NotEqual(LegacyElementKind.Unknown, element.Kind));
            if (name.StartsWith("Surface_with_Image"))
            {
                Assert.Empty(elements);
                return;
            }

            var countMatch = Regex.Match(name, @"^Surface_with_(\d+)_different");
            if (countMatch.Success)
            {
                Assert.Equal(int.Parse(countMatch.Groups[1].Value), elements.Count);
                return;
            }

            string containerType = name.Substring(0, name.IndexOf('_'));
            Assert.All(elements, element => Assert.Equal(containerType, element.TypeName.Substring(element.TypeName.LastIndexOf('.') + 1)));

            var boundsMatch = Regex.Match(name, @"_lt_(\d+)_(\d+)_wh_(\d+)_(\d+)$");
            if (boundsMatch.Success)
            {
                var element = Assert.Single(elements);
                Assert.Equal(int.Parse(boundsMatch.Groups[1].Value), element.Left);
                Assert.Equal(int.Parse(boundsMatch.Groups[2].Value), element.Top);
                Assert.Equal(int.Parse(boundsMatch.Groups[3].Value), element.Width);
                Assert.Equal(int.Parse(boundsMatch.Groups[4].Value), element.Height);
            }
        }

        [Theory]
        [MemberData(nameof(GreenshotFiles))]
        public void ReadGreenshotFile_AllVersions_ElementsMatchFileName(string relativePath)
        {
            var elements = ReadGreenshotFile(relativePath, out var file);

            string versionDirectory = Path.GetFileName(Path.GetDirectoryName(relativePath));
            Assert.Equal("Greenshot" + versionDirectory.Substring("File_Version_".Length).Replace("1.", "01."), file.Marker);
            AssertMatchesFileName(relativePath, elements);
        }

        [Theory]
        [MemberData(nameof(TemplateFiles))]
        public void ReadTemplate_AllVersions_ElementsMatchFileName(string relativePath)
        {
            var elements = ReadTemplate(relativePath);

            AssertMatchesFileName(relativePath, elements);
        }

        [Theory]
        [InlineData("File_Version_1.02")]
        [InlineData("File_Version_1.03")]
        public void Read_Rectangle_FieldsWithKnownColors(string version)
        {
            var element = Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", version, "RectangleContainer_lt_100_200_wh_150_80.greenshot"), out _));

            Assert.Equal(LegacyElementKind.Rectangle, element.Kind);
            var lineColor = Assert.IsType<LegacyColor>(element.Fields.Single(f => f.FieldTypeName == "LINE_COLOR").Value);
            Assert.True(lineColor.IsKnownColor);
            Assert.Equal(141, lineColor.KnownColor); // KnownColor.Red
            var fillColor = Assert.IsType<LegacyColor>(element.Fields.Single(f => f.FieldTypeName == "FILL_COLOR").Value);
            Assert.Equal(27, fillColor.KnownColor); // KnownColor.Transparent
            Assert.IsType<int>(element.Fields.Single(f => f.FieldTypeName == "LINE_THICKNESS").Value);
            Assert.IsType<bool>(element.Fields.Single(f => f.FieldTypeName == "SHADOW").Value);
            Assert.All(element.Fields, f => Assert.Equal("RectangleContainer", f.Scope));
        }

        [Theory]
        [InlineData("File_Version_1.02")]
        [InlineData("File_Version_1.03")]
        public void Read_Text_TextAndFontFields(string version)
        {
            var element = Assert.IsType<LegacyTextElement>(Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", version, "TextContainer_lt_300_200_wh_300_100.greenshot"), out _)));

            Assert.Equal("Hello Greenshot", element.Text);
            Assert.Equal("Arial", element.Fields.Single(f => f.FieldTypeName == "FONT_FAMILY").Value);
            Assert.IsType<float>(element.Fields.Single(f => f.FieldTypeName == "FONT_SIZE").Value);
            var alignment = Assert.IsType<LegacyEnumValue>(element.Fields.Single(f => f.FieldTypeName == "TEXT_HORIZONTAL_ALIGNMENT").Value);
            Assert.Equal("StringAlignment", alignment.TypeName);
            var fillColor = Assert.IsType<LegacyColor>(element.Fields.Single(f => f.FieldTypeName == "FILL_COLOR").Value);
            Assert.True(fillColor.IsArgb);
            Assert.False(fillColor.IsKnownColor);
        }

        [Theory]
        [InlineData("File_Version_1.02")]
        [InlineData("File_Version_1.03")]
        public void Read_Speechbubble_TextAndTarget(string version)
        {
            var element = Assert.IsType<LegacySpeechbubbleElement>(Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", version, "SpeechbubbleContainer_lt_200_200_wh_150_80.greenshot"), out _)));

            Assert.Equal("Point on 100x300", element.Text);
            Assert.Equal(100, element.TargetGripperLocation.X);
            Assert.Equal(300, element.TargetGripperLocation.Y);
        }

        [Theory]
        [InlineData("File_Version_1.02")]
        [InlineData("File_Version_1.03")]
        [InlineData("File_Version_1.04")]
        public void Read_StepLabels_NumbersAndCounterStart(string version)
        {
            var elements = ReadGreenshotFile(Path.Combine("Greenshotfile", version, "StepLabelContainer_lt_200_200_lt_500_300.greenshot"), out _);

            var labels = elements.Cast<LegacyStepLabelElement>().ToList();
            Assert.Equal(2, labels.Count);
            Assert.Equal(new[] { 1, 2 }, labels.Select(l => l.Number).OrderBy(n => n));
            Assert.All(labels, l => Assert.Equal(1, l.CounterStart));
        }

        [Theory]
        [InlineData("File_Version_1.02")]
        [InlineData("File_Version_1.03")]
        public void Read_Freehand_Points(string version)
        {
            var element = Assert.IsType<LegacyFreehandElement>(Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", version, "FreehandContainer_with_4_points.greenshot"), out _)));

            Assert.Equal(4, element.Points.Count);
            Assert.Equal(240, element.Points[0].X);
            Assert.Equal(171, element.Points[0].Y);
        }

        [Theory]
        [InlineData("File_Version_1.02")]
        [InlineData("File_Version_1.03")]
        public void Read_Highlight_PreparedFilterAndFilterChild(string version)
        {
            var element = Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", version, "HighlightContainer_TextFilter_lt_310_70_wh_195_60.greenshot"), out _));

            Assert.Equal(LegacyElementKind.Highlight, element.Kind);
            var preparedFilter = Assert.IsType<LegacyEnumValue>(element.Fields.Single(f => f.FieldTypeName == "PREPARED_FILTER_HIGHLIGHT").Value);
            Assert.Equal("FilterContainer+PreparedFilter", preparedFilter.TypeName);
            var filter = Assert.Single(element.Filters);
            Assert.Equal("HighlightFilter", filter.Name);
            Assert.IsType<LegacyColor>(filter.Fields.Single(f => f.FieldTypeName == "FILL_COLOR").Value);
        }

        [Theory]
        [InlineData("File_Version_1.02")]
        [InlineData("File_Version_1.03")]
        public void Read_Obfuscate_BlurFilter(string version)
        {
            var element = Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", version, "ObfuscateContainer_BlurFilter_lt_130_70_wh_180_70.greenshot"), out _));

            Assert.Equal(LegacyElementKind.Obfuscate, element.Kind);
            Assert.Equal("BlurFilter", Assert.Single(element.Filters).Name);
        }

        [Theory]
        [InlineData("File_Version_1.02")]
        [InlineData("File_Version_1.03")]
        public void Read_Image_PngData(string version)
        {
            var element = Assert.IsType<LegacyImageElement>(Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", version, "ImageContainer_lt_300_200_wh_100_100.greenshot"), out _)));

            AssertPng(element.ImageData);
        }

        [Theory]
        [InlineData("File_Version_1.02")]
        [InlineData("File_Version_1.03")]
        public void Read_Icon_IcoData(string version)
        {
            var element = Assert.IsType<LegacyIconElement>(Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", version, "IconContainer_lt_400_200_wh_32_32.greenshot"), out _)));

            // .ico header: reserved 0, type 1
            Assert.Equal(new byte[] { 0, 0, 1, 0 }, element.IconData.Take(4).ToArray());
        }

        [Fact]
        public void Read_Metafile_RenderedPng()
        {
            var element = Assert.IsType<LegacyMetafileElement>(Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", "File_Version_1.03", "MetafileContainer_lt_300_200_wh_120_100.greenshot"), out _)));

            AssertPng(element.ImageData);
            Assert.Equal(0, element.RotationAngle);
        }

        [Fact]
        public void Read_Svg_Document()
        {
            var element = Assert.IsType<LegacySvgElement>(Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", "File_Version_1.03", "SvgContainer_lt_300_200_wh_120_100.greenshot"), out _)));

            string svg = System.Text.Encoding.UTF8.GetString(element.SvgData);
            Assert.Contains("<svg", svg);
            Assert.EndsWith(">", svg.TrimEnd());
        }

        [Fact]
        public void Read_Emoji()
        {
            var element = Assert.IsType<LegacyEmojiElement>(Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", "File_Version_1.04", "EmojiContainer_lt_100_200_wh_64_64.greenshot"), out _)));

            Assert.Equal("\U0001F49A", element.Emoji);
        }

        [Fact]
        public void Read_Cursor_Layers()
        {
            var element = Assert.IsType<LegacyCursorElement>(Assert.Single(ReadGreenshotFile(Path.Combine("Greenshotfile", "File_Version_1.04", "CursorContainer_lt_600_100_wh_64_64.greenshot"), out _)));

            AssertPng(element.ColorLayerData);
            AssertPng(element.MaskLayerData);
            Assert.True(element.CursorWidth > 0);
            Assert.True(element.CursorHeight > 0);
        }

        [Fact]
        public void TryLocate_NotAGreenshotFile_ReturnsFalse()
        {
            using var stream = new MemoryStream(new byte[100]);

            Assert.False(LegacyGreenshotFile.TryLocate(stream, out _));
        }

        [Fact]
        public void TryLocate_LengthLargerThanFile_ReturnsFalse()
        {
            using var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(new byte[10]);
            writer.Write(long.MaxValue);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("Greenshot01.03"));
            writer.Flush();

            Assert.False(LegacyGreenshotFile.TryLocate(stream, out _));
        }

        [Fact]
        public void Read_Garbage_ThrowsLegacyFormatException()
        {
            using var stream = new MemoryStream(Enumerable.Range(0, 200).Select(i => (byte)(i * 7)).ToArray());

            Assert.Throws<LegacyFormatException>(() => LegacyElementReader.Read(stream));
        }

        [Fact]
        public void Read_TruncatedFile_ThrowsLegacyFormatException()
        {
            var bytes = File.ReadAllBytes(Path.Combine(TestDataDirectory, "GreenshotTemplate", "File_Version_1.03", "TextContainer_lt_300_200_wh_300_100.gst"));
            using var stream = new MemoryStream(bytes, 0, bytes.Length / 2);

            Assert.Throws<LegacyFormatException>(() => LegacyElementReader.Read(stream));
        }

#if NETFRAMEWORK
        [Serializable]
        private sealed class NotAGreenshotType : ISerializable
        {
            public static bool Instantiated;

            public NotAGreenshotType()
            {
            }

            private NotAGreenshotType(SerializationInfo info, StreamingContext context)
            {
                Instantiated = true;
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("payload", "calc.exe");
            }
        }

        /// <summary>
        /// A crafted file with a foreign type must not instantiate that type, it is only reported as unknown element
        /// </summary>
        [Fact]
        public void Read_ForeignType_IsNotInstantiated()
        {
            using var stream = new MemoryStream();
#pragma warning disable SYSLIB0011 // BinaryFormatter is only used to create the test payload
            new BinaryFormatter().Serialize(stream, new List<object> { new NotAGreenshotType() });
#pragma warning restore SYSLIB0011
            stream.Position = 0;
            NotAGreenshotType.Instantiated = false;

            var element = Assert.Single(LegacyElementReader.Read(stream));

            Assert.Equal(LegacyElementKind.Unknown, element.Kind);
            Assert.False(NotAGreenshotType.Instantiated);
        }
#endif

        private static void AssertPng(byte[] data)
        {
            Assert.NotNull(data);
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, data.Take(4).ToArray());
        }
    }
}
