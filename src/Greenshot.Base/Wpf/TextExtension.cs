/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Windows.Data;
using System.Windows.Markup;
using Greenshot.Base.Languages;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// A text of a language section in XAML: <c>{wpf:Text Editor.Undo}</c> binds to the property Undo of the [Editor] section
    /// (plugins: <c>{wpf:Text Imgur.History}</c>). The section notifies changed texts, so a language switch updates the view.
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public class TextExtension : MarkupExtension
    {
        public TextExtension()
        {
        }

        /// <param name="path">Section.Property, e.g. Settings.Title</param>
        public TextExtension(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Section.Property, e.g. Settings.Title
        /// </summary>
        [ConstructorArgument("path")]
        public string Path { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            int dot = Path?.IndexOf('.') ?? -1;
            if (dot <= 0 || dot == Path.Length - 1)
            {
                return Path;
            }

            object section;
            try
            {
                section = Texts.Config.GetSection(Path.Substring(0, dot));
            }
            catch (Exception)
            {
                // Design time or the plugin's section isn't registered (yet)
                return Path;
            }

            if (section == null)
            {
                return Path;
            }

            var binding = new Binding(Path.Substring(dot + 1))
            {
                Source = section,
                Mode = BindingMode.OneWay,
                FallbackValue = Path
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
