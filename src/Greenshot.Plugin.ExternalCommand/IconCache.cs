/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Expressions;

namespace Greenshot.Plugin.ExternalCommand;

public static class IconCache
{
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(IconCache));

    /// <summary>
    /// The icon of the command (cached, don't dispose it): the icon of the executable or the logo of the Windows App.
    /// </summary>
    /// <param name="rawCommandLine">the program of the command, a template</param>
    /// <param name="commandName">the name of the command</param>
    /// <param name="cancellationToken">CancellationToken</param>
    public static async Task<Image> IconForCommandAsync(string rawCommandLine, string commandName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(commandName) && string.IsNullOrEmpty(rawCommandLine))
        {
            return null;
        }

        string expanded = null;
        if (!string.IsNullOrWhiteSpace(rawCommandLine))
        {
            try
            {
                expanded = OutputTemplates.RenderCommand(rawCommandLine, VariableScope.Empty, out _);
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
                icon = await PluginUtils.GetCachedExeIconAsync(exePath, 0, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LOG.Warn("Problem loading icon for " + exePath, ex);
            }
        }

        // 2. Fallback: try Windows App logo (for AppExecutionAliases, packaged apps, or store apps)
        if (icon == null)
        {
            try
            {
                icon = await WindowsAppHelper.GetAppLogoAsync(expanded ?? rawCommandLine, commandName, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LOG.Warn("Problem loading Windows App icon for " + commandName, ex);
            }
        }

        return icon;
    }
}
