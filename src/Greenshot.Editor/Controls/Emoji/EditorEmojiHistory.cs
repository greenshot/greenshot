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

using System.Collections.Generic;
using System.Linq;
using Greenshot.Base.Wpf.Emoji;
using Greenshot.Editor.Configuration;

namespace Greenshot.Editor.Controls.Emoji
{
    /// <summary>
    /// The emoji history of the editor, stored in greenshot.ini
    /// </summary>
    public sealed class EditorEmojiHistory : IEmojiHistory
    {
        private readonly IEditorConfiguration _configuration;

        public EditorEmojiHistory(IEditorConfiguration configuration) => _configuration = configuration;

        public IList<string> RecentEmojis
        {
            get => _configuration.RecentEmojis;
            set => _configuration.RecentEmojis = value?.ToList() ?? new List<string>();
        }
    }
}
