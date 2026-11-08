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

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;

namespace Greenshot.Plugin.ExternalCommand;

[IniSection("ExternalCommand")]
[Description("Greenshot ExternalCommand Plugin configuration")]
public interface IExternalCommandConfiguration : IIniSection, IAfterLoad
{
    [Description("The external commands, each encrypted. Change them in the settings, they can't be edited here.")]
    Dictionary<string, string> Definitions { get; set; }

    [Description("True when the commands of Greenshot 1.3 (the keys below) were converted into Definitions; they are only read once and kept for going back to 1.3.")]
    bool DefinitionsConverted { get; set; }

    [Description("Greenshot 1.3: the commands that are available.")]
    List<string> Commands { get; set; }

    [Description("Greenshot 1.3: redirect the standard error of all external commands.")]
    [DefaultValue(true)]
    bool RedirectStandardError { get; set; }

    [Description("Greenshot 1.3: redirect the standard output of all external commands.")]
    [DefaultValue(true)]
    bool RedirectStandardOutput { get; set; }

    [Description("Greenshot 1.3: show the standard output of all external commands in the log.")]
    [DefaultValue(false)]
    bool ShowStandardOutputInLog { get; set; }

    [DataMember(Name = "ParseForUri")]
    [Description("Greenshot 1.3: parse the output for a URI.")]
    [DefaultValue(true)]
    bool ParseOutputForUri { get; set; }

    [Description("Greenshot 1.3: place the standard output on the clipboard.")]
    [DefaultValue(false)]
    bool OutputToClipboard { get; set; }

    [Description("Greenshot 1.3: place a URI found in the output on the clipboard.")]
    [DefaultValue(true)]
    bool UriToClipboard { get; set; }

    [Description("Greenshot 1.3: the commandline ({0} for the file).")]
    Dictionary<string, string> Commandline { get; set; }

    [Description("Greenshot 1.3: the arguments ({0} for the file).")]
    Dictionary<string, string> Argument { get; set; }

    [Description("The program of a command before the definitions (early 1.4 builds), only read to convert it.")]
    Dictionary<string, string> CommandlineTemplate { get; set; }

    [Description("The arguments of a command before the definitions (early 1.4 builds), only read to convert it.")]
    Dictionary<string, string> ArgumentTemplate { get; set; }

    [Description("Greenshot 1.3: the output file format per command.")]
    Dictionary<string, string> OutputFormat { get; set; }

    [Description("Greenshot 1.3: start the command in the background.")]
    Dictionary<string, bool> RunInbackground { get; set; }

    [Description("Greenshot 1.3: redirect standard error per command.")]
    Dictionary<string, bool> RedirectStandardErrorCommand { get; set; }

    [Description("Greenshot 1.3: redirect standard output per command.")]
    Dictionary<string, bool> RedirectStandardOutputCommand { get; set; }

    [Description("Greenshot 1.3: show standard output in the log per command.")]
    Dictionary<string, bool> ShowStandardOutputInLogCommand { get; set; }

    [Description("Greenshot 1.3: parse the output for a URI per command.")]
    Dictionary<string, bool> ParseOutputForUriCommand { get; set; }

    [Description("Greenshot 1.3: copy the standard output to the clipboard per command.")]
    Dictionary<string, bool> OutputToClipboardCommand { get; set; }

    [Description("Greenshot 1.3: copy a found URI to the clipboard per command.")]
    Dictionary<string, bool> UriToClipboardCommand { get; set; }

    [Description("The built-in commands (MS Paint, Paint.NET) which were deleted in Greenshot 1.3: they aren't offered.")]
    List<string> DeletedBuildInCommands { get; set; }

    /// <summary>
    /// Mark the section as changed after the dictionaries were changed.
    /// Implemented in <see cref="ExternalCommandConfigurationImpl"/>.
    /// </summary>
    void MarkChanged();
}
