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

using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;

namespace Greenshot.Plugin.Zxing;

public class ZxingPlugin : IGreenshotPlugin, IRecipeStepProvider
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ZxingPlugin));
    private static IZxingConfiguration _config;
    private ZxingCaptureProcessor _captureProcessor;
    private ZxingEditorPlugin _editorPlugin;
    private ZxingHotspotTransformer _hotspotTransformer;

    public string Name => "Zxing";

    public bool IsConfigurable => true;

    public void RegisterConfiguration(IniConfig iniConfig)
    {
        var section = new ZxingConfigurationImpl();
        iniConfig.AddSection(section);
        _config = section;
    }

    public void RegisterServices(IServiceLocator serviceLocator)
    {
        _captureProcessor = new ZxingCaptureProcessor(_config);
        _editorPlugin = new ZxingEditorPlugin(_config);
        _hotspotTransformer = new ZxingHotspotTransformer();
        serviceLocator.AddService<IProcessor>(_captureProcessor);
        serviceLocator.AddService<IEditorPlugin>(_editorPlugin);
        serviceLocator.AddService<IFeatureHotspotTransformer>(_hotspotTransformer);
        serviceLocator.AddService<IDestination>(new ZxingQrDestination());
        serviceLocator.AddService<IRecipeStepProvider>(this);
        StepRegistry.Instance.RegisterProvider(this);
    }

    /// <summary>
    /// Registers recipe step factories provided by the ZXing plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.RegisterStepFactory("Zxing", config => new ZxingStep(config));
        registry.RegisterStepFactory("ZxingQr", config => new ZxingStep(config));
        registry.RegisterStepFactory("ZxingBarcode", config => new ZxingStep(config));
        registry.RegisterStepFactory("BarcodeScan", config => new ZxingStep(config));
        registry.RegisterStepFactory("DecodeBarcode", config => new ZxingStep(config));
        registry.RegisterStepFactory("QrCode", config => new ZxingStep(config));
    }

    public bool Start()
    {
        return true;
    }

    public void Shutdown()
    {
        Log.Debug("ZXing plugin shutdown.");
    }

    public void Configure()
    {
        using (var form = new ZxingSettingsForm(_config))
        {
            form.ShowDialog();
        }
    }

    public void Dispose()
    {
    }
}
