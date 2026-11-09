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
using Dapplo.Ini;
using Greenshot.Base.Definitions;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.SmartText.Detection;
using log4net;

namespace Greenshot.Plugin.SmartText
{
    /// <summary>
    /// The own patterns as definitions: one .gssmarttext.json file per pattern in the SmartText folder next to greenshot.ini, plain
    /// JSON (a pattern holds no secrets), checked on each read. Like the commands, a file Greenshot didn't write is ignored and one
    /// changed outside Greenshot is only used after the user approved it.
    /// </summary>
    public sealed class SmartTextKind : IDefinitionKind<SmartTextPattern>
    {
        public static SmartTextKind Instance { get; } = new SmartTextKind();

        /// <summary>Larger files aren't read</summary>
        public const int MaximumLength = 64 * 1024;

        public const string FileExtension = ".gssmarttext.json";

        /// <summary>Indented and camelCase, as in the files of the SmartText folder</summary>
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private SmartTextKind()
        {
        }

        public string Designation => "SmartText";

        public string NameOf(SmartTextPattern definition) => definition.Name;

        public SmartTextPattern Read(string json)
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

        public string Write(SmartTextPattern definition)
        {
            var copy = definition.Clone();
            copy.Kind = SmartTextPattern.KindName;
            copy.Version = SmartTextPattern.CurrentVersion;
            return JsonSerializer.Serialize(copy, JsonOptions);
        }

        /// <summary>
        /// The pattern and, with a label, the label
        /// </summary>
        public string Describe(SmartTextPattern definition) =>
            string.IsNullOrWhiteSpace(definition.Label) ? definition.Pattern : $"{definition.Label} → {definition.Pattern}";

        /// <summary>
        /// A pattern isn't approved per recipe: it runs nothing, the file itself is approved
        /// </summary>
        public RecipeGatedAction CreateGatedAction(SmartTextPattern definition) => null;

        /// <summary>
        /// Read and check a pattern file
        /// </summary>
        /// <exception cref="DefinitionFileException">it isn't a pattern or can't be used, the message says why</exception>
        public static SmartTextPattern Parse(string json)
        {
            var language = SmartTextPlugin.Language;
            if (string.IsNullOrWhiteSpace(json)) throw new DefinitionFileException(language.ErrorNotAPattern);
            if (json.Length > MaximumLength) throw new DefinitionFileException(language.ErrorFileTooLarge);

            SmartTextPattern definition;
            try
            {
                definition = JsonSerializer.Deserialize<SmartTextPattern>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new DefinitionFileException(language.ErrorNotAPattern, ex);
            }

            if (definition == null || !string.Equals(definition.Kind, SmartTextPattern.KindName, StringComparison.OrdinalIgnoreCase))
            {
                throw new DefinitionFileException(language.ErrorNotAPattern);
            }

            if (definition.Version > SmartTextPattern.CurrentVersion)
            {
                throw new DefinitionFileException(string.Format(language.ErrorNewerVersion, definition.Version));
            }

            Check(definition);
            return definition;
        }

        /// <summary>
        /// Check a pattern before it is stored
        /// </summary>
        /// <exception cref="DefinitionFileException">the pattern can't be used, the message says why</exception>
        public static void Check(SmartTextPattern definition)
        {
            var problems = SmartTextPatternCheck.Problems(definition);
            if (problems.Count > 0)
            {
                throw new DefinitionFileException(string.Join(" ", problems));
            }

            definition.Name = definition.Name.Trim();
        }
    }

    /// <summary>
    /// The own patterns in the SmartText folder
    /// </summary>
    public sealed class SmartTextStore : DefinitionStore<SmartTextPattern>
    {
        public SmartTextStore(IDictionary<string, string> definitions, Action changed)
            : base(SmartTextKind.Instance, definitions, changed, null, text => text, text => text)
        {
        }
    }

    /// <summary>
    /// Which detectors there are and which are on: the built-in ones, the own patterns, and the setting per detector
    /// </summary>
    public static class SmartTextDefinitions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SmartTextDefinitions));

        /// <summary>
        /// The folder of the own patterns, next to greenshot.ini: one .gssmarttext.json file per pattern
        /// </summary>
        public static string FolderPath => DefinitionFolder.NextToConfiguration("SmartText");

        /// <summary>
        /// The configuration section, null when it isn't available (tests without the plugin's settings)
        /// </summary>
        public static ISmartTextConfiguration Config
        {
            get
            {
                try
                {
                    return IniConfigRegistry.GetSection<ISmartTextConfiguration>();
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The own patterns of the SmartText folder; without the configuration an empty store
        /// </summary>
        public static SmartTextStore CreateStore() =>
            Config == null
                ? new SmartTextStore(new Dictionary<string, string>(), null)
                : new SmartTextStore(DefinitionFolder.Open(FolderPath, SmartTextKind.FileExtension, SmartTextKind.MaximumLength), null);

        /// <summary>
        /// The name of a built-in detector in the language of the user
        /// </summary>
        public static string NameOf(string builtInId)
        {
            var language = SmartTextPlugin.Language;
            return builtInId switch
            {
                BuiltInDetectors.Url => language.NameUrl,
                BuiltInDetectors.Email => language.NameEmail,
                BuiltInDetectors.Phone => language.NamePhone,
                BuiltInDetectors.Iban => language.NameIban,
                BuiltInDetectors.Date => language.NameDate,
                BuiltInDetectors.IssueKey => language.NameIssueKey,
                BuiltInDetectors.IpAddress => language.NameIpAddress,
                BuiltInDetectors.MacAddress => language.NameMacAddress,
                BuiltInDetectors.Guid => language.NameGuid,
                BuiltInDetectors.Hash => language.NameHash,
                BuiltInDetectors.ErrorCode => language.NameErrorCode,
                BuiltInDetectors.Path => language.NamePath,
                BuiltInDetectors.Money => language.NameMoney,
                BuiltInDetectors.VatId => language.NameVatId,
                BuiltInDetectors.Bic => language.NameBic,
                BuiltInDetectors.Isbn => language.NameIsbn,
                BuiltInDetectors.Ean => language.NameEan,
                BuiltInDetectors.CardNumber => language.NameCardNumber,
                BuiltInDetectors.Coordinates => language.NameCoordinates,
                _ => builtInId
            };
        }

        /// <summary>
        /// Is the detector on: the setting, else its default (built-in ones as they come, own patterns on)
        /// </summary>
        public static bool IsOn(string id, bool isOnByDefault)
        {
            var detectors = Config?.Detectors;
            return detectors != null && id != null && detectors.Keys.FirstOrDefault(key => string.Equals(key, id, StringComparison.OrdinalIgnoreCase)) is { } key
                ? detectors[key]
                : isOnByDefault;
        }

        /// <summary>
        /// Turn a detector on or off, kept in greenshot.ini at once; the default is not written
        /// </summary>
        public static void SetOn(string id, bool on, bool isOnByDefault)
        {
            var config = Config;
            if (config == null || string.IsNullOrWhiteSpace(id)) return;
            var detectors = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in config.Detectors ?? new Dictionary<string, bool>())
            {
                if (!string.Equals(entry.Key, id, StringComparison.OrdinalIgnoreCase)) detectors[entry.Key] = entry.Value;
            }

            if (on != isOnByDefault)
            {
                detectors[id] = on;
            }

            config.Detectors = detectors;
            (config as SmartTextConfigurationImpl)?.MarkChanged();
            IniConfigRegistry.Get()?.Save();
        }

        /// <summary>
        /// Can an own pattern have the name: not the id or the name of a built-in detector
        /// </summary>
        public static bool IsBuiltInName(string name, IEnumerable<BuiltInDetector> builtIns) =>
            !string.IsNullOrWhiteSpace(name) && builtIns.Any(builtIn => string.Equals(builtIn.Id, name.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                                                         string.Equals(NameOf(builtIn.Id), name.Trim(), StringComparison.CurrentCultureIgnoreCase));

        /// <summary>
        /// The detectors to run
        /// </summary>
        /// <param name="names">the ids of built-in detectors and the names of own patterns, null or empty: all which are on</param>
        /// <param name="inline">the patterns of the recipe, they come first</param>
        /// <param name="unknown">the names which are no detector</param>
        /// <param name="culture">the culture of dates, null: the current one</param>
        /// <param name="area">where to look, it replaces the area of each pattern; null: as the patterns say</param>
        public static IReadOnlyList<SmartTextDetector> Detectors(IReadOnlyCollection<string> names, IEnumerable<SmartTextPattern> inline, out IReadOnlyList<string> unknown,
            CultureInfo culture = null, SmartTextArea area = null)
        {
            var detectors = DetectorsOf(names, inline, out unknown, culture);
            if (area == null) return detectors;
            return detectors.Select(detector =>
            {
                var definition = detector.Definition.Clone();
                definition.Area = area;
                return new SmartTextDetector(detector.Id, detector.Name, definition, detector.IsOwnPattern, culture);
            }).ToList();
        }

        private static IReadOnlyList<SmartTextDetector> DetectorsOf(IReadOnlyCollection<string> names, IEnumerable<SmartTextPattern> inline, out IReadOnlyList<string> unknown, CultureInfo culture)
        {
            var detectors = new List<SmartTextDetector>();
            foreach (var pattern in inline ?? Enumerable.Empty<SmartTextPattern>())
            {
                if (pattern == null) continue;
                detectors.Add(new SmartTextDetector(pattern.Name, pattern.Name, pattern, true, culture));
            }

            var builtIns = BuiltInDetectors.Create(culture);
            var own = CreateStore().GetReadable();
            var missing = new List<string>();
            bool all = names == null || names.Count == 0;
            if (all)
            {
                // Without names: all which are on, but not with only inline patterns (the recipe says what it looks for)
                if (detectors.Count == 0)
                {
                    detectors.AddRange(builtIns.Where(builtIn => IsOn(builtIn.Id, builtIn.IsOnByDefault)).Select(builtIn => Create(builtIn, culture)));
                    detectors.AddRange(own.Where(pattern => IsOn(pattern.Name, true)).Select(pattern => Create(pattern, culture)).Where(d => d != null));
                }
            }
            else
            {
                foreach (string name in names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var builtIn = BuiltInDetectors.Find(builtIns, name);
                    var pattern = builtIn == null ? own.FirstOrDefault(p => string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)) : null;
                    var detector = builtIn != null ? Create(builtIn, culture) : pattern != null ? Create(pattern, culture) : null;
                    if (detector == null)
                    {
                        missing.Add(name);
                        continue;
                    }

                    detectors.Add(detector);
                }
            }

            unknown = missing;
            return detectors;
        }

        private static SmartTextDetector Create(BuiltInDetector builtIn, CultureInfo culture) =>
            new SmartTextDetector(builtIn.Id, NameOf(builtIn.Id), builtIn.Pattern, false, culture);

        private static SmartTextDetector Create(SmartTextPattern pattern, CultureInfo culture)
        {
            try
            {
                return new SmartTextDetector(pattern.Name, pattern.Name, pattern, true, culture);
            }
            catch (ArgumentException ex)
            {
                Log.WarnFormat("The Smart text pattern {0} can't be used: {1}", pattern.Name, ex.Message);
                return null;
            }
        }
    }
}
