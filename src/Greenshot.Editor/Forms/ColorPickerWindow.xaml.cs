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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Icons.SafeHandles;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Controls;
using Color = System.Drawing.Color;
using Cursors = System.Windows.Input.Cursors;
using Point = System.Drawing.Point;

namespace Greenshot.Editor.Forms
{
    public partial class ColorPickerWindow : Window
    {
        private static readonly IEditorConfiguration EditorConfig = IniConfigHelper.EnsureSection<IEditorConfiguration>(() => new EditorConfigurationImpl());

        private System.Windows.Input.Cursor _pipetteCursor;
        private MovableShowColorForm _movableShowColorForm;
        private bool _isDraggingPipette;
        private bool _isUpdating;
        private Color _selectedColor;

        private readonly List<Button> _recentButtons = new List<Button>();

        public Color SelectedColor
        {
            get => _selectedColor;
            set => PreviewColor(value);
        }

        public int PaletteCount => PaletteGrid?.Children.Count ?? 0;

        public int RecentColorsCount => RecentColorsGrid?.Children.Count ?? 0;

        public ColorPickerWindow()
        {
            InitializeComponent();

            try
            {
                var icon = GreenshotResources.GetGreenshotIcon();
                Icon = icon != null ? ImageHelper.ToBitmapSource(icon) : null;
            }
            catch
            {
                // Headless/test fallback
            }

            try
            {
                using var pipetteBmp = Pipette.CreatePipetteBitmap();
                PipetteIcon.Source = pipetteBmp.ToBitmapSource();
                _pipetteCursor = CreateWpfCursor(pipetteBmp, 1, 14);
            }
            catch
            {
                // Fallback if cursor creation fails
            }

            CreateColorPalette();
            CreateRecentColorSlots();
            UpdateRecentColors();

            SelectedColor = Color.Black;
        }

        private static System.Windows.Input.Cursor CreateWpfCursor(Bitmap bitmap, int hotspotX, int hotspotY)
        {
            using SafeIconHandle iconHandle = new SafeIconHandle(bitmap.GetHicon());
            NativeIconMethods.GetIconInfo(iconHandle, out var iconInfo);
            iconInfo.Hotspot = new NativePoint(hotspotX, hotspotY);
            iconInfo.IsIcon = false;
            var hIcon = NativeIconMethods.CreateIconIndirect(ref iconInfo);
            return CursorInteropHelper.Create(new SafeIconHandle(hIcon));
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var source = (HwndSource)PresentationSource.FromVisual(this);
            source?.AddHook(HwndHook);

            PositionWindow();
        }

        protected override void OnClosed(EventArgs e)
        {
            _movableShowColorForm?.Dispose();
            _movableShowColorForm = null;
            base.OnClosed(e);
        }

        private void PositionWindow()
        {
            try
            {
                var mouse = System.Windows.Forms.Cursor.Position;
                var screen = System.Windows.Forms.Screen.FromPoint(mouse);
                var workingArea = screen.WorkingArea;

                var source = PresentationSource.FromVisual(this);
                double dpiScaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                double dpiScaleY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

                double winWidth = ActualWidth > 0 ? ActualWidth : 420;
                double winHeight = ActualHeight > 0 ? ActualHeight : 260;

                double mouseDipX = mouse.X / dpiScaleX;
                double mouseDipY = mouse.Y / dpiScaleY;

                double workLeft = workingArea.Left / dpiScaleX;
                double workTop = workingArea.Top / dpiScaleY;
                double workRight = workingArea.Right / dpiScaleX;
                double workBottom = workingArea.Bottom / dpiScaleY;

                double targetX = Math.Max(workLeft, Math.Min(mouseDipX - winWidth / 2, workRight - winWidth));
                double targetY = Math.Max(workTop, Math.Min(mouseDipY - winHeight / 2, workBottom - winHeight));

                Left = targetX;
                Top = targetY;
            }
            catch
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void CreateColorPalette()
        {
            int[][] baseColors = new int[][]
            {
                new int[] { 255, 0, 0 },
                new int[] { 255, 127, 0 },
                new int[] { 255, 255, 0 },
                new int[] { 127, 255, 0 },
                new int[] { 0, 255, 0 },
                new int[] { 0, 255, 127 },
                new int[] { 0, 255, 255 },
                new int[] { 0, 127, 255 },
                new int[] { 0, 0, 255 },
                new int[] { 127, 0, 255 },
                new int[] { 255, 0, 255 },
                new int[] { 255, 0, 127 },
                new int[] { 127, 127, 127 }
            };

            Color[,] palette = new Color[13, 11];
            int shades = 11;
            int shadedColorsNum = (shades - 1) / 2; // 5

            for (int col = 0; col < baseColors.Length; col++)
            {
                int r = baseColors[col][0];
                int g = baseColors[col][1];
                int b = baseColors[col][2];

                for (int i = 0; i <= shadedColorsNum; i++)
                {
                    palette[col, i] = Color.FromArgb(255, r * i / shadedColorsNum, g * i / shadedColorsNum, b * i / shadedColorsNum);
                    if (i > 0)
                    {
                        palette[col, i + shadedColorsNum] = Color.FromArgb(255,
                            r + (255 - r) * i / shadedColorsNum,
                            g + (255 - g) * i / shadedColorsNum,
                            b + (255 - b) * i / shadedColorsNum);
                    }
                }
            }

            for (int row = 0; row < 11; row++)
            {
                for (int col = 0; col < 13; col++)
                {
                    var color = palette[col, row];
                    var btn = new Button
                    {
                        Style = (Style)FindResource("PaletteSwatchStyle"),
                        Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B)),
                        Tag = color,
                        ToolTip = $"{ColorTranslator.ToHtml(color)} | R:{color.R}, G:{color.G}, B:{color.B}"
                    };
                    btn.PreviewMouseLeftButtonDown += PaletteButton_PreviewMouseLeftButtonDown;
                    PaletteGrid.Children.Add(btn);
                }
            }
        }

        private void PaletteButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Color color)
            {
                PreviewColor(color);
                if (e.ClickCount == 2)
                {
                    ApplyColor();
                    e.Handled = true;
                }
            }
        }

        private void CreateRecentColorSlots()
        {
            for (int i = 0; i < 12; i++)
            {
                var btn = new Button
                {
                    Style = (Style)FindResource("PaletteSwatchStyle"),
                    Background = System.Windows.Media.Brushes.Transparent,
                    IsEnabled = false
                };
                btn.PreviewMouseLeftButtonDown += RecentButton_PreviewMouseLeftButtonDown;
                _recentButtons.Add(btn);
                RecentColorsGrid.Children.Add(btn);
            }
        }

        private void RecentButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Color color)
            {
                PreviewColor(color);
                if (e.ClickCount == 2)
                {
                    ApplyColor();
                    e.Handled = true;
                }
            }
        }

        private void UpdateRecentColors()
        {
            if (EditorConfig?.RecentColors == null)
            {
                return;
            }

            for (int i = 0; i < _recentButtons.Count; i++)
            {
                var btn = _recentButtons[i];
                if (i < EditorConfig.RecentColors.Count)
                {
                    var c = EditorConfig.RecentColors[i];
                    btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B));
                    btn.IsEnabled = true;
                    btn.Tag = c;
                    btn.ToolTip = $"{ColorTranslator.ToHtml(c)} | R:{c.R}, G:{c.G}, B:{c.B}";
                }
                else
                {
                    btn.Background = System.Windows.Media.Brushes.Transparent;
                    btn.IsEnabled = false;
                    btn.Tag = null;
                    btn.ToolTip = null;
                }
            }
        }

        private void AddToRecentColors(Color c)
        {
            if (EditorConfig?.RecentColors == null)
            {
                return;
            }

            EditorConfig.RecentColors.Remove(c);
            EditorConfig.RecentColors.Insert(0, c);
            if (EditorConfig.RecentColors.Count > 12)
            {
                EditorConfig.RecentColors.RemoveRange(12, EditorConfig.RecentColors.Count - 12);
            }

            UpdateRecentColors();
        }

        private void PreviewColor(Color color, object source = null)
        {
            _isUpdating = true;
            _selectedColor = color;

            PreviewColorBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));

            if (!ReferenceEquals(source, TextBoxHtml))
            {
                if (color.A < 255)
                {
                    TextBoxHtml.Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
                }
                else
                {
                    TextBoxHtml.Text = ColorTranslator.ToHtml(color);
                }
            }

            if (!ReferenceEquals(source, TextBoxRed))
            {
                TextBoxRed.Text = color.R.ToString();
            }

            if (!ReferenceEquals(source, TextBoxGreen))
            {
                TextBoxGreen.Text = color.G.ToString();
            }

            if (!ReferenceEquals(source, TextBoxBlue))
            {
                TextBoxBlue.Text = color.B.ToString();
            }

            if (!ReferenceEquals(source, TextBoxAlpha))
            {
                TextBoxAlpha.Text = color.A.ToString();
            }

            _isUpdating = false;
        }

        private void BtnTransparent_Click(object sender, RoutedEventArgs e)
        {
            PreviewColor(Color.Transparent);
        }

        private void TextBoxHtml_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating)
            {
                return;
            }

            string text = TextBoxHtml.Text.Trim().TrimStart('#');
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Color parsedColor;
            if (int.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hexVal))
            {
                if (text.Length <= 6)
                {
                    parsedColor = Color.FromArgb(255, Color.FromArgb(hexVal));
                }
                else
                {
                    parsedColor = Color.FromArgb(hexVal);
                }
            }
            else
            {
                try
                {
                    var known = (KnownColor)Enum.Parse(typeof(KnownColor), text, true);
                    parsedColor = Color.FromKnownColor(known);
                }
                catch
                {
                    return;
                }
            }

            PreviewColor(parsedColor, TextBoxHtml);
        }

        private void TextBoxRgb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating)
            {
                return;
            }

            int a = ParseColorPart(TextBoxAlpha.Text, 255);
            int r = ParseColorPart(TextBoxRed.Text, 0);
            int g = ParseColorPart(TextBoxGreen.Text, 0);
            int b = ParseColorPart(TextBoxBlue.Text, 0);

            PreviewColor(Color.FromArgb(a, r, g, b), sender);
        }

        private static int ParseColorPart(string text, int fallback)
        {
            if (int.TryParse(text, out int val))
            {
                return Math.Max(0, Math.Min(255, val));
            }

            return fallback;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                ApplyColor();
                e.Handled = true;
            }
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            ApplyColor();
        }

        private void ApplyColor()
        {
            AddToRecentColors(_selectedColor);
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #region Pipette Logic

        private void BtnPipette_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            User32Api.SetCapture(helper.Handle);
            _isDraggingPipette = true;

            if (_pipetteCursor != null)
            {
                Mouse.OverrideCursor = _pipetteCursor;
            }

            _movableShowColorForm ??= new MovableShowColorForm();
            _movableShowColorForm.Visible = true;

            var screenPt = System.Windows.Forms.Cursor.Position;
            _movableShowColorForm.MoveTo(new NativePoint(screenPt.X, screenPt.Y));

            e.Handled = true;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!_isDraggingPipette)
            {
                return IntPtr.Zero;
            }

            if (msg == (int)WindowsMessages.WM_MOUSEMOVE)
            {
                var screenPt = System.Windows.Forms.Cursor.Position;
                _movableShowColorForm?.MoveTo(new NativePoint(screenPt.X, screenPt.Y));
                handled = true;
            }
            else if (msg == (int)WindowsMessages.WM_LBUTTONUP)
            {
                User32Api.ReleaseCapture();
                _isDraggingPipette = false;
                Mouse.OverrideCursor = null;

                if (_movableShowColorForm != null)
                {
                    _movableShowColorForm.Visible = false;
                    PreviewColor(_movableShowColorForm.Color);
                }

                handled = true;
            }
            else if (msg == (int)WindowsMessages.WM_KEYDOWN && (int)wParam == 27) // Escape key
            {
                User32Api.ReleaseCapture();
                _isDraggingPipette = false;
                Mouse.OverrideCursor = null;

                if (_movableShowColorForm != null)
                {
                    _movableShowColorForm.Visible = false;
                }

                handled = true;
            }
            else if (msg == (int)WindowsMessages.WM_CAPTURECHANGED)
            {
                _isDraggingPipette = false;
                Mouse.OverrideCursor = null;

                if (_movableShowColorForm != null)
                {
                    _movableShowColorForm.Visible = false;
                }
            }

            return IntPtr.Zero;
        }

        #endregion
    }
}
