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
using Dapplo.Ini;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Forms.Wpf
{
    public partial class PrintOptionsWindow : Window
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(PrintOptionsWindow));
        private readonly ICoreConfiguration _coreConfiguration;

        public PrintOptionsWindow()
        {
            _coreConfiguration = IniConfigRegistry.GetSection<ICoreConfiguration>();
            InitializeComponent();
            try
            {
                Icon = ImageHelper.ToBitmapSource(GreenshotResources.GetGreenshotIcon());
            }
            catch (Exception ex)
            {
                LOG.Debug("Could not set window icon", ex);
            }

            LoadSettings();
        }

        private void LoadSettings()
        {
            CheckboxAllowShrink.IsChecked = _coreConfiguration.OutputPrintAllowShrink;
            CheckboxAllowEnlarge.IsChecked = _coreConfiguration.OutputPrintAllowEnlarge;
            CheckboxAllowRotate.IsChecked = _coreConfiguration.OutputPrintAllowRotate;
            CheckboxAllowCenter.IsChecked = _coreConfiguration.OutputPrintCenter;
            CheckboxDateTime.IsChecked = _coreConfiguration.OutputPrintFooter;
            CheckboxPrintInverted.IsChecked = _coreConfiguration.OutputPrintInverted;

            if (_coreConfiguration.OutputPrintGrayscale)
            {
                RadioBtnGrayScale.IsChecked = true;
            }
            else if (_coreConfiguration.OutputPrintMonochrome)
            {
                RadioBtnMonochrome.IsChecked = true;
            }
            else
            {
                RadioBtnColorPrint.IsChecked = true;
            }

            CheckboxDontAskAgain.IsChecked = false;
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
            _coreConfiguration.OutputPrintAllowShrink = CheckboxAllowShrink.IsChecked == true;
            _coreConfiguration.OutputPrintAllowEnlarge = CheckboxAllowEnlarge.IsChecked == true;
            _coreConfiguration.OutputPrintAllowRotate = CheckboxAllowRotate.IsChecked == true;
            _coreConfiguration.OutputPrintCenter = CheckboxAllowCenter.IsChecked == true;
            _coreConfiguration.OutputPrintFooter = CheckboxDateTime.IsChecked == true;
            _coreConfiguration.OutputPrintInverted = CheckboxPrintInverted.IsChecked == true;

            _coreConfiguration.OutputPrintGrayscale = RadioBtnGrayScale.IsChecked == true;
            _coreConfiguration.OutputPrintMonochrome = RadioBtnMonochrome.IsChecked == true;

            _coreConfiguration.OutputPrintPromptOptions = CheckboxDontAskAgain.IsChecked != true;
            IniConfigRegistry.Get().Save();

            DialogResult = true;
            Close();
        }
    }
}
