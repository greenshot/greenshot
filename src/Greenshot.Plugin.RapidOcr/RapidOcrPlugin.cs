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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Plugin.RapidOcr.Files;

namespace Greenshot.Plugin.RapidOcr;

/// <summary>
/// Adds the OCR engine RapidOcr, chosen in the settings next to the OCR language. Nothing is downloaded before the user agrees.
/// See docs/vNext/rapid-ocr.md.
/// </summary>
public class RapidOcrPlugin : IGreenshotPlugin, IConfigurablePlugin
{
    private RapidOcrEngine _engine;
    private IRapidOcrConfiguration _configuration;

    public string Name => "RapidOcr";

    /// <summary>
    /// RapidOCR next to greenshot.ini, so the portable Greenshot keeps it next to itself
    /// </summary>
    internal static string FilesDirectory
    {
        get
        {
            string iniDirectory = Path.GetDirectoryName(GreenshotEnvironment.ConfigLocation);
            if (string.IsNullOrEmpty(iniDirectory))
            {
                iniDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Greenshot");
            }

            return Path.Combine(iniDirectory, "RapidOCR");
        }
    }

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IRapidOcrLanguage>(new RapidOcrLanguageImpl());
        var configuration = new RapidOcrConfigurationImpl();
        services.AddConfiguration(configuration);
        _configuration = configuration;
        _engine = new RapidOcrEngine(new RapidOcrFiles(FilesDirectory), configuration);
        services.AddService<IOcrEngine>(_engine);
        services.AddSettingsView<IRapidOcrConfiguration>(settings => new Views.RapidOcrConfigurationView(settings));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => _configuration;

    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        _engine?.Dispose();
        return default;
    }
}
