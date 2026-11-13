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
using System;
using Avalonia.Data;

namespace Greenshot.Editor.Standalone.Controls
{
    /// <summary>
    /// {l:Text Save} in the XAML: the text of the language pack with that key. It is a binding, so it follows when the
    /// language changes.
    /// </summary>
    public sealed class TextExtension
    {
        public TextExtension()
        {
        }

        public TextExtension(string key)
        {
            Key = key;
        }

        /// <summary>
        /// The name of the property of <see cref="IEditorLanguage"/>
        /// </summary>
        public string Key { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider) => new Binding(Key) { Source = EditorTexts.Text, Mode = BindingMode.OneWay };
    }
}
