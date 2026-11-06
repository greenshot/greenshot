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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// Turns a pattern of Greenshot 1.3 (${capturetime:d"yyyy"}, ${title}, ${NUM}, %USERNAME%) into a template, once, when a
    /// setting is first read with its new key. Old names become their new ones, the old options (d, p, s, r) become a format
    /// or functions, other names were environment variables.
    /// </summary>
    public static class LegacyTemplateConverter
    {
        private static readonly Regex VarRegexp = new Regex(@"\${(?<variable>[^:}]+)[:]?(?<parameters>[^}]*)}", RegexOptions.CultureInvariant);
        private static readonly Regex CmdVarRegexp = new Regex(@"%(?<variable>[A-Za-z_][A-Za-z0-9_()]*)%", RegexOptions.CultureInvariant);
        private static readonly Regex SplitRegexp = new Regex(";(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", RegexOptions.CultureInvariant);
        private static readonly Regex RandomRegexp = new Regex("^R+$", RegexOptions.CultureInvariant);

        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["title"] = "payload.title",
            ["capturetime"] = "payload.time",
            ["now"] = "time.now",
            ["NUM"] = "counter.global",
            ["user"] = "user.name",
            ["domain"] = "user.domain",
            ["hostname"] = "machine.name",
            ["MyPictures"] = "user.pictures",
            ["MyMusic"] = "user.music",
            ["MyDocuments"] = "user.documents",
            ["Personal"] = "user.documents",
            ["Desktop"] = "user.desktop",
            ["ApplicationData"] = "user.appdata",
            ["LocalApplicationData"] = "user.localappdata"
        };

        /// <summary>The parts of the capture time: the old name, the .NET format, the old default width</summary>
        private static readonly Dictionary<string, (string Format, int Width)> TimeParts = new Dictionary<string, (string, int)>(StringComparer.Ordinal)
        {
            ["YYYY"] = ("yyyy", 4),
            ["MM"] = ("MM", 2),
            ["DD"] = ("dd", 2),
            ["hh"] = ("HH", 2),
            ["mm"] = ("mm", 2),
            ["ss"] = ("ss", 2)
        };

        /// <summary>Is it a name of the 1.3 patterns (title, capturetime, NUM, YYYY, RRR, ...)?</summary>
        public static bool IsLegacyName(string name) =>
            !string.IsNullOrEmpty(name) && (Names.ContainsKey(name) || TimeParts.ContainsKey(name) || RandomRegexp.IsMatch(name));

        /// <summary>
        /// The arguments of an external command of 1.3 as a template: {0} was the file (string.Format, so {{ and }} were braces),
        /// ${...} and %NAME% were filled in
        /// </summary>
        public static string ConvertArguments(string arguments)
        {
            if (string.IsNullOrEmpty(arguments)) return arguments;
            return ReplaceFileArgument(Convert(arguments, true));
        }

        /// <summary>{0} becomes ${output.file}, {{ and }} become braces; the ${...} stay as they are</summary>
        public static string ReplaceFileArgument(string template)
        {
            if (string.IsNullOrEmpty(template)) return template;
            var sb = new StringBuilder();
            int depth = 0;
            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];
                // Inside a ${...} nothing is string.Format
                if (c == '$' && i + 1 < template.Length && template[i + 1] == '{') depth++;
                if (depth > 0)
                {
                    sb.Append(c);
                    if (c == '}') depth--;
                    continue;
                }
                if (string.CompareOrdinal(template, i, "{0}", 0, 3) == 0)
                {
                    sb.Append("${output.file}");
                    i += 2;
                }
                else if ((c == '{' || c == '}') && i + 1 < template.Length && template[i + 1] == c)
                {
                    sb.Append(c);
                    i++;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>Is the text a pattern of 1.3, which needs converting? A template that is already new stays as it is.</summary>
        public static bool NeedsConversion(string pattern, bool percentVariables)
        {
            if (string.IsNullOrEmpty(pattern)) return false;
            if (percentVariables && CmdVarRegexp.IsMatch(pattern)) return true;
            foreach (Match match in VarRegexp.Matches(pattern))
            {
                string variable = match.Groups["variable"].Value.Trim();
                if (!variable.Contains(".") || match.Groups["parameters"].Value.StartsWith("d", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>
        /// The template for an old pattern.
        /// </summary>
        /// <param name="pattern">The pattern of 1.3</param>
        /// <param name="percentVariables">The setting also took %NAME% for environment variables (folders, commands)</param>
        public static string Convert(string pattern, bool percentVariables)
        {
            if (string.IsNullOrEmpty(pattern)) return pattern;
            var sb = new StringBuilder();
            int last = 0;
            foreach (Match match in VarRegexp.Matches(pattern))
            {
                sb.Append(Literal(pattern.Substring(last, match.Index - last), percentVariables));
                sb.Append(ConvertVariable(match.Groups["variable"].Value, match.Groups["parameters"].Value));
                last = match.Index + match.Length;
            }
            sb.Append(Literal(pattern.Substring(last), percentVariables));
            return sb.ToString();
        }

        private static string Literal(string text, bool percentVariables)
        {
            // A $ right before a ${ would make it $${, which means a literal ${ now
            text = text.Replace("${", "$${");
            if (text.EndsWith("$", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 1) + "${'$'}";
            if (!percentVariables) return text;
            return CmdVarRegexp.Replace(text, m => "${" + EnvironmentName(m.Groups["variable"].Value) + "}");
        }

        private static string EnvironmentName(string name) =>
            Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]*$") ? "env." + name : $"env.{Regex.Replace(name, "[^A-Za-z0-9_]", "_")}";

        private static string ConvertVariable(string variable, string parameters)
        {
            variable = variable.Trim();
            string dateFormat = null;
            int padWidth = 0;
            char padChar = ' ';
            int? start = null;
            int? length = null;
            var replacements = new List<(string Old, string New)>();
            if (parameters.Length > 0)
            {
                foreach (string parameter in SplitRegexp.Split(parameters))
                {
                    if (parameter.Length == 0) continue;
                    string value = parameter.Substring(1);
                    switch (parameter[0])
                    {
                        case 'p':
                            var pad = value.Split(',');
                            if (int.TryParse(pad[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)) padWidth = width;
                            if (pad.Length > 1 && pad[1].Length > 0) padChar = pad[1][0];
                            break;
                        case 'r':
                            var replace = value.Split(',');
                            if (replace.Length == 2) replacements.Add((replace[0], replace[1]));
                            break;
                        case 'd':
                            dateFormat = value.Trim('"');
                            break;
                        case 's':
                            var range = value.Split(',');
                            if (int.TryParse(range[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int s)) start = s;
                            if (range.Length > 1 && int.TryParse(range[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int l)) length = l;
                            break;
                    }
                }
            }

            string expression;
            string format = null;
            if (TimeParts.TryGetValue(variable, out var part))
            {
                expression = "payload.time";
                format = part.Format;
                if (padWidth != 0)
                {
                    // An own padding: the number without its zeros ("%M", a single M alone is the month and day), padded as before
                    format = part.Width == 4 ? part.Format : "%" + part.Format.Substring(0, 1);
                }
            }
            else if (Names.TryGetValue(variable, out string name))
            {
                expression = name;
                if (variable == "capturetime" || variable == "now")
                {
                    format = dateFormat ?? "yyyy-MM-dd HH-mm-ss";
                }
                else if (variable == "NUM" && padWidth == 0)
                {
                    format = "000000";
                }
            }
            else if (RandomRegexp.IsMatch(variable))
            {
                expression = $"random({variable.Length})";
            }
            else
            {
                // 1.3 took every other name from the environment variables
                expression = EnvironmentName(variable);
            }

            bool functions = padWidth != 0 || start.HasValue || length.HasValue || replacements.Count > 0;
            if (!functions)
            {
                return format == null ? "${" + expression + "}" : "${" + expression + ":" + QuoteFormat(format) + "}";
            }
            if (format != null)
            {
                expression = $"format({expression}, {Quote(format)})";
            }
            if (padWidth > 0)
            {
                expression = $"padRight({expression}, {padWidth}, {Quote(padChar.ToString())})";
            }
            else if (padWidth < 0)
            {
                expression = $"padLeft({expression}, {-padWidth}, {Quote(padChar.ToString())})";
            }
            if (start.HasValue || length.HasValue)
            {
                expression = length.HasValue && length.Value != 0
                    ? $"substring({expression}, {start ?? 0}, {length.Value})"
                    : $"substring({expression}, {start ?? 0})";
            }
            foreach (var (oldText, newText) in replacements)
            {
                expression = $"replace({expression}, {Quote(oldText)}, {Quote(newText)})";
            }
            return "${" + expression + "}";
        }

        /// <summary>A text value in an expression: in ' or ", whichever it doesn't contain</summary>
        private static string Quote(string text) => text.Contains("'") ? "\"" + text + "\"" : "'" + text + "'";

        /// <summary>A format needs quotes when it has a }, starts with a quote or has spaces around it</summary>
        private static string QuoteFormat(string format) =>
            format.IndexOf('}') >= 0 || format.StartsWith("'", StringComparison.Ordinal) || format.StartsWith("\"", StringComparison.Ordinal) || format.Trim().Length != format.Length
                ? Quote(format)
                : format;
    }
}
