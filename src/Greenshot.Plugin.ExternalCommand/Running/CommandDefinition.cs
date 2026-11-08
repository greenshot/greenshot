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
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenshot.Base.Definitions;
using Greenshot.Base.Expressions;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Plugin.ExternalCommand.Running
{
    /// <summary>
    /// An external command as a definition: the same fields as the ExternalCommand recipe step, without the ResultName, which
    /// belongs to the recipe. Stored as a .gscommand.json file in the Commands folder.
    /// See docs/vNext/destination-definitions.md and docs/vNext/capture-recipes.md.
    /// </summary>
    public sealed class CommandDefinition
    {
        public const string KindName = "externalCommand";
        public const int CurrentVersion = 1;
        public const string DefaultArguments = "\"${export.file}\"";

        public string Kind { get; set; } = KindName;

        public int Version { get; set; } = CurrentVersion;

        public string Name { get; set; }

        /// <summary>What it is for, shown nowhere else than in the file</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; }

        /// <summary>The program, a template (Command context)</summary>
        public string Program { get; set; }

        /// <summary>The arguments, a template (Argument context)</summary>
        public string Arguments { get; set; } = DefaultArguments;

        /// <summary>A template (Folder context), null: the folder of the file</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string WorkingDirectory { get; set; }

        /// <summary>New, LastExport or None</summary>
        public string File { get; set; } = "New";

        /// <summary>The file format id, null: PNG</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Format { get; set; }

        /// <summary>None, Image or Text</summary>
        public string StandardInput { get; set; } = "None";

        /// <summary>A template (Text context), only with StandardInput Text</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string StandardInputText { get; set; }

        /// <summary>Text, Image, File or None</summary>
        public string Output { get; set; } = nameof(CommandOutput.None);

        /// <summary>None, Output or Url</summary>
        public string CopyToClipboard { get; set; } = "None";

        /// <summary>e.g. 0 or 0,1</summary>
        public string SuccessExitCodes { get; set; } = "0";

        /// <summary>Seconds, 0: no limit</summary>
        public int Timeout { get; set; }

        /// <summary>utf-8, oem or an encoding name</summary>
        public string Encoding { get; set; } = "utf-8";

        /// <summary>Started without waiting for it</summary>
        public bool RunInBackground { get; set; }

        /// <summary>
        /// A copy, e.g. to edit it
        /// </summary>
        public CommandDefinition Clone() => (CommandDefinition)MemberwiseClone();
    }

    /// <summary>
    /// External commands as definitions: read and checked from JSON, described by their program and arguments, approved as an
    /// external command.
    /// </summary>
    public sealed class CommandKind : IDefinitionKind<CommandDefinition>
    {
        public static CommandKind Instance { get; } = new CommandKind();

        /// <summary>
        /// Larger files aren't read
        /// </summary>
        public const int MaximumLength = 64 * 1024;

        public const string FileExtension = ".gscommand.json";

        public static readonly string[] FileValues = { "New", "LastExport", "None" };
        public static readonly string[] StandardInputValues = { "None", "Image", "Text" };
        public static readonly string[] OutputValues = Enum.GetNames(typeof(CommandOutput));
        public static readonly string[] ClipboardValues = { "None", "Output", "Url" };

        /// <summary>
        /// Indented, as in the .gscommand.json files of the Commands folder (UTF-8: nothing needs escaping for HTML)
        /// </summary>
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private CommandKind()
        {
        }

        private static IExternalCommandLanguage Language => Texts.Get<IExternalCommandLanguage>();

        public string Designation => "ExternalCommand";

        public string NameOf(CommandDefinition definition) => definition.Name;

        public CommandDefinition Read(string json)
        {
            try
            {
                return Parse(json);
            }
            catch (DefinitionFileException)
            {
                return null;
            }
        }

        public string Write(CommandDefinition definition) => JsonSerializer.Serialize(definition, JsonOptions);

        /// <summary>
        /// The program and its arguments
        /// </summary>
        public string Describe(CommandDefinition definition) => $"{definition.Program} {definition.Arguments}".Trim();

        public RecipeGatedAction CreateGatedAction(CommandDefinition definition) =>
            new RecipeGatedAction(RecipeGateType.ExternalCommand, GateTarget(definition), "Recipe.gate_external_command");

        /// <summary>
        /// What the user approves: the name, the program with its arguments, the text on standard input and whether the command replaces the image
        /// </summary>
        public static string GateTarget(CommandDefinition definition)
        {
            string target = string.IsNullOrEmpty(definition.Name) ? Instance.Describe(definition) : $"{definition.Name} ({Instance.Describe(definition)})";
            if (Is(definition.StandardInput, "Text"))
            {
                target += $", standard input: {definition.StandardInputText}";
            }

            if (Is(definition.Output, nameof(CommandOutput.Image)) || Is(definition.Output, nameof(CommandOutput.File)))
            {
                target += ", replaces the image";
            }

            return target;
        }

        /// <summary>
        /// Read and check a command definition, the values of the choices normalized (case).
        /// </summary>
        /// <exception cref="DefinitionFileException">it can't be used, the message says why</exception>
        public static CommandDefinition Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new DefinitionFileException(Language.ErrorNotACommand);
            if (json.Length > MaximumLength) throw new DefinitionFileException(Language.ErrorFileTooLarge);

            CommandDefinition definition;
            try
            {
                definition = JsonSerializer.Deserialize<CommandDefinition>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new DefinitionFileException(Language.ErrorNotACommand, ex);
            }

            if (definition == null || !string.Equals(definition.Kind, CommandDefinition.KindName, StringComparison.OrdinalIgnoreCase))
            {
                throw new DefinitionFileException(Language.ErrorNotACommand);
            }

            if (definition.Version > CommandDefinition.CurrentVersion)
            {
                throw new DefinitionFileException(string.Format(Language.ErrorNewerVersion, definition.Version));
            }

            Check(definition);
            return definition;
        }

        /// <summary>
        /// Check the definition like a file, normalize the values of the choices (case, defaults for empty values).
        /// </summary>
        /// <exception cref="DefinitionFileException">it can't be used, the message says why</exception>
        public static void Check(CommandDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            definition.Kind = CommandDefinition.KindName;
            definition.Version = CommandDefinition.CurrentVersion;
            definition.Name = definition.Name?.Trim();
            if (string.IsNullOrEmpty(definition.Name))
            {
                throw new DefinitionFileException(Language.ErrorNoName);
            }

            if (string.IsNullOrWhiteSpace(definition.Program))
            {
                throw new DefinitionFileException(Language.ErrorNoProgram);
            }

            if (OutputTemplates.RenderCommand(definition.Program, VariableScope.Empty, out string problem) == null)
            {
                throw new DefinitionFileException(string.Format(Language.ErrorProgram, problem));
            }

            definition.Arguments ??= string.Empty;
            definition.File = OneOf(definition.File, "File", FileValues);
            definition.StandardInput = OneOf(definition.StandardInput, "StandardInput", StandardInputValues);
            definition.Output = OneOf(definition.Output, "Output", OutputValues);
            definition.CopyToClipboard = OneOf(definition.CopyToClipboard, "CopyToClipboard", ClipboardValues);
            definition.WorkingDirectory = string.IsNullOrWhiteSpace(definition.WorkingDirectory) ? null : definition.WorkingDirectory;
            definition.Format = string.IsNullOrWhiteSpace(definition.Format) ? null : definition.Format.Trim();

            if (!Is(definition.StandardInput, "Text"))
            {
                definition.StandardInputText = null;
            }

            if (definition.RunInBackground && (Is(definition.Output, nameof(CommandOutput.Image)) || Is(definition.Output, nameof(CommandOutput.File))))
            {
                throw new DefinitionFileException(Language.ErrorBackgroundImage);
            }

            if (Is(definition.Output, nameof(CommandOutput.File)) && Is(definition.File, "None"))
            {
                throw new DefinitionFileException(Language.ErrorOutputFileWithoutFile);
            }

            if (!Is(definition.CopyToClipboard, "None") && !Is(definition.Output, nameof(CommandOutput.Text)))
            {
                throw new DefinitionFileException(Language.ErrorClipboardWithoutText);
            }

            try
            {
                definition.SuccessExitCodes = string.Join(",", CommandRunner.ParseExitCodes(definition.SuccessExitCodes).Select(code => code.ToString(CultureInfo.InvariantCulture)));
            }
            catch (FormatException ex)
            {
                throw new DefinitionFileException(string.Format(Language.ErrorValue, "SuccessExitCodes", ex.Message), ex);
            }

            if (definition.Timeout < 0)
            {
                throw new DefinitionFileException(string.Format(Language.ErrorValue, "Timeout", definition.Timeout));
            }

            definition.Encoding = string.IsNullOrWhiteSpace(definition.Encoding) ? "utf-8" : definition.Encoding.Trim();
            try
            {
                Recipes.ExternalCommandStep.EncodingOf(definition.Encoding);
            }
            catch (ArgumentException ex)
            {
                throw new DefinitionFileException(string.Format(Language.ErrorValue, "Encoding", definition.Encoding), ex);
            }
        }

        private static string OneOf(string value, string name, IReadOnlyList<string> allowed)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return allowed.Contains("None") ? "None" : allowed[0];
            }

            return allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase))
                   ?? throw new DefinitionFileException(string.Format(Language.ErrorValue, name, value));
        }

        internal static bool Is(string value, string expected) => string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The commands, with the built-in ones (MS Paint, Paint.NET when installed): one .gscommand.json file per command in the
    /// Commands folder (<see cref="DefinitionFolder"/>), plain JSON (a command holds no secrets), checked on each read.
    /// </summary>
    public sealed class CommandStore : DefinitionStore<CommandDefinition>
    {
        public CommandStore(IDictionary<string, string> definitions, Action changed, IEnumerable<CommandDefinition> builtIns = null)
            : base(CommandKind.Instance, definitions, changed, null, text => text, text => text, builtIns)
        {
        }
    }
}
