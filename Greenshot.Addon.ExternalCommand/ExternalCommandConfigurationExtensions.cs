#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.IO;
using GreenshotPlugin.Core;

namespace GreenshotExternalCommandPlugin
{
    public static class ExternalCommandConfigurationExtensions
    {
        private const string MsPaint = "MS Paint";

        private const string PaintDotNet = "Paint.NET";
        private static readonly string PaintPath;
        private static readonly bool HasPaint;
        private static readonly string PaintDotNetPath;
        private static readonly bool HasPaintDotNet;

        static ExternalCommandConfigurationExtensions()
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
        ///     Delete the configuration for the specified command
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="command">string with command</param>
        public static void Delete(this IExternalCommandConfiguration configuration, string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return;
            }
            configuration.Commands.Remove(command);
            configuration.Commandline.Remove(command);
            configuration.Argument.Remove(command);
            configuration.RunInbackground.Remove(command);
            if (!MsPaint.Equals(command) && !PaintDotNet.Equals(command))
            {
                return;
            }

            if (!configuration.DeletedBuildInCommands.Contains(command))
            {
                configuration.DeletedBuildInCommands.Add(command);
            }
        }

        /// <summary>
        /// Fix some settings after loading
        /// </summary>
        /// <param name="configuration"></param>
        public static void AfterLoad(this IExternalCommandConfiguration configuration)
        {
            // Check if we need to add MsPaint
            if (HasPaint && !configuration.Commands.Contains(MsPaint) && !configuration.DeletedBuildInCommands.Contains(MsPaint))
            {
                configuration.Commands.Add(MsPaint);
                configuration.Commandline.Add(MsPaint, PaintPath);
                configuration.Argument.Add(MsPaint, "\"{0}\"");
                configuration.RunInbackground.Add(MsPaint, true);
            }

            // Check if we need to add Paint.NET
            if (!HasPaintDotNet || configuration.Commands.Contains(PaintDotNet) ||
                configuration.DeletedBuildInCommands.Contains(PaintDotNet))
            {
                return;
            }

            configuration.Commands.Add(PaintDotNet);
            configuration.Commandline.Add(PaintDotNet, PaintDotNetPath);
            configuration.Argument.Add(PaintDotNet, "\"{0}\"");
            configuration.RunInbackground.Add(PaintDotNet, true);
        }
    }
}
