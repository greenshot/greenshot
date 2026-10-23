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

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// One transition of the flow, in the recipe file one entry of "transitions":
    /// { "from": "a", "to": "b" } always, { "from": "check", "to": "b", "branch": "true" } when the branch of a decision node was taken,
    /// { "from": "upload", "to": "save", "onError": "*" } when "from" failed ("from" "*" for every node, "onError" the exception type or "*"),
    /// and instead of "to" "recipe": "other_recipe" to continue with another recipe after an error.
    /// </summary>
    public class RecipeTransition
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Branch { get; set; }
        public string OnError { get; set; }
        public string Recipe { get; set; }

        public RecipeTransition()
        {
        }

        public RecipeTransition(string from, string to)
        {
            From = from;
            To = to;
        }

        /// <summary>
        /// Followed when the node failed
        /// </summary>
        [JsonIgnore]
        public bool IsError => OnError != null;

        /// <summary>
        /// Followed when the branch of the decision node was taken
        /// </summary>
        [JsonIgnore]
        public bool IsBranch => !IsError && Branch != null;

        /// <summary>
        /// Followed always after the node ran
        /// </summary>
        [JsonIgnore]
        public bool IsPlain => !IsError && Branch == null;

        public RecipeTransition Clone() => (RecipeTransition)MemberwiseClone();

        public override string ToString() => IsError
            ? $"{From} [Error:{OnError}] -> {(string.IsNullOrEmpty(To) ? $"Recipe:{Recipe}" : To)}"
            : IsBranch ? $"{From} [{Branch}] -> {To}" : $"{From} -> {To}";
    }

    /// <summary>
    /// Flow definition for a DAG capture recipe: the entry nodes and the transitions between nodes.
    /// Supports splitting onto multiple nodes and merging paths, while disallowing cycles/loops.
    /// </summary>
    public class RecipeFlowConfig
    {
        /// <summary>
        /// List of entry nodes where execution starts in the DAG.
        /// </summary>
        public List<string> StartNodes { get; set; } = new List<string>();

        /// <summary>
        /// All transitions: plain, branch and error ones, see <see cref="RecipeTransition"/>.
        /// </summary>
        public List<RecipeTransition> Transitions { get; set; } = new List<RecipeTransition>();

        /// <summary>
        /// The transitions followed when the branch of a decision node was taken
        /// </summary>
        [JsonIgnore]
        public IEnumerable<RecipeTransition> ConditionalTransitions => (Transitions ?? Enumerable.Empty<RecipeTransition>()).Where(t => t != null && t.IsBranch);

        /// <summary>
        /// The transitions followed when a node failed
        /// </summary>
        [JsonIgnore]
        public IEnumerable<RecipeTransition> ErrorTransitions => (Transitions ?? Enumerable.Empty<RecipeTransition>()).Where(t => t != null && t.IsError);

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
        /// The targets of the plain transitions of the node
        /// </summary>
        public List<string> GetTargets(string fromNodeId) =>
            (Transitions ?? Enumerable.Empty<RecipeTransition>())
            .Where(t => t != null && t.IsPlain && !string.IsNullOrWhiteSpace(t.To) && string.Equals(t.From, fromNodeId, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.To)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        /// <summary>
        /// The targets of the plain and the branch transitions per node (not the error transitions).
        /// </summary>
        public Dictionary<string, List<string>> GetUnifiedTransitions()
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var transition in (Transitions ?? Enumerable.Empty<RecipeTransition>()).Where(t => t != null && !t.IsError))
            {
                if (string.IsNullOrWhiteSpace(transition.From) || string.IsNullOrWhiteSpace(transition.To)) continue;
                if (!map.TryGetValue(transition.From, out var list))
                {
                    list = new List<string>();
                    map[transition.From] = list;
                }

                if (!list.Contains(transition.To, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(transition.To);
                }
            }

            return map;
        }

        private RecipeFlowConfig Add(RecipeTransition transition)
        {
            Transitions ??= new List<RecipeTransition>();
            bool exists = Transitions.Any(t => t != null &&
                string.Equals(t.From, transition.From, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.To, transition.To, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.Branch, transition.Branch, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.OnError, transition.OnError, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.Recipe, transition.Recipe, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                Transitions.Add(transition);
            }
            return this;
        }

        /// <summary>
        /// Adds a transition from one node to another.
        /// </summary>
        public RecipeFlowConfig AddTransition(string fromNodeId, string toNodeId)
        {
            if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(toNodeId)) return this;
            return Add(new RecipeTransition(fromNodeId, toNodeId));
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
            return Add(new RecipeTransition(fromNodeId, toNodeId) { Branch = branchKey });
        }

        /// <summary>
        /// Adds an error-handling transition from a node to another target node within the recipe.
        /// </summary>
        public RecipeFlowConfig AddErrorTransition(string fromNodeId, string toNodeId, string errorType = "*")
        {
            if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(toNodeId)) return this;
            return Add(new RecipeTransition(fromNodeId, toNodeId) { OnError = string.IsNullOrWhiteSpace(errorType) ? "*" : errorType });
        }

        /// <summary>
        /// Adds an error-handling transition that forwards the capture context to another recipe when a node fails.
        /// </summary>
        public RecipeFlowConfig AddErrorRecipeTransition(string fromNodeId, string targetRecipeId, string errorType = "*")
        {
            if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(targetRecipeId)) return this;
            return Add(new RecipeTransition { From = fromNodeId, Recipe = targetRecipeId, OnError = string.IsNullOrWhiteSpace(errorType) ? "*" : errorType });
        }

        /// <summary>
        /// Finds the best matching error transition for the given node and exception.
        /// </summary>
        public RecipeTransition GetErrorTransition(string fromNodeId, Exception ex)
        {
            var errorTransitions = ErrorTransitions.ToList();
            if (errorTransitions.Count == 0 || string.IsNullOrWhiteSpace(fromNodeId))
            {
                return null;
            }

            string exType = ex?.GetType().Name ?? "";
            string innerType = ex?.InnerException?.GetType().Name ?? "";

            bool MatchesErrorType(RecipeTransition et)
            {
                return string.Equals(et.OnError, exType, StringComparison.OrdinalIgnoreCase) ||
                       (!string.IsNullOrEmpty(innerType) && string.Equals(et.OnError, innerType, StringComparison.OrdinalIgnoreCase));
            }

            bool IsWildcardErrorType(RecipeTransition et)
            {
                return string.Equals(et.OnError, "*", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(et.OnError);
            }

            bool MatchesExactFrom(RecipeTransition et)
            {
                return string.Equals(et.From, fromNodeId, StringComparison.OrdinalIgnoreCase);
            }

            bool IsWildcardFrom(RecipeTransition et)
            {
                return string.Equals(et.From, "*", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(et.From);
            }

            // 1. Exact node + exact error type
            var match = errorTransitions.FirstOrDefault(et => MatchesExactFrom(et) && MatchesErrorType(et));
            if (match != null) return match;

            // 2. Exact node + wildcard error type
            match = errorTransitions.FirstOrDefault(et => MatchesExactFrom(et) && IsWildcardErrorType(et));
            if (match != null) return match;

            // 3. Wildcard node ("*" or empty) + exact error type
            match = errorTransitions.FirstOrDefault(et => IsWildcardFrom(et) && MatchesErrorType(et));
            if (match != null) return match;

            // 4. Wildcard node ("*" or empty) + wildcard error type ("*" or empty)
            return errorTransitions.FirstOrDefault(et => IsWildcardFrom(et) && IsWildcardErrorType(et));
        }

        public RecipeFlowConfig Clone()
        {
            return new RecipeFlowConfig
            {
                StartNodes = new List<string>(StartNodes ?? Enumerable.Empty<string>()),
                Transitions = (Transitions ?? Enumerable.Empty<RecipeTransition>()).Where(t => t != null).Select(t => t.Clone()).ToList()
            };
        }
    }
}
