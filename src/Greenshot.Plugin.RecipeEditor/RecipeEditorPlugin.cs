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
using System.Windows;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.RecipeEditor.Views;
using log4net;

namespace Greenshot.Plugin.RecipeEditor;

public class RecipeEditorPlugin : IGreenshotPlugin, IRecipeEditorService
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeEditorPlugin));
    private static IRecipeConfiguration _config;
    private ToolStripMenuItem _itemPlugInConfig;
    private static RecipeEditorWindow _activeRecipeEditorWindow;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public string Name => "RecipeEditor";

    public bool IsConfigurable => true;

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        if (_itemPlugInConfig != null)
        {
            _itemPlugInConfig.Dispose();
            _itemPlugInConfig = null;
        }
    }

    public void RegisterConfiguration(IniConfig iniConfig)
    {
        var section = new RecipeConfigurationImpl();
        iniConfig.AddSection(section);
        _config = section;
    }

    public void RegisterServices(IServiceLocator serviceLocator)
    {
        serviceLocator.AddService<IRecipeEditorService>(this);
    }

    public bool Start()
    {
        if (_config != null && _config.QuicklinkEnabled)
        {
            _itemPlugInConfig = new ToolStripMenuItem
            {
                Text = Language.GetString("contextmenu_recipeeditor") ?? "Recipe Editor...",
                Visible = true
            };
            _itemPlugInConfig.Click += (s, e) => OpenEditor();
            PluginUtils.AddToContextMenu(_itemPlugInConfig);
        }

        return true;
    }

    public void Shutdown()
    {
        Dispose();
    }

    public void Configure()
    {
        OpenEditor();
    }

    public UIElement CreateConfigurationControl()
    {
        return new RecipeEditorConfigurationView(_config);
    }

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

                var dlg = new Dialogs.RecipeManagerDialog(vm)
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

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            ShowAction();
        }
        else
        {
            var staThread = new Thread(new ThreadStart(ShowAction));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
        }
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

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            ShowAction();
        }
        else
        {
            var staThread = new Thread(new ThreadStart(ShowAction));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
        }
    }
}
