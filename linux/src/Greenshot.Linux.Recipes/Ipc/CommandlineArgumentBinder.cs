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

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SkiaSharp;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Triggers;

namespace Greenshot.Linux.Recipes.Ipc
{
    /// <summary>
    /// Result of binding caller supplied arguments to the arguments a recipe's Commandline trigger declares.
    /// </summary>
    public sealed class ArgumentBindingResult
    {
        private ArgumentBindingResult(IDictionary<string, object> variables, string error)
        {
            Variables = variables;
            Error = error;
        }

        /// <summary>Flow context variables (keyed by the declared variable name), or null when binding failed.</summary>
        public IDictionary<string, object> Variables { get; }

        /// <summary>Message for stderr when binding failed.</summary>
        public string Error { get; }

        public bool Success => Error == null;

        internal static ArgumentBindingResult Ok(IDictionary<string, object> variables) => new ArgumentBindingResult(variables, null);

        internal static ArgumentBindingResult Fail(string error) => new ArgumentBindingResult(null, error);
    }

    /// <summary>
    /// Binds the arguments a caller (command line, greenshot: URL, browser extension) passes to a recipe.
    /// <list type="bullet">
    /// <item>Only arguments the recipe's Commandline trigger declares are accepted, so a caller cannot set internal flow
    /// variables (e.g. OverrideDestinations or CaptureDelay).</item>
    /// <item>Each value is stored only under the declared variable name.</item>
    /// <item>The declared <see cref="CommandlineArgument.Type"/> decides validation and conversion: paths are sanitized
    /// (and resolved against the caller's working directory) because they are declared as paths, not because of their name.</item>
    /// </list>
    /// </summary>
    public static class CommandlineArgumentBinder
    {
        /// <summary>
        /// Binds <paramref name="supplied"/> (argument name -> raw value) to <paramref name="declared"/>.
        /// </summary>
        /// <param name="declared">Arguments declared on the Commandline trigger</param>
        /// <param name="supplied">Arguments supplied by the caller (names are matched case-insensitively)</param>
        /// <param name="recipeName">For error messages</param>
        /// <param name="cwd">Caller's working directory, used to resolve relative paths</param>
        /// <param name="source">Connection source, used for the path security rules</param>
        /// <param name="aiClient">The AI tool for an AI tool trigger: Window arguments resolve the window ids it got from list_windows</param>
        public static ArgumentBindingResult Bind(IList<CommandlineArgument> declared, IDictionary<string, string> supplied, string recipeName, string cwd, string source, object aiClient = null)
        {
            declared = (declared ?? new List<CommandlineArgument>()).Where(a => !string.IsNullOrWhiteSpace(a?.Name)).ToList();
            supplied = supplied ?? new Dictionary<string, string>();

            var byName = new Dictionary<string, CommandlineArgument>(StringComparer.OrdinalIgnoreCase);
            foreach (var argument in declared)
            {
                byName[argument.Name] = argument;
            }

            // 1. Only declared arguments are accepted
            var unknown = supplied.Keys.Where(k => !byName.ContainsKey(k)).ToList();
            if (unknown.Count > 0)
            {
                string accepted = declared.Count == 0
                    ? $"Recipe '{recipeName}' does not accept arguments."
                    : $"Accepted arguments: {string.Join(", ", declared.Select(a => "--" + a.Name))}.";
                return ArgumentBindingResult.Fail($"Error: unknown argument '{unknown[0]}' for recipe '{recipeName}'. {accepted}");
            }

            // 2. Required / default / type conversion per declared argument
            var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var argument in declared)
            {
                string raw = null;
                foreach (var pair in supplied)
                {
                    if (string.Equals(pair.Key, argument.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        raw = pair.Value;
                    }
                }
                if (string.IsNullOrWhiteSpace(raw))
                {
                    raw = string.IsNullOrEmpty(argument.DefaultValue) ? null : argument.DefaultValue;
                }

                if (raw == null)
                {
                    if (argument.Required)
                    {
                        string hint = string.IsNullOrWhiteSpace(argument.Description) ? string.Empty : $" ({argument.Description})";
                        return ArgumentBindingResult.Fail($"Error: missing required argument '--{argument.Name}'{hint}.");
                    }
                    continue;
                }

                if (!TryConvert(argument, raw, cwd, source, aiClient, out object value, out string error))
                {
                    return ArgumentBindingResult.Fail(error);
                }
                variables[argument.EffectiveVariable] = value;
            }

            return ArgumentBindingResult.Ok(variables);
        }

        private static bool TryConvert(CommandlineArgument argument, string raw, string cwd, string source, object aiClient, out object value, out string error)
        {
            value = null;
            error = null;
            string name = "--" + argument.Name;

            var allowed = argument.AllowedValues?.Where(v => v != null).ToList() ?? new List<string>();
            if (argument.Type == ContractDataType.Enum || allowed.Count > 0)
            {
                string match = allowed.FirstOrDefault(v => string.Equals(v, raw, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    error = allowed.Count == 0
                        ? $"Error: argument '{name}' is declared as Enum but has no allowed values."
                        : $"Error: invalid value '{raw}' for argument '{name}'. Allowed values: {string.Join(", ", allowed)}.";
                    return false;
                }
                if (argument.Type == ContractDataType.Enum || argument.Type == ContractDataType.String)
                {
                    value = match;
                    return true;
                }
                raw = match;
            }

            switch (argument.Type)
            {
                case ContractDataType.Integer:
                    if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer))
                    {
                        value = integer >= int.MinValue && integer <= int.MaxValue ? (object)(int)integer : integer;
                        return true;
                    }
                    error = $"Error: argument '{name}' expects a whole number, got '{raw}'.";
                    return false;

                case ContractDataType.Decimal:
                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                    {
                        value = number;
                        return true;
                    }
                    error = $"Error: argument '{name}' expects a number, got '{raw}'.";
                    return false;

                case ContractDataType.Boolean:
                    switch (raw.Trim().ToLowerInvariant())
                    {
                        case "true":
                        case "yes":
                        case "on":
                        case "1":
                            value = true;
                            return true;
                        case "false":
                        case "no":
                        case "off":
                        case "0":
                            value = false;
                            return true;
                    }
                    error = $"Error: argument '{name}' expects true or false, got '{raw}'.";
                    return false;

                case ContractDataType.FilePath:
                case ContractDataType.DirectoryPath:
                    if (!PathSanitizer.TrySanitizeAndResolve(raw, cwd, source, out string fullPath, out string pathError))
                    {
                        error = $"[SECURITY] Error: invalid path for argument '{name}': {pathError}";
                        return false;
                    }
                    value = fullPath;
                    return true;

                case ContractDataType.Window:
                    return TryResolveWindow(name, raw, aiClient, out value, out error);

                case ContractDataType.Region:
                    if (TryParseRegion(raw, out var region))
                    {
                        value = region;
                        return true;
                    }
                    error = $"Error: argument '{name}' expects a region 'x,y,width,height' in screen coordinates (width and height above 0), got '{raw}'.";
                    return false;

                default:
                    // String and Object: passed on as text
                    value = raw;
                    return true;
            }
        }

        /// <summary>
        /// Resolves a Window argument of an AI tool: the window id it got from list_windows (set by the AI tools of the app)
        /// </summary>
        public static Func<object, string, (bool Success, object Window, string Error)> WindowResolver { get; set; }

        /// <summary>
        /// A Window argument: only a window id an AI tool got from list_windows, never a handle, title or anything else.
        /// </summary>
        private static bool TryResolveWindow(string name, string raw, object aiClient, out object value, out string error)
        {
            value = null;
            if (aiClient == null || WindowResolver == null)
            {
                error = $"Error: argument '{name}' is a window, window arguments are only available to AI tools.";
                return false;
            }

            var (success, window, resolveError) = WindowResolver(aiClient, raw);
            if (!success)
            {
                error = $"Error: argument '{name}': {resolveError}";
                return false;
            }

            value = window;
            error = null;
            return true;
        }

        /// <summary>
        /// "x,y,width,height" (spaces and ';' allowed) in screen coordinates, width and height above 0.
        /// </summary>
        internal static bool TryParseRegion(string raw, out SKRectI region)
        {
            region = SKRectI.Empty;
            var parts = (raw ?? string.Empty).Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                return false;
            }
            var numbers = new int[4];
            for (int i = 0; i < 4; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i]))
                {
                    return false;
                }
            }
            if (numbers[2] <= 0 || numbers[3] <= 0)
            {
                return false;
            }
            region = SKRectI.Create(numbers[0], numbers[1], numbers[2], numbers[3]);
            return true;
        }
    }
}
