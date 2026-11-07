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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Expressions;

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
        CommandlineTemplate.Remove(command);
        ArgumentTemplate.Remove(command);
        RunInbackground.Remove(command);
        OutputFormat.Remove(command);
        RedirectStandardErrorCommand?.Remove(command);
        RedirectStandardOutputCommand?.Remove(command);
        ShowStandardOutputInLogCommand?.Remove(command);
        ParseOutputForUriCommand?.Remove(command);
        OutputToClipboardCommand?.Remove(command);
        UriToClipboardCommand?.Remove(command);

        if (MsPaint.Equals(command) || PaintDotNet.Equals(command))
        {
            if (!DeletedBuildInCommands.Contains(command))
            {
                DeletedBuildInCommands.Add(command);
            }
        }
        MarkAsDirty();
    }

    public void OnAfterLoad()
    {
        Commands ??= new List<string>();
        Commandline ??= new Dictionary<string, string>();
        Argument ??= new Dictionary<string, string>();
        CommandlineTemplate ??= new Dictionary<string, string>();
        ArgumentTemplate ??= new Dictionary<string, string>();
        // The templates are filled once from the command lines of 1.3, which stay for going back to 1.3
        foreach (var pair in Commandline.Where(p => !CommandlineTemplate.ContainsKey(p.Key)).ToList())
        {
            CommandlineTemplate[pair.Key] = LegacyTemplateConverter.Convert(pair.Value, true);
            MarkAsDirty();
        }
        foreach (var pair in Argument.Where(p => !ArgumentTemplate.ContainsKey(p.Key)).ToList())
        {
            ArgumentTemplate[pair.Key] = LegacyTemplateConverter.ConvertArguments(pair.Value);
            MarkAsDirty();
        }
        RunInbackground ??= new Dictionary<string, bool>();
        OutputFormat ??= new Dictionary<string, string>();
        RedirectStandardErrorCommand ??= new Dictionary<string, bool>();
        RedirectStandardOutputCommand ??= new Dictionary<string, bool>();
        ShowStandardOutputInLogCommand ??= new Dictionary<string, bool>();
        ParseOutputForUriCommand ??= new Dictionary<string, bool>();
        OutputToClipboardCommand ??= new Dictionary<string, bool>();
        UriToClipboardCommand ??= new Dictionary<string, bool>();
        DeletedBuildInCommands ??= new List<string>();

        // Check if we need to add MsPaint
        if (HasPaint && !Commands.Contains(MsPaint) && !DeletedBuildInCommands.Contains(MsPaint))
        {
            Commands.Add(MsPaint);
            CommandlineTemplate[MsPaint] = PaintPath;
            ArgumentTemplate[MsPaint] = "\"${export.file}\"";
            RunInbackground.Add(MsPaint, true);
            MarkAsDirty();
        }

        // Check if we need to add Paint.NET
        if (HasPaintDotNet && !Commands.Contains(PaintDotNet) && !DeletedBuildInCommands.Contains(PaintDotNet))
        {
            Commands.Add(PaintDotNet);
            CommandlineTemplate[PaintDotNet] = PaintDotNetPath;
            ArgumentTemplate[PaintDotNet] = "\"${export.file}\"";
            RunInbackground.Add(PaintDotNet, true);
            MarkAsDirty();
        }
    }
}
