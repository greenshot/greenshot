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

using System.Globalization;
using SkiaSharp;

namespace Greenshot.Linux.Core.Ocr;

public sealed record OcrWord(string Text, SKRectI Bounds);

/// <summary>
/// A line of recognized text with its words, the OcrLine of the Windows OCR
/// </summary>
public sealed record OcrLine(string Text, SKRectI Bounds, IReadOnlyList<OcrWord> Words);

/// <summary>
/// Text recognition with tesseract, the Windows.Media.Ocr of the Windows version.
/// The tesseract program and its language packs come from the distribution (tesseract, tesseract-langpack-deu, ...).
/// </summary>
public static class TesseractOcr
{
    /// <summary>
    /// Small text is recognized much better when it is larger; captures smaller than this many pixels are scaled up
    /// </summary>
    private const long UpscaleBelowPixels = 4_000_000;

    public static bool IsAvailable => ProcessRunner.IsOnPath("tesseract");

    /// <summary>
    /// The installed language packs, e.g. eng, deu, swe
    /// </summary>
    public static async Task<IReadOnlyList<string>> GetLanguagesAsync()
    {
        if (!IsAvailable)
        {
            return [];
        }

        var result = await ProcessRunner.RunAsync("tesseract", ["--list-langs"], timeout: TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        // The first line is "List of available languages in ...", "osd" is the orientation detection, not a language
        return result.OutputText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !l.Contains(' ') && l != "osd")
            .ToList();
    }

    /// <summary>
    /// The languages to recognize: the configured ones, else the language of Greenshot and English, as far as installed
    /// </summary>
    public static string ChooseLanguages(string? configured, string uiLanguage, IReadOnlyList<string> installed)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        var wanted = new List<string>();
        if (ToTesseract(uiLanguage) is { } ui)
        {
            wanted.Add(ui);
        }

        wanted.Add("eng");
        var available = wanted.Distinct().Where(installed.Contains).ToList();
        return available.Count > 0 ? string.Join('+', available) : installed.FirstOrDefault() ?? "eng";
    }

    /// <summary>
    /// The tesseract code (ISO 639-2) of an IETF tag
    /// </summary>
    internal static string? ToTesseract(string ietf)
    {
        var lower = ietf.ToLowerInvariant();
        if (lower.StartsWith("zh", StringComparison.Ordinal))
        {
            return lower.Contains("tw") || lower.Contains("hant") ? "chi_tra" : "chi_sim";
        }

        return lower.Split('-')[0] switch
        {
            "ar" => "ara", "ca" => "cat", "cs" => "ces", "da" => "dan", "de" => "deu", "el" => "ell", "en" => "eng",
            "es" => "spa", "et" => "est", "fa" => "fas", "fi" => "fin", "fr" => "fra", "he" => "heb", "hu" => "hun",
            "id" => "ind", "it" => "ita", "ja" => "jpn", "ko" => "kor", "lt" => "lit", "lv" => "lav", "nl" => "nld",
            "nb" or "nn" or "no" => "nor", "pl" => "pol", "pt" => "por", "ro" => "ron", "ru" => "rus", "sk" => "slk",
            "sl" => "slv", "sr" => "srp", "sv" => "swe", "tr" => "tur", "uk" => "ukr", "vi" => "vie",
            _ => null,
        };
    }

    /// <summary>
    /// Recognize the text of the image; positions are pixels of the image
    /// </summary>
    public static async Task<IReadOnlyList<OcrLine>> RecognizeAsync(SKBitmap image, string languages, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Text recognition needs tesseract, install it with the package manager (e.g. tesseract and tesseract-langpack-eng)");
        }

        float scale = (long)image.Width * image.Height < UpscaleBelowPixels ? 2 : 1;
        byte[] png;
        if (scale > 1)
        {
            using var larger = new SKBitmap(new SKImageInfo((int)(image.Width * scale), (int)(image.Height * scale), SKColorType.Bgra8888, SKAlphaType.Premul));
            using (var canvas = new SKCanvas(larger))
            using (var source = SKImage.FromBitmap(image))
            {
                // The text has to stay on white: transparent parts would become black
                canvas.Clear(SKColors.White);
                canvas.DrawImage(source, new SKRect(0, 0, larger.Width, larger.Height), new SKSamplingOptions(SKCubicResampler.Mitchell));
            }

            png = Encode(larger);
        }
        else
        {
            png = Encode(image);
        }

        var result = await ProcessRunner.RunAsync("tesseract", ["stdin", "stdout", "-l", languages, "--psm", "3", "tsv"], png,
            TimeSpan.FromSeconds(120), cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"tesseract failed: {result.Error.Trim()}");
        }

        return ParseTsv(result.OutputText, scale);
    }

    /// <summary>
    /// The TSV of tesseract: level, page, block, paragraph, line, word, left, top, width, height, confidence, text.
    /// Words (level 5) are grouped into their lines; the positions are divided by the scale the image was enlarged with.
    /// </summary>
    internal static IReadOnlyList<OcrLine> ParseTsv(string tsv, float scale = 1)
    {
        var lines = new List<(string Key, List<OcrWord> Words)>();
        foreach (var row in tsv.Split('\n').Skip(1))
        {
            var columns = row.TrimEnd('\r').Split('\t');
            if (columns.Length < 12 || columns[0] != "5")
            {
                continue;
            }

            var text = columns[11].Trim();
            if (text.Length == 0 || !float.TryParse(columns[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var confidence) || confidence < 0)
            {
                continue;
            }

            int Value(int index) => int.Parse(columns[index], CultureInfo.InvariantCulture);
            var bounds = SKRectI.Create(
                (int)Math.Floor(Value(6) / scale), (int)Math.Floor(Value(7) / scale),
                (int)Math.Ceiling(Value(8) / scale), (int)Math.Ceiling(Value(9) / scale));
            var key = $"{columns[1]}.{columns[2]}.{columns[3]}.{columns[4]}";
            if (lines.Count == 0 || lines[^1].Key != key)
            {
                lines.Add((key, new List<OcrWord>()));
            }

            lines[^1].Words.Add(new OcrWord(text, bounds));
        }

        return lines.Select(l =>
        {
            var bounds = l.Words.Select(w => w.Bounds).Aggregate((a, b) => SKRectI.Union(a, b));
            return new OcrLine(string.Join(' ', l.Words.Select(w => w.Text)), bounds, l.Words);
        }).ToList();
    }

    private static byte[] Encode(SKBitmap bitmap)
    {
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
