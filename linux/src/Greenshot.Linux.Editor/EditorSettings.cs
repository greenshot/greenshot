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

using System.Globalization;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Editor.Effects;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using SkiaSharp;

namespace Greenshot.Linux.Editor;

/// <summary>
/// The [Editor] settings of greenshot.ini, with the key names of IEditorConfiguration.
/// The last used field values go to their own section [EditorLastUsedFields] as "RectangleContainer.LINE_COLOR=#FFFF0000",
/// so the Dapplo.Ini format of the Windows LastUsedFieldValues is never overwritten.
/// </summary>
public sealed class EditorSettings(CoreSettings settings)
{
    public const string SectionName = "Editor";
    public const string LastUsedSectionName = "EditorLastUsedFields";

    public int FreehandSensitivity
    {
        get => Math.Clamp(GetInt(nameof(FreehandSensitivity), 3), 1, 100);
        set => settings.SetValue(SectionName, nameof(FreehandSensitivity), value.ToString(CultureInfo.InvariantCulture));
    }

    public bool SuppressSaveDialogAtClose
    {
        get => bool.TryParse(settings.GetValue(SectionName, nameof(SuppressSaveDialogAtClose)), out var value) && value;
        set => settings.SetValue(SectionName, nameof(SuppressSaveDialogAtClose), value ? "True" : "False");
    }

    public bool MatchSizeToCapture
    {
        get => !bool.TryParse(settings.GetValue(SectionName, nameof(MatchSizeToCapture)), out var value) || value;
        set => settings.SetValue(SectionName, nameof(MatchSizeToCapture), value ? "True" : "False");
    }

    /// <summary>
    /// The emoji used last, new emoji elements start with it
    /// </summary>
    public string RecentEmoji
    {
        get => settings.GetValue(SectionName, nameof(RecentEmoji)) is { Length: > 0 } emoji ? emoji : Elements.EmojiElement.DefaultEmoji;
        set => settings.SetValue(SectionName, nameof(RecentEmoji), value);
    }

    /// <summary>
    /// The settings of the drop shadow effect (Ctrl+Q), DropShadowEffectSettings of Windows
    /// </summary>
    public DropShadowSettings DropShadowEffectSettings
    {
        get => DropShadowSettings.Parse(settings.GetValue(SectionName, nameof(DropShadowEffectSettings)));
        set => settings.SetValue(SectionName, nameof(DropShadowEffectSettings), value.ToString());
    }

    /// <summary>
    /// The settings of the torn edges (Ctrl+T), also for the edges of the crop, TornEdgeEffectSettings of Windows
    /// </summary>
    public TornEdgeSettings TornEdgeEffectSettings
    {
        get => TornEdgeSettings.Parse(settings.GetValue(SectionName, nameof(TornEdgeEffectSettings)));
        set => settings.SetValue(SectionName, nameof(TornEdgeEffectSettings), value.ToString());
    }

    /// <summary>
    /// The colors picked last, newest first, at most 12
    /// </summary>
    public IReadOnlyList<SKColor> RecentColors
    {
        get => (settings.GetValue(SectionName, nameof(RecentColors)) ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(text => SKColor.TryParse(text, out var color) ? color : (SKColor?)null)
            .OfType<SKColor>()
            .ToList();
        set => settings.SetValue(SectionName, nameof(RecentColors), string.Join(',', value.Take(12).Select(c => ElementStyle.Format(c))));
    }

    public void AddRecentColor(SKColor color)
    {
        var colors = RecentColors.Where(c => c != color).Prepend(color).ToList();
        RecentColors = colors;
    }

    /// <summary>
    /// The style for a new element: the defaults of its type with the last used values on top
    /// </summary>
    public ElementStyle StyleFor(DrawableElement element)
    {
        var style = element.CreateDefaultStyle();
        foreach (var field in element.Fields)
        {
            var text = settings.GetValue(LastUsedSectionName, Key(element, field));
            if (text != null && ElementStyle.Parse(field, text) is { } value)
            {
                style.Set(field, value);
            }
        }

        return style;
    }

    /// <summary>
    /// Remember a value the user chose, new elements of the type start with it
    /// </summary>
    public void RememberField(DrawableElement element, FieldType field, object value) =>
        settings.SetValue(LastUsedSectionName, Key(element, field), ElementStyle.Format(value));

    /// <summary>
    /// A last used value of something which is no element, e.g. the cut edges of the crop tool; null when there is none
    /// </summary>
    public object? GetLastUsed(string typeName, FieldType field) =>
        settings.GetValue(LastUsedSectionName, $"{typeName}.{field.WindowsName()}") is { } text ? ElementStyle.Parse(field, text) : null;

    public void RememberField(string typeName, FieldType field, object value) =>
        settings.SetValue(LastUsedSectionName, $"{typeName}.{field.WindowsName()}", ElementStyle.Format(value));

    public void Save() => settings.Save();

    private static string Key(DrawableElement element, FieldType field) => $"{element.WindowsTypeName}.{field.WindowsName()}";

    private int GetInt(string key, int defaultValue) =>
        int.TryParse(settings.GetValue(SectionName, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
}
