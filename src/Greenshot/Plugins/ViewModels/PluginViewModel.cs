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


using System.ComponentModel;
using System.Windows;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugins.ViewModels
{
    public class PluginViewModel : INotifyPropertyChanged
    {
        private bool _isAllowed = true;

        /// <summary>
        /// The loaded plugin, null when it isn't loaded
        /// </summary>
        public IGreenshotPlugin Plugin { get; set; }

        /// <summary>
        /// The name in ExcludePluginNames and IncludePluginNames: Box for Greenshot.Plugin.Box.dll
        /// </summary>
        public string PluginName { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// The version, or why the plugin isn't loaded
        /// </summary>
        public string Version { get; set; }
        public string Company { get; set; }
        public string Location { get; set; }
        public bool IsConfigurable => Plugin is IConfigurablePlugin;

        /// <summary>
        /// The plugin is loaded at the next start, unchecked it is in ExcludePluginNames
        /// </summary>
        public bool IsAllowed
        {
            get => _isAllowed;
            set
            {
                if (_isAllowed != value)
                {
                    _isAllowed = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAllowed)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        /// <summary>
        /// The text a screen reader reads for the row of the plugin list
        /// </summary>
        public override string ToString() => Name ?? base.ToString();
    }
}
