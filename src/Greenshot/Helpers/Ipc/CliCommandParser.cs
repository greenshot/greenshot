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
using System.Collections.Generic;
using System.Linq;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// Result of parsing a forwarded command line.
    /// </summary>
    public sealed class CliParseResult
    {
        private CliParseResult(IpcEnvelope envelope, string error, bool json)
        {
            Envelope = envelope;
            Error = error;
            Json = json;
        }

        /// <summary>The command to dispatch, or null when parsing failed.</summary>
        public IpcEnvelope Envelope { get; }

        /// <summary>The message for stderr when parsing failed.</summary>
        public string Error { get; }

        /// <summary>True when --json was requested (also for errors, so they are reported as JSON).</summary>
        public bool Json { get; }

        public bool Success => Envelope != null;

        internal static CliParseResult Ok(IpcEnvelope envelope) => new CliParseResult(envelope, null, envelope.Json);

        internal static CliParseResult Fail(string detail, bool json, bool showUsageHint = true) =>
            new CliParseResult(null, "Error: " + detail + (showUsageHint ? "\nUse 'greenshot --help' for usage." : string.Empty), json);
    }

    /// <summary>
    /// Parses the raw command line forwarded by greenshot.com / greenshot-proxy.exe (CLI request) into the command to dispatch.
    /// What is accepted depends on the connection source, which the executable announced in its HELLO frame:
    /// <list type="bullet">
    /// <item><c>cli</c>: the full command line syntax of greenshot.com</item>
    /// <item><c>url_scheme</c>: exactly one <c>greenshot:</c> URL</item>
    /// <item><c>open_with</c>: file paths, optionally preceded by <c>--file</c></item>
    /// </list>
    /// The resulting command is still subject to the per-source whitelist of <see cref="IpcSecurityDispatcher"/>.
    /// </summary>
    public static class CliCommandParser
    {
        /// <summary>Process exit code for an invalid command line.</summary>
        public const int UsageExitCode = 2;

        public static CliParseResult Parse(IList<string> argv, string source, string cwd)
        {
            argv = argv ?? new List<string>();

            if (string.Equals(source, IpcSources.UrlScheme, StringComparison.OrdinalIgnoreCase))
            {
                return ParseUrlScheme(argv, cwd);
            }
            if (string.Equals(source, IpcSources.OpenWith, StringComparison.OrdinalIgnoreCase))
            {
                return ParseOpenWith(argv, cwd);
            }
            if (string.Equals(source, IpcSources.Cli, StringComparison.OrdinalIgnoreCase))
            {
                return ParseCommandLine(argv, cwd);
            }
            return CliParseResult.Fail($"command lines are not accepted from source '{source}'.", false, false);
        }

        private static IpcEnvelope CreateEnvelope(string command, string cwd)
        {
            return new IpcEnvelope
            {
                Version = 1,
                Command = command,
                Cwd = cwd
            };
        }

        private static bool IsAny(string arg, params string[] options)
        {
            return options.Any(o => string.Equals(arg, o, StringComparison.OrdinalIgnoreCase));
        }

        private static CliParseResult ParseUrlScheme(IList<string> argv, string cwd)
        {
            if (argv.Count != 1 || !argv[0].StartsWith("greenshot:", StringComparison.OrdinalIgnoreCase))
            {
                return CliParseResult.Fail("expected exactly one greenshot: URL.", false, false);
            }
            var envelope = CreateEnvelope("URL_SCHEME", cwd);
            envelope.RawInput = argv[0];
            return CliParseResult.Ok(envelope);
        }

        private static CliParseResult ParseOpenWith(IList<string> argv, string cwd)
        {
            int first = argv.Count > 0 && IsAny(argv[0], "--file", "-f") ? 1 : 0;
            if (argv.Count <= first)
            {
                return CliParseResult.Fail("no file to open.", false, false);
            }
            var envelope = CreateEnvelope("OPEN_FILE", cwd);
            envelope.Files.AddRange(argv.Skip(first));
            return CliParseResult.Ok(envelope);
        }

        private static CliParseResult ParseCommandLine(IList<string> argv, string cwd)
        {
            bool json = argv.Any(a => string.Equals(a, "--json", StringComparison.OrdinalIgnoreCase));
            if (argv.Count == 0)
            {
                return CliParseResult.Fail("no command given.", json);
            }

            string first = argv[0];

            if (IsAny(first, "--list-recipes", "-l", "list-recipes"))
            {
                return ParseJsonFlagOnly(argv, 1, CreateEnvelope("LIST_RECIPES", cwd));
            }

            if (IsAny(first, "--info", "--describe", "-i", "info", "describe"))
            {
                if (argv.Count < 2)
                {
                    return CliParseResult.Fail($"missing recipe identifier after '{first}'.", json);
                }
                var envelope = CreateEnvelope("DESCRIBE_RECIPE", cwd);
                envelope.Recipe = argv[1];
                return ParseJsonFlagOnly(argv, 2, envelope);
            }

            if (IsAny(first, "--recipe", "-r", "run"))
            {
                return ParseRunRecipe(argv, cwd, json);
            }

            if (IsAny(first, "--file", "-f"))
            {
                if (argv.Count < 2)
                {
                    return CliParseResult.Fail($"missing file argument after '{first}'.", json);
                }
                var envelope = CreateEnvelope("OPEN_FILE", cwd);
                envelope.Files.AddRange(argv.Skip(1));
                return CliParseResult.Ok(envelope);
            }

            if (IsAny(first, "--reload", "--exit", "--version", "-v", "version"))
            {
                if (argv.Count > 1)
                {
                    return CliParseResult.Fail($"unexpected argument '{argv[1]}'.", json);
                }
                string command = IsAny(first, "--reload") ? "RELOAD_CONFIG" : IsAny(first, "--exit") ? "EXIT" : "VERSION";
                return CliParseResult.Ok(CreateEnvelope(command, cwd));
            }

            if (first.StartsWith("greenshot:", StringComparison.OrdinalIgnoreCase))
            {
                if (argv.Count > 1)
                {
                    return CliParseResult.Fail($"unexpected argument '{argv[1]}' after the URL.", json);
                }
                var envelope = CreateEnvelope("URL_SCHEME", cwd);
                envelope.RawInput = first;
                return CliParseResult.Ok(envelope);
            }

            if (first.StartsWith("-", StringComparison.Ordinal))
            {
                return CliParseResult.Fail($"unrecognized option '{first}'.", json);
            }

            // Default: positional file path(s)
            var openEnvelope = CreateEnvelope("OPEN_FILE", cwd);
            openEnvelope.Files.AddRange(argv);
            return CliParseResult.Ok(openEnvelope);
        }

        /// <summary>Accepts only a trailing "--json" (for --list-recipes and --info).</summary>
        private static CliParseResult ParseJsonFlagOnly(IList<string> argv, int firstIndex, IpcEnvelope envelope)
        {
            for (int i = firstIndex; i < argv.Count; i++)
            {
                if (IsAny(argv[i], "--json"))
                {
                    envelope.Json = true;
                }
                else
                {
                    return CliParseResult.Fail($"unexpected argument '{argv[i]}'.", envelope.Json || argv.Skip(i).Any(a => IsAny(a, "--json")));
                }
            }
            return CliParseResult.Ok(envelope);
        }

        /// <summary>
        /// --recipe &lt;id&gt; followed by recipe arguments (key=value, --key=value, --key value; values are taken verbatim,
        /// also when they start with '-') and the options --json, --query/-q, --async/--fire-and-forget and "--".
        /// </summary>
        private static CliParseResult ParseRunRecipe(IList<string> argv, string cwd, bool json)
        {
            if (argv.Count < 2)
            {
                return CliParseResult.Fail($"missing recipe identifier after '{argv[0]}'.", json);
            }

            var envelope = CreateEnvelope("RUN_RECIPE", cwd);
            envelope.Recipe = argv[1];

            bool optionsEnded = false;
            int i = 2;
            while (i < argv.Count)
            {
                string arg = argv[i];

                if (!optionsEnded)
                {
                    if (arg == "--")
                    {
                        optionsEnded = true;
                        i++;
                        continue;
                    }
                    if (IsAny(arg, "--json"))
                    {
                        envelope.Json = true;
                        i++;
                        continue;
                    }
                    if (IsAny(arg, "--async", "--fire-and-forget"))
                    {
                        envelope.Async = true;
                        i++;
                        continue;
                    }
                    if (IsAny(arg, "--query", "-q"))
                    {
                        if (i + 1 >= argv.Count)
                        {
                            return CliParseResult.Fail($"'{arg}' requires an expression.", json);
                        }
                        envelope.Query = argv[i + 1];
                        i += 2;
                        continue;
                    }
                    if (arg.StartsWith("--query=", StringComparison.OrdinalIgnoreCase))
                    {
                        envelope.Query = arg.Substring("--query=".Length);
                        i++;
                        continue;
                    }
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        // --key=value, --key value (also -k value)
                        string name = arg.StartsWith("--", StringComparison.Ordinal) ? arg.Substring(2) : arg.Substring(1);
                        int equals = name.IndexOf('=');
                        string key = equals >= 0 ? name.Substring(0, equals) : name;
                        if (key.Length == 0)
                        {
                            return CliParseResult.Fail($"invalid argument '{arg}'.", json);
                        }

                        string value;
                        if (equals >= 0)
                        {
                            value = name.Substring(equals + 1);
                            i++;
                        }
                        else
                        {
                            if (i + 1 >= argv.Count)
                            {
                                return CliParseResult.Fail($"missing value for argument '{arg}'.", json);
                            }
                            value = argv[i + 1];
                            i += 2;
                        }

                        envelope.Parameters[key] = value;
                        continue;
                    }
                }

                // key=value
                int separator = arg.IndexOf('=');
                if (separator <= 0)
                {
                    return CliParseResult.Fail($"unexpected argument '{arg}'. Pass recipe arguments as key=value, --key=value or --key value.", json);
                }
                envelope.Parameters[arg.Substring(0, separator)] = arg.Substring(separator + 1);
                i++;
            }

            return CliParseResult.Ok(envelope);
        }
    }
}
