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

using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Editor;

public class EditorTests
{
    private static SKBitmap White(int width = 200, int height = 100)
    {
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        bitmap.Erase(SKColors.White);
        return bitmap;
    }

    private static (EditorSurface Surface, EditorController Controller) Create(int width = 200, int height = 100)
    {
        var surface = new EditorSurface(White(width, height));
        return (surface, new EditorController(surface));
    }

    private static void Drag(EditorController controller, float x1, float y1, float x2, float y2, InputModifiers modifiers = InputModifiers.None)
    {
        controller.PointerPressed(new SKPoint(x1, y1), modifiers);
        controller.PointerMoved(new SKPoint((x1 + x2) / 2, (y1 + y2) / 2), modifiers);
        controller.PointerMoved(new SKPoint(x2, y2), modifiers);
        controller.PointerReleased(new SKPoint(x2, y2), modifiers);
    }

    [Fact]
    public void Drawing_a_rectangle_creates_and_selects_it()
    {
        var (surface, controller) = Create();
        controller.Tool = EditorTool.Rectangle;
        Drag(controller, 60, 50, 10, 20);

        var rectangle = Assert.IsType<RectangleElement>(Assert.Single(surface.Elements));
        Assert.Equal(new SKRect(10, 20, 60, 50), rectangle.Bounds);
        Assert.Same(rectangle, Assert.Single(surface.Selection));
        Assert.Equal(SKColors.Red, rectangle.Style.LineColor);
        Assert.True(surface.CanUndo);
    }

    [Fact]
    public void A_click_with_the_rectangle_tool_creates_nothing_and_leaves_no_undo_step()
    {
        var (surface, controller) = Create();
        controller.Tool = EditorTool.Rectangle;
        Drag(controller, 10, 10, 11, 11);
        Assert.Empty(surface.Elements);
        Assert.False(surface.CanUndo);
    }

    [Fact]
    public void A_click_with_the_text_tool_creates_the_default_box_and_asks_for_the_text()
    {
        var (surface, controller) = Create();
        DrawableElement? edited = null;
        controller.TextEditRequested += element => edited = element;
        controller.Tool = EditorTool.Text;
        Drag(controller, 10, 10, 10, 10);

        var text = Assert.IsType<TextElement>(Assert.Single(surface.Elements));
        Assert.Same(text, edited);
        Assert.Equal(new SKRect(10, 10, 160, 50), text.Bounds);

        // An empty new text box is removed again, without an undo step
        controller.CommitText(text, string.Empty);
        Assert.Empty(surface.Elements);
        Assert.False(surface.CanUndo);
    }

    [Fact]
    public void Undo_and_redo_restore_the_elements()
    {
        var (surface, controller) = Create();
        controller.Tool = EditorTool.Ellipse;
        Drag(controller, 10, 10, 50, 50);
        controller.Tool = EditorTool.Select;
        // Grab the outline at 45 degrees, away from the handles
        Drag(controller, 44, 16, 54, 26);

        var moved = Assert.Single(surface.Elements).Bounds;
        Assert.Equal(new SKRect(20, 20, 60, 60), moved);

        surface.Undo();
        Assert.Equal(new SKRect(10, 10, 50, 50), Assert.Single(surface.Elements).Bounds);
        surface.Undo();
        Assert.Empty(surface.Elements);
        surface.Redo();
        surface.Redo();
        Assert.Equal(moved, Assert.Single(surface.Elements).Bounds);
        Assert.False(surface.CanRedo);
    }

    [Fact]
    public void Handles_resize_the_selected_element()
    {
        var (surface, controller) = Create();
        controller.Tool = EditorTool.Rectangle;
        Drag(controller, 10, 10, 50, 50);
        Drag(controller, 50, 50, 80, 70);
        Assert.Equal(new SKRect(10, 10, 80, 70), Assert.Single(surface.Elements).Bounds);
    }

    [Fact]
    public void Shift_makes_lines_snap_to_15_degrees()
    {
        var (surface, controller) = Create();
        controller.Tool = EditorTool.Line;
        Drag(controller, 10, 10, 110, 14, InputModifiers.Shift);
        var line = Assert.IsType<LineElement>(Assert.Single(surface.Elements));
        Assert.Equal(10, line.End.Y, 3);
    }

    [Fact]
    public void Step_labels_are_numbered_in_order_from_the_counter_start()
    {
        var (surface, controller) = Create();
        controller.Tool = EditorTool.StepLabel;
        Drag(controller, 20, 20, 20, 20);
        Drag(controller, 60, 20, 60, 20);
        var labels = surface.Elements.OfType<StepLabelElement>().ToList();
        Assert.Equal([1, 2], labels.Select(surface.StepNumber));
        surface.CounterStart = 5;
        Assert.Equal(6, surface.StepNumber(labels[1]));
        Assert.Equal(new SKRect(5, 5, 35, 35), labels[0].Bounds);
    }

    [Fact]
    public void Rubber_band_selects_the_elements_it_touches()
    {
        var (surface, controller) = Create();
        controller.Tool = EditorTool.Rectangle;
        Drag(controller, 10, 10, 30, 30);
        Drag(controller, 100, 10, 130, 30);
        controller.Tool = EditorTool.Select;
        Drag(controller, 5, 5, 40, 40);
        Assert.IsType<RectangleElement>(Assert.Single(surface.Selection));
        controller.SelectAll();
        Assert.Equal(2, surface.Selection.Count);
    }

    [Fact]
    public void Changing_a_field_applies_to_the_selection_and_is_remembered()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gs-editor-" + Guid.NewGuid().ToString("N"));
        try
        {
            var core = CoreSettings.Load(Path.Combine(folder, "greenshot.ini"));
            var settings = new EditorSettings(core);
            var surface = new EditorSurface(White());
            var controller = new EditorController(surface, settings);
            controller.Tool = EditorTool.Rectangle;
            Drag(controller, 10, 10, 50, 50);
            controller.SetFieldValue(FieldType.LineColor, SKColors.Blue);

            Assert.Equal(SKColors.Blue, surface.Elements[0].Style.LineColor);
            Assert.Equal("#FF0000FF", core.GetValue(EditorSettings.LastUsedSectionName, "RectangleContainer.LINE_COLOR"));

            // A new editor starts with the remembered color
            var next = new EditorController(new EditorSurface(White()), new EditorSettings(core));
            Assert.Equal(SKColors.Blue, next.ToolStyle(EditorTool.Rectangle).LineColor);
            Assert.Equal(SKColors.Red, next.ToolStyle(EditorTool.Ellipse).LineColor);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }

    [Fact]
    public void Visible_fields_follow_the_tool_and_the_selection()
    {
        var (_, controller) = Create();
        controller.Tool = EditorTool.Arrow;
        Assert.Contains(FieldType.ArrowHeads, controller.VisibleFields);
        controller.Tool = EditorTool.Obfuscate;
        Assert.Contains(FieldType.PixelSize, controller.VisibleFields);
        controller.SetFieldValue(FieldType.ObfuscateFilter, ObfuscateFilter.Blur);
        Assert.Contains(FieldType.BlurRadius, controller.VisibleFields);
        Assert.DoesNotContain(FieldType.PixelSize, controller.VisibleFields);
    }

    [Fact]
    public void Copy_and_paste_offsets_each_copy()
    {
        var (surface, controller) = Create();
        controller.Tool = EditorTool.Rectangle;
        Drag(controller, 10, 10, 30, 30);
        var json = controller.CopySelection()!;
        controller.PasteElements(json);
        controller.PasteElements(json);
        Assert.Equal([10f, 20f, 30f], surface.Elements.Select(e => e.Bounds.Left));
    }

    [Fact]
    public void Crop_moves_the_elements()
    {
        var (surface, controller) = Create();
        controller.Tool = EditorTool.Rectangle;
        Drag(controller, 50, 40, 70, 60);
        controller.Tool = EditorTool.Crop;
        Drag(controller, 40, 30, 120, 90);
        Assert.True(controller.ConfirmCrop());

        Assert.Equal(80, surface.Width);
        Assert.Equal(60, surface.Height);
        Assert.Equal(new SKRect(10, 10, 30, 30), surface.Elements[0].Bounds);
        surface.Undo();
        Assert.Equal(200, surface.Width);
    }

    [Fact]
    public void Arrange_changes_the_order()
    {
        var (surface, controller) = Create();
        controller.Tool = EditorTool.Rectangle;
        Drag(controller, 10, 10, 30, 30);
        controller.Tool = EditorTool.Ellipse;
        Drag(controller, 10, 10, 30, 30);
        controller.Tool = EditorTool.Select;
        surface.Select(surface.Elements[1]);
        surface.Arrange(EditorSurface.ZOrder.Bottom);
        Assert.IsType<EllipseElement>(surface.Elements[0]);
    }
}
