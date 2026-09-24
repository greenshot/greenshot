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

            if (stepType.StartsWith("ExternalCommand", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stepType, "ExecuteCommand", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stepType, "RunCommand", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e36209")); // Amber / Rust
            }

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
                case "SaveFile":
                case "SaveToFile":
                case "Clipboard":
                case "Editor":
                case "Printer":
                case "Email":
                case "CustomDestination":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8250df")); // Purple
                case "Notification":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#57606a")); // Gray
                case "Conditional":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bc4c00")); // Orange
                case "UserPrompt":
                case "PromptChoice":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a7f37")); // Forest Green
                case "Imgur":
                case "ImgurUpload":
                case "UploadToImgur":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2da44e")); // Imgur Green
                case "Jira":
                case "JiraUpload":
                case "UploadToJira":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0052cc")); // Jira Blue
                case "Confluence":
                case "ConfluenceUpload":
                case "UploadToConfluence":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172b4d")); // Confluence Navy
                case "Office":
                case "Excel":
                case "PowerPoint":
                case "Powerpoint":
                case "Word":
                case "OneNote":
                case "Outlook":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d83b01")); // Office Red/Orange
                case "Zxing":
                case "ZxingQr":
                case "ZxingBarcode":
                case "BarcodeScan":
                case "DecodeBarcode":
                case "QrCode":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6f42c1")); // ZXing Violet
                case "Box":
                case "BoxUpload":
                case "UploadToBox":
                case "Dropbox":
                case "DropboxUpload":
                case "UploadToDropbox":
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

            if (stepType.StartsWith("ExternalCommand", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stepType, "ExecuteCommand", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stepType, "RunCommand", StringComparison.OrdinalIgnoreCase))
            {
                return "⚡";
            }

            switch (stepType)
            {
                case "Source": return "📷";
                case "InteractiveSelection": return "🔲";
                case "Border": return "⏹️";
                case "Effect": return "✨";
                case "TextEffect":
                case "ObfuscateText": return "🛡️";
                case "Annotation": return "🎨";
                case "SetVariable": return "💲";
                case "ImmediateFeedback": return "🔊";
                case "Processors": return "⚙️";
                case "Destinations": return "↗️";
                case "SaveFile":
                case "SaveToFile": return "💾";
                case "Clipboard": return "📋";
                case "Editor": return "✏️";
                case "Printer": return "🖨️";
                case "Email": return "✉️";
                case "CustomDestination": return "🔌";
                case "Notification": return "🔔";
                case "Conditional": return "🔀";
                case "UserPrompt":
                case "PromptChoice": return "❓";
                case "Imgur":
                case "ImgurUpload":
                case "UploadToImgur": return "🖼️";
                case "Jira":
                case "JiraUpload":
                case "UploadToJira": return "🎯";
                case "Confluence":
                case "ConfluenceUpload":
                case "UploadToConfluence": return "📄";
                case "Office":
                case "Excel":
                case "PowerPoint":
                case "Powerpoint":
                case "Word":
                case "OneNote":
                case "Outlook": return "📊";
                case "Zxing":
                case "ZxingQr":
                case "ZxingBarcode":
                case "BarcodeScan":
                case "DecodeBarcode":
                case "QrCode": return "🔍";
                case "Box":
                case "BoxUpload":
                case "UploadToBox":
                case "Dropbox":
                case "DropboxUpload":
                case "UploadToDropbox": return "📦";
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
