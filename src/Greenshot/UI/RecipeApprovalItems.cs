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
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Recipes;

namespace Greenshot.UI
{
    /// <summary>
    /// What the approval window shows: the recipe, the exact content that is approved, and where it came from.
    /// </summary>
    public sealed class RecipeApprovalRequest
    {
        public CaptureRecipe Recipe { get; set; }

        /// <summary>
        /// The file the recipe is (or will be) saved in
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The content that was read (or will be written) once: shown, parsed and hashed, so the approval is for exactly this
        /// </summary>
        public string Content { get; set; }

        public string ContentHash { get; set; }

        public RecipeValidationResult Validation { get; set; }

        /// <summary>
        /// The trust record of the file, for an earlier approval
        /// </summary>
        public RecipeTrustRecord PreviousRecord { get; set; }

        /// <summary>
        /// The AI tool which proposed the recipe, null when the user imported or saved it
        /// </summary>
        public string ProposedByName { get; set; }

        public string ProposedByPath { get; set; }

        public string ProposedBySigner { get; set; }

        /// <summary>
        /// What the AI tool says the user asked for (the AI's words)
        /// </summary>
        public string AiRequest { get; set; }

        /// <summary>
        /// The AI tool's explanation of the recipe (the AI's words)
        /// </summary>
        public string AiExplanation { get; set; }

        /// <summary>
        /// The recipe this one replaces (a built-in recipe or the current version), to show the changes
        /// </summary>
        public CaptureRecipe ReplacedRecipe { get; set; }

        /// <summary>
        /// The JSON of the replaced recipe or of the previously approved file, for the diff
        /// </summary>
        public string PreviousContent { get; set; }

        public bool ReplacesBuiltIn { get; set; }

        /// <summary>
        /// Start with every trigger and gated action switched off (AI proposals, and changed files an AI tool created)
        /// </summary>
        public bool StartSwitchedOff { get; set; }

        /// <summary>
        /// The user saved the recipe in Greenshot's recipe editor: the reasons why the change needs a decision, null otherwise
        /// </summary>
        public IReadOnlyList<string> OwnEditReasons { get; set; }

        /// <summary>
        /// For an own edit: the switches as they will be (unchanged triggers keep theirs, the new ones the user added are on)
        /// </summary>
        public RecipeApproval SuggestedApproval { get; set; }

        /// <summary>
        /// Only show the recipe and its approval (details): nothing can be switched, the only button is Close
        /// </summary>
        public bool IsReadOnly { get; set; }

        public bool IsOwnEdit => OwnEditReasons != null;

        public bool IsAiProposal => !string.IsNullOrEmpty(ProposedByName);
    }

    /// <summary>
    /// A line of "What it does"
    /// </summary>
    public sealed class StepLineModel
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public Thickness Margin { get; set; }
        public Brush Foreground { get; set; }
        public FontWeight FontWeight { get; set; } = FontWeights.Normal;
    }

    /// <summary>
    /// A switch in the approval window (a trigger or a kind of gated action)
    /// </summary>
    public abstract class ApprovalSwitch : INotifyPropertyChanged
    {
        private bool _isChecked;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OffVisibility)));
            }
        }

        public string Title { get; set; }

        /// <summary>
        /// Why it matters, empty when it doesn't
        /// </summary>
        public string Explanation { get; set; }

        public Visibility ExplanationVisibility => string.IsNullOrEmpty(Explanation) ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// Shown while it is switched off
        /// </summary>
        public Visibility OffVisibility => _isChecked ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// A trigger the user can switch on or off
    /// </summary>
    public sealed class TriggerApprovalItem : ApprovalSwitch
    {
        /// <summary>
        /// The trigger is switched off in the recipe itself: leaving it off here changes nothing
        /// </summary>
        public bool IsDisabledInRecipe { get; set; }

        public string Key { get; set; }
    }

    /// <summary>
    /// A kind of gated action (external commands, network, file system) the user has to allow
    /// </summary>
    public sealed class GateApprovalItem : ApprovalSwitch
    {
        public RecipeGateType GateType { get; set; }

        public List<string> Targets { get; } = new List<string>();

        public string TargetsText => string.Join("\n", Targets);
    }
}
