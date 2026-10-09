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

using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.Pdf.Configuration;
using Greenshot.Plugin.Pdf.FileFormatHandlers;
using Greenshot.Plugin.Pdf.Recipes;

namespace Greenshot.Plugin.Pdf;

public sealed class PdfPlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
{
    private IPdfConfiguration _configuration;

    public string Name => "PDF";

    public ValueTask DisposeAsync()
    {
        return default;
    }

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IPdfLanguage>(new PdfLanguageImpl());
        var configuration = new PdfConfigurationImpl();
        services.AddConfiguration(configuration);
        _configuration = configuration;
        services.AddService<IFileFormatHandler>(new PdfFileFormatHandler(configuration));
        services.AddRecipeStepProvider(this);
        services.AddSettingsView<IPdfConfiguration>(settings => new Views.PdfConfigurationControl(settings));
    }

    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.Register<PdfStep>(config => new PdfStep(config, _configuration));
    }

    public object CreateSettingsViewModel(System.IServiceProvider services) => _configuration;

    public Task StartAsync(System.IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
