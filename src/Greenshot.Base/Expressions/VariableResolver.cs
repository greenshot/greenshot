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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using log4net;
using static Greenshot.Base.Expressions.ExpressionValues;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// The result of <see cref="IVariableResolver.RenderFileName"/>: the name, and the counters to store once the file
    /// was really written or uploaded.
    /// </summary>
    public sealed class RenderedFileName
    {
        private readonly Action _commit;
        private int _committed;

        internal RenderedFileName(string name, string fullPath, Action commit)
        {
            Name = name;
            FullPath = fullPath;
            _commit = commit;
        }

        /// <summary>The file name with extension, may hold sub folders ("2026-11-06\001 - Notepad.png")</summary>
        public string Name { get; }

        /// <summary>The full path in <see cref="VariableScope.TargetFolder"/>, null without a target folder</summary>
        public string FullPath { get; }

        /// <summary>The file was written or uploaded: store the counters it used (once)</summary>
        public void Commit()
        {
            if (Interlocked.Exchange(ref _committed, 1) == 0)
            {
                _commit?.Invoke();
            }
        }

        public override string ToString() => FullPath ?? Name;
    }

    /// <summary>
    /// The one implementation of <see cref="IVariableResolver"/>, see docs/vNext/expression-engine.md
    /// </summary>
    public sealed class VariableResolver : IVariableResolver, IVariableRegistry
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(VariableResolver));
        private static readonly Lazy<VariableResolver> LazyInstance = new Lazy<VariableResolver>(() => new VariableResolver(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>The resolver of Greenshot</summary>
        public static VariableResolver Instance => LazyInstance.Value;

        internal const string CounterGlobal = "counter.global";
        internal const string CounterFolder = "counter.folder";
        internal const string CounterName = "counter.name";
        internal const string CounterScope = "counter.scope";

        /// <summary>A value in a file name is at most this long (as the window title in 1.3)</summary>
        internal const int MaxFileNameValueLength = 80;

        private readonly ConcurrentDictionary<string, IVariableProvider> _providers = new ConcurrentDictionary<string, IVariableProvider>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, ExpressionFunction> _functions = new ConcurrentDictionary<string, ExpressionFunction>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, Template> _templates = new ConcurrentDictionary<string, Template>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<(string, TemplatePurpose), IReadOnlyList<TemplateError>> _validations = new ConcurrentDictionary<(string, TemplatePurpose), IReadOnlyList<TemplateError>>();
        private readonly Func<long> _getGlobalCounter;
        private readonly Action<long> _setGlobalCounter;
        private readonly CounterStore _counterStore;
        private readonly VariableProvider _counterDescriptors;

        [ThreadStatic]
        private static bool _renderingOptionTemplate;

        public VariableResolver() : this(null, null, null)
        {
        }

        /// <summary>For tests: where the global and scoped counters are stored</summary>
        internal VariableResolver(Func<long> getGlobalCounter, Action<long> setGlobalCounter, Func<string> counterFilePath)
        {
            _getGlobalCounter = getGlobalCounter ?? GetGlobalCounterFromSettings;
            _setGlobalCounter = setGlobalCounter ?? SetGlobalCounterInSettings;
            _counterStore = new CounterStore(counterFilePath);
            foreach (var provider in CoreVariables.Create())
            {
                Register(provider);
            }
            _counterDescriptors = new VariableProvider("counter");
            var global = _counterDescriptors.Add<long>("global", VariableTrust.Trusted, "One number for all files, stored in greenshot.ini; goes up when a file was written or uploaded", null);
            global.IsCounter = true;
            global.Available = "file names";
            var folder = _counterDescriptors.Add<long>("folder", VariableTrust.Trusted, "The next free number in the folder the file goes to, read from its files", null);
            folder.IsCounter = true;
            folder.NeedsTargetFolder = true;
            folder.Available = "file names of a file written to a folder (not uploads)";
            var name = _counterDescriptors.Add<long>("name", VariableTrust.Trusted, "The next free number among the files of the folder with the same name apart from the number", null);
            name.IsCounter = true;
            name.NeedsTargetFolder = true;
            name.Available = "file names of a file written to a folder (not uploads)";
            _providers[_counterDescriptors.Namespace] = _counterDescriptors;
            foreach (var function in BuiltInFunctions.Create())
            {
                Register(function);
            }
            Register(new ExpressionFunction(CounterScope, 1, 1, "counter.scope(key)", "A stored count per key value, e.g. counter.scope(session.key ?? 'none')", a => null));
        }

        private static ICoreConfiguration CoreConfig
        {
            get
            {
                try
                {
                    return IniConfigRegistry.GetSection<ICoreConfiguration>();
                }
                catch (Exception ex) when (!(ex is OutOfMemoryException))
                {
                    return null;
                }
            }
        }

        private static long GetGlobalCounterFromSettings() => CoreConfig?.OutputFileIncrementingNumber ?? 0;

        private static void SetGlobalCounterInSettings(long value)
        {
            var config = CoreConfig;
            if (config == null) return;
            uint stored = (uint)Math.Max(0, Math.Min(uint.MaxValue, value));
            if (stored > config.OutputFileIncrementingNumber)
            {
                config.OutputFileIncrementingNumber = stored;
            }
        }

        public void Register(IVariableProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (!_providers.TryAdd(provider.Namespace, provider))
            {
                throw new InvalidOperationException($"The namespace '{provider.Namespace}' is already registered");
            }
            _validations.Clear();
        }

        public void Register(ExpressionFunction function)
        {
            if (function == null) throw new ArgumentNullException(nameof(function));
            if (!_functions.TryAdd(function.Name, function))
            {
                throw new InvalidOperationException($"The function '{function.Name}' is already registered");
            }
            _validations.Clear();
        }

        public IReadOnlyList<ExpressionFunction> Functions => _functions.Values.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();

        public Template Parse(string text)
        {
            text ??= string.Empty;
            if (_templates.Count > 4096) _templates.Clear();
            return _templates.GetOrAdd(text, TemplateParser.Parse);
        }

        public IReadOnlyList<VariableDescriptor> DescribeAll() =>
            _providers.Values
                .SelectMany(p => p.Variables)
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public IReadOnlyList<VariableDescriptor> Describe(TemplatePurpose purpose) =>
            _providers.Values
                .SelectMany(p => p.Variables)
                .Where(d => IsAllowed(d, purpose) == null)
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        #region Validation

        public IReadOnlyList<TemplateError> Validate(string text, TemplatePurpose purpose)
        {
            text ??= string.Empty;
            return _validations.GetOrAdd((text, purpose), key => ValidateCore(Parse(key.Item1), key.Item2));
        }

        private IReadOnlyList<TemplateError> ValidateCore(Template template, TemplatePurpose purpose)
        {
            var errors = new List<TemplateError>(template.Errors);
            if (errors.Count > 0) return errors;

            int folderCounters = 0;
            for (int index = 0; index < template.Segments.Count; index++)
            {
                var segment = template.Segments[index];
                if (segment.IsLiteral)
                {
                    CheckLiteral(segment, index, template, purpose, errors);
                    continue;
                }
                foreach (var node in Walk(segment.Expression))
                {
                    switch (node)
                    {
                        case VariableNode variable:
                            CheckVariable(variable, purpose, errors);
                            if (IsFolderCounter(variable.Name))
                            {
                                folderCounters++;
                                CheckFolderCounter(template, index, variable, purpose, errors);
                            }
                            break;
                        case CallNode call:
                            CheckCall(call, purpose, errors);
                            break;
                    }
                }
            }
            if (folderCounters > 1)
            {
                errors.Add(new TemplateError(0, "Only one counter.folder or counter.name can be used in a file name"));
            }
            return errors;
        }

        /// <summary>The fixed text of a file name or folder can't hold what Windows doesn't allow in a path</summary>
        private static void CheckLiteral(TemplateSegment segment, int index, Template template, TemplatePurpose purpose, List<TemplateError> errors)
        {
            if (purpose != TemplatePurpose.FileName && purpose != TemplatePurpose.Folder) return;
            string text = segment.Text;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                // A drive (C:) is the only place for a colon, at the start of a folder
                bool driveColon = c == ':' && purpose == TemplatePurpose.Folder && index == 0 && i == 1;
                if (c < ' ' || (c == ':' && !driveColon) || "<>\"|?*".IndexOf(c) >= 0)
                {
                    errors.Add(new TemplateError(segment.Position + i, $"'{c}' can't be in a file name or folder"));
                    return;
                }
            }
        }

        private static IEnumerable<ExpressionNode> Walk(ExpressionNode root)
        {
            var stack = new Stack<ExpressionNode>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node == null) continue;
                yield return node;
                foreach (var child in node.Children) stack.Push(child);
            }
        }

        private static bool IsFolderCounter(string name) =>
            string.Equals(name, CounterFolder, StringComparison.OrdinalIgnoreCase) || string.Equals(name, CounterName, StringComparison.OrdinalIgnoreCase);

        private static bool IsCounterName(string name) =>
            string.Equals(name, CounterGlobal, StringComparison.OrdinalIgnoreCase) || IsFolderCounter(name);

        internal VariableDescriptor FindDescriptor(string name)
        {
            int dot = name.IndexOf('.');
            if (dot <= 0) return null;
            if (!_providers.TryGetValue(name.Substring(0, dot), out var provider)) return null;
            if (provider is VariableProvider known) return known.Find(name);
            VariableDescriptor wildcard = null;
            foreach (var descriptor in provider.Variables)
            {
                if (string.Equals(descriptor.Name, name, StringComparison.OrdinalIgnoreCase)) return descriptor;
                if (descriptor.Matches(name) && (wildcard == null || descriptor.Name.Length > wildcard.Name.Length)) wildcard = descriptor;
            }
            return wildcard;
        }

        private void CheckVariable(VariableNode variable, TemplatePurpose purpose, List<TemplateError> errors)
        {
            var descriptor = FindDescriptor(variable.Name);
            if (descriptor == null)
            {
                if (variable.Name.StartsWith("session.", StringComparison.OrdinalIgnoreCase) || variable.Name.StartsWith("profile.", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(variable.Name, "counter.session", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new TemplateError(variable.Position, $"'{variable.Name}' is reserved for work sessions and profiles, which don't exist yet"));
                    return;
                }
                string hint = LegacyNames.Hint(variable.Name);
                errors.Add(new TemplateError(variable.Position, hint == null
                    ? $"'{variable.Name}' is not a known value"
                    : $"'{variable.Name}' is not a known value, use {hint}"));
                return;
            }
            string problem = IsAllowed(descriptor, purpose);
            if (problem != null)
            {
                errors.Add(new TemplateError(variable.Position, string.Format(problem, variable.Name)));
                return;
            }
            if (descriptor.WindowsOnly && Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                errors.Add(new TemplateError(variable.Position, $"'{variable.Name}' only has a value on Windows, use env.{LastPart(variable.Name)}") { IsWarning = true });
            }
        }

        private static string LastPart(string name)
        {
            int dot = name.LastIndexOf('.');
            return dot < 0 ? name : name.Substring(dot + 1);
        }

        /// <summary>Why the value can't be used for the purpose, as a format with {0} for the name; null when it can</summary>
        private static string IsAllowed(VariableDescriptor descriptor, TemplatePurpose purpose)
        {
            if (descriptor.Trust == VariableTrust.Untrusted)
            {
                switch (purpose)
                {
                    case TemplatePurpose.Folder:
                        return "${{{0}}} can't be used here: this is a folder or a path, and the value of {0} comes from outside Greenshot " +
                               "(the capture, an opened file, the command line or a recipe variable), so it could point anywhere. " +
                               "Such a value may only be part of a file name. Use a fixed folder here, e.g. ${{user.pictures}}";
                    case TemplatePurpose.Command:
                    case TemplatePurpose.Argument:
                        return "${{{0}}} can't be used here: this is part of a command, and the value of {0} comes from outside Greenshot " +
                               "(the capture, an opened file, the command line or a recipe variable), so it could run anything";
                }
            }
            if (descriptor.IsCounter && purpose != TemplatePurpose.FileName)
            {
                return "${{{0}}} is a counter, and counters can only be used in a file name";
            }
            return null;
        }

        private void CheckCall(CallNode call, TemplatePurpose purpose, List<TemplateError> errors)
        {
            if (!_functions.TryGetValue(call.Name, out var function))
            {
                errors.Add(new TemplateError(call.Position, $"'{call.Name}(...)' is not a known function"));
                return;
            }
            if (call.Arguments.Count < function.MinArguments || call.Arguments.Count > function.MaxArguments)
            {
                errors.Add(new TemplateError(call.Position, $"{function.Signature} gets {call.Arguments.Count} value(s)"));
                return;
            }
            if (string.Equals(call.Name, CounterScope, StringComparison.OrdinalIgnoreCase) && purpose != TemplatePurpose.FileName)
            {
                errors.Add(new TemplateError(call.Position, "'counter.scope(...)' is a counter, counters can only be used in file names"));
            }
        }

        private static void CheckFolderCounter(Template template, int index, VariableNode variable, TemplatePurpose purpose, List<TemplateError> errors)
        {
            var segment = template.Segments[index];
            if (purpose != TemplatePurpose.FileName) return;
            if (!(segment.Expression is VariableNode))
            {
                errors.Add(new TemplateError(variable.Position, $"'{variable.Name}' must be alone in its ${{...}}, e.g. ${{{variable.Name}:000}}"));
                return;
            }
            for (int i = index + 1; i < template.Segments.Count; i++)
            {
                var later = template.Segments[i];
                if (later.IsLiteral && later.Text.IndexOfAny(new[] { '\\', '/' }) >= 0)
                {
                    errors.Add(new TemplateError(variable.Position, $"'{variable.Name}' must be in the file name, after the last '\\'"));
                    return;
                }
            }
            bool fixedBefore = index == 0 || (template.Segments[index - 1].IsLiteral && template.Segments[index - 1].Text.Length > 0);
            bool fixedAfter = index == template.Segments.Count - 1 || (template.Segments[index + 1].IsLiteral && template.Segments[index + 1].Text.Length > 0);
            if (!fixedBefore && !fixedAfter)
            {
                errors.Add(new TemplateError(variable.Position, $"'{variable.Name}' needs fixed text (e.g. ' ', '-' or '_') next to it, else its digits can't be told apart from the other values; use counter.scope(...) there"));
            }
        }

        #endregion

        #region Rendering

        private static CultureInfo CultureOf(TemplatePurpose purpose)
        {
            switch (purpose)
            {
                case TemplatePurpose.Folder:
                case TemplatePurpose.FileName:
                    return CultureInfo.CurrentCulture;
                case TemplatePurpose.Text:
                    return Texts.CurrentCulture ?? CultureInfo.CurrentCulture;
                default:
                    return CultureInfo.InvariantCulture;
            }
        }

        private static bool MustPassValidation(TemplatePurpose purpose) =>
            purpose == TemplatePurpose.Folder || purpose == TemplatePurpose.FileName || purpose == TemplatePurpose.Command || purpose == TemplatePurpose.Argument;

        private Template Prepare(string text, TemplatePurpose purpose)
        {
            var template = Parse(text);
            var errors = Validate(text, purpose).Where(e => !e.IsWarning).ToList();
            if (errors.Count > 0)
            {
                if (MustPassValidation(purpose))
                {
                    throw new TemplateException(text, errors);
                }
                Log.WarnFormat("'{0}' has problems, missing values stay empty: {1}", text, string.Join("; ", errors));
            }
            return template;
        }

        public string Render(string text, TemplatePurpose purpose, VariableScope scope)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
            if (purpose == TemplatePurpose.FileName)
            {
                // Without an extension and as a preview: nothing is reserved or stored
                var preview = (scope ?? VariableScope.Empty).With();
                preview.Preview = true;
                return RenderFileName(text, null, preview).Name;
            }
            var template = Prepare(text, purpose);
            if (template.IsPlainText) return LiteralText(template);
            var eval = new Evaluation(this, scope ?? VariableScope.Empty, purpose);
            return RenderSegments(template.Segments, eval);
        }

        private static string LiteralText(Template template)
        {
            var sb = new StringBuilder();
            foreach (var segment in template.Segments) sb.Append(segment.Text);
            return sb.ToString();
        }

        public object Evaluate(string text, VariableScope scope, TemplatePurpose purpose = TemplatePurpose.Value)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var template = Prepare(text, purpose);
            if (template.IsPlainText)
            {
                string plain = LiteralText(template);
                return purpose == TemplatePurpose.Condition ? ToBoolean(plain) : plain;
            }
            var eval = new Evaluation(this, scope ?? VariableScope.Empty, purpose);
            object value = template.IsSingleExpression
                ? eval.Evaluate(template.Segments[0].Expression)
                : RenderSegments(template.Segments, eval);
            return purpose == TemplatePurpose.Condition ? ToBoolean(value) : value;
        }

        /// <summary>A template option (format "template") where it is used; options can't be used inside it</summary>
        internal string RenderOptionTemplate(string template, VariableScope scope)
        {
            if (_renderingOptionTemplate) return string.Empty;
            _renderingOptionTemplate = true;
            try
            {
                return Render(template, TemplatePurpose.Text, scope);
            }
            finally
            {
                _renderingOptionTemplate = false;
            }
        }

        public RenderedFileName RenderFileName(string text, string extension, VariableScope scope)
        {
            scope = (scope ?? VariableScope.Empty).With();
            scope.FixedNow ??= DateTime.Now;
            var template = Prepare(text ?? string.Empty, TemplatePurpose.FileName);
            var eval = new Evaluation(this, scope, TemplatePurpose.FileName);
            int counterIndex = FindFolderCounter(template);
            if (counterIndex >= 0)
            {
                eval.FolderCounter = NextFolderNumber(template, counterIndex, eval, extension);
            }
            // Values never hold a slash, so every slash left is a sub folder of the template
            string name = ToSeparators(RenderSegments(template.Segments, eval)) + (extension ?? string.Empty);
            string fullPath = null;
            if (!string.IsNullOrEmpty(scope.TargetFolder))
            {
                fullPath = Path.Combine(scope.TargetFolder, name);
                string root = Path.GetFullPath(scope.TargetFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!Path.GetFullPath(fullPath).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    throw new TemplateException(text, new[] { new TemplateError(0, $"'{name}' would leave the folder {scope.TargetFolder}") });
                }
            }
            long? global = eval.GlobalCounter;
            var scoped = eval.ScopedCounters.ToList();
            Action commit = null;
            if (!scope.Preview && (global.HasValue || scoped.Count > 0))
            {
                commit = () =>
                {
                    if (global.HasValue) _setGlobalCounter(global.Value);
                    foreach (var pair in scoped) _counterStore.Commit(pair.Key, pair.Value);
                };
            }
            return new RenderedFileName(name, fullPath, commit);
        }

        private static string RenderSegments(IReadOnlyList<TemplateSegment> segments, Evaluation eval)
        {
            var sb = new StringBuilder();
            foreach (var segment in segments)
            {
                sb.Append(segment.IsLiteral ? segment.Text : eval.SegmentText(segment));
            }
            return sb.ToString();
        }

        private static string ToSeparators(string path) =>
            path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

        /// <summary>Not allowed in a Windows file name; also left out on other systems, the files may be copied to Windows</summary>
        private static readonly char[] NotInFileNames = "<>:\"/\\|?*".ToCharArray();

        /// <summary>One value in a file name: one safe path part (no \ / : .. and the like), at most 80 characters</summary>
        internal static string FileNameSafe(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var chars = FilenameHelper.MakeFilenameSafe(value).ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] < ' ' || Array.IndexOf(NotInFileNames, chars[i]) >= 0) chars[i] = '_';
            }
            string safe = new string(chars).Trim();
            if (safe.Length > MaxFileNameValueLength) safe = safe.Substring(0, MaxFileNameValueLength);
            if (safe.Trim('.').Length == 0) safe = safe.Replace('.', '_');
            return safe;
        }

        private static int FindFolderCounter(Template template)
        {
            for (int i = 0; i < template.Segments.Count; i++)
            {
                if (template.Segments[i].Expression is VariableNode variable && IsFolderCounter(variable.Name)) return i;
            }
            return -1;
        }

        /// <summary>
        /// The next number for counter.folder or counter.name: the highest number of the files in the folder which fit
        /// the template, plus one. Fixed text must match, the counter is the digits, other values match anything
        /// (counter.folder) or must be the same (counter.name).
        /// </summary>
        private long NextFolderNumber(Template template, int counterIndex, Evaluation eval, string extension)
        {
            var counter = template.Segments[counterIndex];
            bool sameName = string.Equals(((VariableNode)counter.Expression).Name, CounterName, StringComparison.OrdinalIgnoreCase);

            // Where the file part starts: after the last \ or / of the fixed text
            int fileStartSegment = 0;
            int fileStartChar = 0;
            for (int i = 0; i < counterIndex; i++)
            {
                var segment = template.Segments[i];
                if (!segment.IsLiteral) continue;
                int slash = segment.Text.LastIndexOfAny(new[] { '\\', '/' });
                if (slash >= 0)
                {
                    fileStartSegment = i;
                    fileStartChar = slash + 1;
                }
            }

            var folder = new StringBuilder();
            var pattern = new StringBuilder("^");
            for (int i = 0; i < template.Segments.Count; i++)
            {
                var segment = template.Segments[i];
                bool inFile = i > fileStartSegment || (i == fileStartSegment && fileStartChar > 0) || (fileStartSegment == 0 && fileStartChar == 0);
                if (i < fileStartSegment || (i == fileStartSegment && fileStartChar > 0 && segment.IsLiteral))
                {
                    // The folder part, and the start of the literal holding the last slash
                    string text = segment.IsLiteral ? segment.Text : eval.SegmentText(segment);
                    if (i == fileStartSegment)
                    {
                        folder.Append(text, 0, fileStartChar);
                        pattern.Append(Regex.Escape(text.Substring(fileStartChar)));
                    }
                    else
                    {
                        folder.Append(text);
                    }
                    continue;
                }
                if (!inFile) continue;
                if (i == counterIndex)
                {
                    int width = ZeroWidth(counter.Format);
                    pattern.Append(width > 0 ? $"(\\d{{{width}}})" : "(\\d+)");
                }
                else if (segment.IsLiteral)
                {
                    pattern.Append(Regex.Escape(segment.Text));
                }
                else
                {
                    pattern.Append(sameName ? Regex.Escape(eval.SegmentText(segment)) : ".*?");
                }
            }
            pattern.Append(extension == null ? "(\\.[^.\\\\/]*)?" : Regex.Escape(extension)).Append('$');

            string directory = string.IsNullOrEmpty(eval.Scope.TargetFolder) ? null : Path.Combine(eval.Scope.TargetFolder, ToSeparators(folder.ToString()));
            long highest = 0;
            if (directory != null)
            {
                try
                {
                    if (Directory.Exists(directory))
                    {
                        var regex = new Regex(pattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                        foreach (string file in Directory.EnumerateFiles(directory))
                        {
                            var match = regex.Match(Path.GetFileName(file));
                            if (match.Success && long.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out long number))
                            {
                                highest = Math.Max(highest, number);
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is RegexMatchTimeoutException || ex is ArgumentException)
                {
                    Log.WarnFormat("Couldn't read the numbers in {0}: {1}", directory, ex.Message);
                }
            }
            return highest + 1;
        }

        /// <summary>"000" is 3 digits, other formats any number of digits</summary>
        private static int ZeroWidth(string format)
        {
            if (string.IsNullOrEmpty(format) || format.Any(c => c != '0')) return 0;
            return format.Length;
        }

        #endregion

        /// <summary>
        /// One evaluation: the scope, the purpose and the counter values, so a counter used twice gives one number.
        /// </summary>
        private sealed class Evaluation
        {
            private readonly VariableResolver _resolver;
            private readonly TemplatePurpose _purpose;
            private readonly CultureInfo _culture;

            public Evaluation(VariableResolver resolver, VariableScope scope, TemplatePurpose purpose)
            {
                _resolver = resolver;
                Scope = scope;
                _purpose = purpose;
                _culture = CultureOf(purpose);
            }

            public VariableScope Scope { get; }
            public long? GlobalCounter { get; private set; }
            public long? FolderCounter { get; set; }
            public Dictionary<string, long> ScopedCounters { get; } = new Dictionary<string, long>(StringComparer.Ordinal);

            public string SegmentText(TemplateSegment segment)
            {
                object value = Evaluate(segment.Expression);
                string text = ToText(value, segment.Format, _culture);
                return _purpose == TemplatePurpose.FileName ? FileNameSafe(text) : text;
            }

            public object Evaluate(ExpressionNode node)
            {
                switch (node)
                {
                    case LiteralNode literal:
                        return literal.Value;
                    case VariableNode variable:
                        return Lookup(variable.Name);
                    case UnaryNode unary:
                        object operand = Evaluate(unary.Operand);
                        return unary.Operator == "!" ? (object)!ToBoolean(operand) : Normalize(-ToNumber(operand));
                    case ConditionalNode conditional:
                        return ToBoolean(Evaluate(conditional.Condition)) ? Evaluate(conditional.WhenTrue) : Evaluate(conditional.WhenFalse);
                    case BinaryNode binary:
                        return Binary(binary);
                    case CallNode call:
                        return Call(call);
                    default:
                        return null;
                }
            }

            private object Binary(BinaryNode node)
            {
                switch (node.Operator)
                {
                    case "&&":
                        return ToBoolean(Evaluate(node.Left)) && ToBoolean(Evaluate(node.Right));
                    case "||":
                        return ToBoolean(Evaluate(node.Left)) || ToBoolean(Evaluate(node.Right));
                    case "??":
                        object first = Evaluate(node.Left);
                        return IsMissing(first) ? Evaluate(node.Right) : first;
                }
                object left = Evaluate(node.Left);
                object right = Evaluate(node.Right);
                switch (node.Operator)
                {
                    case "==": return AreEqual(left, right);
                    case "!=": return !AreEqual(left, right);
                    case "<": return Compare(left, right) < 0;
                    case "<=": return Compare(left, right) <= 0;
                    case ">": return Compare(left, right) > 0;
                    case ">=": return Compare(left, right) >= 0;
                    case "+":
                        if (left is string || right is string || left is DateTime || right is DateTime)
                        {
                            return ToText(left, null, _culture) + ToText(right, null, _culture);
                        }
                        return Normalize(ToNumber(left) + ToNumber(right));
                    case "-": return Normalize(ToNumber(left) - ToNumber(right));
                    case "*": return Normalize(ToNumber(left) * ToNumber(right));
                    case "/":
                        double divisor = ToNumber(right);
                        return divisor == 0 ? null : Normalize(ToNumber(left) / divisor);
                    case "%":
                        double modulus = ToNumber(right);
                        return modulus == 0 ? null : Normalize(ToNumber(left) % modulus);
                }
                return null;
            }

            private object Call(CallNode call)
            {
                if (!_resolver._functions.TryGetValue(call.Name, out var function)) return null;
                var arguments = call.Arguments.Select(Evaluate).ToList();
                if (string.Equals(call.Name, CounterScope, StringComparison.OrdinalIgnoreCase))
                {
                    string key = CounterStore.NormalizeKey(ToText(arguments.FirstOrDefault(), null, CultureInfo.InvariantCulture));
                    if (!ScopedCounters.TryGetValue(key, out long next))
                    {
                        next = _resolver._counterStore.Peek(key) + 1;
                        ScopedCounters[key] = next;
                    }
                    return next;
                }
                try
                {
                    return function.Body(arguments);
                }
                catch (Exception ex) when (ex is RegexMatchTimeoutException || ex is ArgumentException || ex is FormatException || ex is OverflowException)
                {
                    Log.WarnFormat("{0} failed: {1}", function.Signature, ex.Message);
                    return null;
                }
            }

            private object Lookup(string name)
            {
                if (Scope.Values != null && Scope.Values.TryGetValue(name, out var given)) return given;
                if (string.Equals(name, CounterGlobal, StringComparison.OrdinalIgnoreCase))
                {
                    GlobalCounter ??= _resolver._getGlobalCounter() + 1;
                    return GlobalCounter.Value;
                }
                if (IsFolderCounter(name))
                {
                    return FolderCounter ?? 1L;
                }
                if (_renderingOptionTemplate && name.StartsWith("option.", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                int dot = name.IndexOf('.');
                if (dot > 0 && _resolver._providers.TryGetValue(name.Substring(0, dot), out var provider))
                {
                    try
                    {
                        if (provider.TryGetValue(name, Scope, out var value)) return value;
                    }
                    catch (Exception ex) when (!(ex is OutOfMemoryException))
                    {
                        Log.WarnFormat("The value of {0} couldn't be read: {1}", name, ex.Message);
                    }
                }
                return null;
            }
        }
    }
}
