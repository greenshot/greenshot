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

/// <summary>
/// Crop out, the cut edges and the torn edges element, and the settings of the drop shadow and torn edges
/// </summary>
public class CutTests
{
    /// <summary>
    /// Rows of 10 pixels in different colors, so what is left can be told
    /// </summary>
    private static SKBitmap Striped(int width = 200, int height = 100)
    {
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        for (int y = 0; y < height; y++)
        {
            var color = new SKColor((byte)(y / 10 * 20), 100, 200);
            for (int x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, color);
            }
        }

        return bitmap;
    }

    private static SKColor Row(int y) => new((byte)(y / 10 * 20), 100, 200);

    private static RectangleElement Rectangle(float left, float top, float right, float bottom) =>
        new() { Bounds = new SKRect(left, top, right, bottom), Style = new RectangleElement().CreateDefaultStyle() };

    [Fact]
    public void Settings_use_the_format_of_the_windows_ini()
    {
        const string windows = "Darkness:0.60|ShadowSize:7|ShadowOffset:-1,-1|GenerateShadow:True|ToothHeight:12|HorizontalToothRange:20|VerticalToothRange:20|Edges:True,False,True,True|BackgroundColor:-16777216";
        var settings = TornEdgeSettings.Parse(windows);
        Assert.Equal([true, false, true, true], settings.Edges);
        Assert.Equal(SKColors.Black, settings.BackgroundColor);
        Assert.Equal(new SKPointI(-1, -1), settings.ShadowOffset);
        Assert.Equal(windows, settings.ToString());

        var shadow = DropShadowSettings.Parse("Darkness:0.30|ShadowSize:4|ShadowOffset:2,3");
        Assert.Equal((0.3f, 4, new SKPointI(2, 3)), (shadow.Darkness, shadow.ShadowSize, shadow.ShadowOffset));
        Assert.Equal(7, DropShadowSettings.Parse("Darkness:7|ShadowSize:x").ShadowSize);
    }

    [Fact]
    public void Crop_out_horizontally_removes_the_rows_and_moves_the_elements_below()
    {
        using var surface = new EditorSurface(Striped());
        var above = Rectangle(10, 5, 50, 15);
        var inside = Rectangle(10, 32, 50, 38);
        var below = Rectangle(10, 70, 50, 80);
        var across = Rectangle(10, 25, 50, 45);
        foreach (var element in new[] { above, inside, below, across })
        {
            surface.Add(element, select: false);
        }

        Assert.True(surface.ApplyCutOut(new SKRectI(0, 30, 200, 50), horizontal: true));

        Assert.Equal(80, surface.Height);
        Assert.Equal(Row(29), surface.Image.GetPixel(5, 29));
        Assert.Equal(Row(50), surface.Image.GetPixel(5, 30));
        Assert.DoesNotContain(inside, surface.Elements);
        Assert.Equal(new SKRect(10, 50, 50, 60), below.Bounds);
        Assert.Equal(new SKRect(10, 5, 50, 15), above.Bounds);
        Assert.Equal(new SKRect(10, 25, 50, 45), across.Bounds);

        surface.Undo();
        Assert.Equal(100, surface.Height);
        Assert.Equal(4, surface.Elements.Count);
    }

    [Fact]
    public void Crop_out_with_a_cut_edge_style_keeps_a_gap_for_the_cut_mark()
    {
        using var surface = new EditorSurface(Striped());
        var settings = new TornEdgeSettings { ToothHeight = 4, GenerateShadow = false };

        Assert.True(surface.ApplyCutOut(new SKRectI(60, 0, 100, 100), horizontal: false, CutMarkStyle.ZigZag, settings));

        // 4 columns stay for the gap
        Assert.Equal(164, surface.Width);
        var mark = Assert.IsType<CutMarkElement>(Assert.Single(surface.Elements));
        Assert.False(mark.Horizontal);
        Assert.Equal(CutMarkStyle.ZigZag, mark.Style.CutMarkStyle);
        Assert.Equal(new SKRect(56, 0, 68, 100), mark.Bounds);

        // The gap is cut out of the picture: transparent without a fill color (and here without the shadow which falls into it)
        using var flattened = surface.Flatten();
        Assert.Equal(0, flattened.GetPixel(62, 50).Alpha);
        Assert.Equal(255, flattened.GetPixel(20, 50).Alpha);
    }

    [Fact]
    public void Crop_out_of_everything_or_at_the_border_has_no_cut_mark()
    {
        using var surface = new EditorSurface(Striped());
        Assert.False(surface.ApplyCutOut(new SKRectI(0, 0, 200, 100), horizontal: true, CutMarkStyle.Torn));
        Assert.True(surface.ApplyCutOut(new SKRectI(0, 0, 200, 20), horizontal: true, CutMarkStyle.Torn));
        Assert.Empty(surface.Elements);
        Assert.Equal(80, surface.Height);
    }

    [Fact]
    public void A_cut_mark_keeps_its_gap_when_the_teeth_change()
    {
        var mark = new CutMarkElement { Horizontal = true, Bounds = new SKRect(0, 40, 200, 70) };
        mark.Style = mark.CreateDefaultStyle();
        mark.FitToImage(200, 100);
        mark.SetField(FieldType.ToothHeight, 15);
        Assert.Equal(new SKRect(0, 37, 200, 73), mark.Bounds);

        // It can only move across the cut, and always spans the image
        mark.MoveBy(30, 5);
        Assert.Equal(new SKRect(0, 42, 200, 78), mark.Bounds);
    }

    [Fact]
    public void Crop_with_a_cut_edge_style_tears_the_cut_sides_and_makes_room_for_the_shadow()
    {
        using var surface = new EditorSurface(Striped());
        var settings = new TornEdgeSettings { ShadowSize = 5, ShadowOffset = new SKPointI(-1, -1) };
        var inside = Rectangle(60, 30, 80, 40);

        surface.Add(inside, select: false);
        Assert.True(surface.ApplyCrop(new SKRectI(50, 0, 150, 60), CutMarkStyle.Wave, settings));

        // Top and the image's own top stay straight; right, bottom and left were cut and get 6 pixels for the shadow
        Assert.Equal((112, 66), (surface.Width, surface.Height));
        var edges = Assert.IsType<TornEdgeElement>(surface.Elements.Last());
        Assert.Equal([false, true, true, true], edges.Edges);
        Assert.Equal([0, 6, 6, 6], edges.Margins);
        Assert.Equal(CutMarkStyle.Wave, edges.Style.CutMarkStyle);
        Assert.Equal(new SKRect(16, 30, 36, 40), inside.Bounds);
    }

    [Fact]
    public void Torn_edges_are_an_element_which_can_be_applied_to_the_image()
    {
        using var surface = new EditorSurface(Striped());
        var edges = surface.AddTornEdges(new TornEdgeSettings());

        // Size 7 and offset 1: 8 pixels on every side
        Assert.Equal((216, 116), (surface.Width, surface.Height));
        Assert.Same(edges, Assert.Single(surface.Selection));
        Assert.Same(edges, surface.AddTornEdges(new TornEdgeSettings()));
        Assert.Equal(new SKRect(0, 0, 216, 116), edges.Bounds);
        Assert.True(edges.HitTest(new SKPoint(100, 3), 0));
        Assert.False(edges.HitTest(new SKPoint(100, 60), 0));

        int seed = edges.Seed;
        edges.NewSeed();
        Assert.NotEqual(seed, edges.Seed);

        Assert.True(surface.ApplyElementToImage(edges));
        Assert.Empty(surface.Elements);
        Assert.Equal(0, surface.Image.GetPixel(0, 0).Alpha);
        Assert.Equal(Row(50), surface.Image.GetPixel(108, 58));
    }

    [Fact]
    public void Cut_elements_survive_undo_and_copy()
    {
        using var surface = new EditorSurface(Striped());
        surface.ApplyCutOut(new SKRectI(0, 40, 200, 60), horizontal: true, CutMarkStyle.Torn, new TornEdgeSettings { ToothHeight = 5 });
        var mark = Assert.IsType<CutMarkElement>(Assert.Single(surface.Elements));

        var copy = Assert.IsType<CutMarkElement>(Assert.Single(ElementSerializer.Clone([mark])));
        Assert.Equal((mark.Seed, mark.Horizontal, mark.Bounds, mark.ToothHeight), (copy.Seed, copy.Horizontal, copy.Bounds, copy.ToothHeight));

        surface.AddTornEdges(new TornEdgeSettings { GenerateShadow = false });
        surface.Undo();
        Assert.IsType<CutMarkElement>(Assert.Single(surface.Elements));
        surface.Redo();
        Assert.Equal(2, surface.Elements.Count);
    }

    [Fact]
    public void The_crop_tool_cuts_out_in_its_crop_mode()
    {
        using var surface = new EditorSurface(Striped());
        var controller = new EditorController(surface) { Tool = EditorTool.Crop };
        controller.SetFieldValue(FieldType.CropMode, CropMode.Horizontal);
        Assert.Equal([FieldType.CropMode, FieldType.CutMarkStyle], controller.VisibleFields);

        controller.PointerPressed(new SKPoint(50, 20), InputModifiers.None);
        controller.PointerMoved(new SKPoint(80, 40), InputModifiers.None);
        controller.PointerReleased(new SKPoint(80, 40), InputModifiers.None);
        // A crop out spans the image, it can only be sized across the cut
        Assert.Equal(new SKRect(0, 20, 200, 40), controller.CropArea);
        Assert.Equal([HandlePosition.Top, HandlePosition.Bottom], controller.CropHandles.Select(h => h.Position));

        Assert.True(controller.ConfirmCrop());
        Assert.Equal((200, 80), (surface.Width, surface.Height));

        controller.Tool = EditorTool.Crop;
        controller.CycleCropMode();
        Assert.Equal(CropMode.Default, controller.CropMode);
    }

    [Fact]
    public void Drop_shadow_and_torn_edges_use_their_settings()
    {
        using var source = Striped(50, 40);
        using var shadow = ImageEffects.DropShadow(source, new DropShadowSettings { ShadowSize = 3, ShadowOffset = new SKPointI(2, 2) }).Image;
        Assert.Equal((56, 46), (shadow.Width, shadow.Height));

        var settings = new TornEdgeSettings { GenerateShadow = false, BackgroundColor = SKColors.Yellow, Edges = [true, false, false, false] };
        using var torn = ImageEffects.TornEdge(source, settings).Image;
        Assert.Equal((50, 40), (torn.Width, torn.Height));
        Assert.Equal(SKColors.Yellow, torn.GetPixel(25, 0));
        Assert.Equal(Row(39), torn.GetPixel(25, 39));
    }
}
