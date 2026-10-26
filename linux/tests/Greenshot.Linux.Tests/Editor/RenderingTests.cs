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

using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Effects;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Serialization;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Editor;

public class RenderingTests
{
    private static SKBitmap Filled(SKColor color, int width = 100, int height = 100)
    {
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        bitmap.Erase(color);
        return bitmap;
    }

    private static T WithDefaults<T>(T element, SKRect bounds) where T : DrawableElement
    {
        element.Style = element.CreateDefaultStyle();
        element.Bounds = bounds;
        return element;
    }

    [Fact]
    public void A_rectangle_draws_its_line_and_its_shadow()
    {
        using var surface = new EditorSurface(Filled(SKColors.White));
        surface.Add(WithDefaults(new RectangleElement(), new SKRect(20, 20, 60, 60)), select: false);
        using var result = surface.Flatten();

        var line = result.GetPixel(20, 40);
        Assert.True(line.Red > 200 && line.Green < 80, $"line is {line}");
        Assert.Equal(SKColors.White, result.GetPixel(40, 40));
        // The shadow lies right below the line, darker than the background
        Assert.True(result.GetPixel(63, 40).Red < 250);
    }

    [Fact]
    public void Text_highlight_keeps_the_darker_channels()
    {
        using var image = Filled(new SKColor(0x40, 0xC0, 0xFF));
        using var surface = new EditorSurface(image.Copy());
        surface.Add(WithDefaults(new HighlightElement(), new SKRect(0, 0, 50, 50)), select: false);
        using var result = surface.Flatten();
        // Yellow is (255, 255, 0): red and green stay, blue goes to 0
        Assert.Equal(new SKColor(0x40, 0xC0, 0x00), result.GetPixel(10, 10));
        Assert.Equal(new SKColor(0x40, 0xC0, 0xFF), result.GetPixel(70, 70));
    }

    [Fact]
    public void Pixelate_averages_each_block()
    {
        using var image = Filled(SKColors.Black, 10, 10);
        for (int x = 0; x < 5; x += 2)
        {
            for (int y = 0; y < 5; y++)
            {
                image.SetPixel(x, y, SKColors.White);
            }
        }

        using var surface = new EditorSurface(image.Copy());
        surface.Add(WithDefaults(new ObfuscateElement(), new SKRect(0, 0, 10, 10)), select: false);
        using var result = surface.Flatten();
        // 3 of 5 columns of the first block are white: 3/5 of 255
        Assert.Equal(153, result.GetPixel(0, 0).Red);
        Assert.Equal(result.GetPixel(0, 0), result.GetPixel(4, 4));
        Assert.Equal(SKColors.Black, result.GetPixel(7, 7));
    }

    [Fact]
    public void Grayscale_highlight_changes_only_the_outside()
    {
        using var surface = new EditorSurface(Filled(SKColors.Red));
        var element = WithDefaults(new HighlightElement(), new SKRect(10, 10, 30, 30));
        element.Style.HighlightFilter = HighlightFilter.Grayscale;
        surface.Add(element, select: false);
        using var result = surface.Flatten();
        Assert.Equal(SKColors.Red, result.GetPixel(20, 20));
        var outside = result.GetPixel(80, 80);
        Assert.Equal(outside.Red, outside.Green);
    }

    [Fact]
    public void Step_labels_draw_a_filled_circle()
    {
        using var surface = new EditorSurface(Filled(SKColors.White));
        surface.Add(WithDefaults(new StepLabelElement(), new SKRect(10, 10, 40, 40)), select: false);
        using var result = surface.Flatten();
        // Dark red near the edge of the circle, away from the white number
        Assert.Equal(new SKColor(0x8B, 0, 0), result.GetPixel(14, 25));
    }

    [Fact]
    public void All_elements_survive_serialization()
    {
        var elements = new List<DrawableElement>
        {
            WithDefaults(new RectangleElement(), new SKRect(1, 2, 3, 4)),
            WithDefaults(new EllipseElement(), new SKRect(1, 2, 3, 4)),
            WithDefaults(new LineElement(), new SKRect(30, 20, 10, 5)),
            WithDefaults(new ArrowElement(), new SKRect(1, 2, 3, 4)),
            new FreehandElement { Points = [new SKPoint(1, 2), new SKPoint(5, 6)] },
            new TextElement { Text = "Hello\nWorld", Bounds = new SKRect(0, 0, 100, 40) },
            new SpeechBubbleElement { Text = "Hi", Target = new SKPoint(-5, 7), Bounds = new SKRect(0, 0, 100, 40) },
            WithDefaults(new StepLabelElement(), new SKRect(0, 0, 30, 30)),
            WithDefaults(new HighlightElement(), new SKRect(0, 0, 30, 30)),
            WithDefaults(new ObfuscateElement(), new SKRect(0, 0, 30, 30)),
        };
        elements[0].Style.FillColor = new SKColor(1, 2, 3, 4);
        elements[3].Style.ArrowHeads = ArrowHeads.Both;

        var json = ElementSerializer.Serialize(elements, counterStart: 3);
        var (copy, counterStart) = ElementSerializer.Deserialize(json);

        Assert.Equal(3, counterStart);
        Assert.Equal(elements.Select(e => e.GetType()), copy.Select(e => e.GetType()));
        Assert.Equal(elements.Select(e => e.Bounds), copy.Select(e => e.Bounds));
        Assert.Equal(new SKColor(1, 2, 3, 4), copy[0].Style.FillColor);
        Assert.Equal(ArrowHeads.Both, copy[3].Style.ArrowHeads);
        Assert.Equal("Hello\nWorld", ((TextElement)copy[5]).Text);
        Assert.Equal(new SKPoint(-5, 7), ((SpeechBubbleElement)copy[6]).Target);
        Assert.Equal(2, ((FreehandElement)copy[4]).Points.Count);
    }

    [Fact]
    public void Greenshot_files_keep_image_and_elements()
    {
        using var stream = new MemoryStream();
        using var image = Filled(SKColors.Green, 40, 30);
        var json = ElementSerializer.Serialize([new TextElement { Text = "x", Bounds = new SKRect(1, 1, 20, 20) }]);
        GreenshotFile.Save(stream, image, json);

        stream.Position = 0;
        var content = GreenshotFile.Load(stream);
        Assert.Equal(40, content.Image.Width);
        Assert.Equal(json, content.ElementsJson);
        Assert.Null(content.Warning);
    }

    [Fact]
    public void Windows_greenshot_files_load_the_image_with_a_warning()
    {
        using var stream = new MemoryStream();
        using (var image = Filled(SKColors.Green, 40, 30))
        using (var png = image.Encode(SKEncodedImageFormat.Png, 100))
        {
            png.SaveTo(stream);
        }

        var binary = new byte[] { 0, 1, 2, 3 };
        stream.Write(binary);
        stream.Write(BitConverter.GetBytes((long)binary.Length));
        stream.Write("Greenshot01.04"u8);
        stream.Position = 0;

        var content = GreenshotFile.Load(stream);
        Assert.Equal(30, content.Image.Height);
        Assert.Null(content.ElementsJson);
        Assert.Contains("Windows", content.Warning);
    }

    [Fact]
    public void Rotation_maps_pixels_and_elements_the_same_way()
    {
        using var image = Filled(SKColors.White, 40, 20);
        image.SetPixel(5, 2, SKColors.Red);
        var clockwise = ImageEffects.RotateClockwise(image);
        var point = clockwise.ElementTransform.MapPoint(new SKPoint(5.5f, 2.5f));
        Assert.Equal(20, clockwise.Image.Width);
        Assert.Equal(SKColors.Red, clockwise.Image.GetPixel((int)point.X, (int)point.Y));

        var counter = ImageEffects.RotateCounterClockwise(image);
        point = counter.ElementTransform.MapPoint(new SKPoint(5.5f, 2.5f));
        Assert.Equal(SKColors.Red, counter.Image.GetPixel((int)point.X, (int)point.Y));
    }

    [Fact]
    public void Effects_grow_the_image_like_on_windows()
    {
        using var image = Filled(SKColors.White, 50, 40);
        Assert.Equal((54, 44), Size(ImageEffects.Border(image)));
        Assert.Equal((64, 54), Size(ImageEffects.DropShadow(image)));
        Assert.Equal((64, 54), Size(ImageEffects.TornEdge(image, seed: 1)));
        Assert.Equal((100, 80), Size(ImageEffects.Resize(image, 100, 80)));
        Assert.Equal((100, 90), Size(ImageEffects.ResizeCanvas(image, 25, 25, 25, 25)));
    }

    [Fact]
    public void Invert_and_grayscale_change_the_colors()
    {
        using var image = Filled(new SKColor(255, 0, 0));
        Assert.Equal(new SKColor(0, 255, 255), ImageEffects.Invert(image).Image.GetPixel(1, 1));
        var gray = ImageEffects.Grayscale(image).Image.GetPixel(1, 1);
        Assert.Equal(gray.Red, gray.Blue);
        Assert.InRange(gray.Red, 70, 80);
    }

    [Fact]
    public void Auto_crop_removes_a_uniform_border()
    {
        using var image = Filled(SKColors.White, 50, 40);
        image.SetPixel(10, 5, SKColors.Black);
        image.SetPixel(30, 25, SKColors.Black);
        Assert.Equal(new SKRectI(10, 5, 31, 26), ImageEffects.FindAutoCropArea(image));
        using var plain = Filled(SKColors.White, 50, 40);
        Assert.Null(ImageEffects.FindAutoCropArea(plain));
    }

    private static (int, int) Size(EffectResult result)
    {
        using (result.Image)
        {
            return (result.Image.Width, result.Image.Height);
        }
    }
}
