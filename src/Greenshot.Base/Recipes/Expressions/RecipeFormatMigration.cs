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
using System.Text;
using System.Text.RegularExpressions;
using Greenshot.Base.Expressions;

namespace Greenshot.Base.Recipes.Expressions
{
    /// <summary>
    /// Recipes of format 2.x (the earlier 1.4 betas) to format 3.0 when they are read: the ${...} use the names of the one
    /// expression engine (see docs/vNext/variables.md). The file stays as it is (and so its approval), the recipe editor writes 3.0.
    /// </summary>
    public static class RecipeFormatMigration
    {
        private static readonly Regex Identifier = new Regex(@"[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)*", RegexOptions.CultureInvariant);

        /// <summary>Parameters which held a file name pattern of 1.3 (${capturetime:d"..."}, ${title})</summary>
        private static readonly System.Text.RegularExpressions.Regex PercentVariable = new System.Text.RegularExpressions.Regex(@"%([A-Za-z_][A-Za-z0-9_]*)%");
        private static readonly HashSet<string> FileNameParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FilenamePattern", "FooterPattern" };

        private static readonly HashSet<string> UserNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "name", "domain", "profile", "desktop", "documents", "pictures", "videos", "music", "appdata", "localappdata", "temp" };

        private static readonly Dictionary<string, string> MachineNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "name", ["machinename"] = "name", ["computername"] = "name",
            ["os"] = "os", ["osdescription"] = "osversion", ["osversion"] = "osversion",
            ["processors"] = "processors", ["processorcount"] = "processors",
            ["is64bit"] = "is64bit", ["is64bitoperatingsystem"] = "is64bit",
            ["systemdir"] = "systemdir", ["systemdirectory"] = "systemdir",
            ["programfiles"] = "programfiles", ["commonappdata"] = "commonappdata"
        };

        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "true", "false", "null" };

        /// <summary>Does the recipe have the format of the earlier betas (2.x)?</summary>
        public static bool NeedsMigration(FlowDefinition definition) =>
            definition?.Version != null && definition.Version.Split('.')[0].Trim() == "2";

        /// <summary>Converts the recipe to format 3.0 when it is 2.x, true when it was converted</summary>
        public static bool Migrate(FlowDefinition definition)
        {
            if (!NeedsMigration(definition)) return false;
            foreach (var node in definition.Nodes ?? Enumerable.Empty<RecipeNodeConfig>())
            {
                if (node == null) continue;
                node.When = MigrateText(node.When);
                if (node.Parameters != null)
                {
                    foreach (var key in node.Parameters.Keys.ToList())
                    {
                        node.Parameters[key] = MigrateParameter(node.StepType, key, node.Parameters[key]);
                    }
                }
            }
            if (definition is CaptureRecipe recipe)
            {
                foreach (var trigger in recipe.Triggers ?? Enumerable.Empty<Triggers.TriggerConfig>())
                {
                    if (trigger == null) continue;
                    trigger.When = MigrateText(trigger.When);
                    if (trigger.Parameters != null)
                    {
                        foreach (var key in trigger.Parameters.Keys.ToList())
                        {
                            trigger.Parameters[key] = MigrateValue(trigger.Parameters[key]);
                        }
                    }
                }
            }
            foreach (var option in definition.Options ?? Enumerable.Empty<RecipeOption>())
            {
                if (option?.DefaultValue is string text) option.DefaultValue = MigrateText(text);
            }
            definition.Version = FlowDefinition.CurrentVersion;
            return true;
        }

        private static object MigrateParameter(string stepType, string key, object value)
        {
            if (value is string text)
            {
                if (string.Equals(stepType, "RecordVideo", StringComparison.OrdinalIgnoreCase))
                {
                    // 2.x expanded %VAR% in the folder and file name and put the start time in {yyyyMMdd_HHmmss}
                    string migrated = PercentVariable.Replace(MigrateText(text), m => "${env." + m.Groups[1].Value + "}");
                    return migrated.Replace("{yyyyMMdd_HHmmss}", "${time.now:yyyyMMdd_HHmmss}");
                }
                if (FileNameParameters.Contains(key)) return MigrateFileNameTemplate(text);
                if (string.Equals(key, "Arguments", StringComparison.OrdinalIgnoreCase) && string.Equals(stepType, "ExternalCommand", StringComparison.OrdinalIgnoreCase))
                {
                    return LegacyTemplateConverter.ReplaceFileArgument(MigrateText(text));
                }
            }
            return MigrateValue(value);
        }

        private static object MigrateValue(object value)
        {
            switch (value)
            {
                case string text:
                    return MigrateText(text);
                case IDictionary<string, object> dictionary:
                    foreach (var key in dictionary.Keys.ToList()) dictionary[key] = MigrateValue(dictionary[key]);
                    return dictionary;
                case IList<object> list:
                    for (int i = 0; i < list.Count; i++) list[i] = MigrateValue(list[i]);
                    return list;
                default:
                    return value;
            }
        }

        /// <summary>A file name or footer of 2.x: the names of 1.3 ($ {title}, ${capturetime:d"..."}) or recipe expressions</summary>
        public static string MigrateFileNameTemplate(string text) =>
            ReplaceExpressions(text, inner =>
            {
                int colon = inner.IndexOf(':');
                string name = (colon < 0 ? inner : inner.Substring(0, colon)).Trim();
                return LegacyTemplateConverter.IsLegacyName(name)
                    ? LegacyTemplateConverter.Convert("${" + inner + "}", false)
                    : "${" + MigrateExpression(inner) + "}";
            });

        /// <summary>A text of 2.x with its ${...} in the names of 3.0</summary>
        public static string MigrateText(string text) => ReplaceExpressions(text, inner => "${" + MigrateExpression(inner) + "}");

        private static string ReplaceExpressions(string text, Func<string, string> replace)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("${", StringComparison.Ordinal) < 0) return text;
            var sb = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                int start = text.IndexOf("${", i, StringComparison.Ordinal);
                if (start < 0)
                {
                    sb.Append(text, i, text.Length - i);
                    break;
                }
                int end = FindClosingBrace(text, start + 2);
                if (end < 0)
                {
                    sb.Append(text, i, text.Length - i);
                    break;
                }
                sb.Append(text, i, start - i);
                sb.Append(replace(text.Substring(start + 2, end - start - 2)));
                i = end + 1;
            }
            return sb.ToString();
        }

        private static int FindClosingBrace(string text, int from)
        {
            char quote = '\0';
            for (int i = from; i < text.Length; i++)
            {
                char c = text[i];
                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                    continue;
                }
                if (c == '\'' || c == '"') quote = c;
                else if (c == '}') return i;
            }
            return -1;
        }

        /// <summary>
        /// The inside of a ${...} of 2.x in 3.0: the names of the values, "now:format" as time.now with a format, and a choice
        /// (a ? b : c) in parentheses, because a ':' starts the format now.
        /// </summary>
        public static string MigrateExpression(string expression)
        {
            string trimmed = expression.Trim();
            if (trimmed.StartsWith("now:", StringComparison.OrdinalIgnoreCase)) return "time.now:" + trimmed.Substring(4);
            if (trimmed.StartsWith("utcnow:", StringComparison.OrdinalIgnoreCase)) return "time.utc:" + trimmed.Substring(7);

            var sb = new StringBuilder();
            char quote = '\0';
            int depth = 0;
            bool topLevelColon = false;
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (quote != '\0')
                {
                    sb.Append(c);
                    if (c == quote) quote = '\0';
                    continue;
                }
                if (c == '\'' || c == '"')
                {
                    quote = c;
                    sb.Append(c);
                    continue;
                }
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == ':' && depth == 0) topLevelColon = true;
                bool startsName = (char.IsLetter(c) || c == '_') && (i == 0 || !(char.IsLetterOrDigit(trimmed[i - 1]) || trimmed[i - 1] == '_' || trimmed[i - 1] == '.'));
                if (startsName)
                {
                    var match = Identifier.Match(trimmed, i);
                    if (match.Success && match.Index == i)
                    {
                        int after = i + match.Length;
                        while (after < trimmed.Length && trimmed[after] == ' ') after++;
                        bool isCall = after < trimmed.Length && trimmed[after] == '(';
                        sb.Append(isCall ? match.Value : Rename(match.Value));
                        i += match.Length - 1;
                        continue;
                    }
                }
                sb.Append(c);
            }
            string result = sb.ToString();
            return topLevelColon ? "(" + result + ")" : result;
        }

        /// <summary>A name of the 2.x evaluator in 3.0</summary>
        internal static string Rename(string name)
        {
            if (Keywords.Contains(name)) return name.ToLowerInvariant();
            int dot = name.IndexOf('.');
            string ns = dot < 0 ? null : name.Substring(0, dot).ToLowerInvariant();
            string rest = dot < 0 ? name : name.Substring(dot + 1);
            switch (ns)
            {
                case null:
                    if (string.Equals(name, "now", StringComparison.OrdinalIgnoreCase)) return "format(time.now, 'yyyy-MM-dd_HH-mm-ss')";
                    if (string.Equals(name, "utcnow", StringComparison.OrdinalIgnoreCase)) return "format(time.utc, 'yyyy-MM-dd_HH-mm-ss')";
                    if (string.Equals(name, "ocr_text", StringComparison.OrdinalIgnoreCase)) return "payload.ocr.text";
                    // The folder of the opened file of 1.3 file name patterns, also used for SaveDirectory (2.x lost it)
                    if (string.Equals(name, "dirname", StringComparison.Ordinal)) return "payload.folder";
                    // The size of the surface, which annotations had as width, w, height and h
                    if (string.Equals(name, "width", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "w", StringComparison.OrdinalIgnoreCase)) return "payload.width";
                    if (string.Equals(name, "height", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "h", StringComparison.OrdinalIgnoreCase)) return "payload.height";
                    // A bare name was a value of the running recipe
                    return "var." + name;
                case "context":
                    return LegacyNames.Hint(name) ?? "var." + rest;
                case "user":
                    if (UserNames.Contains(rest)) return "user." + rest.ToLowerInvariant();
                    string userName = LegacyNames.Hint(name);
                    return userName ?? "user.env." + rest;
                case "machine":
                    return MachineNames.TryGetValue(rest, out string machine) ? "machine." + machine : "machine.env." + rest;
                case "config":
                    // config.key was a setting of the core section, config.<section>.<key> now
                    return rest.IndexOf('.') < 0 ? "config.Core." + rest : name;
                case "payload":
                    return LegacyNames.Hint(name) ?? name;
                case "surface":
                    return "payload." + rest;
                case "option":
                case "var":
                case "time":
                case "env":
                case "recipe":
                case "counter":
                case "output":
                    return name;
                default:
                    // A dotted name of a step output (Destination.Filename, Barcode.Text) was a value of the running recipe too
                    return "var." + name;
            }
        }
    }
}
