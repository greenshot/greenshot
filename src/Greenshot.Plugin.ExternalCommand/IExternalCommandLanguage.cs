/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Plugin.ExternalCommand
{
    /// <summary>
    /// The texts of the [ExternalCommand] section of greenshot.externalcommand.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("ExternalCommand", ModuleName = "externalcommand")]
    public interface IExternalCommandLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Configure external commands
        /// </summary>
        string ContextmenuConfigure { get; }

        /// <summary>
        /// Arguments
        /// </summary>
        string LabelArgument { get; }

        /// <summary>
        /// Command
        /// </summary>
        string LabelCommand { get; }

        /// <summary>
        /// {0} is the filename of your screenshot
        /// </summary>
        string LabelInformation { get; }

        /// <summary>
        /// Name
        /// </summary>
        string LabelName { get; }

        /// <summary>
        /// Image format
        /// </summary>
        string LabelOutputimageformat { get; }

        /// <summary>
        /// Copy standard output to clipboard
        /// </summary>
        string LabelOutputToClipboard { get; }

        /// <summary>
        /// Parse output for URI and open on notification click
        /// </summary>
        string LabelParseOutputForUri { get; }

        /// <summary>
        /// Redirect standard error to Greenshot log (as warning)
        /// </summary>
        string LabelRedirectStandardError { get; }

        /// <summary>
        /// Redirect standard output
        /// </summary>
        string LabelRedirectStandardOutput { get; }

        /// <summary>
        /// Run in background
        /// </summary>
        string LabelRunInBackground { get; }

        /// <summary>
        /// Show standard output in Greenshot log
        /// </summary>
        string LabelShowOutputInLog { get; }

        /// <summary>
        /// Copy found URI to clipboard
        /// </summary>
        string LabelUriToClipboard { get; }

        /// <summary>
        /// Delete
        /// </summary>
        string SettingsDelete { get; }

        /// <summary>
        /// Configure command
        /// </summary>
        string SettingsDetailTitle { get; }

        /// <summary>
        /// Edit
        /// </summary>
        string SettingsEdit { get; }

        /// <summary>
        /// General settings
        /// </summary>
        string SettingsGeneralTitle { get; }

        /// <summary>
        /// Select a command to edit details, or click New to add one.
        /// </summary>
        string SettingsInstructions { get; }

        /// <summary>
        /// New
        /// </summary>
        string SettingsNew { get; }

        /// <summary>
        /// External command settings
        /// </summary>
        string SettingsTitle { get; }

        /// <summary>
        /// A command with this name already exists. Please choose a different name.
        /// </summary>
        string TooltipDuplicateName { get; }
    }
}
