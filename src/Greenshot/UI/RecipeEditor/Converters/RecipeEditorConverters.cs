using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Greenshot.UI.RecipeEditor.Converters
{
    public class StepTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stepType = value as string;
            switch (stepType)
            {
                case "Source":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0969da")); // Blue
                case "InteractiveSelection":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f883d")); // Green
                case "Border":
                case "Effect":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bf8700")); // Gold / Amber
                case "TextEffect":
                case "ObfuscateText":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cf222e")); // Red
                case "Drawable":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8250df")); // Purple
                case "SetVariable":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0550ae")); // Dark Blue
                case "ImmediateFeedback":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2da44e")); // Green
                case "Processors":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0969da")); // Cyan/Blue
                case "Destinations":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8250df")); // Purple
                case "Notification":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#57606a")); // Gray
                case "Conditional":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bc4c00")); // Orange
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
            switch (stepType)
            {
                case "Source": return "📷";
                case "InteractiveSelection": return "🔲";
                case "Border": return "⏹️";
                case "Effect": return "✨";
                case "TextEffect":
                case "ObfuscateText": return "🛡️";
                case "Drawable": return "🎨";
                case "SetVariable": return "💲";
                case "ImmediateFeedback": return "🔊";
                case "Processors": return "⚙️";
                case "Destinations": return "↗️";
                case "Notification": return "🔔";
                case "Conditional": return "🔀";
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
}
