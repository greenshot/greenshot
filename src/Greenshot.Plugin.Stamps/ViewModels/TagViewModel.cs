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

using Greenshot.Base.Languages;

namespace Greenshot.Plugin.Stamps.ViewModels;

/// <summary>
/// A tag shown as pill: of the filter, or of the selected stamp
/// </summary>
public sealed class TagViewModel
{
    internal TagViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }

    /// <summary>
    /// What a screen reader says for the "x" of the pill
    /// </summary>
    public string RemoveName => string.Format(Texts.Get<IStampsLanguage>().RemoveTag, Name);

    public override string ToString() => Name;
}
