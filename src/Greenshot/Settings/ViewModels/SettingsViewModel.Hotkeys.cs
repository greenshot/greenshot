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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Dapplo.Ini.Interfaces;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;
using Greenshot.Recipes;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The hotkeys of the recipes which are on, on the General tab: the hotkeys of the general settings, the Hotkey options of
    /// recipes and the hotkeys written in recipe files. The values are written when the settings are saved (OK).
    /// </summary>
    public partial class SettingsViewModel
    {
        public ObservableCollection<RecipeHotkeyViewModel> Hotkeys { get; } = new ObservableCollection<RecipeHotkeyViewModel>();

        private void InitializeHotkeys()
        {
            Hotkeys.Clear();
            try
            {
                foreach (var hotkey in RecipeManager.Instance.GetActiveHotkeys().OrderBy(h => h.SortOrder).ThenBy(h => h.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                {
                    var row = new RecipeHotkeyViewModel(hotkey, CoreConfiguration);
                    row.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(RecipeHotkeyViewModel.HotkeyString)) UpdateHotkeyConflicts();
                    };
                    Hotkeys.Add(row);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't read the hotkeys of the recipes.", ex);
            }
            UpdateHotkeyConflicts();
        }

        /// <summary>
        /// A hotkey of two recipes: the second one would take the key from the first, both rows say so (red, OK stops there)
        /// </summary>
        private void UpdateHotkeyConflicts()
        {
            foreach (var row in Hotkeys)
            {
                var sequence = row.Sequence;
                var others = sequence == null
                    ? new List<string>()
                    : Hotkeys.Where(other => !ReferenceEquals(other, row) && sequence.Equals(other.Sequence)).Select(other => other.Label).ToList();
                row.ConflictText = others.Count == 0 ? null : string.Format(Texts.Settings.HotkeyConflict ?? "The same hotkey as {0}, only one of them can work", string.Join(", ", others));
            }
        }

        /// <summary>
        /// Writes the changed hotkeys: to [Core] or as the value of the recipe's Hotkey option
        /// </summary>
        public void SaveHotkeys()
        {
            foreach (var hotkey in Hotkeys)
            {
                hotkey.Save();
            }
        }
    }

    /// <summary>
    /// One hotkey on the General tab
    /// </summary>
    public class RecipeHotkeyViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly RecipeHotkey _hotkey;
        private readonly ICoreConfiguration _coreConfiguration;
        private string _hotkeyString;
        private string _conflictText;

        public RecipeHotkeyViewModel(RecipeHotkey hotkey, ICoreConfiguration coreConfiguration)
        {
            _hotkey = hotkey;
            _coreConfiguration = coreConfiguration;
            _hotkeyString = hotkey.Hotkey;
            OpenRecipeEditorCommand = new RelayCommand(() =>
                SimpleServiceProvider.Current.GetInstance<IRecipeEditorService>(isOptional: true)?.OpenEditor(_hotkey.Recipe?.Id));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Label => _hotkey.DisplayName;

        public string HotkeyString
        {
            get => _hotkeyString;
            set
            {
                if (string.Equals(_hotkeyString, value, StringComparison.Ordinal)) return;
                _hotkeyString = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Sequence));
            }
        }

        /// <summary>
        /// The hotkey as a sequence to compare with the others, null without a hotkey
        /// </summary>
        public HotkeySequence Sequence
        {
            get
            {
                try
                {
                    var sequence = HotkeySequence.Parse(_hotkeyString);
                    return sequence.IsEmpty ? null : sequence;
                }
                catch (Exception)
                {
                    // Not a hotkey which can be read, it can't take a key from another one either
                    return null;
                }
            }
        }

        /// <summary>
        /// Says which other recipe has the same hotkey, null when no other one has it
        /// </summary>
        public string ConflictText
        {
            get => _conflictText;
            set
            {
                if (string.Equals(_conflictText, value, StringComparison.Ordinal)) return;
                _conflictText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasConflict));
                OnPropertyChanged(nameof(HasErrors));
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(HotkeyString)));
            }
        }

        /// <inheritdoc />
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// Another recipe has the same hotkey
        /// </summary>
        public bool HasConflict => _conflictText != null;

        /// <summary>
        /// Only a hotkey which can be changed here is an error which keeps OK from closing; one of a recipe file or
        /// fixed by the administrator only shows the text, else the window could never be closed with OK
        /// </summary>
        public bool HasErrors => HasConflict && CanChange && IsEditable;

        /// <inheritdoc />
        public IEnumerable GetErrors(string propertyName) =>
            HasErrors && (string.IsNullOrEmpty(propertyName) || propertyName == nameof(HotkeyString))
                ? new[] { _conflictText }
                : Array.Empty<string>();

        /// <summary>
        /// The hotkey of a general setting or an option can be changed here, one written in a recipe file in the recipe editor
        /// </summary>
        public bool CanChange => !_hotkey.IsInRecipeFile;

        /// <summary>
        /// False when the administrator fixed the setting (greenshot-fixed.ini)
        /// </summary>
        public bool IsEditable => _hotkey.SettingName == null || !(_coreConfiguration is IIniSection section && section.IsConstant(_hotkey.SettingName));

        /// <summary>
        /// The recipe editor (not in Greenshot Light) changes a hotkey written in the recipe file
        /// </summary>
        public bool CanOpenRecipeEditor => _hotkey.IsInRecipeFile && SimpleServiceProvider.Current.GetInstance<IRecipeEditorService>(isOptional: true) != null;

        public ICommand OpenRecipeEditorCommand { get; }

        public void Save()
        {
            if (!CanChange || !IsEditable || string.Equals(_hotkeyString ?? string.Empty, _hotkey.Hotkey ?? string.Empty, StringComparison.Ordinal)) return;
            if (_hotkey.SettingName != null)
            {
                typeof(ICoreConfiguration).GetProperty(_hotkey.SettingName)?.SetValue(_coreConfiguration, _hotkeyString);
            }
            else
            {
                RecipeOptionStore.SetValue(_hotkey.Recipe.Id, _hotkey.Option, _hotkeyString);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
