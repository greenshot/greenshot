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

    public IReadOnlyList<ChoiceOption> FileOptions { get; } = new[]
    {
        new ChoiceOption(CommandDefinitions.FileNew, Language.ChoiceFileNew),
        new ChoiceOption(CommandDefinitions.FileLastExport, Language.ChoiceFileLastExport),
        new ChoiceOption(CommandDefinitions.FileNone, Language.ChoiceFileNone)
    };

    public IReadOnlyList<ChoiceOption> StandardInputOptions { get; } = new[]
    {
        new ChoiceOption(CommandDefinitions.InputNone, Language.ChoiceInputNone),
        new ChoiceOption(CommandDefinitions.InputImage, Language.ChoiceInputImage),
        new ChoiceOption(CommandDefinitions.InputText, Language.ChoiceInputText)
    };

    public IReadOnlyList<ChoiceOption> OutputOptions { get; } = new[]
    {
        new ChoiceOption(nameof(CommandOutput.None), Language.ChoiceOutputNone),
        new ChoiceOption(nameof(CommandOutput.Text), Language.ChoiceOutputText),
        new ChoiceOption(nameof(CommandOutput.Image), Language.ChoiceOutputImage),
        new ChoiceOption(nameof(CommandOutput.File), Language.ChoiceOutputFile)
    };

    public IReadOnlyList<ChoiceOption> ClipboardOptions { get; } = new[]
    {
        new ChoiceOption(CommandDefinitions.ClipboardNone, Language.ChoiceClipboardNone),
        new ChoiceOption(CommandDefinitions.ClipboardOutput, Language.ChoiceClipboardOutput),
        new ChoiceOption(CommandDefinitions.ClipboardUrl, Language.ChoiceClipboardUrl)
    };

    public string Name
    {
        get => Definition.Name;
        set => Set(() => Definition.Name = value);
    }

    public string Program
    {
        get => Definition.Program;
        set => Set(() => Definition.Program = value);
    }

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

    public string File
    {
        get => Definition.File;
        set => Set(() => Definition.File = value);
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

    public string Output
    {
        get => Definition.Output;
        set => Set(() => Definition.Output = value, nameof(HasTextOutput));
    }

    /// <summary>Only a text output can go to the clipboard</summary>
    public bool HasTextOutput => CommandKind.Is(Definition.Output, nameof(CommandOutput.Text));

    public string CopyToClipboard
    {
        get => Definition.CopyToClipboard;
        set => Set(() => Definition.CopyToClipboard = value);
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

    public bool RunInBackground
    {
        get => Definition.RunInBackground;
        set => Set(() => Definition.RunInBackground = value);
    }

    /// <summary>
    /// The definition to save: the clipboard is only kept for a text output
    /// </summary>
    internal CommandDefinition ToDefinition()
    {
        var definition = Definition.Clone();
        if (!HasTextOutput)
        {
            definition.CopyToClipboard = CommandDefinitions.ClipboardNone;
        }

        return definition;
    }

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
