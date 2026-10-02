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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Puts the extensions into the slots of a recipe: the effective recipe which runs. Pure: the inputs are not changed.
    /// <list type="bullet">
    /// <item>A Slot node is replaced by the chains of the extensions it accepts, by order, then by id: the first chain takes
    /// the slot's incoming edges, each chain's ends lead to the next chain, the last chain's ends take the slot's outgoing edges.</item>
    /// <item>The nodes of an extension get prefixed ids ("ext_border/border") and <see cref="RecipeNodeConfig.ContributedBy"/>;
    /// their ${option.key} becomes ${option.ext_border.key}, so they read the extension's options.</item>
    /// <item>An extension with "when" starts with a Conditional node which skips its chain when the expression is false.</item>
    /// <item>BeforeDestination slots stay where they are; their extensions become <see cref="CaptureRecipe.DestinationChains"/>,
    /// run by the export steps.</item>
    /// </list>
    /// </summary>
    public static class RecipeComposer
    {
        /// <summary>
        /// Between the extension id and the node id of a composed node
        /// </summary>
        public const string PrefixSeparator = "/";

        private const string WhenRunBranch = "run";
        private const string WhenSkipBranch = "skip";

        private static readonly Regex ExpressionPattern = new Regex(@"\$\{([^}]*)\}", RegexOptions.Compiled);
        private static readonly Regex OptionReferencePattern = new Regex(@"(?<![A-Za-z0-9_.])option\.(?=[A-Za-z])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Whether the extension targets the recipe by its "extends.recipes": an id, "*" or "*capture" (AI tool recipes only by id)
        /// </summary>
        public static bool Targets(RecipeExtension extension, CaptureRecipe recipe)
        {
            if (extension?.Extends?.Recipes == null || recipe?.Id == null) return false;
            foreach (var target in extension.Extends.Recipes.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()))
            {
                if (string.Equals(target, recipe.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (recipe.IsAiToolRecipe)
                {
                    continue;
                }
                if (target == RecipeExtension.TargetAll)
                {
                    return true;
                }
                if (string.Equals(target, RecipeExtension.TargetCaptures, StringComparison.OrdinalIgnoreCase) && recipe.HasSourceStep() && recipe.HasDestinationStep())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The Slot nodes of the recipe with the name, in node order
        /// </summary>
        public static IReadOnlyList<RecipeNodeConfig> FindSlots(FlowDefinition recipe, string slotName)
        {
            return recipe?.Nodes?.Where(n => n != null && string.Equals(RecipeSlots.GetSlotName(n), slotName, StringComparison.OrdinalIgnoreCase)).ToList()
                   ?? new List<RecipeNodeConfig>();
        }

        /// <summary>
        /// Whether the extension could change the recipe: it targets it and the recipe has a slot which accepts it.
        /// The user's settings (switched on, which recipes) are not looked at.
        /// </summary>
        public static bool CanExtend(RecipeExtension extension, CaptureRecipe recipe)
        {
            var slotName = extension?.SlotName;
            return slotName != null && Targets(extension, recipe) && FindSlots(recipe, slotName).Any(s => RecipeSlots.Accepts(s, extension.Id));
        }

        /// <summary>
        /// The extensions which change the recipe with these settings, by order, then by id
        /// </summary>
        public static IReadOnlyList<RecipeExtension> FindApplicableExtensions(CaptureRecipe recipe, IEnumerable<RecipeExtension> extensions, Func<RecipeExtension, RecipeExtensionSettings> settings = null)
        {
            if (recipe == null || extensions == null) return new List<RecipeExtension>();
            return extensions
                .Where(e => e?.Id != null && CanExtend(e, recipe) && (settings?.Invoke(e) ?? new RecipeExtensionSettings()).AllowsRecipe(recipe.Id))
                .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
                .OrderBy(e => e.Extends.Order).ThenBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The recipe with the extensions in its slots. Returns the recipe itself when no extension changes it, or when it was composed already.
        /// </summary>
        /// <param name="recipe">The recipe as loaded</param>
        /// <param name="extensions">The extensions which may change it (only valid ones)</param>
        /// <param name="settings">The user's settings of an extension; null: all switched on, for all recipes</param>
        public static CaptureRecipe Compose(CaptureRecipe recipe, IEnumerable<RecipeExtension> extensions, Func<RecipeExtension, RecipeExtensionSettings> settings = null)
        {
            if (recipe == null || recipe.IsComposed) return recipe;

            var applicable = FindApplicableExtensions(recipe, extensions, settings);
            if (applicable.Count == 0) return recipe;

            var composed = recipe.Clone();
            composed.IsComposed = true;
            var usedIds = new HashSet<string>(composed.Nodes.Where(n => n?.Id != null).Select(n => n.Id), StringComparer.OrdinalIgnoreCase);
            var insertions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var applied = new List<RecipeExtension>();
            var destinationChains = new List<ExtensionChain>();

            foreach (var slotNode in recipe.Nodes.Where(n => RecipeSlots.GetSlotName(n) != null).ToList())
            {
                string slotName = RecipeSlots.GetSlotName(slotNode);
                var chains = applicable.Where(e => e.SlotName == slotName && RecipeSlots.Accepts(slotNode, e.Id)).ToList();
                if (chains.Count == 0) continue;

                if (slotName == RecipeSlots.BeforeDestination)
                {
                    foreach (var extension in chains.Where(e => destinationChains.All(c => !string.Equals(c.Extension.Id, e.Id, StringComparison.OrdinalIgnoreCase))))
                    {
                        var onlyDestinations = (settings?.Invoke(extension) ?? new RecipeExtensionSettings()).OnlyDestinations;
                        destinationChains.Add(new ExtensionChain(extension, BuildStandalone(extension), onlyDestinations));
                        applied.Add(extension);
                    }
                    continue;
                }

                var built = chains.Select(extension => BuildChain(extension, NextPrefix(extension, slotNode, insertions), usedIds)).ToList();
                ReplaceSlot(composed, slotNode.Id, built);
                applied.AddRange(chains);
            }

            composed.AppliedExtensions = applied.GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
            composed.DestinationChains = destinationChains;
            return composed;
        }

        /// <summary>
        /// "ext_border/" the first time the extension goes into the recipe, "ext_border/&lt;slot node id&gt;/" for further slots
        /// </summary>
        private static string NextPrefix(RecipeExtension extension, RecipeNodeConfig slotNode, IDictionary<string, int> insertions)
        {
            insertions.TryGetValue(extension.Id, out int count);
            insertions[extension.Id] = count + 1;
            return count == 0
                ? extension.Id + PrefixSeparator
                : extension.Id + PrefixSeparator + slotNode.Id + PrefixSeparator;
        }

        /// <summary>
        /// An extension's chain, ready to be wired between a slot's incoming and outgoing edges
        /// </summary>
        private sealed class BuiltChain
        {
            public readonly List<RecipeNodeConfig> Nodes = new List<RecipeNodeConfig>();
            public readonly List<string> Entry = new List<string>();
            public readonly List<string> Starts = new List<string>();
            public readonly Dictionary<string, List<string>> Transitions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            public readonly List<RecipeConditionalTransitionConfig> ConditionalTransitions = new List<RecipeConditionalTransitionConfig>();
            public readonly List<RecipeErrorTransitionConfig> ErrorTransitions = new List<RecipeErrorTransitionConfig>();

            /// <summary>
            /// Nodes which lead on to the recipe: without outgoing edges, or with a transition to Out
            /// </summary>
            public readonly List<string> Ends = new List<string>();

            /// <summary>
            /// Conditional branches routed to Out
            /// </summary>
            public readonly List<(string From, string Branch)> BranchesToOut = new List<(string, string)>();

            public string WhenNodeId;
        }

        private static BuiltChain BuildChain(RecipeExtension extension, string prefix, ISet<string> usedIds)
        {
            var chain = new BuiltChain();
            var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in extension.Nodes.Where(n => n?.Id != null))
            {
                idMap[node.Id] = UniqueId(prefix + node.Id, usedIds);
            }

            string Map(string id) => id != null && idMap.TryGetValue(id, out var mapped) ? mapped : null;
            bool IsOut(string id) => string.Equals(id, RecipeExtension.OutNode, StringComparison.OrdinalIgnoreCase);

            foreach (var node in extension.Nodes.Where(n => n?.Id != null))
            {
                var clone = node.Clone();
                clone.Id = Map(node.Id);
                clone.ContributedBy = extension.Id;
                clone.Parameters = (Dictionary<string, object>)RewriteOptionReferences(clone.Parameters, extension.Id);
                clone.EnabledExpression = RewriteOptionReferences(clone.EnabledExpression, extension.Id);
                clone.OnErrorNodeId = Map(node.OnErrorNodeId);
                chain.Nodes.Add(clone);
            }

            var flow = extension.Flow ?? new RecipeFlowConfig();
            var toOut = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var transition in flow.Transitions ?? new Dictionary<string, List<string>>())
            {
                string from = Map(transition.Key);
                if (from == null) continue;
                foreach (var target in transition.Value ?? new List<string>())
                {
                    if (IsOut(target))
                    {
                        toOut.Add(from);
                    }
                    else if (Map(target) is string to)
                    {
                        AddTo(chain.Transitions, from, to);
                    }
                }
            }

            var conditionalSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ct in flow.ConditionalTransitions ?? new List<RecipeConditionalTransitionConfig>())
            {
                string from = Map(ct?.From);
                if (from == null) continue;
                conditionalSources.Add(from);
                if (IsOut(ct.To))
                {
                    chain.BranchesToOut.Add((from, ct.Branch));
                }
                else if (Map(ct.To) is string to)
                {
                    chain.ConditionalTransitions.Add(new RecipeConditionalTransitionConfig(from, ct.Branch, to));
                }
            }

            foreach (var et in flow.ErrorTransitions ?? new List<RecipeErrorTransitionConfig>())
            {
                // An extension's error transitions only apply to its own nodes
                string from = Map(et?.From);
                if (from == null) continue;
                chain.ErrorTransitions.Add(new RecipeErrorTransitionConfig(from, Map(et.To), et.TargetRecipeId, et.ErrorType));
            }

            var starts = flow.GetEffectiveStartNodes().Select(Map).Where(id => id != null).ToList();
            if (starts.Count == 0 && chain.Nodes.Count > 0)
            {
                starts.Add(chain.Nodes[0].Id);
            }
            chain.Starts.AddRange(starts);

            foreach (var node in chain.Nodes)
            {
                bool hasOutgoing = chain.Transitions.ContainsKey(node.Id) || conditionalSources.Contains(node.Id);
                if (!hasOutgoing || toOut.Contains(node.Id))
                {
                    chain.Ends.Add(node.Id);
                }
            }

            if (!string.IsNullOrWhiteSpace(extension.When))
            {
                chain.WhenNodeId = UniqueId(prefix + "when", usedIds);
                var whenNode = RecipeStepConfig.CreateConditional(chain.WhenNodeId, new[]
                {
                    new KeyValuePair<string, string>(WhenRunBranch, RewriteOptionReferences(extension.When.Trim(), extension.Id)),
                    new KeyValuePair<string, string>(WhenSkipBranch, "else")
                });
                whenNode.Name = $"{extension.Name ?? extension.Id}: when";
                whenNode.ContributedBy = extension.Id;
                chain.Nodes.Insert(0, whenNode);
                foreach (var start in chain.Starts)
                {
                    chain.ConditionalTransitions.Add(new RecipeConditionalTransitionConfig(chain.WhenNodeId, WhenRunBranch, start));
                }
                chain.Entry.Add(chain.WhenNodeId);
            }
            else
            {
                chain.Entry.AddRange(chain.Starts);
            }

            return chain;
        }

        /// <summary>
        /// Adds the chain to the flow, its ends leading to the exits
        /// </summary>
        private static void Connect(RecipeFlowConfig flow, BuiltChain chain, IReadOnlyList<string> exits)
        {
            foreach (var transition in chain.Transitions)
            {
                flow.AddTransitions(transition.Key, transition.Value);
            }
            foreach (var ct in chain.ConditionalTransitions)
            {
                flow.AddConditionalTransition(ct.From, ct.Branch, ct.To);
            }
            foreach (var et in chain.ErrorTransitions)
            {
                flow.ErrorTransitions ??= new List<RecipeErrorTransitionConfig>();
                flow.ErrorTransitions.Add(et);
            }
            foreach (var end in chain.Ends)
            {
                flow.AddTransitions(end, exits);
            }
            foreach (var (from, branch) in chain.BranchesToOut)
            {
                foreach (var exit in exits)
                {
                    flow.AddConditionalTransition(from, branch, exit);
                }
            }
            if (chain.WhenNodeId != null)
            {
                foreach (var exit in exits)
                {
                    flow.AddConditionalTransition(chain.WhenNodeId, WhenSkipBranch, exit);
                }
            }
        }

        private static void ReplaceSlot(CaptureRecipe composed, string slotId, IReadOnlyList<BuiltChain> chains)
        {
            var flow = composed.Flow ??= new RecipeFlowConfig();
            flow.Transitions ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            flow.ConditionalTransitions ??= new List<RecipeConditionalTransitionConfig>();
            flow.StartNodes ??= new List<string>();

            var exits = flow.Transitions.TryGetValue(slotId, out var slotTargets) && slotTargets != null ? slotTargets.ToList() : new List<string>();
            flow.Transitions.Remove(slotId);
            flow.ConditionalTransitions.RemoveAll(ct => string.Equals(ct?.From, slotId, StringComparison.OrdinalIgnoreCase));
            flow.ErrorTransitions?.RemoveAll(et => string.Equals(et?.From, slotId, StringComparison.OrdinalIgnoreCase));

            // Wired from the last chain to the first: each chain leads to the entry of the next one
            IReadOnlyList<string> next = exits;
            for (int i = chains.Count - 1; i >= 0; i--)
            {
                Connect(flow, chains[i], next);
                next = chains[i].Entry;
            }
            var entry = next;

            // The slot's incoming edges go to the first chain
            foreach (var key in flow.Transitions.Keys.ToList())
            {
                flow.Transitions[key] = ReplaceTarget(flow.Transitions[key], slotId, entry);
            }
            foreach (var ct in flow.ConditionalTransitions.Where(ct => string.Equals(ct?.To, slotId, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                flow.ConditionalTransitions.Remove(ct);
                foreach (var target in entry)
                {
                    flow.AddConditionalTransition(ct.From, ct.Branch, target);
                }
            }
            flow.StartNodes = ReplaceTarget(flow.StartNodes, slotId, entry);
            string firstEntry = entry.FirstOrDefault();
            foreach (var et in flow.ErrorTransitions?.Where(et => string.Equals(et?.To, slotId, StringComparison.OrdinalIgnoreCase)) ?? Enumerable.Empty<RecipeErrorTransitionConfig>())
            {
                et.To = firstEntry;
            }
            foreach (var node in composed.Nodes.Where(n => string.Equals(n?.OnErrorNodeId, slotId, StringComparison.OrdinalIgnoreCase)))
            {
                node.OnErrorNodeId = firstEntry;
            }

            // The chains' nodes take the place of the slot node
            int index = composed.Nodes.FindIndex(n => string.Equals(n?.Id, slotId, StringComparison.OrdinalIgnoreCase));
            if (index < 0) index = composed.Nodes.Count;
            else composed.Nodes.RemoveAt(index);
            composed.Nodes.InsertRange(index, chains.SelectMany(c => c.Nodes));
        }

        /// <summary>
        /// An extension's chain as a flow of its own (BeforeDestination): starts at its entry, its ends end the flow
        /// </summary>
        private static CaptureRecipe BuildStandalone(RecipeExtension extension)
        {
            var chain = BuildChain(extension, extension.Id + PrefixSeparator, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            var flow = new RecipeFlowConfig(chain.Entry);
            Connect(flow, chain, Array.Empty<string>());
            var standalone = new CaptureRecipe(extension.Id, extension.Name, extension.Description)
            {
                Nodes = chain.Nodes,
                Flow = flow,
                IsComposed = true,
                IsBuiltIn = extension.IsBuiltIn,
                AppliedExtensions = new[] { extension }
            };
            return standalone;
        }

        private static List<string> ReplaceTarget(List<string> targets, string oldTarget, IReadOnlyList<string> newTargets)
        {
            if (targets == null || !targets.Contains(oldTarget, StringComparer.OrdinalIgnoreCase)) return targets;
            var result = new List<string>();
            foreach (var target in targets)
            {
                var replacement = string.Equals(target, oldTarget, StringComparison.OrdinalIgnoreCase) ? newTargets : new[] { target };
                foreach (var id in replacement.Where(id => !result.Contains(id, StringComparer.OrdinalIgnoreCase)))
                {
                    result.Add(id);
                }
            }
            return result;
        }

        private static void AddTo(IDictionary<string, List<string>> transitions, string from, string to)
        {
            if (!transitions.TryGetValue(from, out var list))
            {
                list = new List<string>();
                transitions[from] = list;
            }
            if (!list.Contains(to, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(to);
            }
        }

        private static string UniqueId(string id, ISet<string> usedIds)
        {
            string unique = id;
            for (int i = 2; usedIds.Contains(unique); i++)
            {
                unique = $"{id}_{i}";
            }
            usedIds.Add(unique);
            return unique;
        }

        /// <summary>
        /// option.key in the ${...} of a text becomes option.&lt;extension id&gt;.key
        /// </summary>
        public static string RewriteOptionReferences(string text, string extensionId)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("option.", StringComparison.OrdinalIgnoreCase) < 0) return text;
            return ExpressionPattern.Replace(text, match => "${" + OptionReferencePattern.Replace(match.Groups[1].Value, "option." + extensionId + ".") + "}");
        }

        /// <summary>
        /// Rewrites the option references in all texts of a parameter value (texts, JSON, dictionaries and lists); unchanged values are returned as they are
        /// </summary>
        private static object RewriteOptionReferences(object value, string extensionId)
        {
            switch (value)
            {
                case null:
                    return null;
                case string text:
                    return RewriteOptionReferences(text, extensionId);
                case JToken token:
                    if (token.ToString(Newtonsoft.Json.Formatting.None).IndexOf("option.", StringComparison.OrdinalIgnoreCase) < 0) return token;
                    var copy = token.DeepClone();
                    var values = copy is JContainer container ? container.Descendants().OfType<JValue>() : new[] { (JValue)copy };
                    foreach (var jValue in values.Where(v => v.Type == JTokenType.String).ToList())
                    {
                        jValue.Value = RewriteOptionReferences((string)jValue.Value, extensionId);
                    }
                    return copy;
                case IDictionary<string, object> dictionary:
                    var rewritten = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    bool changed = false;
                    foreach (var pair in dictionary)
                    {
                        var newValue = RewriteOptionReferences(pair.Value, extensionId);
                        changed |= !ReferenceEquals(newValue, pair.Value);
                        rewritten[pair.Key] = newValue;
                    }
                    return changed || dictionary is Dictionary<string, object> ? rewritten : dictionary;
                case IDictionary<string, string> texts:
                    // e.g. the branches of a Conditional built in code
                    if (!texts.Values.Any(v => v != null && v.IndexOf("option.", StringComparison.OrdinalIgnoreCase) >= 0)) return texts;
                    var rewrittenTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in texts)
                    {
                        rewrittenTexts[pair.Key] = RewriteOptionReferences(pair.Value, extensionId);
                    }
                    return rewrittenTexts;
                case IEnumerable enumerable:
                    var items = enumerable.Cast<object>().ToList();
                    var newItems = items.Select(i => RewriteOptionReferences(i, extensionId)).ToList();
                    return items.Where((item, i) => !ReferenceEquals(item, newItems[i])).Any() ? newItems : value;
                default:
                    return value;
            }
        }
    }
}
