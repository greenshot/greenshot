/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Plugin.Confluence.Support;

/// <summary>
/// The Translate Markup extension returns a binding to a TranslationData
/// that provides a translated resource of the specified key
/// </summary>
public class TranslateExtension : MarkupExtension
{
    private string _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateExtension"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    public TranslateExtension(string key)
    {
        _key = key;
    }

    [ConstructorArgument("key")]
    public string Key
    {
        get { return _key; }
        set { _key = value; }
    }

    /// <summary>
    /// See <see cref="MarkupExtension.ProvideValue" />
    /// </summary>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding("Value")
        {
            Source = new TranslationData(_key)
        };
        return binding.ProvideValue(serviceProvider);
    }
}