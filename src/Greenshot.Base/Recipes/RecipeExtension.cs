/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using Greenshot.Base.Pipeline.Contracts;
using Newtonsoft.Json;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// A recipe extension: a small flow between In and Out which Greenshot puts into a slot of other recipes (built-in ones
    /// included), e.g. a border before the export of every capture. It has no triggers and no source, can't change the
    /// nodes of the recipe it extends and can't extend another extension. See <see cref="RecipeComposer"/>.
    /// </summary>
    public class RecipeExtension : FlowDefinition
    {
        /// <summary>
        /// A transition target in an extension flow: where the extension ends and the recipe goes on
        /// </summary>
        public const string OutNode = "Out";

        /// <summary>
        /// The node ids an extension can't use (In is where it starts, its startNodes)
        /// </summary>
        public static readonly IReadOnlyCollection<string> ReservedNodeIds = new[] { "In", OutNode };

        /// <summary>
        /// The key of the Boolean option which switches the extension on and off; an extension without one is always on
        /// </summary>
        public const string EnabledOptionKey = "enabled";

        /// <summary>
        /// For <see cref="ExtensionTarget.Recipes"/>: every recipe with a slot (AI tool recipes only by id)
        /// </summary>
        public const string TargetAll = "*";

        /// <summary>
        /// For <see cref="ExtensionTarget.Recipes"/>: every recipe with a Source step and a destination (AI tool recipes only by id)
        /// </summary>
        public const string TargetCaptures = "*capture";

        /// <summary>
        /// "Apply to": all the recipes the extension targets, or only the ones the user picked. Set in Settings > Recipes.
        /// </summary>
        public static readonly RecipeOption ApplyToOption = new RecipeOption
        {
            Key = "applyTo",
            Type = ContractDataType.Enum,
            DefaultValue = ApplyToAll,
            Choices = new List<RecipeOptionChoice>
            {
                new RecipeOptionChoice { Value = ApplyToAll },
                new RecipeOptionChoice { Value = ApplyToOnly }
            }
        };

        public const string ApplyToAll = "all";
        public const string ApplyToOnly = "only";

        /// <summary>
        /// The recipe ids for "Apply to: only these recipes", comma separated
        /// </summary>
        public static readonly RecipeOption OnlyRecipesOption = new RecipeOption { Key = "onlyRecipes", Type = ContractDataType.String, DefaultValue = string.Empty };

        /// <summary>
        /// The recipe ids the extension doesn't change ("except these recipes"), comma separated
        /// </summary>
        public static readonly RecipeOption ExceptRecipesOption = new RecipeOption { Key = "exceptRecipes", Type = ContractDataType.String, DefaultValue = string.Empty };

        /// <summary>
        /// BeforeDestination only: the destination designations it runs for ("Only for these destinations"), empty for all
        /// </summary>
        public static readonly RecipeOption OnlyDestinationsOption = new RecipeOption { Key = "onlyDestinations", Type = ContractDataType.String, DefaultValue = string.Empty };

        /// <summary>
        /// The settings every extension has, stored like its options (they can't be declared as options)
        /// </summary>
        public static readonly IReadOnlyList<RecipeOption> ScopeOptions = new[] { ApplyToOption, OnlyRecipesOption, ExceptRecipesOption, OnlyDestinationsOption };

        public override string Kind => KindExtension;

        /// <summary>
        /// Which recipes it extends, at which slot and in which order
        /// </summary>
        public ExtensionTarget Extends { get; set; } = new ExtensionTarget();

        /// <summary>
        /// Optional expression evaluated at the slot, e.g. "${payload.width > 800}"; when false the chain is skipped for that run
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string When { get; set; }

        /// <summary>
        /// The file the extension was loaded from, null for a built-in one
        /// </summary>
        [JsonIgnore]
        public string FilePath { get; set; }

        /// <summary>
        /// The AI tool which wrote the file, null when it wasn't written by an AI tool. Set from the approval, never read from the file.
        /// </summary>
        [JsonIgnore]
        public string ProposedBy { get; set; }

        /// <summary>
        /// A file which replaces the built-in extension with the same id
        /// </summary>
        [JsonIgnore]
        public bool IsOverridden { get; set; }

        /// <summary>
        /// The extension as a recipe without triggers, for what works on recipes: describing it, its approval and its step contracts
        /// </summary>
        public CaptureRecipe AsRecipeView()
        {
            var view = new CaptureRecipe(Id ?? "extension", Name, Description)
            {
                FilePath = FilePath,
                IsBuiltIn = IsBuiltIn,
                Triggers = new List<Triggers.TriggerConfig>()
            };
            var copy = Clone();
            view.Version = copy.Version;
            view.Requires = copy.Requires;
            view.Nodes = copy.Nodes;
            view.Flow = copy.Flow;
            view.Options = copy.Options;
            return view;
        }

        public RecipeExtension()
        {
        }

        public RecipeExtension(string id, string name, string description = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? id;
            Description = description;
        }

        public RecipeExtension AddNode(RecipeNodeConfig node)
        {
            if (node != null)
            {
                if (Nodes == null) Nodes = new List<RecipeNodeConfig>();
                Nodes.Add(node);
            }
            return this;
        }

        public RecipeExtension AddOption(RecipeOption option)
        {
            if (option != null)
            {
                if (Options == null) Options = new List<RecipeOption>();
                Options.Add(option);
            }
            return this;
        }

        /// <summary>
        /// The Boolean option which switches the extension, null when it is always on
        /// </summary>
        [JsonIgnore]
        public RecipeOption EnabledOption
        {
            get
            {
                var option = FindOption(EnabledOptionKey);
                return option?.Type == ContractDataType.Boolean ? option : null;
            }
        }

        /// <summary>
        /// The slot name, normalized; null when unknown
        /// </summary>
        [JsonIgnore]
        public string SlotName => RecipeSlots.Normalize(Extends?.Slot);

        public RecipeExtension Clone()
        {
            var clone = new RecipeExtension
            {
                Extends = Extends?.Clone() ?? new ExtensionTarget(),
                When = When,
                FilePath = FilePath,
                ProposedBy = ProposedBy,
                IsOverridden = IsOverridden
            };
            CopyTo(clone);
            return clone;
        }
    }

    /// <summary>
    /// Where an extension goes: <c>"extends": { "recipes": [ "*capture" ], "slot": "BeforeExport", "order": 100 }</c>
    /// </summary>
    public class ExtensionTarget
    {
        /// <summary>
        /// Recipe ids, "*" (every recipe with the slot) or "*capture" (recipes with a Source step and a destination).
        /// AI tool recipes are only changed when named by id.
        /// </summary>
        public List<string> Recipes { get; set; } = new List<string>();

        /// <summary>
        /// AfterCapture, BeforeExport, AfterExport or BeforeDestination
        /// </summary>
        public string Slot { get; set; }

        /// <summary>
        /// Several extensions on one slot run by order, then by id
        /// </summary>
        public int Order { get; set; }

        public ExtensionTarget Clone() => new ExtensionTarget { Recipes = Recipes?.ToList() ?? new List<string>(), Slot = Slot, Order = Order };
    }

    /// <summary>
    /// The settings of an extension the user picks in Settings > Recipes: switched on, which recipes, which destinations
    /// </summary>
    public class RecipeExtensionSettings
    {
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// False for "only these recipes" (<see cref="OnlyRecipes"/>)
        /// </summary>
        public bool ApplyToAll { get; set; } = true;

        public ISet<string> OnlyRecipes { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ISet<string> ExceptRecipes { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// BeforeDestination: the designations it runs for, empty for all
        /// </summary>
        public ISet<string> OnlyDestinations { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Whether the user's choice lets the extension change the recipe
        /// </summary>
        public bool AllowsRecipe(string recipeId)
        {
            if (!Enabled || string.IsNullOrEmpty(recipeId)) return false;
            if (ExceptRecipes.Contains(recipeId)) return false;
            return ApplyToAll || OnlyRecipes.Contains(recipeId);
        }

        /// <summary>
        /// The settings as stored in greenshot.ini ([RecipeOptions])
        /// </summary>
        public static RecipeExtensionSettings FromStore(RecipeExtension extension)
        {
            var settings = new RecipeExtensionSettings();
            if (extension == null) return settings;
            var enabledOption = extension.EnabledOption;
            settings.Enabled = enabledOption == null || RecipeOptionStore.GetValue(extension, enabledOption) is true;
            settings.ApplyToAll = !string.Equals(RecipeOptionStore.GetValue(extension, RecipeExtension.ApplyToOption) as string, RecipeExtension.ApplyToOnly, StringComparison.OrdinalIgnoreCase);
            settings.OnlyRecipes = ToSet(RecipeOptionStore.GetValue(extension, RecipeExtension.OnlyRecipesOption));
            settings.ExceptRecipes = ToSet(RecipeOptionStore.GetValue(extension, RecipeExtension.ExceptRecipesOption));
            settings.OnlyDestinations = ToSet(RecipeOptionStore.GetValue(extension, RecipeExtension.OnlyDestinationsOption));
            return settings;
        }

        private static ISet<string> ToSet(object value) => new HashSet<string>(RecipeSlots.SplitList(value as string), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// An extension's flow as it runs on its own: for the BeforeDestination slot, run by the export steps once per destination
    /// </summary>
    public class ExtensionChain
    {
        public ExtensionChain(RecipeExtension extension, CaptureRecipe flow, IEnumerable<string> onlyDestinations)
        {
            Extension = extension ?? throw new ArgumentNullException(nameof(extension));
            Flow = flow ?? throw new ArgumentNullException(nameof(flow));
            OnlyDestinations = new HashSet<string>(onlyDestinations ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        public RecipeExtension Extension { get; }

        /// <summary>
        /// The chain with prefixed node ids, ready to run
        /// </summary>
        public CaptureRecipe Flow { get; }

        /// <summary>
        /// The destination designations it runs for, empty for all
        /// </summary>
        public ISet<string> OnlyDestinations { get; }

        public bool RunsFor(string designation) => OnlyDestinations.Count == 0 || (designation != null && OnlyDestinations.Contains(designation));

        public override string ToString() => $"{Extension.Id} ({Flow.Nodes.Count} node(s))";
    }
}
