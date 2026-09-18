/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Drawing;
using System.IO;
using Greenshot.Base.Core;
using Dapplo.Ini;

namespace Greenshot.Plugin.ExternalCommand;

public static class IconCache
{
    private static IExternalCommandConfiguration Config
    {
        get
        {
            try
            {
                return IniConfigRegistry.GetSection<IExternalCommandConfiguration>();
            }
            catch
            {
                return null;
            }
        }
    }
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(IconCache));

    public static Image IconForCommand(string commandName)
    {
        if (string.IsNullOrEmpty(commandName))
        {
            return null;
        }

        var configuration = Config;
        string rawCommandLine = null;
        if (configuration?.Commandline != null && configuration.Commandline.TryGetValue(commandName, out var cmdLine))
        {
            rawCommandLine = cmdLine;
        }

        string expanded = null;
        if (!string.IsNullOrWhiteSpace(rawCommandLine))
        {
            try
            {
                expanded = FilenameHelper.FillVariables(rawCommandLine, true);
                expanded = FilenameHelper.FillCmdVariables(expanded, true);
            }
            catch (Exception ex)
            {
                LOG.Warn("Problem expanding command line variables for " + rawCommandLine, ex);
            }
        }

        Image icon = null;

        // 1. Try loading from executable if path exists
        string exePath = expanded;
        if (!string.IsNullOrEmpty(exePath) && !File.Exists(exePath))
        {
            exePath = PluginUtils.GetExePath(exePath);
        }

        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
        {
            try
            {
                icon = PluginUtils.GetCachedExeIcon(exePath, 0);
            }
            catch (Exception ex)
            {
                LOG.Warn("Problem loading icon for " + exePath, ex);
            }
        }

        // 2. Fallback: try Windows App logo (for AppExecutionAliases, packaged apps, or store apps)
        if (icon == null)
        {
            try
            {
                icon = WindowsAppHelper.GetAppLogo(expanded ?? rawCommandLine, commandName);
            }
            catch (Exception ex)
            {
                LOG.Warn("Problem loading Windows App icon for " + commandName, ex);
            }
        }

        return icon;
    }
}