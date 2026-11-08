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
using System.IO;
using System.Linq;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Definitions;
using Greenshot.Base.Expressions;
using log4net;

namespace Greenshot.Plugin.ExternalCommand.Running
{
    /// <summary>
    /// Where the command definitions come from: the store in the configuration, the built-in commands (MS Paint, Paint.NET when
    /// installed), and the one-time conversion of the commands of Greenshot 1.3.
    /// </summary>
    public static class CommandDefinitions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CommandDefinitions));

        public const string FileNew = "New";
        public const string FileLastExport = "LastExport";
        public const string FileNone = "None";
        public const string InputNone = "None";
        public const string InputImage = "Image";
        public const string InputText = "Text";
        public const string ClipboardNone = "None";
        public const string ClipboardOutput = "Output";
        public const string ClipboardUrl = "Url";

        /// <summary>
        /// What a file name handed to a command may not contain: no shell reads the arguments (the program is started directly), so only
        /// what breaks a quoted argument (a quote, a line break) or is expanded by cmd.exe (%) is refused.
        /// </summary>
        private static readonly char[] UnsafeFileNameChars = { '"', '%', '\n', '\r' };

        public const string MsPaint = "MS Paint";
        public const string PaintDotNet = "Paint.NET";

        private static readonly Lazy<string> PaintPath = new Lazy<string>(() => ExistingFile(() => PluginUtils.GetExePath("pbrush.exe")));

        private static readonly Lazy<string> PaintDotNetPath = new Lazy<string>(() =>
            ExistingFile(() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Paint.NET\PaintDotNet.exe")));

        /// <summary>
        /// The configuration section, null when it isn't available (tests without the plugin's settings)
        /// </summary>
        public static IExternalCommandConfiguration Config
        {
            get
            {
                try
                {
                    return IniConfigRegistry.GetSection<IExternalCommandConfiguration>();
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The commands of the Commands folder with the built-in ones; the commands of 1.3 are converted the first time.
        /// Without the configuration an empty store.
        /// </summary>
        public static CommandStore CreateStore()
        {
            var config = Config;
            if (config == null)
            {
                return new CommandStore(new Dictionary<string, string>(), null);
            }

            config.OnAfterLoad();
            var store = new CommandStore(new DefinitionFolder(FolderPath, CommandKind.FileExtension, CommandKind.MaximumLength), null, BuiltIns(config));
            if (!config.DefinitionsConverted)
            {
                ConvertOnce(config, store);
                // The conversion is kept at once, it isn't done again at the next start
                IniConfigRegistry.Get()?.Save();
            }

            return store;
        }

        /// <summary>
        /// The folder of the commands, next to greenshot.ini: one .gscommand.json file per command
        /// </summary>
        public static string FolderPath => DefinitionFolder.NextToConfiguration("Commands");

        /// <summary>
        /// The definition with the name, null when there is none
        /// </summary>
        public static CommandDefinition Find(string name) => string.IsNullOrWhiteSpace(name) ? null : CreateStore().Find(name)?.Definition;

        /// <summary>
        /// MS Paint and Paint.NET, when they are installed and weren't deleted in Greenshot 1.3
        /// </summary>
        public static IReadOnlyList<CommandDefinition> BuiltIns(IExternalCommandConfiguration config)
        {
            var deleted = config?.DeletedBuildInCommands ?? new List<string>();
            var builtIns = new List<CommandDefinition>();
            if (PaintPath.Value != null && !deleted.Contains(MsPaint))
            {
                builtIns.Add(BuiltIn(MsPaint, PaintPath.Value));
            }

            if (PaintDotNetPath.Value != null && !deleted.Contains(PaintDotNet))
            {
                builtIns.Add(BuiltIn(PaintDotNet, PaintDotNetPath.Value));
            }

            return builtIns;
        }

        private static CommandDefinition BuiltIn(string name, string program) => new CommandDefinition
        {
            Name = name,
            Program = program,
            Arguments = CommandDefinition.DefaultArguments,
            RunInBackground = true
        };

        /// <summary>
        /// Convert the commands of Greenshot 1.3 (and of early 1.4 builds) into definitions, once. The old keys stay for going back to 1.3.
        /// A command which is a built-in one with the same program and arguments isn't converted, the built-in one takes its place.
        /// </summary>
        public static void ConvertOnce(IExternalCommandConfiguration config, CommandStore store)
        {
            if (config == null || store == null || config.DefinitionsConverted)
            {
                return;
            }

            config.OnAfterLoad();
            var builtIns = store.GetAll().Where(stored => stored.IsBuiltIn).Select(stored => stored.Definition).ToList();
            foreach (string name in config.Commands.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToList())
            {
                var definition = FromLegacy(config, name);
                if (definition == null)
                {
                    Log.WarnFormat("The external command {0} of Greenshot 1.3 has no program, it isn't converted", name);
                    continue;
                }

                var builtIn = builtIns.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
                if (builtIn != null)
                {
                    if (string.Equals(builtIn.Program, definition.Program, StringComparison.OrdinalIgnoreCase) && string.Equals(builtIn.Arguments, definition.Arguments, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // Changed by the user: kept next to the built-in one
                    definition.Name = name + " (1.3)";
                }

                try
                {
                    CommandKind.Check(definition);
                    store.Add(definition);
                    Log.InfoFormat("Converted the external command {0} of Greenshot 1.3", definition.Name);
                }
                catch (Exception ex) when (ex is DefinitionFileException or InvalidOperationException or IOException or UnauthorizedAccessException)
                {
                    Log.WarnFormat("The external command {0} of Greenshot 1.3 can't be converted: {1}", name, ex.Message);
                }
            }

            config.DefinitionsConverted = true;
            config.MarkChanged();
        }

        /// <summary>
        /// The command of the old keys as a definition (not checked), null without a program
        /// </summary>
        public static CommandDefinition FromLegacy(IExternalCommandConfiguration config, string name)
        {
            string program = Get(config.CommandlineTemplate, name);
            if (program == null && Get(config.Commandline, name) is { } commandline)
            {
                program = LegacyTemplateConverter.Convert(commandline, true);
            }

            if (program == null)
            {
                return null;
            }

            string arguments = Get(config.ArgumentTemplate, name);
            if (arguments == null && Get(config.Argument, name) is { } argument)
            {
                arguments = LegacyTemplateConverter.ConvertArguments(argument);
            }

            bool readOutput = PerCommand(config.RedirectStandardOutputCommand, name, config.RedirectStandardOutput);
            bool findUrl = PerCommand(config.ParseOutputForUriCommand, name, config.ParseOutputForUri);
            bool copyOutput = PerCommand(config.OutputToClipboardCommand, name, config.OutputToClipboard);
            bool copyUrl = PerCommand(config.UriToClipboardCommand, name, config.UriToClipboard);
            string clipboard = !readOutput ? "None" : copyUrl && findUrl ? "Url" : copyOutput ? "Output" : "None";
            return new CommandDefinition
            {
                Name = name,
                Program = program,
                Arguments = arguments ?? CommandDefinition.DefaultArguments,
                Format = Get(config.OutputFormat, name),
                // Greenshot 1.3 handed over the file the capture was saved to, when it was
                File = FileLastExport,
                RunInBackground = config.RunInbackground == null || !config.RunInbackground.TryGetValue(name, out bool background) || background,
                // The output was read for the clipboard and for the link of the notification
                Output = readOutput && (findUrl || copyOutput) ? nameof(CommandOutput.Text) : nameof(CommandOutput.None),
                CopyToClipboard = clipboard
            };
        }

        /// <summary>
        /// The request of a command: program, arguments, working folder and the text on standard input rendered, the output as the
        /// definition says (none for a command in the background). An image on standard input is added by the caller.
        /// </summary>
        /// <exception cref="ArgumentException">a template can't be rendered, or the file name can't be passed safely</exception>
        public static CommandRequest CreateRequest(CommandDefinition definition, string fullPath, VariableScope scope)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            scope ??= VariableScope.Empty;
            var encoding = Recipes.ExternalCommandStep.EncodingOf(definition.Encoding);
            var request = new CommandRequest
            {
                Program = OutputTemplates.RenderCommand(definition.Program, scope, out string problem) ?? throw new ArgumentException(problem),
                Arguments = RenderArguments(definition.Arguments, fullPath, scope),
                Output = definition.RunInBackground ? CommandOutput.None : (CommandOutput)Enum.Parse(typeof(CommandOutput), definition.Output ?? nameof(CommandOutput.None), true),
                ReadError = !definition.RunInBackground,
                FindUrl = true,
                Encoding = encoding,
                Timeout = definition.Timeout > 0 ? TimeSpan.FromSeconds(definition.Timeout) : null,
                SuccessExitCodes = CommandRunner.ParseExitCodes(definition.SuccessExitCodes)
            };

            if (!string.IsNullOrWhiteSpace(definition.WorkingDirectory))
            {
                request.WorkingDirectory = OutputTemplates.RenderFolder(definition.WorkingDirectory, scope, out problem) ?? throw new ArgumentException(problem);
            }
            else if (fullPath != null)
            {
                request.WorkingDirectory = Path.GetDirectoryName(fullPath);
            }

            if (CommandKind.Is(definition.StandardInput, InputText))
            {
                string text = VariableResolver.Instance.Render(definition.StandardInputText ?? string.Empty, TemplatePurpose.Text, scope);
                request.StandardInput = encoding.GetBytes(text ?? string.Empty);
            }

            return request;
        }

        /// <summary>
        /// The arguments of a command: the template rendered for the file (${export.file}), which can't hold characters which break a
        /// quoted argument.
        /// </summary>
        /// <exception cref="ArgumentException">The file name has a character which can't be passed safely</exception>
        public static string RenderArguments(string arguments, string fullpath, VariableScope scope)
        {
            if (fullpath != null && fullpath.IndexOfAny(UnsafeFileNameChars) >= 0)
            {
                throw new ArgumentException($"The file name '{fullpath}' contains a character which can't be passed to a command safely (\", % or a line break).");
            }

            return VariableResolver.Instance.Render(arguments ?? string.Empty, TemplatePurpose.Argument, (scope ?? VariableScope.Empty).With(exportFile: fullpath));
        }

        private static string Get(Dictionary<string, string> values, string name) =>
            values != null && values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

        private static bool PerCommand(Dictionary<string, bool> values, string name, bool fallback) =>
            values != null && values.TryGetValue(name, out var value) ? value : fallback;

        private static string ExistingFile(Func<string> path)
        {
            try
            {
                string file = path();
                return !string.IsNullOrEmpty(file) && File.Exists(file) ? file : null;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                return null;
            }
        }
    }
}
