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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;
using Greenshot.Plugin.ExternalCommand.Running;

namespace Greenshot.Plugin.ExternalCommand.ViewModels;

/// <summary>
/// An app in the list, its icon comes later
/// </summary>
public sealed class AppPickerItem : INotifyPropertyChanged
{
    private ImageSource _icon;

    internal AppPickerItem(InstalledApp app)
    {
        App = app;
    }

    internal InstalledApp App { get; }

    public string Name => App.Name;

    public string Program => App.Program;

    public ImageSource Icon
    {
        get => _icon;
        internal set
        {
            _icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>The text a screen reader reads</summary>
    public override string ToString() => Name;
}

/// <summary>
/// Pick an app for a command: the apps which open the image format, the App Paths and the App Execution Aliases, with a search
/// </summary>
public sealed class AppPickerViewModel : INotifyPropertyChanged
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(AppPickerViewModel));
    private readonly List<AppPickerItem> _all;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private string _search;
    private AppPickerItem _selected;

    /// <param name="apps">the apps to offer</param>
    internal AppPickerViewModel(IEnumerable<InstalledApp> apps)
    {
        _all = (apps ?? Enumerable.Empty<InstalledApp>()).Select(app => new AppPickerItem(app)).ToList();
        Apps = new ObservableCollection<AppPickerItem>(_all);
        LoadIconsAsync().FireAndLog("Load the icons of the apps", Log);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<AppPickerItem> Apps { get; }

    public bool HasNoApps => Apps.Count == 0;

    public string Search
    {
        get => _search;
        set
        {
            _search = value;
            Changed();
            Apps.Clear();
            foreach (var item in _all.Where(item => Matches(item, value)))
            {
                Apps.Add(item);
            }

            SelectedApp = Apps.Contains(_selected) ? _selected : Apps.FirstOrDefault();
            Changed(nameof(HasNoApps));
        }
    }

    public AppPickerItem SelectedApp
    {
        get => _selected;
        set
        {
            _selected = value;
            Changed();
            Changed(nameof(CanChoose));
        }
    }

    public bool CanChoose => _selected != null;

    /// <summary>
    /// A part of the name or the program, ignoring case
    /// </summary>
    internal static bool Matches(AppPickerItem item, string search) =>
        string.IsNullOrWhiteSpace(search)
        || item.Name.IndexOf(search.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0
        || item.Program.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;

    private async Task LoadIconsAsync()
    {
        foreach (var item in _all)
        {
            var image = await IconCache.IconForCommandAsync(item.Program, item.Name).ConfigureAwait(false);
            if (image == null) continue;
            ImageSource icon;
            // The icon cache owns its images
            lock (image)
            {
                icon = image.ToBitmapSource();
            }

            icon?.Freeze();
            await _dispatcher.InvokeAsync(() => item.Icon = icon).Task.ConfigureAwait(false);
        }
    }

    private void Changed([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
