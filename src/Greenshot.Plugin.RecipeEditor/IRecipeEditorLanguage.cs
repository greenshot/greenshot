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

namespace Greenshot.Plugin.RecipeEditor
{
    /// <summary>
    /// The texts of the [RecipeEditor] section of greenshot.recipeeditor.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("RecipeEditor", ModuleName = "recipeeditor")]
    public interface IRecipeEditorLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// ⭐ Toggle Start Step
        /// </summary>
        string ToggleStartStep { get; }

        /// <summary>
        /// 🗑️ Delete Step
        /// </summary>
        string DeleteStep { get; }

        /// <summary>
        /// START
        /// </summary>
        string BadgeStart { get; }

        /// <summary>
        /// No automatic step plugs in
        /// </summary>
        string SlotNoExtensions { get; }

        /// <summary>
        /// Branches:
        /// </summary>
        string Branches { get; }

        /// <summary>
        /// Review approval...
        /// </summary>
        string ReviewApproval { get; }

        /// <summary>
        /// Switch triggers and permissions of this recipe on or off
        /// </summary>
        string TipReviewApproval { get; }

        /// <summary>
        /// Recipe
        /// </summary>
        string Recipe { get; }

        /// <summary>
        /// Click to activate or deactivate this recipe in Greenshot
        /// </summary>
        string TipToggleRecipeActive { get; }

        /// <summary>
        /// Manage Recipes...
        /// </summary>
        string ManageRecipes { get; }

        /// <summary>
        /// Open Visual Recipe Manager (Ctrl+M)
        /// </summary>
        string TipManageRecipes { get; }

        /// <summary>
        /// New
        /// </summary>
        string BtnNew { get; }

        /// <summary>
        /// Create new recipe
        /// </summary>
        string TipNewRecipe { get; }

        /// <summary>
        /// New Automatic Step
        /// </summary>
        string BtnNewExtension { get; }

        /// <summary>
        /// Create a new automatic step: steps Greenshot adds to other recipes (like the built-in border)
        /// </summary>
        string TipNewExtension { get; }

        /// <summary>
        /// Open...
        /// </summary>
        string BtnOpen { get; }

        /// <summary>
        /// Open recipe file from disk
        /// </summary>
        string TipOpenRecipe { get; }

        /// <summary>
        /// Save
        /// </summary>
        string BtnSave { get; }

        /// <summary>
        /// Save current recipe
        /// </summary>
        string TipSaveRecipe { get; }

        /// <summary>
        /// Save As...
        /// </summary>
        string BtnSaveAs { get; }

        /// <summary>
        /// Save recipe to new file
        /// </summary>
        string TipSaveRecipeAs { get; }

        /// <summary>
        /// ↶ Undo
        /// </summary>
        string BtnUndo { get; }

        /// <summary>
        /// Undo the last change (Ctrl+Z)
        /// </summary>
        string TipUndo { get; }

        /// <summary>
        /// ↷ Redo
        /// </summary>
        string BtnRedo { get; }

        /// <summary>
        /// Redo the change (Ctrl+Y)
        /// </summary>
        string TipRedo { get; }

        /// <summary>
        /// Auto-Layout
        /// </summary>
        string BtnAutoLayout { get; }

        /// <summary>
        /// Auto-organize DAG layout
        /// </summary>
        string TipAutoLayout { get; }

        /// <summary>
        /// View / Edit Raw Recipe JSON
        /// </summary>
        string TipEditJson { get; }

        /// <summary>
        /// Generate Mermaid Diagram DSL for documentation
        /// </summary>
        string TipMermaid { get; }

        /// <summary>
        /// ▶ Test Run Recipe
        /// </summary>
        string BtnTestRun { get; }

        /// <summary>
        /// Execute capture recipe in Greenshot
        /// </summary>
        string TipTestRun { get; }

        /// <summary>
        /// STEP TOOLBOX
        /// </summary>
        string HeaderStepToolbox { get; }

        /// <summary>
        /// Select a step or an arrow first: the new step goes right after it, or into the arrow.
        /// </summary>
        string HintToolbox { get; }

        /// <summary>
        /// ACQUISITION
        /// </summary>
        string HeaderAcquisition { get; }

        /// <summary>
        /// Source (Acquisition)
        /// </summary>
        string StepSource { get; }

        /// <summary>
        /// Interactive Selection
        /// </summary>
        string StepInteractiveSelection { get; }

        /// <summary>
        /// Record Video (WGC)
        /// </summary>
        string StepRecordVideo { get; }

        /// <summary>
        /// FILTERS &amp; EFFECTS
        /// </summary>
        string HeaderFiltersEffects { get; }

        /// <summary>
        /// Effect (Border/Shadow/Filters)
        /// </summary>
        string StepEffect { get; }

        /// <summary>
        /// Text Effect / DLP
        /// </summary>
        string StepTextEffect { get; }

        /// <summary>
        /// ANNOTATIONS &amp; DATA
        /// </summary>
        string HeaderAnnotationsData { get; }

        /// <summary>
        /// Annotation
        /// </summary>
        string StepAnnotation { get; }

        /// <summary>
        /// Set Variable
        /// </summary>
        string StepSetVariable { get; }

        /// <summary>
        /// FLOW &amp; FEEDBACK
        /// </summary>
        string HeaderFlowFeedback { get; }

        /// <summary>
        /// Conditional Branch
        /// </summary>
        string StepConditional { get; }

        /// <summary>
        /// Slot for Automatic Steps
        /// </summary>
        string StepSlot { get; }

        /// <summary>
        /// A place where automatic steps (border, drop shadow, caption, ...) are added
        /// </summary>
        string TipStepSlot { get; }

        /// <summary>
        /// Shutter Sound
        /// </summary>
        string StepShutterSound { get; }

        /// <summary>
        /// Notification
        /// </summary>
        string StepNotification { get; }

        /// <summary>
        /// Processors / OCR
        /// </summary>
        string StepProcessors { get; }

        /// <summary>
        /// Standard Output (CLI)
        /// </summary>
        string StepStdout { get; }

        /// <summary>
        /// Standard Error / Exit Code (CLI)
        /// </summary>
        string StepStderr { get; }

        /// <summary>
        /// OUTPUT &amp; DESTINATIONS
        /// </summary>
        string HeaderOutputDestinations { get; }

        /// <summary>
        /// Dynamic Destination (WPF)
        /// </summary>
        string StepDynamicDestination { get; }

        /// <summary>
        /// Save to File
        /// </summary>
        string StepSaveFile { get; }

        /// <summary>
        /// System Clipboard
        /// </summary>
        string StepClipboard { get; }

        /// <summary>
        /// Printer
        /// </summary>
        string StepPrinter { get; }

        /// <summary>
        /// E-Mail
        /// </summary>
        string StepEmail { get; }

        /// <summary>
        /// Multi-Destinations
        /// </summary>
        string StepDestinations { get; }

        /// <summary>
        /// PLUGINS
        /// </summary>
        string HeaderPlugins { get; }

        /// <summary>
        /// External Command
        /// </summary>
        string StepExternalCommand { get; }

        /// <summary>
        /// Confluence Page
        /// </summary>
        string StepConfluence { get; }

        /// <summary>
        /// ZXing Barcode / QR
        /// </summary>
        string StepZxing { get; }

        /// <summary>
        /// Box Cloud Storage
        /// </summary>
        string StepBox { get; }

        /// <summary>
        /// Click to select connection (Press Delete or Backspace to remove)
        /// </summary>
        string TipConnection { get; }

        /// <summary>
        /// 🗑️ Delete Connection
        /// </summary>
        string DeleteConnection { get; }

        /// <summary>
        /// Recipe JSON Definition
        /// </summary>
        string RecipeJson { get; }

        /// <summary>
        /// Apply Changes
        /// </summary>
        string BtnApplyChanges { get; }

        /// <summary>
        /// Mermaid Workflow DSL
        /// </summary>
        string MermaidDsl { get; }

        /// <summary>
        /// Copy this Mermaid syntax to use in documentation, markdown files, or PR descriptions.
        /// </summary>
        string HintMermaid { get; }

        /// <summary>
        /// 📋 Copy Mermaid Code
        /// </summary>
        string BtnCopyMermaid { get; }

        /// <summary>
        /// ⚙️ Step Inspector
        /// </summary>
        string TabStepInspector { get; }

        /// <summary>
        /// CONNECTION SELECTED
        /// </summary>
        string HeaderConnectionSelected { get; }

        /// <summary>
        /// FROM
        /// </summary>
        string ConnectionFrom { get; }

        /// <summary>
        /// TO
        /// </summary>
        string ConnectionTo { get; }

        /// <summary>
        /// Remove this connection (or press Delete / Backspace)
        /// </summary>
        string TipDeleteConnection { get; }

        /// <summary>
        /// Tip: Press Delete or Backspace key to remove the connection.
        /// </summary>
        string HintDeleteConnection { get; }

        /// <summary>
        /// Flow Entry Point
        /// </summary>
        string FlowEntryPoint { get; }

        /// <summary>
        /// Defines if this step starts the recipe execution
        /// </summary>
        string HintFlowEntryPoint { get; }

        /// <summary>
        /// ⭐ START STEP
        /// </summary>
        string BadgeStartStep { get; }

        /// <summary>
        /// ⭐ Mark as Start Step
        /// </summary>
        string BtnMarkStart { get; }

        /// <summary>
        /// Add this step as a parallel entry point
        /// </summary>
        string TipMarkStart { get; }

        /// <summary>
        /// ⭐ Remove Start Step
        /// </summary>
        string BtnRemoveStart { get; }

        /// <summary>
        /// Remove this step from start steps
        /// </summary>
        string TipRemoveStart { get; }

        /// <summary>
        /// Delete this step from the recipe
        /// </summary>
        string TipDeleteStep { get; }

        /// <summary>
        /// Node ID
        /// </summary>
        string NodeId { get; }

        /// <summary>
        /// Unique identifier for this step. Editing this ID updates all DAG references.
        /// </summary>
        string TipNodeId { get; }

        /// <summary>
        /// Step Type
        /// </summary>
        string StepType { get; }

        /// <summary>
        /// Display Name / Label
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// STEP PARAMETERS
        /// </summary>
        string HeaderStepParameters { get; }

        /// <summary>
        /// Source Type
        /// </summary>
        string SourceType { get; }

        /// <summary>
        /// Region (Interactive select)
        /// </summary>
        string SourceRegion { get; }

        /// <summary>
        /// Window (Window picker)
        /// </summary>
        string SourceWindow { get; }

        /// <summary>
        /// ActiveWindow (Focused window)
        /// </summary>
        string SourceActiveWindow { get; }

        /// <summary>
        /// FullScreen (All displays)
        /// </summary>
        string SourceFullscreen { get; }

        /// <summary>
        /// LastRegion (Repeat region)
        /// </summary>
        string SourceLastRegion { get; }

        /// <summary>
        /// Clipboard (From clipboard image)
        /// </summary>
        string SourceClipboard { get; }

        /// <summary>
        /// File (Load from disk)
        /// </summary>
        string SourceFile { get; }

        /// <summary>
        /// TextOcr (Screen OCR capture)
        /// </summary>
        string SourceTextOcr { get; }

        /// <summary>
        /// CurrentEditor (Current editor image)
        /// </summary>
        string SourceCurrentEditor { get; }

        /// <summary>
        /// Browser extension (capture sent by the browser extension)
        /// </summary>
        string SourceBrowserExtension { get; }

        /// <summary>
        /// File (empty = the Filename variable, e.g. from the command line)
        /// </summary>
        string SourceFilePath { get; }

        /// <summary>
        /// Window Title (exact)
        /// </summary>
        string WindowTitleExact { get; }

        /// <summary>
        /// Window Title Pattern (Regex)
        /// </summary>
        string WindowTitlePattern { get; }

        /// <summary>
        /// Process Name
        /// </summary>
        string ProcessName { get; }

        /// <summary>
        /// Match case (title and process name)
        /// </summary>
        string MatchCase { get; }

        /// <summary>
        /// Capture Mouse Cursor
        /// </summary>
        string CaptureMouseCursor { get; }

        /// <summary>
        /// Always True
        /// </summary>
        string AlwaysTrue { get; }

        /// <summary>
        /// Always False
        /// </summary>
        string AlwaysFalse { get; }

        /// <summary>
        /// Screen Capture Mode
        /// </summary>
        string ScreenCaptureMode { get; }

        /// <summary>
        /// Auto (Screen with the mouse)
        /// </summary>
        string ScreenAuto { get; }

        /// <summary>
        /// All Displays
        /// </summary>
        string ScreenAll { get; }

        /// <summary>
        /// Fixed Screen (from Configuration)
        /// </summary>
        string ScreenFixed { get; }

        /// <summary>
        /// Align Multi-DPI Displays
        /// </summary>
        string AlignDpi { get; }

        /// <summary>
        /// Enabled (Align coordinates)
        /// </summary>
        string AlignDpiEnabled { get; }

        /// <summary>
        /// Disabled
        /// </summary>
        string Disabled { get; }

        /// <summary>
        /// Delay (ms)
        /// </summary>
        string DelayMs { get; }

        /// <summary>
        /// Selection Mode
        /// </summary>
        string SelectionMode { get; }

        /// <summary>
        /// Region (Rubberband rectangle)
        /// </summary>
        string SelectionRegion { get; }

        /// <summary>
        /// Window (Hover &amp; click window)
        /// </summary>
        string SelectionWindow { get; }

        /// <summary>
        /// Text (OCR selection box)
        /// </summary>
        string SelectionText { get; }

        /// <summary>
        /// Window Snapping
        /// </summary>
        string WindowSnapping { get; }

        /// <summary>
        /// Enabled (Snap to Windows)
        /// </summary>
        string WindowSnappingEnabled { get; }

        /// <summary>
        /// Source / Target Type
        /// </summary>
        string SourceTargetType { get; }

        /// <summary>
        /// Active Window (Currently focused)
        /// </summary>
        string TargetActiveWindow { get; }

        /// <summary>
        /// Window (Match title / process)
        /// </summary>
        string TargetWindow { get; }

        /// <summary>
        /// Monitor (Display)
        /// </summary>
        string TargetMonitor { get; }

        /// <summary>
        /// Region (Crop coordinates)
        /// </summary>
        string TargetRegion { get; }

        /// <summary>
        /// Process Name (e.g. notepad)
        /// </summary>
        string ProcessNameExample { get; }

        /// <summary>
        /// Record until target window closes
        /// </summary>
        string RecordUntilWindowCloses { get; }

        /// <summary>
        /// Region X (px)
        /// </summary>
        string RegionX { get; }

        /// <summary>
        /// Region Y (px)
        /// </summary>
        string RegionY { get; }

        /// <summary>
        /// Region Width
        /// </summary>
        string RegionWidth { get; }

        /// <summary>
        /// Region Height
        /// </summary>
        string RegionHeight { get; }

        /// <summary>
        /// Monitor Index (-1 = Primary display)
        /// </summary>
        string MonitorIndex { get; }

        /// <summary>
        /// Duration (s, 0=unlimited)
        /// </summary>
        string Duration { get; }

        /// <summary>
        /// Countdown Delay (ms)
        /// </summary>
        string CountdownDelayMs { get; }

        /// <summary>
        /// Output MP4 File Path
        /// </summary>
        string OutputMp4Path { get; }

        /// <summary>
        /// Preset
        /// </summary>
        string Preset { get; }

        /// <summary>
        /// Balanced (30 FPS, 2.5 Mbps)
        /// </summary>
        string PresetBalanced { get; }

        /// <summary>
        /// Small Size (15 FPS, 800 kbps, 720p)
        /// </summary>
        string PresetSmall { get; }

        /// <summary>
        /// High Quality (60 FPS, 8 Mbps)
        /// </summary>
        string PresetHighQuality { get; }

        /// <summary>
        /// Gaming (144 FPS, 25 Mbps)
        /// </summary>
        string PresetGaming { get; }

        /// <summary>
        /// Custom (Manual values below)
        /// </summary>
        string PresetCustom { get; }

        /// <summary>
        /// Format / Codec
        /// </summary>
        string FormatCodec { get; }

        /// <summary>
        /// Frame Rate (FPS)
        /// </summary>
        string FrameRate { get; }

        /// <summary>
        /// Bitrate (bps)
        /// </summary>
        string Bitrate { get; }

        /// <summary>
        /// Width (0=orig)
        /// </summary>
        string VideoWidth { get; }

        /// <summary>
        /// Height (0=orig)
        /// </summary>
        string VideoHeight { get; }

        /// <summary>
        /// Scale Factor
        /// </summary>
        string ScaleFactor { get; }

        /// <summary>
        /// Audio Recording Source
        /// </summary>
        string AudioSource { get; }

        /// <summary>
        /// None (Mute video)
        /// </summary>
        string AudioNone { get; }

        /// <summary>
        /// Microphone (WASAPI input)
        /// </summary>
        string AudioMicrophone { get; }

        /// <summary>
        /// System Audio (Speaker loopback)
        /// </summary>
        string AudioSystem { get; }

        /// <summary>
        /// Window Resize Behavior
        /// </summary>
        string WindowResizeBehavior { get; }

        /// <summary>
        /// Letterbox (Fixed Canvas, centered)
        /// </summary>
        string ResizeLetterbox { get; }

        /// <summary>
        /// Crop to Canvas
        /// </summary>
        string ResizeCrop { get; }

        /// <summary>
        /// Scale Content
        /// </summary>
        string ResizeScale { get; }

        /// <summary>
        /// Color Mode
        /// </summary>
        string ColorMode { get; }

        /// <summary>
        /// Full Color (32-bit BGRA)
        /// </summary>
        string ColorFull32bit { get; }

        /// <summary>
        /// Grayscale (Monochrome)
        /// </summary>
        string ColorGrayscaleMono { get; }

        /// <summary>
        /// Show OS Capture Border (Yellow WGC border)
        /// </summary>
        string ShowWgcBorder { get; }

        /// <summary>
        /// Prevent System Sleep while recording
        /// </summary>
        string PreventSleep { get; }

        /// <summary>
        /// Auto-pause when Windows locked (Win+L)
        /// </summary>
        string AutoPauseLocked { get; }

        /// <summary>
        /// Effect Type
        /// </summary>
        string EffectType { get; }

        /// <summary>
        /// Border / Frame
        /// </summary>
        string EffectBorder { get; }

        /// <summary>
        /// Drop Shadow
        /// </summary>
        string EffectDropShadow { get; }

        /// <summary>
        /// Torn Edge
        /// </summary>
        string EffectTornEdge { get; }

        /// <summary>
        /// Monochrome (B&amp;W)
        /// </summary>
        string EffectMonochrome { get; }

        /// <summary>
        /// Adjust (Brightness/Contrast/Gamma)
        /// </summary>
        string EffectAdjust { get; }

        /// <summary>
        /// Rotate
        /// </summary>
        string EffectRotate { get; }

        /// <summary>
        /// Resize Image
        /// </summary>
        string EffectResizeImage { get; }

        /// <summary>
        /// Resize Canvas (Add Padding)
        /// </summary>
        string EffectResizeCanvas { get; }

        /// <summary>
        /// Reduce Colors
        /// </summary>
        string EffectReduceColors { get; }

        /// <summary>
        /// Border Width (px)
        /// </summary>
        string BorderWidth { get; }

        /// <summary>
        /// Border Color (hex value or name)
        /// </summary>
        string BorderColor { get; }

        /// <summary>
        /// Click to pick color...
        /// </summary>
        string ClickPickColor { get; }

        /// <summary>
        /// Pick color
        /// </summary>
        string PickColor { get; }

        /// <summary>
        /// Shadow Size (px)
        /// </summary>
        string ShadowSize { get; }

        /// <summary>
        /// Shadow Darkness (0.0 - 1.0)
        /// </summary>
        string ShadowDarkness { get; }

        /// <summary>
        /// Offset X (px)
        /// </summary>
        string OffsetXPx { get; }

        /// <summary>
        /// Offset Y (px)
        /// </summary>
        string OffsetYPx { get; }

        /// <summary>
        /// Tooth Height (px)
        /// </summary>
        string ToothHeight { get; }

        /// <summary>
        /// H-Tooth Range
        /// </summary>
        string HToothRange { get; }

        /// <summary>
        /// V-Tooth Range
        /// </summary>
        string VToothRange { get; }

        /// <summary>
        /// Active Edges (top,right,bottom,left)
        /// </summary>
        string ActiveEdges { get; }

        /// <summary>
        /// Generate Drop Shadow
        /// </summary>
        string GenerateDropShadow { get; }

        /// <summary>
        /// B&amp;W Threshold (0 - 255)
        /// </summary>
        string BwThreshold { get; }

        /// <summary>
        /// Brightness (e.g. 1.0 = normal, 1.2 = lighter, 0.8 = darker)
        /// </summary>
        string Brightness { get; }

        /// <summary>
        /// Contrast (e.g. 1.0 = normal, 1.3 = higher contrast)
        /// </summary>
        string Contrast { get; }

        /// <summary>
        /// Gamma (e.g. 1.0 = normal)
        /// </summary>
        string Gamma { get; }

        /// <summary>
        /// Rotation Angle (Degrees clockwise)
        /// </summary>
        string RotationAngle { get; }

        /// <summary>
        /// Target Width (px)
        /// </summary>
        string TargetWidth { get; }

        /// <summary>
        /// Target Height (px)
        /// </summary>
        string TargetHeight { get; }

        /// <summary>
        /// Scale Percentage (% or 0 for exact WxH)
        /// </summary>
        string ScalePercentage { get; }

        /// <summary>
        /// Maintain Aspect Ratio
        /// </summary>
        string MaintainAspectRatio { get; }

        /// <summary>
        /// Padding / Margin (px on all sides)
        /// </summary>
        string PaddingMargin { get; }

        /// <summary>
        /// Background Color (hex value or name)
        /// </summary>
        string BackgroundColor { get; }

        /// <summary>
        /// Target Colors (2 - 256)
        /// </summary>
        string TargetColors { get; }

        /// <summary>
        /// Matte Background Color
        /// </summary>
        string MatteColor { get; }

        /// <summary>
        /// Inverts all RGB pixel colors across the capture surface.
        /// </summary>
        string HintInvert { get; }

        /// <summary>
        /// Converts all colors on the capture surface to grayscale.
        /// </summary>
        string HintGrayscale { get; }

        /// <summary>
        /// DLP Action
        /// </summary>
        string DlpAction { get; }

        /// <summary>
        /// Redact (filled box)
        /// </summary>
        string DlpRedact { get; }

        /// <summary>
        /// Pixelize (Mosaic text)
        /// </summary>
        string DlpPixelize { get; }

        /// <summary>
        /// Blur (Gaussian blur)
        /// </summary>
        string DlpBlur { get; }

        /// <summary>
        /// Highlight (Yellow tint)
        /// </summary>
        string DlpHighlight { get; }

        /// <summary>
        /// Magnify
        /// </summary>
        string DlpMagnify { get; }

        /// <summary>
        /// Patterns / Regex (one per line)
        /// </summary>
        string PatternsRegex { get; }

        /// <summary>
        /// Fill Color (for Redact and Highlight)
        /// </summary>
        string FillColor { get; }

        /// <summary>
        /// Variables (Key / Value)
        /// </summary>
        string Variables { get; }

        /// <summary>
        /// + Add Variable
        /// </summary>
        string BtnAddVariable { get; }

        /// <summary>
        /// Variable Key
        /// </summary>
        string VariableKey { get; }

        /// <summary>
        /// Variable Value
        /// </summary>
        string VariableValue { get; }

        /// <summary>
        /// Delete Variable
        /// </summary>
        string DeleteVariable { get; }

        /// <summary>
        /// Available scopes: ${user.username}, ${machine.computername}, ${now:yyyy-MM-dd}, ${payload.width}
        /// </summary>
        string HintVariableScopes { get; }

        /// <summary>
        /// Annotations
        /// </summary>
        string Annotations { get; }

        /// <summary>
        /// Annotation type to add
        /// </summary>
        string AnnotationTypeToAdd { get; }

        /// <summary>
        /// Arrow
        /// </summary>
        string AnnotationTypeArrow { get; }

        /// <summary>
        /// Barcode
        /// </summary>
        string AnnotationTypeBarcode { get; }

        /// <summary>
        /// Blur
        /// </summary>
        string AnnotationTypeBlur { get; }

        /// <summary>
        /// Cursor
        /// </summary>
        string AnnotationTypeCursor { get; }

        /// <summary>
        /// Ellipse
        /// </summary>
        string AnnotationTypeEllipse { get; }

        /// <summary>
        /// Emoji
        /// </summary>
        string AnnotationTypeEmoji { get; }

        /// <summary>
        /// Highlight
        /// </summary>
        string AnnotationTypeHighlight { get; }

        /// <summary>
        /// Image
        /// </summary>
        string AnnotationTypeImage { get; }

        /// <summary>
        /// Line
        /// </summary>
        string AnnotationTypeLine { get; }

        /// <summary>
        /// Magnify
        /// </summary>
        string AnnotationTypeMagnify { get; }

        /// <summary>
        /// Pixelize
        /// </summary>
        string AnnotationTypePixelize { get; }

        /// <summary>
        /// QR code
        /// </summary>
        string AnnotationTypeQrCode { get; }

        /// <summary>
        /// Rectangle
        /// </summary>
        string AnnotationTypeRectangle { get; }

        /// <summary>
        /// Speech bubble
        /// </summary>
        string AnnotationTypeSpeechBubble { get; }

        /// <summary>
        /// Step label
        /// </summary>
        string AnnotationTypeStepLabel { get; }

        /// <summary>
        /// SVG
        /// </summary>
        string AnnotationTypeSvg { get; }

        /// <summary>
        /// Text
        /// </summary>
        string AnnotationTypeText { get; }

        /// <summary>
        /// + Add Annotation
        /// </summary>
        string BtnAddAnnotation { get; }

        /// <summary>
        /// Template:
        /// </summary>
        string Template { get; }

        /// <summary>
        /// 📷 From Editor...
        /// </summary>
        string BtnFromEditor { get; }

        /// <summary>
        /// Import annotations from open image editor
        /// </summary>
        string TipFromEditor { get; }

        /// <summary>
        /// 📁 From File...
        /// </summary>
        string BtnFromFile { get; }

        /// <summary>
        /// Import annotations from .greenshot file
        /// </summary>
        string TipFromFile { get; }

        /// <summary>
        /// Annotation type
        /// </summary>
        string AnnotationType { get; }

        /// <summary>
        /// Delete Drawable
        /// </summary>
        string DeleteAnnotation { get; }

        /// <summary>
        /// Text / Content (${...} supported)
        /// </summary>
        string AnnotationContent { get; }

        /// <summary>
        /// Step Number / Counter
        /// </summary>
        string StepNumber { get; }

        /// <summary>
        /// Payload / Text / URL (${...} supported)
        /// </summary>
        string PayloadText { get; }

        /// <summary>
        /// Plugin Configuration
        /// </summary>
        string PluginConfiguration { get; }

        /// <summary>
        /// ⚙ Configure...
        /// </summary>
        string BtnConfigure { get; }

        /// <summary>
        /// Open the plugin configuration editor
        /// </summary>
        string TipConfigure { get; }

        /// <summary>
        /// Font &amp; Text Style
        /// </summary>
        string FontTextStyle { get; }

        /// <summary>
        /// Font Family
        /// </summary>
        string FontFamily { get; }

        /// <summary>
        /// Font Size (pt)
        /// </summary>
        string FontSize { get; }

        /// <summary>
        /// Text Alignment
        /// </summary>
        string TextAlignment { get; }

        /// <summary>
        /// Tail Direction / Placement
        /// </summary>
        string TailDirection { get; }

        /// <summary>
        /// Bottom-Left (Default)
        /// </summary>
        string PositionBottomLeftDefault { get; }

        /// <summary>
        /// Bottom-Right
        /// </summary>
        string PositionBottomRight { get; }

        /// <summary>
        /// Bottom-Center
        /// </summary>
        string PositionBottomCenter { get; }

        /// <summary>
        /// Top-Left
        /// </summary>
        string PositionTopLeft { get; }

        /// <summary>
        /// Top-Right
        /// </summary>
        string PositionTopRight { get; }

        /// <summary>
        /// Top-Center
        /// </summary>
        string PositionTopCenter { get; }

        /// <summary>
        /// Left
        /// </summary>
        string PositionLeft { get; }

        /// <summary>
        /// Right
        /// </summary>
        string PositionRight { get; }

        /// <summary>
        /// Tail Offset X
        /// </summary>
        string TailOffsetX { get; }

        /// <summary>
        /// Tail Offset Y
        /// </summary>
        string TailOffsetY { get; }

        /// <summary>
        /// Emoji / Icon
        /// </summary>
        string EmojiIcon { get; }

        /// <summary>
        /// Pick emoji
        /// </summary>
        string PickEmoji { get; }

        /// <summary>
        /// Blur Radius
        /// </summary>
        string BlurRadius { get; }

        /// <summary>
        /// Pixel Size
        /// </summary>
        string PixelSize { get; }

        /// <summary>
        /// Magnification Factor (e.g. 2, 3)
        /// </summary>
        string Magnification { get; }

        /// <summary>
        /// Image Source
        /// </summary>
        string AnnotationImageSource { get; }

        /// <summary>
        /// Embed (Base64)
        /// </summary>
        string EmbedBase64 { get; }

        /// <summary>
        /// Link File
        /// </summary>
        string LinkFile { get; }

        /// <summary>
        /// Image file
        /// </summary>
        string ImageFile { get; }

        /// <summary>
        /// SVG Source
        /// </summary>
        string SvgSource { get; }

        /// <summary>
        /// Embed (XML)
        /// </summary>
        string EmbedXml { get; }

        /// <summary>
        /// SVG file
        /// </summary>
        string SvgFile { get; }

        /// <summary>
        /// Inline SVG XML
        /// </summary>
        string InlineSvg { get; }

        /// <summary>
        /// Cursor Source
        /// </summary>
        string CursorSource { get; }

        /// <summary>
        /// Windows Cursor Preset
        /// </summary>
        string CursorPreset { get; }

        /// <summary>
        /// 📦 Embedded Cursor Bitmap (Base64)
        /// </summary>
        string EmbeddedCursor { get; }

        /// <summary>
        /// Bitmap cursor is embedded directly in this recipe.
        /// </summary>
        string HintEmbeddedCursor { get; }

        /// <summary>
        /// Load File...
        /// </summary>
        string BtnLoadFile { get; }

        /// <summary>
        /// Replace embedded bitmap from file
        /// </summary>
        string TipLoadFile { get; }

        /// <summary>
        /// Cursor file
        /// </summary>
        string CursorFile { get; }

        /// <summary>
        /// H-Anchor
        /// </summary>
        string HAnchor { get; }

        /// <summary>
        /// None (Absolute)
        /// </summary>
        string AnchorNone { get; }

        /// <summary>
        /// Center
        /// </summary>
        string PositionCenter { get; }

        /// <summary>
        /// V-Anchor
        /// </summary>
        string VAnchor { get; }

        /// <summary>
        /// Top
        /// </summary>
        string PositionTop { get; }

        /// <summary>
        /// Bottom
        /// </summary>
        string PositionBottom { get; }

        /// <summary>
        /// Width
        /// </summary>
        string Width { get; }

        /// <summary>
        /// Height
        /// </summary>
        string Height { get; }

        /// <summary>
        /// Lock Ratio
        /// </summary>
        string LockRatio { get; }

        /// <summary>
        /// Locks dimensions to maintain aspect ratio
        /// </summary>
        string TipLockRatio { get; }

        /// <summary>
        /// Offset X / Left
        /// </summary>
        string OffsetXLeft { get; }

        /// <summary>
        /// Offset Y / Top
        /// </summary>
        string OffsetYTop { get; }

        /// <summary>
        /// Line / Border Thickness
        /// </summary>
        string LineThickness { get; }

        /// <summary>
        /// Shadow
        /// </summary>
        string Shadow { get; }

        /// <summary>
        /// Enable drop shadow effect
        /// </summary>
        string TipShadow { get; }

        /// <summary>
        /// Code Color (Fore)
        /// </summary>
        string CodeColor { get; }

        /// <summary>
        /// Click to pick foreground color...
        /// </summary>
        string ClickPickForeground { get; }

        /// <summary>
        /// Pick foreground color
        /// </summary>
        string PickForeground { get; }

        /// <summary>
        /// Background (Back)
        /// </summary>
        string BackgroundBack { get; }

        /// <summary>
        /// Click to pick background color...
        /// </summary>
        string ClickPickBackground { get; }

        /// <summary>
        /// Pick background color
        /// </summary>
        string PickBackground { get; }

        /// <summary>
        /// Quiet Zone Margin
        /// </summary>
        string QuietZone { get; }

        /// <summary>
        /// Render QR modules with rounded corners
        /// </summary>
        string TipRoundedDots { get; }

        /// <summary>
        /// Condition Branches
        /// </summary>
        string ConditionBranches { get; }

        /// <summary>
        /// + Add Branch
        /// </summary>
        string BtnAddBranch { get; }

        /// <summary>
        /// Branch identifier (e.g. A, B, True, False)
        /// </summary>
        string BranchId { get; }

        /// <summary>
        /// Delete branch
        /// </summary>
        string DeleteBranch { get; }

        /// <summary>
        /// Branch condition
        /// </summary>
        string BranchCondition { get; }

        /// <summary>
        /// Condition expression (e.g. ${payload.width &gt; 800} or 'else')
        /// </summary>
        string TipBranchCondition { get; }

        /// <summary>
        /// Branch Execution Logic:
        /// </summary>
        string BranchLogicTitle { get; }

        /// <summary>
        /// User Prompt Interaction:
        /// </summary>
        string PromptInteractionTitle { get; }

        /// <summary>
        /// Prompt Window Title
        /// </summary>
        string PromptTitle { get; }

        /// <summary>
        /// Prompt Message / Question
        /// </summary>
        string PromptMessage { get; }

        /// <summary>
        /// Show Capture Image Preview
        /// </summary>
        string ShowCapturePreview { get; }

        /// <summary>
        /// Timeout (seconds, 0=None)
        /// </summary>
        string PromptTimeout { get; }

        /// <summary>
        /// Default Choice Key
        /// </summary>
        string DefaultChoice { get; }

        /// <summary>
        /// Select the fallback or default choice key for timeouts / Enter key
        /// </summary>
        string TipDefaultChoice { get; }

        /// <summary>
        /// Decision Choices &amp; Buttons
        /// </summary>
        string DecisionChoices { get; }

        /// <summary>
        /// + Add Choice
        /// </summary>
        string BtnAddChoice { get; }

        /// <summary>
        /// Choice key (branch identifier)
        /// </summary>
        string ChoiceKey { get; }

        /// <summary>
        /// Delete choice
        /// </summary>
        string DeleteChoice { get; }

        /// <summary>
        /// Button Label
        /// </summary>
        string ButtonLabel { get; }

        /// <summary>
        /// Button style
        /// </summary>
        string ButtonStyle { get; }

        /// <summary>
        /// Primary (Accent)
        /// </summary>
        string StylePrimary { get; }

        /// <summary>
        /// Secondary (Subtle)
        /// </summary>
        string StyleSecondary { get; }

        /// <summary>
        /// Danger (Red)
        /// </summary>
        string StyleDanger { get; }

        /// <summary>
        /// Default
        /// </summary>
        string ChoiceDefault { get; }

        /// <summary>
        /// Cancel
        /// </summary>
        string ChoiceCancel { get; }

        /// <summary>
        /// Play Shutter Camera Sound
        /// </summary>
        string PlayShutterSound { get; }

        /// <summary>
        /// Custom Sound File (.wav, optional)
        /// </summary>
        string CustomSoundFile { get; }

        /// <summary>
        /// Path to custom .wav sound file or expression (leave empty for default)
        /// </summary>
        string TipCustomSound { get; }

        /// <summary>
        /// Browse for .wav audio file
        /// </summary>
        string BrowseSound { get; }

        /// <summary>
        /// Play preview sound
        /// </summary>
        string PlayPreviewSound { get; }

        /// <summary>
        /// Leave empty to use the default camera shutter sound configured in Greenshot settings.
        /// </summary>
        string HintDefaultSound { get; }

        /// <summary>
        /// Slot
        /// </summary>
        string Slot { get; }

        /// <summary>
        /// Accept: all, none, or the ids of automatic steps separated by commas
        /// </summary>
        string SlotAccept { get; }

        /// <summary>
        /// Automatic steps which plug in now (switched in Settings &gt; Recipes):
        /// </summary>
        string SlotExtensionsNow { get; }

        /// <summary>
        /// Show the notification when the flow completes
        /// </summary>
        string ShowOnCompletion { get; }

        /// <summary>
        /// Text written to standard output (supports ${...})
        /// </summary>
        string StdoutText { get; }

        /// <summary>
        /// Error message written to standard error (supports ${...})
        /// </summary>
        string StderrText { get; }

        /// <summary>
        /// Exit Code
        /// </summary>
        string ExitCode { get; }

        /// <summary>
        /// End the recipe after writing the message
        /// </summary>
        string StderrEndRecipe { get; }

        /// <summary>
        /// Processor Execution Mode
        /// </summary>
        string ProcessorMode { get; }

        /// <summary>
        /// ⚡ All Active Processors
        /// </summary>
        string ProcessorsAll { get; }

        /// <summary>
        /// 🔍 OCR Only (Windows 10 OCR)
        /// </summary>
        string ProcessorsOcrOnly { get; }

        /// <summary>
        /// 🛠️ Selected Processors
        /// </summary>
        string ProcessorsSelected { get; }

        /// <summary>
        /// Execution Timing Stage
        /// </summary>
        string TimingStage { get; }

        /// <summary>
        /// Any / All Stages
        /// </summary>
        string StageAny { get; }

        /// <summary>
        /// Before Interactive Selection
        /// </summary>
        string StageBefore { get; }

        /// <summary>
        /// After Interactive Selection
        /// </summary>
        string StageAfter { get; }

        /// <summary>
        /// Active Processors
        /// </summary>
        string ActiveProcessors { get; }

        /// <summary>
        /// Windows OCR (Text &amp; Words Recognition)
        /// </summary>
        string ProcessorWindowsOcr { get; }

        /// <summary>
        /// Window Title Fixer (Auto-detect Window Titles)
        /// </summary>
        string ProcessorTitleFix { get; }

        /// <summary>
        /// Plugin Processors (QR / Barcode Scanner, etc.)
        /// </summary>
        string ProcessorPlugins { get; }

        /// <summary>
        /// Specific Processor IDs (Optional)
        /// </summary>
        string ProcessorIds { get; }

        /// <summary>
        /// Comma-separated processor designations (e.g. Windows10OcrProcessor, TitleFix).
        /// </summary>
        string HintProcessorIds { get; }

        /// <summary>
        /// Save Directory
        /// </summary>
        string SaveDirectory { get; }

        /// <summary>
        /// Browse for storage folder
        /// </summary>
        string TipBrowseFolder { get; }

        /// <summary>
        /// Filename Pattern
        /// </summary>
        string FilenamePattern { get; }

        /// <summary>
        /// Image Format
        /// </summary>
        string ImageFormat { get; }

        /// <summary>
        /// Allow Overwrite
        /// </summary>
        string AllowOverwrite { get; }

        /// <summary>
        /// Enabled (Overwrite existing)
        /// </summary>
        string OverwriteEnabled { get; }

        /// <summary>
        /// Disabled (Auto-number files)
        /// </summary>
        string OverwriteDisabled { get; }

        /// <summary>
        /// Quality Prompt
        /// </summary>
        string QualityPrompt { get; }

        /// <summary>
        /// Prompt when saving
        /// </summary>
        string PromptWhenSaving { get; }

        /// <summary>
        /// Never prompt
        /// </summary>
        string NeverPrompt { get; }

        /// <summary>
        /// Copy Path to Clipboard
        /// </summary>
        string CopyPath { get; }

        /// <summary>
        /// Enabled (Copy filepath)
        /// </summary>
        string CopyPathEnabled { get; }

        /// <summary>
        /// JPEG Quality (1-100 or empty=Config)
        /// </summary>
        string JpegQualityConfig { get; }

        /// <summary>
        /// Clipboard Content
        /// </summary>
        string ClipboardContent { get; }

        /// <summary>
        /// 🖼️ Image only (standard)
        /// </summary>
        string ClipboardImageOnly { get; }

        /// <summary>
        /// 📄 OCR / Plain Text only
        /// </summary>
        string ClipboardOcrOnly { get; }

        /// <summary>
        /// 🖼️ + 📄 Image and OCR Text
        /// </summary>
        string ClipboardImageAndOcr { get; }

        /// <summary>
        /// Clipboard Formats (Active for this step)
        /// </summary>
        string ClipboardFormats { get; }

        /// <summary>
        /// PNG (Lossless / Office &amp; Web)
        /// </summary>
        string FormatPng { get; }

        /// <summary>
        /// DIB (Device Independent Bitmap)
        /// </summary>
        string FormatDib { get; }

        /// <summary>
        /// DIBv5 (32-bit with Alpha Channel)
        /// </summary>
        string FormatDibv5 { get; }

        /// <summary>
        /// Bitmap (Windows Standard)
        /// </summary>
        string FormatBitmap { get; }

        /// <summary>
        /// HTML (Image File Reference)
        /// </summary>
        string FormatHtmlFile { get; }

        /// <summary>
        /// HTML (Inline Base64 Data URL)
        /// </summary>
        string FormatHtmlInline { get; }

        /// <summary>
        /// Plain Text / OCR Text
        /// </summary>
        string FormatText { get; }

        /// <summary>
        /// Text / OCR Template
        /// </summary>
        string OcrTemplate { get; }

        /// <summary>
        /// Use ${ocr_text} for extracted OCR text, ${title}, ${now:yyyy-MM-dd}, etc.
        /// </summary>
        string HintOcrTemplate { get; }

        /// <summary>
        /// 🎨 Opens the captured image in the interactive Greenshot Image Editor for annotations and drawing.
        /// </summary>
        string HintEditor { get; }

        /// <summary>
        /// Match Editor Window Size to Capture
        /// </summary>
        string MatchEditorSize { get; }

        /// <summary>
        /// Yes (Resize window to match)
        /// </summary>
        string MatchEditorSizeYes { get; }

        /// <summary>
        /// No (Keep current window size)
        /// </summary>
        string MatchEditorSizeNo { get; }

        /// <summary>
        /// Target Editor
        /// </summary>
        string TargetEditor { get; }

        /// <summary>
        /// New editor
        /// </summary>
        string EditorNew { get; }

        /// <summary>
        /// Available editor
        /// </summary>
        string EditorAvailable { get; }

        /// <summary>
        /// Current editor
        /// </summary>
        string EditorCurrent { get; }

        /// <summary>
        /// Printer Name (empty = Default Printer)
        /// </summary>
        string PrinterName { get; }

        /// <summary>
        /// Show Print Dialog
        /// </summary>
        string ShowPrintDialog { get; }

        /// <summary>
        /// Always Show Print Dialog
        /// </summary>
        string PrintDialogAlways { get; }

        /// <summary>
        /// Silent (Direct Print)
        /// </summary>
        string PrintDialogSilent { get; }

        /// <summary>
        /// Auto-Rotate to Fit Page
        /// </summary>
        string AutoRotate { get; }

        /// <summary>
        /// Yes (Rotate if needed)
        /// </summary>
        string AutoRotateYes { get; }

        /// <summary>
        /// No (Keep orientation)
        /// </summary>
        string AutoRotateNo { get; }

        /// <summary>
        /// Allow Enlarge to Fit Page
        /// </summary>
        string AllowEnlarge { get; }

        /// <summary>
        /// Yes (Enlarge small captures)
        /// </summary>
        string AllowEnlargeYes { get; }

        /// <summary>
        /// No
        /// </summary>
        string No { get; }

        /// <summary>
        /// Allow Shrink to Fit Page
        /// </summary>
        string AllowShrink { get; }

        /// <summary>
        /// Yes (Shrink oversized captures)
        /// </summary>
        string AllowShrinkYes { get; }

        /// <summary>
        /// Center on Page
        /// </summary>
        string CenterOnPage { get; }

        /// <summary>
        /// Yes
        /// </summary>
        string Yes { get; }

        /// <summary>
        /// Full Color
        /// </summary>
        string FullColor { get; }

        /// <summary>
        /// Monochrome (Black &amp; White)
        /// </summary>
        string MonochromeBw { get; }

        /// <summary>
        /// Print Footer Date / Title
        /// </summary>
        string PrintFooter { get; }

        /// <summary>
        /// Yes (Print Footer)
        /// </summary>
        string PrintFooterYes { get; }

        /// <summary>
        /// Footer Pattern (empty = Config)
        /// </summary>
        string FooterPattern { get; }

        /// <summary>
        /// Destination ID / Name
        /// </summary>
        string DestinationId { get; }

        /// <summary>
        /// Enter the designation of a destination (e.g. Jira, Box, Dropbox, Word, ZxingQrDestination, or External &lt;command name&gt;).
        /// </summary>
        string HintDestinationId { get; }

        /// <summary>
        /// Export Destinations
        /// </summary>
        string ExportDestinations { get; }

        /// <summary>
        /// Greenshot Image Editor
        /// </summary>
        string DestinationEditor { get; }

        /// <summary>
        /// Custom Destinations (comma separated)
        /// </summary>
        string CustomDestinations { get; }

        /// <summary>
        /// Example: Jira, Confluence, Dropbox, ExternalCommand
        /// </summary>
        string HintCustomDestinations { get; }

        /// <summary>
        /// Window Title
        /// </summary>
        string PickerWindowTitle { get; }

        /// <summary>
        /// Show capture thumbnail preview
        /// </summary>
        string ShowThumbnail { get; }

        /// <summary>
        /// Allow forwarding capture to other recipes
        /// </summary>
        string AllowForwarding { get; }

        /// <summary>
        /// Auto-Dismiss Timeout (Seconds, 0 = Disabled)
        /// </summary>
        string AutoDismissTimeout { get; }

        /// <summary>
        /// Specific Destinations Filter (Optional, comma-separated)
        /// </summary>
        string DestinationsFilter { get; }

        /// <summary>
        /// Leave empty to display all active destinations (Clipboard, Editor, File, Printer, etc.).
        /// </summary>
        string HintDestinationsFilter { get; }

        /// <summary>
        /// 🎯 Interactive &amp; Error Recovery Ready
        /// </summary>
        string DynamicDestinationTitle { get; }

        /// <summary>
        /// Displays a modern WPF flyout with quick-action tiles and a thumbnail. If routed to from an error, it highlights the failed destination and offers immediate retry.
        /// </summary>
        string HintDynamicDestination { get; }

        /// <summary>
        /// Configured Command (name from the External Command settings)
        /// </summary>
        string ConfiguredCommand { get; }

        /// <summary>
        /// When a configured command is set, the executable and arguments below are not used.
        /// </summary>
        string HintConfiguredCommand { get; }

        /// <summary>
        /// Executable / Script Path
        /// </summary>
        string ExecutablePath { get; }

        /// <summary>
        /// Browse for executable or script
        /// </summary>
        string TipBrowseExecutable { get; }

        /// <summary>
        /// Arguments (use {0} for image file)
        /// </summary>
        string Arguments { get; }

        /// <summary>
        /// Temporary Image Format
        /// </summary>
        string TempImageFormat { get; }

        /// <summary>
        /// Shell Verb (Optional, e.g. print; replaces running the executable)
        /// </summary>
        string ShellVerb { get; }

        /// <summary>
        /// Working Directory (Optional)
        /// </summary>
        string WorkingDirectory { get; }

        /// <summary>
        /// Run in background (asynchronously)
        /// </summary>
        string RunAsync { get; }

        /// <summary>
        /// Copy standard output to clipboard
        /// </summary>
        string CopyStdout { get; }

        /// <summary>
        /// Copy detected URL from output to clipboard
        /// </summary>
        string CopyDetectedUrl { get; }

        /// <summary>
        /// Reload image after execution
        /// </summary>
        string ReloadImage { get; }

        /// <summary>
        /// Store Output in Variable (Optional)
        /// </summary>
        string StoreOutputVariable { get; }

        /// <summary>
        /// Store Exit Code in Variable (Optional)
        /// </summary>
        string StoreExitCodeVariable { get; }

        /// <summary>
        /// Jira Issue Key / ID
        /// </summary>
        string JiraIssueKey { get; }

        /// <summary>
        /// Upload Format
        /// </summary>
        string UploadFormat { get; }

        /// <summary>
        /// JPEG Quality (1-100)
        /// </summary>
        string JpegQuality { get; }

        /// <summary>
        /// Confluence Page ID / Title
        /// </summary>
        string ConfluencePageId { get; }

        /// <summary>
        /// Target Office Application
        /// </summary>
        string TargetOfficeApp { get; }

        /// <summary>
        /// Extracted Text Variable Name
        /// </summary>
        string OcrVariableName { get; }

        /// <summary>
        /// Copy decoded text to clipboard
        /// </summary>
        string CopyDecodedText { get; }

        /// <summary>
        /// Cloud Storage Destination
        /// </summary>
        string CloudDestination { get; }

        /// <summary>
        /// Captures will be uploaded directly to your cloud storage account and the public sharing link placed in the clipboard/properties.
        /// </summary>
        string HintCloudDestination { get; }

        /// <summary>
        /// STEP ERROR HANDLING
        /// </summary>
        string HeaderStepErrorHandling { get; }

        /// <summary>
        /// Action to take if this step fails (e.g. clipboard lock, network error)
        /// </summary>
        string HintStepErrorHandling { get; }

        /// <summary>
        /// On Error / Failure
        /// </summary>
        string OnError { get; }

        /// <summary>
        /// Default (Use Recipe Fallback / Terminate)
        /// </summary>
        string OnErrorDefault { get; }

        /// <summary>
        /// Route to Step in this Recipe...
        /// </summary>
        string OnErrorRouteStep { get; }

        /// <summary>
        /// Forward to another Recipe...
        /// </summary>
        string OnErrorForwardRecipe { get; }

        /// <summary>
        /// Fallback Step Node
        /// </summary>
        string FallbackStep { get; }

        /// <summary>
        /// Fallback Recipe
        /// </summary>
        string FallbackRecipe { get; }

        /// <summary>
        /// No Element Selected
        /// </summary>
        string NoElementSelected { get; }

        /// <summary>
        /// Click a step node or connection wire on the canvas to configure parameters, or select a step from the toolbox to add one.
        /// </summary>
        string HintNoElementSelected { get; }

        /// <summary>
        /// ⚡ Recipe &amp; Triggers
        /// </summary>
        string TabRecipeTriggers { get; }

        /// <summary>
        /// RECIPE CONFIGURATION
        /// </summary>
        string HeaderRecipeConfiguration { get; }

        /// <summary>
        /// Recipe ID
        /// </summary>
        string RecipeId { get; }

        /// <summary>
        /// 📋 Copy
        /// </summary>
        string BtnCopy { get; }

        /// <summary>
        /// Copy Recipe ID to Clipboard
        /// </summary>
        string TipCopyRecipeId { get; }

        /// <summary>
        /// Recipe Name
        /// </summary>
        string RecipeName { get; }

        /// <summary>
        /// Description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// AUTOMATIC STEP
        /// </summary>
        string HeaderAutomaticStep { get; }

        /// <summary>
        /// Greenshot adds these steps to other recipes. Connect them between In and Out on the canvas; switch the automatic step on in Settings &gt; Recipes.
        /// </summary>
        string HintAutomaticStep { get; }

        /// <summary>
        /// Recipes: ids, * (every recipe with the slot) or *capture, separated by commas
        /// </summary>
        string ExtensionRecipes { get; }

        /// <summary>
        /// Slot (where in the recipes)
        /// </summary>
        string ExtensionSlot { get; }

        /// <summary>
        /// Order (several on one slot run by order, then by id)
        /// </summary>
        string ExtensionOrder { get; }

        /// <summary>
        /// Only when (optional), e.g. ${payload.width &gt; 800}
        /// </summary>
        string ExtensionWhen { get; }

        /// <summary>
        /// OPTIONS
        /// </summary>
        string HeaderOptions { get; }

        /// <summary>
        /// + Option
        /// </summary>
        string BtnAddOption { get; }

        /// <summary>
        /// Values the user sets once in Settings &gt; Recipes (switches and choices also in the quick settings). The steps read them with ${option.key}.
        /// </summary>
        string HintOptions { get; }

        /// <summary>
        /// Key
        /// </summary>
        string OptionKey { get; }

        /// <summary>
        /// Remove the option
        /// </summary>
        string TipRemoveOption { get; }

        /// <summary>
        /// Type
        /// </summary>
        string OptionType { get; }

        /// <summary>
        /// Label
        /// </summary>
        string OptionLabel { get; }

        /// <summary>
        /// Default
        /// </summary>
        string OptionDefault { get; }

        /// <summary>
        /// Range
        /// </summary>
        string OptionRange { get; }

        /// <summary>
        /// Smallest value
        /// </summary>
        string TipOptionMin { get; }

        /// <summary>
        /// Largest value
        /// </summary>
        string TipOptionMax { get; }

        /// <summary>
        /// Choices
        /// </summary>
        string OptionChoices { get; }

        /// <summary>
        /// One per line: value, or value = label
        /// </summary>
        string TipOptionChoices { get; }

        /// <summary>
        /// Quick settings
        /// </summary>
        string OptionQuickSettings { get; }

        /// <summary>
        /// Value may contain ${...}
        /// </summary>
        string OptionExpandPlaceholders { get; }

        /// <summary>
        /// Text options only: placeholders in the value the user sets are filled in when the step runs, e.g. a caption "Captured ${now:yyyy-MM-dd}" gets the date. Without it the value is used as typed.
        /// </summary>
        string TipOptionExpandPlaceholders { get; }

        /// <summary>
        /// Only when
        /// </summary>
        string OptionEnabledWhen { get; }

        /// <summary>
        /// The key of a Boolean option: this option can only be changed while that one is on
        /// </summary>
        string TipOptionEnabledWhen { get; }

        /// <summary>
        /// START STEPS (ENTRY POINTS)
        /// </summary>
        string HeaderStartSteps { get; }

        /// <summary>
        /// Select one or more entry steps. Multiple start steps execute concurrently in parallel when triggered.
        /// </summary>
        string HintStartSteps { get; }

        /// <summary>
        /// TRIGGERS
        /// </summary>
        string HeaderTriggers { get; }

        /// <summary>
        /// + Hotkey
        /// </summary>
        string TriggerAddHotkey { get; }

        /// <summary>
        /// + Context Menu
        /// </summary>
        string TriggerAddContextMenu { get; }

        /// <summary>
        /// + Editor
        /// </summary>
        string TriggerAddEditor { get; }

        /// <summary>
        /// + Clipboard
        /// </summary>
        string TriggerAddClipboard { get; }

        /// <summary>
        /// + CLI
        /// </summary>
        string TriggerAddCli { get; }

        /// <summary>
        /// Add Command Line Trigger
        /// </summary>
        string TipTriggerAddCli { get; }

        /// <summary>
        /// + Open With
        /// </summary>
        string TriggerAddOpenWith { get; }

        /// <summary>
        /// Add Open With File Trigger
        /// </summary>
        string TipTriggerAddOpenWith { get; }

        /// <summary>
        /// + Browser extension
        /// </summary>
        string TriggerAddBrowserExtension { get; }

        /// <summary>
        /// Add Browser Extension Trigger
        /// </summary>
        string TipTriggerAddBrowserExtension { get; }

        /// <summary>
        /// + AI Tool
        /// </summary>
        string TriggerAddAiTool { get; }

        /// <summary>
        /// Offer the recipe as a tool to AI tools (greenshot-mcp)
        /// </summary>
        string TipTriggerAddAiTool { get; }

        /// <summary>
        /// + Manual
        /// </summary>
        string TriggerAddManual { get; }

        /// <summary>
        /// Enabled
        /// </summary>
        string Enabled { get; }

        /// <summary>
        /// Delete Trigger
        /// </summary>
        string DeleteTrigger { get; }

        /// <summary>
        /// Hotkey Trigger
        /// </summary>
        string HotkeyTrigger { get; }

        /// <summary>
        /// Menu Item Label
        /// </summary>
        string MenuItemLabel { get; }

        /// <summary>
        /// Menu Group
        /// </summary>
        string MenuGroup { get; }

        /// <summary>
        /// Menu Item Order (lower values first)
        /// </summary>
        string MenuItemOrder { get; }

        /// <summary>
        /// Editor Menu Item Label
        /// </summary>
        string EditorMenuItemLabel { get; }

        /// <summary>
        /// Editor Menu Group
        /// </summary>
        string EditorMenuGroup { get; }

        /// <summary>
        /// Editor Menu Item Order (lower values first)
        /// </summary>
        string EditorMenuItemOrder { get; }

        /// <summary>
        /// Format Filter (Optional, e.g. PNG, DeviceIndependentBitmap)
        /// </summary>
        string FormatFilter { get; }

        /// <summary>
        /// Trigger Name
        /// </summary>
        string TriggerName { get; }

        /// <summary>
        /// CLI Command Name (e.g. ocr, my-task)
        /// </summary>
        string CliCommandName { get; }

        /// <summary>
        /// CLI Description (Shown in -l / --list-recipes)
        /// </summary>
        string CliDescription { get; }

        /// <summary>
        /// Fire and Forget (Return immediately without waiting)
        /// </summary>
        string FireAndForgetWait { get; }

        /// <summary>
        /// File Extensions (e.g. .png;.jpg;.bmp, empty for all)
        /// </summary>
        string FileExtensions { get; }

        /// <summary>
        /// Fire and Forget
        /// </summary>
        string FireAndForget { get; }

        /// <summary>
        /// Tool Name for the AI (letters, digits, _ and -)
        /// </summary>
        string AiToolName { get; }

        /// <summary>
        /// Title (shown to people)
        /// </summary>
        string AiToolTitle { get; }

        /// <summary>
        /// Description (what the tool does, for the AI)
        /// </summary>
        string AiToolDescription { get; }

        /// <summary>
        /// Read only (the tool only captures, it doesn't save or send anything)
        /// </summary>
        string AiToolReadOnly { get; }

        /// <summary>
        /// Browser Filter (e.g. chrome, firefox, or empty for all)
        /// </summary>
        string BrowserFilter { get; }

        /// <summary>
        /// ERROR HANDLING &amp; FALLBACK
        /// </summary>
        string HeaderErrorHandling { get; }

        /// <summary>
        /// Define what should happen when a step fails (e.g. clipboard lock, network timeout). Route the capture to a fallback step or another recipe.
        /// </summary>
        string HintErrorHandling { get; }

        /// <summary>
        /// + Add Error Fallback Route
        /// </summary>
        string BtnAddErrorRoute { get; }

        /// <summary>
        /// On Error In
        /// </summary>
        string ErrorRouteFrom { get; }

        /// <summary>
        /// Node ID or * for any step in this recipe
        /// </summary>
        string TipErrorRouteFrom { get; }

        /// <summary>
        /// Remove this error route
        /// </summary>
        string RemoveErrorRoute { get; }

        /// <summary>
        /// Error Filter (* = all exceptions)
        /// </summary>
        string ErrorFilter { get; }

        /// <summary>
        /// Route To
        /// </summary>
        string RouteTo { get; }

        /// <summary>
        /// Step in this Recipe
        /// </summary>
        string RouteStep { get; }

        /// <summary>
        /// Another Recipe
        /// </summary>
        string RouteRecipe { get; }

        /// <summary>
        /// Greenshot DAG Pipeline Engine
        /// </summary>
        string PipelineEngine { get; }

        /// <summary>
        /// (Deactivated)
        /// </summary>
        string Deactivated { get; }

        /// <summary>
        /// Each condition gets its own output connector pin (A, B, C...).
        /// </summary>
        string HintBranchPins { get; }

        /// <summary>
        /// Connect each pin to the destination step for that branch.
        /// </summary>
        string HintBranchConnect { get; }

        /// <summary>
        /// Use 'else' for default fallback branch.
        /// </summary>
        string HintBranchElse { get; }

        /// <summary>
        /// Displays an interactive modal dialog to the user during capture flow.
        /// </summary>
        string HintPromptDialog { get; }

        /// <summary>
        /// Execution branches along the connector pin corresponding to the clicked button.
        /// </summary>
        string HintPromptBranches { get; }

        /// <summary>
        /// Greenshot - Capture Recipe Manager
        /// </summary>
        string ManagerTitle { get; }

        /// <summary>
        /// Click to toggle Activate / Deactivate
        /// </summary>
        string ManagerToggleActiveTooltip { get; }

        /// <summary>
        /// This recipe was written by an AI tool and approved by you
        /// </summary>
        string ManagerAiBadgeTooltip { get; }

        /// <summary>
        /// ✏️ Edit
        /// </summary>
        string ManagerBtnEdit { get; }

        /// <summary>
        /// Open recipe in Visual Editor canvas
        /// </summary>
        string ManagerBtnEditTooltip { get; }

        /// <summary>
        /// ▶ Test Run
        /// </summary>
        string ManagerBtnTestRun { get; }

        /// <summary>
        /// Test execute capture recipe in Greenshot
        /// </summary>
        string ManagerBtnTestRunTooltip { get; }

        /// <summary>
        /// ℹ Details
        /// </summary>
        string ManagerBtnDetails { get; }

        /// <summary>
        /// What the recipe does, its triggers, its approval and the changes against the built-in recipe
        /// </summary>
        string ManagerBtnDetailsTooltip { get; }

        /// <summary>
        /// 🛡 Permissions
        /// </summary>
        string ManagerBtnPermissions { get; }

        /// <summary>
        /// Switch the recipe's triggers and permissions on or off
        /// </summary>
        string ManagerBtnPermissionsTooltip { get; }

        /// <summary>
        /// steps
        /// </summary>
        string ManagerSteps { get; }

        /// <summary>
        /// Search recipes
        /// </summary>
        string ManagerSearch { get; }

        /// <summary>
        /// ➕ Load Recipe...
        /// </summary>
        string ManagerBtnLoad { get; }

        /// <summary>
        /// Load and register a .gsrecipe.json file into Greenshot
        /// </summary>
        string ManagerBtnLoadTooltip { get; }

        /// <summary>
        /// ✨ New Recipe
        /// </summary>
        string ManagerBtnNew { get; }

        /// <summary>
        /// Create a new capture recipe in the Visual Editor
        /// </summary>
        string ManagerBtnNewTooltip { get; }

        /// <summary>
        /// 🔄 Reload All
        /// </summary>
        string ManagerBtnReloadAll { get; }

        /// <summary>
        /// Reload configured recipe files from disk
        /// </summary>
        string ManagerBtnReloadAllTooltip { get; }

        /// <summary>
        /// ↩ Reset All
        /// </summary>
        string ManagerBtnResetAll { get; }

        /// <summary>
        /// Bring back the built-in version of every recipe a file replaces
        /// </summary>
        string ManagerBtnResetAllTooltip { get; }

        /// <summary>
        /// All
        /// </summary>
        string ManagerFilterAll { get; }

        /// <summary>
        /// Active
        /// </summary>
        string ManagerFilterActive { get; }

        /// <summary>
        /// Deactivated
        /// </summary>
        string ManagerFilterDeactivated { get; }

        /// <summary>
        /// Built-in
        /// </summary>
        string ManagerFilterBuiltIn { get; }

        /// <summary>
        /// Custom
        /// </summary>
        string ManagerFilterCustom { get; }

        /// <summary>
        /// AI
        /// </summary>
        string ManagerFilterAi { get; }

        /// <summary>
        /// Recipes written by AI tools
        /// </summary>
        string ManagerFilterAiTooltip { get; }

        /// <summary>
        /// RECIPES
        /// </summary>
        string ManagerHeaderRecipes { get; }

        /// <summary>
        /// AUTOMATIC STEPS
        /// </summary>
        string ManagerHeaderAutomaticSteps { get; }

        /// <summary>
        /// Steps Greenshot adds to the recipes with a fitting slot, switched in Settings &gt; Recipes.
        /// </summary>
        string ManagerAutomaticStepsHint { get; }

        /// <summary>
        /// Total:
        /// </summary>
        string ManagerTotal { get; }

        /// <summary>
        /// Active:
        /// </summary>
        string ManagerActive { get; }

        /// <summary>
        /// Deactivated:
        /// </summary>
        string ManagerDeactivated { get; }

        /// <summary>
        /// Greenshot - Import Annotations
        /// </summary>
        string ImportTitle { get; }

        /// <summary>
        /// Sort A-Z ▲
        /// </summary>
        string ImportSortAscending { get; }

        /// <summary>
        /// Sort Z-A ▼
        /// </summary>
        string ImportSortDescending { get; }

        /// <summary>
        /// Toggle alphabetical sort
        /// </summary>
        string ImportSortTooltip { get; }

        /// <summary>
        /// Target Step Mode:
        /// </summary>
        string ImportTargetStepMode { get; }

        /// <summary>
        /// Append to existing annotations
        /// </summary>
        string ImportModeAppend { get; }

        /// <summary>
        /// Replace existing annotations
        /// </summary>
        string ImportModeReplace { get; }

        /// <summary>
        /// Select / Deselect all
        /// </summary>
        string ImportSelectAll { get; }

        /// <summary>
        /// Type (A-Z)
        /// </summary>
        string ImportColumnType { get; }

        /// <summary>
        /// Description / Content
        /// </summary>
        string ImportColumnDescription { get; }

        /// <summary>
        /// Asset Storage
        /// </summary>
        string ImportColumnStorage { get; }

        /// <summary>
        /// 📦 Embed (Base64)
        /// </summary>
        string ImportStorageEmbed { get; }

        /// <summary>
        /// 💾 Save to File
        /// </summary>
        string ImportStorageFile { get; }

        /// <summary>
        /// 💾 File Storage Destination (For elements set to 'Save to File'):
        /// </summary>
        string ImportFileDestination { get; }

        /// <summary>
        /// Save to Folder:
        /// </summary>
        string ImportSaveToFolder { get; }

        /// <summary>
        /// Browse...
        /// </summary>
        string ImportBtnBrowse { get; }

        /// <summary>
        /// Filename Prefix:
        /// </summary>
        string ImportFilenamePrefix { get; }

        /// <summary>
        /// Prefix used for all exported asset files
        /// </summary>
        string ImportFilenamePrefixTooltip { get; }

        /// <summary>
        /// ℹ Exported files will be named: &lt;Folder&gt;\{prefix}_{type}_{number}.png (e.g., "myrecipe_cursor_1.png")
        /// </summary>
        string ImportFileNamingHint { get; }

        /// <summary>
        /// Proceed Import
        /// </summary>
        string ImportBtnProceed { get; }

        /// <summary>
        /// Greenshot - Select Image Editor
        /// </summary>
        string SelectEditorTitle { get; }

        /// <summary>
        /// Multiple image editors are open. Select an editor to preview its annotations and import them into the recipe step:
        /// </summary>
        string SelectEditorIntro { get; }

        /// <summary>
        /// Open Editors
        /// </summary>
        string SelectEditorListHeader { get; }

        /// <summary>
        /// PREVIEW
        /// </summary>
        string SelectEditorPreviewHeader { get; }

        /// <summary>
        /// Preview
        /// </summary>
        string SelectEditorPreview { get; }

        /// <summary>
        /// No preview available
        /// </summary>
        string SelectEditorNoPreview { get; }

        /// <summary>
        /// Import Annotations
        /// </summary>
        string SelectEditorBtnImport { get; }

        /// <summary>
        /// Configured Recipe Files (semicolon-separated):
        /// </summary>
        string ConfigRecipeFiles { get; }

        /// <summary>
        /// OVERRIDDEN
        /// </summary>
        string BadgeOverridden { get; }

        /// <summary>
        /// BUILT-IN
        /// </summary>
        string BadgeBuiltIn { get; }

        /// <summary>
        /// CUSTOM
        /// </summary>
        string BadgeCustom { get; }

        /// <summary>
        /// BY {0}
        /// </summary>
        string BadgeAiBy { get; }

        /// <summary>
        /// ACTIVE
        /// </summary>
        string BadgeActive { get; }

        /// <summary>
        /// DEACTIVATED
        /// </summary>
        string BadgeDeactivated { get; }

        /// <summary>
        /// Automatic step at {0}, fits {1} recipe(s)
        /// </summary>
        string TriggerSummaryExtension { get; }

        /// <summary>
        /// Context menu (Default)
        /// </summary>
        string TriggerSummaryDefaultContextMenu { get; }

        /// <summary>
        /// No triggers (Editor/DAG only)
        /// </summary>
        string TriggerSummaryNone { get; }

        /// <summary>
        /// (disabled)
        /// </summary>
        string TriggerSuffixDisabled { get; }

        /// <summary>
        /// (off, not approved)
        /// </summary>
        string TriggerSuffixNotApproved { get; }

        /// <summary>
        /// Hotkey
        /// </summary>
        string TriggerHotkey { get; }

        /// <summary>
        /// Context menu ("{0}")
        /// </summary>
        string TriggerSummaryContextMenu { get; }

        /// <summary>
        /// Editor ("{0}")
        /// </summary>
        string TriggerSummaryEditor { get; }

        /// <summary>
        /// Clipboard Monitor
        /// </summary>
        string TriggerClipboardMonitor { get; }

        /// <summary>
        /// (+ web pages)
        /// </summary>
        string TriggerSuffixWebPages { get; }

        /// <summary>
        /// (web pages: not approved)
        /// </summary>
        string TriggerSuffixWebPagesNotApproved { get; }

        /// <summary>
        /// AI tool "{0}"
        /// </summary>
        string TriggerSummaryAiTool { get; }

        /// <summary>
        /// Open file
        /// </summary>
        string TriggerOpenFile { get; }

        /// <summary>
        /// Browser extension
        /// </summary>
        string TriggerBrowserExtension { get; }

        /// <summary>
        /// Schedule
        /// </summary>
        string TriggerSchedule { get; }

        /// <summary>
        /// Manual
        /// </summary>
        string TriggerManual { get; }

        /// <summary>
        /// Reset Default
        /// </summary>
        string BtnResetDefault { get; }

        /// <summary>
        /// Unload
        /// </summary>
        string BtnUnload { get; }

        /// <summary>
        /// Revert overridden recipe back to original default definition
        /// </summary>
        string TooltipResetDefault { get; }

        /// <summary>
        /// Unload and unregister this custom recipe from Greenshot
        /// </summary>
        string TooltipUnload { get; }

        /// <summary>
        /// Recipe Permissions
        /// </summary>
        string TitleRecipePermissions { get; }

        /// <summary>
        /// Reset Recipe to Default
        /// </summary>
        string TitleResetRecipe { get; }

        /// <summary>
        /// Unload Recipe
        /// </summary>
        string TitleUnloadRecipe { get; }

        /// <summary>
        /// Are you sure you want to revert '{0}' back to its default built-in definition?
        /// </summary>
        string ConfirmResetRecipe { get; }

        /// <summary>
        /// Are you sure you want to unload '{0}'? It will be unregistered from Greenshot.
        /// </summary>
        string ConfirmUnloadRecipe { get; }

        /// <summary>
        /// Cannot test run recipe. Fix validation errors first:
        /// </summary>
        string ErrorTestRunValidation { get; }

        /// <summary>
        /// Validation Error
        /// </summary>
        string TitleValidationError { get; }

        /// <summary>
        /// Recipe execution encountered an error:
        /// </summary>
        string ErrorExecution { get; }

        /// <summary>
        /// Execution Error
        /// </summary>
        string TitleExecutionError { get; }

        /// <summary>
        /// Bring back the built-in version of these recipes?
        /// </summary>
        string ConfirmResetAll { get; }

        /// <summary>
        /// Reset All to Default
        /// </summary>
        string TitleResetAll { get; }

        /// <summary>
        /// Load Capture Recipe into Greenshot
        /// </summary>
        string TitleLoadRecipe { get; }

        /// <summary>
        /// Recipe validation failed:
        /// </summary>
        string ErrorRecipeValidation { get; }

        /// <summary>
        /// Loaded {0}
        /// </summary>
        string StatusLoaded { get; }

        /// <summary>
        /// Validation failed for {0}
        /// </summary>
        string StatusValidationFailed { get; }

        /// <summary>
        /// Loaded and registered {0}
        /// </summary>
        string StatusLoadedRegistered { get; }

        /// <summary>
        /// Error Loading Recipe
        /// </summary>
        string TitleErrorLoadingRecipe { get; }

        /// <summary>
        /// Reloaded recipes from disk.
        /// </summary>
        string StatusReloaded { get; }

        /// <summary>
        /// Switch to Light Mode
        /// </summary>
        string TooltipSwitchLight { get; }

        /// <summary>
        /// Switch to Dark Mode
        /// </summary>
        string TooltipSwitchDark { get; }

        /// <summary>
        /// The capture flow runner is not available.
        /// </summary>
        string ErrorFlowRunnerUnavailable { get; }

        /// <summary>
        /// unknown error
        /// </summary>
        string ErrorUnknown { get; }

        /// <summary>
        /// Inserted '{0}' between '{1}' and '{2}'
        /// </summary>
        string StatusInsertedBetween { get; }

        /// <summary>
        /// It fits no recipe now (none has this slot, or no recipe matches).
        /// </summary>
        string ExtensionReachNone { get; }

        /// <summary>
        /// It fits {0} recipe(s) now: {1}
        /// </summary>
        string ExtensionReach { get; }

        /// <summary>
        /// Opened automatic step '{0}'
        /// </summary>
        string StatusOpenedExtension { get; }

        /// <summary>
        /// New Automatic Step
        /// </summary>
        string NewExtensionName { get; }

        /// <summary>
        /// Changes every capture before it goes to a destination
        /// </summary>
        string NewExtensionDescription { get; }

        /// <summary>
        /// On
        /// </summary>
        string NewExtensionOptionOn { get; }

        /// <summary>
        /// Created a new automatic step: connect its steps between In and Out, set where it goes in the recipe tab.
        /// </summary>
        string StatusNewExtension { get; }

        /// <summary>
        /// Automatic Step Not Saved
        /// </summary>
        string TitleExtensionNotSaved { get; }

        /// <summary>
        /// The automatic step was not saved.
        /// </summary>
        string StatusExtensionNotSaved { get; }

        /// <summary>
        /// Applied the JSON of the automatic step.
        /// </summary>
        string StatusExtensionJsonApplied { get; }

        /// <summary>
        /// In
        /// </summary>
        string NodeIn { get; }

        /// <summary>
        /// Out
        /// </summary>
        string NodeOut { get; }

        /// <summary>
        /// Step
        /// </summary>
        string Step { get; }

        /// <summary>
        /// New option
        /// </summary>
        string NewOptionLabel { get; }

        /// <summary>
        /// Added option '{0}': the steps read it with {1}
        /// </summary>
        string StatusOptionAdded { get; }

        /// <summary>
        /// Removed option '{0}'
        /// </summary>
        string StatusOptionRemoved { get; }

        /// <summary>
        /// Run Recipe
        /// </summary>
        string TriggerDefaultMenuItemText { get; }

        /// <summary>
        /// Hotkey: {0}
        /// </summary>
        string TriggerTitleHotkey { get; }

        /// <summary>
        /// Context Menu: {0}
        /// </summary>
        string TriggerTitleContextMenu { get; }

        /// <summary>
        /// Editor Menu: {0}
        /// </summary>
        string TriggerTitleEditor { get; }

        /// <summary>
        /// Clipboard: {0}
        /// </summary>
        string TriggerTitleClipboard { get; }

        /// <summary>
        /// CLI: {0}
        /// </summary>
        string TriggerTitleCli { get; }

        /// <summary>
        /// Open With File (All)
        /// </summary>
        string TriggerTitleOpenFileAll { get; }

        /// <summary>
        /// Open With: {0}
        /// </summary>
        string TriggerTitleOpenFile { get; }

        /// <summary>
        /// Browser Extension (All)
        /// </summary>
        string TriggerTitleBrowserAll { get; }

        /// <summary>
        /// Browser: {0}
        /// </summary>
        string TriggerTitleBrowser { get; }

        /// <summary>
        /// AI tool: {0}
        /// </summary>
        string TriggerTitleAiTool { get; }

        /// <summary>
        /// Trigger: {0}
        /// </summary>
        string TriggerTitleOther { get; }

        /// <summary>
        /// Source: {0} • 1 element found
        /// </summary>
        string ImportSourceOne { get; }

        /// <summary>
        /// Source: {0} • {1} elements found
        /// </summary>
        string ImportSourceMany { get; }

        /// <summary>
        /// Image Source
        /// </summary>
        string ImportImageSource { get; }

        /// <summary>
        /// {0} of {1} element selected
        /// </summary>
        string ImportSelectedOne { get; }

        /// <summary>
        /// {0} of {1} elements selected
        /// </summary>
        string ImportSelectedMany { get; }

        /// <summary>
        /// Select Destination Folder for Exported Assets
        /// </summary>
        string ImportSelectFolder { get; }

        /// <summary>
        /// Please select at least one element to import.
        /// </summary>
        string ImportNothingSelected { get; }

        /// <summary>
        /// Import Annotations
        /// </summary>
        string TitleImportAnnotations { get; }

        /// <summary>
        /// Please specify a destination folder for elements to be saved on disk.
        /// </summary>
        string ImportNoFolder { get; }

        /// <summary>
        /// Invalid Directory
        /// </summary>
        string TitleInvalidDirectory { get; }

        /// <summary>
        /// Could not create or access directory '{0}':
        /// </summary>
        string ImportFolderError { get; }

        /// <summary>
        /// Untitled Image
        /// </summary>
        string EditorUntitled { get; }

        /// <summary>
        /// Unknown size
        /// </summary>
        string EditorUnknownSize { get; }

        /// <summary>
        /// {0} annotation
        /// </summary>
        string AnnotationCountOne { get; }

        /// <summary>
        /// {0} annotations
        /// </summary>
        string AnnotationCountMany { get; }

        /// <summary>
        /// Editor {0} of {1} selected
        /// </summary>
        string EditorSelectedOf { get; }

        /// <summary>
        /// No editor selected
        /// </summary>
        string EditorNoneSelected { get; }

        /// <summary>
        /// Text: "{0}"
        /// </summary>
        string AnnotationText { get; }

        /// <summary>
        /// Bubble: "{0}"
        /// </summary>
        string AnnotationBubble { get; }

        /// <summary>
        /// Step Counter {0}
        /// </summary>
        string AnnotationStepCounter { get; }

        /// <summary>
        /// Emoji {0}
        /// </summary>
        string AnnotationEmoji { get; }

        /// <summary>
        /// {0} (Thickness: {1})
        /// </summary>
        string AnnotationThickness { get; }

        /// <summary>
        /// Blur (Radius: {0})
        /// </summary>
        string AnnotationBlur { get; }

        /// <summary>
        /// Pixelize (Block: {0})
        /// </summary>
        string AnnotationPixelize { get; }

        /// <summary>
        /// Highlight Area
        /// </summary>
        string AnnotationHighlight { get; }

        /// <summary>
        /// Magnifier ({0}x)
        /// </summary>
        string AnnotationMagnifier { get; }

        /// <summary>
        /// Bitmap Image ({0})
        /// </summary>
        string AnnotationBitmap { get; }

        /// <summary>
        /// Scalable Vector Graphics (SVG)
        /// </summary>
        string AnnotationSvg { get; }

        /// <summary>
        /// Mouse Cursor (Bitmap)
        /// </summary>
        string AnnotationCursor { get; }

        /// <summary>
        /// Ready
        /// </summary>
        string StatusReady { get; }

        /// <summary>
        /// Active
        /// </summary>
        string RecipeActive { get; }

        /// <summary>
        /// Deactivated
        /// </summary>
        string RecipeDeactivated { get; }

        /// <summary>
        /// Revert overridden recipe to default built-in definition
        /// </summary>
        string TooltipResetActiveRecipe { get; }

        /// <summary>
        /// Unload custom recipe from Greenshot
        /// </summary>
        string TooltipUnloadActiveRecipe { get; }

        /// <summary>
        /// Untitled Recipe
        /// </summary>
        string UntitledRecipe { get; }

        /// <summary>
        /// Greenshot - Recipe Visual Editor
        /// </summary>
        string WindowTitle { get; }

        /// <summary>
        /// {0} - Greenshot Recipe Visual Editor
        /// </summary>
        string WindowTitleRecipe { get; }

        /// <summary>
        /// (unsaved changes)
        /// </summary>
        string TitleUnsavedSuffix { get; }

        /// <summary>
        /// Undone
        /// </summary>
        string StatusUndone { get; }

        /// <summary>
        /// Redone
        /// </summary>
        string StatusRedone { get; }

        /// <summary>
        /// Saving asks for your approval:
        /// </summary>
        string ApprovalSaveReasons { get; }

        /// <summary>
        /// This recipe file isn't approved yet. Greenshot asks for your approval before it runs.
        /// </summary>
        string ApprovalNotApproved { get; }

        /// <summary>
        /// This recipe file changed outside Greenshot. Greenshot asks for your approval before it runs.
        /// </summary>
        string ApprovalChanged { get; }

        /// <summary>
        /// 1 trigger was left off when this recipe was approved{0}. Review the approval to switch it on.
        /// </summary>
        string ApprovalTriggerOffOne { get; }

        /// <summary>
        /// {0} triggers were left off when this recipe was approved{1}. Review the approval to switch them on.
        /// </summary>
        string ApprovalTriggersOffMany { get; }

        /// <summary>
        /// Unsaved Changes
        /// </summary>
        string TitleUnsavedChanges { get; }

        /// <summary>
        /// "{0}" has unsaved changes. Do you want to save them?
        /// </summary>
        string ConfirmUnsavedChanges { get; }

        /// <summary>
        /// Don't Save
        /// </summary>
        string BtnDontSave { get; }

        /// <summary>
        /// Copied Recipe ID '{0}' to clipboard
        /// </summary>
        string StatusCopiedRecipeId { get; }

        /// <summary>
        /// Failed to copy to clipboard: {0}
        /// </summary>
        string StatusCopyFailed { get; }

        /// <summary>
        /// Connecting from Input pin of '{0}'... Drop onto an Output pin.
        /// </summary>
        string StatusConnectingFromInput { get; }

        /// <summary>
        /// Connecting from Output pin of '{0}'... Drop onto an Input pin.
        /// </summary>
        string StatusConnectingFromOutput { get; }

        /// <summary>
        /// Connection cancelled (dropped on canvas empty space).
        /// </summary>
        string StatusConnectionCancelled { get; }

        /// <summary>
        /// Cannot connect a pin to itself.
        /// </summary>
        string StatusConnectPinItself { get; }

        /// <summary>
        /// (From Configuration)
        /// </summary>
        string OutputFormatFromConfiguration { get; }

        /// <summary>
        /// Loaded recipe '{0}' ({1} steps, {2} triggers)
        /// </summary>
        string StatusRecipeLoaded { get; }

        /// <summary>
        /// Added '{0}' to Start Steps
        /// </summary>
        string StatusStartStepAdded { get; }

        /// <summary>
        /// Removed '{0}' from Start Steps
        /// </summary>
        string StatusStartStepRemoved { get; }

        /// <summary>
        /// '{0}' marked as Start Step
        /// </summary>
        string StatusStartStepMarked { get; }

        /// <summary>
        /// {0} Trigger
        /// </summary>
        string TriggerDefaultName { get; }

        /// <summary>
        /// Capture with Recipe
        /// </summary>
        string TriggerDefaultContextMenu { get; }

        /// <summary>
        /// Apply Recipe
        /// </summary>
        string TriggerDefaultEditor { get; }

        /// <summary>
        /// Custom commandline recipe
        /// </summary>
        string TriggerDefaultCliDescription { get; }

        /// <summary>
        /// My tool
        /// </summary>
        string TriggerDefaultAiTitle { get; }

        /// <summary>
        /// Added {0} trigger
        /// </summary>
        string StatusTriggerAdded { get; }

        /// <summary>
        /// Trigger removed
        /// </summary>
        string StatusTriggerRemoved { get; }

        /// <summary>
        /// Cannot connect a step to itself.
        /// </summary>
        string StatusConnectStepItself { get; }

        /// <summary>
        /// Cannot connect two Input pins together. Please connect an Output pin to an Input pin.
        /// </summary>
        string StatusConnectTwoInputs { get; }

        /// <summary>
        /// Cannot connect two Output pins together. Please connect an Output pin to an Input pin.
        /// </summary>
        string StatusConnectTwoOutputs { get; }

        /// <summary>
        /// Connection from '{0}' to '{1}' already exists.
        /// </summary>
        string StatusConnectionExists { get; }

        /// <summary>
        /// Warning: Connected '{0}' -&gt; '{1}' (Creates a Cycle in the DAG!)
        /// </summary>
        string StatusConnectedCycle { get; }

        /// <summary>
        /// Connected '{0}' -&gt; '{1}'
        /// </summary>
        string StatusConnected { get; }

        /// <summary>
        /// Connection removed
        /// </summary>
        string StatusConnectionRemoved { get; }

        /// <summary>
        /// Added step: {0}
        /// </summary>
        string StatusStepAdded { get; }

        /// <summary>
        /// Inserted {0} between '{1}' and '{2}'
        /// </summary>
        string StatusStepInsertedBetween { get; }

        /// <summary>
        /// Inserted {0} after '{1}'
        /// </summary>
        string StatusStepInsertedAfter { get; }

        /// <summary>
        /// In and Out keep their names.
        /// </summary>
        string StatusInOutNames { get; }

        /// <summary>
        /// Cannot rename to '{0}': A step with this ID already exists.
        /// </summary>
        string StatusRenameExists { get; }

        /// <summary>
        /// Renamed step ID from '{0}' to '{1}'
        /// </summary>
        string StatusRenamedStep { get; }

        /// <summary>
        /// In and Out belong to the automatic step, they can't be deleted.
        /// </summary>
        string StatusInOutDelete { get; }

        /// <summary>
        /// Deleted step '{0}' ({1})
        /// </summary>
        string StatusDeletedStep { get; }

        /// <summary>
        /// Validation Warning: {0}
        /// </summary>
        string StatusValidationWarning { get; }

        /// <summary>
        /// Workflow DAG Valid
        /// </summary>
        string StatusDagValid { get; }

        /// <summary>
        /// Added recipe error fallback route
        /// </summary>
        string StatusErrorRouteAdded { get; }

        /// <summary>
        /// Removed recipe error fallback route
        /// </summary>
        string StatusErrorRouteRemoved { get; }

        /// <summary>
        /// {0} (off)
        /// </summary>
        string ExtensionOffSuffix { get; }

        /// <summary>
        /// New Custom Workflow
        /// </summary>
        string NewRecipeName { get; }

        /// <summary>
        /// Custom visual capture recipe
        /// </summary>
        string NewRecipeDescription { get; }

        /// <summary>
        /// Created new recipe.
        /// </summary>
        string StatusRecipeCreated { get; }

        /// <summary>
        /// Revert '{0}' to default built-in definition?
        /// </summary>
        string ConfirmResetActiveRecipe { get; }

        /// <summary>
        /// Unload '{0}' from Greenshot?
        /// </summary>
        string ConfirmUnloadActiveRecipe { get; }

        /// <summary>
        /// Your unsaved changes are discarded.
        /// </summary>
        string UnsavedChangesDiscarded { get; }

        /// <summary>
        /// Unloaded recipe '{0}'.
        /// </summary>
        string StatusRecipeUnloaded { get; }

        /// <summary>
        /// Open Greenshot Recipe
        /// </summary>
        string TitleOpenRecipe { get; }

        /// <summary>
        /// Recipe failed validation:
        /// </summary>
        string ErrorRecipeFailedValidation { get; }

        /// <summary>
        /// Recipe failed validation: {0}
        /// </summary>
        string StatusRecipeFailedValidation { get; }

        /// <summary>
        /// Loaded and registered automatic step: {0}
        /// </summary>
        string StatusLoadedExtension { get; }

        /// <summary>
        /// Loaded and registered: {0}
        /// </summary>
        string StatusLoadedRegisteredFile { get; }

        /// <summary>
        /// Loaded: {0}
        /// </summary>
        string StatusLoadedFile { get; }

        /// <summary>
        /// Saved recipe to {0}
        /// </summary>
        string StatusSavedRecipe { get; }

        /// <summary>
        /// Failed to save recipe:
        /// </summary>
        string ErrorSaveRecipe { get; }

        /// <summary>
        /// Error Saving Recipe
        /// </summary>
        string TitleErrorSavingRecipe { get; }

        /// <summary>
        /// Recipe Not Saved
        /// </summary>
        string TitleRecipeNotSaved { get; }

        /// <summary>
        /// The recipe was not saved.
        /// </summary>
        string StatusRecipeNotSaved { get; }

        /// <summary>
        /// Save Greenshot Recipe As...
        /// </summary>
        string TitleSaveRecipeAs { get; }

        /// <summary>
        /// An automatic step runs inside the recipes it changes: save it, switch it on in Settings &gt; Recipes and test run one of those recipes.
        /// </summary>
        string TestRunExtensionInfo { get; }

        /// <summary>
        /// Test Run
        /// </summary>
        string TitleTestRun { get; }

        /// <summary>
        /// Executing test run for '{0}'...
        /// </summary>
        string StatusTestRunExecuting { get; }

        /// <summary>
        /// Test run of '{0}' ended: {1}.
        /// </summary>
        string StatusTestRunEnded { get; }

        /// <summary>
        /// Test run error: {0}
        /// </summary>
        string StatusTestRunError { get; }

        /// <summary>
        /// Applied recipe JSON changes.
        /// </summary>
        string StatusJsonApplied { get; }

        /// <summary>
        /// Invalid JSON syntax or schema:
        /// </summary>
        string ErrorInvalidJson { get; }

        /// <summary>
        /// JSON Error
        /// </summary>
        string TitleJsonError { get; }

        /// <summary>
        /// Copied Mermaid DSL to clipboard.
        /// </summary>
        string StatusMermaidCopied { get; }

        /// <summary>
        /// Circle Color
        /// </summary>
        string ColorCircle { get; }

        /// <summary>
        /// Highlight Color
        /// </summary>
        string ColorHighlight { get; }

        /// <summary>
        /// Background Color
        /// </summary>
        string ColorBackground { get; }

        /// <summary>
        /// Fill Color
        /// </summary>
        string ColorFill { get; }

        /// <summary>
        /// Number Color
        /// </summary>
        string ColorNumber { get; }

        /// <summary>
        /// Text Color
        /// </summary>
        string ColorText { get; }

        /// <summary>
        /// Line Color
        /// </summary>
        string ColorLine { get; }

        /// <summary>
        /// Link / Text
        /// </summary>
        string QrLinkText { get; }

        /// <summary>
        /// Rounded Dots
        /// </summary>
        string QrRoundedDots { get; }

        /// <summary>
        /// {0} custom parameter(s) configured
        /// </summary>
        string AnnotationCustomParameters { get; }

        /// <summary>
        /// Default configuration
        /// </summary>
        string AnnotationDefaultConfiguration { get; }

        /// <summary>
        /// Embedded Cursor (Bitmap)
        /// </summary>
        string AssetEmbeddedCursor { get; }

        /// <summary>
        /// Preset: {0}
        /// </summary>
        string AssetCursorPreset { get; }

        /// <summary>
        /// Embedded Image
        /// </summary>
        string AssetEmbeddedImage { get; }

        /// <summary>
        /// Embedded SVG
        /// </summary>
        string AssetEmbeddedSvg { get; }

        /// <summary>
        /// Select SVG Vector Graphic
        /// </summary>
        string TitleSelectSvg { get; }

        /// <summary>
        /// Scalable Vector Graphics (*.svg)
        /// </summary>
        string FilterSvg { get; }

        /// <summary>
        /// All Files (*.*)
        /// </summary>
        string FilterAllFiles { get; }

        /// <summary>
        /// Select Cursor or Image File
        /// </summary>
        string TitleSelectCursor { get; }

        /// <summary>
        /// Cursor and Image Files (*.cur;*.ico;*.png)
        /// </summary>
        string FilterCursorImage { get; }

        /// <summary>
        /// Select Image File
        /// </summary>
        string TitleSelectImage { get; }

        /// <summary>
        /// Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico)
        /// </summary>
        string FilterImage { get; }

        /// <summary>
        /// After the capture and the selection, before the processors (OCR, ...).
        /// </summary>
        string SlotAfterCapture { get; }

        /// <summary>
        /// When the image is final, once before all destinations.
        /// </summary>
        string SlotBeforeExport { get; }

        /// <summary>
        /// After the destinations, e.g. to do something with an upload link.
        /// </summary>
        string SlotAfterExport { get; }

        /// <summary>
        /// Runs in the export steps for each destination, on its own copy: e.g. a border only for email. Where the slot node is doesn't matter.
        /// </summary>
        string SlotBeforeDestination { get; }

        /// <summary>
        /// Option {0}
        /// </summary>
        string PromptChoiceDefaultLabel { get; }

        /// <summary>
        /// Select Destination Folder for Captures
        /// </summary>
        string TitleSelectCaptureFolder { get; }

        /// <summary>
        /// Select Executable or Script for External Command
        /// </summary>
        string TitleSelectExecutable { get; }

        /// <summary>
        /// Executable &amp; Script Files (*.exe;*.cmd;*.bat;*.ps1;*.vbs)
        /// </summary>
        string FilterExecutable { get; }

        /// <summary>
        /// Select Feedback / Shutter Sound File (.wav)
        /// </summary>
        string TitleSelectSound { get; }

        /// <summary>
        /// Wave Audio Files (*.wav)
        /// </summary>
        string FilterWave { get; }

        /// <summary>
        /// All Audio Files (*.wav;*.mp3)
        /// </summary>
        string FilterAudio { get; }

        /// <summary>
        /// No open image editors were found in Greenshot.
        /// </summary>
        string ImportNoEditors { get; }

        /// <summary>
        /// Would you like to import annotations from a saved .greenshot file instead?
        /// </summary>
        string ImportAskGreenshotFile { get; }

        /// <summary>
        /// No Open Editors Found
        /// </summary>
        string TitleNoEditors { get; }

        /// <summary>
        /// Could not access the surface of the selected editor.
        /// </summary>
        string ImportNoSurface { get; }

        /// <summary>
        /// Import Failed
        /// </summary>
        string TitleImportFailed { get; }

        /// <summary>
        /// The selected image editor does not contain any annotations.
        /// </summary>
        string ImportEditorNoAnnotations { get; }

        /// <summary>
        /// No Annotations Found
        /// </summary>
        string TitleNoAnnotations { get; }

        /// <summary>
        /// Image Editor
        /// </summary>
        string ImportSourceImageEditor { get; }

        /// <summary>
        /// Select Greenshot File to Import Annotations
        /// </summary>
        string TitleSelectGreenshotFile { get; }

        /// <summary>
        /// Greenshot Files (*.greenshot)
        /// </summary>
        string FilterGreenshot { get; }

        /// <summary>
        /// No annotations were found in the selected .greenshot file.
        /// </summary>
        string ImportFileNoAnnotations { get; }

        /// <summary>
        /// Failed to load .greenshot file:
        /// </summary>
        string ImportFileFailed { get; }

        /// <summary>
        /// Import Error
        /// </summary>
        string TitleImportError { get; }

        /// <summary>
        /// Source: {0}
        /// </summary>
        string SummarySource { get; }

        /// <summary>
        /// Mode: {0}
        /// </summary>
        string SummaryMode { get; }

        /// <summary>
        /// Border: {0}px {1}
        /// </summary>
        string SummaryBorder { get; }

        /// <summary>
        /// Shadow: {0}px ({1})
        /// </summary>
        string SummaryShadow { get; }

        /// <summary>
        /// TornEdge ({0})
        /// </summary>
        string SummaryTornEdge { get; }

        /// <summary>
        /// Rotate: {0}°
        /// </summary>
        string SummaryRotate { get; }

        /// <summary>
        /// Resize: {0}%
        /// </summary>
        string SummaryResizePercent { get; }

        /// <summary>
        /// Resize: {0}x{1}
        /// </summary>
        string SummaryResizeSize { get; }

        /// <summary>
        /// Canvas Padding: {0}px
        /// </summary>
        string SummaryCanvasPadding { get; }

        /// <summary>
        /// Monochrome (Thresh: {0})
        /// </summary>
        string SummaryMonochrome { get; }

        /// <summary>
        /// Adjust (B:{0} C:{1} G:{2})
        /// </summary>
        string SummaryAdjust { get; }

        /// <summary>
        /// Reduce Colors ({0})
        /// </summary>
        string SummaryReduceColors { get; }

        /// <summary>
        /// Remove Transparency
        /// </summary>
        string SummaryRemoveTransparency { get; }

        /// <summary>
        /// Grayscale
        /// </summary>
        string SummaryGrayscale { get; }

        /// <summary>
        /// Invert Colors
        /// </summary>
        string SummaryInvert { get; }

        /// <summary>
        /// Effect: {0}
        /// </summary>
        string SummaryEffect { get; }

        /// <summary>
        /// DLP: {0}
        /// </summary>
        string SummaryDlp { get; }

        /// <summary>
        /// {0} item(s): {1}
        /// </summary>
        string SummaryAnnotations { get; }

        /// <summary>
        /// Annotations (0)
        /// </summary>
        string SummaryAnnotationsNone { get; }

        /// <summary>
        /// Variables (0)
        /// </summary>
        string SummaryVariablesNone { get; }

        /// <summary>
        /// Sound: Enabled
        /// </summary>
        string SummarySoundEnabled { get; }

        /// <summary>
        /// Silent
        /// </summary>
        string SummarySilent { get; }

        /// <summary>
        /// Save to: {0}
        /// </summary>
        string SummarySaveTo { get; }

        /// <summary>
        /// Save to File (Default)
        /// </summary>
        string SummarySaveDefault { get; }

        /// <summary>
        /// Clipboard: OCR Text only
        /// </summary>
        string SummaryClipboardOcrText { get; }

        /// <summary>
        /// Clipboard: {0}
        /// </summary>
        string SummaryClipboard { get; }

        /// <summary>
        /// Copy to Clipboard
        /// </summary>
        string SummaryCopyClipboard { get; }

        /// <summary>
        /// Open in Image Editor
        /// </summary>
        string SummaryEditor { get; }

        /// <summary>
        /// Send to Printer
        /// </summary>
        string SummaryPrinter { get; }

        /// <summary>
        /// Send via E-Mail
        /// </summary>
        string SummaryEmail { get; }

        /// <summary>
        /// Custom: {0}
        /// </summary>
        string SummaryCustom { get; }

        /// <summary>
        /// Custom Destination
        /// </summary>
        string SummaryCustomDestination { get; }

        /// <summary>
        /// To: {0}
        /// </summary>
        string SummaryTo { get; }

        /// <summary>
        /// Export targets
        /// </summary>
        string SummaryExportTargets { get; }

        /// <summary>
        /// All Active
        /// </summary>
        string SummaryAllActive { get; }

        /// <summary>
        /// Dynamic Destination ({0}, {1})
        /// </summary>
        string SummaryDynamicDestination { get; }

        /// <summary>
        /// Notification: shown
        /// </summary>
        string SummaryNotificationShown { get; }

        /// <summary>
        /// Notification: hidden
        /// </summary>
        string SummaryNotificationHidden { get; }

        /// <summary>
        /// Where the automatic step starts: connect it to the first step(s)
        /// </summary>
        string SummaryIn { get; }

        /// <summary>
        /// Where the automatic step ends and the recipe goes on
        /// </summary>
        string SummaryOut { get; }

        /// <summary>
        /// Slot {0}: automatic steps are added here
        /// </summary>
        string SummarySlotAll { get; }

        /// <summary>
        /// Slot {0}, accepts: {1}
        /// </summary>
        string SummarySlotAccepts { get; }

        /// <summary>
        /// Stderr (exit {0}): {1}
        /// </summary>
        string SummaryStderr { get; }

        /// <summary>
        /// Stderr (exit {0}, abort): {1}
        /// </summary>
        string SummaryStderrAbort { get; }

        /// <summary>
        /// {0} decision branch(es)
        /// </summary>
        string SummaryBranches { get; }

        /// <summary>
        /// Prompt: {0}
        /// </summary>
        string SummaryPrompt { get; }

        /// <summary>
        /// User Decision Prompt
        /// </summary>
        string SummaryPromptNone { get; }

        /// <summary>
        /// Processors: Windows OCR
        /// </summary>
        string SummaryProcessorsOcr { get; }

        /// <summary>
        /// Processors: {0}
        /// </summary>
        string SummaryProcessors { get; }

        /// <summary>
        /// Processors (None)
        /// </summary>
        string SummaryProcessorsNone { get; }

        /// <summary>
        /// Processors: All ({0})
        /// </summary>
        string SummaryProcessorsAllTiming { get; }

        /// <summary>
        /// Processors: All Active
        /// </summary>
        string SummaryProcessorsAll { get; }

        /// <summary>
        /// Record Video: {0} ({1} FPS, {2})
        /// </summary>
        string SummaryRecordVideo { get; }

        /// <summary>
        /// Run: {0} {1}
        /// </summary>
        string SummaryRun { get; }

        /// <summary>
        /// Execute Command
        /// </summary>
        string SummaryExecuteCommand { get; }

        /// <summary>
        /// Attach to Jira: {0}
        /// </summary>
        string SummaryJira { get; }

        /// <summary>
        /// Attach to Jira Issue
        /// </summary>
        string SummaryJiraNone { get; }

        /// <summary>
        /// Upload to Confluence: {0}
        /// </summary>
        string SummaryConfluence { get; }

        /// <summary>
        /// Upload to Confluence Page
        /// </summary>
        string SummaryConfluenceNone { get; }

        /// <summary>
        /// Export to Microsoft {0}
        /// </summary>
        string SummaryOffice { get; }

        /// <summary>
        /// Scan Barcode / QR Code
        /// </summary>
        string SummaryBarcode { get; }

        /// <summary>
        /// Upload to Box
        /// </summary>
        string SummaryBox { get; }

        /// <summary>
        /// Upload to Dropbox
        /// </summary>
        string SummaryDropbox { get; }
    }
}
