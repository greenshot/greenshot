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

using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Zxing.ViewModels;
using Greenshot.Plugin.Zxing.Views;

namespace Greenshot.Plugin.Zxing
{
    /// <summary>
    /// The Barcode section of the Selection panel, for one selected barcode: its fields, a change changes the barcode.
    /// It doesn't open the panel by itself (a barcode needs no choice before it's used); a double click on the barcode opens it.
    /// </summary>
    internal sealed class BarcodeSection : IEditorSection
    {
        public const string SectionId = "Greenshot.BarcodeSection";

        public string Id => SectionId;

        public string Title => Texts.Get<IZxingLanguage>().SectionTitle;

        public int Order => EditorSectionOrder.Element;

        public bool ShowsPanel => false;

        public bool FillsHeight => false;

        public bool AppliesTo(IEditorPanelContext context) => BarcodeSectionViewModel.SelectedBarcode(context) != null;

        public object CreateViewModel(IEditorPanelContext context) => new BarcodeSectionViewModel(context);

        public object CreateView(object viewModel) => new BarcodeEditorView { DataContext = ((BarcodeSectionViewModel)viewModel).Editor };
    }
}
