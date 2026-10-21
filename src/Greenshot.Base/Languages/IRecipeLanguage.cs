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

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Base.Languages
{
    /// <summary>
    /// The texts of the [Recipe] section of greenshot.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Recipe")]
    public interface IRecipeLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Border
        /// </summary>
        string ExtensionBorder { get; }

        /// <summary>
        /// Color
        /// </summary>
        string ExtensionBorderColor { get; }

        /// <summary>
        /// Adds a border around the capture before it is exported.
        /// </summary>
        string ExtensionBorderDescription { get; }

        /// <summary>
        /// Add a border
        /// </summary>
        string ExtensionBorderEnabled { get; }

        /// <summary>
        /// Width (px)
        /// </summary>
        string ExtensionBorderWidth { get; }

        /// <summary>
        /// Caption
        /// </summary>
        string ExtensionCaption { get; }

        /// <summary>
        /// Bar color
        /// </summary>
        string ExtensionCaptionBarcolor { get; }

        /// <summary>
        /// Adds a bar with a text, for example the date and time, above or below the capture.
        /// </summary>
        string ExtensionCaptionDescription { get; }

        /// <summary>
        /// Add a caption
        /// </summary>
        string ExtensionCaptionEnabled { get; }

        /// <summary>
        /// Font size
        /// </summary>
        string ExtensionCaptionFontsize { get; }

        /// <summary>
        /// Position
        /// </summary>
        string ExtensionCaptionPosition { get; }

        /// <summary>
        /// Below the capture
        /// </summary>
        string ExtensionCaptionPositionBottom { get; }

        /// <summary>
        /// Above the capture
        /// </summary>
        string ExtensionCaptionPositionTop { get; }

        /// <summary>
        /// Text
        /// </summary>
        string ExtensionCaptionText { get; }

        /// <summary>
        /// Text color
        /// </summary>
        string ExtensionCaptionTextcolor { get; }

        /// <summary>
        /// The text of the caption: ${now:yyyy-MM-dd HH:mm:ss} is the date and time, ${user.username} your user name.
        /// </summary>
        string ExtensionCaptionTextDescription { get; }

        /// <summary>
        /// Drop shadow
        /// </summary>
        string ExtensionDropshadow { get; }

        /// <summary>
        /// Darkness
        /// </summary>
        string ExtensionDropshadowDarkness { get; }

        /// <summary>
        /// Adds a drop shadow to the capture before it is exported.
        /// </summary>
        string ExtensionDropshadowDescription { get; }

        /// <summary>
        /// Add a drop shadow
        /// </summary>
        string ExtensionDropshadowEnabled { get; }

        /// <summary>
        /// Offset (px)
        /// </summary>
        string ExtensionDropshadowOffset { get; }

        /// <summary>
        /// Size (px)
        /// </summary>
        string ExtensionDropshadowSize { get; }

        /// <summary>
        /// Custom Action
        /// </summary>
        string GateCustom { get; }

        /// <summary>
        /// External Command
        /// </summary>
        string GateExternalCommand { get; }

        /// <summary>
        /// File System Access
        /// </summary>
        string GateFileSystemAccess { get; }

        /// <summary>
        /// Network Access
        /// </summary>
        string GateNetworkAccess { get; }

        /// <summary>
        /// Recipe Import
        /// </summary>
        string Import { get; }

        /// <summary>
        /// Failed to load recipe:
        /// </summary>
        string ImportFailed { get; }

        /// <summary>
        /// Import Capture Recipe
        /// </summary>
        string ImportTitle { get; }

        /// <summary>
        /// Reject / Block
        /// </summary>
        string ApprovalBtnReject { get; }

        /// <summary>
        /// Revoke Approval
        /// </summary>
        string ApprovalBtnRevoke { get; }

        /// <summary>
        /// Revoke
        /// </summary>
        string ApprovalBtnRevokeShort { get; }

        /// <summary>
        /// Keep
        /// </summary>
        string ApprovalBtnKeep { get; }

        /// <summary>
        /// Keep as Is
        /// </summary>
        string ApprovalBtnKeepAsIs { get; }

        /// <summary>
        /// Close the review without changing anything
        /// </summary>
        string ApprovalKeepTooltip { get; }

        /// <summary>
        /// Approve
        /// </summary>
        string ApprovalBtnApprove { get; }

        /// <summary>
        /// Go Back
        /// </summary>
        string ApprovalBtnGoBack { get; }

        /// <summary>
        /// Save Recipe
        /// </summary>
        string ApprovalBtnSaveRecipe { get; }

        /// <summary>
        /// Approve &amp; Save
        /// </summary>
        string ApprovalBtnApproveSave { get; }

        /// <summary>
        /// Approve Changes
        /// </summary>
        string ApprovalBtnApproveChanges { get; }

        /// <summary>
        /// Save Changes
        /// </summary>
        string ApprovalBtnSaveChanges { get; }

        /// <summary>
        /// Approve &amp; Enable
        /// </summary>
        string ApprovalBtnApproveEnable { get; }

        /// <summary>
        /// Unnamed Recipe
        /// </summary>
        string ApprovalUnnamedRecipe { get; }

        /// <summary>
        /// Unknown Recipe
        /// </summary>
        string ApprovalUnknownRecipe { get; }

        /// <summary>
        /// Unknown
        /// </summary>
        string ApprovalUnknown { get; }

        /// <summary>
        /// Unknown file path
        /// </summary>
        string ApprovalUnknownPath { get; }

        /// <summary>
        /// Built into Greenshot
        /// </summary>
        string ApprovalBuiltinPath { get; }

        /// <summary>
        /// Recipe configuration failed validation.
        /// </summary>
        string ApprovalFailedValidation { get; }

        /// <summary>
        /// No description provided.
        /// </summary>
        string ApprovalNoDescription { get; }

        /// <summary>
        /// Not signed
        /// </summary>
        string ApprovalNotSigned { get; }

        /// <summary>
        /// Signed by {0}
        /// </summary>
        string ApprovalSignedBy { get; }

        /// <summary>
        /// VALIDATION ERROR
        /// </summary>
        string ApprovalBadgeValidation { get; }

        /// <summary>
        /// PROPOSED BY AI
        /// </summary>
        string ApprovalBadgeAi { get; }

        /// <summary>
        /// YOUR CHANGE
        /// </summary>
        string ApprovalBadgeOwn { get; }

        /// <summary>
        /// MODIFIED ON DISK
        /// </summary>
        string ApprovalBadgeModified { get; }

        /// <summary>
        /// ALREADY APPROVED
        /// </summary>
        string ApprovalBadgeApproved { get; }

        /// <summary>
        /// NEW RECIPE
        /// </summary>
        string ApprovalBadgeNew { get; }

        /// <summary>
        /// BUILT-IN
        /// </summary>
        string ApprovalBadgeBuiltin { get; }

        /// <summary>
        /// NEW AUTOMATIC STEP
        /// </summary>
        string ApprovalBadgeExtNew { get; }

        /// <summary>
        /// Recipe Validation Failed
        /// </summary>
        string ApprovalValidationFailed { get; }

        /// <summary>
        /// Greenshot could not load or register this capture recipe because it contains configuration errors. Review the diagnostic details below:
        /// </summary>
        string ApprovalValidationDescription { get; }

        /// <summary>
        /// This recipe could not be registered or loaded due to configuration errors. Review the issues and diagnostic hints below:
        /// </summary>
        string ApprovalValidationInfo { get; }

        /// <summary>
        /// An unknown validation error occurred while loading the recipe.
        /// </summary>
        string ApprovalValidationUnknown { get; }

        /// <summary>
        /// Recommendation / Hint:
        /// </summary>
        string ApprovalHint { get; }

        /// <summary>
        /// Recipe Proposed by an AI Tool
        /// </summary>
        string ApprovalAiSubtitle { get; }

        /// <summary>
        /// An AI Tool Wants to Change a Recipe
        /// </summary>
        string ApprovalAiTitleChange { get; }

        /// <summary>
        /// An AI Tool Proposes a Recipe
        /// </summary>
        string ApprovalAiTitleNew { get; }

        /// <summary>
        /// This recipe was written by an AI tool, not by Greenshot or by you. Check what it does below (Greenshot describes it from the recipe itself) and switch on only the triggers you want.
        /// </summary>
        string ApprovalAiDescription { get; }

        /// <summary>
        /// Written by an AI tool
        /// </summary>
        string ApprovalAiWritten { get; }

        /// <summary>
        /// What the AI tool says you asked for (the AI's words):
        /// </summary>
        string ApprovalAiRequest { get; }

        /// <summary>
        /// The AI tool's explanation (the AI's words):
        /// </summary>
        string ApprovalAiExplanation { get; }

        /// <summary>
        /// Only "What it does" below comes from Greenshot itself, check it against what you asked for.
        /// </summary>
        string ApprovalAiCheck { get; }

        /// <summary>
        /// Approve Your Changes
        /// </summary>
        string ApprovalOwnSubtitle { get; }

        /// <summary>
        /// Your Change Needs a Decision
        /// </summary>
        string ApprovalOwnTitle { get; }

        /// <summary>
        /// You saved this recipe in the recipe editor. Most changes are approved without asking, but this one adds something that needs your decision: {0}
        /// </summary>
        string ApprovalOwnDescription { get; }

        /// <summary>
        /// Recipe Modification Detected
        /// </summary>
        string ApprovalModifiedSubtitle { get; }

        /// <summary>
        /// Recipe Changed Outside Greenshot
        /// </summary>
        string ApprovalModifiedTitle { get; }

        /// <summary>
        /// You approved this capture recipe before, but its file was changed outside Greenshot since then (by another program or by hand). Review the changes below before approving them.
        /// </summary>
        string ApprovalModifiedDescription { get; }

        /// <summary>
        /// File Content Modified on Disk
        /// </summary>
        string ApprovalModifiedAlert { get; }

        /// <summary>
        /// This recipe was previously approved, but its file content on disk has changed since it was last approved. Verify the changes below before re-authorizing execution:
        /// </summary>
        string ApprovalModifiedAlertText { get; }

        /// <summary>
        /// Previously Approved:
        /// </summary>
        string ApprovalPreviouslyApproved { get; }

        /// <summary>
        /// Previous SHA-256:
        /// </summary>
        string ApprovalPreviousSha { get; }

        /// <summary>
        /// New SHA-256:
        /// </summary>
        string ApprovalNewSha { get; }

        /// <summary>
        /// Recipe Security Review
        /// </summary>
        string ApprovalReviewSubtitle { get; }

        /// <summary>
        /// Capture Recipe Review
        /// </summary>
        string ApprovalReviewTitle { get; }

        /// <summary>
        /// Change which triggers may start this recipe and what it may do. "Keep as Is" or closing the window changes nothing.
        /// </summary>
        string ApprovalReviewDescription { get; }

        /// <summary>
        /// Recipe Security Approval
        /// </summary>
        string ApprovalNewSubtitle { get; }

        /// <summary>
        /// External Capture Recipe Detected
        /// </summary>
        string ApprovalNewTitle { get; }

        /// <summary>
        /// A new capture recipe file is requesting to be registered into Greenshot. Review its details, triggers, and execution steps before approving.
        /// </summary>
        string ApprovalNewDescription { get; }

        /// <summary>
        /// Recipe Details
        /// </summary>
        string ApprovalDetailsTitle { get; }

        /// <summary>
        /// What this built-in recipe does and which triggers start it.
        /// </summary>
        string ApprovalDetailsBuiltinDescription { get; }

        /// <summary>
        /// What this recipe does and what you approved for it. Nothing can be changed here, use Review in the settings or Permissions in the recipe manager for that.
        /// </summary>
        string ApprovalDetailsDescription { get; }

        /// <summary>
        /// Automatic Step Validation Failed
        /// </summary>
        string ApprovalExtValidationFailed { get; }

        /// <summary>
        /// Greenshot could not use this automatic step because it contains errors. Review the details below:
        /// </summary>
        string ApprovalExtValidationDescription { get; }

        /// <summary>
        /// Automatic Step Details
        /// </summary>
        string ApprovalExtDetailsTitle { get; }

        /// <summary>
        /// What this automatic step does, which recipes it changes and what you approved for it.
        /// </summary>
        string ApprovalExtDetailsDescription { get; }

        /// <summary>
        /// Automatic Step Proposed by an AI Tool
        /// </summary>
        string ApprovalExtAiSubtitle { get; }

        /// <summary>
        /// An AI Tool Proposes an Automatic Step
        /// </summary>
        string ApprovalExtAiTitle { get; }

        /// <summary>
        /// This automatic step was written by an AI tool. It changes other recipes, the built-in ones included: check which ones and what it adds below. It starts switched off, switch it on in Settings &gt; Recipes.
        /// </summary>
        string ApprovalExtAiDescription { get; }

        /// <summary>
        /// Automatic Step Modification Detected
        /// </summary>
        string ApprovalExtModifiedSubtitle { get; }

        /// <summary>
        /// Automatic Step Changed Outside Greenshot
        /// </summary>
        string ApprovalExtModifiedTitle { get; }

        /// <summary>
        /// You approved this automatic step before, but its file was changed outside Greenshot since then. Until you approve the changes, it doesn't change any recipe.
        /// </summary>
        string ApprovalExtModifiedDescription { get; }

        /// <summary>
        /// Automatic Step Review
        /// </summary>
        string ApprovalExtReviewTitle { get; }

        /// <summary>
        /// Change what this automatic step may do. "Keep as Is" or closing the window changes nothing.
        /// </summary>
        string ApprovalExtReviewDescription { get; }

        /// <summary>
        /// You saved this automatic step in the recipe editor. Most changes are approved without asking, but this one needs your decision: {0}
        /// </summary>
        string ApprovalExtOwnDescription { get; }

        /// <summary>
        /// Automatic Step Approval
        /// </summary>
        string ApprovalExtNewSubtitle { get; }

        /// <summary>
        /// Automatic Step File Detected
        /// </summary>
        string ApprovalExtNewTitle { get; }

        /// <summary>
        /// A file wants to add an automatic step to other recipes, the built-in ones included. Review which recipes it changes and what it adds before approving.
        /// </summary>
        string ApprovalExtNewDescription { get; }

        /// <summary>
        /// Changes other recipes
        /// </summary>
        string ApprovalExtReachTitle { get; }

        /// <summary>
        /// An automatic step adds its steps to other recipes, the built-in ones included. It has no triggers of its own.
        /// </summary>
        string ApprovalExtReachText { get; }

        /// <summary>
        /// Replaces the built-in recipe "{0}"
        /// </summary>
        string ApprovalChangesReplacesBuiltin { get; }

        /// <summary>
        /// Changes the recipe "{0}"
        /// </summary>
        string ApprovalChangesRecipe { get; }

        /// <summary>
        /// What changed since the last approval
        /// </summary>
        string ApprovalChangesSince { get; }

        /// <summary>
        /// The previous version is not known, review the whole recipe
        /// </summary>
        string ApprovalChangesUnknown { get; }

        /// <summary>
        /// No change in what the recipe does (only details like names or layout).
        /// </summary>
        string ApprovalNoChange { get; }

        /// <summary>
        /// Allow this recipe to run programs on this PC
        /// </summary>
        string ApprovalGateExternalCommand { get; }

        /// <summary>
        /// Allow this recipe to upload captures to the internet
        /// </summary>
        string ApprovalGateNetwork { get; }

        /// <summary>
        /// Allow this recipe to read or write these files and folders
        /// </summary>
        string ApprovalGateFiles { get; }

        /// <summary>
        /// Allow these actions
        /// </summary>
        string ApprovalGateCustom { get; }

        /// <summary>
        /// A program started by a recipe can do anything you can do on this PC.
        /// </summary>
        string ApprovalGateExternalCommandExplanation { get; }

        /// <summary>
        /// What is uploaded leaves this PC, the service decides who can see it.
        /// </summary>
        string ApprovalGateNetworkExplanation { get; }

        /// <summary>
        /// The recipe works with files outside Greenshot's usual output folder.
        /// </summary>
        string ApprovalGateFilesExplanation { get; }

        /// <summary>
        /// Hotkey: {0}
        /// </summary>
        string ApprovalTriggerHotkey { get; }

        /// <summary>
        /// Context menu: "{0}"
        /// </summary>
        string ApprovalTriggerContextmenu { get; }

        /// <summary>
        /// Editor: "{0}"
        /// </summary>
        string ApprovalTriggerEditor { get; }

        /// <summary>
        /// Clipboard Monitor
        /// </summary>
        string ApprovalTriggerClipboard { get; }

        /// <summary>
        /// Manual / Triggerless (Invoked via CLI / API)
        /// </summary>
        string ApprovalTriggerManual { get; }

        /// <summary>
        /// {0} (disabled in the recipe)
        /// </summary>
        string ApprovalTriggerDisabled { get; }

        /// <summary>
        /// The triggers start switched off. Tick each one that may start this recipe: a trigger you leave off stays off (you can switch it on later with Permissions in the recipe manager). Without a trigger you can still run the recipe from the recipe list.
        /// </summary>
        string ApprovalTriggersOffHint { get; }

        /// <summary>
        /// Switch off the triggers you don't want.
        /// </summary>
        string ApprovalTriggersOnHint { get; }

        /// <summary>
        /// Off: it won't start the recipe until you tick it.
        /// </summary>
        string ApprovalTriggerOff { get; }

        /// <summary>
        /// Triggers Left Off
        /// </summary>
        string ApprovalLeftOffTitle { get; }

        /// <summary>
        /// This trigger stays off, so it won't start the recipe:
        /// </summary>
        string ApprovalLeftOffOne { get; }

        /// <summary>
        /// These triggers stay off, so they won't start the recipe:
        /// </summary>
        string ApprovalLeftOffMany { get; }

        /// <summary>
        /// You can switch triggers on later with Permissions in the recipe manager, and run the recipe from the recipe list.
        /// </summary>
        string ApprovalLeftOffLater { get; }

        /// <summary>
        /// Revoke the approval of "{0}"? It stops running right away and isn't loaded again. Its file stays where it is: open it again to review and approve it.
        /// </summary>
        string ApprovalRevokeQuestion { get; }

        /// <summary>
        /// Show in Explorer
        /// </summary>
        string ApprovalShowInExplorer { get; }

        /// <summary>
        /// Open recipe file location in Windows Explorer
        /// </summary>
        string ApprovalShowInExplorerTooltip { get; }

        /// <summary>
        /// Explorer
        /// </summary>
        string ApprovalExplorer { get; }

        /// <summary>
        /// Show this file in Windows Explorer
        /// </summary>
        string ApprovalExplorerTooltip { get; }

        /// <summary>
        /// View Recipe JSON
        /// </summary>
        string ApprovalViewRecipeJson { get; }

        /// <summary>
        /// View JSON
        /// </summary>
        string ApprovalViewJson { get; }

        /// <summary>
        /// Inspect the raw recipe JSON definition inside this window
        /// </summary>
        string ApprovalViewJsonTooltip { get; }

        /// <summary>
        /// View Changes
        /// </summary>
        string ApprovalViewChanges { get; }

        /// <summary>
        /// Show the lines that changed since the last approval
        /// </summary>
        string ApprovalViewChangesTooltip { get; }

        /// <summary>
        /// View changed lines
        /// </summary>
        string ApprovalViewChangedLines { get; }

        /// <summary>
        /// Show the changed lines of the recipe JSON
        /// </summary>
        string ApprovalViewChangedLinesTooltip { get; }

        /// <summary>
        /// Recipe ID:
        /// </summary>
        string ApprovalRecipeId { get; }

        /// <summary>
        /// File Path:
        /// </summary>
        string ApprovalFilePath { get; }

        /// <summary>
        /// What starts it
        /// </summary>
        string ApprovalWhatStartsIt { get; }

        /// <summary>
        /// What it does
        /// </summary>
        string ApprovalWhatItDoes { get; }

        /// <summary>
        /// Described by Greenshot from the recipe itself.
        /// </summary>
        string ApprovalDescribedBy { get; }

        /// <summary>
        /// This recipe needs your permission
        /// </summary>
        string ApprovalPermissionTitle { get; }

        /// <summary>
        /// Allow each of these, or reject the recipe:
        /// </summary>
        string ApprovalPermissionText { get; }

        /// <summary>
        /// Open in the recipe editor after saving
        /// </summary>
        string ApprovalOpenInEditor { get; }

        /// <summary>
        /// Edit Recipe File
        /// </summary>
        string ApprovalEditFile { get; }

        /// <summary>
        /// Open recipe file in default editor to fix errors
        /// </summary>
        string ApprovalEditFileTooltip { get; }

        /// <summary>
        /// Recipe JSON Definition
        /// </summary>
        string ApprovalJsonTitle { get; }

        /// <summary>
        /// Changes (- before, + after)
        /// </summary>
        string ApprovalChangesOverlayTitle { get; }

        /// <summary>
        /// Close JSON viewer
        /// </summary>
        string ApprovalCloseViewer { get; }

        /// <summary>
        /// Close JSON viewer (Esc)
        /// </summary>
        string ApprovalCloseViewerTooltip { get; }

        /// <summary>
        /// Copy to Clipboard
        /// </summary>
        string ApprovalCopyClipboard { get; }

        /// <summary>
        /// Copy recipe JSON to clipboard
        /// </summary>
        string ApprovalCopyJsonTooltip { get; }

        /// <summary>
        /// Done
        /// </summary>
        string ApprovalDone { get; }

        /// <summary>
        /// Recipe file is too large to preview (&gt; 1MB).
        /// </summary>
        string ApprovalJsonTooLarge { get; }

        /// <summary>
        /// Recipe content unavailable.
        /// </summary>
        string ApprovalJsonUnavailable { get; }

        /// <summary>
        /// Error reading recipe: {0}
        /// </summary>
        string ApprovalJsonError { get; }

        /// <summary>
        /// The stepType specified is not registered in core Greenshot. If it is provided by a plugin or extension (e.g. Jira, OCR, etc.), verify that the plugin is installed in the Plugins folder and enabled under Greenshot Settings &gt; Plugins.
        /// </summary>
        string ApprovalHintStepType { get; }

        /// <summary>
        /// Valid SourceType values are: {0}.
        /// </summary>
        string ApprovalHintSourceType { get; }

        /// <summary>
        /// Verify that the target node ID matches an existing node's 'id' in the recipe's 'nodes' pipeline list.
        /// </summary>
        string ApprovalHintTransition { get; }

        /// <summary>
        /// Each node in the recipe pipeline must have a unique 'id' identifier.
        /// </summary>
        string ApprovalHintDuplicateId { get; }

        /// <summary>
        /// Recipes must form a directed acyclic graph (DAG). Ensure transitions and fallbacks do not loop back to earlier nodes.
        /// </summary>
        string ApprovalHintCircular { get; }

        /// <summary>
        /// Add at least one execution node to the 'nodes' array of the recipe.
        /// </summary>
        string ApprovalHintNoNodes { get; }

        /// <summary>
        /// Every recipe requires a unique 'id' and a descriptive 'name' property.
        /// </summary>
        string ApprovalHintIdName { get; }

        /// <summary>
        /// Check the recipe JSON syntax for unescaped quotes, trailing commas, or mismatched brackets.
        /// </summary>
        string ApprovalHintJsonSyntax { get; }

        /// <summary>
        /// Greenshot - Decision Prompt
        /// </summary>
        string PromptWindowTitle { get; }

        /// <summary>
        /// Decision Required
        /// </summary>
        string PromptTitle { get; }

        /// <summary>
        /// Please select how you would like to proceed:
        /// </summary>
        string PromptMessage { get; }

        /// <summary>
        /// Auto-selecting default choice in {0}s...
        /// </summary>
        string PromptTimeout { get; }

        /// <summary>
        /// Yes, Proceed
        /// </summary>
        string PromptYes { get; }

        /// <summary>
        /// No, Cancel
        /// </summary>
        string PromptNo { get; }

        /// <summary>
        /// Core Destinations
        /// </summary>
        string PickerCoreDestinations { get; }

        /// <summary>
        /// Destinations
        /// </summary>
        string PickerDestinations { get; }

        /// <summary>
        /// Run Recipe
        /// </summary>
        string PickerRunRecipe { get; }

        /// <summary>
        /// Click to view full preview (Esc to exit)
        /// </summary>
        string PickerPreviewTooltip { get; }

        /// <summary>
        /// Zoom Preview
        /// </summary>
        string PickerZoomPreview { get; }

        /// <summary>
        /// Click anywhere or press Esc to return
        /// </summary>
        string PickerFullscreenHint { get; }

        /// <summary>
        /// Select an alternative destination below, forward to another recipe, or retry:
        /// </summary>
        string PickerErrorSubtitle { get; }

        /// <summary>
        /// Switch to Light Mode (T)
        /// </summary>
        string PickerThemeLight { get; }

        /// <summary>
        /// Switch to Dark Mode (T)
        /// </summary>
        string PickerThemeDark { get; }

        /// <summary>
        /// Auto-closing in {0}s...
        /// </summary>
        string PickerAutoClose { get; }

        /// <summary>
        /// Retry
        /// </summary>
        string PickerRetry { get; }

        /// <summary>
        /// Search destinations
        /// </summary>
        string PickerSearch { get; }

        /// <summary>
        /// Copy image to clipboard
        /// </summary>
        string PickerClipboardSub { get; }

        /// <summary>
        /// Annotate and export
        /// </summary>
        string PickerEditorSub { get; }

        /// <summary>
        /// Save Directly
        /// </summary>
        string PickerFile { get; }

        /// <summary>
        /// Save to folder
        /// </summary>
        string PickerFileSub { get; }

        /// <summary>
        /// Save As...
        /// </summary>
        string PickerFileas { get; }

        /// <summary>
        /// Choose folder &amp; format
        /// </summary>
        string PickerFileasSub { get; }

        /// <summary>
        /// Send to printer
        /// </summary>
        string PickerPrinterSub { get; }

        /// <summary>
        /// OCR Text
        /// </summary>
        string PickerOcr { get; }

        /// <summary>
        /// Extract text from image
        /// </summary>
        string PickerOcrSub { get; }

        /// <summary>
        /// Windows Share
        /// </summary>
        string PickerShare { get; }

        /// <summary>
        /// Share with apps &amp; contacts
        /// </summary>
        string PickerShareSub { get; }

        /// <summary>
        /// New email attachment
        /// </summary>
        string PickerEmailSub { get; }

        /// <summary>
        /// Insert into document
        /// </summary>
        string PickerWordSub { get; }

        /// <summary>
        /// Insert into worksheet
        /// </summary>
        string PickerExcelSub { get; }

        /// <summary>
        /// Insert into presentation
        /// </summary>
        string PickerPowerpointSub { get; }

        /// <summary>
        /// Send to notebook
        /// </summary>
        string PickerOnenoteSub { get; }

        /// <summary>
        /// QR Code Actions
        /// </summary>
        string PickerQr { get; }

        /// <summary>
        /// Detect &amp; decode QR codes
        /// </summary>
        string PickerQrSub { get; }

        /// <summary>
        /// Open in Paint
        /// </summary>
        string PickerPaintSub { get; }

        /// <summary>
        /// Upload &amp; copy link
        /// </summary>
        string PickerUploadSub { get; }

        /// <summary>
        /// Attach to issue
        /// </summary>
        string PickerJiraSub { get; }

        /// <summary>
        /// Attach to page
        /// </summary>
        string PickerConfluenceSub { get; }

        /// <summary>
        /// Cloud upload
        /// </summary>
        string PickerCloudSub { get; }

        /// <summary>
        /// Export destination
        /// </summary>
        string PickerExportSub { get; }
    }
}
