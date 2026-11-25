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


using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Languages;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Panels.ViewModels;
using Greenshot.Editor.Panels.Views;

namespace Greenshot.Editor.Panels.Sections
{
    /// <summary>
    /// The torn sides and the shadow of the selected torn edges. The teeth, the color of the torn off parts, the line and
    /// whether there is a shadow are properties of the element in the toolbar, like for the other elements.
    /// </summary>
    internal sealed class TornEdgesSection : IEditorSection
    {
        public const string SectionId = "Greenshot.TornEdges";

        private readonly IEditorConfiguration _configuration;

        public TornEdgesSection(IEditorConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Id => SectionId;

        public string Title => Texts.Editor.TornEdge;

        public int Order => EditorSectionOrder.Element;

        public bool ShowsPanel => false;

        public bool FillsHeight => false;

        public bool AppliesTo(IEditorPanelContext context) => context.CurrentSurface?.SelectedElements is { Count: 1 } selected && selected[0] is TornEdgeContainer;

        public object CreateViewModel(IEditorPanelContext context) => new TornEdgesSectionViewModel(context, _configuration);

        public object CreateView(object viewModel) => new TornEdgesSectionView { DataContext = viewModel };
    }
}
