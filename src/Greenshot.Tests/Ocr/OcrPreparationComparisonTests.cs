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
using System.Threading.Tasks;
using Greenshot.Plugin.Win10;
using Xunit;

namespace Greenshot.Tests.Ocr
{
    /// <summary>
    /// Compares what Windows OCR reads with and without the preparation experiments, on own captures.
    /// Runs only when GREENSHOT_OCR_SAMPLES names a folder with PNG files; writes &lt;name&gt;.ocr.txt next to each.
    /// </summary>
    public class OcrPreparationComparisonTests
    {
        private const string SamplesVariable = "GREENSHOT_OCR_SAMPLES";

        private static readonly OcrPreparation[] Variants =
        {
            OcrPreparation.None, OcrPreparation.RemoveLines, OcrPreparation.Upscale, OcrPreparation.RemoveLines | OcrPreparation.Upscale
        };

        [Fact]
        public async Task Samples_AreReadWithEveryVariant()
        {
            string folder = Environment.GetEnvironmentVariable(SamplesVariable);
            Assert.SkipWhen(string.IsNullOrEmpty(folder) || !Directory.Exists(folder), $"Set {SamplesVariable} to a folder with PNG captures");
            Assert.SkipWhen(Environment.OSVersion.Platform != PlatformID.Win32NT, "Needs Windows OCR");

            var files = Directory.GetFiles(folder, "*.png");
            Assert.NotEmpty(files);
            var provider = new Win10OcrProvider();
            foreach (var file in files)
            {
                var report = new StringBuilder();
                using (var image = Image.FromFile(file))
                {
                    foreach (var variant in Variants)
                    {
                        var lines = await provider.DoOcrAsync(image, null, variant);
                        report.AppendLine($"===== {variant} =====");
                        foreach (var line in lines)
                        {
                            report.AppendLine(string.Join(" ", line.Words.Select(word => word.Text)));
                        }

                        report.AppendLine();
                    }
                }

                File.WriteAllText(Path.ChangeExtension(file, ".ocr.txt"), report.ToString(), Encoding.UTF8);
            }
        }
    }
}
