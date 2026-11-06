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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Stamps.Library;

namespace Greenshot.Plugin.Stamps;

/// <summary>
/// Stamps: small images (PNG, SVG, ...) collected in a folder, with names and tags, inserted into the editor as elements
/// or given to other programs with the clipboard or by dragging. Shown as a panel of the editor. A beta feature: without "beta tester" nothing is registered.
/// See docs/vNext/stamps.md.
/// </summary>
public class StampsPlugin : IGreenshotPlugin
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(StampsPlugin));
    private IStampsConfiguration _config;
    private StampLibrary _library;

    public ValueTask DisposeAsync() => default;

    public string Name => "Stamps";

    /// <summary>
    /// The feature is for beta testers only
    /// </summary>
    internal static bool IsEnabled => IniConfigRegistry.GetSection<ICoreConfiguration>()?.IsBetaTester == true;

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IStampsLanguage>(new StampsLanguageImpl());
        var section = new StampsConfigurationImpl();
        services.AddConfiguration(section);
        _config = section;

        // In the editor the stamps are a panel. Decided with the loaded configuration: nothing is registered for a user who isn't a beta tester
        var panel = new StampsPanel(() => Library);
        services.AddServices<IEditorPanel>(() => IsEnabled ? new IEditorPanel[] { panel } : Array.Empty<IEditorPanel>());
    }

    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// The stamps folder: the configured one, else Stamps next to greenshot.ini (so the portable Greenshot keeps it next to itself)
    /// </summary>
    internal string LibraryDirectory
    {
        get
        {
            string configured = _config?.LibraryPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return Environment.ExpandEnvironmentVariables(configured.Trim());
            }

            string iniDirectory = Path.GetDirectoryName(GreenshotEnvironment.ConfigLocation);
            if (string.IsNullOrEmpty(iniDirectory))
            {
                iniDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Greenshot");
            }

            return Path.Combine(iniDirectory, "Stamps");
        }
    }

    /// <summary>
    /// The stamps folder, shared by the panels of all editor windows: read the first time it's needed
    /// </summary>
    private StampLibrary Library
    {
        get
        {
            if (_library == null)
            {
                _library = new StampLibrary(LibraryDirectory, StampImages.SupportedExtensions());
                try
                {
                    _library.Load();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Log.Warn($"Can't read the stamps folder {_library.Directory}", ex);
                }
            }

            return _library;
        }
    }
}
