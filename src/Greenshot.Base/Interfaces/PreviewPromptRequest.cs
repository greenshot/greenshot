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

using System.Collections.Generic;
using System.Drawing;
using Greenshot.Base.Recipes;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// A question about the capture with a preview of it, choices and an optional "don't ask again" check box, for flow steps and
    /// destinations (ShowDialogAsync). The view is registered by Greenshot; returns null when the user closed it otherwise.
    /// </summary>
    public sealed class PreviewPromptRequest : IDialogViewModel<PreviewPromptResult>
    {
        public PreviewPromptRequest(string title, string message, Image preview, IReadOnlyList<PromptChoice> choices, string checkBoxText = null)
        {
            Title = title;
            Message = message;
            Preview = preview;
            Choices = choices ?? new List<PromptChoice>();
            CheckBoxText = checkBoxText;
        }

        public string Title { get; }

        public string Message { get; }

        /// <summary>
        /// The capture as it will be exported, borrowed; null: no preview
        /// </summary>
        public Image Preview { get; }

        /// <summary>
        /// The buttons, the one with IsDefault reacts to Enter, the one with IsCancel to Escape and closing the window
        /// </summary>
        public IReadOnlyList<PromptChoice> Choices { get; }

        /// <summary>
        /// The text of the check box, null: no check box
        /// </summary>
        public string CheckBoxText { get; }
    }

    /// <summary>
    /// The answer to a <see cref="PreviewPromptRequest"/>
    /// </summary>
    public sealed class PreviewPromptResult
    {
        public PreviewPromptResult(string choiceKey, bool isChecked)
        {
            ChoiceKey = choiceKey;
            IsChecked = isChecked;
        }

        /// <summary>
        /// The key of the chosen choice
        /// </summary>
        public string ChoiceKey { get; }

        /// <summary>
        /// The state of the check box, false without one
        /// </summary>
        public bool IsChecked { get; }
    }
}
