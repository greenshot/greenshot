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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using Greenshot.Base.Core;
using Greenshot.Base.Definitions;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Plugin.ExternalCommand.Running;
using log4net;

namespace Greenshot.Plugin.ExternalCommand.ViewModels;

/// <summary>
/// The settings of the plugin: the commands, added, imported, changed, exported and removed here. Used on the UI thread.
/// </summary>
public sealed class CommandSettingsViewModel : INotifyPropertyChanged
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(CommandSettingsViewModel));
    private readonly CommandStore _store;
    private readonly Dispatcher _dispatcher;
    private DefinitionListItem _selectedCommand;

    internal CommandSettingsViewModel(CommandStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _dispatcher = Dispatcher.CurrentDispatcher;
        Refresh();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static IExternalCommandLanguage Language => Texts.Get<IExternalCommandLanguage>();

    public ObservableCollection<DefinitionListItem> Commands { get; } = new ObservableCollection<DefinitionListItem>();

    public DefinitionListItem SelectedCommand
    {
        get => _selectedCommand;
        set
        {
            _selectedCommand = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEditSelected));
            OnPropertyChanged(nameof(CanExportSelected));
            OnPropertyChanged(nameof(CanRemoveSelected));
            OnPropertyChanged(nameof(CanApproveSelected));
            OnPropertyChanged(nameof(BuiltInHint));
        }
    }

    /// <summary>
    /// The check box of a row: a command which is off has no destination (from the menus and the destination settings) and recipes
    /// can't use it, at once
    /// </summary>
    private void Turn(DefinitionListItem item, bool on)
    {
        string id = item.Id;
        string name = item.Name;
        CommandDefinitions.SetTurnedOff(name, !on);
        if (on)
        {
            ExternalCommandPlugin.AddDestination(_store.GetAll().FirstOrDefault(stored => stored.Id == id && stored.IsApproved)?.Definition);
        }
        else
        {
            ExternalCommandPlugin.RemoveDestination(name);
        }

        Log.InfoFormat("Turned the external command {0} {1}", name, on ? "on" : "off");
    }

    /// <summary>A file which was added or changed outside Greenshot and can be read</summary>
    public bool CanApproveSelected => SelectedCommand?.CanBeApproved == true;

    /// <summary>A stored and approved command which can be read</summary>
    public bool CanEditSelected => SelectedCommand?.IsEditable == true;

    /// <summary>Every command which can be read, the built-in ones too</summary>
    public bool CanExportSelected => SelectedCommand?.IsReadable == true;

    /// <summary>Every stored command, also one which can't be read anymore</summary>
    public bool CanRemoveSelected => SelectedCommand != null && !SelectedCommand.IsBuiltIn;

    /// <summary>
    /// Why Edit and Remove are off for the selected command: it comes with Greenshot. Null otherwise.
    /// </summary>
    public string BuiltInHint => SelectedCommand?.IsBuiltIn == true ? string.Format(Language.BuiltInHint, SelectedCommand.Name) : null;

    public void Refresh()
    {
        string selected = SelectedCommand?.Id;
        Commands.Clear();
        var all = _store.GetAll();
        foreach (var stored in all)
        {
            Commands.Add(ItemOf(stored, null));
        }

        SelectedCommand = Commands.FirstOrDefault(item => item.Id == selected);
        LoadIconsAsync(all).FireAndLog("Load the icons of the external commands", Log);
    }

    private DefinitionListItem ItemOf(StoredDefinition<CommandDefinition> stored, ImageSource icon)
    {
        bool readable = stored.Definition != null;
        var item = new DefinitionListItem(stored.Id, readable ? stored.Name : Language.Unreadable, DefinitionListItem.DescriptionOf(_store.Kind, stored, Language.NotApproved),
            readable, icon, stored.IsBuiltIn, stored.IsApproved);
        return readable ? item.WithOnOff(!CommandDefinitions.IsTurnedOff(stored.Name), string.Format(Language.OfferAsDestination, stored.Name), Turn) : item;
    }

    /// <summary>
    /// The icons of the programs, read in the background; an item gets its icon when it is still in the list
    /// </summary>
    private async Task LoadIconsAsync(IReadOnlyList<StoredDefinition<CommandDefinition>> all)
    {
        foreach (var stored in all.Where(stored => stored.Definition != null))
        {
            var image = await IconCache.IconForCommandAsync(stored.Definition.Program, stored.Name).ConfigureAwait(false);
            if (image == null)
            {
                continue;
            }

            ImageSource icon;
            // The icon cache owns its images
            lock (image)
            {
                icon = image.ToBitmapSource();
            }

            icon?.Freeze();
            await _dispatcher.InvokeAsync(() =>
            {
                int index = Commands.ToList().FindIndex(item => item.Id == stored.Id);
                if (index < 0) return;
                bool wasSelected = SelectedCommand?.Id == stored.Id;
                Commands[index] = ItemOf(stored, icon);
                if (wasSelected)
                {
                    SelectedCommand = Commands[index];
                }
            }).Task.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// A copy of the selected command to change, null when there is none or it can't be read
    /// </summary>
    internal CommandDefinition GetSelectedDefinition() =>
        SelectedCommand == null ? null : _store.GetAll().FirstOrDefault(stored => stored.Id == SelectedCommand.Id)?.Definition?.Clone();

    /// <summary>
    /// What the user confirms before a command file which was added or changed outside Greenshot is used, like an import
    /// </summary>
    internal static string DescribeApproval(CommandDefinition definition)
    {
        var text = new StringBuilder(string.Format(Language.ApproveQuestion, definition.Name, CommandKind.Instance.Describe(definition)));
        if (CommandKind.Is(definition.StandardInput, CommandDefinitions.InputText))
        {
            text.Append("\n\n").Append(string.Format(Language.ImportStandardInput, definition.StandardInputText));
        }

        text.Append("\n\n").Append(Language.ImportTrust);
        return text.ToString();
    }

    /// <summary>
    /// The user confirmed the selected command file: it is used at once, in recipes and in the menus
    /// </summary>
    internal void ApproveSelected()
    {
        if (!CanApproveSelected) return;
        string id = SelectedCommand.Id;
        if (!_store.Approve(id)) return;
        ExternalCommandPlugin.AddDestination(_store.GetAll().FirstOrDefault(stored => stored.Id == id)?.Definition);
        Log.InfoFormat("Approved the external command file {0}", id);
        Refresh();
        SelectedCommand = Commands.FirstOrDefault(item => item.Id == id);
    }

    /// <summary>
    /// Check and save a new or changed command. A changed name renames it.
    /// </summary>
    /// <param name="definition">the command</param>
    /// <param name="originalId">the id of the changed command, null for a new one</param>
    /// <exception cref="DefinitionFileException">it can't be saved, the message says why</exception>
    internal void Save(CommandDefinition definition, string originalId)
    {
        CommandKind.Check(definition);
        var existing = _store.Find(definition.Name);
        if (_store.IsBuiltInName(definition.Name) || (existing != null && existing.Id != originalId))
        {
            throw new DefinitionFileException(Language.TooltipDuplicateName);
        }

        string id = Write(() =>
        {
            string newId = _store.Add(definition);
            if (originalId != null && originalId != newId)
            {
                // A renamed command stays turned off
                string originalName = _store.GetAll().FirstOrDefault(stored => stored.Id == originalId)?.Name;
                if (CommandDefinitions.IsTurnedOff(originalName))
                {
                    CommandDefinitions.SetTurnedOff(originalName, false);
                    CommandDefinitions.SetTurnedOff(definition.Name, true);
                }

                _store.Remove(originalId);
            }

            return newId;
        });

        Log.InfoFormat("Saved the external command {0}", definition.Name);
        Refresh();
        SelectedCommand = Commands.FirstOrDefault(item => item.Id == id);
    }

    /// <summary>
    /// Read and check the command file, null with the reason when it can't be imported.
    /// </summary>
    internal static CommandDefinition ReadFile(string path, out string error) =>
        DefinitionImport.ReadFile(path, CommandKind.MaximumLength, CommandKind.Parse, Language.ErrorFileTooLarge, out error);

    /// <summary>
    /// What the user confirms before the import: the program and its arguments, the text on standard input
    /// </summary>
    internal string DescribeImport(CommandDefinition definition)
    {
        var text = new StringBuilder(string.Format(Language.ImportQuestion, definition.Name, _store.Kind.Describe(definition)));
        if (CommandKind.Is(definition.StandardInput, CommandDefinitions.InputText))
        {
            text.Append("\n\n").Append(string.Format(Language.ImportStandardInput, definition.StandardInputText));
        }

        if (_store.Find(definition.Name) != null)
        {
            text.Append("\n\n").Append(Language.ImportReplaces);
        }

        text.Append("\n\n").Append(Language.ImportTrust);
        return text.ToString();
    }

    /// <summary>
    /// Can the command be imported: not with the name of a built-in one
    /// </summary>
    internal bool CanImport(CommandDefinition definition) => !_store.IsBuiltInName(definition.Name);

    /// <summary>
    /// Add the confirmed command, it replaces the one with the same name.
    /// </summary>
    /// <exception cref="DefinitionFileException">the file of the command can't be written</exception>
    internal void Import(CommandDefinition definition)
    {
        string id = Write(() => _store.Add(definition));
        Log.InfoFormat("Imported the external command {0}: {1}", definition.Name, _store.Kind.Describe(definition));
        Refresh();
        SelectedCommand = Commands.FirstOrDefault(item => item.Id == id);
    }

    /// <summary>
    /// Write the selected command to the file (UTF-8 without a byte order mark)
    /// </summary>
    /// <exception cref="IOException">the file can't be written</exception>
    /// <exception cref="UnauthorizedAccessException">the file can't be written</exception>
    internal void ExportSelected(string path)
    {
        var definition = GetSelectedDefinition() ?? throw new InvalidOperationException("No command which can be read is selected");
        File.WriteAllText(path, _store.Kind.Write(definition), new UTF8Encoding(false));
        Log.InfoFormat("Exported the external command {0} to {1}", definition.Name, path);
    }

    /// <exception cref="DefinitionFileException">the file of the command can't be deleted</exception>
    public void RemoveSelected()
    {
        if (!CanRemoveSelected) return;
        string id = SelectedCommand.Id;
        string name = SelectedCommand.IsReadable ? SelectedCommand.Name : null;
        Write(() => _store.Remove(id));
        if (CommandDefinitions.IsTurnedOff(name))
        {
            CommandDefinitions.SetTurnedOff(name, false);
        }

        Refresh();
    }

    /// <summary>
    /// A change of the Commands folder, a file which can't be written or deleted is reported with the reason
    /// </summary>
    private static TResult Write<TResult>(Func<TResult> change)
    {
        try
        {
            return change();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("Can't change the Commands folder", ex);
            throw new DefinitionFileException(ex.Message, ex);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
