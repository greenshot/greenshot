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
using System.Collections.Generic;
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;

namespace Greenshot.Plugin.ExternalCommand;

public partial class ExternalCommandConfigurationImpl : IExternalCommandConfiguration
{
    private const string MsPaint = "MS Paint";
    private static readonly string PaintPath;
    private static readonly bool HasPaint;

    private const string PaintDotNet = "Paint.NET";
    private static readonly string PaintDotNetPath;
    private static readonly bool HasPaintDotNet;

    static ExternalCommandConfigurationImpl()
    {
        try
        {
            PaintPath = PluginUtils.GetExePath("pbrush.exe");
            HasPaint = !string.IsNullOrEmpty(PaintPath) && File.Exists(PaintPath);
        }
        catch
        {
            // Ignore
        }

        try
        {
            PaintDotNetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Paint.NET\PaintDotNet.exe");
            HasPaintDotNet = !string.IsNullOrEmpty(PaintDotNetPath) && File.Exists(PaintDotNetPath);
        }
        catch
        {
            // Ignore
        }
    }

    public void Delete(string command)
    {
        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        Commands.Remove(command);
        Commandline.Remove(command);
        Argument.Remove(command);
        RunInbackground.Remove(command);

        if (MsPaint.Equals(command) || PaintDotNet.Equals(command))
        {
            if (!DeletedBuildInCommands.Contains(command))
            {
                DeletedBuildInCommands.Add(command);
                // Re-assign to trigger SetRawValue dirty tracking for the in-place Add
                DeletedBuildInCommands = DeletedBuildInCommands;
            }
        }
        MarkAsDirty();
    }

    public void OnAfterLoad()
    {
        Commands ??= new List<string>();
        Commandline ??= new Dictionary<string, string>();
        Argument ??= new Dictionary<string, string>();
        RunInbackground ??= new Dictionary<string, bool>();
        OutputFormat ??= new Dictionary<string, OutputFormat>();
        DeletedBuildInCommands ??= new List<string>();

        // Check if we need to add MsPaint
        if (HasPaint && !Commands.Contains(MsPaint) && !DeletedBuildInCommands.Contains(MsPaint))
        {
            Commands.Add(MsPaint);
            Commandline.Add(MsPaint, PaintPath);
            Argument.Add(MsPaint, "\"{0}\"");
            RunInbackground.Add(MsPaint, true);
            MarkAsDirty();
        }

        // Check if we need to add Paint.NET
        if (HasPaintDotNet && !Commands.Contains(PaintDotNet) && !DeletedBuildInCommands.Contains(PaintDotNet))
        {
            Commands.Add(PaintDotNet);
            Commandline.Add(PaintDotNet, PaintDotNetPath);
            Argument.Add(PaintDotNet, "\"{0}\"");
            RunInbackground.Add(PaintDotNet, true);
            MarkAsDirty();
        }
    }
}
