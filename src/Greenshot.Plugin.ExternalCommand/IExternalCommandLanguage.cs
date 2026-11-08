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
        /// {0} comes with Greenshot: it can be exported, but not changed or removed.
        /// </summary>
        string BuiltInHint { get; }

        /// <summary>
        /// Nothing
        /// </summary>
        string ChoiceClipboardNone { get; }

        /// <summary>
        /// The output
        /// </summary>
        string ChoiceClipboardOutput { get; }

        /// <summary>
        /// The link in the output
        /// </summary>
        string ChoiceClipboardUrl { get; }

        /// <summary>
        /// The saved file, else a new one
        /// </summary>
        string ChoiceFileLastExport { get; }

        /// <summary>
        /// A new file
        /// </summary>
        string ChoiceFileNew { get; }

        /// <summary>
        /// No file
        /// </summary>
        string ChoiceFileNone { get; }

        /// <summary>
        /// The image
        /// </summary>
        string ChoiceInputImage { get; }

        /// <summary>
        /// Nothing
        /// </summary>
        string ChoiceInputNone { get; }

        /// <summary>
        /// A text
        /// </summary>
        string ChoiceInputText { get; }

        /// <summary>
        /// The changed file (only in recipes)
        /// </summary>
        string ChoiceOutputFile { get; }

        /// <summary>
        /// A new image (only in recipes)
        /// </summary>
        string ChoiceOutputImage { get; }

        /// <summary>
        /// Not read
        /// </summary>
        string ChoiceOutputNone { get; }

        /// <summary>
        /// Text, a link in it is the link of the notification
        /// </summary>
        string ChoiceOutputText { get; }

        /// <summary>
        /// Configure external commands
        /// </summary>
        string ContextmenuConfigure { get; }

        /// <summary>
        /// The command can't be saved: {0}
        /// </summary>
        string EditorFailed { get; }

        /// <summary>
        /// Save
        /// </summary>
        string EditorSave { get; }

        /// <summary>
        /// Edit {0}
        /// </summary>
        string EditorTitleEdit { get; }

        /// <summary>
        /// New command
        /// </summary>
        string EditorTitleNew { get; }

        /// <summary>
        /// A command in the background can't give a new image or change the file.
        /// </summary>
        string ErrorBackgroundImage { get; }

        /// <summary>
        /// Only a text output can go to the clipboard.
        /// </summary>
        string ErrorClipboardWithoutText { get; }

        /// <summary>
        /// The file is too large for a command.
        /// </summary>
        string ErrorFileTooLarge { get; }

        /// <summary>
        /// The command is of version {0}, which needs a newer Greenshot.
        /// </summary>
        string ErrorNewerVersion { get; }

        /// <summary>
        /// The command has no name.
        /// </summary>
        string ErrorNoName { get; }

        /// <summary>
        /// The command has no program.
        /// </summary>
        string ErrorNoProgram { get; }

        /// <summary>
        /// This is not a Greenshot command file.
        /// </summary>
        string ErrorNotACommand { get; }

        /// <summary>
        /// The output "the changed file" needs a file.
        /// </summary>
        string ErrorOutputFileWithoutFile { get; }

        /// <summary>
        /// The program can't be used: {0}
        /// </summary>
        string ErrorProgram { get; }

        /// <summary>
        /// {0} can't be "{1}".
        /// </summary>
        string ErrorValue { get; }

        /// <summary>
        /// The command couldn't be exported: {0}
        /// </summary>
        string ExportFailed { get; }

        /// <summary>
        /// Export a command
        /// </summary>
        string ExportTitle { get; }

        /// <summary>
        /// Import
        /// </summary>
        string ImportButton { get; }

        /// <summary>
        /// {0} can't be imported: {1}
        /// </summary>
        string ImportFailed { get; }

        /// <summary>
        /// Greenshot commands (*.gscommand.json)|*.gscommand.json|All files (*.*)|*.*
        /// </summary>
        string ImportFileFilter { get; }

        /// <summary>
        /// Import the command {0}?  Greenshot will run this program with your captures: {1}
        /// </summary>
        string ImportQuestion { get; }

        /// <summary>
        /// It replaces the command with the same name.
        /// </summary>
        string ImportReplaces { get; }

        /// <summary>
        /// The program also gets this text: {0}
        /// </summary>
        string ImportStandardInput { get; }

        /// <summary>
        /// Import a command
        /// </summary>
        string ImportTitle { get; }

        /// <summary>
        /// Only import commands from someone you trust: the program runs with your rights.
        /// </summary>
        string ImportTrust { get; }

        /// <summary>
        /// Arguments
        /// </summary>
        string LabelArgument { get; }

        /// <summary>
        /// ${export.file} is the file of the capture, quote it: "${export.file}"
        /// </summary>
        string LabelArgumentsHint { get; }

        /// <summary>
        /// Choose the program
        /// </summary>
        string LabelBrowse { get; }

        /// <summary>
        /// Commands
        /// </summary>
        string LabelCommands { get; }

        /// <summary>
        /// Clipboard
        /// </summary>
        string LabelCopyToClipboard { get; }

        /// <summary>
        /// Encoding
        /// </summary>
        string LabelEncoding { get; }

        /// <summary>
        /// File
        /// </summary>
        string LabelFile { get; }

        /// <summary>
        /// Name
        /// </summary>
        string LabelName { get; }

        /// <summary>
        /// Output
        /// </summary>
        string LabelOutput { get; }

        /// <summary>
        /// Image format
        /// </summary>
        string LabelOutputimageformat { get; }

        /// <summary>
        /// Program
        /// </summary>
        string LabelProgram { get; }

        /// <summary>
        /// Run in background
        /// </summary>
        string LabelRunInBackground { get; }

        /// <summary>
        /// Standard input
        /// </summary>
        string LabelStandardInput { get; }

        /// <summary>
        /// Text
        /// </summary>
        string LabelStandardInputText { get; }

        /// <summary>
        /// Success exit codes
        /// </summary>
        string LabelSuccessExitCodes { get; }

        /// <summary>
        /// Timeout (seconds, 0: none)
        /// </summary>
        string LabelTimeout { get; }

        /// <summary>
        /// Working folder
        /// </summary>
        string LabelWorkingDirectory { get; }

        /// <summary>
        /// No commands yet: add one, or import a command file.
        /// </summary>
        string NoCommands { get; }

        /// <summary>
        /// Remove the command {0}?
        /// </summary>
        string RemoveQuestion { get; }

        /// <summary>
        /// New and removed commands show up in the menus after Greenshot is restarted, a changed command is used at once.
        /// </summary>
        string RestartHint { get; }

        /// <summary>
        /// Delete
        /// </summary>
        string SettingsDelete { get; }

        /// <summary>
        /// Greenshot hands your capture to these programs. Each command is a destination, recipes use it by its name.
        /// </summary>
        string SettingsDescription { get; }

        /// <summary>
        /// Edit
        /// </summary>
        string SettingsEdit { get; }

        /// <summary>
        /// Export...
        /// </summary>
        string SettingsExport { get; }

        /// <summary>
        /// Import...
        /// </summary>
        string SettingsImport { get; }

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

        /// <summary>
        /// (can't be read)
        /// </summary>
        string Unreadable { get; }
    }
}
