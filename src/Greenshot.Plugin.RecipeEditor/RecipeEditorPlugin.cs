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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Plugin.RecipeEditor.Views;
using log4net;
using Greenshot.Base.Languages;

namespace Greenshot.Plugin.RecipeEditor;

public class RecipeEditorPlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeEditorService
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeEditorPlugin));
    private static IRecipeConfiguration _config;
    private ToolStripMenuItem _itemPlugInConfig;
    private static RecipeEditorWindow _activeRecipeEditorWindow;

    public ValueTask DisposeAsync()
    {
        // The menu item is removed and disposed in StopAsync
        return default;
    }

    public string Name => "RecipeEditor";

    public void ConfigureServices(IPluginServices services)
    {
        var section = new RecipeConfigurationImpl();
        services.AddConfiguration(section);
        _config = section;

        services.AddService<IRecipeEditorService>(this);
        services.AddSettingsView<IRecipeConfiguration>(config => new RecipeEditorConfigurationView(config));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => _config;

    /// <summary>
    /// Add the quick link to the context menu (on the UI thread)
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<IUiDispatcher>().RunOnUiAsync(Start, cancellationToken);

    private void Start()
    {
        if (_config != null && _config.QuicklinkEnabled)
        {
            _itemPlugInConfig = new ToolStripMenuItem
            {
                Text = Texts.Core.ContextmenuRecipeeditor ?? "Recipe Editor...",
                Visible = true
            };
            _itemPlugInConfig.Click += (s, e) => OpenEditor();
            PluginUtils.AddToContextMenu(_itemPlugInConfig);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        UiDispatcher.Current.RunOnUiAsync(() =>
        {
            _itemPlugInConfig?.Dispose();
            _itemPlugInConfig = null;
        }, cancellationToken);

    public bool IsEditorOpen => _activeRecipeEditorWindow != null && _activeRecipeEditorWindow.IsLoaded;

    public void OpenRecipeManager()
    {
        void ShowAction()
        {
            try
            {
                if (_activeRecipeEditorWindow != null && _activeRecipeEditorWindow.IsLoaded)
                {
                    if (_activeRecipeEditorWindow.WindowState == WindowState.Minimized)
                    {
                        _activeRecipeEditorWindow.WindowState = WindowState.Normal;
                    }
                    _activeRecipeEditorWindow.Activate();
                    _activeRecipeEditorWindow.Focus();
                    _activeRecipeEditorWindow.ViewModel?.OpenRecipeManager();
                    return;
                }

                var recipeManager = SimpleServiceProvider.Current?.GetInstance<IRecipeManager>(isOptional: true);
                var pipeline = SimpleServiceProvider.Current?.GetInstance<ICapturePipeline>(isOptional: true);
                var vm = new ViewModels.RecipeManagerViewModel(
                    recipeManager,
                    pipeline,
                    selected =>
                    {
                        OpenEditor(selected.Id);
                    },
                    () =>
                    {
                        OpenEditor();
                    });

                var dlg = new RecipeManagerWindow(vm)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(dlg);
                dlg.Show();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to open Recipe Manager dialog", ex);
            }
        }

        // Windows are shown on the one UI thread
        UiDispatcher.Current.RunOnUiAsync(ShowAction).FireAndLog("Show a recipe editor window", Log);
    }

    public void OpenEditor(string recipeId = null)
    {
        void ShowAction()
        {
            try
            {
                if (_activeRecipeEditorWindow != null && _activeRecipeEditorWindow.IsLoaded)
                {
                    if (_activeRecipeEditorWindow.WindowState == WindowState.Minimized)
                    {
                        _activeRecipeEditorWindow.WindowState = WindowState.Normal;
                    }
                    _activeRecipeEditorWindow.Activate();
                    _activeRecipeEditorWindow.Focus();

                    if (!string.IsNullOrEmpty(recipeId))
                    {
                        _activeRecipeEditorWindow.ViewModel?.SelectRecipeById(recipeId);
                    }
                    return;
                }

                var recipeManager = SimpleServiceProvider.Current?.GetInstance<IRecipeManager>(isOptional: true);
                _activeRecipeEditorWindow = new RecipeEditorWindow(recipeManager);
                _activeRecipeEditorWindow.Closed += (s, e) => _activeRecipeEditorWindow = null;
                System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(_activeRecipeEditorWindow);
                _activeRecipeEditorWindow.Show();

                if (!string.IsNullOrEmpty(recipeId))
                {
                    _activeRecipeEditorWindow.ViewModel?.SelectRecipeById(recipeId);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to open Recipe Editor window", ex);
            }
        }

        // Windows are shown on the one UI thread
        UiDispatcher.Current.RunOnUiAsync(ShowAction).FireAndLog("Show a recipe editor window", Log);
    }
}
