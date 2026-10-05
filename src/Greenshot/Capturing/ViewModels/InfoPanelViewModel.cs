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
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;

namespace Greenshot.Capturing.ViewModels
{
    /// <summary>
    /// What the info panel shows, the bindings of InfoPanelView.xaml update it
    /// </summary>
    public class InfoPanelViewModel : INotifyPropertyChanged
    {
        private string _screen = string.Empty;
        private string _allScreens = string.Empty;
        private bool _hasMoreScreens;
        private string _selection = string.Empty;
        private string _window = string.Empty;
        private string _mouse = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Title => Texts.Core.CaptureInfoTitle;
        public string ScreenLabel => Texts.Core.CaptureInfoScreen;
        public string AllScreensLabel => Texts.Core.CaptureInfoAllScreens;
        public string SelectionLabel => Texts.Core.CaptureInfoSelection;
        public string WindowLabel => Texts.Core.CaptureInfoWindow;
        public string MouseLabel => Texts.Core.CaptureInfoMouse;

        /// <summary>
        /// The resolution of the monitor under the cursor
        /// </summary>
        public string Screen
        {
            get => _screen;
            set => Set(ref _screen, value);
        }

        /// <summary>
        /// The size of all monitors together, only shown when there is more than one
        /// </summary>
        public string AllScreens
        {
            get => _allScreens;
            set => Set(ref _allScreens, value);
        }

        public bool HasMoreScreens
        {
            get => _hasMoreScreens;
            set
            {
                if (Set(ref _hasMoreScreens, value))
                {
                    OnPropertyChanged(nameof(AllScreensVisibility));
                }
            }
        }

        public Visibility AllScreensVisibility => _hasMoreScreens ? Visibility.Visible : Visibility.Collapsed;

        public string Selection
        {
            get => _selection;
            set => Set(ref _selection, value);
        }

        /// <summary>
        /// The title of the window under the cursor
        /// </summary>
        public string Window
        {
            get => _window;
            set => Set(ref _window, value);
        }

        public string Mouse
        {
            get => _mouse;
            set => Set(ref _mouse, value);
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
