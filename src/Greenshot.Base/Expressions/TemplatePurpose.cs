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

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// What a text with ${...} is used for. The purpose decides which values may be used, how they are escaped and in
    /// which culture they are formatted, see docs/vNext/expression-engine.md
    /// </summary>
    public enum TemplatePurpose
    {
        /// <summary>A folder (output folder, recipe SaveDirectory): only trusted values, the result is a full path</summary>
        Folder,

        /// <summary>A file name, may hold "\" for sub folders: each value becomes one safe path part</summary>
        FileName,

        /// <summary>The program of a command line: only trusted values</summary>
        Command,

        /// <summary>The arguments or working folder of a command line: only trusted values</summary>
        Argument,

        /// <summary>A text for people (footer, e-mail subject, caption, clipboard text): all values, UI language</summary>
        Text,

        /// <summary>A condition (when, enabled, conditional branches): the result is a boolean</summary>
        Condition,

        /// <summary>A recipe parameter value: a single expression keeps its type (number, boolean, ...)</summary>
        Value
    }

    /// <summary>
    /// Whether a value may choose a folder or a program.
    /// </summary>
    public enum VariableTrust
    {
        /// <summary>Set by the user, Greenshot or Windows</summary>
        Trusted,

        /// <summary>Comes from a capture or from outside: window title, OCR text, file names, typed or detected work items</summary>
        Untrusted
    }

    /// <summary>
    /// The description of one variable, for checking, help, completion and the docs.
    /// </summary>
    public sealed class VariableDescriptor
    {
        public VariableDescriptor(string name, Type type, VariableTrust trust, string description)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? typeof(string);
            Trust = trust;
            Description = description;
        }

        /// <summary>Full name, e.g. "payload.title". A name ending in ".*" stands for any name below it, e.g. "env.*"</summary>
        public string Name { get; }

        /// <summary>The type of the value (string, int, DateTime, bool, ...)</summary>
        public Type Type { get; }

        public VariableTrust Trust { get; }

        /// <summary>English description, used in the help when no translation exists</summary>
        public string Description { get; }

        /// <summary>Only Windows has this value; elsewhere it is empty</summary>
        public bool WindowsOnly { get; set; }

        /// <summary>Only exists while a file is written, e.g. the folder counters (not for uploads)</summary>
        public bool NeedsTargetFolder { get; set; }

        /// <summary>Changes state when used outside a preview (counters)</summary>
        public bool IsCounter { get; set; }

        /// <summary>Is <paramref name="fullName"/> this variable, or below it for a ".*" descriptor</summary>
        public bool Matches(string fullName)
        {
            if (Name.EndsWith(".*", StringComparison.Ordinal))
            {
                string prefix = Name.Substring(0, Name.Length - 1);
                return fullName.Length > prefix.Length && fullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }
            return string.Equals(Name, fullName, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// A problem found when checking a text, with the position in the text.
    /// </summary>
    public sealed class TemplateError
    {
        public TemplateError(int position, string message)
        {
            Position = position;
            Message = message;
        }

        /// <summary>The position in the text, 0 based</summary>
        public int Position { get; }

        public string Message { get; }

        /// <summary>Only a warning: the text still works (e.g. a Windows-only value)</summary>
        public bool IsWarning { get; set; }

        public override string ToString() => $"{Message} (at {Position + 1})";
    }

    /// <summary>
    /// Thrown when a text is used which doesn't pass the check, e.g. a window title choosing a folder.
    /// </summary>
    public sealed class TemplateException : Exception
    {
        public TemplateException(string text, IReadOnlyList<TemplateError> errors)
            : base($"'{text}': {string.Join("; ", errors)}")
        {
            Errors = errors;
        }

        public IReadOnlyList<TemplateError> Errors { get; }
    }
}
