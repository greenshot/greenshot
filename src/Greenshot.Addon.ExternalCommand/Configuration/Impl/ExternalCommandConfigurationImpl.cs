// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using Dapplo.Config.Ini;
using Greenshot.Addon.ExternalCommand.Entities;
using Greenshot.Core.Enums;

namespace Greenshot.Addon.ExternalCommand.Configuration.Impl
{
    internal class ExternalCommandConfigurationImpl : IniSectionBase<IExternalCommandConfiguration>, IExternalCommandConfiguration
    {
        public override void AfterLoad()
        {
            ExternalCommandConfigurationExtensions.AfterLoad(this);
            base.AfterLoad();
        }

        public string OutputFilePath { get; set; }
        public bool OutputFileAllowOverwrite { get; set; }
        public string OutputFileFilenamePattern { get; set; }
        public OutputFormats OutputFileFormat { get; set; }
        public bool OutputFileReduceColors { get; set; }
        public bool OutputFileAutoReduceColors { get; set; }
        public int OutputFileReduceColorsTo { get; set; }
        public int OutputFileJpegQuality { get; set; }
        public bool OutputFilePromptQuality { get; set; }
        public uint OutputFileIncrementingNumber { get; set; }
        public string OptimizePNGCommand { get; set; }
        public string OptimizePNGCommandArguments { get; set; }

        public bool UseOwnSettings { get; set; }

        public IList<string> Commands { get; set; }
        public IDictionary<string, string> Commandline { get; set; }
        public IDictionary<string, string> Argument { get; set; }
        public IDictionary<string, bool> RunInbackground { get; set; }
        public IDictionary<string, CommandBehaviors> Behaviors { get; set; }
        public IList<string> DeletedBuildInCommands { get; set; }
    }
}
