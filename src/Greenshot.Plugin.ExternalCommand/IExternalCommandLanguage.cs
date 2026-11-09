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
        /// Browse...
        /// </summary>
        string AppPickerBrowse { get; }

        /// <summary>
        /// Choose
        /// </summary>
        string AppPickerChoose { get; }

        /// <summary>
        /// The apps which open images, and the programs Windows knows by name. Not in the list? Browse for the program.
        /// </summary>
        string AppPickerHint { get; }

        /// <summary>
        /// No app found, browse for the program.
        /// </summary>
        string AppPickerNone { get; }

        /// <summary>
        /// Search
        /// </summary>
        string AppPickerSearch { get; }

        /// <summary>
        /// Choose an app
        /// </summary>
        string AppPickerTitle { get; }

        /// <summary>
        /// A command file Greenshot wrote which was changed outside Greenshot is only used after you approve it: Greenshot asks when it starts, or approve it here. A file copied into the Commands folder is ignored, import it.
        /// </summary>
        string ApprovalHint { get; }

        /// <summary>
        /// Approve
        /// </summary>
        string ApproveButton { get; }

        /// <summary>
        /// Not now
        /// </summary>
        string ApproveLater { get; }

        /// <summary>
        /// The command {0} was added or changed outside Greenshot. Use it?\n\nGreenshot will run this program with your captures:\n{1}
        /// </summary>
        string ApproveQuestion { get; }

        /// <summary>
        /// Approve a command
        /// </summary>
        string ApproveTitle { get; }

        /// <summary>
        /// {0} comes with Greenshot: it can be exported, but not changed or removed.
        /// </summary>
        string BuiltInHint { get; }

        /// <summary>
        /// Copy the link it prints
        /// </summary>
        string ChoiceAfterCopyLink { get; }

        /// <summary>
        /// Copy what it prints
        /// </summary>
        string ChoiceAfterCopyOutput { get; }

        /// <summary>
        /// Nothing
        /// </summary>
        string ChoiceAfterNothing { get; }

        /// <summary>
        /// Show a link it prints in the notification
        /// </summary>
        string ChoiceAfterNotification { get; }

        /// <summary>
        /// The capture, for a tool which reads it when it gets "-" instead of a file name
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
        /// A Windows App only gets the file: it runs in the background, without input or output.
        /// </summary>
        string ErrorWindowsApp { get; }

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
        /// Import the command {0}?\n\nGreenshot will run this program with your captures:\n{1}
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
        /// For command-line tools
        /// </summary>
        string LabelAdvanced { get; }

        /// <summary>
        /// Afterwards
        /// </summary>
        string LabelAfterwards { get; }

        /// <summary>
        /// Arguments
        /// </summary>
        string LabelArgument { get; }

        /// <summary>
        /// Greenshot saves the capture to a file and puts its path where ${export.file} is, keep the quotes: "${export.file}". Without ${export.file} no file is written.
        /// </summary>
        string LabelArgumentsHint { get; }

        /// <summary>
        /// Choose app...
        /// </summary>
        string LabelChooseApp { get; }

        /// <summary>
        /// Commands
        /// </summary>
        string LabelCommands { get; }

        /// <summary>
        /// Encoding of what it prints
        /// </summary>
        string LabelEncoding { get; }

        /// <summary>
        /// Empty means UTF-8. oem for old console tools, or the name of an encoding, e.g. windows-1252.
        /// </summary>
        string LabelEncodingHint { get; }

        /// <summary>
        /// Use the file you saved last instead of a new one, when there is one
        /// </summary>
        string LabelLastSavedFile { get; }

        /// <summary>
        /// Name
        /// </summary>
        string LabelName { get; }

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
        /// Greenshot doesn't wait for the program to close, for an app which stays open like an image editor. What it prints isn't read.
        /// </summary>
        string LabelRunInBackgroundHint { get; }

        /// <summary>
        /// Send to the program's input (standard input)
        /// </summary>
        string LabelStandardInput { get; }

        /// <summary>
        /// Text
        /// </summary>
        string LabelStandardInputText { get; }

        /// <summary>
        /// The program reads this text from its input; nothing is quoted or split, so it is the place for values with spaces or quotes. Variables work, e.g. ${payload.title}; ${payload.ocr.text} only has the text in a recipe, after its OCR step.
        /// </summary>
        string LabelStandardInputTextHint { get; }

        /// <summary>
        /// Exit codes which mean success
        /// </summary>
        string LabelSuccessExitCodes { get; }

        /// <summary>
        /// Empty means 0. Several are separated by commas, e.g. 0,1. Any other exit code is shown as a failure.
        /// </summary>
        string LabelSuccessExitCodesHint { get; }

        /// <summary>
        /// Timeout (seconds, 0: none)
        /// </summary>
        string LabelTimeout { get; }

        /// <summary>
        /// Working folder
        /// </summary>
        string LabelWorkingDirectory { get; }

        /// <summary>
        /// Empty: the folder of the capture file. Only needed for a program which looks for its own files in the current folder.
        /// </summary>
        string LabelWorkingDirectoryHint { get; }

        /// <summary>
        /// No commands yet: add one, or import a command file.
        /// </summary>
        string NoCommands { get; }

        /// <summary>
        /// Not approved: {0}
        /// </summary>
        string NotApproved { get; }

        /// <summary>
        /// This command gives back a new image or changes the file, which only a recipe uses. From the menu Greenshot doesn't read what it prints.
        /// </summary>
        string RecipeOutputHint { get; }

        /// <summary>
        /// Remove the command {0}?
        /// </summary>
        string RemoveQuestion { get; }

        /// <summary>
        /// New and removed commands show up in the menus after Greenshot is restarted, a changed command is used at once.
        /// </summary>
        string RestartHint { get; }

        /// <summary>
        /// Approve...
        /// </summary>
        string SettingsApprove { get; }

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
