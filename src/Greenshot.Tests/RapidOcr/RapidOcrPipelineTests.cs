/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Linq;
using System.Text;
using Greenshot.Plugin.RapidOcr.Recognition;
using Xunit;

namespace Greenshot.Tests.RapidOcr
{
    /// <summary>
    /// The steps of RapidOCR around the models: text boxes from the probability map, cutting out lines
    /// </summary>
    public class RapidOcrPipelineTests
    {
        private const string ModelsVariable = "GREENSHOT_RAPIDOCR_MODELS";
        private const string SamplesVariable = "GREENSHOT_OCR_SAMPLES";

        [Fact]
        public void TextBoxes_AreFoundGrownAndScaled()
        {
            // A map of 64 x 32 with two "lines" of text, the image is twice as large
            int width = 64, height = 32;
            var map = new float[width * height];
            Fill(map, width, 4, 4, 30, 10, 0.9f);
            Fill(map, width, 4, 18, 50, 24, 0.9f);

            var boxes = TextBoxFinder.Find(map, width, height, 128, 64);

            Assert.Equal(2, boxes.Count);
            var first = boxes[0];
            // Grown around the area (4..30 x 4..10), then scaled by 2
            Assert.True(first.TopLeft.X < 8 && first.TopLeft.Y < 8, first.TopLeft.ToString());
            Assert.True(first.BottomRight.X > 60 && first.BottomRight.Y > 20, first.BottomRight.ToString());
            Assert.True(boxes[1].TopLeft.Y > first.TopLeft.Y);
        }

        [Fact]
        public void TextBoxes_WeakAreasAreIgnored()
        {
            int width = 64, height = 32;
            var map = new float[width * height];
            // Above the threshold for the mask, but the mean is below the box threshold
            Fill(map, width, 4, 4, 30, 10, 0.4f);
            Assert.Empty(TextBoxFinder.Find(map, width, height, 64, 32));
        }

        [Fact]
        public void TextBoxes_OnOneLineAreSortedLeftToRight()
        {
            var right = new TextBox(new PointD(100, 12), new PointD(150, 12), new PointD(150, 30), new PointD(100, 30), 1);
            var left = new TextBox(new PointD(10, 15), new PointD(50, 15), new PointD(50, 30), new PointD(10, 30), 1);
            var below = new TextBox(new PointD(10, 40), new PointD(50, 40), new PointD(50, 60), new PointD(10, 60), 1);
            var sorted = TextBoxFinder.Sort(new[] { below, right, left }.ToList());
            Assert.Equal(new[] { left, right, below }, sorted);
        }

        [Fact]
        public void Crop_IsUprightAndResizeKeepsColors()
        {
            var image = new BgrImage(4, 2, new byte[] { 1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3 });
            var crop = image.Crop(new PointD(0, 0), new PointD(4, 0), new PointD(0, 2), 4, 2);
            Assert.Equal(image.Data, crop.Data);
            var resized = image.Resize(8, 4);
            Assert.All(Enumerable.Range(0, 8 * 4), i => Assert.Equal(new byte[] { 1, 2, 3 }, resized.Data.Skip(i * 3).Take(3).ToArray()));
            var turned = new BgrImage(2, 1, new byte[] { 10, 10, 10, 20, 20, 20 }).RotateCounterClockwise();
            Assert.Equal((1, 2), (turned.Width, turned.Height));
            Assert.Equal(20, turned.Data[0]);
        }

        /// <summary>
        /// With the models (GREENSHOT_RAPIDOCR_MODELS: a folder with det.onnx and rec.onnx, onnxruntime findable) and captures
        /// (GREENSHOT_OCR_SAMPLES): writes &lt;name&gt;.rapidocr.txt next to each capture
        /// </summary>
        [Fact]
        public void Samples_AreRead()
        {
            string models = Environment.GetEnvironmentVariable(ModelsVariable);
            string samples = Environment.GetEnvironmentVariable(SamplesVariable);
            Assert.SkipWhen(string.IsNullOrEmpty(models) || string.IsNullOrEmpty(samples), $"Set {ModelsVariable} and {SamplesVariable}");

            using var pipeline = new RapidOcrPipeline(Path.Combine(models, "det.onnx"), Path.Combine(models, "rec.onnx"));
            foreach (var file in Directory.GetFiles(samples, "*.png"))
            {
                using var bitmap = new Bitmap(file);
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var lines = pipeline.Read(bitmap);
                var report = new StringBuilder();
                foreach (var line in lines)
                {
                    report.AppendLine(line.Text);
                }
                report.AppendLine($"--- {lines.Count} lines in {watch.ElapsedMilliseconds} ms");
                foreach (var line in lines)
                {
                    report.AppendLine($"{line.Bounds}: " + string.Join(" | ", line.Words.Select(word => $"{word.Text} {word.Bounds}")));
                }
                File.WriteAllText(Path.ChangeExtension(file, ".rapidocr.txt"), report.ToString(), Encoding.UTF8);
            }
        }

        private static void Fill(float[] map, int width, int left, int top, int right, int bottom, float value)
        {
            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    map[y * width + x] = value;
                }
            }
        }
    }
}
