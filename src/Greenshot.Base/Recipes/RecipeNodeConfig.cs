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
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes.Expressions;
using Greenshot.Base.Recipes.Pipeline;
using Newtonsoft.Json.Linq;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Configuration for an individual node within a DAG capture recipe.
    /// Each node has a unique flow-local ID, a step/node type, and configuration parameters.
    /// </summary>
    public class RecipeNodeConfig
    {
        /// <summary>
        /// Unique flow-local identifier for this node (e.g. "acquire_screen", "add_watermark", "save_file").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The step type / action executed by this node (e.g. "Source", "Drawable", "Effect", "Destinations", "SetVariable").
        /// </summary>
        public string StepType { get; set; }

        /// <summary>
        /// Optional human-readable display name for logs and diagnostics.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether this node is active in the flow. Disabled nodes are skipped during execution.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// An expression deciding at run time whether the node runs, e.g. "${option.border}" to switch a step with a recipe option.
        /// When set, it replaces <see cref="Enabled"/>; a node which doesn't run passes the flow on to the next nodes.
        /// In the recipe file both are "enabled": true, false or the expression.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string EnabledExpression { get; set; }

        [Newtonsoft.Json.JsonProperty("enabled")]
        private object EnabledJson
        {
            get => string.IsNullOrWhiteSpace(EnabledExpression) ? Enabled : (object)EnabledExpression;
            set
            {
                EnabledExpression = null;
                if (value is JValue jValue)
                {
                    value = jValue.Value;
                }
                switch (value)
                {
                    case bool enabled:
                        Enabled = enabled;
                        break;
                    case string text when bool.TryParse(text.Trim(), out var parsed):
                        Enabled = parsed;
                        break;
                    case string text when !string.IsNullOrWhiteSpace(text):
                        Enabled = true;
                        EnabledExpression = text.Trim();
                        break;
                    default:
                        Enabled = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Optional fallback step node ID within the same recipe to route to when this node encounters an error.
        /// </summary>
        public string OnErrorNodeId { get; set; }

        /// <summary>
        /// Optional fallback recipe ID to invoke when this node encounters an error.
        /// </summary>
        public string OnErrorRecipeId { get; set; }

        private Dictionary<string, object> _parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Configuration parameter dictionary for node execution. Supports dynamic expression evaluation (${...}).
        /// </summary>
        public Dictionary<string, object> Parameters
        {
            get => _parameters;
            set => _parameters = value != null
                ? new Dictionary<string, object>(value, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public RecipeNodeConfig()
        {
        }

        public RecipeNodeConfig(string id, string stepType, string name = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            StepType = stepType ?? throw new ArgumentNullException(nameof(stepType));
            Name = name ?? id;
        }

        /// <summary>
        /// Checks whether the parameter key is present in the configuration.
        /// </summary>
        public bool HasParameter(string key) => _parameters != null && !string.IsNullOrEmpty(key) && _parameters.ContainsKey(key);

        public T GetParameter<T>(string key, T defaultValue = default)
        {
            if (Parameters != null && Parameters.TryGetValue(key, out var val) && val != null)
            {
                if (val is T typed)
                {
                    return typed;
                }

                if (val is JToken jToken)
                {
                    try
                    {
                        return jToken.ToObject<T>();
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                try
                {
                    var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                    if (targetType.IsEnum)
                    {
                        if (val is string str)
                        {
                            return (T)Enum.Parse(targetType, str, true);
                        }
                        return (T)Enum.ToObject(targetType, val);
                    }

                    if (typeof(T) == typeof(List<string>) && val is IEnumerable enumerable && !(val is string))
                    {
                        var list = new List<string>();
                        foreach (var item in enumerable)
                        {
                            if (item != null) list.Add(item.ToString());
                        }
                        return (T)(object)list;
                    }

                    return (T)Convert.ChangeType(val, targetType);
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public RecipeNodeConfig Set(string key, object value)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            Parameters[key] = value;
            return this;
        }

        public RecipeNodeConfig WithName(string name)
        {
            Name = name;
            return this;
        }

        /// <summary>
        /// Whether the node runs in this flow: it is enabled and its <see cref="EnabledExpression"/>, if any, is true.
        /// An expression which can't be evaluated counts as false.
        /// </summary>
        public bool ShouldRun(CaptureFlowContext context)
        {
            if (!Enabled) return false;
            if (string.IsNullOrWhiteSpace(EnabledExpression)) return true;
            return ExpressionEvaluator.Instance.Evaluate(EnabledExpression, context, false);
        }

        /// <summary>
        /// Provenance: the id of the extension which put this node into a composed recipe, null for the recipe's own nodes.
        /// Set only by <see cref="RecipeComposer"/>, never read from a file.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string ContributedBy { get; set; }

        public RecipeNodeConfig Clone()
        {
            var clone = new RecipeNodeConfig
            {
                Id = Id,
                StepType = StepType,
                Name = Name,
                Enabled = Enabled,
                EnabledExpression = EnabledExpression,
                OnErrorNodeId = OnErrorNodeId,
                OnErrorRecipeId = OnErrorRecipeId,
                ContributedBy = ContributedBy,
                Parameters = new Dictionary<string, object>(Parameters, StringComparer.OrdinalIgnoreCase)
            };
            return clone;
        }

        public override string ToString() => $"[{Id}] {Name ?? StepType}";
    }
}
