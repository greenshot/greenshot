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
using Newtonsoft.Json;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Represents a branch-specific transition originating from a Conditional node.
    /// </summary>
    public class RecipeConditionalTransitionConfig
    {
        public string From { get; set; }
        public string Branch { get; set; }
        public string To { get; set; }

        public RecipeConditionalTransitionConfig()
        {
        }

        public RecipeConditionalTransitionConfig(string from, string branch, string to)
        {
            From = from;
            Branch = branch;
            To = to;
        }

        public override string ToString() => $"{From} [{Branch}] -> {To}";
    }

    /// <summary>
    /// Flow definition for a DAG capture recipe. Defines entry nodes, standard transitions, and conditional transitions between nodes.
    /// Supports splitting onto multiple nodes and merging paths, while disallowing cycles/loops.
    /// </summary>
    public class RecipeFlowConfig
    {
        /// <summary>
        /// List of entry nodes where execution starts in the DAG.
        /// </summary>
        public List<string> StartNodes { get; set; } = new List<string>();

        /// <summary>
        /// Explicit transition mapping from a source node ID to a list of target node IDs.
        /// E.g. { "acquire": ["selection"], "selection": ["watermark", "effects"], "effects": ["export"], "watermark": ["export"] }
        /// </summary>
        [JsonConverter(typeof(TransitionsDictionaryConverter))]
        public Dictionary<string, List<string>> Transitions { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Branch-specific transitions originating from Conditional decision nodes.
        /// </summary>
        public List<RecipeConditionalTransitionConfig> ConditionalTransitions { get; set; } = new List<RecipeConditionalTransitionConfig>();

        public RecipeFlowConfig()
        {
        }

        public RecipeFlowConfig(string startNode)
        {
            if (!string.IsNullOrWhiteSpace(startNode))
            {
                StartNodes.Add(startNode);
            }
        }

        public RecipeFlowConfig(IEnumerable<string> startNodes)
        {
            if (startNodes != null)
            {
                StartNodes.AddRange(startNodes.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }

        /// <summary>
        /// Returns all effective starting / root nodes for the DAG.
        /// </summary>
        public List<string> GetEffectiveStartNodes()
        {
            var starts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (StartNodes != null)
            {
                foreach (var s in StartNodes)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        starts.Add(s);
                    }
                }
            }
            return starts.ToList();
        }

        /// <summary>
        /// Retrieves all unified transitions combining Transitions dictionary, ConditionalTransitions, and Edges list.
        /// </summary>
        public Dictionary<string, List<string>> GetUnifiedTransitions()
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (Transitions != null)
            {
                foreach (var kvp in Transitions)
                {
                    if (!map.TryGetValue(kvp.Key, out var list))
                    {
                        list = new List<string>();
                        map[kvp.Key] = list;
                    }

                    if (kvp.Value != null)
                    {
                        foreach (var target in kvp.Value)
                        {
                            if (!string.IsNullOrWhiteSpace(target) && !list.Contains(target, StringComparer.OrdinalIgnoreCase))
                            {
                                list.Add(target);
                            }
                        }
                    }
                }
            }

            if (ConditionalTransitions != null)
            {
                foreach (var ct in ConditionalTransitions)
                {
                    if (string.IsNullOrWhiteSpace(ct?.From) || string.IsNullOrWhiteSpace(ct?.To)) continue;

                    if (!map.TryGetValue(ct.From, out var list))
                    {
                        list = new List<string>();
                        map[ct.From] = list;
                    }

                    if (!list.Contains(ct.To, StringComparer.OrdinalIgnoreCase))
                    {
                        list.Add(ct.To);
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Adds a transition from one node to another.
        /// </summary>
        public RecipeFlowConfig AddTransition(string fromNodeId, string toNodeId)
        {
            if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(toNodeId)) return this;

            if (Transitions == null)
            {
                Transitions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            }

            if (!Transitions.TryGetValue(fromNodeId, out var list))
            {
                list = new List<string>();
                Transitions[fromNodeId] = list;
            }

            if (!list.Contains(toNodeId, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(toNodeId);
            }

            return this;
        }

        /// <summary>
        /// Adds transitions from one node to multiple target nodes (split/fork).
        /// </summary>
        public RecipeFlowConfig AddTransitions(string fromNodeId, IEnumerable<string> toNodeIds)
        {
            if (string.IsNullOrWhiteSpace(fromNodeId) || toNodeIds == null) return this;
            foreach (var to in toNodeIds)
            {
                AddTransition(fromNodeId, to);
            }
            return this;
        }

        /// <summary>
        /// Adds a conditional branch transition.
        /// </summary>
        public RecipeFlowConfig AddConditionalTransition(string fromNodeId, string branchKey, string toNodeId)
        {
            if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(branchKey) || string.IsNullOrWhiteSpace(toNodeId)) return this;

            if (ConditionalTransitions == null)
            {
                ConditionalTransitions = new List<RecipeConditionalTransitionConfig>();
            }

            bool exists = ConditionalTransitions.Any(c =>
                string.Equals(c.From, fromNodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.Branch, branchKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.To, toNodeId, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                ConditionalTransitions.Add(new RecipeConditionalTransitionConfig(fromNodeId, branchKey, toNodeId));
            }

            return this;
        }

        public RecipeFlowConfig Clone()
        {
            var clone = new RecipeFlowConfig
            {
                StartNodes = new List<string>(StartNodes ?? Enumerable.Empty<string>()),
                Transitions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
                ConditionalTransitions = new List<RecipeConditionalTransitionConfig>(ConditionalTransitions?.Count ?? 0)
            };

            if (Transitions != null)
            {
                foreach (var kvp in Transitions)
                {
                    clone.Transitions[kvp.Key] = new List<string>(kvp.Value);
                }
            }

            if (ConditionalTransitions != null)
            {
                foreach (var ct in ConditionalTransitions)
                {
                    clone.ConditionalTransitions.Add(new RecipeConditionalTransitionConfig(ct.From, ct.Branch, ct.To));
                }
            }

            return clone;
        }
    }
}
