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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Recipes;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;

namespace Greenshot.UI
{

    /// <summary>
    /// Modern WPF dialog for reviewing and approving external capture recipes with dark/light mode support.
    /// </summary>
    public partial class RecipeApprovalWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// What the user approved, and whether to open the recipe editor afterwards
        /// </summary>
        public sealed class ApprovalResult
        {
            public ApprovalResult(RecipeApproval approval, bool openInEditor)
            {
                Approval = approval;
                OpenInEditor = openInEditor;
            }

            public RecipeApproval Approval { get; }

            public bool OpenInEditor { get; }

            /// <summary>
            /// The user revoked an earlier approval in the review
            /// </summary>
            public bool IsRevoked { get; private set; }

            public static ApprovalResult Revoked => new ApprovalResult(null, false) { IsRevoked = true };
        }

        /// <summary>
        /// The user revoked the approval in the review of an approved recipe
        /// </summary>
        public bool IsRevoked { get; private set; }

        public string RejectButtonText { get; private set; } = "Reject / Block";

        private readonly CaptureRecipe _recipe;
        private readonly string _content;
        private readonly string _diffText;
        private bool _isJsonViewerVisible;
        private string _recipeJsonContent;
        private string _overlayTitle = "Recipe JSON Definition";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string RecipeName { get; set; }
        public string RecipeVersion { get; set; }
        public string RecipeDescription { get; set; }
        public string RecipeId { get; set; }
        public string FilePath { get; set; }
        public string FileHash { get; set; }

        public bool IsJsonViewerVisible
        {
            get => _isJsonViewerVisible;
            set
            {
                if (_isJsonViewerVisible != value)
                {
                    _isJsonViewerVisible = value;
                    OnPropertyChanged(nameof(IsJsonViewerVisible));
                    OnPropertyChanged(nameof(JsonViewerVisibility));
                }
            }
        }

        public Visibility JsonViewerVisibility => _isJsonViewerVisible ? Visibility.Visible : Visibility.Collapsed;

        public string RecipeJsonContent
        {
            get => _recipeJsonContent;
            set
            {
                if (_recipeJsonContent != value)
                {
                    _recipeJsonContent = value;
                    OnPropertyChanged(nameof(RecipeJsonContent));
                }
            }
        }

        /// <summary>
        /// Title of the overlay: the JSON or the changes
        /// </summary>
        public string OverlayTitle
        {
            get => _overlayTitle;
            set
            {
                if (_overlayTitle != value)
                {
                    _overlayTitle = value;
                    OnPropertyChanged(nameof(OverlayTitle));
                }
            }
        }

        // An AI tool proposed the recipe
        public bool IsAiProposal { get; private set; }
        public Visibility AiProposalVisibility => IsAiProposal ? Visibility.Visible : Visibility.Collapsed;
        public string ProposedByText { get; private set; }
        public string ProposedBySignerText { get; private set; }
        public string AiRequest { get; private set; }
        public string AiExplanation { get; private set; }
        public Visibility AiRequestVisibility => string.IsNullOrWhiteSpace(AiRequest) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility AiExplanationVisibility => string.IsNullOrWhiteSpace(AiExplanation) ? Visibility.Collapsed : Visibility.Visible;

        // The recipe replaces a built-in recipe or an existing version, or the file changed: what is different
        public string ChangesTitle { get; private set; }
        public ObservableCollection<string> ChangeDescriptions { get; } = new ObservableCollection<string>();
        public Visibility ChangesVisibility => string.IsNullOrEmpty(ChangesTitle) || IsValidationError ? Visibility.Collapsed : Visibility.Visible;
        public Visibility DiffButtonVisibility => string.IsNullOrEmpty(_diffText) ? Visibility.Collapsed : Visibility.Visible;

        public RecipeApprovalMode ApprovalMode { get; private set; }
        public bool IsModified => ApprovalMode == RecipeApprovalMode.Modified;
        public bool IsNewRecipe => ApprovalMode == RecipeApprovalMode.NewRecipe;
        public bool IsValidationError => ApprovalMode == RecipeApprovalMode.ValidationError;

        public Visibility ValidationErrorVisibility => IsValidationError ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ApprovalActionsVisibility => !IsValidationError ? Visibility.Visible : Visibility.Collapsed;

        public string WindowTitleSubtitle { get; private set; }
        public string WindowFullTitle => $"Greenshot{WindowTitleSubtitle}";
        public string HeaderIcon { get; private set; }
        public string HeaderTitle { get; private set; }
        public string HeaderDescription { get; private set; }
        public string StatusBadgeText { get; private set; }
        public SolidColorBrush StatusBadgeBackgroundBrush { get; private set; }
        public SolidColorBrush StatusBadgeBorderBrush { get; private set; }
        public SolidColorBrush StatusBadgeForegroundBrush { get; private set; }
        public string PreviousApprovalDate { get; private set; }
        public string PreviousFileHash { get; private set; }
        public string ApproveButtonText { get; private set; }
        public Visibility ModificationWarningVisibility => IsModified ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WarningIconVisibility => IsModified ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShieldIconVisibility => (!IsModified && !IsValidationError) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ErrorIconVisibility => IsValidationError ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<RecipeValidationErrorItem> ValidationErrors { get; } = new ObservableCollection<RecipeValidationErrorItem>();
        public ObservableCollection<TriggerBadgeModel> TriggerBadges { get; } = new ObservableCollection<TriggerBadgeModel>();
        public ObservableCollection<TriggerApprovalItem> TriggerItems { get; } = new ObservableCollection<TriggerApprovalItem>();
        public ObservableCollection<StepLineModel> StepDescriptions { get; } = new ObservableCollection<StepLineModel>();
        public ObservableCollection<GateApprovalItem> GateItems { get; } = new ObservableCollection<GateApprovalItem>();

        public Visibility TriggersVisibility => TriggerBadges.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TriggerItemsVisibility => TriggerItems.Count > 0 && !IsValidationError ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TriggerBadgesVisibility => TriggerItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PipelineVisibility => StepDescriptions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ExternalCommandWarningVisibility => (HasExternalCommands && !IsValidationError) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility OpenInEditorVisibility => IsAiProposal && !IsValidationError ? Visibility.Visible : Visibility.Collapsed;
        public bool HasExternalCommands { get; set; }
        public string TriggerHint { get; private set; }

        /// <summary>
        /// The hint is a warning when the triggers start switched off, it is easy to overlook
        /// </summary>
        public SolidColorBrush TriggerHintBrush { get; private set; }

        public bool IsApproved { get; private set; }

        /// <summary>
        /// What the user approved, set when the window closes with Approve
        /// </summary>
        public RecipeApproval Approval { get; private set; }

        /// <summary>
        /// The user wants to see the recipe in the recipe editor after saving it
        /// </summary>
        public bool OpenInEditor { get; private set; }

        // Theme brushes for WPF binding
        public SolidColorBrush WindowBackgroundBrush => WpfThemeHelper.WindowBackground;
        public SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public SolidColorBrush AccentBrush => WpfThemeHelper.Accent;
        public SolidColorBrush WarningBackgroundBrush => WpfThemeHelper.WarningBackground;
        public SolidColorBrush WarningBorderBrush => WpfThemeHelper.WarningBorder;
        public SolidColorBrush WarningTextBrush => WpfThemeHelper.WarningText;
        public SolidColorBrush ErrorBackgroundBrush => WpfThemeHelper.ErrorBackground;
        public SolidColorBrush ErrorBorderBrush => WpfThemeHelper.ErrorBorder;
        public SolidColorBrush ErrorTextBrush => WpfThemeHelper.ErrorText;
        public SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;

        public RecipeApprovalWindow(RecipeApprovalRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var recipe = request.Recipe;
            var validationResult = request.Validation;
            string filePath = request.FilePath;
            _recipe = recipe;
            _content = request.Content;
            InitializeComponent();

            RecipeName = recipe?.Name ?? (File.Exists(filePath) ? Path.GetFileName(filePath) : "Unnamed Recipe");
            RecipeVersion = string.IsNullOrWhiteSpace(recipe?.Version) ? "" : $"v{recipe.Version}";
            RecipeDescription = string.IsNullOrWhiteSpace(recipe?.Description) ? (validationResult != null && !validationResult.IsValid ? "Recipe configuration failed validation." : "No description provided.") : recipe.Description;
            RecipeId = recipe?.Id ?? "unknown";
            FilePath = filePath ?? "Unknown file path";
            // The hash of the content that was read once, never of the file as it is now
            FileHash = request.ContentHash ?? "Unknown";

            IsAiProposal = request.IsAiProposal;
            if (IsAiProposal)
            {
                ProposedByText = string.IsNullOrWhiteSpace(request.ProposedByPath) ? request.ProposedByName : $"{request.ProposedByName} ({request.ProposedByPath})";
                ProposedBySignerText = string.IsNullOrWhiteSpace(request.ProposedBySigner) ? "Not signed" : $"Signed by {request.ProposedBySigner}";
                AiRequest = request.AiRequest;
                AiExplanation = request.AiExplanation;
            }

            var prev = request.PreviousRecord;
            if (validationResult != null && !validationResult.IsValid)
            {
                ApprovalMode = RecipeApprovalMode.ValidationError;
                WindowTitleSubtitle = " — Recipe Validation Failed";
                HeaderIcon = "❌";
                HeaderTitle = "Recipe Validation Failed";
                HeaderDescription = "Greenshot could not load or register this capture recipe because it contains configuration errors. Review the diagnostic details below:";
                StatusBadgeText = "VALIDATION ERROR";
                StatusBadgeBackgroundBrush = ErrorBackgroundBrush;
                StatusBadgeBorderBrush = ErrorBorderBrush;
                StatusBadgeForegroundBrush = ErrorTextBrush;
                ApproveButtonText = "Close";

                foreach (var err in validationResult.Errors)
                {
                    ValidationErrors.Add(RecipeValidationErrorItem.Create(err));
                }
            }
            else if (IsAiProposal)
            {
                ApprovalMode = RecipeApprovalMode.NewRecipe;
                WindowTitleSubtitle = " — Recipe Proposed by an AI Tool";
                HeaderIcon = "🤖";
                HeaderTitle = request.ReplacedRecipe != null ? "An AI Tool Wants to Change a Recipe" : "An AI Tool Proposes a Recipe";
                HeaderDescription = "This recipe was written by an AI tool, not by Greenshot or by you. Check what it does below (Greenshot describes it from the recipe itself) and switch on only the triggers you want.";
                StatusBadgeText = "PROPOSED BY AI";
                StatusBadgeBackgroundBrush = WarningBackgroundBrush;
                StatusBadgeBorderBrush = WarningBorderBrush;
                StatusBadgeForegroundBrush = WarningTextBrush;
                ApproveButtonText = "Save Recipe";
            }
            else if (request.IsOwnEdit)
            {
                ApprovalMode = prev != null ? RecipeApprovalMode.Modified : RecipeApprovalMode.NewRecipe;
                WindowTitleSubtitle = " — Approve Your Changes";
                HeaderIcon = "🛡️";
                HeaderTitle = "Your Change Needs a Decision";
                HeaderDescription = "You saved this recipe in the recipe editor. Most changes are approved without asking, but this one adds something that needs your decision: " +
                                    string.Join(" ", request.OwnEditReasons);
                StatusBadgeText = "YOUR CHANGE";
                StatusBadgeBackgroundBrush = BadgeBackgroundBrush;
                StatusBadgeBorderBrush = CardBorderBrush;
                StatusBadgeForegroundBrush = AccentBrush;
                if (prev != null)
                {
                    PreviousApprovalDate = prev.ApprovedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                    PreviousFileHash = prev.Sha256Hash;
                }
                ApproveButtonText = "Approve & Save";
            }
            else if (prev != null && !string.Equals(prev.Sha256Hash, FileHash, StringComparison.OrdinalIgnoreCase))
            {
                ApprovalMode = RecipeApprovalMode.Modified;
                WindowTitleSubtitle = " — Recipe Modification Detected";
                HeaderIcon = "⚠️";
                HeaderTitle = "Recipe Changed Outside Greenshot";
                HeaderDescription = "You approved this capture recipe before, but its file was changed outside Greenshot since then (by another program or by hand). Review the changes below before approving them.";
                StatusBadgeText = "MODIFIED ON DISK";
                StatusBadgeBackgroundBrush = WarningBackgroundBrush;
                StatusBadgeBorderBrush = WarningBorderBrush;
                StatusBadgeForegroundBrush = WarningTextBrush;
                PreviousApprovalDate = prev.ApprovedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                PreviousFileHash = prev.Sha256Hash;
                ApproveButtonText = "Approve Changes";
            }
            else if (prev != null)
            {
                ApprovalMode = RecipeApprovalMode.ReVerify;
                RejectButtonText = "Revoke Approval";
                WindowTitleSubtitle = " — Recipe Security Review";
                HeaderIcon = "🛡️";
                HeaderTitle = "Capture Recipe Review";
                HeaderDescription = "Reviewing registration, triggers, and execution permissions for this capture recipe.";
                StatusBadgeText = "ALREADY APPROVED";
                StatusBadgeBackgroundBrush = BadgeBackgroundBrush;
                StatusBadgeBorderBrush = CardBorderBrush;
                StatusBadgeForegroundBrush = TextSecondaryBrush;
                PreviousApprovalDate = prev.ApprovedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                PreviousFileHash = prev.Sha256Hash;
                ApproveButtonText = "Confirm & Enable";
            }
            else
            {
                ApprovalMode = RecipeApprovalMode.NewRecipe;
                WindowTitleSubtitle = " — Recipe Security Approval";
                HeaderIcon = "🛡️";
                HeaderTitle = "External Capture Recipe Detected";
                HeaderDescription = "A new capture recipe file is requesting to be registered into Greenshot. Review its details, triggers, and execution steps before approving.";
                StatusBadgeText = "NEW RECIPE";
                StatusBadgeBackgroundBrush = BadgeBackgroundBrush;
                StatusBadgeBorderBrush = CardBorderBrush;
                StatusBadgeForegroundBrush = AccentBrush;
                PreviousApprovalDate = null;
                PreviousFileHash = null;
                ApproveButtonText = "Approve & Enable";
            }

            // What changed: against the replaced recipe (built-in or current version) or the previously approved file
            CaptureRecipe previousRecipe = request.ReplacedRecipe;
            string previousContent = request.PreviousContent;
            if (previousRecipe == null && ApprovalMode == RecipeApprovalMode.Modified && !string.IsNullOrEmpty(prev?.ApprovedContent))
            {
                previousContent ??= prev.ApprovedContent;
                previousRecipe = TryFindRecipe(prev.ApprovedContent, recipe?.Id);
            }
            if (request.ReplacedRecipe != null)
            {
                ChangesTitle = request.ReplacesBuiltIn ? $"Replaces the built-in recipe \"{request.ReplacedRecipe.Name}\"" : $"Changes the recipe \"{request.ReplacedRecipe.Name}\"";
            }
            else if (ApprovalMode == RecipeApprovalMode.Modified)
            {
                ChangesTitle = previousRecipe != null ? "What changed since the last approval" : "The previous version is not known, review the whole recipe";
            }
            if (previousRecipe != null && recipe != null)
            {
                foreach (var change in RecipeDescriber.DescribeChanges(previousRecipe, recipe))
                {
                    ChangeDescriptions.Add(change);
                }
                if (ChangeDescriptions.Count == 0)
                {
                    ChangeDescriptions.Add("No change in what the recipe does (only details like names or layout).");
                }
            }
            if (!string.IsNullOrEmpty(previousContent) && !string.IsNullOrEmpty(_content))
            {
                _diffText = RecipeTextDiff.ToUnifiedText(previousContent, _content);
            }

            PopulateTriggers(recipe);
            bool startSwitchedOff = IsAiProposal || request.StartSwitchedOff;
            var previousApproval = prev?.GetApproval(recipe?.Id);
            if (request.SuggestedApproval != null)
            {
                PopulateTriggerItems(recipe, defaultOn: true, request.SuggestedApproval, keepPrevious: true);
            }
            else
            {
                PopulateTriggerItems(recipe, defaultOn: !startSwitchedOff, previousApproval, request.PreviousRecord != null && ApprovalMode != RecipeApprovalMode.Modified);
            }
            PopulateSteps(recipe);

            // Gated actions: one switch per kind, all of them have to be allowed
            if (!IsValidationError && validationResult != null && validationResult.HasGatedActions)
            {
                HasExternalCommands = true;
                foreach (var group in validationResult.GatedActions.GroupBy(a => a.GateType).OrderBy(g => g.Key))
                {
                    var item = new GateApprovalItem
                    {
                        GateType = group.Key,
                        Title = GetGateQuestion(group.Key),
                        Explanation = GetGateExplanation(group.Key),
                        // A review keeps what was allowed; otherwise external commands are always asked, the rest is pre-selected for what the user imports themselves
                        IsChecked = ApprovalMode == RecipeApprovalMode.ReVerify && previousApproval != null
                            ? previousApproval.IsGateAllowed(group.Key)
                            : request.IsOwnEdit && previousApproval?.IsGateAllowed(group.Key) == true ||
                              !startSwitchedOff && group.Key != RecipeGateType.ExternalCommand && group.Key != RecipeGateType.Custom
                    };
                    item.Targets.AddRange(group.Select(a => "• " + RecipeDescriber.DescribeGatedAction(a)).Distinct());
                    item.PropertyChanged += (_, _) => UpdateApproveEnabled();
                    GateItems.Add(item);
                }
            }

            // After everything is filled, the bindings read the computed visibilities once
            DataContext = this;
            Background = WindowBackgroundBrush;
            Title = WindowFullTitle;
            UpdateApproveEnabled();
        }

        private static CaptureRecipe TryFindRecipe(string content, string recipeId)
        {
            try
            {
                return RecipeSerializer.DeserializeList(content, validate: false)
                    .FirstOrDefault(r => string.Equals(r.Id, recipeId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetGateQuestion(RecipeGateType gateType)
        {
            return gateType switch
            {
                RecipeGateType.ExternalCommand => "Allow this recipe to run programs on this PC",
                RecipeGateType.NetworkAccess => "Allow this recipe to upload captures to the internet",
                RecipeGateType.FileSystemAccess => "Allow this recipe to read or write these files and folders",
                _ => "Allow these actions"
            };
        }

        private static string GetGateExplanation(RecipeGateType gateType)
        {
            return gateType switch
            {
                RecipeGateType.ExternalCommand => "A program started by a recipe can do anything you can do on this PC.",
                RecipeGateType.NetworkAccess => "What is uploaded leaves this PC, the service decides who can see it.",
                RecipeGateType.FileSystemAccess => "The recipe works with files outside Greenshot's usual output folder.",
                _ => null
            };
        }

        private void UpdateApproveEnabled()
        {
            if (BtnApprove != null)
            {
                BtnApprove.IsEnabled = GateItems.All(g => g.IsChecked);
            }
        }

        public RecipeApprovalWindow(string filePath, RecipeValidationResult validationResult = null, CaptureRecipe recipe = null, string rawErrorMessage = null)
        {
            _recipe = recipe;
            ApprovalMode = RecipeApprovalMode.ValidationError;
            InitializeComponent();

            RecipeName = recipe?.Name ?? (File.Exists(filePath) ? Path.GetFileName(filePath) : "Unknown Recipe");
            RecipeVersion = string.IsNullOrWhiteSpace(recipe?.Version) ? "" : $"v{recipe.Version}";
            RecipeDescription = string.IsNullOrWhiteSpace(recipe?.Description) ? "Recipe configuration failed validation." : recipe.Description;
            RecipeId = recipe?.Id ?? "unknown";
            FilePath = filePath ?? "Unknown file path";
            FileHash = RecipeTrustStore.ComputeSha256(filePath) ?? "Unknown";

            WindowTitleSubtitle = " — Recipe Validation Failed";
            HeaderIcon = "❌";
            HeaderTitle = "Recipe Validation Failed";
            HeaderDescription = "Greenshot could not load or register this capture recipe because it contains configuration errors. Review the diagnostic details below:";
            StatusBadgeText = "VALIDATION ERROR";
            StatusBadgeBackgroundBrush = ErrorBackgroundBrush;
            StatusBadgeBorderBrush = ErrorBorderBrush;
            StatusBadgeForegroundBrush = ErrorTextBrush;
            ApproveButtonText = "Close";

            DataContext = this;
            Background = WindowBackgroundBrush;
            Title = WindowFullTitle;

            if (validationResult != null && validationResult.Errors.Count > 0)
            {
                foreach (var err in validationResult.Errors)
                {
                    ValidationErrors.Add(RecipeValidationErrorItem.Create(err));
                }
            }
            else if (!string.IsNullOrWhiteSpace(rawErrorMessage))
            {
                ValidationErrors.Add(RecipeValidationErrorItem.Create(rawErrorMessage));
            }
            else
            {
                ValidationErrors.Add(RecipeValidationErrorItem.Create("An unknown validation error occurred while loading the recipe."));
            }

            PopulateTriggers(recipe);
            PopulateSteps(recipe);
        }

        /// <summary>
        /// Show the validation errors (modal on the UI thread, from another thread it's posted to it)
        /// </summary>
        public static void ShowValidationError(string filePath, RecipeValidationResult validationResult = null, CaptureRecipe recipe = null, string rawErrorMessage = null)
        {
            UiDispatcher.Current.RunOnUiAsync(Show).FireAndLog("Show the recipe validation errors");

            void Show()
            {
                var window = new RecipeApprovalWindow(filePath, validationResult, recipe, rawErrorMessage)
                {
                    Topmost = true,
                    ShowActivated = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                IntPtr ownerHwnd = IntPtr.Zero;
                var mainForm = SimpleServiceProvider.Current.GetInstance<System.Windows.Forms.Form>(isOptional: true);
                if (mainForm != null && mainForm.IsHandleCreated)
                {
                    try
                    {
                        if (mainForm.Visible && !mainForm.Disposing && !mainForm.IsDisposed)
                        {
                            ownerHwnd = mainForm.Handle;
                        }
                    }
                    catch
                    {
                        ownerHwnd = IntPtr.Zero;
                    }
                }

                if (ownerHwnd == IntPtr.Zero && System.Windows.Application.Current != null)
                {
                    try
                    {
                        var activeWpfWindow = System.Windows.Application.Current.Windows
                            .OfType<System.Windows.Window>()
                            .FirstOrDefault(w => w.IsActive && w != window);
                        if (activeWpfWindow != null)
                        {
                            ownerHwnd = new System.Windows.Interop.WindowInteropHelper(activeWpfWindow).Handle;
                        }
                    }
                    catch
                    {
                        ownerHwnd = IntPtr.Zero;
                    }
                }

                if (ownerHwnd != IntPtr.Zero)
                {
                    new System.Windows.Interop.WindowInteropHelper(window).Owner = ownerHwnd;
                }

                window.ShowDialog();
            }
        }

        private void PopulateTriggers(CaptureRecipe recipe)
        {
            TriggerBadges.Clear();
            if (recipe?.Triggers != null && recipe.Triggers.Count > 0)
            {
                foreach (var trigger in recipe.Triggers)
                {
                    string label;
                    if (string.Equals(trigger.TriggerType, TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase))
                    {
                        label = $"⌨ Hotkey: {trigger.GetParameter<string>("Hotkey", "None")}";
                    }
                    else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeContextMenu, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(trigger.TriggerType, TriggerConfig.TypeSystray, StringComparison.OrdinalIgnoreCase))
                    {
                        label = $"📋 Systray: \"{trigger.GetParameter<string>("MenuItemText", recipe.Name)}\"";
                    }
                    else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeEditor, StringComparison.OrdinalIgnoreCase))
                    {
                        label = $"🎨 Editor: \"{trigger.GetParameter<string>("MenuItemText", recipe.Name)}\"";
                    }
                    else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeClipboard, StringComparison.OrdinalIgnoreCase))
                    {
                        label = "📋 Clipboard Monitor";
                    }
                    else
                    {
                        label = $"{trigger.TriggerType}: {trigger.Name}";
                    }

                    TriggerBadges.Add(new TriggerBadgeModel
                    {
                        Text = label,
                        ForegroundBrush = TextPrimaryBrush,
                        BackgroundBrush = BadgeBackgroundBrush,
                        BorderBrush = CardBorderBrush
                    });
                }
            }
            else if (!IsValidationError)
            {
                TriggerBadges.Add(new TriggerBadgeModel
                {
                    Text = "ℹ Manual / Triggerless (Invoked via CLI / API)",
                    ForegroundBrush = TextSecondaryBrush,
                    BackgroundBrush = BadgeBackgroundBrush,
                    BorderBrush = CardBorderBrush
                });
            }
        }

        private void PopulateTriggerItems(CaptureRecipe recipe, bool defaultOn, RecipeApproval previousApproval, bool keepPrevious)
        {
            TriggerItems.Clear();
            if (IsValidationError)
            {
                return;
            }
            foreach (var trigger in RecipeDescriber.DescribeTriggers(recipe))
            {
                TriggerItems.Add(new TriggerApprovalItem
                {
                    Key = trigger.Key,
                    Title = trigger.IsDisabled ? $"{trigger.Label} (disabled in the recipe)" : trigger.Label,
                    IsDisabledInRecipe = trigger.IsDisabled,
                    Explanation = trigger.Risk,
                    IsChecked = keepPrevious && previousApproval != null ? previousApproval.IsTriggerApproved(trigger.Key) : defaultOn
                });
            }
            TriggerHint = !defaultOn
                ? "⚠ The triggers start switched off. Tick each one that may start this recipe: a trigger you leave off stays off " +
                  "(you can switch it on later with Permissions in the recipe manager). Without a trigger you can still run the recipe from the recipe list."
                : "Switch off the triggers you don't want.";
            TriggerHintBrush = !defaultOn ? WarningTextBrush : TextSecondaryBrush;
        }

        private void PopulateSteps(CaptureRecipe recipe)
        {
            StepDescriptions.Clear();
            if (recipe?.Nodes == null)
            {
                return;
            }
            foreach (var line in RecipeDescriber.DescribeSteps(recipe))
            {
                StepDescriptions.Add(new StepLineModel
                {
                    Icon = line.Risk switch
                    {
                        RecipeGateType.NetworkAccess => "🌐",
                        RecipeGateType.ExternalCommand => "⚙",
                        RecipeGateType.FileSystemAccess => "📁",
                        RecipeGateType.Custom => "⚠",
                        _ => "▶"
                    },
                    Text = line.Text,
                    Margin = line.IsDetail ? new Thickness(24, 0, 0, 4) : new Thickness(0, 2, 0, 4),
                    Foreground = line.Risk.HasValue ? WarningTextBrush : TextPrimaryBrush,
                    FontWeight = line.Risk.HasValue && !line.IsDetail ? FontWeights.SemiBold : FontWeights.Normal
                });
            }
        }

        private void OnCloseTitleBarClicked(object sender, RoutedEventArgs e)
        {
            IsApproved = false;
            DialogResult = false;
            Close();
        }

        private void OnTitleBarMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                DragMove();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyImmersiveDarkMode();

            Topmost = true;
            Activate();
            Focus();

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                SetForegroundWindow(helper.Handle);
                BringWindowToTop(helper.Handle);
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            CenterWindowOnScreen();
            // Switch to manual sizing after initial layout so the user can freely resize
            SizeToContent = SizeToContent.Manual;
            if (ContentScrollViewer != null)
            {
                ContentScrollViewer.ClearValue(MaxHeightProperty);
            }
        }

        private void CenterWindowOnScreen()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    var screen = System.Windows.Forms.Screen.FromHandle(helper.Handle);
                    var bounds = screen.WorkingArea;
                    Left = bounds.Left + (bounds.Width - ActualWidth) / 2;
                    Top = bounds.Top + (bounds.Height - ActualHeight) / 2;
                    return;
                }
            }
            catch
            {
                // Fallback
            }

            try
            {
                var workArea = SystemParameters.WorkArea;
                Left = workArea.Left + (workArea.Width - ActualWidth) / 2;
                Top = workArea.Top + (workArea.Height - ActualHeight) / 2;
            }
            catch
            {
                // Silently ignore
            }
        }

        private void OnOpenInExplorerClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(FilePath))
                {
                    ExplorerHelper.OpenInExplorer(FilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open Explorer for '{FilePath}': {ex.Message}");
            }
        }

        private void OnViewFileClicked(object sender, RoutedEventArgs e)
        {
            ToggleJsonViewer();
        }

        private void OnCloseJsonViewerClicked(object sender, RoutedEventArgs e)
        {
            IsJsonViewerVisible = false;
        }

        private void OnCopyJsonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(RecipeJsonContent))
                {
                    ClipboardHelper.SetClipboardData(RecipeJsonContent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy JSON to clipboard: {ex.Message}");
            }
        }

        public void ToggleJsonViewer()
        {
            IsJsonViewerVisible = !IsJsonViewerVisible;
            if (IsJsonViewerVisible)
            {
                OverlayTitle = "Recipe JSON Definition";
                LoadJsonContent();
            }
        }

        private void OnViewChangesClicked(object sender, RoutedEventArgs e)
        {
            OverlayTitle = "Changes (- before, + after)";
            RecipeJsonContent = _diffText;
            IsJsonViewerVisible = true;
        }

        private void LoadJsonContent()
        {
            try
            {
                if (_content != null)
                {
                    // What is approved, the file could have changed since it was read
                    RecipeJsonContent = _content.Length > 1024 * 1024 ? "// Recipe file is too large to preview (> 1MB)." : _content;
                }
                else if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
                {
                    var fi = new FileInfo(FilePath);
                    if (fi.Length > 1024 * 1024)
                    {
                        RecipeJsonContent = "// Recipe file is too large to preview (> 1MB).";
                    }
                    else
                    {
                        RecipeJsonContent = File.ReadAllText(FilePath);
                    }
                }
                else if (_recipe != null)
                {
                    RecipeJsonContent = RecipeSerializer.Serialize(_recipe);
                }
                else
                {
                    RecipeJsonContent = "// Recipe content unavailable.";
                }
            }
            catch (Exception ex)
            {
                RecipeJsonContent = $"// Error reading recipe: {ex.Message}";
            }
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape && IsJsonViewerVisible)
            {
                IsJsonViewerVisible = false;
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }

        private void ApplyImmersiveDarkMode()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero && WpfThemeHelper.IsDarkMode)
                {
                    int useImmersiveDarkMode = 1;
                    int hr = DwmSetWindowAttribute(helper.Handle, 20, ref useImmersiveDarkMode, sizeof(int));
                    if (hr != 0)
                    {
                        DwmSetWindowAttribute(helper.Handle, 19, ref useImmersiveDarkMode, sizeof(int));
                    }
                }
            }
            catch
            {
                // Silently ignore if DWM call is unsupported on older OS
            }
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            IsApproved = false;
            DialogResult = false;
            Close();
        }

        private void OnEditFileClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(FilePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open recipe file in editor: {ex.Message}");
            }
        }

        private void OnApproveClicked(object sender, RoutedEventArgs e)
        {
            if (GateItems.Any(g => !g.IsChecked))
            {
                return;
            }
            // A trigger left off is easy to miss: say so before it is approved like that
            var leftOff = TriggerItems.Where(t => !t.IsChecked && !t.IsDisabledInRecipe).Select(t => "• " + t.Title).ToList();
            if (leftOff.Count > 0)
            {
                string text = (leftOff.Count == 1 ? "This trigger stays off, so it won't start the recipe:" : "These triggers stay off, so they won't start the recipe:") +
                              "\n\n" + string.Join("\n", leftOff) +
                              "\n\nYou can switch triggers on later with Permissions in the recipe manager, and run the recipe from the recipe list.";
                int choice = ThemedMessageBox.ShowChoice(this, "Triggers Left Off", text, MessageBoxImage.Warning,
                    new[] { "Approve", "Go Back" }, defaultIndex: 1, cancelIndex: 1);
                if (choice != 0)
                {
                    return;
                }
            }
            IsApproved = true;
            Approval = new RecipeApproval
            {
                RecipeId = _recipe?.Id,
                ApprovedTriggers = TriggerItems.Where(t => t.IsChecked).Select(t => t.Key).ToList(),
                AllowedGates = GateItems.Where(g => g.IsChecked).Select(g => g.GateType).ToList()
            };
            OpenInEditor = ChkOpenInEditor?.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void OnRejectClicked(object sender, RoutedEventArgs e)
        {
            if (ApprovalMode == RecipeApprovalMode.ReVerify)
            {
                int choice = ThemedMessageBox.ShowChoice(this, "Revoke Approval",
                    $"Revoke the approval of \"{_recipe?.Name}\"? It stops running right away and isn't loaded again. Its file stays where it is: " +
                    "open it again to review and approve it.", MessageBoxImage.Warning, new[] { "Revoke", "Keep" }, defaultIndex: 1, cancelIndex: 1);
                if (choice != 0)
                {
                    return;
                }
                IsRevoked = true;
            }
            IsApproved = false;
            DialogResult = false;
            Close();
        }
    }
}
