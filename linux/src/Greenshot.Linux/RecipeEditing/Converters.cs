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
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace Greenshot.Linux.RecipeEditing;

/// <summary>
/// The color of the header of a step card, by the kind of step
/// </summary>
public sealed class StepTypeToColorConverter : IValueConverter
{
    public static readonly StepTypeToColorConverter Instance = new();

    public static IBrush For(string? stepType) => new SolidColorBrush(Color.Parse(stepType switch
    {
        "ExternalCommand" => "#e36209",
        "Source" or "Ocr" => "#0969da",
        "InteractiveSelection" => "#1f883d",
        "Effect" => "#bf8700",
        "TextEffect" or "Stderr" => "#cf222e",
        "Annotation" or "Destinations" or "DynamicDestination" or "SaveFile" or "Clipboard" or "Editor" or "Printer" or "Email" or "CustomDestination" => "#8250df",
        "SetVariable" => "#0550ae",
        "ImmediateFeedback" => "#2da44e",
        "Conditional" => "#bc4c00",
        "Slot" => "#6e7781",
        "In" or "Out" => "#1f6feb",
        "UserPrompt" => "#1a7f37",
        "Jira" => "#0052cc",
        "Confluence" => "#172b4d",
        "BarcodeScan" => "#6f42c1",
        "Box" or "Dropbox" => "#0061ff",
        _ => "#57606a",
    }));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => For(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// The icon of a step, before its title
/// </summary>
public sealed class StepTypeToIconConverter : IValueConverter
{
    public static readonly StepTypeToIconConverter Instance = new();

    public static string For(string? stepType) => stepType switch
    {
        "ExternalCommand" => "⚡",
        "Source" => "📷",
        "InteractiveSelection" => "🔲",
        "Effect" => "✨",
        "TextEffect" => "🛡️",
        "Annotation" => "🎨",
        "SetVariable" => "💲",
        "ImmediateFeedback" => "🔊",
        "Ocr" => "🔤",
        "Destinations" => "↗️",
        "DynamicDestination" or "Jira" => "🎯",
        "SaveFile" => "💾",
        "Clipboard" => "📋",
        "Editor" => "✏️",
        "Printer" => "🖨️",
        "Email" => "✉️",
        "CustomDestination" => "🔌",
        "Notification" => "🔔",
        "Stdout" => "📤",
        "Stderr" => "⛔",
        "RecordVideo" => "🎥",
        "Conditional" => "🔀",
        "Slot" => "🧩",
        "In" => "▶",
        "Out" => "⏏",
        "UserPrompt" => "❓",
        "Confluence" => "📄",
        "BarcodeScan" => "🔍",
        _ => "📦",
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => For(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// True when the text is the parameter (case-insensitive), for the parts of the inspector of one kind of step
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    public static readonly StringEqualsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && parameter is string p && p.Split('|').Any(option => string.Equals(s, option, StringComparison.OrdinalIgnoreCase));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// "#RRGGBB", "#AARRGGBB", a color name or "transparent" as brush
/// </summary>
public sealed class StringToBrushConverter : IValueConverter
{
    public static readonly StringToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && !string.IsNullOrWhiteSpace(s) && Color.TryParse(s.Trim(), out var color) ? new SolidColorBrush(color) : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Green for active, grey for switched off; readable on the light and the dark background
/// </summary>
public sealed class BoolToStatusBrushConverter : IValueConverter
{
    public static readonly BoolToStatusBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool dark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        return value is true
            ? new SolidColorBrush(Color.Parse(dark ? "#6CCB5F" : "#0F7B0F"))
            : new SolidColorBrush(Color.Parse(dark ? "#AFAFAF" : "#646464"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// True when the text is not empty
/// </summary>
public sealed class HasTextConverter : IValueConverter
{
    public static readonly HasTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is string s && !string.IsNullOrWhiteSpace(s);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
