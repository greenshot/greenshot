using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Greenshot.Plugin.RecipeEditor.Converters
{
    public class StepTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stepType = value as string;
            if (string.IsNullOrEmpty(stepType))
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#57606a"));
            }

            if (string.Equals(stepType, "ExternalCommand", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e36209")); // Amber / Rust
            }

            switch (stepType)
            {
                case "Source":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0969da")); // Blue
                case "InteractiveSelection":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f883d")); // Green
                case "Effect":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bf8700")); // Gold / Amber
                case "TextEffect":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cf222e")); // Red
                case "Annotation":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8250df")); // Purple
                case "SetVariable":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0550ae")); // Dark Blue
                case "ImmediateFeedback":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2da44e")); // Green
                case "Processors":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0969da")); // Cyan/Blue
                case "Destinations":
                case "DynamicDestination":
                case "SaveFile":
                case "Clipboard":
                case "Editor":
                case "Printer":
                case "Email":
                case "CustomDestination":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8250df")); // Purple
                case "Notification":
                case "Stdout":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#57606a")); // Gray
                case "Stderr":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cf222e")); // Red
                case "Conditional":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bc4c00")); // Orange
                case "Slot":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6e7781")); // Slate
                case "In":
                case "Out":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f6feb")); // Boundary blue
                case "UserPrompt":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a7f37")); // Forest Green
                case "Imgur":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2da44e")); // Imgur Green
                case "Jira":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0052cc")); // Jira Blue
                case "Confluence":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172b4d")); // Confluence Navy
                case "Office":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d83b01")); // Office Red/Orange
                case "BarcodeScan":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6f42c1")); // ZXing Violet
                case "Box":
                case "Dropbox":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0061ff")); // Cloud Blue
                default:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#57606a"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StepTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stepType = value as string;
            if (string.IsNullOrEmpty(stepType)) return "📦";

            if (string.Equals(stepType, "ExternalCommand", StringComparison.OrdinalIgnoreCase))
            {
                return "⚡";
            }

            switch (stepType)
            {
                case "Source": return "📷";
                case "InteractiveSelection": return "🔲";
                case "Effect": return "✨";
                case "TextEffect": return "🛡️";
                case "Annotation": return "🎨";
                case "SetVariable": return "💲";
                case "ImmediateFeedback": return "🔊";
                case "Processors": return "⚙️";
                case "Destinations": return "↗️";
                case "DynamicDestination": return "🎯";
                case "SaveFile": return "💾";
                case "Clipboard": return "📋";
                case "Editor": return "✏️";
                case "Printer": return "🖨️";
                case "Email": return "✉️";
                case "CustomDestination": return "🔌";
                case "Notification": return "🔔";
                case "Stdout": return "📤";
                case "Stderr": return "⛔";
                case "RecordVideo": return "🎥";
                case "Conditional": return "🔀";
                case "Slot": return "🧩";
                case "In": return "▶";
                case "Out": return "⏏";
                case "UserPrompt": return "❓";
                case "Imgur": return "🖼️";
                case "Jira": return "🎯";
                case "Confluence": return "📄";
                case "Office": return "📊";
                case "BarcodeScan": return "🔍";
                case "Box":
                case "Dropbox": return "📦";
                default: return "📦";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool flag && flag;
            if (Invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            string target = parameter as string;
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(target)) return Visibility.Collapsed;
            return string.Equals(s, target, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string colorStr = value as string;
            if (string.IsNullOrWhiteSpace(colorStr)) return Brushes.Transparent;

            try
            {
                if (string.Equals(colorStr, "transparent", StringComparison.OrdinalIgnoreCase))
                {
                    return Brushes.Transparent;
                }

                var converted = ColorConverter.ConvertFromString(colorStr);
                if (converted is Color col)
                {
                    return new SolidColorBrush(col);
                }
            }
            catch
            {
                // Fall back to transparent on parse failure
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToStatusBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush ActiveBrush = new SolidColorBrush(Color.FromRgb(0x23, 0x86, 0x36)); // Green
        private static readonly SolidColorBrush InactiveBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x90)); // Gray

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEnabled = value is bool b && b;
            return isEnabled ? ActiveBrush : InactiveBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
