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
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using log4net;

namespace Greenshot.Editor.Forms
{
    public partial class TornEdgeSettingsWindow : Window
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(TornEdgeSettingsWindow));
        private readonly TornEdgeEffect _effect;
        private bool _isUpdatingEdges;

        public TornEdgeSettingsWindow() : this(new TornEdgeEffect())
        {
        }

        public TornEdgeSettingsWindow(TornEdgeEffect effect)
        {
            _effect = effect ?? new TornEdgeEffect();
            InitializeComponent();
            try
            {
                Icon = ImageHelper.ToBitmapSource(GreenshotResources.GetGreenshotIcon());
            }
            catch (Exception ex)
            {
                LOG.Debug("Could not set window icon", ex);
            }

            ShowSettings();
        }

        private void ShowSettings()
        {
            GenerateShadowCheckBox.IsChecked = _effect.GenerateShadow;
            DarknessSlider.Value = Math.Max(1, Math.Min(40, (int)(_effect.Darkness * 40)));
            OffsetXSlider.Value = Math.Max(-20, Math.Min(20, _effect.ShadowOffset.X));
            OffsetYSlider.Value = Math.Max(-20, Math.Min(20, _effect.ShadowOffset.Y));
            ThicknessSlider.Value = Math.Max(1, Math.Min(20, _effect.ShadowSize));

            ToothSizeSlider.Value = Math.Max(0, Math.Min(40, _effect.ToothHeight));
            VerticalToothRangeSlider.Value = Math.Max(0, Math.Min(40, _effect.VerticalToothRange));
            HorizontalToothRangeSlider.Value = Math.Max(0, Math.Min(40, _effect.HorizontalToothRange));

            _isUpdatingEdges = true;
            TopEdgeCheckBox.IsChecked = _effect.Edges != null && _effect.Edges.Length > 0 && _effect.Edges[0];
            RightEdgeCheckBox.IsChecked = _effect.Edges != null && _effect.Edges.Length > 1 && _effect.Edges[1];
            BottomEdgeCheckBox.IsChecked = _effect.Edges != null && _effect.Edges.Length > 2 && _effect.Edges[2];
            LeftEdgeCheckBox.IsChecked = _effect.Edges != null && _effect.Edges.Length > 3 && _effect.Edges[3];
            AllEdgesCheckBox.IsChecked = TopEdgeCheckBox.IsChecked == true && RightEdgeCheckBox.IsChecked == true &&
                                        BottomEdgeCheckBox.IsChecked == true && LeftEdgeCheckBox.IsChecked == true;
            _isUpdatingEdges = false;
        }

        private void AllEdges_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingEdges) return;
            _isUpdatingEdges = true;
            bool check = AllEdgesCheckBox.IsChecked == true;
            TopEdgeCheckBox.IsChecked = check;
            RightEdgeCheckBox.IsChecked = check;
            BottomEdgeCheckBox.IsChecked = check;
            LeftEdgeCheckBox.IsChecked = check;
            _isUpdatingEdges = false;
        }

        private void Edge_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingEdges) return;
            _isUpdatingEdges = true;
            AllEdgesCheckBox.IsChecked = TopEdgeCheckBox.IsChecked == true && RightEdgeCheckBox.IsChecked == true &&
                                        BottomEdgeCheckBox.IsChecked == true && LeftEdgeCheckBox.IsChecked == true;
            _isUpdatingEdges = false;
        }

        private void GenerateShadow_Click(object sender, RoutedEventArgs e)
        {
            // Enabled state bound to CheckBox.IsChecked
        }

        public bool? ShowDialog(System.Windows.Forms.IWin32Window owner)
        {
            if (owner != null)
            {
                new WindowInteropHelper(this) { Owner = owner.Handle };
            }
            return ShowDialog();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _effect.Darkness = (float)(DarknessSlider.Value / 40.0);
            _effect.ShadowOffset = new NativePoint((int)OffsetXSlider.Value, (int)OffsetYSlider.Value);
            _effect.ShadowSize = (int)ThicknessSlider.Value;
            _effect.ToothHeight = (int)ToothSizeSlider.Value;
            _effect.VerticalToothRange = (int)VerticalToothRangeSlider.Value;
            _effect.HorizontalToothRange = (int)HorizontalToothRangeSlider.Value;
            _effect.Edges = new[]
            {
                TopEdgeCheckBox.IsChecked == true,
                RightEdgeCheckBox.IsChecked == true,
                BottomEdgeCheckBox.IsChecked == true,
                LeftEdgeCheckBox.IsChecked == true
            };
            _effect.GenerateShadow = GenerateShadowCheckBox.IsChecked == true;
            DialogResult = true;
            Close();
        }
    }
}
