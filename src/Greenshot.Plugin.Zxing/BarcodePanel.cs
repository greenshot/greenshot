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
    /// The Barcode panel of the editor: the fields of a new barcode with a preview and Insert. Shown from the Plugins menu or the panel list.
    /// </summary>
    internal sealed class BarcodePanel : IEditorPanel
    {
        public const string PanelId = "Greenshot.Barcode";

        public string Id => PanelId;

        public string Title => Texts.Get<IZxingLanguage>().PanelTitle;

        // After Stamps (100)
        public int Order => 110;

        public string MenuText => Texts.Get<IZxingLanguage>().EditorMenu;

        public string Shortcut => null;

        public bool IsAvailable(IEditorPanelContext context) => true;

        public object CreateViewModel(IEditorPanelContext context) => new BarcodePanelViewModel(context);

        public object CreateView(object viewModel)
        {
            var panelViewModel = (BarcodePanelViewModel)viewModel;
            var view = new BarcodeEditorView { DataContext = panelViewModel.Editor };
            view.ShowInsert(panelViewModel);
            return view;
        }
    }
}
