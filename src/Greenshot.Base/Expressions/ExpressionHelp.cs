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

namespace Greenshot.Base.Expressions
{
    /// <summary>What a help entry is</summary>
    public enum ExpressionHelpGroup
    {
        /// <summary>How to write a value, a format, a fallback, ...</summary>
        Syntax,
        /// <summary>An option of the recipe: ${option.key}</summary>
        RecipeOption,
        /// <summary>A variable of the recipe: a trigger value, a step output, SetVariable: ${var.name}</summary>
        RecipeVariable,
        /// <summary>A value of the engine: ${payload.title}, ${time.now}, ...</summary>
        Value,
        /// <summary>A function: left(text, count), ...</summary>
        Function
    }

    /// <summary>One line of the expression help</summary>
    public sealed class ExpressionHelpEntry
    {
        public ExpressionHelpEntry(ExpressionHelpGroup group, string expression, string description)
        {
            Group = group;
            Expression = expression;
            Description = description ?? string.Empty;
        }

        public ExpressionHelpGroup Group { get; }

        /// <summary>What to write, e.g. "${option.border}" or "left(text, count)"</summary>
        public string Expression { get; }

        public string Description { get; }

        public override string ToString() => Expression;
    }

    /// <summary>
    /// The values and functions a text can use, for the help windows (settings, recipe editor); options and variables of a recipe
    /// come from RecipeExpressionHelp.
    /// </summary>
    public static class ExpressionHelp
    {
        public static IReadOnlyList<ExpressionHelpEntry> For(TemplatePurpose purpose)
        {
            var entries = new List<ExpressionHelpEntry>
            {
                new ExpressionHelpEntry(ExpressionHelpGroup.Syntax, "${name}", "A value, e.g. ${payload.title}"),
                new ExpressionHelpEntry(ExpressionHelpGroup.Syntax, "${name:format}", "A value with a .NET format, e.g. ${payload.time:yyyy-MM-dd}"),
                new ExpressionHelpEntry(ExpressionHelpGroup.Syntax, "${name ?? 'text'}", "A fallback when the value is missing or empty"),
                new ExpressionHelpEntry(ExpressionHelpGroup.Syntax, "${(a > b ? 'x' : 'y')}", "A choice, in parentheses"),
                new ExpressionHelpEntry(ExpressionHelpGroup.Syntax, "$${", "A ${ as it is")
            };
            if (purpose == TemplatePurpose.FileName)
            {
                entries.Add(new ExpressionHelpEntry(ExpressionHelpGroup.Syntax, "\\", "A sub folder, e.g. ${payload.time:yyyy-MM}\\${payload.title}"));
            }
            var resolver = VariableResolver.Instance;
            foreach (var variable in resolver.Describe(purpose)
                         .Where(v => !v.Name.StartsWith("option.", StringComparison.OrdinalIgnoreCase) && !v.Name.StartsWith("var.", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
            {
                string description = string.IsNullOrEmpty(variable.Available) ? variable.Description : $"{variable.Description} ({variable.Available})";
                entries.Add(new ExpressionHelpEntry(ExpressionHelpGroup.Value, "${" + VariableDocumentation.DisplayName(variable.Name) + "}", description));
            }
            foreach (var function in resolver.Functions)
            {
                entries.Add(new ExpressionHelpEntry(ExpressionHelpGroup.Function, function.Signature, function.Description));
            }
            return entries;
        }
    }
}
