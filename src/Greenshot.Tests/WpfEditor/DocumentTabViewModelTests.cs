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
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.WpfEditor.Tools;
using Greenshot.WpfEditor.ViewModels;
using Xunit;
using static Greenshot.Tests.WpfEditor.WpfEditorTestHelper;

namespace Greenshot.Tests.WpfEditor
{
    [Collection(TestCollections.WpfEditorSession)]
    public class DocumentTabViewModelTests
    {
        private static Element Only<T>(DocumentTabViewModel tab) where T : Element =>
            tab.Committed.Layers.SelectMany(layer => layer.Elements).OfType<T>().Single();

        [Fact]
        public void RectangleTool_DragAddsARectangle_UndoRemovesIt_RedoBringsItBack()
        {
            RunOnSta(() =>
            {
                using var tab = new DocumentTabViewModel(NewDocument(), "test") { ToolKind = ToolKind.Rectangle };
                Drag(tab, 10, 20, 110, 70);

                var rectangle = (RectangleElement)Only<RectangleElement>(tab);
                Assert.Equal(new RectD(10, 20, 100, 50), rectangle.Area);
                Assert.Equal(new[] { rectangle.Id }, tab.Selection);
                Assert.True(tab.IsModified);
                Assert.True(tab.CanUndo);

                tab.Undo();
                Assert.Empty(tab.Committed.Layers.SelectMany(layer => layer.Elements).OfType<RectangleElement>());
                Assert.Empty(tab.Selection);

                tab.Redo();
                Assert.Equal(rectangle.Id, Only<RectangleElement>(tab).Id);
            });
        }

        [Fact]
        public void SelectTool_DragMovesTheElement_ArrowKeysNudgeIt()
        {
            RunOnSta(() =>
            {
                var filled = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 40, 40)) { Style = new Style { Fill = FillStyle.Of(new Rgba(0, 0, 255, 255)) } };
                using var tab = new DocumentTabViewModel(DocumentEdits.Add(NewDocument(), new[] { filled }), "test");
                Drag(tab, 30, 30, 60, 50);

                Assert.Equal(new RectD(40, 30, 40, 40), ((RectangleElement)Only<RectangleElement>(tab)).Area);
                Assert.Equal(new[] { filled.Id }, tab.Selection);

                Assert.True(tab.Key(ToolKey.Right, true, false));
                Assert.Equal(new RectD(50, 30, 40, 40), ((RectangleElement)Only<RectangleElement>(tab)).Area);
            });
        }

        [Fact]
        public void SelectTool_RubberBandSelectsTheElementsInside()
        {
            RunOnSta(() =>
            {
                var inside = new EllipseElement(Guid.NewGuid(), new RectD(20, 20, 20, 20));
                var outside = new EllipseElement(Guid.NewGuid(), new RectD(200, 200, 20, 20));
                using var tab = new DocumentTabViewModel(DocumentEdits.Add(NewDocument(), new Element[] { inside, outside }), "test");
                Drag(tab, 5, 5, 100, 100);

                Assert.Equal(new[] { inside.Id }, tab.Selection);
                Assert.False(tab.CanUndo);
            });
        }

        [Fact]
        public void MergedChanges_AreOneUndoStep()
        {
            RunOnSta(() =>
            {
                var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 40, 40));
                using var tab = new DocumentTabViewModel(DocumentEdits.Add(NewDocument(), new[] { rectangle }), "test");
                tab.Select(new[] { rectangle.Id });
                for (int width = 3; width <= 8; width++)
                {
                    int lineWidth = width;
                    tab.ChangeSelected(element => element with { Style = element.Style with { Stroke = StrokeStyle.Of(new Rgba(0, 0, 0, 255), lineWidth) } }, "Style", "stroke.width");
                }

                Assert.Equal(8, tab.Committed.StyleOf(Only<RectangleElement>(tab)).Stroke.Width);
                tab.Undo();
                Assert.False(tab.CanUndo);
                Assert.Equal(2, tab.Committed.StyleOf(Only<RectangleElement>(tab)).Stroke.Width);
            });
        }

        [Fact]
        public void NewText_LeftEmpty_IsRemovedWithItsUndoStep()
        {
            RunOnSta(() =>
            {
                using var tab = new DocumentTabViewModel(NewDocument(), "test") { ToolKind = ToolKind.Text };
                Guid? editing = null;
                tab.TextEditRequested += id => editing = id;
                tab.Tool.Down(At(50, 50));
                tab.Tool.Up(At(50, 50));

                Assert.NotNull(editing);
                tab.EndTextEdit(string.Empty, true);

                Assert.Empty(tab.Committed.Layers.SelectMany(layer => layer.Elements).OfType<TextElement>());
                Assert.False(tab.CanUndo);
            });
        }

        [Fact]
        public void NewText_Typed_IsOneUndoStepWithTheText()
        {
            RunOnSta(() =>
            {
                using var tab = new DocumentTabViewModel(NewDocument(), "test") { ToolKind = ToolKind.Text };
                tab.Tool.Down(At(50, 50));
                tab.Tool.Up(At(50, 50));
                // While typing, the element doesn't show its text: the text box over it does
                Assert.NotNull(tab.EditingText);
                tab.EndTextEdit("Hello", true);

                Assert.Equal("Hello", ((TextElement)Only<TextElement>(tab)).Text);
                tab.Undo();
                Assert.False(tab.CanUndo);
                Assert.Empty(tab.Committed.Layers.SelectMany(layer => layer.Elements).OfType<TextElement>());
            });
        }

        [Fact]
        public void DuplicateAndPaste_GiveNewElementsNextToTheOriginal()
        {
            RunOnSta(() =>
            {
                var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 40, 40));
                using var tab = new DocumentTabViewModel(DocumentEdits.Add(NewDocument(), new[] { rectangle }), "test");
                tab.Select(new[] { rectangle.Id });

                tab.DuplicateSelection();
                var rectangles = tab.Committed.Layers.SelectMany(layer => layer.Elements).OfType<RectangleElement>().ToList();
                Assert.Equal(2, rectangles.Count);
                var copy = rectangles.Single(element => element.Id != rectangle.Id);
                Assert.Equal(new RectD(20, 20, 40, 40), copy.Area);
                Assert.Equal(new[] { copy.Id }, tab.Selection);

                Assert.True(tab.CopySelection());
                tab.PasteElements();
                Assert.Equal(3, tab.Committed.Layers.SelectMany(layer => layer.Elements).OfType<RectangleElement>().Count());
            });
        }

        [Fact]
        public void Delete_RemovesTheSelection()
        {
            RunOnSta(() =>
            {
                var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 40, 40));
                using var tab = new DocumentTabViewModel(DocumentEdits.Add(NewDocument(), new[] { rectangle }), "test");
                tab.Select(new[] { rectangle.Id });

                tab.DeleteSelection();

                Assert.Null(DocumentEdits.Find(tab.Committed, rectangle.Id));
                Assert.Empty(tab.Selection);
            });
        }

        [Fact]
        public void CropTool_EnterAppliesTheCrop()
        {
            RunOnSta(() =>
            {
                using var tab = new DocumentTabViewModel(NewDocument(), "test") { ToolKind = ToolKind.Crop };
                Drag(tab, 50, 40, 250, 140);

                Assert.True(tab.Key(ToolKey.Enter, false, false));
                Assert.Equal(new RectD(0, 0, 200, 100), tab.Committed.CanvasArea());
                Assert.True(tab.CanUndo);
            });
        }

        [Fact]
        public void Turn_TurnsTheCanvas()
        {
            RunOnSta(() =>
            {
                using var tab = new DocumentTabViewModel(NewDocument(), "test");

                tab.Turn(90);

                var area = tab.Committed.CanvasArea();
                Assert.Equal(300, area.Width);
                Assert.Equal(400, area.Height);
            });
        }

        [Fact]
        public void AMovedElement_KeepsItsBitmap_OnlyItsPlaceChanges()
        {
            RunOnSta(() =>
            {
                var filled = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 40, 40)) { Style = new Style { Fill = FillStyle.Of(new Rgba(0, 0, 255, 255)) } };
                using var tab = new DocumentTabViewModel(DocumentEdits.Add(NewDocument(), new[] { filled }), "test");
                var before = tab.Composition.Pieces.Single(piece => piece.Id == filled.Id);

                tab.Tool.Down(At(30, 30));
                tab.Tool.Move(At(130, 80));
                var dragged = tab.Composition.Pieces.Single(piece => piece.Id == filled.Id);
                tab.Tool.Up(At(130, 80));
                var dropped = tab.Composition.Pieces.Single(piece => piece.Id == filled.Id);

                Assert.Same(before.Bitmap, dragged.Bitmap);
                Assert.Equal(before.Area.Offset(100, 50), dragged.Area);
                Assert.Same(before.Bitmap, dropped.Bitmap);
            });
        }

        [Fact]
        public void AnObfuscation_IsDrawnAgain_OnlyWhenSomethingBelowItChanges()
        {
            RunOnSta(() =>
            {
                var below = new RectangleElement(Guid.NewGuid(), new RectD(20, 20, 40, 40)) { Style = new Style { Fill = FillStyle.Of(new Rgba(0, 0, 255, 255)) } };
                var obfuscate = new ObfuscateElement(Guid.NewGuid(), new RectD(10, 10, 80, 80), ObfuscateMode.Pixelate, 10);
                var above = new RectangleElement(Guid.NewGuid(), new RectD(150, 150, 40, 40));
                using var tab = new DocumentTabViewModel(DocumentEdits.Add(NewDocument(), new Element[] { below, obfuscate, above }), "test");
                // A drawing again is a new piece (the bitmap itself is kept and written again)
                Greenshot.WpfEditor.Rendering.CompositionPiece Obfuscation() => tab.Composition.Pieces.Single(piece => piece.Id == obfuscate.Id);
                var first = Obfuscation();

                tab.Preview(DocumentEdits.Replace(tab.Committed, above with { Area = new RectD(160, 150, 40, 40) }));
                Assert.Same(first, Obfuscation());

                tab.Preview(DocumentEdits.Replace(tab.Committed, below with { Area = new RectD(30, 20, 40, 40) }));
                Assert.NotSame(first, Obfuscation());
            });
        }
    }
}
