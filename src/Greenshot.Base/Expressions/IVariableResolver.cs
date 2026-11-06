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
    /// The single place which fills in ${...}: core, recipes, plugins and the settings UI use it, nothing else replaces
    /// variables. Get it with <see cref="VariableResolver.Instance"/> or from the service provider.
    /// </summary>
    public interface IVariableResolver
    {
        /// <summary>Parse a text (cached)</summary>
        Template Parse(string text);

        /// <summary>
        /// Check a text for a purpose: syntax, unknown names and functions, values that may not be used there
        /// (e.g. a window title choosing a folder), folder counters without a folder. Warnings are included.
        /// </summary>
        IReadOnlyList<TemplateError> Validate(string text, TemplatePurpose purpose);

        /// <summary>
        /// Fill in the text. Values are escaped for the purpose; the result is never evaluated again.
        /// Throws <see cref="TemplateException"/> when <see cref="Validate"/> has errors.
        /// Counters change only through <see cref="RenderFileName"/> and its commit.
        /// </summary>
        string Render(string text, TemplatePurpose purpose, VariableScope scope);

        /// <summary>
        /// Fill in a file name (purpose <see cref="TemplatePurpose.FileName"/>), with the extension.
        /// Folder counters read <see cref="VariableScope.TargetFolder"/> (nothing is reserved: two saves at the same moment
        /// can get the same number). Call <see cref="RenderedFileName.Commit"/> after the file was written (or uploaded):
        /// then the stored counters go up; without a commit nothing changes.
        /// </summary>
        RenderedFileName RenderFileName(string text, string extension, VariableScope scope);

        /// <summary>
        /// The value of a text: a single ${...} keeps its type (number, boolean, list), anything else is a text.
        /// Used for recipe parameters and conditions.
        /// </summary>
        object Evaluate(string text, VariableScope scope, TemplatePurpose purpose = TemplatePurpose.Value);

        /// <summary>All variables, whatever they can be used for (the docs)</summary>
        IReadOnlyList<VariableDescriptor> DescribeAll();

        /// <summary>The variables a purpose can use, for help and completion</summary>
        IReadOnlyList<VariableDescriptor> Describe(TemplatePurpose purpose);

        /// <summary>The functions, for help and completion</summary>
        IReadOnlyList<ExpressionFunction> Functions { get; }
    }

    /// <summary>
    /// Plugins add their own variables and functions here, at start.
    /// </summary>
    public interface IVariableRegistry
    {
        /// <summary>Add a provider; its namespace may not be taken yet</summary>
        void Register(IVariableProvider provider);

        /// <summary>Add a function; it must be pure (no side effects), its name should carry the plugin's prefix</summary>
        void Register(ExpressionFunction function);
    }

    /// <summary>
    /// The values of one namespace, e.g. "payload" or "jira".
    /// </summary>
    public interface IVariableProvider
    {
        /// <summary>The first part of the names, without the dot: "payload"</summary>
        string Namespace { get; }

        /// <summary>The variables, with their full names ("payload.title")</summary>
        IEnumerable<VariableDescriptor> Variables { get; }

        /// <summary>
        /// The value of a name (full name, e.g. "payload.title"). False when this provider doesn't know the name.
        /// A known name without a value right now (no session, no title) returns true with null.
        /// </summary>
        bool TryGetValue(string name, VariableScope scope, out object value);
    }

    /// <summary>
    /// A function usable in expressions.
    /// </summary>
    public sealed class ExpressionFunction
    {
        public ExpressionFunction(string name, int minArguments, int maxArguments, string signature, string description, Func<IReadOnlyList<object>, object> body)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MinArguments = minArguments;
            MaxArguments = maxArguments;
            Signature = signature;
            Description = description;
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public string Name { get; }
        public int MinArguments { get; }
        public int MaxArguments { get; }

        /// <summary>For the help: "left(text, count)"</summary>
        public string Signature { get; }

        public string Description { get; }

        /// <summary>Gets the evaluated arguments</summary>
        public Func<IReadOnlyList<object>, object> Body { get; }
    }
}
