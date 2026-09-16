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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Newtonsoft.Json.Linq;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// Expression evaluator supporting arithmetic, logical comparisons, string interpolation,
    /// and scoped variable resolution across user environment, machine environment, configuration, and flow context.
    /// </summary>
    public class ExpressionEvaluator : IExpressionEvaluator
    {
        private static readonly Regex TokenRegex = new Regex(@"\$\{([^}]+)\}", RegexOptions.Compiled);
        private static readonly Lazy<ICoreConfiguration> CoreConfigLazy = new Lazy<ICoreConfiguration>(() =>
        {
            try
            {
                return IniConfigRegistry.GetSection<ICoreConfiguration>();
            }
            catch
            {
                return null;
            }
        });

        private static ExpressionEvaluator _instance;
        public static ExpressionEvaluator Instance => _instance ??= new ExpressionEvaluator();

        public object Evaluate(string expressionOrTemplate, CaptureFlowContext context, IDictionary<string, object> extraVariables = null)
        {
            if (string.IsNullOrWhiteSpace(expressionOrTemplate))
            {
                return expressionOrTemplate;
            }

            string trimmed = expressionOrTemplate.Trim();

            // If the whole string is a single expression ${...}, evaluate and return the raw object
            if (trimmed.StartsWith("${") && trimmed.EndsWith("}") && trimmed.IndexOf("${", 2, StringComparison.Ordinal) == -1)
            {
                string inner = trimmed.Substring(2, trimmed.Length - 3).Trim();
                return EvaluateExpression(inner, context, extraVariables);
            }

            // If string contains multiple ${...} tokens or embedded text, interpolate into a string
            if (TokenRegex.IsMatch(expressionOrTemplate))
            {
                return TokenRegex.Replace(expressionOrTemplate, match =>
                {
                    string expr = match.Groups[1].Value.Trim();
                    object val = EvaluateExpression(expr, context, extraVariables);
                    return val?.ToString() ?? string.Empty;
                });
            }

            return expressionOrTemplate;
        }

        public T Evaluate<T>(string expressionOrTemplate, CaptureFlowContext context, T defaultValue = default, IDictionary<string, object> extraVariables = null)
        {
            if (expressionOrTemplate == null) return defaultValue;

            object raw = Evaluate(expressionOrTemplate, context, extraVariables);
            if (raw == null) return defaultValue;

            return ConvertValue<T>(raw, defaultValue);
        }

        public Dictionary<string, object> ResolveParameters(IDictionary<string, object> parameters, CaptureFlowContext context, IDictionary<string, object> extraVariables = null)
        {
            if (parameters == null) return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var resolved = new Dictionary<string, object>(parameters.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in parameters)
            {
                resolved[kvp.Key] = ResolveObject(kvp.Value, context, extraVariables);
            }
            return resolved;
        }

        private object ResolveObject(object value, CaptureFlowContext context, IDictionary<string, object> extraVariables)
        {
            if (value == null) return null;

            if (value is string str)
            {
                if (str.Contains("${"))
                {
                    return Evaluate(str, context, extraVariables);
                }
                return str;
            }

            if (value is JValue jValue)
            {
                if (jValue.Type == JTokenType.String)
                {
                    string s = jValue.Value<string>();
                    if (s != null && s.Contains("${"))
                    {
                        return Evaluate(s, context, extraVariables);
                    }
                }
                return jValue.Value;
            }

            if (value is IDictionary<string, object> dict)
            {
                return ResolveParameters(dict, context, extraVariables);
            }

            if (value is JObject jObj)
            {
                var dictObj = jObj.ToObject<Dictionary<string, object>>();
                return ResolveParameters(dictObj, context, extraVariables);
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                var list = new List<object>();
                foreach (var item in enumerable)
                {
                    list.Add(ResolveObject(item, context, extraVariables));
                }
                return list;
            }

            return value;
        }

        /// <summary>
        /// Evaluates an expression inside ${...}
        /// </summary>
        public object EvaluateExpression(string expression, CaptureFlowContext context, IDictionary<string, object> extraVariables = null)
        {
            if (string.IsNullOrWhiteSpace(expression)) return null;

            expression = expression.Trim();

            // Date/time with format specifier: e.g. "now:yyyy-MM-dd_HH-mm-ss" or "utcnow:yyyyMMdd"
            if (expression.StartsWith("now:", StringComparison.OrdinalIgnoreCase))
            {
                string format = expression.Substring(4);
                return DateTime.Now.ToString(format, CultureInfo.InvariantCulture);
            }
            if (expression.StartsWith("utcnow:", StringComparison.OrdinalIgnoreCase))
            {
                string format = expression.Substring(7);
                return DateTime.UtcNow.ToString(format, CultureInfo.InvariantCulture);
            }

            // Check ternary operator: condition ? trueExpr : falseExpr
            int qIndex = FindTernaryQuestionMark(expression);
            if (qIndex > 0)
            {
                string condPart = expression.Substring(0, qIndex).Trim();
                int colonIndex = FindTernaryColon(expression, qIndex + 1);
                if (colonIndex > qIndex)
                {
                    string truePart = expression.Substring(qIndex + 1, colonIndex - qIndex - 1).Trim();
                    string falsePart = expression.Substring(colonIndex + 1).Trim();

                    object condVal = EvaluateExpression(condPart, context, extraVariables);
                    bool isTrue = ConvertToBoolean(condVal);
                    return isTrue ? EvaluateExpression(truePart, context, extraVariables) : EvaluateExpression(falsePart, context, extraVariables);
                }
            }

            // Parse and evaluate arithmetic / comparison expressions or single variable identifier
            return EvaluateMathOrComparison(expression, context, extraVariables);
        }

        private static int FindTernaryQuestionMark(string expr)
        {
            int parens = 0;
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '(') parens++;
                else if (c == ')') parens--;
                else if (c == '?' && parens == 0)
                {
                    // Ensure it's not "??" (null coalescing)
                    if (i + 1 < expr.Length && expr[i + 1] == '?')
                    {
                        i++;
                        continue;
                    }
                    return i;
                }
            }
            return -1;
        }

        private static int FindTernaryColon(string expr, int startIndex)
        {
            int parens = 0;
            int nestedTernary = 0;
            for (int i = startIndex; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '(') parens++;
                else if (c == ')') parens--;
                else if (c == '?' && parens == 0) nestedTernary++;
                else if (c == ':' && parens == 0)
                {
                    if (nestedTernary > 0)
                    {
                        nestedTernary--;
                    }
                    else
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Simple recursive descent / shunting-yard evaluator for arithmetic and comparisons.
        /// </summary>
        private object EvaluateMathOrComparison(string expression, CaptureFlowContext context, IDictionary<string, object> extraVariables)
        {
            var tokens = Tokenize(expression);
            if (tokens.Count == 0) return null;

            if (tokens.Count == 1)
            {
                return ResolveSingleToken(tokens[0], context, extraVariables);
            }

            // Logical OR (||)
            int orIndex = FindLowestPrecedenceOperator(tokens, new[] { "||" });
            if (orIndex >= 0)
            {
                var left = EvaluateTokens(tokens.Take(orIndex).ToList(), context, extraVariables);
                var right = EvaluateTokens(tokens.Skip(orIndex + 1).ToList(), context, extraVariables);
                return ConvertToBoolean(left) || ConvertToBoolean(right);
            }

            // Logical AND (&&)
            int andIndex = FindLowestPrecedenceOperator(tokens, new[] { "&&" });
            if (andIndex >= 0)
            {
                var left = EvaluateTokens(tokens.Take(andIndex).ToList(), context, extraVariables);
                var right = EvaluateTokens(tokens.Skip(andIndex + 1).ToList(), context, extraVariables);
                return ConvertToBoolean(left) && ConvertToBoolean(right);
            }

            // Comparisons (==, !=, <=, >=, <, >)
            int compIndex = FindLowestPrecedenceOperator(tokens, new[] { "==", "!=", "<=", ">=", "<", ">" });
            if (compIndex >= 0)
            {
                string op = tokens[compIndex];
                var left = EvaluateTokens(tokens.Take(compIndex).ToList(), context, extraVariables);
                var right = EvaluateTokens(tokens.Skip(compIndex + 1).ToList(), context, extraVariables);
                return EvaluateComparison(left, op, right);
            }

            // Add / Subtract (+, -)
            int addSubIndex = FindLowestPrecedenceOperator(tokens, new[] { "+", "-" });
            if (addSubIndex >= 0)
            {
                string op = tokens[addSubIndex];
                var left = EvaluateTokens(tokens.Take(addSubIndex).ToList(), context, extraVariables);
                var right = EvaluateTokens(tokens.Skip(addSubIndex + 1).ToList(), context, extraVariables);

                if (op == "+" && (left is string || right is string))
                {
                    return $"{left}{right}";
                }

                double lNum = ConvertToDouble(left);
                double rNum = ConvertToDouble(right);
                return op == "+" ? (lNum + rNum) : (lNum - rNum);
            }

            // Multiply / Divide / Modulo (*, /, %)
            int mulDivIndex = FindLowestPrecedenceOperator(tokens, new[] { "*", "/", "%" });
            if (mulDivIndex >= 0)
            {
                string op = tokens[mulDivIndex];
                var left = EvaluateTokens(tokens.Take(mulDivIndex).ToList(), context, extraVariables);
                var right = EvaluateTokens(tokens.Skip(mulDivIndex + 1).ToList(), context, extraVariables);

                double lNum = ConvertToDouble(left);
                double rNum = ConvertToDouble(right);
                if (op == "*") return lNum * rNum;
                if (op == "/") return rNum != 0 ? lNum / rNum : 0;
                if (op == "%") return rNum != 0 ? lNum % rNum : 0;
            }

            // Parenthesized expression unwrapping
            if (tokens[0] == "(" && tokens[tokens.Count - 1] == ")")
            {
                return EvaluateTokens(tokens.Skip(1).Take(tokens.Count - 2).ToList(), context, extraVariables);
            }

            return ResolveSingleToken(string.Join(" ", tokens), context, extraVariables);
        }

        private object EvaluateTokens(List<string> tokens, CaptureFlowContext context, IDictionary<string, object> extraVariables)
        {
            if (tokens.Count == 0) return null;
            if (tokens.Count == 1) return ResolveSingleToken(tokens[0], context, extraVariables);
            return EvaluateMathOrComparison(string.Join(" ", tokens), context, extraVariables);
        }

        private static int FindLowestPrecedenceOperator(List<string> tokens, string[] operators)
        {
            int parens = 0;
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                string t = tokens[i];
                if (t == ")") parens++;
                else if (t == "(") parens--;
                else if (parens == 0 && operators.Contains(t))
                {
                    // Avoid unary minus
                    if (t == "-" && (i == 0 || IsOperator(tokens[i - 1])))
                    {
                        continue;
                    }
                    return i;
                }
            }
            return -1;
        }

        private static bool IsOperator(string token)
        {
            return token == "+" || token == "-" || token == "*" || token == "/" || token == "%" ||
                   token == "==" || token == "!=" || token == "<" || token == ">" || token == "<=" || token == ">=" ||
                   token == "&&" || token == "||" || token == "(";
        }

        private static List<string> Tokenize(string expr)
        {
            var list = new List<string>();
            var sb = new StringBuilder();

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];

                if (char.IsWhiteSpace(c))
                {
                    Flush(sb, list);
                    continue;
                }

                if (c == '(' || c == ')')
                {
                    Flush(sb, list);
                    list.Add(c.ToString());
                    continue;
                }

                if (c == '+' || c == '-' || c == '*' || c == '/' || c == '%')
                {
                    Flush(sb, list);
                    list.Add(c.ToString());
                    continue;
                }

                if (c == '=' || c == '!' || c == '<' || c == '>')
                {
                    Flush(sb, list);
                    if (i + 1 < expr.Length && expr[i + 1] == '=')
                    {
                        list.Add($"{c}=");
                        i++;
                    }
                    else
                    {
                        list.Add(c.ToString());
                    }
                    continue;
                }

                if (c == '&' && i + 1 < expr.Length && expr[i + 1] == '&')
                {
                    Flush(sb, list);
                    list.Add("&&");
                    i++;
                    continue;
                }

                if (c == '|' && i + 1 < expr.Length && expr[i + 1] == '|')
                {
                    Flush(sb, list);
                    list.Add("||");
                    i++;
                    continue;
                }

                sb.Append(c);
            }

            Flush(sb, list);
            return list;
        }

        private static void Flush(StringBuilder sb, List<string> list)
        {
            if (sb.Length > 0)
            {
                list.Add(sb.ToString());
                sb.Clear();
            }
        }

        /// <summary>
        /// Resolves a single identifier or constant literal.
        /// </summary>
        public object ResolveSingleToken(string token, CaptureFlowContext context, IDictionary<string, object> extraVariables)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            token = token.Trim();

            // String literal: '...' or "..."
            if ((token.StartsWith("\"") && token.EndsWith("\"")) || (token.StartsWith("'") && token.EndsWith("'")))
            {
                return token.Substring(1, token.Length - 2);
            }

            // Numeric literal
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
            {
                return num;
            }

            // Boolean literal
            if (bool.TryParse(token, out bool b))
            {
                return b;
            }

            // Null literal
            if (string.Equals(token, "null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Extra variables / local scope
            if (extraVariables != null)
            {
                if (extraVariables.TryGetValue(token, out var extraVal)) return extraVal;
            }

            // 1. User Scoped Environment Variables (EnvironmentVariableTarget.User)
            // e.g. user.username, user.temp, user.appdata
            if (token.StartsWith("user.", StringComparison.OrdinalIgnoreCase))
            {
                string key = token.Substring(5);
                return ResolveUserEnvironment(key);
            }

            // 2. Machine Scoped Environment Variables (EnvironmentVariableTarget.Machine)
            // e.g. machine.computername, machine.os, machine.programfiles
            if (token.StartsWith("machine.", StringComparison.OrdinalIgnoreCase))
            {
                string key = token.Substring(8);
                return ResolveMachineEnvironment(key);
            }

            // 3. Configuration Scoped Properties
            // e.g. config.language, config.capturemousepointer
            if (token.StartsWith("config.", StringComparison.OrdinalIgnoreCase))
            {
                string key = token.Substring(7);
                return ResolveConfigProperty(key);
            }

            // 4. Flow Context Properties & Custom Variables
            // e.g. context.watermark_text, context.WindowTitle
            if (token.StartsWith("context.", StringComparison.OrdinalIgnoreCase))
            {
                string key = token.Substring(8);
                return ResolveContextProperty(key, context);
            }

            // 5. Visual Payload & Surface properties
            // e.g. payload.width, payload.height, payload.extractedtext, payload.title, payload.filename
            if (token.StartsWith("payload.", StringComparison.OrdinalIgnoreCase))
            {
                string key = token.Substring(8);
                return ResolvePayloadProperty(key, context);
            }

            // Date / Time shortcuts
            if (string.Equals(token, "now", StringComparison.OrdinalIgnoreCase)) return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            if (string.Equals(token, "utcnow", StringComparison.OrdinalIgnoreCase)) return DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);

            // Direct check in context properties bag
            if (context?.Properties != null && context.Properties.TryGetValue(token, out var ctxPropVal))
            {
                return ctxPropVal;
            }

            return null;
        }

        private static object ResolveUserEnvironment(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            // Known user identities & special folders
            switch (key.ToLowerInvariant())
            {
                case "name":
                case "username":
                    return Environment.UserName;
                case "domain":
                case "userdomain":
                    return Environment.UserDomainName;
                case "profile":
                case "userprofile":
                    return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                case "appdata":
                case "roamingappdata":
                    return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                case "localappdata":
                    return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                case "desktop":
                    return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                case "documents":
                case "mydocuments":
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                case "pictures":
                case "mypictures":
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                case "temp":
                    return Path.GetTempPath();
            }

            // Check Windows User Environment Variables (EnvironmentVariableTarget.User)
            try
            {
                string userVar = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User);
                if (userVar != null) return userVar;
            }
            catch { }

            // Fallback to current process user environment
            return Environment.GetEnvironmentVariable(key);
        }

        private static object ResolveMachineEnvironment(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            switch (key.ToLowerInvariant())
            {
                case "name":
                case "machinename":
                case "computername":
                    return Environment.MachineName;
                case "os":
                case "osdescription":
                    return Environment.OSVersion.Platform.ToString();
                case "osversion":
                    return Environment.OSVersion.VersionString;
                case "processors":
                case "processorcount":
                    return Environment.ProcessorCount;
                case "is64bit":
                case "is64bitoperatingsystem":
                    return Environment.Is64BitOperatingSystem;
                case "systemdir":
                case "systemdirectory":
                    return Environment.SystemDirectory;
                case "programfiles":
                    return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                case "commonappdata":
                    return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }

            // Check Windows Machine Environment Variables (EnvironmentVariableTarget.Machine)
            try
            {
                string machineVar = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Machine);
                if (machineVar != null) return machineVar;
            }
            catch { }

            return Environment.GetEnvironmentVariable(key);
        }

        private static object ResolveConfigProperty(string key)
        {
            var config = CoreConfigLazy.Value;
            if (config == null || string.IsNullOrWhiteSpace(key)) return null;

            // Reflect on ICoreConfiguration
            var prop = typeof(ICoreConfiguration).GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null)
            {
                return prop.GetValue(config);
            }

            return null;
        }

        private static object ResolveContextProperty(string key, CaptureFlowContext context)
        {
            if (context == null || string.IsNullOrWhiteSpace(key)) return null;

            if (string.Equals(key, "ExecutionId", StringComparison.OrdinalIgnoreCase)) return context.ExecutionId.ToString();
            if (string.Equals(key, "State", StringComparison.OrdinalIgnoreCase)) return context.State.ToString();
            if (string.Equals(key, "RecipeName", StringComparison.OrdinalIgnoreCase)) return context.Recipe?.Name;
            if (string.Equals(key, "RecipeId", StringComparison.OrdinalIgnoreCase)) return context.Recipe?.Id;
            if (string.Equals(key, "AbortReason", StringComparison.OrdinalIgnoreCase)) return context.AbortReason;

            if (context.Properties != null && context.Properties.TryGetValue(key, out var val))
            {
                return val;
            }

            return null;
        }

        private static object ResolvePayloadProperty(string key, CaptureFlowContext context)
        {
            var payload = context?.Payload;
            if (payload == null) return 0;

            var surface = payload.Surface;
            var image = surface?.Image ?? payload.RawCapture?.Image;

            switch (key.ToLowerInvariant())
            {
                case "width":
                case "w":
                    return image?.Width ?? 0;
                case "height":
                case "h":
                    return image?.Height ?? 0;
                case "text":
                case "extractedtext":
                    return payload.ExtractedText ?? string.Empty;
                case "title":
                    return payload.RawCapture?.CaptureDetails?.Title ?? string.Empty;
                case "filename":
                    return payload.RawCapture?.CaptureDetails?.Filename ?? string.Empty;
            }

            if (payload.Metadata != null && payload.Metadata.TryGetValue(key, out var metaVal))
            {
                return metaVal;
            }

            return null;
        }

        private static bool EvaluateComparison(object left, string op, object right)
        {
            if (left == null && right == null) return op == "==" || op == "<=" || op == ">=";
            if (left == null || right == null) return op == "!=";

            if (left is bool lBool && right is bool rBool)
            {
                return op == "==" ? (lBool == rBool) : (lBool != rBool);
            }

            // Numeric comparison
            if (IsNumeric(left) && IsNumeric(right))
            {
                double l = ConvertToDouble(left);
                double r = ConvertToDouble(right);
                switch (op)
                {
                    case "==": return Math.Abs(l - r) < 0.0000001;
                    case "!=": return Math.Abs(l - r) >= 0.0000001;
                    case "<": return l < r;
                    case "<=": return l <= r;
                    case ">": return l > r;
                    case ">=": return l >= r;
                }
            }

            // String comparison
            string lStr = left.ToString();
            string rStr = right.ToString();
            int cmp = string.Compare(lStr, rStr, StringComparison.OrdinalIgnoreCase);
            switch (op)
            {
                case "==": return cmp == 0;
                case "!=": return cmp != 0;
                case "<": return cmp < 0;
                case "<=": return cmp <= 0;
                case ">": return cmp > 0;
                case ">=": return cmp >= 0;
            }

            return false;
        }

        private static bool IsNumeric(object val)
        {
            return val is byte || val is sbyte || val is short || val is ushort ||
                   val is int || val is uint || val is long || val is ulong ||
                   val is float || val is double || val is decimal;
        }

        private static double ConvertToDouble(object val)
        {
            if (val == null) return 0;
            if (val is double d) return d;
            if (val is float f) return f;
            if (val is int i) return i;
            if (val is long l) return l;
            if (double.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double res)) return res;
            return 0;
        }

        private static bool ConvertToBoolean(object val)
        {
            if (val == null) return false;
            if (val is bool b) return b;
            if (val is double d) return d != 0;
            if (val is int i) return i != 0;
            if (bool.TryParse(val.ToString(), out bool parsed)) return parsed;
            return !string.IsNullOrWhiteSpace(val.ToString());
        }

        private static T ConvertValue<T>(object value, T defaultValue)
        {
            if (value == null) return defaultValue;
            if (value is T typed) return typed;

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (targetType == typeof(string))
            {
                return (T)(object)value.ToString();
            }

            if (targetType.IsEnum)
            {
                if (value is string str)
                {
                    try
                    {
                        return (T)Enum.Parse(targetType, str, true);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
                return (T)Enum.ToObject(targetType, value);
            }

            if (targetType == typeof(Color))
            {
                if (value is Color c) return (T)(object)c;
                if (value is string cs)
                {
                    try
                    {
                        return (T)(object)ColorTranslator.FromHtml(cs);
                    }
                    catch
                    {
                        var named = Color.FromName(cs);
                        return named.IsKnownColor ? (T)(object)named : defaultValue;
                    }
                }
            }

            try
            {
                return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
