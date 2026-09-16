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

using System.Collections.Generic;
using Greenshot.Base.Pipeline;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// Interface for evaluating dynamic expressions, interpolating tokens,
    /// and resolving variables from environment (user / machine), configuration, and context.
    /// </summary>
    public interface IExpressionEvaluator
    {
        /// <summary>
        /// Evaluates a single value or expression string in the given context.
        /// If the input is enclosed in "${...}", it evaluates the expression inside.
        /// If the input contains multiple "${...}" tokens, it performs string interpolation.
        /// </summary>
        /// <param name="expressionOrTemplate">Expression or string template</param>
        /// <param name="context">Active capture flow context</param>
        /// <param name="extraVariables">Optional node-local or temporary variables</param>
        /// <returns>Evaluated result object</returns>
        object Evaluate(string expressionOrTemplate, CaptureFlowContext context, IDictionary<string, object> extraVariables = null);

        /// <summary>
        /// Evaluates an expression or template and converts the result to type T.
        /// </summary>
        T Evaluate<T>(string expressionOrTemplate, CaptureFlowContext context, T defaultValue = default, IDictionary<string, object> extraVariables = null);

        /// <summary>
        /// Resolves all string values in a parameter dictionary recursively, replacing expressions.
        /// </summary>
        Dictionary<string, object> ResolveParameters(IDictionary<string, object> parameters, CaptureFlowContext context, IDictionary<string, object> extraVariables = null);
    }
}
