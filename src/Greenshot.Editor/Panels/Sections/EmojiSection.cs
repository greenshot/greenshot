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


using System.Linq;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Languages;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Controls.Emoji;
using Greenshot.Editor.Panels.ViewModels;

namespace Greenshot.Editor.Panels.Sections
{
    /// <summary>
    /// The emoji picker: for the selected emoji, and for the Emoji tool (the emoji which is placed next).
    /// It opens the Selection panel by itself, without taking the keyboard.
    /// </summary>
    internal sealed class EmojiSection : IEditorSection
    {
        public const string SectionId = "Greenshot.Emoji";

        private readonly IEditorConfiguration _configuration;

        public EmojiSection(IEditorConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Id => SectionId;

        public string Title => Texts.Editor.SectionEmoji;

        public int Order => EditorSectionOrder.Element;

        public bool ShowsPanel => true;

        public bool AppliesTo(IEditorPanelContext context)
        {
            var surface = context.CurrentSurface;
            if (surface == null)
            {
                return false;
            }

            var selected = surface.SelectedElements;
            if (selected is { Count: > 0 })
            {
                return selected.All(element => element is IEmojiContainer);
            }

            return surface.DrawingMode == DrawingModes.Emoji;
        }

        public object CreateViewModel(IEditorPanelContext context) => new EmojiSectionViewModel(context, _configuration);

        public object CreateView(object viewModel) => new EmojiPicker { DataContext = ((EmojiSectionViewModel)viewModel).Picker };
    }
}
