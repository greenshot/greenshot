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

using System.Drawing;
using System.Linq;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Panels;
using Greenshot.Plugin.Zxing.Drawing;
using Greenshot.Plugin.Zxing.ViewModels;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The barcode fields of the Barcode panel and section: the payload from the fields, insert and change as one undo step.
    /// Only the view models, no WPF.
    /// </summary>
    public class BarcodePanelTests
    {
        public BarcodePanelTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static (EditorPanelContext Context, int[] FocusCount) CreateContext(Surface surface, System.Collections.Generic.List<(string, bool)> shownPanels = null)
        {
            var focusCount = new int[1];
            var context = new EditorPanelContext(null, (id, activate) => shownPanels?.Add((id, activate)), () => focusCount[0]++);
            context.SetSurface(surface);
            return (context, focusCount);
        }

        private static BarcodeContainer AddBarcode(Surface surface, string text)
        {
            var barcode = new BarcodeContainer(surface, new ZxingModel { RawText = text }) { Left = 10, Top = 10, Width = 100, Height = 100 };
            surface.AddElement(barcode, false, false);
            return barcode;
        }

        [Fact]
        public void Fields_MakeThePayloadOfTheKindOfQrCode()
        {
            var viewModel = new BarcodeEditorViewModel(new ZxingModel());
            Assert.False(viewModel.IsValid);
            Assert.False(string.IsNullOrEmpty(viewModel.StatusText));

            viewModel.QrCategoryIndex = 1;
            viewModel.WifiSsid = "Home";
            viewModel.WifiPassword = "Secret";
            viewModel.WifiEncryptionIndex = 1;

            Assert.True(viewModel.ShowsWifi);
            Assert.False(viewModel.ShowsText);
            Assert.Equal("WIFI:S:Home;T:WEP;P:Secret;;", viewModel.Payload);
            Assert.True(viewModel.IsValid);
            Assert.NotNull(viewModel.Preview);
            Assert.Equal(string.Empty, viewModel.StatusText);
        }

        [Fact]
        public void Fields_ShowWhyAFormatCantEncodeTheValue()
        {
            var viewModel = new BarcodeEditorViewModel(new ZxingModel { RawText = "not digits" });
            // EAN-13 (1D): only digits
            viewModel.FormatIndex = 7;

            Assert.False(viewModel.IsQrCode);
            Assert.False(viewModel.Is2D);
            Assert.True(viewModel.ShowsText);
            Assert.False(viewModel.IsValid);
            Assert.True(viewModel.StatusIsError);
            Assert.Null(viewModel.Preview);
        }

        [Fact]
        public void Insert_AddsTheBarcodeInTheVisiblePartAsOneUndoStep_AndShowsItInTheSelectionPanel()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var shownPanels = new System.Collections.Generic.List<(string, bool)>();
            var (context, focusCount) = CreateContext(surface, shownPanels);
            using var panel = new BarcodePanelViewModel(context);
            Assert.True(panel.CanInsert);

            var barcode = panel.Insert();

            Assert.NotNull(barcode);
            Assert.Same(barcode, surface.Elements.Single());
            Assert.Same(barcode, surface.SelectedElements.Single());
            Assert.Equal("https://getgreenshot.org", barcode.Model.RawText);
            // A copy: the next change in the panel doesn't change the inserted barcode
            Assert.NotSame(panel.Editor.Model, barcode.Model);
            Assert.True(barcode.Left > 0 && barcode.Top > 0);
            // The Selection panel with the Barcode section, the keyboard stays on the canvas
            Assert.Equal(("Greenshot.Selection", false), shownPanels.Single());
            Assert.Equal(1, focusCount[0]);

            surface.Undo();
            Assert.Empty(surface.Elements);
            Assert.False(surface.CanUndo);
        }

        [Fact]
        public void Section_ChangesTheSelectedBarcode_TypingInOneFieldIsOneUndoStep()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var (context, _) = CreateContext(surface);
            var barcode = AddBarcode(surface, "a");
            surface.SelectElement(barcode);
            using var section = new BarcodeSectionViewModel(context);
            context.OnSelectionChanged();
            Assert.Equal("a", section.Editor.RawText);

            section.Editor.RawText = "ab";
            section.Editor.RawText = "abc";
            Assert.Equal("abc", barcode.Model.RawText);
            Assert.True(surface.Modified);

            section.Editor.Margin = 3;
            Assert.Equal(3, barcode.Margin);

            // The margin, then the whole text
            surface.Undo();
            Assert.Equal(1, barcode.Margin);
            Assert.Equal("abc", barcode.Model.RawText);
            surface.Undo();
            Assert.Equal("a", barcode.Model.RawText);
            Assert.False(surface.CanUndo);

            // The section follows the undo
            context.OnElementsChanged();
            Assert.Equal("a", section.Editor.RawText);

            surface.Redo();
            Assert.Equal("abc", barcode.Model.RawText);
        }

        [Fact]
        public void Section_DoesNotGiveTheBarcodeAValueItsFormatCantEncode()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var (context, _) = CreateContext(surface);
            var barcode = AddBarcode(surface, "text");
            surface.SelectElement(barcode);
            using var section = new BarcodeSectionViewModel(context);

            section.Editor.FormatIndex = 7;

            Assert.Equal(0, barcode.Model.FormatIndex);
            Assert.False(surface.CanUndo);
            Assert.True(section.Editor.StatusIsError);
        }

        [Fact]
        public void DoubleClick_OnABarcode_AsksForTheProperties()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var barcode = AddBarcode(surface, "text");
            int requests = 0;
            surface.PropertiesRequested += (_, _) => requests++;

            barcode.OnDoubleClick();

            Assert.Equal(1, requests);
        }
    }
}
