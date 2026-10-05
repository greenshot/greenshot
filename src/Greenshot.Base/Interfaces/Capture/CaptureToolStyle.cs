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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Greenshot.Base.Wpf;

namespace Greenshot.Base.Interfaces.Capture
{
    /// <summary>
    /// The look of Greenshot for capture tools and overlays: the colors of the current (light or dark) theme, the font,
    /// and helpers to draw text, panels and key caps like the rest of Greenshot's WPF UI does.
    /// Everything is in pixels of the capture: one unit is one pixel, also on high DPI screens.
    /// </summary>
    public sealed class CaptureToolStyle
    {
        private readonly double _dpiScale;
        private readonly Typeface _typeface;
        private readonly Typeface _boldTypeface;
        private readonly Pen _panelBorder;
        private readonly Pen _keyCapBorder;

        /// <param name="dpiScale">The scale of the monitor the capture window was created on, 1.0 for 96 DPI</param>
        public CaptureToolStyle(double dpiScale)
        {
            _dpiScale = dpiScale <= 0 ? 1 : dpiScale;
            var palette = ThemeManager.Instance.CurrentPalette;
            PanelBackground = Frozen(palette.CardBackground);
            PanelBorder = Frozen(palette.CardBorder);
            Foreground = Frozen(palette.TextPrimary);
            MutedForeground = Frozen(palette.TextSecondary);
            Accent = Frozen(palette.Accent);
            KeyCapBackground = Frozen(palette.ControlBackgroundBrush);
            KeyCapBorder = Frozen(palette.BorderBrush);

            FontFamily = new FontFamily("Segoe UI");
            _typeface = new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _boldTypeface = new Typeface(FontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
            _panelBorder = Frozen(new Pen(PanelBorder, 1));
            _keyCapBorder = Frozen(new Pen(KeyCapBorder, 1));
        }

        /// <summary>
        /// Resource keys of the theme brushes for WPF content of panels, e.g. Foreground="{DynamicResource CaptureTool.Accent}"
        /// </summary>
        public const string PanelBackgroundKey = "CaptureTool.PanelBackground";
        public const string PanelBorderKey = "CaptureTool.PanelBorder";
        public const string ForegroundKey = "CaptureTool.Foreground";
        public const string MutedForegroundKey = "CaptureTool.MutedForeground";
        public const string AccentKey = "CaptureTool.Accent";
        public const string KeyCapBackgroundKey = "CaptureTool.KeyCapBackground";
        public const string KeyCapBorderKey = "CaptureTool.KeyCapBorder";

        /// <summary>
        /// True when Greenshot uses the dark theme
        /// </summary>
        public bool IsDarkTheme => ThemeManager.Instance.IsDarkTheme;

        public Brush PanelBackground { get; }
        public Brush PanelBorder { get; }
        public Brush Foreground { get; }
        public Brush MutedForeground { get; }
        public Brush Accent { get; }
        public Brush KeyCapBackground { get; }
        public Brush KeyCapBorder { get; }

        /// <summary>
        /// The font of Greenshot's UI
        /// </summary>
        public FontFamily FontFamily { get; }

        /// <summary>
        /// Space between the border of a panel and its content, in pixels
        /// </summary>
        public double PanelPadding => Math.Round(10 * _dpiScale);

        /// <summary>
        /// The radius of the corners of a panel, in pixels
        /// </summary>
        public double PanelCornerRadius => Math.Round(6 * _dpiScale);

        /// <summary>
        /// Scale a size in device independent units (as used in XAML) to pixels of the capture window
        /// </summary>
        public double Scale(double units) => units * _dpiScale;

        /// <summary>
        /// Text in the font of Greenshot, the size in points scaled like the rest of the UI
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="points">Size in points, 9 is the normal UI text</param>
        /// <param name="brush">null for Foreground</param>
        /// <param name="bold">true for semibold</param>
        public FormattedText CreateText(string text, double points = 9, Brush brush = null, bool bold = false)
        {
            return new FormattedText(text ?? string.Empty, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, bold ? _boldTypeface : _typeface,
                points * 96 / 72 * _dpiScale, brush ?? Foreground, 1.0);
        }

        /// <summary>
        /// The background and border of a panel, as the capture window draws them for ICaptureToolHost.ShowPanel
        /// </summary>
        public void DrawPanel(DrawingContext drawingContext, Rect bounds)
        {
            // Half a pixel in, so the one pixel border is sharp
            var rect = new Rect(bounds.X + 0.5, bounds.Y + 0.5, bounds.Width - 1, bounds.Height - 1);
            drawingContext.DrawRoundedRectangle(PanelBackground, _panelBorder, rect, PanelCornerRadius, PanelCornerRadius);
        }

        /// <summary>
        /// The panel around WPF content, as the capture window shows it for ICaptureToolHost.ShowPanel(owner, content):
        /// the theme brushes as resources, Greenshot's font and foreground for the content, and a scale so the content
        /// is in device independent units in the capture window (where one unit is one pixel).
        /// </summary>
        public Border CreatePanel(FrameworkElement content)
        {
            var border = new Border
            {
                Background = PanelBackground,
                BorderBrush = PanelBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Child = content,
                IsHitTestVisible = false,
                LayoutTransform = new ScaleTransform(_dpiScale, _dpiScale),
                SnapsToDevicePixels = true
            };
            border.Resources[PanelBackgroundKey] = PanelBackground;
            border.Resources[PanelBorderKey] = PanelBorder;
            border.Resources[ForegroundKey] = Foreground;
            border.Resources[MutedForegroundKey] = MutedForeground;
            border.Resources[AccentKey] = Accent;
            border.Resources[KeyCapBackgroundKey] = KeyCapBackground;
            border.Resources[KeyCapBorderKey] = KeyCapBorder;
            TextElement.SetForeground(border, Foreground);
            TextElement.SetFontFamily(border, FontFamily);
            TextElement.SetFontSize(border, 12);
            // The capture window renders text for pixels, the content gets the normal WPF text rendering back
            TextOptions.SetTextFormattingMode(border, TextFormattingMode.Ideal);
            return border;
        }

        /// <summary>
        /// The size of a key cap for the text
        /// </summary>
        public Size MeasureKeyCap(string key)
        {
            var text = CreateText(key, 9, null, true);
            return new Size(Math.Ceiling(text.Width + Scale(14)), Math.Ceiling(text.Height + Scale(7)));
        }

        /// <summary>
        /// A key cap like the hotkey settings show them (e.g. "Ctrl", "F1")
        /// </summary>
        /// <returns>The size of the key cap</returns>
        public Size DrawKeyCap(DrawingContext drawingContext, string key, Point topLeft)
        {
            var size = MeasureKeyCap(key);
            var rect = new Rect(topLeft.X + 0.5, topLeft.Y + 0.5, size.Width - 1, size.Height - 1);
            double radius = Scale(4);
            drawingContext.DrawRoundedRectangle(KeyCapBackground, _keyCapBorder, rect, radius, radius);
            var text = CreateText(key, 9, null, true);
            drawingContext.DrawText(text, new Point(topLeft.X + (size.Width - text.Width) / 2, topLeft.Y + (size.Height - text.Height) / 2));
            return size;
        }

        private static T Frozen<T>(T freezable) where T : Freezable
        {
            var copy = (T)freezable.Clone();
            copy.Freeze();
            return copy;
        }
    }
}
