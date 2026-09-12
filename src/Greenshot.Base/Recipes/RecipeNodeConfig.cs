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
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Configuration parameter dictionary for node execution. Supports dynamic expression evaluation (${...}).
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public RecipeNodeConfig()
        {
        }

        public RecipeNodeConfig(string id, string stepType, string name = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            StepType = stepType ?? throw new ArgumentNullException(nameof(stepType));
            Name = name ?? id;
        }

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

        public RecipeNodeConfig Clone()
        {
            var clone = new RecipeNodeConfig
            {
                Id = Id,
                StepType = StepType,
                Name = Name,
                Enabled = Enabled,
                Parameters = new Dictionary<string, object>(Parameters, StringComparer.OrdinalIgnoreCase)
            };
            return clone;
        }

        public override string ToString() => $"[{Id}] {Name ?? StepType}";
    }
}
