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
using System.Drawing;
using System.Globalization;
using Greenshot.Base.Expressions;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Base.Recipes.Expressions
{
    /// <summary>
    /// The ${...} of recipes: parameters, conditions and texts, with the values of the running recipe. One engine with the settings,
    /// see <see cref="VariableResolver"/>.
    /// </summary>
    public static class RecipeExpressions
    {
        /// <summary>The scope of a running recipe, with extra values by full name (e.g. "var.UserChoice.ask") which come first</summary>
        public static VariableScope ScopeOf(CaptureFlowContext flow, IDictionary<string, object> values = null)
        {
            var scope = VariableScope.ForFlow(flow);
            scope.Values = values;
            return scope;
        }

        /// <summary>
        /// The value of a text: a single ${...} keeps its type (a number, a date, true or false), other texts are a text
        /// </summary>
        public static object Evaluate(string text, CaptureFlowContext flow, TemplatePurpose purpose = TemplatePurpose.Value, IDictionary<string, object> values = null)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("${", StringComparison.Ordinal) < 0) return text;
            return VariableResolver.Instance.Evaluate(text, ScopeOf(flow, values), purpose);
        }

        /// <summary>The value of a text as <typeparamref name="T"/>, the default when it is missing or doesn't fit</summary>
        public static T Evaluate<T>(string text, CaptureFlowContext flow, T defaultValue = default, IDictionary<string, object> values = null)
        {
            if (text == null) return defaultValue;
            return ConvertValue(Evaluate(text, flow, TemplatePurpose.Value, values), defaultValue);
        }

        /// <summary>A condition ("when"): true or false, an empty condition is true</summary>
        public static bool IsMet(string condition, CaptureFlowContext flow, IDictionary<string, object> values = null)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;
            return ExpressionValues.ToBoolean(VariableResolver.Instance.Evaluate(condition.Trim(), ScopeOf(flow, values), TemplatePurpose.Condition));
        }

        /// <summary>
        /// The parameters of a node with their ${...} evaluated, each for its purpose (a folder can't use what came with the capture).
        /// Parameters the step renders itself stay as they are. A folder, command or argument with an error throws a <see cref="TemplateException"/>.
        /// </summary>
        public static Dictionary<string, object> ResolveParameters(IDictionary<string, object> parameters, CaptureFlowContext flow, StepContract contract = null, IDictionary<string, object> values = null)
        {
            var resolved = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (parameters == null) return resolved;
            var scope = ScopeOf(flow, values);
            foreach (var pair in parameters)
            {
                var declared = contract?.FindParameter(pair.Key);
                if (declared != null && declared.RenderedByStep)
                {
                    resolved[pair.Key] = pair.Value;
                    continue;
                }
                resolved[pair.Key] = Resolve(pair.Value, scope, declared?.EffectivePurpose ?? TemplatePurpose.Value);
            }
            return resolved;
        }

        private static object Resolve(object value, VariableScope scope, TemplatePurpose purpose)
        {
            switch (value)
            {
                case null:
                    return null;
                case string text:
                    if (text.IndexOf("${", StringComparison.Ordinal) < 0) return text;
                    return purpose == TemplatePurpose.Value || purpose == TemplatePurpose.Condition
                        ? VariableResolver.Instance.Evaluate(text, scope, purpose)
                        : VariableResolver.Instance.Render(text, purpose, scope);
                case IDictionary<string, object> dictionary:
                    var copy = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in dictionary) copy[pair.Key] = Resolve(pair.Value, scope, TemplatePurpose.Value);
                    return copy;
                case IEnumerable enumerable:
                    var list = new List<object>();
                    foreach (var item in enumerable) list.Add(Resolve(item, scope, TemplatePurpose.Value));
                    return list;
                default:
                    return value;
            }
        }

        /// <summary>A value as <typeparamref name="T"/>: numbers, booleans, enums (by name) and colors (#RRGGBB or a name)</summary>
        public static T ConvertValue<T>(object value, T defaultValue)
        {
            if (value == null) return defaultValue;
            if (value is T typed) return typed;

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (targetType == typeof(string))
            {
                return (T)(object)ExpressionValues.ToText(value, null, CultureInfo.InvariantCulture);
            }
            if (targetType == typeof(bool))
            {
                return (T)(object)ExpressionValues.ToBoolean(value);
            }
            if (targetType.IsEnum)
            {
                try
                {
                    return value is string name ? (T)Enum.Parse(targetType, name, true) : (T)Enum.ToObject(targetType, value);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
                {
                    return defaultValue;
                }
            }
            if (targetType == typeof(Color) && value is string colorText)
            {
                try
                {
                    return (T)(object)ColorTranslator.FromHtml(colorText);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is FormatException)
                {
                    var named = Color.FromName(colorText);
                    return named.IsKnownColor ? (T)(object)named : defaultValue;
                }
            }
            try
            {
                return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
            {
                return defaultValue;
            }
        }
    }
}
