/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.Recipes;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Evaluates whether a conditional pipeline step should execute.
    /// </summary>
    public interface IStepCondition
    {
        /// <summary>
        /// Human-readable description of the condition.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Evaluates the condition against the current pipeline context.
        /// </summary>
        bool Evaluate(CaptureFlowContext context);
    }

    /// <summary>
    /// Evaluates a predicate function.
    /// </summary>
    public class PredicateCondition : IStepCondition
    {
        private readonly Func<CaptureFlowContext, bool> _predicate;
        public string Description { get; }

        public PredicateCondition(string description, Func<CaptureFlowContext, bool> predicate)
        {
            Description = description ?? "Custom predicate";
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public bool Evaluate(CaptureFlowContext context) => _predicate(context);
    }

    /// <summary>
    /// Checks if the recipe source type matches an expected type.
    /// </summary>
    public class SourceTypeCondition : IStepCondition
    {
        public CaptureSourceType ExpectedSourceType { get; }
        public string Description => $"Source is {ExpectedSourceType}";

        public SourceTypeCondition(CaptureSourceType expectedSourceType)
        {
            ExpectedSourceType = expectedSourceType;
        }

        public bool Evaluate(CaptureFlowContext context)
        {
            var sourceStep = context.Recipe?.FindStep(WellKnownStepTypes.Source);
            if (sourceStep != null)
            {
                var st = sourceStep.GetParameter<CaptureSourceType>("SourceType");
                return st == ExpectedSourceType;
            }
            return false;
        }
    }

    /// <summary>
    /// Checks if the extracted OCR or text payload contains a given substring.
    /// </summary>
    public class TextContainsCondition : IStepCondition
    {
        public string Substring { get; }
        public StringComparison Comparison { get; }
        public string Description => $"Text contains '{Substring}'";

        public TextContainsCondition(string substring, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            Substring = substring;
            Comparison = comparison;
        }

        public bool Evaluate(CaptureFlowContext context)
        {
            var text = context.Payload?.ExtractedText;
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(Substring)) return false;
            return text.IndexOf(Substring, Comparison) >= 0;
        }
    }
}
