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
using System.Linq;
using System.Windows.Input;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    /// <summary>
    /// The translations of the texts of the recipe or automatic step ("translations" in the file). The rows edit the
    /// translations of <see cref="ActiveRecipe"/> directly, the unsaved state and undo see the changes.
    /// </summary>
    public partial class RecipeEditorViewModel
    {
        private ICommand _addTranslationLanguageCommand;
        private ICommand _removeTranslationLanguageCommand;
        private string _selectedTranslationLanguage;
        private string _newTranslationLanguage;

        public ObservableCollection<string> TranslationLanguages { get; } = new ObservableCollection<string>();

        public ObservableCollection<RecipeTranslationItemViewModel> TranslationRows { get; } = new ObservableCollection<RecipeTranslationItemViewModel>();

        public bool HasTranslationLanguage => !string.IsNullOrEmpty(_selectedTranslationLanguage);

        /// <summary>
        /// The language whose texts are shown; choosing it again shows the texts the recipe has now
        /// </summary>
        public string SelectedTranslationLanguage
        {
            get => _selectedTranslationLanguage;
            set
            {
                _selectedTranslationLanguage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTranslationLanguage));
                LoadTranslationRows();
            }
        }

        /// <summary>
        /// The language to add, e.g. "de" or "de-DE"
        /// </summary>
        public string NewTranslationLanguage
        {
            get => _newTranslationLanguage;
            set => SetField(ref _newTranslationLanguage, value);
        }

        public ICommand AddTranslationLanguageCommand => _addTranslationLanguageCommand ??= new RelayCommand(AddTranslationLanguage);

        public ICommand RemoveTranslationLanguageCommand => _removeTranslationLanguageCommand ??= new RelayCommand(RemoveTranslationLanguage);

        private void LoadTranslations(CaptureRecipe recipe)
        {
            TranslationLanguages.Clear();
            foreach (var language in recipe?.Translations?.Keys.OrderBy(l => l, StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>())
            {
                TranslationLanguages.Add(language);
            }
            SelectedTranslationLanguage = TranslationLanguages.FirstOrDefault();
        }

        private void LoadTranslationRows()
        {
            TranslationRows.Clear();
            if (ActiveRecipe == null || !HasTranslationLanguage) return;
            foreach (var text in ActiveRecipe.GetTranslatableTexts())
            {
                TranslationRows.Add(new RecipeTranslationItemViewModel(() => ActiveRecipe, _selectedTranslationLanguage, text.Key, text.Value));
            }
        }

        public void AddTranslationLanguage()
        {
            string language = NewTranslationLanguage?.Trim();
            if (ActiveRecipe == null || string.IsNullOrEmpty(language)) return;
            if (!RecipeValidator.IsLanguageTag(language))
            {
                StatusMessage = string.Format(Texts.Get<IRecipeEditorLanguage>().StatusTranslationLanguageInvalid, language);
                return;
            }
            string existing = TranslationLanguages.FirstOrDefault(l => string.Equals(l, language, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                // The language is written into the file with its first text, a language without texts is an error
                TranslationLanguages.Add(language);
                existing = language;
            }
            NewTranslationLanguage = null;
            SelectedTranslationLanguage = existing;
        }

        public void RemoveTranslationLanguage()
        {
            string language = _selectedTranslationLanguage;
            if (ActiveRecipe == null || language == null) return;
            if (ActiveRecipe.Translations != null)
            {
                ActiveRecipe.Translations.Remove(language);
                if (ActiveRecipe.Translations.Count == 0)
                {
                    ActiveRecipe.Translations = null;
                }
            }
            TranslationLanguages.Remove(language);
            SelectedTranslationLanguage = TranslationLanguages.FirstOrDefault();
        }
    }

    /// <summary>
    /// One text of the recipe in one language: the text of the file and its translation
    /// </summary>
    public class RecipeTranslationItemViewModel : ViewModelBase
    {
        private readonly Func<FlowDefinition> _getDefinition;
        private readonly string _language;

        public RecipeTranslationItemViewModel(Func<FlowDefinition> getDefinition, string language, string path, string original)
        {
            _getDefinition = getDefinition;
            _language = language;
            Path = path;
            Original = original;
        }

        /// <summary>
        /// Where the text is, e.g. "name" or "options.folder.label"
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The text of the file
        /// </summary>
        public string Original { get; }

        /// <summary>
        /// The translation, empty for none (then the text of the file is shown)
        /// </summary>
        public string Text
        {
            get
            {
                var texts = FindTexts(_getDefinition()?.Translations);
                return texts != null && texts.TryGetValue(Path, out var text) ? text : "";
            }
            set
            {
                var definition = _getDefinition();
                if (definition == null) return;
                var texts = FindTexts(definition.Translations);
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (texts == null) return;
                    texts.Remove(Path);
                    if (texts.Count == 0)
                    {
                        definition.Translations.Remove(definition.Translations.First(t => ReferenceEquals(t.Value, texts)).Key);
                        if (definition.Translations.Count == 0) definition.Translations = null;
                    }
                }
                else
                {
                    if (texts == null)
                    {
                        definition.Translations ??= new Dictionary<string, Dictionary<string, string>>();
                        texts = new Dictionary<string, string>();
                        definition.Translations[_language] = texts;
                    }
                    texts[Path] = value.Trim();
                }
                OnPropertyChanged();
            }
        }

        private Dictionary<string, string> FindTexts(Dictionary<string, Dictionary<string, string>> translations) =>
            translations?.FirstOrDefault(t => string.Equals(t.Key, _language, StringComparison.OrdinalIgnoreCase) && t.Value != null).Value;
    }
}
