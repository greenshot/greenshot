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
using Greenshot.Editor.Panels.ViewModels;
using Greenshot.Editor.Panels.Views;

namespace Greenshot.Editor.Panels.Sections
{
    /// <summary>
    /// Copy and apply the style or the size of elements, for every selection
    /// </summary>
    internal sealed class StyleAndSizeSection : IEditorSection
    {
        public const string SectionId = "Greenshot.StyleAndSize";

        public string Id => SectionId;

        public string Title => Texts.Editor.SectionStyleAndSize;

        public int Order => EditorSectionOrder.StyleAndSize;

        public bool ShowsPanel => false;

        public bool FillsHeight => false;

        public bool AppliesTo(IEditorPanelContext context) => context.CurrentSurface?.SelectedElements?.Count > 0;

        public object CreateViewModel(IEditorPanelContext context) => new StyleAndSizeViewModel(context);

        public object CreateView(object viewModel) => new StyleAndSizeView { DataContext = viewModel };
    }
}
