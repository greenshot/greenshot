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

using System.Text.RegularExpressions;
using Avalonia.Markup.Xaml;
using Greenshot.Linux.Core.Languages;

namespace Greenshot.Linux.RecipeEditing;

/// <summary>
/// A text of the recipe editor in XAML, the wpf:Text of Windows: {r:Text RecipeEditor.delay_ms}. Unlike l:Text the
/// text is kept as it is ("Delay (ms)"), only the access key is removed.
/// </summary>
public sealed partial class TextExtension : MarkupExtension
{
    public TextExtension()
    {
    }

    public TextExtension(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Section.key, e.g. RecipeEditor.delay_ms
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        int dot = Key.IndexOf('.');
        return dot <= 0 ? Key : WithoutAccessKey(Texts.Get(Key[..dot], Key[(dot + 1)..]));
    }

    /// <summary>
    /// "&amp;Save" is an access key, "Recipe &amp; Triggers" is not
    /// </summary>
    internal static string WithoutAccessKey(string text) => AccessKeyRegex().Replace(text, string.Empty);

    [GeneratedRegex(@"&(?=\S)")]
    private static partial Regex AccessKeyRegex();
}
