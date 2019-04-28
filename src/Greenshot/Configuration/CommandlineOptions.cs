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
using CommandLine;

namespace Greenshot.Configuration
{
    /// <summary>
    /// The commandline options for Greenshot
    /// </summary>
    public class CommandlineOptions
    {
        /// <summary>
        /// A commandline setting to define what files to open
        /// </summary>
        [Option('f', "file", HelpText = "Files to open.")]
        public IEnumerable<string> ImageFiles { get; set; }

        /// <summary>
        /// A commandline setting to define that we need to exit
        /// </summary>
        [Option(Default = false, HelpText = "Exit the running Greenshot instance.")]
        public bool Exit { get; set; }

        /// <summary>
        /// A commandline setting to define that a reload is needed
        /// </summary>
        [Option(Default = false, HelpText = "Reload configuration.")]
        public bool Reload { get; set; }

        /// <summary>
        /// A commandline setting to define the language
        /// </summary>
        [Option('l', "language", Default = "en-US", HelpText = "Language to use.")]
        public string Language { get; set; }

        /// <summary>
        /// A commandline setting to define the directory to use for the configuration files
        /// </summary>
        [Option('i', "inidirectory", HelpText = "The directory to use to locate the ini file(s). This can be used to use Greenshot with different profiles.")]
        public string IniDirectory { get; set; }

        /// <summary>
        /// A commandline setting to define verbose
        /// </summary>
        [Option(Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }
    }
}
