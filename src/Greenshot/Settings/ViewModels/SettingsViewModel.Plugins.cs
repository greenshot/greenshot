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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Plugins;
using Greenshot.Plugins.ViewModels;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The plugins tab. Not in Greenshot Light, which has no plugins.
    /// </summary>
    public partial class SettingsViewModel
    {
        private PluginViewModel _selectedPlugin;

        public ObservableCollection<UIElement> PluginControls { get; } = new ObservableCollection<UIElement>();

        public ObservableCollection<PluginViewModel> Plugins { get; private set; }

        public PluginViewModel SelectedPlugin
        {
            get => _selectedPlugin;
            set
            {
                if (_selectedPlugin != value)
                {
                    _selectedPlugin = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanConfigureSelectedPlugin));
                    OnPropertyChanged(nameof(SelectedPluginControl));
                    OnPropertyChanged(nameof(HasSelectedPluginControl));
                    OnPropertyChanged(nameof(SelectedPluginControlVisibility));
                    OnPropertyChanged(nameof(NoSelectedPluginControlVisibility));
                }
            }
        }

        public UIElement SelectedPluginControl => SelectedPlugin?.GetConfigurationControl();
        public bool HasSelectedPluginControl => SelectedPluginControl != null;
        public Visibility SelectedPluginControlVisibility => HasSelectedPluginControl ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NoSelectedPluginControlVisibility => HasSelectedPluginControl ? Visibility.Collapsed : Visibility.Visible;

        public void SelectPluginByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || Plugins == null) return;
            var item = Plugins.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                SelectedPlugin = item;
            }
        }

        public bool CanConfigureSelectedPlugin => SelectedPlugin?.IsConfigurable == true;

        public void ConfigureSelectedPlugin()
        {
            if (CanConfigureSelectedPlugin)
            {
                SimpleServiceProvider.Current.GetInstance<IGreenshotShell>(isOptional: true)?.ShowSetting(SelectedPlugin?.Name);
            }
        }

        /// <summary>
        /// greenshot-fixed.ini doesn't let the user switch plugins off
        /// </summary>
        public bool ArePluginExclusionsFixed => ConfigurationRestrictions.ArePluginExclusionsFixed(CoreConfiguration);

        /// <summary>
        /// The loaded plugins, the plugins which are installed but not loaded, and those which the settings name but aren't installed
        /// </summary>
        private void InitializePlugins()
        {
            var items = new List<PluginViewModel>();
            try
            {
                var plugins = SimpleServiceProvider.Current.GetAllInstances<IGreenshotPlugin>();
                if (plugins != null)
                {
                    foreach (var plugin in plugins)
                    {
                        var assembly = plugin.GetType().Assembly;
                        var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;
                        var version = EnvironmentInfo.GetVersion(assembly);
                        var location = assembly.Location ?? string.Empty;

                        items.Add(new PluginViewModel
                        {
                            Plugin = plugin,
                            PluginName = ConfigurationRestrictions.PluginNameFromFile(location),
                            Name = plugin.Name,
                            Version = version,
                            Company = company,
                            Location = location
                        });
                    }
                }
            }
            catch
            {
                // In some test scenarios SimpleServiceProvider might not have plugins registered
            }

            var found = PluginHelper.Instance.FoundPluginNames;
            var named = found
                .Concat(ConfigurationRestrictions.Names(CoreConfiguration.ExcludePluginNames))
                .Concat(ConfigurationRestrictions.Names(CoreConfiguration.BlockedPluginNames))
                .Concat(ConfigurationRestrictions.Names(CoreConfiguration.AllowedPluginNames))
                .Concat(ConfigurationRestrictions.Names(CoreConfiguration.ExcludePlugins).Select(ConfigurationRestrictions.PluginNameFromLegacyIdentifier))
                .Concat(ConfigurationRestrictions.Names(CoreConfiguration.IncludePlugins).Select(ConfigurationRestrictions.PluginNameFromLegacyIdentifier))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(name => !items.Any(item => string.Equals(item.PluginName, name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            foreach (var name in named)
            {
                bool isAllowed = ConfigurationRestrictions.IsPluginAllowed(CoreConfiguration, name);
                string status = ConfigurationRestrictions.IsPluginBlockedByAdministrator(CoreConfiguration, name) ? Texts.Settings.PluginBlocked
                    : !found.Contains(name, StringComparer.OrdinalIgnoreCase) ? Texts.Settings.PluginNotInstalled
                    : PluginHelper.Instance.HeldBackPlugins.Any(plugin => string.Equals(plugin.Name, name, StringComparison.OrdinalIgnoreCase)) ? Texts.Settings.PluginNotSigned
                    : isAllowed ? Texts.Settings.PluginNotLoaded
                    : Texts.Settings.PluginOff;
                items.Add(new PluginViewModel { PluginName = name, Name = name, Version = status });
            }

            foreach (var item in items)
            {
                item.IsAllowed = ConfigurationRestrictions.IsPluginAllowed(CoreConfiguration, item.PluginName);
                item.CanChangeAllowed = !ArePluginExclusionsFixed && !ConfigurationRestrictions.IsPluginBlockedByAdministrator(CoreConfiguration, item.PluginName);
            }

            Plugins = new ObservableCollection<PluginViewModel>(items.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase));

            // The signatures are checked while the window opens, about 6 ms per file
            LoadPluginSignaturesAsync(items.Where(item => !string.IsNullOrEmpty(item.Location)).ToList()).FireAndLog("Check the plugin signatures");
        }

        /// <summary>
        /// Started on the UI thread, the results are set there (the continuation returns to the UI thread)
        /// </summary>
        private static async Task LoadPluginSignaturesAsync(IReadOnlyList<PluginViewModel> loadedPlugins)
        {
            if (loadedPlugins.Count == 0)
            {
                return;
            }

            var files = loadedPlugins.Select(item => item.Location).ToList();
            // PARALLEL: WinVerifyTrust reads every file, the settings window must not wait for it
#pragma warning disable RS0030 // R10: documented parallel branch
            var signatures = await Task.Run(() =>
            {
                string greenshotExe = System.Windows.Forms.Application.ExecutablePath;
                string greenshotSigner = AuthenticodeHelper.VerifyAuthenticode(greenshotExe) ? AuthenticodeHelper.GetSignerThumbprint(greenshotExe) : null;
                return files.Select(file =>
                {
                    string signer = AuthenticodeHelper.GetVerifiedSigner(file);
                    bool likeGreenshot = signer != null && greenshotSigner != null
                        && string.Equals(AuthenticodeHelper.GetSignerThumbprint(file), greenshotSigner, StringComparison.OrdinalIgnoreCase);
                    return (Signer: signer, LikeGreenshot: likeGreenshot);
                }).ToList();
            }).ConfigureAwait(true);
#pragma warning restore RS0030

            for (int i = 0; i < loadedPlugins.Count; i++)
            {
                loadedPlugins[i].Signature = signatures[i].Signer == null ? Texts.Settings.PluginNotSigned : string.Format(Texts.Settings.PluginSignedBy, signatures[i].Signer);
                loadedPlugins[i].IsSignedLikeGreenshot = signatures[i].LikeGreenshot;
            }
        }

        /// <summary>
        /// Add a plugin which isn't installed by its name: unchecked to block it before it is installed, checked to allow it
        /// </summary>
        /// <returns>false when the name is empty or already listed</returns>
        public bool AddPluginName(string name, bool isAllowed)
        {
            name = name?.Trim();
            if (string.IsNullOrEmpty(name) || Plugins.Any(item => string.Equals(item.PluginName, name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var item = new PluginViewModel { PluginName = name, Name = name, Version = Texts.Settings.PluginNotInstalled, IsAllowed = isAllowed };
            Plugins.Add(item);
            SelectedPlugin = item;
            return true;
        }

        private void SavePluginRestrictions()
        {
            if (ArePluginExclusionsFixed || Plugins == null)
            {
                return;
            }

            var changeable = Plugins.Where(item => item.CanChangeAllowed).ToList();
            CoreConfiguration.ExcludePluginNames = ConfigurationRestrictions.ReplaceListed(CoreConfiguration.ExcludePluginNames,
                changeable.Select(item => item.PluginName),
                changeable.Where(item => !item.IsAllowed).Select(item => item.PluginName));
        }

        private void AppendPluginRestrictions(StringBuilder text, bool allowOnlyChecked)
        {
            if (allowOnlyChecked)
            {
                AppendList(text, nameof(ICoreConfiguration.AllowedPluginNames), Plugins.Where(item => item.IsAllowed).Select(item => item.PluginName));
            }
            else
            {
                AppendList(text, nameof(ICoreConfiguration.BlockedPluginNames), Plugins.Where(item => !item.IsAllowed).Select(item => item.PluginName));
            }
        }
    }
}
