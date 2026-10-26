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

using System.Text;
using Greenshot.Linux.Core.Barcodes;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Capturing.Interactive;
using Greenshot.Linux.Core.Ocr;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Editor;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Destinations;

public class DestinationTests
{
    private const string Tsv = "level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext\n"
        + "1\t1\t0\t0\t0\t0\t0\t0\t800\t200\t-1\t\n"
        + "4\t1\t1\t1\t1\t0\t20\t20\t300\t30\t-1\t\n"
        + "5\t1\t1\t1\t1\t1\t20\t20\t80\t30\t95.5\tHello\n"
        + "5\t1\t1\t1\t1\t2\t110\t22\t90\t28\t91.0\tsecret-42\n"
        + "5\t1\t1\t1\t2\t1\t20\t80\t60\t30\t88.0\tNext\n"
        + "5\t1\t1\t1\t2\t2\t90\t80\t40\t30\t-1\t \n";

    [Fact]
    public void Tesseract_tsv_becomes_lines_of_words()
    {
        var lines = TesseractOcr.ParseTsv(Tsv);
        Assert.Equal(["Hello secret-42", "Next"], lines.Select(l => l.Text));
        Assert.Equal(SKRectI.Create(20, 20, 180, 30), lines[0].Bounds);
    }

    [Fact]
    public void Upscaled_positions_are_mapped_back()
    {
        var lines = TesseractOcr.ParseTsv(Tsv, scale: 2);
        Assert.Equal(SKRectI.Create(10, 10, 40, 15), lines[0].Words[0].Bounds);
    }

    [Theory]
    [InlineData("", "de-DE", "deu+eng")]
    [InlineData("", "sv-SE", "eng")]
    [InlineData("", "zh-TW", "eng")]
    [InlineData("fra", "de-DE", "fra")]
    public void Ocr_languages_follow_greenshot_when_installed(string configured, string ui, string expected)
    {
        Assert.Equal(expected, TesseractOcr.ChooseLanguages(configured, ui, ["eng", "deu", "osd"]));
    }

    [Fact]
    public void Qr_codes_can_be_made_and_read()
    {
        using var code = Barcodes.CreateQrCode("https://getgreenshot.org/", 200);
        using var capture = new SKBitmap(new SKImageInfo(500, 400, SKColorType.Bgra8888, SKAlphaType.Premul));
        capture.Erase(SKColors.White);
        using (var canvas = new SKCanvas(capture))
        {
            canvas.DrawBitmap(code, 150, 100);
        }

        var found = Assert.Single(Barcodes.Detect(capture));
        Assert.Equal("https://getgreenshot.org/", found.Text);
        Assert.True(found.IsUrl);
        Assert.Equal("QR_CODE", found.Format);
    }

    [Fact]
    public void An_image_without_codes_has_none()
    {
        using var empty = new SKBitmap(100, 100);
        empty.Erase(SKColors.White);
        Assert.Empty(Barcodes.Detect(empty));
    }

    [Fact]
    public void Small_images_are_printed_at_100_pixels_per_inch_and_centered()
    {
        var area = new SKRect(0, 0, 500, 700);
        var (rect, rotated) = PrintLayout.Arrange(100, 200, area, new PrintOptions());
        Assert.False(rotated);
        Assert.Equal(72, rect.Width, 3);
        Assert.Equal(144, rect.Height, 3);
        Assert.Equal(250, rect.MidX, 3);
    }

    [Fact]
    public void Large_images_shrink_and_turn_when_allowed()
    {
        var area = new SKRect(0, 0, 500, 700);
        var (rect, rotated) = PrintLayout.Arrange(2000, 1000, area, new PrintOptions(AllowRotate: true, Center: false));
        Assert.True(rotated);
        Assert.Equal(new SKPoint(0, 0), rect.Location);
        Assert.True(rect.Width <= 500.01 && rect.Height <= 700.01);

        var (noShrink, _) = PrintLayout.Arrange(2000, 1000, area, new PrintOptions(AllowShrink: false));
        Assert.Equal(1440, noShrink.Width, 3);
    }

    [Fact]
    public void Printing_makes_a_pdf()
    {
        using var image = new SKBitmap(300, 200);
        image.Erase(SKColors.Red);
        var pdf = PrintLayout.CreatePdf(image, PageSetup.A4, new PrintOptions(Footer: "2026-10-26 - Firefox"), "Firefox");
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void Redaction_covers_the_words_of_a_match()
    {
        var lines = TesseractOcr.ParseTsv(Tsv);
        var matches = TextRedaction.FindMatches(lines, "SECRET", useRegex: false, caseSensitive: false, 0, 0);
        Assert.Equal(new SKRect(110, 22, 200, 50), Assert.Single(matches));

        var regex = TextRedaction.FindMatches(lines, @"secret-\d+|Next", useRegex: true, caseSensitive: true);
        Assert.Equal(2, regex.Count);
        // 10% wider and 20% higher in total, half on each side
        Assert.Equal(110 - 4.5f, regex[0].Left, 3);

        Assert.Empty(TextRedaction.FindMatches(lines, "SECRET", useRegex: false, caseSensitive: true));
        Assert.Throws<System.Text.RegularExpressions.RegexParseException>(() => TextRedaction.FindMatches(lines, "(", true, false));
    }

    [Fact]
    public void Redaction_adds_obfuscations_in_one_undo_step()
    {
        var image = new SKBitmap(300, 100);
        image.Erase(SKColors.White);
        using var surface = new EditorSurface(image);
        var controller = new EditorController(surface);
        TextRedaction.Apply(controller, [new SKRect(1, 1, 10, 10), new SKRect(20, 1, 30, 10)]);
        Assert.Equal(2, surface.Elements.Count);
        surface.Undo();
        Assert.Empty(surface.Elements);
    }

    [Fact]
    public void Text_mode_reads_the_screen_once_and_a_click_captures_a_line()
    {
        var model = new SelectionModel(new SKRectI(0, 0, 500, 500), [], CaptureMode.Region, new SKPointI(0, 0));
        int requests = 0;
        model.TextRecognitionRequested += () => requests++;
        SelectionResult? result = null;
        model.Completed += r => result = r;

        model.ToggleTextMode();
        model.ToggleTextMode();
        model.ToggleTextMode();
        Assert.Equal(1, requests);

        var line = new TextLine(SKRectI.Create(10, 10, 200, 20), [SKRectI.Create(10, 10, 90, 20), SKRectI.Create(110, 10, 100, 20)]);
        model.SetTextLines([line]);
        model.MoveTo(new SKPointI(50, 15));
        Assert.Equal(line, model.HoveredLine);
        Assert.Equal(line.Bounds, model.Highlight);

        model.Press();
        model.Release();
        Assert.Equal(CaptureMode.Text, result!.Mode);
        Assert.Equal(new SKRectI(7, 7, 213, 33), result.Pixels);
    }

    [Fact]
    public void Dragging_in_text_mode_highlights_the_words_inside()
    {
        var model = new SelectionModel(new SKRectI(0, 0, 500, 500), [], CaptureMode.Region, new SKPointI(0, 0));
        model.ToggleTextMode();
        model.SetTextLines([new TextLine(SKRectI.Create(10, 10, 200, 20), [SKRectI.Create(10, 10, 90, 20), SKRectI.Create(110, 10, 100, 20)])]);
        model.MoveTo(new SKPointI(0, 0));
        model.Press();
        model.MoveTo(new SKPointI(50, 50));
        Assert.Equal([SKRectI.Create(10, 10, 90, 20)], model.SelectedWords);
    }

    [Fact]
    public void T_selects_a_region_for_text()
    {
        var model = new SelectionModel(new SKRectI(0, 0, 500, 500), [], CaptureMode.Region, new SKPointI(0, 0));
        SelectionResult? result = null;
        model.Completed += r => result = r;
        model.ToggleTextMode();
        model.MoveTo(new SKPointI(10, 10));
        model.Press();
        model.MoveTo(new SKPointI(100, 50));
        model.Release();
        Assert.Equal(CaptureMode.Text, result!.Mode);
        Assert.Equal(new SKRectI(10, 10, 101, 51), result.Pixels);
    }
}
