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
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// A font size which grows with the "Text size" of Windows: <c>FontSize="{wpf:FontSize 12}"</c>, also as the value of a Setter.
    /// A plain number would stay the same, WPF on .NET Framework doesn't scale it.
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public class FontSizeExtension : MarkupExtension
    {
        public FontSizeExtension()
        {
        }

        /// <param name="size">The size at 100% text size, in device independent pixels</param>
        public FontSizeExtension(double size)
        {
            Size = size;
        }

        /// <summary>
        /// The size at 100% text size, in device independent pixels
        /// </summary>
        [ConstructorArgument("size")]
        public double Size { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return CreateBinding(Size).ProvideValue(serviceProvider);
        }

        /// <summary>
        /// The binding of a font size set in code: <c>element.SetBinding(TextElement.FontSizeProperty, FontSizeExtension.CreateBinding(12))</c>
        /// </summary>
        public static Binding CreateBinding(double size)
        {
            return new Binding(nameof(ThemeManager.TextScale))
            {
                Source = ThemeManager.Instance,
                Mode = BindingMode.OneWay,
                Converter = ScaleConverter.Instance,
                ConverterParameter = size,
                FallbackValue = size
            };
        }

        /// <summary>
        /// The size for text which is drawn, not shown in an element (e.g. FormattedText)
        /// </summary>
        public static double Scale(double size) => size * ThemeManager.Instance.TextScale;

        private sealed class ScaleConverter : IValueConverter
        {
            public static readonly ScaleConverter Instance = new ScaleConverter();

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                double scale = value is double factor ? factor : 1;
                return (double)parameter * scale;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
        }
    }
}
