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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Languages;
using Greenshot.Plugin.ExternalCommand.Running;

namespace Greenshot.Plugin.ExternalCommand.ViewModels;

/// <summary>
/// A value of a choice with its text
/// </summary>
public sealed class ChoiceOption
{
    public ChoiceOption(string value, string text)
    {
        Value = value;
        Text = text;
    }

    public string Value { get; }

    public string Text { get; }

    /// <summary>
    /// The text a screen reader reads
    /// </summary>
    public override string ToString() => Text;
}

/// <summary>
/// A command being added or changed: a copy of the definition, saved with <see cref="CommandSettingsViewModel.Save"/>.
/// </summary>
public sealed class CommandEditorViewModel : INotifyPropertyChanged
{
    internal CommandEditorViewModel(CommandDefinition definition, string originalId)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        OriginalId = originalId;
        Definition.Format ??= WellKnownFileFormats.Png;
        var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
        var formats = registry?.GetSaveableFileFormatOptions().ToList() ?? new List<FileFormatOption>();
        if (!formats.Any(option => string.Equals(option.Id, Definition.Format, StringComparison.OrdinalIgnoreCase)))
        {
            formats.Add(new FileFormatOption { Id = Definition.Format, DisplayName = Definition.Format, DisplayNameWithPreferredExtension = Definition.Format });
        }

        Formats = formats;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static IExternalCommandLanguage Language => Texts.Get<IExternalCommandLanguage>();

    internal CommandDefinition Definition { get; }

    /// <summary>The id of the changed command, null for a new one</summary>
    internal string OriginalId { get; }

    public string Title => OriginalId == null ? Language.EditorTitleNew : string.Format(Language.EditorTitleEdit, Definition.Name);

    public IReadOnlyList<FileFormatOption> Formats { get; }

    public IReadOnlyList<ChoiceOption> StandardInputOptions { get; } = new[]
    {
        new ChoiceOption(CommandDefinitions.InputNone, Language.ChoiceInputNone),
        new ChoiceOption(CommandDefinitions.InputImage, Language.ChoiceInputImage),
        new ChoiceOption(CommandDefinitions.InputText, Language.ChoiceInputText)
    };

    private const string AfterNothing = "Nothing";
    private const string AfterNotification = "Notification";

    /// <summary>
    /// What happens with what the program prints: the output and the clipboard in one choice
    /// </summary>
    public IReadOnlyList<ChoiceOption> AfterwardsOptions { get; } = new[]
    {
        new ChoiceOption(AfterNothing, Language.ChoiceAfterNothing),
        new ChoiceOption(AfterNotification, Language.ChoiceAfterNotification),
        new ChoiceOption(CommandDefinitions.ClipboardOutput, Language.ChoiceAfterCopyOutput),
        new ChoiceOption(CommandDefinitions.ClipboardUrl, Language.ChoiceAfterCopyLink)
    };

    public string Name
    {
        get => Definition.Name;
        set => Set(() => Definition.Name = value);
    }

    public string Program
    {
        get => Definition.Program;
        set
        {
            Set(() => Definition.Program = value, nameof(IsWindowsApp));
            if (IsWindowsApp && !RunInBackground)
            {
                RunInBackground = true;
            }

            OnPropertyChanged(nameof(CanChangeRunInBackground));
            OnPropertyChanged(nameof(RunInBackgroundHint));
        }
    }

    /// <summary>
    /// The program is a Windows App (shell:AppsFolder\&lt;app id&gt;): Windows starts it with the file, Greenshot gets no process
    /// to wait for or to read from, so it always runs in the background
    /// </summary>
    public bool IsWindowsApp => WindowsAppLauncher.IsAppProgram(Definition.Program);

    public bool CanChangeRunInBackground => !IsWindowsApp;

    public string RunInBackgroundHint => IsWindowsApp ? Language.LabelRunInBackgroundWindowsApp : Language.LabelRunInBackgroundHint;

    public string Arguments
    {
        get => Definition.Arguments;
        set => Set(() => Definition.Arguments = value);
    }

    public string WorkingDirectory
    {
        get => Definition.WorkingDirectory;
        set => Set(() => Definition.WorkingDirectory = value);
    }

    /// <summary>
    /// The file saved last instead of a new one; whether there is a file at all comes from the arguments (<see cref="UsesFile"/>)
    /// </summary>
    public bool UseLastSavedFile
    {
        get => CommandKind.Is(Definition.File, CommandDefinitions.FileLastExport);
        set => Set(() => Definition.File = value ? CommandDefinitions.FileLastExport : CommandDefinitions.FileNew);
    }

    public string Format
    {
        get => Definition.Format;
        set => Set(() => Definition.Format = value);
    }

    public string StandardInput
    {
        get => Definition.StandardInput;
        set => Set(() => Definition.StandardInput = value, nameof(HasStandardInputText));
    }

    /// <summary>The text is only used with standard input Text</summary>
    public bool HasStandardInputText => CommandKind.Is(Definition.StandardInput, CommandDefinitions.InputText);

    public string StandardInputText
    {
        get => Definition.StandardInputText;
        set => Set(() => Definition.StandardInputText = value);
    }

    /// <summary>
    /// A new image or the changed file: only a recipe reads that, the editor doesn't offer it but keeps it
    /// </summary>
    public bool HasRecipeOutput => CommandKind.Is(Definition.Output, nameof(CommandOutput.Image)) || CommandKind.Is(Definition.Output, nameof(CommandOutput.File));

    /// <summary>
    /// What happens with what the program prints, null for an output of a recipe
    /// </summary>
    public string Afterwards
    {
        get
        {
            if (HasRecipeOutput) return null;
            if (!CommandKind.Is(Definition.Output, nameof(CommandOutput.Text))) return AfterNothing;
            return CommandKind.Is(Definition.CopyToClipboard, CommandDefinitions.ClipboardNone) || string.IsNullOrEmpty(Definition.CopyToClipboard)
                ? AfterNotification
                : AfterwardsOptions.FirstOrDefault(option => CommandKind.Is(option.Value, Definition.CopyToClipboard))?.Value ?? AfterNotification;
        }
        set
        {
            if (value == null) return;
            Set(() =>
            {
                Definition.Output = value == AfterNothing ? nameof(CommandOutput.None) : nameof(CommandOutput.Text);
                Definition.CopyToClipboard = value is AfterNothing or AfterNotification ? CommandDefinitions.ClipboardNone : value;
            }, nameof(HasRecipeOutput));
        }
    }

    public string SuccessExitCodes
    {
        get => Definition.SuccessExitCodes;
        set => Set(() => Definition.SuccessExitCodes = value);
    }

    public int Timeout
    {
        get => Definition.Timeout;
        set => Set(() => Definition.Timeout = value);
    }

    public string Encoding
    {
        get => Definition.Encoding;
        set => Set(() => Definition.Encoding = value);
    }

    /// <summary>
    /// Greenshot doesn't wait for the program, what it prints isn't read
    /// </summary>
    public bool RunInBackground
    {
        get => Definition.RunInBackground;
        set => Set(() => Definition.RunInBackground = value, nameof(CanChooseAfterwards));
    }

    public bool CanChooseAfterwards => !Definition.RunInBackground;

    /// <summary>
    /// A setting for command-line tools isn't the default: that part is open when the window opens
    /// </summary>
    public bool HasAdvancedSettings =>
        !CommandKind.Is(Definition.StandardInput, CommandDefinitions.InputNone)
        || UseLastSavedFile
        || !string.IsNullOrWhiteSpace(Definition.WorkingDirectory)
        || (!string.IsNullOrWhiteSpace(Definition.SuccessExitCodes) && Definition.SuccessExitCodes.Trim() != "0")
        || Definition.Timeout != 0
        || (!string.IsNullOrWhiteSpace(Definition.Encoding) && !CommandKind.Is(Definition.Encoding, "utf-8"));

    /// <summary>
    /// Take the program and the arguments of a picked app; Greenshot doesn't wait for an app, it stays open
    /// </summary>
    internal void TakeApp(InstalledApp app)
    {
        if (app == null) return;
        Program = app.Program;
        Arguments = string.IsNullOrWhiteSpace(app.Arguments) ? CommandDefinition.DefaultArguments : app.Arguments;
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = app.Name;
        }

        if (WindowsAppLauncher.IsAppProgram(app.Program))
        {
            // A Windows App only gets the file
            StandardInput = CommandDefinitions.InputNone;
            Afterwards = AfterNothing;
            RunInBackground = true;
        }
        else if (!HasRecipeOutput)
        {
            Afterwards = AfterNothing;
            RunInBackground = true;
        }
    }

    /// <summary>
    /// The command uses a file: ${export.file} in what the program gets, or it gives back the changed file
    /// </summary>
    internal static bool UsesFile(CommandDefinition definition) =>
        new[] { definition.Arguments, definition.WorkingDirectory, definition.StandardInputText }
            .Any(text => text?.IndexOf("export.file", StringComparison.OrdinalIgnoreCase) >= 0)
        || CommandKind.Is(definition.Output, nameof(CommandOutput.File));

    /// <summary>
    /// The definition to save: a file only when the command uses it, the clipboard only for a text output, nothing read in the background
    /// </summary>
    internal CommandDefinition ToDefinition()
    {
        var definition = Definition.Clone();
        bool lastSaved = CommandKind.Is(definition.File, CommandDefinitions.FileLastExport);
        definition.File = !UsesFile(definition) ? CommandDefinitions.FileNone : lastSaved ? CommandDefinitions.FileLastExport : CommandDefinitions.FileNew;
        if (definition.RunInBackground && !HasRecipeOutput)
        {
            definition.Output = nameof(CommandOutput.None);
        }

        if (!CommandKind.Is(definition.Output, nameof(CommandOutput.Text)))
        {
            definition.CopyToClipboard = CommandDefinitions.ClipboardNone;
        }

        return definition;
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void Set(Action change, string alsoChanged = null, [CallerMemberName] string propertyName = null)
    {
        change();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (alsoChanged != null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(alsoChanged));
        }
    }
}
