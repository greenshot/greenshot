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
using System.Linq;
using System.Text;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// The table of all values for docs/vNext/variables.md, made from the descriptors of the resolver, so the docs say what the
    /// code has (a test compares them).
    /// </summary>
    public static class VariableDocumentation
    {
        /// <summary>The line before the generated table in the docs</summary>
        public const string StartMarker = "<!-- values:start (generated from the code by VariableDocumentation.ValueTable, a test compares it) -->";

        /// <summary>The line after the generated table in the docs</summary>
        public const string EndMarker = "<!-- values:end -->";

        public static string ValueTable(IVariableResolver resolver)
        {
            var sb = new StringBuilder();
            sb.Append("| Value | Type | There | Trusted | Description |\n");
            sb.Append("|---|---|---|---|---|\n");
            foreach (var variable in (resolver ?? VariableResolver.Instance).DescribeAll())
            {
                sb.Append("| `").Append(DisplayName(variable.Name)).Append("` | ")
                    .Append(TypeName(variable.Type)).Append(" | ")
                    .Append(Cell(variable.Available ?? "always")).Append(" | ")
                    .Append(variable.Trust == VariableTrust.Trusted ? "yes" : "no").Append(" | ")
                    .Append(Cell(variable.Description)).Append(" |\n");
            }
            return sb.ToString();
        }

        /// <summary>The name to write, a placeholder for a name below a namespace: env.&lt;name&gt;, config.&lt;section&gt;.&lt;key&gt;</summary>
        public static string DisplayName(string name)
        {
            if (!name.EndsWith(".*", StringComparison.Ordinal))
            {
                return name;
            }
            string prefix = name.Substring(0, name.Length - 1);
            return prefix switch
            {
                "config." => "config.<section>.<key>",
                "option." => "option.<key>",
                _ => prefix + "<name>"
            };
        }

        private static string TypeName(Type type)
        {
            if (type == typeof(string)) return "text";
            if (type == typeof(bool)) return "true/false";
            if (type == typeof(DateTime)) return "date and time";
            if (new[] { typeof(int), typeof(long), typeof(double), typeof(decimal) }.Contains(type)) return "number";
            return "any";
        }

        private static string Cell(string text) => (text ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
