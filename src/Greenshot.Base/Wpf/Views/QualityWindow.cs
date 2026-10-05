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

using System.Windows;
using System.Windows.Controls;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;

namespace Greenshot.Base.Wpf.Views
{
    /// <summary>
    /// Asks for the output settings of a file: the JPEG quality (only for JPEG) and whether to reduce the colors.
    /// OK takes the values; Cancel, Escape or closing the window means the user doesn't want to save (the settings stay as they were).
    /// </summary>
    public sealed class QualityWindow : Window
    {
        private static ICoreConfiguration CoreConfig => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());

        private readonly SurfaceOutputSettings _settings;
        private readonly Slider _qualitySlider;
        private readonly CheckBox _reduceColors;
        private readonly CheckBox _dontAskAgain;

        public QualityWindow(SurfaceOutputSettings settings)
        {
            _settings = settings;
            string title = Texts.Core.QualitydialogTitle;
            ThemedControls.ApplyDialogLook(this, title);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            Width = 380;
            SizeToContent = SizeToContent.Height;
            try
            {
                Icon = ImageHelper.ToBitmapSource(GreenshotResources.GetGreenshotIcon());
            }
            catch
            {
                // The window works without an icon
            }

            bool isJpeg = WellKnownFileFormats.IsEqualFormat(WellKnownFileFormats.Jpg, settings.Format);

            _reduceColors = ThemedControls.CreateCheckBox(Texts.Settings.Reducecolors, settings.ReduceColors);

            var qualityLabel = new TextBlock
            {
                Text = Texts.Core.JpegqualitydialogChoosejpegquality,
                TextWrapping = TextWrapping.Wrap,
                Foreground = WpfThemeHelper.TextPrimary,
                Margin = new Thickness(0, 10, 0, 4)
            };
            _qualitySlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = settings.JPGQuality,
                SmallChange = 1,
                LargeChange = 10,
                TickFrequency = 10,
                TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight,
                IsSnapToTickEnabled = false,
                VerticalAlignment = VerticalAlignment.Center
            };
            var qualityValue = new TextBlock
            {
                Width = 36,
                TextAlignment = TextAlignment.Right,
                Foreground = WpfThemeHelper.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                Text = settings.JPGQuality.ToString()
            };
            _qualitySlider.ValueChanged += (s, e) => qualityValue.Text = ((int)e.NewValue).ToString();
            var qualityRow = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(qualityValue, Dock.Right);
            qualityRow.Children.Add(qualityValue);
            qualityRow.Children.Add(_qualitySlider);
            // The quality only matters for JPEG
            qualityLabel.IsEnabled = qualityRow.IsEnabled = isJpeg;
            qualityLabel.Opacity = qualityRow.Opacity = isJpeg ? 1 : 0.5;

            _dontAskAgain = ThemedControls.CreateCheckBox(Texts.Core.QualitydialogDontaskagain, false);
            _dontAskAgain.Margin = new Thickness(0, 10, 0, 0);

            var ok = ThemedControls.CreateButton(Texts.Core.Ok, true, false);
            ok.Click += (s, e) =>
            {
                Apply();
                DialogResult = true;
            };
            var cancel = ThemedControls.CreateButton(Texts.Core.Cancel, false, true);
            var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            buttonRow.Children.Add(ok);
            buttonRow.Children.Add(cancel);

            var root = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };
            root.Children.Add(ThemedControls.CreateHeader(this, title));
            root.Children.Add(_reduceColors);
            root.Children.Add(qualityLabel);
            root.Children.Add(qualityRow);
            root.Children.Add(_dontAskAgain);
            root.Children.Add(buttonRow);
            Content = root;
            Loaded += (s, e) =>
            {
                Activate();
                ok.Focus();
            };
        }

        /// <summary>
        /// The settings, changed when the user pressed OK
        /// </summary>
        public SurfaceOutputSettings Settings => _settings;

        private void Apply()
        {
            _settings.JPGQuality = (int)_qualitySlider.Value;
            _settings.ReduceColors = _reduceColors.IsChecked == true;
            if (_dontAskAgain.IsChecked == true)
            {
                CoreConfig.OutputFileJpegQuality = _settings.JPGQuality;
                CoreConfig.OutputFilePromptQuality = false;
                CoreConfig.OutputFileReduceColors = _settings.ReduceColors;
            }
        }
    }
}
