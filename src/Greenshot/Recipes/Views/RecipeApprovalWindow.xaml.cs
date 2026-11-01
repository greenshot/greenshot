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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Recipes.Approval;
using Greenshot.Recipes.ViewModels;

namespace Greenshot.Recipes.Views
{

    /// <summary>
    /// Modern WPF dialog for reviewing and approving external capture recipes with dark/light mode support.
    /// </summary>
    public partial class RecipeApprovalWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// The user revoked the approval in the review of an approved recipe
        /// </summary>
        public bool IsRevoked { get; private set; }

        public string RejectButtonText { get; private set; } = Texts.Recipe.ApprovalBtnReject;

        private readonly CaptureRecipe _recipe;
        private readonly string _content;
        private readonly string _diffText;
        private bool _isJsonViewerVisible;
        private IInputElement _focusBeforeJsonViewer;
        private string _recipeJsonContent;
        private string _overlayTitle = Texts.Recipe.ApprovalJsonTitle;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string RecipeName { get; set; }
        public string RecipeVersion { get; set; }
        public string RecipeDescription { get; set; }
        public string RecipeId { get; set; }
        public string RecipeAuthor { get; set; }
        public bool HasRecipeAuthor => !string.IsNullOrEmpty(RecipeAuthor);
        public string RecipeCopyright { get; set; }
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
                    // The keyboard focus goes into the viewer and back to where it was, else it stays on the controls behind the overlay
                    if (_isJsonViewerVisible)
                    {
                        _focusBeforeJsonViewer = System.Windows.Input.Keyboard.FocusedElement;
                        JsonTextBox?.Focus();
                    }
                    else
                    {
                        _focusBeforeJsonViewer?.Focus();
                        _focusBeforeJsonViewer = null;
                    }
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

        public ObservableCollection<RecipeValidationErrorViewModel> ValidationErrors { get; } = new ObservableCollection<RecipeValidationErrorViewModel>();
        public ObservableCollection<TriggerBadgeViewModel> TriggerBadges { get; } = new ObservableCollection<TriggerBadgeViewModel>();
        public ObservableCollection<TriggerApprovalViewModel> TriggerItems { get; } = new ObservableCollection<TriggerApprovalViewModel>();
        public ObservableCollection<StepLineViewModel> StepDescriptions { get; } = new ObservableCollection<StepLineViewModel>();
        public ObservableCollection<GateApprovalViewModel> GateItems { get; } = new ObservableCollection<GateApprovalViewModel>();

        public Visibility TriggersVisibility => TriggerBadges.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TriggerItemsVisibility => TriggerItems.Count > 0 && !IsValidationError ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TriggerBadgesVisibility => TriggerItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PipelineVisibility => StepDescriptions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ExternalCommandWarningVisibility => (HasExternalCommands && !IsValidationError) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility OpenInEditorVisibility => IsAiProposal && !IsValidationError && !IsReadOnly ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Details only: the switches show the approval but can't be changed
        /// </summary>
        public bool IsReadOnly { get; private set; }

        public bool IsEditable => !IsReadOnly;

        public bool IsReview => ApprovalMode == RecipeApprovalMode.ReVerify && !IsReadOnly;

        public Visibility DecisionVisibility => !IsValidationError && !IsReadOnly ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// A review offers "Keep as Is": closing it changes nothing
        /// </summary>
        public Visibility KeepVisibility => IsReview ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ReadOnlyCloseVisibility => IsReadOnly && !IsValidationError ? Visibility.Visible : Visibility.Collapsed;
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

            RecipeName = recipe?.Name ?? (File.Exists(filePath) ? Path.GetFileName(filePath) : Texts.Recipe.ApprovalUnnamedRecipe);
            RecipeVersion = string.IsNullOrWhiteSpace(recipe?.Version) ? "" : $"v{recipe.Version}";
            RecipeDescription = string.IsNullOrWhiteSpace(recipe?.Description) ? (validationResult != null && !validationResult.IsValid ? Texts.Recipe.ApprovalFailedValidation : Texts.Recipe.ApprovalNoDescription) : recipe.Description;
            RecipeId = recipe?.Id ?? Texts.Recipe.ApprovalUnknown;
            RecipeAuthor = DescribeAuthor(recipe);
            RecipeCopyright = string.IsNullOrWhiteSpace(recipe?.Copyright) ? null : recipe.Copyright;
            FilePath = filePath ?? Texts.Recipe.ApprovalUnknownPath;
            // The hash of the content that was read once, never of the file as it is now
            FileHash = request.ContentHash ?? Texts.Recipe.ApprovalUnknown;

            IsAiProposal = request.IsAiProposal;
            if (IsAiProposal)
            {
                ProposedByText = string.IsNullOrWhiteSpace(request.ProposedByPath) ? request.ProposedByName : $"{request.ProposedByName} ({request.ProposedByPath})";
                ProposedBySignerText = string.IsNullOrWhiteSpace(request.ProposedBySigner) ? Texts.Recipe.ApprovalNotSigned : string.Format(Texts.Recipe.ApprovalSignedBy, request.ProposedBySigner);
                AiRequest = request.AiRequest;
                AiExplanation = request.AiExplanation;
            }

            var prev = request.PreviousRecord;
            if (validationResult != null && !validationResult.IsValid)
            {
                ApprovalMode = RecipeApprovalMode.ValidationError;
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalValidationFailed;
                HeaderIcon = "❌";
                HeaderTitle = Texts.Recipe.ApprovalValidationFailed;
                HeaderDescription = Texts.Recipe.ApprovalValidationDescription;
                StatusBadgeText = Texts.Recipe.ApprovalBadgeValidation;
                StatusBadgeBackgroundBrush = ErrorBackgroundBrush;
                StatusBadgeBorderBrush = ErrorBorderBrush;
                StatusBadgeForegroundBrush = ErrorTextBrush;
                ApproveButtonText = Texts.Core.AccessibleClose;

                foreach (var err in validationResult.Errors)
                {
                    ValidationErrors.Add(RecipeValidationErrorViewModel.Create(err));
                }
            }
            else if (IsAiProposal)
            {
                ApprovalMode = RecipeApprovalMode.NewRecipe;
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalAiSubtitle;
                HeaderIcon = "🤖";
                HeaderTitle = request.ReplacedRecipe != null ? Texts.Recipe.ApprovalAiTitleChange : Texts.Recipe.ApprovalAiTitleNew;
                HeaderDescription = Texts.Recipe.ApprovalAiDescription;
                StatusBadgeText = Texts.Recipe.ApprovalBadgeAi;
                StatusBadgeBackgroundBrush = WarningBackgroundBrush;
                StatusBadgeBorderBrush = WarningBorderBrush;
                StatusBadgeForegroundBrush = WarningTextBrush;
                ApproveButtonText = Texts.Recipe.ApprovalBtnSaveRecipe;
            }
            else if (request.IsOwnEdit)
            {
                ApprovalMode = prev != null ? RecipeApprovalMode.Modified : RecipeApprovalMode.NewRecipe;
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalOwnSubtitle;
                HeaderIcon = "🛡️";
                HeaderTitle = Texts.Recipe.ApprovalOwnTitle;
                HeaderDescription = string.Format(Texts.Recipe.ApprovalOwnDescription, string.Join(" ", request.OwnEditReasons));
                StatusBadgeText = Texts.Recipe.ApprovalBadgeOwn;
                StatusBadgeBackgroundBrush = BadgeBackgroundBrush;
                StatusBadgeBorderBrush = CardBorderBrush;
                StatusBadgeForegroundBrush = AccentBrush;
                if (prev != null)
                {
                    PreviousApprovalDate = prev.ApprovedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                    PreviousFileHash = prev.Sha256Hash;
                }
                ApproveButtonText = Texts.Recipe.ApprovalBtnApproveSave;
            }
            else if (prev != null && !string.Equals(prev.Sha256Hash, FileHash, StringComparison.OrdinalIgnoreCase))
            {
                ApprovalMode = RecipeApprovalMode.Modified;
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalModifiedSubtitle;
                HeaderIcon = "⚠️";
                HeaderTitle = Texts.Recipe.ApprovalModifiedTitle;
                HeaderDescription = Texts.Recipe.ApprovalModifiedDescription;
                StatusBadgeText = Texts.Recipe.ApprovalBadgeModified;
                StatusBadgeBackgroundBrush = WarningBackgroundBrush;
                StatusBadgeBorderBrush = WarningBorderBrush;
                StatusBadgeForegroundBrush = WarningTextBrush;
                PreviousApprovalDate = prev.ApprovedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                PreviousFileHash = prev.Sha256Hash;
                ApproveButtonText = Texts.Recipe.ApprovalBtnApproveChanges;
            }
            else if (prev != null)
            {
                ApprovalMode = RecipeApprovalMode.ReVerify;
                RejectButtonText = Texts.Recipe.ApprovalBtnRevoke;
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalReviewSubtitle;
                HeaderIcon = "🛡️";
                HeaderTitle = Texts.Recipe.ApprovalReviewTitle;
                HeaderDescription = Texts.Recipe.ApprovalReviewDescription;
                StatusBadgeText = Texts.Recipe.ApprovalBadgeApproved;
                StatusBadgeBackgroundBrush = BadgeBackgroundBrush;
                StatusBadgeBorderBrush = CardBorderBrush;
                StatusBadgeForegroundBrush = TextSecondaryBrush;
                PreviousApprovalDate = prev.ApprovedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                PreviousFileHash = prev.Sha256Hash;
                ApproveButtonText = Texts.Recipe.ApprovalBtnSaveChanges;
            }
            else
            {
                ApprovalMode = RecipeApprovalMode.NewRecipe;
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalNewSubtitle;
                HeaderIcon = "🛡️";
                HeaderTitle = Texts.Recipe.ApprovalNewTitle;
                HeaderDescription = Texts.Recipe.ApprovalNewDescription;
                StatusBadgeText = Texts.Recipe.ApprovalBadgeNew;
                StatusBadgeBackgroundBrush = BadgeBackgroundBrush;
                StatusBadgeBorderBrush = CardBorderBrush;
                StatusBadgeForegroundBrush = AccentBrush;
                PreviousApprovalDate = null;
                PreviousFileHash = null;
                ApproveButtonText = Texts.Recipe.ApprovalBtnApproveEnable;
            }

            if (request.IsReadOnly && !IsValidationError)
            {
                IsReadOnly = true;
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalDetailsTitle;
                HeaderIcon = "📄";
                HeaderTitle = Texts.Recipe.ApprovalDetailsTitle;
                HeaderDescription = string.IsNullOrEmpty(request.FilePath)
                    ? Texts.Recipe.ApprovalDetailsBuiltinDescription
                    : Texts.Recipe.ApprovalDetailsDescription;
                if (string.IsNullOrEmpty(request.FilePath))
                {
                    FilePath = Texts.Recipe.ApprovalBuiltinPath;
                    StatusBadgeText = Texts.Recipe.ApprovalBadgeBuiltin;
                    StatusBadgeBackgroundBrush = BadgeBackgroundBrush;
                    StatusBadgeBorderBrush = CardBorderBrush;
                    StatusBadgeForegroundBrush = TextSecondaryBrush;
                }
            }

            if (request.Extension != null)
            {
                ApplyExtensionTexts(request);
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
                ChangesTitle = string.Format(request.ReplacesBuiltIn ? Texts.Recipe.ApprovalChangesReplacesBuiltin : Texts.Recipe.ApprovalChangesRecipe, request.ReplacedRecipe.Name);
            }
            else if (ApprovalMode == RecipeApprovalMode.Modified)
            {
                ChangesTitle = previousRecipe != null ? Texts.Recipe.ApprovalChangesSince : Texts.Recipe.ApprovalChangesUnknown;
            }
            if (previousRecipe != null && recipe != null)
            {
                foreach (var change in RecipeDescriber.DescribeChanges(previousRecipe, recipe))
                {
                    ChangeDescriptions.Add(change);
                }
                if (ChangeDescriptions.Count == 0)
                {
                    ChangeDescriptions.Add(Texts.Recipe.ApprovalNoChange);
                }
            }
            if (!string.IsNullOrEmpty(previousContent) && !string.IsNullOrEmpty(_content))
            {
                _diffText = RecipeTextDiff.ToUnifiedText(previousContent, _content);
            }

            if (!IsExtension)
            {
                PopulateTriggers(recipe);
            }
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
                    var item = new GateApprovalViewModel
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
            if (IsReview && BtnReject != null)
            {
                // Esc means "Keep as Is" in a review, never "Revoke"
                BtnReject.IsCancel = false;
            }
        }

        /// <summary>
        /// An automatic step (recipe extension) has no triggers, it changes other recipes: the texts say so, and which recipes it changes
        /// </summary>
        public bool IsExtension { get; private set; }

        public ObservableCollection<string> ExtensionReachLines { get; } = new ObservableCollection<string>();

        public Visibility ExtensionReachVisibility => IsExtension && !IsValidationError ? Visibility.Visible : Visibility.Collapsed;

        private void ApplyExtensionTexts(RecipeApprovalRequest request)
        {
            IsExtension = true;
            foreach (var line in request.ExtensionReach ?? Array.Empty<string>())
            {
                ExtensionReachLines.Add(line);
            }

            if (IsValidationError)
            {
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalExtValidationFailed;
                HeaderTitle = Texts.Recipe.ApprovalExtValidationFailed;
                HeaderDescription = Texts.Recipe.ApprovalExtValidationDescription;
            }
            else if (IsReadOnly)
            {
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalExtDetailsTitle;
                HeaderTitle = Texts.Recipe.ApprovalExtDetailsTitle;
                HeaderDescription = Texts.Recipe.ApprovalExtDetailsDescription;
            }
            else if (IsAiProposal)
            {
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalExtAiSubtitle;
                HeaderTitle = Texts.Recipe.ApprovalExtAiTitle;
                HeaderDescription = Texts.Recipe.ApprovalExtAiDescription;
            }
            else if (ApprovalMode == RecipeApprovalMode.Modified)
            {
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalExtModifiedSubtitle;
                HeaderTitle = Texts.Recipe.ApprovalExtModifiedTitle;
                HeaderDescription = Texts.Recipe.ApprovalExtModifiedDescription;
            }
            else if (IsReview)
            {
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalExtReviewTitle;
                HeaderTitle = Texts.Recipe.ApprovalExtReviewTitle;
                HeaderDescription = Texts.Recipe.ApprovalExtReviewDescription;
            }
            else if (request.IsOwnEdit)
            {
                HeaderDescription = string.Format(Texts.Recipe.ApprovalExtOwnDescription, string.Join(" ", request.OwnEditReasons));
            }
            else
            {
                WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalExtNewSubtitle;
                HeaderTitle = Texts.Recipe.ApprovalExtNewTitle;
                HeaderDescription = Texts.Recipe.ApprovalExtNewDescription;
                StatusBadgeText = Texts.Recipe.ApprovalBadgeExtNew;
            }
        }

        /// <summary>
        /// The authors of the recipe as written in it, with its license, e.g. "Jane Doe, John Roe (MIT)"; null without authors
        /// </summary>
        internal static string DescribeAuthor(FlowDefinition definition)
        {
            var names = definition?.Authors?.Where(a => !string.IsNullOrWhiteSpace(a?.Name)).Select(a => a.Name.Trim()).ToList();
            if (names == null || names.Count == 0)
            {
                return null;
            }
            string authors = string.Join(", ", names);
            return string.IsNullOrWhiteSpace(definition.License) ? authors : $"{authors} ({definition.License.Trim()})";
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
                RecipeGateType.ExternalCommand => Texts.Recipe.ApprovalGateExternalCommand,
                RecipeGateType.NetworkAccess => Texts.Recipe.ApprovalGateNetwork,
                RecipeGateType.FileSystemAccess => Texts.Recipe.ApprovalGateFiles,
                _ => Texts.Recipe.ApprovalGateCustom
            };
        }

        private static string GetGateExplanation(RecipeGateType gateType)
        {
            return gateType switch
            {
                RecipeGateType.ExternalCommand => Texts.Recipe.ApprovalGateExternalCommandExplanation,
                RecipeGateType.NetworkAccess => Texts.Recipe.ApprovalGateNetworkExplanation,
                RecipeGateType.FileSystemAccess => Texts.Recipe.ApprovalGateFilesExplanation,
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

            RecipeName = recipe?.Name ?? (File.Exists(filePath) ? Path.GetFileName(filePath) : Texts.Recipe.ApprovalUnknownRecipe);
            RecipeVersion = string.IsNullOrWhiteSpace(recipe?.Version) ? "" : $"v{recipe.Version}";
            RecipeDescription = string.IsNullOrWhiteSpace(recipe?.Description) ? Texts.Recipe.ApprovalFailedValidation : recipe.Description;
            RecipeId = recipe?.Id ?? Texts.Recipe.ApprovalUnknown;
            FilePath = filePath ?? Texts.Recipe.ApprovalUnknownPath;
            FileHash = RecipeTrustStore.ComputeSha256(filePath) ?? Texts.Recipe.ApprovalUnknown;

            WindowTitleSubtitle = " — " + Texts.Recipe.ApprovalValidationFailed;
            HeaderIcon = "❌";
            HeaderTitle = Texts.Recipe.ApprovalValidationFailed;
            HeaderDescription = Texts.Recipe.ApprovalValidationDescription;
            StatusBadgeText = Texts.Recipe.ApprovalBadgeValidation;
            StatusBadgeBackgroundBrush = ErrorBackgroundBrush;
            StatusBadgeBorderBrush = ErrorBorderBrush;
            StatusBadgeForegroundBrush = ErrorTextBrush;
            ApproveButtonText = Texts.Core.AccessibleClose;

            DataContext = this;
            Background = WindowBackgroundBrush;
            Title = WindowFullTitle;

            if (validationResult != null && validationResult.Errors.Count > 0)
            {
                foreach (var err in validationResult.Errors)
                {
                    ValidationErrors.Add(RecipeValidationErrorViewModel.Create(err));
                }
            }
            else if (!string.IsNullOrWhiteSpace(rawErrorMessage))
            {
                ValidationErrors.Add(RecipeValidationErrorViewModel.Create(rawErrorMessage));
            }
            else
            {
                ValidationErrors.Add(RecipeValidationErrorViewModel.Create(Texts.Recipe.ApprovalValidationUnknown));
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

                // The active WPF window owns the errors, otherwise they stand alone (topmost)
                IntPtr ownerHwnd = IntPtr.Zero;
                if (System.Windows.Application.Current != null)
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
                        label = "⌨ " + string.Format(Texts.Recipe.ApprovalTriggerHotkey, trigger.GetParameter<string>("Hotkey", "None"));
                    }
                    else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeContextMenu, StringComparison.OrdinalIgnoreCase))
                    {
                        label = "📋 " + string.Format(Texts.Recipe.ApprovalTriggerContextmenu, trigger.GetParameter<string>("MenuItemText", recipe.Name));
                    }
                    else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeEditor, StringComparison.OrdinalIgnoreCase))
                    {
                        label = "🎨 " + string.Format(Texts.Recipe.ApprovalTriggerEditor, trigger.GetParameter<string>("MenuItemText", recipe.Name));
                    }
                    else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeClipboard, StringComparison.OrdinalIgnoreCase))
                    {
                        label = "📋 " + Texts.Recipe.ApprovalTriggerClipboard;
                    }
                    else
                    {
                        label = $"{trigger.TriggerType}: {trigger.Name}";
                    }

                    TriggerBadges.Add(new TriggerBadgeViewModel
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
                TriggerBadges.Add(new TriggerBadgeViewModel
                {
                    Text = "ℹ " + Texts.Recipe.ApprovalTriggerManual,
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
                TriggerItems.Add(new TriggerApprovalViewModel
                {
                    Key = trigger.Key,
                    Title = trigger.IsDisabled ? string.Format(Texts.Recipe.ApprovalTriggerDisabled, trigger.Label) : trigger.Label,
                    IsDisabledInRecipe = trigger.IsDisabled,
                    Explanation = trigger.Risk,
                    IsChecked = keepPrevious && previousApproval != null ? previousApproval.IsTriggerApproved(trigger.Key) : defaultOn
                });
            }
            TriggerHint = !defaultOn
                ? "⚠ " + Texts.Recipe.ApprovalTriggersOffHint
                : Texts.Recipe.ApprovalTriggersOnHint;
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
                StepDescriptions.Add(new StepLineViewModel
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
                OverlayTitle = Texts.Recipe.ApprovalJsonTitle;
                LoadJsonContent();
            }
        }

        private void OnViewChangesClicked(object sender, RoutedEventArgs e)
        {
            OverlayTitle = Texts.Recipe.ApprovalChangesOverlayTitle;
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
                    RecipeJsonContent = _content.Length > 1024 * 1024 ? "// " + Texts.Recipe.ApprovalJsonTooLarge : _content;
                }
                else if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
                {
                    var fi = new FileInfo(FilePath);
                    if (fi.Length > 1024 * 1024)
                    {
                        RecipeJsonContent = "// " + Texts.Recipe.ApprovalJsonTooLarge;
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
                    RecipeJsonContent = "// " + Texts.Recipe.ApprovalJsonUnavailable;
                }
            }
            catch (Exception ex)
            {
                RecipeJsonContent = "// " + string.Format(Texts.Recipe.ApprovalJsonError, ex.Message);
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
            // The title bar in the colors of the theme, it follows theme changes from now on
            WindowFrameTheme.Attach(this);
        }

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
                string text = (leftOff.Count == 1 ? Texts.Recipe.ApprovalLeftOffOne : Texts.Recipe.ApprovalLeftOffMany) +
                              "\n\n" + string.Join("\n", leftOff) +
                              "\n\n" + Texts.Recipe.ApprovalLeftOffLater;
                int choice = ThemedMessageBox.ShowChoice(this, Texts.Recipe.ApprovalLeftOffTitle, text, MessageBoxImage.Warning,
                    new[] { Texts.Recipe.ApprovalBtnApprove, Texts.Recipe.ApprovalBtnGoBack }, defaultIndex: 1, cancelIndex: 1);
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
                int choice = ThemedMessageBox.ShowChoice(this, Texts.Recipe.ApprovalBtnRevoke,
                    string.Format(Texts.Recipe.ApprovalRevokeQuestion, _recipe?.Name), MessageBoxImage.Warning,
                    new[] { Texts.Recipe.ApprovalBtnRevokeShort, Texts.Recipe.ApprovalBtnKeep }, defaultIndex: 1, cancelIndex: 1);
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
