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
using System.Linq;
using System.Text.Json.Serialization;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// What recipes and recipe extensions have in common: a DAG of nodes with its flow, the options the user sets once
    /// and the plugins it needs. A <see cref="CaptureRecipe"/> adds triggers, a <see cref="RecipeExtension"/> adds where it
    /// goes into other recipes. Both are stored in the same file type, told apart by <see cref="Kind"/>.
    /// </summary>
    public abstract class FlowDefinition
    {
        public const string KindRecipe = "recipe";
        public const string KindExtension = "extension";

        /// <summary>
        /// "recipe" or "extension", written first in the file
        /// </summary>
        public abstract string Kind { get; }

        /// <summary>
        /// The JSON schema of the file, for editors (e.g. "../recipe.schema.json"); Greenshot doesn't read it
        /// </summary>
        [JsonPropertyName("$schema"), JsonPropertyOrder(-20)]
        public string Schema { get; set; }

        /// <summary>
        /// The version of the recipe format: "2.0" since transitions are one list and conditions are in "when".
        /// </summary>
        public string Version { get; set; } = CurrentVersion;

        public const string CurrentVersion = "2.0";

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// The name in the language of Greenshot: from <see cref="Translations"/>, else the name of the file
        /// </summary>
        [JsonIgnore]
        public string DisplayName => Translate(TranslationName, Name ?? Id);

        /// <summary>
        /// The description in the language of Greenshot: from <see cref="Translations"/>, else the description of the file
        /// </summary>
        [JsonIgnore]
        public string DisplayDescription => Translate(TranslationDescription, Description);

        /// <summary>
        /// Who wrote it, shown when the recipe is approved; kept in the file so the credit travels with it
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RecipeAuthor> Authors { get; set; }

        /// <summary>
        /// The copyright notice, e.g. "Copyright (c) 2026 Jane Doe", which the license may require to stay with every copy
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Copyright { get; set; }

        /// <summary>
        /// The license of the file as an SPDX identifier, e.g. "MIT"
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string License { get; set; }

        /// <summary>
        /// Explicit extension or plugin dependencies required to execute this flow.
        /// </summary>
        public List<RecipeRequirement> Requires { get; set; } = new List<RecipeRequirement>();

        /// <summary>
        /// Specified flow-local nodes configured for execution.
        /// </summary>
        public List<RecipeNodeConfig> Nodes { get; set; } = new List<RecipeNodeConfig>();

        /// <summary>
        /// Flow definition specifying entry point(s) and node transitions (edges).
        /// </summary>
        public RecipeFlowConfig Flow { get; set; } = new RecipeFlowConfig();

        /// <summary>
        /// Values the user sets once in Settings > Recipes, read by the nodes with ${option.key}. See <see cref="RecipeOption"/>.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RecipeOption> Options
        {
            get => _options;
            set
            {
                _options = value;
                // The options look up their texts in the translations of the file they belong to
                if (value == null) return;
                foreach (var option in value.Where(o => o != null))
                {
                    option.Owner = this;
                }
            }
        }
        private List<RecipeOption> _options;

        public const string TranslationName = "name";
        public const string TranslationDescription = "description";

        /// <summary>
        /// The texts of the file in other languages, by language (e.g. "de-DE" or "de") and then by path: "name", "description",
        /// "options.{key}.label", "options.{key}.description" and "options.{key}.choices.{value}". The texts of the file
        /// itself are used for every language which isn't here, see <see cref="Translate"/>.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), JsonPropertyOrder(20)]
        public Dictionary<string, Dictionary<string, string>> Translations { get; set; }

        /// <summary>
        /// The text for the path in the language of Greenshot: the exact language (de-DE), else the language alone (de),
        /// else the text of the file. A built-in text in the form Section.key is translated by the language files of Greenshot.
        /// </summary>
        /// <param name="path">e.g. "name" or "options.folder.label"</param>
        /// <param name="text">the text of the file</param>
        public string Translate(string path, string text) => Translate(path, text, CurrentLanguage());

        /// <summary>
        /// The text for the path in the given language, see <see cref="Translate(string,string)"/>
        /// </summary>
        public string Translate(string path, string text, string language)
        {
            if (Translations != null && Translations.Count > 0 && path != null)
            {
                if (!string.IsNullOrEmpty(language))
                {
                    int dash = language.IndexOf('-');
                    if (TryGetTranslation(language, path, out var translated) ||
                        dash > 0 && TryGetTranslation(language.Substring(0, dash), path, out translated))
                    {
                        return translated;
                    }
                }
            }
            return RecipeText.Translate(text);
        }

        private bool TryGetTranslation(string language, string path, out string translated)
        {
            foreach (var entry in Translations)
            {
                if (string.Equals(entry.Key, language, StringComparison.OrdinalIgnoreCase) &&
                    entry.Value != null && entry.Value.TryGetValue(path, out translated) && !string.IsNullOrWhiteSpace(translated))
                {
                    return true;
                }
            }
            translated = null;
            return false;
        }

        private static string CurrentLanguage()
        {
            try
            {
                return Texts.Config.CurrentLanguage;
            }
            catch (Exception)
            {
                // No languages loaded (tests, design time)
                return null;
            }
        }

        /// <summary>
        /// Indicates if this is one of Greenshot's built-in recipes or extensions. Set by Greenshot, never read from a file.
        /// </summary>
        [JsonIgnore]
        public bool IsBuiltIn { get; set; }

        public RecipeOption FindOption(string key)
        {
            return Options?.FirstOrDefault(o => o != null && string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        [JsonIgnore]
        public bool HasOptions => Options != null && Options.Any(o => o != null);

        public RecipeNodeConfig FindNode(string nodeId)
        {
            return Nodes?.FirstOrDefault(n => string.Equals(n.Id, nodeId, StringComparison.OrdinalIgnoreCase));
        }

        public RecipeNodeConfig FindFirstNodeByType(string stepType)
        {
            return Nodes?.FirstOrDefault(n => string.Equals(n.StepType, stepType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks whether the flow contains any destination/export steps.
        /// </summary>
        public bool HasDestinationStep()
        {
            if (Nodes == null || Nodes.Count == 0) return false;
            return Nodes.Any(n => n != null && StepRegistry.Instance.IsDestinationStep(n.StepType));
        }

        /// <summary>
        /// Determines whether the flow contains any source step.
        /// </summary>
        public bool HasSourceStep()
        {
            if (Nodes == null || Nodes.Count == 0) return false;
            return Nodes.Any(node =>
                string.Equals(node?.StepType, WellKnownStepTypes.Source, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(node?.StepType, WellKnownStepTypes.RecordVideo, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Copies the common parts into a clone
        /// </summary>
        protected void CopyTo(FlowDefinition clone)
        {
            clone.Schema = Schema;
            clone.Version = Version;
            clone.Id = Id;
            clone.Name = Name;
            clone.Description = Description;
            clone.Authors = Authors?.Select(a => a?.Clone()).ToList();
            clone.Copyright = Copyright;
            clone.License = License;
            clone.IsBuiltIn = IsBuiltIn;
            clone.Requires = Requires?.ToList() ?? new List<RecipeRequirement>();
            clone.Nodes = Nodes?.Select(n => n?.Clone()).ToList() ?? new List<RecipeNodeConfig>();
            clone.Flow = Flow?.Clone() ?? new RecipeFlowConfig();
            clone.Options = Options?.Select(o => o?.Clone()).ToList();
            clone.Translations = Translations?.ToDictionary(t => t.Key, t => t.Value == null ? null : new Dictionary<string, string>(t.Value));
        }

        public override string ToString() => Name ?? Id;
    }
}
