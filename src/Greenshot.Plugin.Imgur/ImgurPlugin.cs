/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.Imgur;

/// <summary>
/// This is the ImgurPlugin code
/// </summary>
public class ImgurPlugin : IGreenshotPlugin
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ImgurPlugin));
    private static ImgurConfiguration _config;
    private ComponentResourceManager _resources;
    private ToolStripMenuItem _historyMenuItem;
    private ToolStripMenuItem _itemPlugInConfig;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        if (_historyMenuItem != null)
        {
            _historyMenuItem.Dispose();
            _historyMenuItem = null;
        }

        if (_itemPlugInConfig != null)
        {
            _itemPlugInConfig.Dispose();
            _itemPlugInConfig = null;
        }
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "Imgur";

    /// <summary>
    /// Specifies if the plugin can be configured
    /// </summary>
    public bool IsConfigurable => true;

    /// <summary>
    /// Implementation of the IGreenshotPlugin.Initialize
    /// </summary>
    /// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
    public bool Initialize()
    {
        // Get configuration
        _config = IniConfig.GetIniSection<ImgurConfiguration>();
        _resources = new ComponentResourceManager(typeof(ImgurPlugin));

        ToolStripMenuItem itemPlugInRoot = new ToolStripMenuItem("Imgur")
        {
            Image = (Image) _resources.GetObject("Imgur")
        };

        _itemPlugInConfig = new ToolStripMenuItem(Language.GetString("imgur", LangKey.configure));
        _itemPlugInConfig.Click += delegate { _config.ShowConfigDialog(); };
        itemPlugInRoot.DropDownItems.Add(_itemPlugInConfig);

        PluginUtils.AddToContextMenu(itemPlugInRoot);
        Language.LanguageChanged += OnLanguageChanged;

        // Enable history if there are items available
        UpdateHistoryMenuItem();
        return true;
    }

    public void OnLanguageChanged(object sender, EventArgs e)
    {
        if (_itemPlugInConfig != null)
        {
            _itemPlugInConfig.Text = Language.GetString("imgur", LangKey.configure);
        }

        if (_historyMenuItem != null)
        {
            _historyMenuItem.Text = Language.GetString("imgur", LangKey.history);
        }
    }

    private void UpdateHistoryMenuItem()
    {
        if (_historyMenuItem == null)
        {
            return;
        }

        try
        {
            var form = SimpleServiceProvider.Current.GetInstance<Form>();
            form.BeginInvoke((MethodInvoker) delegate
            {
                var historyMenuItem = _historyMenuItem;
                if (historyMenuItem == null)
                {
                    return;
                }

                if (_config?.ImgurUploadHistory != null && _config.ImgurUploadHistory.Count > 0)
                {
                    historyMenuItem.Enabled = true;
                }
                else
                {
                    historyMenuItem.Enabled = false;
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error("Error loading history", ex);
        }
    }

    public virtual void Shutdown()
    {
        Log.Debug("Imgur Plugin shutdown.");
        Language.LanguageChanged -= OnLanguageChanged;
    }

    /// <summary>
    /// Implementation of the IPlugin.Configure
    /// </summary>
    public virtual void Configure()
    {
        _config.ShowConfigDialog();
    }
}