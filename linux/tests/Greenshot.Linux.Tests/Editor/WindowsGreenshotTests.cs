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

#pragma warning disable SYSLIB0011

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using Filters = Greenshot.Editor.Drawing.Filters;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using CutMarkStyle = Greenshot.Linux.Editor.Fields.CutMarkStyle;
using HighlightFilter = Greenshot.Linux.Editor.Fields.HighlightFilter;
using WindowsCutMarkStyle = Greenshot.Editor.Drawing.CutMarkStyle;
using Greenshot.Linux.Editor.Serialization;
using SkiaSharp;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;

namespace Greenshot.Linux.Tests.Editor;

/// <summary>
/// .greenshot files of the Windows editor: the elements written by the BinaryFormatter become elements of this editor
/// </summary>
public class WindowsGreenshotTests
{
    /// <summary>
    /// The names the Windows editor writes: its classes are in the assembly Greenshot.Editor, the bitmap is System.Drawing's
    /// </summary>
    private sealed class WindowsNames : SerializationBinder
    {
        public override void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            if (serializedType == typeof(FakeStringAlignment))
            {
                assemblyName = "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                typeName = "System.Drawing.StringAlignment";
            }
            else if (serializedType == typeof(FakeBitmap))
            {
                assemblyName = "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                typeName = "System.Drawing.Bitmap";
            }
            else if (serializedType.Namespace?.StartsWith("Greenshot.", StringComparison.Ordinal) == true)
            {
                assemblyName = serializedType.Namespace.StartsWith("Greenshot.Base", StringComparison.Ordinal)
                    ? "Greenshot.Base, Version=1.4.0.0, Culture=neutral, PublicKeyToken=null"
                    : "Greenshot.Editor, Version=1.4.0.0, Culture=neutral, PublicKeyToken=null";
                typeName = serializedType.FullName;
            }
            else
            {
                assemblyName = null;
                typeName = null;
            }
        }

        public override Type BindToType(string assemblyName, string typeName) => throw new NotSupportedException();
    }

    private static byte[] Png(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// A .greenshot file like Greenshot for Windows writes it: the PNG, the elements, their length and the marker
    /// </summary>
    private static byte[] WindowsFile(DrawableContainerList elements)
    {
        using var file = new MemoryStream();
        file.Write(Png(300, 200, SKColors.White));
        using var serialized = new MemoryStream();
        new BinaryFormatter { Binder = new WindowsNames() }.Serialize(serialized, elements);
        serialized.WriteTo(file);
        file.Write(BitConverter.GetBytes(serialized.Length));
        file.Write(Encoding.ASCII.GetBytes("Greenshot01.04"));
        return file.ToArray();
    }

    private static (List<DrawableElement> Elements, int CounterStart, string? Warning) Load(DrawableContainerList list)
    {
        var content = GreenshotFile.Load(new MemoryStream(WindowsFile(list)));
        Assert.NotNull(content.ElementsJson);
        var (elements, counterStart) = ElementSerializer.Deserialize(content.ElementsJson!);
        return (elements, counterStart, content.Warning);
    }

    [Fact]
    public void Shapes_and_text_keep_their_place_and_fields()
    {
        var list = new DrawableContainerList
        {
            new RectangleContainer().At(10, 20, 50, 30).Set("LINE_THICKNESS", 4).Set("LINE_COLOR", Color.Blue).Set("FILL_COLOR", Color.FromArgb(128, 255, 0, 0)).Set("SHADOW", false),
            new ArrowContainer().At(100, 100, -40, 20).Set("LINE_COLOR", Color.Red).Set("ARROWHEADS", ArrowContainer.ArrowHeadCombination.BOTH),
            new TextContainer().WithText("Hello\nWindows").At(5, 150, 120, 40)
                .Set("FONT_FAMILY", "DejaVu Sans").Set("FONT_SIZE", 16f).Set("FONT_BOLD", true).Set("TEXT_HORIZONTAL_ALIGNMENT", FakeStringAlignment.Far),
            new SpeechbubbleContainer().Pointing(new Point(250, 190)).WithText("Bubble").At(200, 10, 80, 50),
            new EllipseContainer().At(0, 0, 10, 10),
        };

        var (elements, _, warning) = Load(list);

        Assert.Null(warning);
        Assert.Equal(5, elements.Count);
        var rectangle = Assert.IsType<RectangleElement>(elements[0]);
        Assert.Equal(new SKRect(10, 20, 60, 50), rectangle.Bounds);
        Assert.Equal(4, rectangle.Style.LineThickness);
        Assert.Equal(new SKColor(0, 0, 255), rectangle.Style.LineColor);
        Assert.Equal(new SKColor(255, 0, 0, 128), rectangle.Style.FillColor);
        Assert.False(rectangle.Style.Shadow);

        var arrow = Assert.IsType<ArrowElement>(elements[1]);
        // The direction it was drawn in
        Assert.Equal(new SKRect(100, 100, 60, 120), arrow.Bounds);
        Assert.Equal(ArrowHeads.Both, arrow.Style.ArrowHeads);
        Assert.Equal(SKColors.Red, arrow.Style.LineColor);

        var text = Assert.IsType<TextElement>(elements[2]);
        Assert.Equal("Hello\nWindows", text.Text);
        Assert.Equal(("DejaVu Sans", 16f, true), (text.Style.FontFamily, text.Style.FontSize, text.Style.FontBold));
        Assert.Equal(TextHorizontalAlignment.Right, text.Style.TextHorizontalAlignment);

        var bubble = Assert.IsType<SpeechBubbleElement>(elements[3]);
        Assert.Equal(("Bubble", new SKPoint(250, 190)), (bubble.Text, bubble.Target));
        Assert.IsType<EllipseElement>(elements[4]);
    }

    [Fact]
    public void Freehand_step_labels_filters_and_emoji_are_taken_over()
    {
        var obfuscate = new ObfuscateContainer().At(10, 10, 40, 20).Set("PREPARED_FILTER_OBFUSCATE", FilterContainer.PreparedFilter.BLUR);
        ((DrawableContainer)obfuscate).Children.Add(new Filters.BlurFilter().Set("BLUR_RADIUS", 9));
        var highlight = new HighlightContainer().At(60, 10, 40, 20).Set("PREPARED_FILTER_HIGHLIGHT", FilterContainer.PreparedFilter.AREA_HIGHLIGHT);
        ((DrawableContainer)highlight).Children.Add(new Filters.HighlightFilter().Set("FILL_COLOR", Color.Lime));
        var list = new DrawableContainerList
        {
            // Points are relative to where the element was moved
            new FreehandContainer().Through(new Point(1, 2), new Point(30, 40), new Point(10, 60)).At(100, 50, 0, 0).Set("LINE_THICKNESS", 5),
            new StepLabelContainer().Starting(7).At(20, 20, 30, 30).Set("FILL_COLOR", Color.DarkRed),
            obfuscate,
            highlight,
            new EmojiContainer("🦊").At(200, 100, 64, 64),
        };

        var (elements, counterStart, _) = Load(list);

        var freehand = Assert.IsType<FreehandElement>(elements[0]);
        Assert.Equal([new SKPoint(101, 52), new SKPoint(130, 90), new SKPoint(110, 110)], freehand.Points);
        Assert.Equal(5, freehand.Style.LineThickness);
        Assert.IsType<StepLabelElement>(elements[1]);
        Assert.Equal(7, counterStart);
        var blur = Assert.IsType<ObfuscateElement>(elements[2]);
        Assert.Equal((ObfuscateFilter.Blur, 9), (blur.Style.ObfuscateFilter, blur.Style.BlurRadius));
        var area = Assert.IsType<HighlightElement>(elements[3]);
        Assert.Equal(HighlightFilter.AreaHighlight, area.Style.HighlightFilter);
        Assert.Equal(new SKColor(0, 255, 0), area.Style.FillColor);
        Assert.Equal("🦊", Assert.IsType<EmojiElement>(elements[4]).Emoji);
    }

    [Fact]
    public void Cut_edges_torn_edges_and_images_are_taken_over()
    {
        var list = new DrawableContainerList
        {
            new CutMarkContainer(horizontal: true).At(0, 80, 300, 30).Set("CUT_MARK_STYLE", WindowsCutMarkStyle.ZigZag).Set("TOOTH_HEIGHT", 9),
            new TornEdgeContainer().At(0, 0, 300, 200),
            new ImageContainer(new FakeBitmap(Png(20, 10, SKColors.Green))).At(5, 5, 20, 10),
        };

        var (elements, _, _) = Load(list);

        var mark = Assert.IsType<CutMarkElement>(elements[0]);
        Assert.Equal((true, 4711, 6, CutMarkStyle.ZigZag, 9), (mark.Horizontal, mark.Seed, mark.ShadowSize, mark.Style.CutMarkStyle, mark.ToothHeight));
        var torn = Assert.IsType<TornEdgeElement>(elements[1]);
        Assert.Equal([true, false, true, true], torn.Edges);
        Assert.Equal([8, 0, 8, 8], torn.Margins);
        Assert.Equal(42, torn.Seed);
        var image = Assert.IsType<ImageElement>(elements[2]);
        using var bitmap = SKBitmap.Decode(image.Png);
        Assert.Equal(SKColors.Green, bitmap.GetPixel(5, 5));

        // On the surface they span the image like there
        using var surface = new EditorSurface(new SKBitmap(300, 200));
        surface.LoadElements(ElementSerializer.Serialize(elements));
        Assert.Equal(new SKRect(0, 0, 300, 200), surface.Elements[1].Bounds);
    }

    [Fact]
    public void Unknown_containers_are_left_out_with_a_warning()
    {
        var list = new DrawableContainerList { new RectangleContainer().At(1, 1, 5, 5), new FutureContainer().At(0, 0, 10, 10) };

        var (elements, _, warning) = Load(list);

        Assert.IsType<RectangleElement>(Assert.Single(elements));
        Assert.Contains("FutureContainer", warning);
    }

    [Fact]
    public void Damaged_elements_still_open_the_image()
    {
        var file = WindowsFile(new DrawableContainerList { new RectangleContainer() });
        // Garbage instead of the elements, the length and marker stay
        int pngEnd = Png(300, 200, SKColors.White).Length;
        for (int i = pngEnd; i < pngEnd + 40; i++)
        {
            file[i] = 0xFF;
        }

        var content = GreenshotFile.Load(new MemoryStream(file));

        Assert.Equal(300, content.Image.Width);
        Assert.Null(content.ElementsJson);
        Assert.Contains("can't be read", content.Warning);
    }
}
