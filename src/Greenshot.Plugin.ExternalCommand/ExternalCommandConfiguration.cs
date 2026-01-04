/*
 * Greenshot - a free and open source screenshot tool
 * Copyright © 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.IniFile;

namespace Greenshot.Plugin.ExternalCommand
{
    /// <summary>
    /// Description of FlickrConfiguration.
    /// </summary>
    [IniSection("ExternalCommand", Description = "Greenshot ExternalCommand Plugin configuration")]
    public class ExternalCommandConfiguration : IniSection
    {
        [IniProperty("Commands", Description = "The commands that are available.")]
        public List<string> Commands { get; set; }

        [IniProperty("RedirectStandardError", Description = "Redirect the standard error of all external commands, used to output as warning to the greenshot.log.",
            DefaultValue = "true")]
        public bool RedirectStandardError { get; set; }

        [IniProperty("RedirectStandardOutput", Description = "Redirect the standard output of all external commands, used for different other functions (more below).",
            DefaultValue = "true")]
        public bool RedirectStandardOutput { get; set; }

        [IniProperty("ShowStandardOutputInLog",
            Description = "Depends on 'RedirectStandardOutput': Show standard output of all external commands to the Greenshot log, this can be usefull for debugging.",
            DefaultValue = "false")]
        public bool ShowStandardOutputInLog { get; set; }

        [IniProperty("ParseForUri",
            Description = "Depends on 'RedirectStandardOutput': Parse the output and take the first found URI, if a URI is found than clicking on the notify bubble goes there.",
            DefaultValue = "true")]
        public bool ParseOutputForUri { get; set; }

        [IniProperty("OutputToClipboard", Description = "Depends on 'RedirectStandardOutput': Place the standard output on the clipboard.", DefaultValue = "false")]
        public bool OutputToClipboard { get; set; }

        [IniProperty("UriToClipboard",
            Description =
                "Depends on 'RedirectStandardOutput' & 'ParseForUri': If an URI is found in the standard input, place it on the clipboard. (This overwrites the output from OutputToClipboard setting.)",
            DefaultValue = "true")]
        public bool UriToClipboard { get; set; }

        [IniProperty("Commandline", Description = "The commandline for the output command.")]
        public Dictionary<string, string> Commandline { get; set; }

        [IniProperty("Argument", Description = "The arguments for the output command.")]
        public Dictionary<string, string> Argument { get; set; }

        [IniProperty("RunInbackground", Description = "Should the command be started in the background.")]
        public Dictionary<string, bool> RunInbackground { get; set; }

        [IniProperty("DeletedBuildInCommands", Description = "If a build in command was deleted manually, it should not be recreated.")]
        public List<string> DeletedBuildInCommands { get; set; }

        private const string MsPaint = "MS Paint";
        private static readonly string PaintPath;
        private static readonly bool HasPaint;

        private const string PaintDotNet = "Paint.NET";
        private static readonly string PaintDotNetPath;
        private static readonly bool HasPaintDotNet;

        static ExternalCommandConfiguration()
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

        /// <summary>
        /// Delete the configuration for the specified command
        /// </summary>
        /// <param name="command">string with command</param>
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
                }
            }
        }

        public override void AfterLoad()
        {
            base.AfterLoad();

            // Check if we need to add MsPaint
            if (HasPaint && !Commands.Contains(MsPaint) && !DeletedBuildInCommands.Contains(MsPaint))
            {
                Commands.Add(MsPaint);
                Commandline.Add(MsPaint, PaintPath);
                Argument.Add(MsPaint, "\"{0}\"");
                RunInbackground.Add(MsPaint, true);
            }

            // Check if we need to add Paint.NET
            if (HasPaintDotNet && !Commands.Contains(PaintDotNet) && !DeletedBuildInCommands.Contains(PaintDotNet))
            {
                Commands.Add(PaintDotNet);
                Commandline.Add(PaintDotNet, PaintDotNetPath);
                Argument.Add(PaintDotNet, "\"{0}\"");
                RunInbackground.Add(PaintDotNet, true);
            }
        }

        /// <summary>
        /// Supply values we can't put as defaults
        /// </summary>
        /// <param name="property">The property to return a default for</param>
        /// <returns>object with the default value for the supplied property</returns>
        public override object GetDefault(string property) =>
            property switch
            {
                nameof(DeletedBuildInCommands) => (object) new List<string>(),
                nameof(Commands) => new List<string>(),
                nameof(Commandline) => new Dictionary<string, string>(),
                nameof(Argument) => new Dictionary<string, string>(),
                nameof(RunInbackground) => new Dictionary<string, bool>(),
                _ => null
            };
    }
}