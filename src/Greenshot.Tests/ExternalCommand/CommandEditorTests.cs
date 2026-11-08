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

using Greenshot.Plugin.ExternalCommand.Running;
using Greenshot.Plugin.ExternalCommand.ViewModels;
using Xunit;

namespace Greenshot.Tests.ExternalCommand
{
    /// <summary>
    /// The command editor in plain words: the file follows the arguments, one choice for what happens with what the program
    /// prints, the outputs of recipes are kept; and the open commands of the apps Windows knows
    /// </summary>
    public class CommandEditorTests
    {
        public CommandEditorTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        [InlineData("\"C:\\Program Files\\App\\app.exe\" \"%1\"", "C:\\Program Files\\App\\app.exe", "\"${export.file}\"")]
        [InlineData("C:\\Program Files\\App\\app.exe %1", "C:\\Program Files\\App\\app.exe", "\"${export.file}\"")]
        [InlineData("\"C:\\Tools\\edit.exe\" /edit \"%L\" %*", "C:\\Tools\\edit.exe", "/edit \"${export.file}\"")]
        public void OpenCommand_TheFileBecomesExportFile(string command, string program, string arguments)
        {
            Assert.True(InstalledApps.TryParseOpenCommand(command, out string parsedProgram, out string parsedArguments));
            Assert.Equal(program, parsedProgram);
            Assert.Equal(arguments, parsedArguments);
        }

        [Theory]
        [InlineData("")]
        [InlineData("rundll32.exe")]
        [InlineData("\"C:\\Tools\\viewer.exe\"")]
        [InlineData("\"C:\\Tools\\script.cmd\" \"%1\"")]
        [InlineData("\"C:\\Tools\\viewer.exe\" \"%1\" %Unknown%x")]
        public void OpenCommand_WithoutTheFileOrAProgram_IsNotAnApp(string command)
        {
            Assert.False(InstalledApps.TryParseOpenCommand(command, out _, out _));
        }

        [Fact]
        public void ToDefinition_AFileOnlyWhenTheArgumentsUseIt()
        {
            var editor = new CommandEditorViewModel(new CommandDefinition { Name = "A", Program = "a.exe" }, null);
            Assert.Equal(CommandDefinitions.FileNew, editor.ToDefinition().File);

            editor.UseLastSavedFile = true;
            Assert.Equal(CommandDefinitions.FileLastExport, editor.ToDefinition().File);

            editor.Arguments = "--version";
            Assert.Equal(CommandDefinitions.FileNone, editor.ToDefinition().File);
        }

        [Fact]
        public void Afterwards_IsTheOutputAndTheClipboard()
        {
            var editor = new CommandEditorViewModel(new CommandDefinition { Name = "A", Program = "a.exe" }, null);
            Assert.Equal("Nothing", editor.Afterwards);

            editor.Afterwards = CommandDefinitions.ClipboardUrl;
            var definition = editor.ToDefinition();
            Assert.Equal("Text", definition.Output);
            Assert.Equal(CommandDefinitions.ClipboardUrl, definition.CopyToClipboard);

            editor.Afterwards = "Notification";
            definition = editor.ToDefinition();
            Assert.Equal("Text", definition.Output);
            Assert.Equal(CommandDefinitions.ClipboardNone, definition.CopyToClipboard);

            // Greenshot doesn't wait: nothing is read
            editor.RunInBackground = true;
            Assert.False(editor.CanChooseAfterwards);
            Assert.Equal("None", editor.ToDefinition().Output);
        }

        [Fact]
        public void RecipeOutput_IsKept()
        {
            var editor = new CommandEditorViewModel(new CommandDefinition { Name = "A", Program = "a.exe", Output = "Image" }, null);
            Assert.True(editor.HasRecipeOutput);
            Assert.Null(editor.Afterwards);
            editor.Afterwards = null;
            Assert.Equal("Image", editor.ToDefinition().Output);
        }

        [Fact]
        public void TakeApp_SetsProgramArgumentsAndName_AndDoesntWait()
        {
            var editor = new CommandEditorViewModel(new CommandDefinition { Name = null }, null);
            editor.TakeApp(new InstalledApp("Paint.NET", "C:\\Program Files\\paint.net\\paintdotnet.exe", "\"${export.file}\""));
            Assert.Equal("Paint.NET", editor.Name);
            Assert.Equal("C:\\Program Files\\paint.net\\paintdotnet.exe", editor.Program);
            Assert.True(editor.RunInBackground);
        }
    }
}
