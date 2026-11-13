/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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


using System.Collections.Generic;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Languages;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Panels.ViewModels;
using Greenshot.Editor.Panels.Views;

namespace Greenshot.Editor.Panels
{
    /// <summary>
    /// The panel with the properties of the selected elements (or of the tool which is picked), in sections
    /// </summary>
    internal sealed class SelectionPanel : IEditorPanel
    {
        public const string PanelId = "Greenshot.Selection";

        private readonly IReadOnlyList<IEditorSection> _sections;
        private readonly IEditorConfiguration _configuration;

        public SelectionPanel(IReadOnlyList<IEditorSection> sections, IEditorConfiguration configuration)
        {
            _sections = sections;
            _configuration = configuration;
        }

        public string Id => PanelId;

        public string Title => Texts.Editor.PanelSelection;

        public int Order => 0;

        /// <summary>
        /// Shown with "Properties" of an element, or when a choice is needed
        /// </summary>
        public string MenuText => null;

        public string Shortcut => null;

        public bool IsAvailable(IEditorPanelContext context) => true;

        public object CreateViewModel(IEditorPanelContext context) => new SelectionPanelViewModel(context, _sections, _configuration);

        public object CreateView(object viewModel) => new SelectionPanelView { DataContext = viewModel };
    }
}
