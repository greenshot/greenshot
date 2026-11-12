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


using System;
using System.Collections.Generic;
using System.Linq;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Controls.Emoji;
using Greenshot.Editor.Memento;

namespace Greenshot.Editor.Panels.ViewModels
{
    /// <summary>
    /// The emoji section of the Selection panel: a pick changes the selected emoji (one undo step) and is the emoji the Emoji tool
    /// places next. No UI toolkit here.
    /// </summary>
    internal sealed class EmojiSectionViewModel : IDisposable
    {
        private readonly IEditorPanelContext _context;
        private readonly IEditorConfiguration _configuration;

        public EmojiSectionViewModel(IEditorPanelContext context, IEditorConfiguration configuration, IReadOnlyList<Emojis.Group> categories = null)
        {
            _context = context;
            _configuration = configuration;
            Picker = new EmojiPickerViewModel(configuration, categories);
            Picker.Picked += OnPicked;
            UpdateCurrentEmoji();
            _context.SelectionChanged += OnSelectionChanged;
            _context.CurrentSurfaceChanged += OnSelectionChanged;
        }

        /// <summary>
        /// The picker, the view shows it
        /// </summary>
        public EmojiPickerViewModel Picker { get; }

        private IEnumerable<IEmojiContainer> SelectedEmojis()
            => _context.CurrentSurface?.SelectedElements?.OfType<IEmojiContainer>() ?? Enumerable.Empty<IEmojiContainer>();

        /// <summary>
        /// The emoji of the selected emoji element, else the one the Emoji tool places next
        /// </summary>
        private void UpdateCurrentEmoji() => Picker.CurrentEmoji = SelectedEmojis().Select(element => element.Emoji).FirstOrDefault() ?? _configuration.RecentEmoji;

        private void OnPicked(object sender, EmojiPickedEventArgs e)
        {
            // The Emoji tool places it next
            _configuration.RecentEmoji = e.Emoji;
            var targets = SelectedEmojis().Where(element => element.Emoji != e.Emoji).ToList();
            var surface = _context.CurrentSurface;
            if (surface != null && targets.Count > 0)
            {
                surface.MakeUndoable(new EmojiChangeMemento(targets), false);
                foreach (var target in targets)
                {
                    target.Invalidate();
                    target.Emoji = e.Emoji;
                    target.Invalidate();
                }

                surface.Modified = true;
            }

            // A mouse user goes on with the canvas
            if (!e.FromKeyboard)
            {
                _context.FocusCanvas();
            }
        }

        private void OnSelectionChanged(object sender, EventArgs e) => UpdateCurrentEmoji();

        public void Dispose()
        {
            Picker.Picked -= OnPicked;
            _context.SelectionChanged -= OnSelectionChanged;
            _context.CurrentSurfaceChanged -= OnSelectionChanged;
        }
    }
}
