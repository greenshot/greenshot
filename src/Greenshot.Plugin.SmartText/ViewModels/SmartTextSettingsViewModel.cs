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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Greenshot.Base.Definitions;
using Greenshot.Plugin.SmartText.Detection;
using log4net;

namespace Greenshot.Plugin.SmartText.ViewModels;

/// <summary>
/// The settings of Smart text: the built-in detectors on or off, the own patterns added, imported, changed, exported and removed.
/// Used on the UI thread.
/// </summary>
public sealed class SmartTextSettingsViewModel : INotifyPropertyChanged
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(SmartTextSettingsViewModel));
    private readonly SmartTextStore _store;
    private DefinitionListItem _selectedPattern;
    private DefinitionListItem _selectedBuiltIn;

    internal SmartTextSettingsViewModel(SmartTextStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        foreach (var builtIn in BuiltInDetectors.Create())
        {
            string name = SmartTextDefinitions.NameOf(builtIn.Id);
            var item = new DefinitionListItem(builtIn.Id, name, null, true, null, isBuiltIn: true)
                .WithOnOff(SmartTextDefinitions.IsOn(builtIn.Id, builtIn.IsOnByDefault), string.Format(Language.Offer, name),
                    (listItem, on) => TurnBuiltIn(listItem, on, builtIn.IsOnByDefault));
            BuiltIns.Add(item);
        }

        Refresh();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static ISmartTextLanguage Language => SmartTextPlugin.Language;

    public ObservableCollection<DefinitionListItem> BuiltIns { get; } = new ObservableCollection<DefinitionListItem>();

    public ObservableCollection<DefinitionListItem> Patterns { get; } = new ObservableCollection<DefinitionListItem>();

    public DefinitionListItem SelectedBuiltIn
    {
        get => _selectedBuiltIn;
        set
        {
            _selectedBuiltIn = value;
            OnPropertyChanged();
        }
    }

    public DefinitionListItem SelectedPattern
    {
        get => _selectedPattern;
        set
        {
            _selectedPattern = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEditSelected));
            OnPropertyChanged(nameof(CanExportSelected));
            OnPropertyChanged(nameof(CanRemoveSelected));
            OnPropertyChanged(nameof(CanApproveSelected));
        }
    }

    public bool CanApproveSelected => SelectedPattern?.CanBeApproved == true;

    public bool CanEditSelected => SelectedPattern?.IsEditable == true;

    public bool CanExportSelected => SelectedPattern?.IsReadable == true;

    public bool CanRemoveSelected => SelectedPattern != null;

    private static void TurnBuiltIn(DefinitionListItem item, bool on, bool isOnByDefault)
    {
        SmartTextDefinitions.SetOn(item.Id, on, isOnByDefault);
        Log.InfoFormat("Turned the Smart text detector {0} {1}", item.Id, on ? "on" : "off");
    }

    private static void TurnPattern(DefinitionListItem item, bool on)
    {
        SmartTextDefinitions.SetOn(item.Name, on, true);
        Log.InfoFormat("Turned the Smart text pattern {0} {1}", item.Name, on ? "on" : "off");
    }

    public void Refresh()
    {
        string selected = SelectedPattern?.Id;
        Patterns.Clear();
        foreach (var stored in _store.GetAll())
        {
            bool readable = stored.Definition != null;
            var item = new DefinitionListItem(stored.Id, readable ? stored.Name : Language.Unreadable,
                DefinitionListItem.DescriptionOf(_store.Kind, stored, Language.NotApproved), readable, null, stored.IsBuiltIn, stored.IsApproved);
            Patterns.Add(readable ? item.WithOnOff(SmartTextDefinitions.IsOn(stored.Name, true), string.Format(Language.Offer, stored.Name), TurnPattern) : item);
        }

        SelectedPattern = Patterns.FirstOrDefault(item => item.Id == selected);
    }

    /// <summary>A copy of the selected pattern to change, null when there is none or it can't be read</summary>
    internal SmartTextPattern GetSelectedDefinition() =>
        SelectedPattern == null ? null : _store.GetAll().FirstOrDefault(stored => stored.Id == SelectedPattern.Id)?.Definition?.Clone();

    /// <summary>
    /// What the user confirms before a pattern file which was changed outside Greenshot is used, like an import
    /// </summary>
    internal static DefinitionApproval DescribeApproval(SmartTextPattern definition, string fileId)
    {
        var approval = Describe(definition, Language.ApproveTitle, Language.QuestionApprove)
            .With(Language.DetailFile, fileId == null ? null : Path.Combine(SmartTextDefinitions.FolderPath, fileId));
        approval.Notes.Add(Language.ImportTrust);
        return approval;
    }

    /// <summary>The pattern, its label and where it sends the value</summary>
    private static DefinitionApproval Describe(SmartTextPattern definition, string title, string question)
    {
        var approval = new DefinitionApproval
        {
            Title = title,
            Name = definition.Name,
            Kind = Language.KindPattern,
            Question = question
        };
        approval.With(Language.DetailPattern, definition.Pattern)
            .With(Language.DetailLabel, definition.Label)
            .With(Language.DetailOpenUrl, definition.OpenUrl);
        return approval;
    }

    /// <summary>The user confirmed the selected pattern file: it is used at once</summary>
    internal void ApproveSelected()
    {
        if (!CanApproveSelected) return;
        string id = SelectedPattern.Id;
        if (!_store.Approve(id)) return;
        Log.InfoFormat("Approved the Smart text pattern file {0}", id);
        Refresh();
        SelectedPattern = Patterns.FirstOrDefault(item => item.Id == id);
    }

    /// <summary>Can a pattern have the name: not the name of a built-in detector or of another pattern</summary>
    internal bool IsNameFree(string name, string originalId)
    {
        if (SmartTextDefinitions.IsBuiltInName(name, BuiltInDetectors.Create())) return false;
        var existing = _store.Find(name?.Trim());
        return existing == null || existing.Id == originalId;
    }

    /// <summary>
    /// Check and save a new or changed pattern. A changed name renames it.
    /// </summary>
    /// <exception cref="DefinitionFileException">it can't be saved, the message says why</exception>
    internal void Save(SmartTextPattern definition, string originalId)
    {
        SmartTextKind.Check(definition);
        if (!IsNameFree(definition.Name, originalId))
        {
            throw new DefinitionFileException(Language.DuplicateName);
        }

        string id = Write(() =>
        {
            string newId = _store.Add(definition);
            if (originalId != null && originalId != newId)
            {
                // A renamed pattern stays turned off
                string originalName = _store.GetAll().FirstOrDefault(stored => stored.Id == originalId)?.Name;
                if (originalName != null && !SmartTextDefinitions.IsOn(originalName, true))
                {
                    SmartTextDefinitions.SetOn(originalName, true, true);
                    SmartTextDefinitions.SetOn(definition.Name, false, true);
                }

                _store.Remove(originalId);
            }

            return newId;
        });

        Log.InfoFormat("Saved the Smart text pattern {0}", definition.Name);
        Refresh();
        SelectedPattern = Patterns.FirstOrDefault(item => item.Id == id);
    }

    /// <summary>Read and check the pattern file, null with the reason when it can't be imported</summary>
    internal static SmartTextPattern ReadFile(string path, out string error) =>
        DefinitionImport.ReadFile(path, SmartTextKind.MaximumLength, SmartTextKind.Parse, Language.ErrorFileTooLarge, out error);

    /// <summary>What the user confirms before the import</summary>
    internal DefinitionApproval DescribeImport(SmartTextPattern definition, string path)
    {
        var approval = Describe(definition, Language.ImportTitle, Language.QuestionImport).With(Language.DetailFile, path);
        if (_store.Find(definition.Name) != null)
        {
            approval.Notes.Add(Language.ImportReplaces);
        }

        approval.Notes.Add(Language.ImportTrust);
        return approval;
    }

    internal bool CanImport(SmartTextPattern definition) => !SmartTextDefinitions.IsBuiltInName(definition.Name, BuiltInDetectors.Create());

    /// <exception cref="DefinitionFileException">the file can't be written</exception>
    internal void Import(SmartTextPattern definition)
    {
        string id = Write(() => _store.Add(definition));
        Log.InfoFormat("Imported the Smart text pattern {0}", definition.Name);
        Refresh();
        SelectedPattern = Patterns.FirstOrDefault(item => item.Id == id);
    }

    /// <summary>Write the selected pattern to the file (UTF-8 without a byte order mark)</summary>
    internal void ExportSelected(string path)
    {
        var definition = GetSelectedDefinition() ?? throw new InvalidOperationException("No pattern which can be read is selected");
        File.WriteAllText(path, _store.Kind.Write(definition), new UTF8Encoding(false));
        Log.InfoFormat("Exported the Smart text pattern {0} to {1}", definition.Name, path);
    }

    /// <exception cref="DefinitionFileException">the file can't be deleted</exception>
    public void RemoveSelected()
    {
        if (!CanRemoveSelected) return;
        string id = SelectedPattern.Id;
        string name = SelectedPattern.IsReadable ? SelectedPattern.Name : null;
        Write(() => _store.Remove(id));
        if (name != null && !SmartTextDefinitions.IsOn(name, true))
        {
            SmartTextDefinitions.SetOn(name, true, true);
        }

        Refresh();
    }

    private static TResult Write<TResult>(Func<TResult> change)
    {
        try
        {
            return change();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("Can't change the SmartText folder", ex);
            throw new DefinitionFileException(ex.Message, ex);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
