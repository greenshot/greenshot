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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Wpf;
using Greenshot.Editor.Configuration;
using Greenshot.Helpers;
using Greenshot.Base.Threading;

namespace Greenshot.Plugins.ViewModels
{
    public class PluginViewModel
    {
        public IGreenshotPlugin Plugin { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Company { get; set; }
        public string Location { get; set; }
        public bool IsConfigurable => Plugin is IConfigurablePlugin;

        private UIElement _configControl;
        private bool _controlCreated;

        public UIElement GetConfigurationControl()
        {
            if (!_controlCreated)
            {
                _controlCreated = true;
                _configControl = Plugin == null ? null : PluginHelper.Instance.CreateSettingsView(Plugin) as UIElement;
            }
            return _configControl;
        }
    }
}
