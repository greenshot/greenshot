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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Effects;
using Greenshot.Base.Help;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Controls.Emoji;
using Greenshot.Editor.Destinations;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Fields.Binding;
using Greenshot.Editor.Helpers;
using Greenshot.Base.Threading;
using log4net;
using System.Threading.Tasks;
using System.Threading;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Controls;
using Greenshot.Base.Languages;

namespace Greenshot.Editor.Forms
{
    /// <summary>
    /// The ImageEditorForm is the editor for Greenshot
    /// </summary>
    public partial class ImageEditorForm : EditorForm, IImageEditor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImageEditorForm));
        private static readonly IEditorConfiguration EditorConfiguration = IniConfigHelper.EnsureSection<IEditorConfiguration>(() => new EditorConfigurationImpl());
        private static readonly ICoreConfiguration CoreConfiguration = IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());

        private static readonly List<string> IgnoreDestinations = new()
        {
            nameof(WellKnownDestinations.Picker),
            EditorDestination.DESIGNATION
        };

        private static readonly List<IImageEditor> EditorList = new();
        private static readonly object _editorListLock = new();

        private Surface _surface;
        private ToolStripButton[] _toolbarButtons;

        private bool _originalBoldCheckState;
        private bool _originalItalicCheckState;

        // whether part of the editor controls are disabled depending on selected item(s)
        private bool _controlsDisabledDueToConfirmable;

        // Used for tracking the mouse scroll wheel changes
        private DateTime _zoomStartTime = DateTime.Now;

        /// <summary>
        /// All provided zoom values (in percents) in ascending order.
        /// </summary>
        private readonly Fraction[] ZOOM_VALUES = new Fraction[]
        {
            (1, 4), (1, 2), (2, 3), (3, 4), (1, 1), (2, 1), (3, 1), (4, 1), (6, 1)
        };

        public static List<IImageEditor> Editors
        {
            get
            {
                lock (_editorListLock)
                {
                    try
                    {
                        EditorList.Sort((e1, e2) => string.Compare(e1.Surface.CaptureDetails.Title, e2.Surface.CaptureDetails.Title, StringComparison.Ordinal));
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Sorting of editors failed.", ex);
                    }

                    return EditorList;
                }
            }
        }

        /// <summary>
        /// Adjust the icons etc to the supplied DPI settings
        /// </summary>
        /// <param name="oldDpi"></param>
        /// <param name="newDpi"></param>
        protected override void DpiChangedHandler(int oldDpi, int newDpi)
        {
            var newSize = DpiCalculator.ScaleWithDpi(coreConfiguration.IconSize, newDpi);
            toolsToolStrip.ImageScalingSize = newSize;
            menuStrip1.ImageScalingSize = newSize;
            destinationsToolStrip.ImageScalingSize = newSize;
            propertiesToolStrip.ImageScalingSize = newSize;
            propertiesToolStrip.MinimumSize = new Size(150, newSize.Height + 10);
            _surface?.AdjustToDpi(newDpi);

            // The framework's own DPI-triggered scaling runs after this handler returns, and it resizes
            // the canvas control along with every other control on the form - even though the canvas size
            // must always be image-size * zoom-factor in device pixels, independent of monitor DPI. Redo
            // the adjustment once that scaling has completed, so the canvas ends up at its correct size
            // instead of being left clipped.
if (!IsDisposed && !Disposing && IsHandleCreated)
{
    BeginInvoke(new MethodInvoker(() =>
    {
        if (IsDisposed || Disposing || _surface?.Image == null)
        {
            return;
        }

        _surface.AdjustToDpi(DeviceDpi);
        AlignCanvasPositionAfterResize();
    }));
}

            UpdateUi();
        }

        private bool? _matchSizeToCapture;
        private bool MatchSizeToCapture => _matchSizeToCapture ?? EditorConfiguration.MatchSizeToCapture;

        public ImageEditorForm()
        {
            var image = ImageHelper.CreateEmpty(EditorConfiguration.DefaultEditorSize.Width, EditorConfiguration.DefaultEditorSize.Height, PixelFormat.Format32bppArgb, Color.White, 96f, 96f);
            ISurface surface = new Surface(image);
            Initialize(surface, false);
        }

        public ImageEditorForm(ISurface surface, bool outputMade, bool? matchSizeToCapture = null)
        {
            _matchSizeToCapture = matchSizeToCapture;
            Initialize(surface, outputMade);
        }

        /// <summary>
        /// The images of the controls, embedded as plain files (see EmbeddedResources). They are assigned here and not in the
        /// designer: the designer would put them into the .resx as binary data, which needs System.Resources.Extensions.
        /// Never set an Image in the designer, add the file to Resources and a line here.
        /// </summary>
        private void ApplyImages()
        {
            btnCursor.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnCursor.Image");
            btnRect.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnRect.Image");
            btnEllipse.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnEllipse.Image");
            btnLine.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnLine.Image");
            btnArrow.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnArrow.Image");
            btnFreehand.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnFreehand.Image");
            btnText.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnText.Image");
            btnSpeechBubble.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnSpeechBubble.Image");
            btnStepLabel.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnStepLabel01.Image");
            btnHighlight.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnHighlight.Image");
            btnObfuscate.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnObfuscate.Image");
            toolStripSplitButton1.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "toolStripSplitButton1.Image");
            btnResize.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnResize.Image");
            btnCrop.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnCrop.Image");
            rotateCwToolstripButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "rotateCwToolstripButton.Image");
            rotateCcwToolstripButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "rotateCcwToolstripButton.Image");
            undoToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "undoToolStripMenuItem.Image");
            redoToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "redoToolStripMenuItem.Image");
            cutToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "cutToolStripMenuItem.Image");
            copyToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "copyToolStripMenuItem.Image");
            pasteToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "pasteToolStripMenuItem.Image");
            preferencesToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "preferencesToolStripMenuItem.Image");
            addRectangleToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "addRectangleToolStripMenuItem.Image");
            addEllipseToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "addEllipseToolStripMenuItem.Image");
            drawLineToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "drawLineToolStripMenuItem.Image");
            drawArrowToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "drawArrowToolStripMenuItem.Image");
            drawFreehandToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "drawFreehandToolStripMenuItem.Image");
            addTextBoxToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "addTextBoxToolStripMenuItem.Image");
            addSpeechBubbleToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnSpeechBubble.Image");
            addCounterToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnStepLabel01.Image");
            removeObjectToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "removeObjectToolStripMenuItem.Image");
            helpToolStripMenuItem1.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "helpToolStripMenuItem1.Image");
            btnSave.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnSave.Image");
            btnClipboard.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnClipboard.Image");
            btnPrint.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnPrint.Image");
            btnDelete.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnDelete.Image");
            btnCut.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnCut.Image");
            btnCopy.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnCopy.Image");
            btnPaste.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnPaste.Image");
            btnUndo.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnUndo.Image");
            btnRedo.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnRedo.Image");
            btnSettings.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnSettings.Image");
            btnHelp.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnHelp.Image");
            obfuscateModeButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "obfuscateModeButton.Image");
            pixelizeToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "pixelizeToolStripMenuItem.Image");
            blurToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "blurToolStripMenuItem.Image");
            cropModeButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnCrop.Image");
            defaultCropModeToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnCrop.Image");
            verticalCropModeToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "CropVertical.Image");
            horizontalCropModeToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "CropHorizontal.Image");
            autoCropModeToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "AutoCrop.Image");
            highlightModeButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "highlightModeButton.Image");
            textHighlightMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "textHighlightMenuItem.Image");
            areaHighlightMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "areaHighlightMenuItem.Image");
            grayscaleHighlightMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "grayscaleHighlightMenuItem.Image");
            magnifyMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "magnifyMenuItem.Image");
            btnFillColor.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnFillColor.Image");
            btnLineColor.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnLineColor.Image");
            fontBoldButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "fontBoldButton.Image");
            fontItalicButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "fontItalicButton.Image");
            textVerticalAlignmentButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnAlignMiddle.Image");
            alignTopToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnAlignTop.Image");
            alignMiddleToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnAlignMiddle.Image");
            alignBottomToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnAlignBottom.Image");
            arrowHeadsDropDownButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "arrowHeadsDropDownButton.Image");
            arrowHeadStartMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "arrowHeadStartMenuItem.Image");
            arrowHeadEndMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "arrowHeadEndMenuItem.Image");
            arrowHeadBothMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "arrowHeadBothMenuItem.Image");
            arrowHeadNoneMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "arrowHeadNoneMenuItem.Image");
            shadowButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "shadowButton.Image");
            btnConfirm.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnConfirm.Image");
            btnCancel.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnCancel.Image");
            closeAllToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "closeToolStripMenuItem.Image");
            closeToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "closeToolStripMenuItem.Image");
            textHorizontalAlignmentButton.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnAlignCenter.Image");
            alignLeftToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnAlignLeft.Image");
            alignCenterToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnAlignCenter.Image");
            alignRightToolStripMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnAlignRight.Image");
            zoomInMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "zoomInMenuItem.Image");
            zoomOutMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "zoomOutMenuItem.Image");
            zoomBestFitMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "zoomBestFitMenuItem.Image");
            zoomActualSizeMenuItem.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "zoomActualSizeMenuItem.Image");
            zoomStatusDropDownBtn.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "zoomStatusDropDownBtn.Image");
        }

        private void Initialize(ISurface surface, bool outputMade)
        {
            ThreadAssert.IsUi(nameof(ImageEditorForm));
            var timing = new StartupTiming();

            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();
            ApplyImages();
            timing.Mark("InitializeComponent");
            InitializeLanguage();
            timing.Mark("InitializeLanguage");
            AssignEmojiButtonImageAsync().FireAndLog("Render the emoji button image", Log);
            // Add the destinations after the form is loaded, this is needed for the dynamic destinations which need the handle of the form
            Load += (s, eventArgs) =>
            {
                AddDestinations();
                UpdateRecipesMenu();
            };

            EventHandler recipesChangedHandler = (s, e) =>
            {
                // Raised from file watchers and flows: always marshal to the UI thread
                var ui = SimpleServiceProvider.Current.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance;
                ui.InvokeAsync(() =>
                {
                    if (IsDisposed || Disposing) return;
                    UpdateRecipesMenu();
                }).FireAndLog("Update the editor recipes menu", Log);
            };

            var recipeManager = SimpleServiceProvider.Current.GetInstance<IRecipeManager>(isOptional: true);
            if (recipeManager != null)
            {
                recipeManager.RecipesChanged += recipesChangedHandler;
                FormClosed += (s, e) =>
                {
                    recipeManager.RecipesChanged -= recipesChangedHandler;
                };
            }

            // Keep paste enabled/disabled while the editor is open and something else is copied
            Load += (s, e) => SubscribeToClipboardChanges();
            FormClosed += (s, e) =>
            {
                _clipboardSubscription?.Dispose();
                _clipboardSubscription = null;
            };

            // Make sure the editor is placed on the same location as the last editor was on close
            // But only if this still exists, else it will be reset (BUG-1812)
            timing.Mark("Events");
            WindowPlacement editorWindowPlacement = EditorConfigurationHelper.GetEditorPlacement(EditorConfiguration);
            NativeRect screenBounds = DisplayInfo.ScreenBounds;
            if (!screenBounds.Contains(editorWindowPlacement.NormalPosition))
            {
                EditorConfigurationHelper.ResetEditorPlacement(EditorConfiguration);
            }

            timing.Mark("ScreenBounds");
            ApplyStoredPlacement();

            timing.Mark("Placement");
            // init surface
            Surface = surface;
            timing.Mark("SetSurface");
            // Initial "saved" flag for asking if the image needs to be save
            _surface.Modified = !outputMade;

            // Note: SetSurface (called via Surface = surface above) already registered this
            // editor in EditorList. Do NOT add again here — double-registration causes
            // closed editors to linger in the list because Remove() only removes one entry.

            UpdateUi();
            timing.Mark("UpdateUi");

            // Re-apply the capture title after UpdateUi()/ApplyLanguage() which resets Text
            // to just the bare form language key ("Greenshot editor").
            if (_surface?.CaptureDetails?.Title != null)
            {
                Text = _surface.CaptureDetails.Title + " - " + Texts.Editor.Title;
            }

            // Workaround: for the MouseWheel event which doesn't get to the panel
            MouseWheel += PanelMouseWheel;

            // Use best fit, for those capture modes where we can get huge images
            bool useBestFit = _surface.CaptureDetails.CaptureMode switch
            {
                CaptureMode.File => true,
                CaptureMode.Clipboard => true,
                _ => false
            };

            if (useBestFit)
            {
                ZoomBestFitMenuItemClick(this, EventArgs.Empty);
            }

            // Workaround: As the cursor is (mostly) selected on the surface a funny artifact is visible, this fixes it.
            HideToolstripItems();
            timing.Mark("Rest");
            Log.Debug("Editor constructed: " + timing);
        }

        /// <summary>
        /// Place the editor where the last editor was closed.
        /// With a "show" command SetWindowPlacement would already show the unfinished form, every change after that
        /// (surface, size, texts) would be laid out and painted again. The form is shown by Show(), maximized if it was.
        /// </summary>
        private void ApplyStoredPlacement()
        {
            var placement = EditorConfigurationHelper.GetEditorPlacement(EditorConfiguration);
            bool maximized = placement.ShowCmd == ShowWindowCommands.Maximize;
            placement.ShowCmd = ShowWindowCommands.Hide;
            // ReSharper disable once UnusedVariable
            WindowDetails thisForm = new(Handle)
            {
                WindowPlacement = placement
            };
            if (maximized)
            {
                WindowState = FormWindowState.Maximized;
            }
        }

        /// <summary>
        /// The emoji button image is rendered with ImageSharp, which takes long the first time (loading and JIT-compiling
        /// ImageSharp, parsing the Twemoji font). It's rendered in the background once and shared by all editors,
        /// the button gets it when it's available.
        /// </summary>
        private async Task AssignEmojiButtonImageAsync()
        {
            var image = await EmojiRenderer.GetSharedBitmapAsync(EmojiRenderer.EmojiButtonEmoji, EmojiRenderer.EmojiButtonSize).ConfigureAwait(true);
            if (image == null || IsDisposed || Disposing)
            {
                return;
            }

            btnEmoji.Image = image;
        }

        /// <summary>
        /// Measures the phases of the editor startup, for the log
        /// </summary>
        private sealed class StartupTiming
        {
            private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();
            private readonly System.Text.StringBuilder _phases = new();
            private long _last;

            public void Mark(string phase)
            {
                long now = _stopwatch.ElapsedMilliseconds;
                _phases.Append(phase).Append(' ').Append(now - _last).Append(" ms, ");
                _last = now;
            }

            public override string ToString() => $"{_phases}total {_stopwatch.ElapsedMilliseconds} ms";
        }

        /// <summary>
        /// Remove the current surface
        /// </summary>
        private void RemoveSurface()
        {
            if (_surface == null)
            {
                return;
            }

            panel1.Controls.Remove(_surface);
            _surface.Dispose();
            _surface = null;
        }

        /// <summary>
        /// Change the surface
        /// </summary>
        /// <param name="newSurface"></param>
        private void SetSurface(ISurface newSurface)
        {
            if (Surface != null && Surface.Modified)
            {
                throw new ApplicationException("Surface modified");
            }

            // Remove from the global list while _surface is being swapped, so background threads
            // iterating Editors never observe this editor in a null-surface state.
            lock (_editorListLock)
            {
                EditorList.Remove(this);
            }

            RemoveSurface();

            panel1.Height = 10;
            panel1.Width = 10;
            _surface = newSurface as Surface;
            if (_surface != null)
            {
                panel1.Controls.Add(_surface);
            }

            Image backgroundForTransparency = GreenshotResources.GetImage("Checkerboard.Image");
            if (_surface != null)
            {
                _surface.TransparencyBackgroundBrush = new TextureBrush(backgroundForTransparency, WrapMode.Tile);

                _surface.MovingElementChanged += delegate { RefreshEditorControls(); };
                _surface.DrawingModeChanged += Surface_DrawingModeChanged;
                _surface.SurfaceSizeChanged += SurfaceSizeChanged;
                _surface.SurfaceExpanded += SurfaceExpanded;
                _surface.SurfaceMessage += SurfaceMessageReceived;
                _surface.ForegroundColorChanged += ForegroundColorChanged;
                _surface.BackgroundColorChanged += BackgroundColorChanged;
                _surface.LineThicknessChanged += LineThicknessChanged;
                _surface.ShadowChanged += ShadowChanged;
                _surface.FieldAggregator.FieldChanged += FieldAggregatorFieldChanged;
                SurfaceSizeChanged(Surface, null);

                BindFieldControls();
                RefreshEditorControls();
                // Fix title
                if (_surface?.CaptureDetails?.Title != null)
                {
                    Text = _surface.CaptureDetails.Title + " - " + Texts.Editor.Title;
                }
            }

            // Re-register in the global list now that the new surface is fully assigned.
            lock (_editorListLock)
            {
                EditorList.Add(this);
            }

            Activate();
            WindowDetails.ToForeground(Handle);
        }

        private void UpdateUi()
        {
            // Disable access to the settings, for feature #3521446
            preferencesToolStripMenuItem.Visible = !coreConfiguration.DisableSettings;
            toolStripSeparator12.Visible = !coreConfiguration.DisableSettings;
            toolStripSeparator11.Visible = !coreConfiguration.DisableSettings;
            btnSettings.Visible = !coreConfiguration.DisableSettings;

            // Text obfuscation is only available for beta testers
            obfuscateTextToolStripMenuItem.Visible = CoreConfiguration.IsBetaTester;

            // Make sure Double-buffer is enabled
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // resizing the panel is futile, since it is docked. however, it seems
            // to fix the bug (?) with the vscrollbar not being able to shrink to
            // a smaller size than the initial panel size (as set by the forms designer)
            panel1.Height = 10;

            _toolbarButtons = new[]
            {
                btnCursor, btnRect, btnEllipse, btnText, btnLine, btnArrow, btnFreehand, btnHighlight, btnObfuscate, btnCrop, btnStepLabel, btnSpeechBubble, btnEmoji
            };
            //toolbarDropDownButtons = new ToolStripDropDownButton[]{btnBlur, btnPixeliate, btnTextHighlighter, btnAreaHighlighter, btnMagnifier};

            try
            {
                var editorPlugins = SimpleServiceProvider.Current.GetAllInstances<IEditorPlugin>();
                foreach (var plugin in editorPlugins)
                {
                    plugin.InitializeEditor(this, pluginToolStripMenuItem, Surface);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error initializing editor plugins", ex);
            }

            pluginToolStripMenuItem.Visible = pluginToolStripMenuItem.DropDownItems.Count > 0;

            // Make sure the value is set correctly when starting
            if (Surface != null)
            {
                counterUpDown.Value = Surface.CounterStart;
            }
        }

        /// <summary>
        /// Workaround for having a border around the dropdown
        /// See: https://stackoverflow.com/questions/9560812/change-border-of-toolstripcombobox-with-flat-style
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertiesToolStrip_Paint(object sender, PaintEventArgs e)
        {
            using Pen cbBorderPen = new Pen(SystemColors.ActiveBorder);
            // Loop over all items in the propertiesToolStrip
            foreach (ToolStripItem item in propertiesToolStrip.Items)
            {
                // Only ToolStripComboBox that are visible
                if (item is not ToolStripComboBox { Visible: true } cb)
                {
                    continue;
                }

                if (cb.ComboBox == null) continue;

                // Calculate the rectangle
                var r = new NativeRect(cb.ComboBox.Location.X - 1, cb.ComboBox.Location.Y - 1, cb.ComboBox.Size.Width + 1, cb.ComboBox.Size.Height + 1);

                // Draw the rectangle
                e.Graphics.DrawRectangle(cbBorderPen, r);
            }
        }

        /// <summary>
        /// Get all the destinations and display them in the file menu and the buttons
        /// </summary>
        private void AddDestinations()
        {
            // Create export buttons
            foreach (IDestination destination in DestinationHelper.GetAllDestinations())
            {
                var descriptor = destination.Descriptor;
                if (descriptor.Priority <= 2)
                {
                    continue;
                }

                if (!destination.IsAvailableFor(_surface.CaptureDetails))
                {
                    continue;
                }

                if (descriptor.IconKey == null)
                {
                    continue;
                }

                try
                {
                    AddDestinationButton(destination);
                }
                catch (Exception addingException)
                {
                    Log.WarnFormat("Problem adding destination {0}", destination.Designation);
                    Log.Warn("Exception: ", addingException);
                }
            }
        }

        /// <summary>
        /// Export the surface of this editor to the destination, in the background.
        /// </summary>
        private void ExportTo(IDestination destination)
        {
            DestinationExporter.StartExport(destination, _surface);
        }

        private void AddDestinationButton(IDestination toolstripDestination)
        {
            var descriptor = toolstripDestination.Descriptor;
            if (descriptor.HasDynamicDestinations)
            {
                ToolStripSplitButton destinationButton = new()
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    Size = new Size(23, 22),
                    Text = descriptor.DisplayName,
                };
                DestinationMenuBuilder.AssignIcon(destinationButton, descriptor.IconKey);

                // The ButtonClick, this is for the icon, exports to the destination itself
                destinationButton.ButtonClick += delegate { ExportTo(toolstripDestination); };

                // Generate the entries for the drop down: the destination itself and its dynamic destinations
                int generation = 0;
                bool reopening = false;
                destinationButton.DropDownOpening += delegate
                {
                    if (reopening)
                    {
                        // Shown again after the dynamic destinations were added, the items are complete
                        reopening = false;
                        return;
                    }

                    ClearItems(destinationButton.DropDownItems);
                    destinationButton.DropDownItems.Add(DestinationMenuBuilder.CreateMenuItem(toolstripDestination, _surface.CaptureDetails, ExportTo, addDynamics: false));
                    // Only the latest opening adds its items (a slow COM server could answer after the next opening)
                    int currentGeneration = ++generation;
                    AddDynamicDestinationItemsAsync(destinationButton, toolstripDestination, () => currentGeneration == generation, () => reopening = true)
                        .FireAndLog($"Dynamic destinations of {toolstripDestination.Designation}", Log);
                };

                destinationsToolStrip.Items.Insert(destinationsToolStrip.Items.IndexOf(toolStripSeparator16), destinationButton);
            }
            else
            {
                ToolStripButton destinationButton = new ToolStripButton();
                destinationsToolStrip.Items.Insert(destinationsToolStrip.Items.IndexOf(toolStripSeparator16), destinationButton);
                destinationButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
                destinationButton.Size = new Size(23, 22);
                destinationButton.Text = descriptor.DisplayName;
                destinationButton.Click += delegate { ExportTo(toolstripDestination); };
                DestinationMenuBuilder.AssignIcon(destinationButton, descriptor.IconKey);
            }
        }

        /// <summary>
        /// Add the dynamic destinations to the drop down when they arrive (the destination may have to ask a COM server).
        /// </summary>
        private async Task AddDynamicDestinationItemsAsync(ToolStripSplitButton destinationButton, IDestination destination, Func<bool> isCurrent, Action beforeReopen)
        {
            // Loaded on the thread pool, the continuation is back on the UI thread
            var subDestinations = await DestinationMenuBuilder.LoadDynamicDestinationsAsync(destination, _surface.CaptureDetails).ConfigureAwait(true);
            if (destinationButton.IsDisposed || !isCurrent())
            {
                return;
            }

            // The drop down is shown already, it must be hidden while its items change
            DestinationMenuBuilder.UpdateDropDownItems(destinationButton, () =>
            {
                foreach (var subDestination in subDestinations.Where(d => d != null).OrderBy(d => d, DestinationComparer.Instance))
                {
                    destinationButton.DropDownItems.Add(DestinationMenuBuilder.CreateMenuItem(subDestination, _surface.CaptureDetails, ExportTo, addDynamics: false));
                }
            }, beforeReopen);
        }

        /// <summary>
        /// According to some information I found, the clear doesn't work correctly when the shortcutkeys are set?
        /// This helper method takes care of this.
        /// </summary>
        /// <param name="items"></param>
        private void ClearItems(ToolStripItemCollection items)
        {
            foreach (var item in items)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.ShortcutKeys != Keys.None)
                {
                    menuItem.ShortcutKeys = Keys.None;
                }
            }

            items.Clear();
        }

        private void FileMenuDropDownOpening(object sender, EventArgs eventArgs)
        {
            ClearItems(fileStripMenuItem.DropDownItems);

            // Add the destinations
            foreach (IDestination destination in DestinationHelper.GetAllDestinations())
            {
                if (IgnoreDestinations.Contains(destination.Designation))
                {
                    continue;
                }

                if (!destination.IsAvailableFor(_surface.CaptureDetails))
                {
                    continue;
                }

                ToolStripMenuItem item = DestinationMenuBuilder.CreateMenuItem(destination, _surface.CaptureDetails, ExportTo);
                item.ShortcutKeys = DestinationMenuBuilder.ToKeys(destination.Descriptor.Shortcut);
                fileStripMenuItem.DropDownItems.Add(item);
            }

            // add the elements after the destinations
            fileStripMenuItem.DropDownItems.Add(toolStripSeparator9);
            // Only provide the close all if there is more then one editor open, otherwise it doesn't make sense and clutters the UI
            if (EditorList.Count > 1)
            {
                fileStripMenuItem.DropDownItems.Add(closeAllToolStripMenuItem);
            }
            fileStripMenuItem.DropDownItems.Add(closeToolStripMenuItem);
            // reassign the close shortcuts besause ClearItems above removes them
            closeToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
        }

        /// <summary>
        /// This is the SurfaceMessageEvent receiver which display a message in the status bar if the
        /// surface is exported. It also updates the title to represent the filename, if there is one.
        /// Surface messages are raised on the UI thread (export results are applied there).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs)
        {
            ThreadAssert.IsUi(nameof(SurfaceMessageReceived));
            string dateTime = DateTime.Now.ToLongTimeString();
            // TODO: Fix that we only open files, like in the tooltip
            switch (eventArgs.MessageType)
            {
                case SurfaceMessageTyp.Error:
                    UpdateStatusLabel(dateTime + " - ⚠ " + eventArgs.Message, isError: true);
                    break;
                case SurfaceMessageTyp.FileSaved:
                    // Put the event message on the status label and attach the context menu
                    UpdateStatusLabel(dateTime + " - " + eventArgs.Message, fileSavedStatusContextMenu);
                    // Change title
                    Text = eventArgs.Surface.LastSaveFullPath + " - " + Texts.Editor.Title;
                    break;
                default:
                    // Put the event message on the status label
                    UpdateStatusLabel(dateTime + " - " + eventArgs.Message);
                    break;
            }
        }

        /// <summary>
        /// This is called when the foreground color of the select element chances, used for shortcuts
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="eventArgs">SurfaceForegroundColorEventArgs</param>
        private void ForegroundColorChanged(object sender, SurfaceForegroundColorEventArgs eventArgs)
        {
            _surface.FieldAggregator.GetField(FieldType.LINE_COLOR).Value = eventArgs.Color;
        }

        /// <summary>
        /// This is called when the background color of the select element chances, used for shortcuts
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="eventArgs">SurfaceBackgroundColorEventArgs</param>
        private void BackgroundColorChanged(object sender, SurfaceBackgroundColorEventArgs eventArgs)
        {
            _surface.FieldAggregator.GetField(FieldType.FILL_COLOR).Value = eventArgs.Color;
        }

        /// <summary>
        /// This is called when the line thickness of the select element chances, used for shortcuts
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="eventArgs">SurfaceLineThicknessEventArgs</param>
        private void LineThicknessChanged(object sender, SurfaceLineThicknessEventArgs eventArgs)
        {
            _surface.FieldAggregator.GetField(FieldType.LINE_THICKNESS).Value = eventArgs.Thickness;
        }

        /// <summary>
        /// This is called when the shadow of the select element chances, used for shortcuts
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="eventArgs">SurfaceShadowEventArgs</param>
        private void ShadowChanged(object sender, SurfaceShadowEventArgs eventArgs)
        {
            _surface.FieldAggregator.GetField(FieldType.SHADOW).Value = eventArgs.HasShadow;
        }

        /// <summary>
        /// This is called when the size of the surface chances, used for resizing and displaying the size information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SurfaceSizeChanged(object sender, EventArgs e)
        {
            if (MatchSizeToCapture)
            {
                Size = GetOptimalWindowSize();
            }

            dimensionsLabel.Text = Surface.Image.Width + "x" + Surface.Image.Height;
            AlignCanvasPositionAfterResize();
        }

        /// <summary>
        /// This is called when expanding the surface in one direction, used to accomodate shifting an object to one side.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SurfaceExpanded(object sender, EventArgs e)
        {
            UpdateUndoRedoSurfaceDependencies();
        }

        public ISurface Surface
        {
            get { return _surface; }
            set { SetSurface(value); }
        }

        public void SetImagePath(string fullpath)
        {
            // Check if the editor supports the format
            if (fullpath != null && (fullpath.EndsWith(".ico") || fullpath.EndsWith(".wmf")))
            {
                fullpath = null;
            }

            _surface.LastSaveFullPath = fullpath;

            if (fullpath == null)
            {
                return;
            }

            UpdateStatusLabel(string.Format(Texts.Editor.Imagesaved, fullpath), fileSavedStatusContextMenu);
            Text = Path.GetFileName(fullpath) + " - " + Texts.Editor.Title;
        }

        private void Surface_DrawingModeChanged(object source, SurfaceDrawingModeEventArgs eventArgs)
        {
            switch (eventArgs.DrawingMode)
            {
                case DrawingModes.None:
                    SetButtonChecked(btnCursor);
                    break;
                case DrawingModes.Ellipse:
                    SetButtonChecked(btnEllipse);
                    break;
                case DrawingModes.Rect:
                    SetButtonChecked(btnRect);
                    break;
                case DrawingModes.Text:
                    SetButtonChecked(btnText);
                    break;
                case DrawingModes.SpeechBubble:
                    SetButtonChecked(btnSpeechBubble);
                    break;
                case DrawingModes.StepLabel:
                    SetButtonChecked(btnStepLabel);
                    break;
                case DrawingModes.Line:
                    SetButtonChecked(btnLine);
                    break;
                case DrawingModes.Arrow:
                    SetButtonChecked(btnArrow);
                    break;
                case DrawingModes.Crop:
                    SetButtonChecked(btnCrop);
                    break;
                case DrawingModes.Highlight:
                    SetButtonChecked(btnHighlight);
                    break;
                case DrawingModes.Obfuscate:
                    SetButtonChecked(btnObfuscate);
                    break;
                case DrawingModes.Path:
                    SetButtonChecked(btnFreehand);
                    break;
                case DrawingModes.Emoji:
                    SetButtonChecked(btnEmoji);
                    break;
            }

            RefreshEditorControls();
        }

        /**
         * Interfaces for plugins, see GreenshotInterface for more details!
         */
        public Image GetImageForExport()
        {
            return _surface.GetImageForExport();
        }

        public ICaptureDetails CaptureDetails => _surface.CaptureDetails;

        private void BtnSaveClick(object sender, EventArgs e)
        {
            var destinationDesignation = WellKnownDestinations.FileNoDialog;
            if (_surface.LastSaveFullPath == null)
            {
                destinationDesignation = WellKnownDestinations.FileDialog;
            }

            DestinationHelper.StartExport(destinationDesignation, _surface);
        }

        private void BtnClipboardClick(object sender, EventArgs e)
        {
            DestinationHelper.StartExport(WellKnownDestinations.Clipboard, _surface);
        }

        private void BtnPrintClick(object sender, EventArgs e)
        {
            // The BeginInvoke is a solution for the printdialog not having focus
            BeginInvoke((MethodInvoker)delegate { DestinationHelper.StartExport(WellKnownDestinations.Printer, _surface); });
        }

        private void CloseToolStripMenuItemClick(object sender, EventArgs e)
        {
            CloseEditor(closeAllOpenEditors: false);
        }

        private void CloseAllToolStripMenuItemClick(object sender, EventArgs e)
        {
            CloseEditor(closeAllOpenEditors: true);
        }

        /// <summary>
        /// Closes the current editor or all open editors in <see cref="Editors"/>.
        /// </summary>
        /// <param name="closeAllOpenEditors"></param>
        private void CloseEditor(bool closeAllOpenEditors)
        {
            if (closeAllOpenEditors)
            {
                // we have to copy the list because closing the editor will remove it from the list
                List<ImageEditorForm> closinglist = Editors.OfType<ImageEditorForm>().ToList();
                closinglist.ForEach(e => e.Close());
            }
            else
            {
                Close();
            }
        }

        private void BtnEllipseClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.Ellipse;
            RefreshFieldControls();
        }

        private void BtnCursorClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.None;
            RefreshFieldControls();
        }

        private void BtnRectClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.Rect;
            RefreshFieldControls();
        }

        private void BtnTextClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.Text;
            RefreshFieldControls();
        }

        private void BtnSpeechBubbleClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.SpeechBubble;
            RefreshFieldControls();
        }

        private void BtnStepLabelClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.StepLabel;
            RefreshFieldControls();
        }

        private void BtnEmojiClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.Emoji;
            RefreshFieldControls();
        }

        private void BtnLineClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.Line;
            RefreshFieldControls();
        }

        private void BtnArrowClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.Arrow;
            RefreshFieldControls();
        }

        private void BtnCropClick(object sender, EventArgs e)
        {
            if (_surface.DrawingMode == DrawingModes.Crop) return;

            _surface.DrawingMode = DrawingModes.Crop;
            InitCropMode((CropContainer.CropModes)_surface.FieldAggregator.GetField(FieldType.CROPMODE).Value);
            RefreshFieldControls();
        }

        private void BtnHighlightClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.Highlight;
            RefreshFieldControls();
        }

        private void BtnObfuscateClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.Obfuscate;
            RefreshFieldControls();
        }

        private void BtnFreehandClick(object sender, EventArgs e)
        {
            _surface.DrawingMode = DrawingModes.Path;
            RefreshFieldControls();
        }

        private void SetButtonChecked(ToolStripButton btn)
        {
            UncheckAllToolButtons();
            btn.Checked = true;
        }

        private void UncheckAllToolButtons()
        {
            if (_toolbarButtons != null)
            {
                foreach (ToolStripButton butt in _toolbarButtons)
                {
                    butt.Checked = false;
                }
            }
        }

        private void AddRectangleToolStripMenuItemClick(object sender, EventArgs e)
        {
            BtnRectClick(sender, e);
        }

        private void DrawFreehandToolStripMenuItemClick(object sender, EventArgs e)
        {
            BtnFreehandClick(sender, e);
        }

        private void AddEllipseToolStripMenuItemClick(object sender, EventArgs e)
        {
            BtnEllipseClick(sender, e);
        }

        private void AddTextBoxToolStripMenuItemClick(object sender, EventArgs e)
        {
            BtnTextClick(sender, e);
        }

        private void AddSpeechBubbleToolStripMenuItemClick(object sender, EventArgs e)
        {
            BtnSpeechBubbleClick(sender, e);
        }

        private void AddCounterToolStripMenuItemClick(object sender, EventArgs e)
        {
            BtnStepLabelClick(sender, e);
        }

        private void DrawLineToolStripMenuItemClick(object sender, EventArgs e)
        {
            BtnLineClick(sender, e);
        }

        private void DrawArrowToolStripMenuItemClick(object sender, EventArgs e)
        {
            BtnArrowClick(sender, e);
        }

        private void RemoveObjectToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.RemoveSelectedElements();
        }

        private void BtnDeleteClick(object sender, EventArgs e)
        {
            RemoveObjectToolStripMenuItemClick(sender, e);
        }

        private void CutToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.CutSelectedElements();
            UpdateClipboardSurfaceDependencies();
        }

        private void BtnCutClick(object sender, EventArgs e)
        {
            CutToolStripMenuItemClick(sender, e);
        }

        private void CopyToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.CopySelectedElements();
            UpdateClipboardSurfaceDependencies();
        }

        private void BtnCopyClick(object sender, EventArgs e)
        {
            CopyToolStripMenuItemClick(sender, e);
        }

        private void PasteToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.PasteElementFromClipboard();
            UpdateClipboardSurfaceDependencies();
        }

        private void BtnPasteClick(object sender, EventArgs e)
        {
            PasteToolStripMenuItemClick(sender, e);
        }

        private void UndoToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.Undo();
            UpdateUndoRedoSurfaceDependencies();
        }

        private void BtnUndoClick(object sender, EventArgs e)
        {
            UndoToolStripMenuItemClick(sender, e);
        }

        private void RedoToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.Redo();
            UpdateUndoRedoSurfaceDependencies();
        }

        private void BtnRedoClick(object sender, EventArgs e)
        {
            RedoToolStripMenuItemClick(sender, e);
        }

        private void DuplicateToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.DuplicateSelectedElements();
            UpdateClipboardSurfaceDependencies();
        }

        private void UpOneLevelToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.PullElementsUp();
        }

        private void DownOneLevelToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.PushElementsDown();
        }

        private void UpToTopToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.PullElementsToTop();
        }

        private void DownToBottomToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.PushElementsToBottom();
        }


        private void HelpToolStripMenuItem1Click(object sender, EventArgs e)
        {
            AsyncCommand.Run(HelpFileLoader.LoadHelpAsync, "Load the help");
        }

        private void AboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>();
            mainForm.ShowAbout();
        }

        private void PreferencesToolStripMenuItemClick(object sender, EventArgs e)
        {
            var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>();
            mainForm.ShowSetting();
        }

        private void BtnSettingsClick(object sender, EventArgs e)
        {
            PreferencesToolStripMenuItemClick(sender, e);
        }

        private void BtnHelpClick(object sender, EventArgs e)
        {
            HelpToolStripMenuItem1Click(sender, e);
        }

        private void ImageEditorFormActivated(object sender, EventArgs e)
        {
            UpdateClipboardSurfaceDependencies();
            UpdateUndoRedoSurfaceDependencies();
        }

        private void ImageEditorFormFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_surface.Modified && !EditorConfiguration.SuppressSaveDialogAtClose)
            {
                // Make sure the editor is visible
                WindowDetails.ToForeground(Handle);

                MessageBoxButtons buttons = MessageBoxButtons.YesNoCancel;
                // Disallow "CANCEL" if the application needs to shutdown
                if (e.CloseReason == CloseReason.ApplicationExitCall || e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing)
                {
                    buttons = MessageBoxButtons.YesNo;
                }

                DialogResult result = MessageBox.Show(Texts.Editor.CloseOnSave, Texts.Editor.CloseOnSaveTitle, buttons, MessageBoxIcon.Question);
                if (result.Equals(DialogResult.Cancel))
                {
                    e.Cancel = true;
                    return;
                }

                if (result.Equals(DialogResult.Yes))
                {
                    BtnSaveClick(sender, e);
                    // Check if the save was made, if not it was cancelled so we cancel the closing
                    if (_surface.Modified)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            // persist our geometry string.
            EditorConfigurationHelper.SetEditorPlacement(EditorConfiguration, new WindowDetails(Handle).WindowPlacement);
            IniConfigRegistry.Get().Save();

            // remove from the editor list
            lock (_editorListLock)
            {
                EditorList.Remove(this);
            }

            _surface.Dispose();

            if (coreConfiguration.MinimizeWorkingSetSize)
            {
                GC.Collect();
                PsApi.EmptyWorkingSet();
            }
        }

        private void ImageEditorFormKeyDown(object sender, KeyEventArgs e)
        {
            // LOG.Debug("Got key event "+e.KeyCode + ", " + e.Modifiers);
            // avoid conflict with other shortcuts and
            // make sure there's no selected element claiming input focus
            if (e.Modifiers.Equals(Keys.None) && !_surface.KeysLocked)
            {
                switch (e.KeyCode)
                {
                    case Keys.Escape:
                        BtnCursorClick(sender, e);
                        break;
                    case Keys.R:
                        BtnRectClick(sender, e);
                        break;
                    case Keys.E:
                        BtnEllipseClick(sender, e);
                        break;
                    case Keys.L:
                        BtnLineClick(sender, e);
                        break;
                    case Keys.F:
                        BtnFreehandClick(sender, e);
                        break;
                    case Keys.A:
                        BtnArrowClick(sender, e);
                        break;
                    case Keys.T:
                        BtnTextClick(sender, e);
                        break;
                    case Keys.S:
                        BtnSpeechBubbleClick(sender, e);
                        break;
                    case Keys.I:
                        BtnStepLabelClick(sender, e);
                        break;
                    case Keys.H:
                        BtnHighlightClick(sender, e);
                        break;
                    case Keys.O:
                        BtnObfuscateClick(sender, e);
                        break;
                    case Keys.C:
                        BtnCropClick(sender, e);
                        break;
                    case Keys.M:
                        BtnEmojiClick(sender, e);
                        break;
                    case Keys.Z:
                        BtnResizeClick(sender, e);
                        break;
                }
            }
            else if (e.Modifiers.Equals(Keys.Control))
            {
                switch (e.KeyCode)
                {
                    case Keys.Z:
                        UndoToolStripMenuItemClick(sender, e);
                        break;
                    case Keys.Y:
                        RedoToolStripMenuItemClick(sender, e);
                        break;
                    case Keys.Q: // Dropshadow Ctrl + Q
                        AddDropshadowToolStripMenuItemMouseUp(sender, new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));
                        break;
                    case Keys.B: // Border Ctrl + B
                        AddBorderToolStripMenuItemClick(sender, e);
                        break;
                    case Keys.T: // Torn edge Ctrl + T
                        TornEdgesToolStripMenuItemMouseUp(sender, new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));
                        break;
                    case Keys.I: // Invert Ctrl + I
                        InvertToolStripMenuItemClick(sender, e);
                        break;
                    case Keys.G: // Grayscale Ctrl + G
                        GrayscaleToolStripMenuItemClick(sender, e);
                        break;
                    case Keys.Delete: // Clear capture, use transparent background Ctrl + Delete
                        ClearToolStripMenuItemClick(sender, e);
                        break;
                    case Keys.Oemcomma: // Rotate CCW Ctrl + ,
                        RotateCcwToolstripButtonClick(sender, e);
                        break;
                    case Keys.OemPeriod: // Rotate CW Ctrl + .
                        RotateCwToolstripButtonClick(sender, e);
                        break;
                    case Keys.Add: // Ctrl + Num+
                    case Keys.Oemplus: // Ctrl + +
                        ZoomInMenuItemClick(sender, e);
                        break;
                    case Keys.Subtract: // Ctrl + Num-
                    case Keys.OemMinus: // Ctrl + -
                        ZoomOutMenuItemClick(sender, e);
                        break;
                    case Keys.NumPad0: // Ctrl + Num0
                    case Keys.D0: // Ctrl + 0
                        ZoomSetValueMenuItemClick(zoomActualSizeMenuItem, e);
                        break;
                    case Keys.NumPad9: // Ctrl + Num9
                    case Keys.D9: // Ctrl + 9
                        ZoomBestFitMenuItemClick(sender, e);
                        break;
                }
            }
            else if (e.Modifiers.Equals(Keys.Control | Keys.Shift))
            {
                switch (e.KeyCode)
                {
                    case Keys.Add: // Ctrl + Shift + Num+
                    case Keys.Oemplus: // Ctrl + Shift + +
                        EnlargeCanvasToolStripMenuItemClick(sender, e);
                        break;
                    case Keys.Subtract: // Ctrl + Shift + Num-
                    case Keys.OemMinus: // Ctrl + Shift + -
                        ShrinkCanvasToolStripMenuItemClick(sender, e);
                        break;
                }
            }
        }

        /// <summary>
        /// This is a "work-around" for the MouseWheel event which doesn't get to the panel
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">MouseEventArgs</param>
        private void PanelMouseWheel(object sender, MouseEventArgs e)
        {
            if (System.Windows.Forms.Control.ModifierKeys.Equals(Keys.Control))
            {
                if (_zoomStartTime.AddMilliseconds(100) < DateTime.Now) //waiting for next zoom step 100 ms
                {
                    _zoomStartTime = DateTime.Now;
                    if (e.Delta > 0)
                    {
                        ZoomInMenuItemClick(sender, e);
                    }
                    else if (e.Delta < 0)
                    {
                        ZoomOutMenuItemClick(sender, e);
                    }
                }
            }

            panel1.Focus();
        }

        protected override bool ProcessKeyPreview(ref Message msg)
        {
            // disable default key handling if surface has requested a lock
            if (!_surface.KeysLocked)
            {
                return base.ProcessKeyPreview(ref msg);
            }

            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keys)
        {
            // disable default key handling if surface has requested a lock
            if (!_surface.KeysLocked)
            {
                // Go through the destinations to check the EditorShortcut Keys
                // this way the menu entries don't need to be enabled.
                // This also fixes bugs #3526974 & #3527020
                foreach (IDestination destination in DestinationHelper.GetAllDestinations())
                {
                    if (IgnoreDestinations.Contains(destination.Designation))
                    {
                        continue;
                    }

                    if (!destination.IsAvailableFor(_surface.CaptureDetails))
                    {
                        continue;
                    }

                    if (DestinationMenuBuilder.ToKeys(destination.Descriptor.Shortcut) == keys)
                    {
                        ExportTo(destination);
                        return true;
                    }
                }

                if (!_surface.ProcessCmdKey(keys))
                {
                    return base.ProcessCmdKey(ref msg, keys);
                }
            }

            return false;
        }

        private void UpdateUndoRedoSurfaceDependencies()
        {
            if (_surface == null)
            {
                return;
            }

            bool canUndo = _surface.CanUndo;
            btnUndo.Enabled = canUndo;
            undoToolStripMenuItem.Enabled = canUndo;
            // The text has a placeholder for the name of the action, the actions have no names (yet)
            string undoText = string.Format(Texts.Editor.Undo, string.Empty);
            btnUndo.Text = undoText;
            undoToolStripMenuItem.Text = undoText;

            bool canRedo = _surface.CanRedo;
            btnRedo.Enabled = canRedo;
            redoToolStripMenuItem.Enabled = canRedo;
            string redoText = string.Format(Texts.Editor.Redo, string.Empty);
            btnRedo.Text = redoText;
            redoToolStripMenuItem.Text = redoText;
        }

        private void UpdateClipboardSurfaceDependencies()
        {
            if (_surface == null)
            {
                return;
            }

            // check dependencies for the Surface
            bool hasItems = _surface.HasSelectedElements;
            bool actionAllowedForSelection = hasItems && !_controlsDisabledDueToConfirmable;

            // buttons
            btnCut.Enabled = actionAllowedForSelection;
            btnCopy.Enabled = actionAllowedForSelection;
            btnDelete.Enabled = actionAllowedForSelection;

            // menus
            removeObjectToolStripMenuItem.Enabled = actionAllowedForSelection;
            copyToolStripMenuItem.Enabled = actionAllowedForSelection;
            cutToolStripMenuItem.Enabled = actionAllowedForSelection;
            duplicateToolStripMenuItem.Enabled = actionAllowedForSelection;

            // check dependencies for the Clipboard
            // This runs when the editor opens or is activated. Phase 1 only checks the formats, without opening the clipboard;
            // only when a file list, virtual files or HTML could contain an image, phase 2 looks at them in the background.
            bool? clipboardImage = ClipboardHelper.ContainsImageQuick();
            bool hasClipboard = DrawableContainerClipboard.IsAvailable || ClipboardHelper.ContainsText() || clipboardImage == true;
            SetPasteEnabled(hasClipboard);
            if (!hasClipboard && clipboardImage == null)
            {
                EnablePasteForClipboardImageAsync().FireAndLog("Check the clipboard for an image", Log);
            }
        }

        private IDisposable _clipboardSubscription;

        /// <summary>
        /// Update the paste commands when the clipboard changes. The update information arrives on the SharedMessageWindow thread
        /// without opening the clipboard; after a short throttle (the copying application may still be busy) the check runs on the UI thread.
        /// </summary>
        private void SubscribeToClipboardChanges()
        {
            var ui = SimpleServiceProvider.Current.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance;
            try
            {
                _clipboardSubscription = ClipboardNative.OnUpdate
                    // Every subscriber first gets the current state, which the form already checked
                    .Skip(1)
                    .Throttle(TimeSpan.FromMilliseconds(150))
                    .Subscribe(_ => ui.InvokeAsync(() =>
                    {
                        if (IsDisposed || Disposing) return;
                        UpdateClipboardSurfaceDependencies();
                    }).FireAndLog("Update the paste commands after a clipboard change", Log),
                    ex => Log.Warn("Clipboard change notifications stopped", ex));
            }
            catch (Exception ex)
            {
                // E.g. while the process is exiting the SharedMessageWindow isn't created anymore
                Log.Warn("Couldn't subscribe to clipboard changes", ex);
            }
        }

        private void SetPasteEnabled(bool hasClipboard)
        {
            btnPaste.Enabled = hasClipboard && !_controlsDisabledDueToConfirmable;
            pasteToolStripMenuItem.Enabled = hasClipboard && !_controlsDisabledDueToConfirmable;
        }

        /// <summary>
        /// Phase 2 of the clipboard check: continues on the UI thread, enables paste when the clipboard has an image after all
        /// </summary>
        private async Task EnablePasteForClipboardImageAsync()
        {
            if (await ClipboardHelper.ContainsImageAsync() && !IsDisposed)
            {
                SetPasteEnabled(true);
            }
        }

        private void UpdateStatusLabel(string text, ContextMenuStrip contextMenu = null, bool isError = false)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = isError ? Color.DarkRed : SystemColors.ControlText;
            statusStrip1.ContextMenuStrip = contextMenu;
        }

        private void StatusLabelClicked(object sender, MouseEventArgs e)
        {
            ToolStrip ss = (StatusStrip)((ToolStripStatusLabel)sender).Owner;
            ss.ContextMenuStrip?.Show(ss, e.X, e.Y);
        }

        private void CopyPathMenuItemClick(object sender, EventArgs e)
        {
            ClipboardHelper.SetClipboardData(_surface.LastSaveFullPath);
        }

        private void OpenDirectoryMenuItemClick(object sender, EventArgs e)
        {
            ExplorerHelper.OpenInExplorer(_surface.LastSaveFullPath);
        }

        private void BindFieldControls()
        {
            // TODO: This is actually risky, if there are no references than the objects may be garbage collected
            new BidirectionalBinding(btnFillColor, "SelectedColor", _surface.FieldAggregator.GetField(FieldType.FILL_COLOR), "Value", NotNullValidator.GetInstance());
            new BidirectionalBinding(btnLineColor, "SelectedColor", _surface.FieldAggregator.GetField(FieldType.LINE_COLOR), "Value", NotNullValidator.GetInstance());
            new BidirectionalBinding(lineThicknessUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.LINE_THICKNESS), "Value", DecimalIntConverter.GetInstance(),
                NotNullValidator.GetInstance());
            new BidirectionalBinding(blurRadiusUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.BLUR_RADIUS), "Value", DecimalIntConverter.GetInstance(),
                NotNullValidator.GetInstance());
            new BidirectionalBinding(magnificationFactorUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.MAGNIFICATION_FACTOR), "Value",
                DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
            new BidirectionalBinding(pixelSizeUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.PIXEL_SIZE), "Value", DecimalIntConverter.GetInstance(),
                NotNullValidator.GetInstance());
            new BidirectionalBinding(brightnessUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.BRIGHTNESS), "Value", DecimalDoublePercentageConverter.GetInstance(),
                NotNullValidator.GetInstance());
            new BidirectionalBinding(fontFamilyComboBox, "Text", _surface.FieldAggregator.GetField(FieldType.FONT_FAMILY), "Value", NotNullValidator.GetInstance());
            new BidirectionalBinding(fontSizeUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.FONT_SIZE), "Value", DecimalFloatConverter.GetInstance(),
                NotNullValidator.GetInstance());
            new BidirectionalBinding(fontBoldButton, "Checked", _surface.FieldAggregator.GetField(FieldType.FONT_BOLD), "Value", NotNullValidator.GetInstance());
            new BidirectionalBinding(fontItalicButton, "Checked", _surface.FieldAggregator.GetField(FieldType.FONT_ITALIC), "Value", NotNullValidator.GetInstance());
            new BidirectionalBinding(textHorizontalAlignmentButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.TEXT_HORIZONTAL_ALIGNMENT), "Value",
                NotNullValidator.GetInstance());
            new BidirectionalBinding(textVerticalAlignmentButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.TEXT_VERTICAL_ALIGNMENT), "Value",
                NotNullValidator.GetInstance());
            new BidirectionalBinding(shadowButton, "Checked", _surface.FieldAggregator.GetField(FieldType.SHADOW), "Value", NotNullValidator.GetInstance());
            new BidirectionalBinding(previewQualityUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.PREVIEW_QUALITY), "Value",
                DecimalDoublePercentageConverter.GetInstance(), NotNullValidator.GetInstance());
            new BidirectionalBinding(obfuscateModeButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.PREPARED_FILTER_OBFUSCATE), "Value");
            new BidirectionalBinding(cropModeButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.CROPMODE), "Value");
            new BidirectionalBinding(highlightModeButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.PREPARED_FILTER_HIGHLIGHT), "Value");
            new BidirectionalBinding(arrowHeadsDropDownButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.ARROWHEADS), "Value",
                NotNullValidator.GetInstance());
            new BidirectionalBinding(counterUpDown, "Value", _surface, "CounterStart", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
        }

        /// <summary>
        /// shows/hides field controls (2nd toolbar on top) depending on fields of selected elements
        /// </summary>
        private void RefreshFieldControls()
        {
            if (IsDisposed || Disposing) return;
            propertiesToolStrip.SuspendLayout();
            if (_surface.HasSelectedElements || _surface.DrawingMode != DrawingModes.None)
            {
                var props = (FieldAggregator)_surface.FieldAggregator;
                btnFillColor.Visible = props.HasFieldValue(FieldType.FILL_COLOR);
                btnLineColor.Visible = props.HasFieldValue(FieldType.LINE_COLOR);
                lineThicknessLabel.Visible = lineThicknessUpDown.Visible = props.HasFieldValue(FieldType.LINE_THICKNESS);
                blurRadiusLabel.Visible = blurRadiusUpDown.Visible = props.HasFieldValue(FieldType.BLUR_RADIUS);
                previewQualityLabel.Visible = previewQualityUpDown.Visible = props.HasFieldValue(FieldType.PREVIEW_QUALITY);
                magnificationFactorLabel.Visible = magnificationFactorUpDown.Visible = props.HasFieldValue(FieldType.MAGNIFICATION_FACTOR);
                pixelSizeLabel.Visible = pixelSizeUpDown.Visible = props.HasFieldValue(FieldType.PIXEL_SIZE);
                brightnessLabel.Visible = brightnessUpDown.Visible = props.HasFieldValue(FieldType.BRIGHTNESS);
                arrowHeadsLabel.Visible = arrowHeadsDropDownButton.Visible = props.HasFieldValue(FieldType.ARROWHEADS);
                if (props.HasFieldValue(FieldType.ARROWHEADS))
                {
                    SyncArrowHeadControls((ArrowContainer.ArrowHeadCombination)props.GetFieldValue(FieldType.ARROWHEADS));
                }
                fontFamilyComboBox.Visible = props.HasFieldValue(FieldType.FONT_FAMILY);
                fontSizeLabel.Visible = fontSizeUpDown.Visible = props.HasFieldValue(FieldType.FONT_SIZE);
                fontBoldButton.Visible = props.HasFieldValue(FieldType.FONT_BOLD);
                fontItalicButton.Visible = props.HasFieldValue(FieldType.FONT_ITALIC);
                textHorizontalAlignmentButton.Visible = props.HasFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT);
                textVerticalAlignmentButton.Visible = props.HasFieldValue(FieldType.TEXT_VERTICAL_ALIGNMENT);
                shadowButton.Visible = props.HasFieldValue(FieldType.SHADOW);
                counterLabel.Visible = counterUpDown.Visible = props.HasFieldValue(FieldType.FLAGS) && ((FieldFlag)props.GetFieldValue(FieldType.FLAGS)).HasFlag(FieldFlag.COUNTER);

                btnConfirm.Visible = btnCancel.Visible = props.HasFieldValue(FieldType.FLAGS) && ((FieldFlag)props.GetFieldValue(FieldType.FLAGS)).HasFlag(FieldFlag.CONFIRMABLE);
                btnConfirm.Enabled = _surface.HasSelectedElements;

                obfuscateModeButton.Visible = props.HasFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE);
                cropModeButton.Visible = props.HasFieldValue(FieldType.CROPMODE);
                highlightModeButton.Visible = props.HasFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT);
            }
            else
            {
                HideToolstripItems();
            }

            propertiesToolStrip.ResumeLayout();
        }

        private void HideToolstripItems()
        {
            if (IsDisposed || Disposing) return;
            foreach (ToolStripItem toolStripItem in propertiesToolStrip.Items)
            {
                toolStripItem.Visible = false;
            }
        }

        /// <summary>
        /// refreshes all editor controls depending on selected elements and their fields
        /// </summary>
        private void RefreshEditorControls()
        {
            if (IsDisposed || Disposing) return;
            int stepLabels = _surface.CountStepLabels(null);
            Image icon;
            if (stepLabels <= 20)
            {
                icon = EmbeddedResources.GetImage(typeof(ImageEditorForm), $"btnStepLabel{stepLabels:00}.Image");
            }
            else
            {
                icon = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnStepLabel20+.Image");
            }

            btnStepLabel.Image = icon;
            addCounterToolStripMenuItem.Image = icon;

            FieldAggregator props = (FieldAggregator)_surface.FieldAggregator;
            // if a confirmable element is selected, we must disable most of the controls
            // since we demand confirmation or cancel for confirmable element
            if (props.HasFieldValue(FieldType.FLAGS) && ((FieldFlag)props.GetFieldValue(FieldType.FLAGS) & FieldFlag.CONFIRMABLE) == FieldFlag.CONFIRMABLE)
            {
                // disable most controls
                if (!_controlsDisabledDueToConfirmable)
                {
                    ToolStripItemEndisabler.Disable(menuStrip1);
                    ToolStripItemEndisabler.Disable(destinationsToolStrip);
                    ToolStripItemEndisabler.Disable(toolsToolStrip);
                    ToolStripItemEndisabler.Enable(closeToolStripMenuItem);
                    ToolStripItemEndisabler.Enable(closeAllToolStripMenuItem);
                    ToolStripItemEndisabler.Enable(helpToolStripMenuItem);
                    ToolStripItemEndisabler.Enable(aboutToolStripMenuItem);
                    ToolStripItemEndisabler.Enable(preferencesToolStripMenuItem);
                    _controlsDisabledDueToConfirmable = true;
                }
            }
            else if (_controlsDisabledDueToConfirmable)
            {
                // re-enable disabled controls, confirmable element has either been confirmed or cancelled
                ToolStripItemEndisabler.Enable(menuStrip1);
                ToolStripItemEndisabler.Enable(destinationsToolStrip);
                ToolStripItemEndisabler.Enable(toolsToolStrip);
                _controlsDisabledDueToConfirmable = false;
            }

            // en/disable controls depending on whether an element is selected at all
            UpdateClipboardSurfaceDependencies();
            UpdateUndoRedoSurfaceDependencies();

            // Show/hide remove transparency menu item based on whether image has transparency
            if (_surface?.Image != null)
            {
                removeTransparencyToolStripMenuItem.Visible = Image.IsAlphaPixelFormat(_surface.Image.PixelFormat);
            }

            // en/disablearrage controls depending on hierarchy of selected elements
            bool actionAllowedForSelection = _surface.HasSelectedElements && !_controlsDisabledDueToConfirmable;
            bool push = actionAllowedForSelection && _surface.CanPushSelectionDown();
            bool pull = actionAllowedForSelection && _surface.CanPullSelectionUp();
            arrangeToolStripMenuItem.Enabled = push || pull;
            if (arrangeToolStripMenuItem.Enabled)
            {
                upToTopToolStripMenuItem.Enabled = pull;
                upOneLevelToolStripMenuItem.Enabled = pull;
                downToBottomToolStripMenuItem.Enabled = push;
                downOneLevelToolStripMenuItem.Enabled = push;
            }

            // finally show/hide field controls depending on the fields of selected elements
            RefreshFieldControls();
        }


        private void ArrowHeadsDropDownButtonDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            SyncArrowHeadMenuChecked((ArrowContainer.ArrowHeadCombination)e.ClickedItem.Tag);
        }

        private void SyncArrowHeadControls(ArrowContainer.ArrowHeadCombination value)
        {
            arrowHeadsDropDownButton.SelectedTag = value;
            SyncArrowHeadMenuChecked(value);
        }

        private void SyncArrowHeadMenuChecked(ArrowContainer.ArrowHeadCombination value)
        {
            foreach (ToolStripItem item in arrowHeadsDropDownButton.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.Checked = menuItem.Tag != null && menuItem.Tag.Equals(value);
                }
            }
        }

        private void EditToolStripMenuItemClick(object sender, EventArgs e)
        {
            UpdateClipboardSurfaceDependencies();
            UpdateUndoRedoSurfaceDependencies();
        }

        private void FontPropertyChanged(object sender, EventArgs e)
        {
            // in case we forced another FontStyle before, reset it first.
            if (fontBoldButton != null && _originalBoldCheckState != fontBoldButton.Checked)
            {
                fontBoldButton.Checked = _originalBoldCheckState;
            }

            if (fontItalicButton != null && _originalItalicCheckState != fontItalicButton.Checked)
            {
                fontItalicButton.Checked = _originalItalicCheckState;
            }

            var fontFamily = fontFamilyComboBox.FontFamily;

            bool boldAvailable = fontFamily.IsStyleAvailable(FontStyle.Bold);
            if (fontBoldButton != null)
            {
                if (!boldAvailable)
                {
                    _originalBoldCheckState = fontBoldButton.Checked;
                    fontBoldButton.Checked = false;
                }

                fontBoldButton.Enabled = boldAvailable;
            }

            bool italicAvailable = fontFamily.IsStyleAvailable(FontStyle.Italic);
            if (fontItalicButton != null)
            {
                if (!italicAvailable)
                {
                    fontItalicButton.Checked = false;
                }

                fontItalicButton.Enabled = italicAvailable;
            }

            bool regularAvailable = fontFamily.IsStyleAvailable(FontStyle.Regular);
            if (regularAvailable)
            {
                return;
            }

            if (boldAvailable)
            {
                if (fontBoldButton != null)
                {
                    fontBoldButton.Checked = true;
                }
            }
            else if (italicAvailable)
            {
                if (fontItalicButton != null)
                {
                    fontItalicButton.Checked = true;
                }
            }
        }

        private void FieldAggregatorFieldChanged(object sender, FieldChangedEventArgs e)
        {
            // in addition to selection, deselection of elements, we need to
            // refresh toolbar if prepared filter mode is changed
            if (Equals(e.Field.FieldType, FieldType.PREPARED_FILTER_HIGHLIGHT))
            {
                RefreshFieldControls();
            }
        }

        private void FontBoldButtonClick(object sender, EventArgs e)
        {
            _originalBoldCheckState = fontBoldButton.Checked;
        }

        private void FontItalicButtonClick(object sender, EventArgs e)
        {
            _originalItalicCheckState = fontItalicButton.Checked;
        }

        private void ToolBarFocusableElementGotFocus(object sender, EventArgs e)
        {
            _surface.KeysLocked = true;
        }

        private void ToolBarFocusableElementLostFocus(object sender, EventArgs e)
        {
            _surface.KeysLocked = false;
        }

        private void SaveElementsToolStripMenuItemClick(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Greenshot templates (*.gst)|*.gst",
                FileName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(coreConfiguration.OutputFileFilenamePattern, _surface.CaptureDetails)
            };
            DialogResult dialogResult = saveFileDialog.ShowDialog();
            if (dialogResult.Equals(DialogResult.OK))
            {
                using Stream streamWrite = File.OpenWrite(saveFileDialog.FileName);
                _surface.SaveElementsToStream(streamWrite);
            }
        }

        private void LoadElementsToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Greenshot templates (*.gst)|*.gst"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (Stream streamRead = File.OpenRead(openFileDialog.FileName))
                {
                    _surface.LoadElementsFromStream(streamRead);
                }

                _surface.Refresh();
            }
        }

        private void DestinationToolStripMenuItemClick(object sender, EventArgs e)
        {
            IDestination clickedDestination = null;
            if (sender is Control control)
            {
                Control clickedControl = control;
                if (clickedControl.ContextMenuStrip != null)
                {
                    clickedControl.ContextMenuStrip.Show(Cursor.Position);
                    return;
                }

                clickedDestination = (IDestination)clickedControl.Tag;
            }
            else
            {
                if (sender is ToolStripMenuItem item)
                {
                    ToolStripMenuItem clickedMenuItem = item;
                    clickedDestination = (IDestination)clickedMenuItem.Tag;
                }
            }

            // The modified state is cleared when the export succeeds
            if (clickedDestination != null)
            {
                ExportTo(clickedDestination);
            }
        }

        protected void FilterPresetDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            RefreshFieldControls();
            Invalidate(true);
        }

        protected void CropStyleDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            InitCropMode((CropContainer.CropModes)e.ClickedItem.Tag);

            RefreshFieldControls();
            Invalidate(true);
        }

        private void InitCropMode(CropContainer.CropModes mode)
        {
            var cropArea = _surface.Elements.FirstOrDefault(c => c is CropContainer)?.Bounds;

            _surface.DrawingMode = DrawingModes.None;
            _surface.RemoveCropContainer();

            if (mode == CropContainer.CropModes.AutoCrop)
            {
                if (!_surface.AutoCrop(cropArea))
                {
                    //not AutoCrop possible automatic switch to default crop mode
                    _surface.DrawingMode = DrawingModes.Crop;
                    _surface.FieldAggregator.GetField(FieldType.CROPMODE).Value = CropContainer.CropModes.Default;
                    this.cropModeButton.SelectedTag = CropContainer.CropModes.Default;
                    this.statusLabel.Text = Texts.Editor.AutocropNotPossible;
                }
            }
            else
            {
                _surface.DrawingMode = DrawingModes.Crop;
            }
            RefreshEditorControls();
        }

        private void SelectAllToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.SelectAllElements();
        }


        private void BtnConfirmClick(object sender, EventArgs e)
        {
            _surface.Confirm(true);
            RefreshEditorControls();
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            _surface.Confirm(false);
            RefreshEditorControls();
        }

        private void Insert_window_toolstripmenuitemMouseEnter(object sender, EventArgs e)
        {
            ToolStripMenuItem captureWindowMenuItem = (ToolStripMenuItem)sender;
            var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>();
            mainForm.AddCaptureWindowMenuItems(captureWindowMenuItem, Contextmenu_window_Click);
        }

        private void ObfuscateTextToolStripMenuItemClick(object sender, EventArgs e)
        {
            AsyncCommand.Run(ObfuscateTextAsync, "Obfuscate text");
        }

        private async Task ObfuscateTextAsync()
        {
            if (_surface?.CaptureDetails == null)
            {
                MessageBox.Show(Texts.Editor.ObfuscateTextNoCapture, Texts.Editor.ObfuscateTextTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_surface.CaptureDetails.ProcessingTask != null && !_surface.CaptureDetails.ProcessingTask.IsCompleted)
            {
                Cursor = Cursors.WaitCursor;
                try
                {
                    await _surface.CaptureDetails.ProcessingTask;
                }
                catch (Exception ex)
                {
                    Log.Error("Error waiting for background OCR processing in editor", ex);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }

            List<IOcrLineFeature> ocrLines;
            lock (_surface.CaptureDetails.Features)
            {
                ocrLines = _surface.CaptureDetails.Features.OfType<IOcrLineFeature>().ToList();
            }

            if (!ocrLines.Any())
            {
                var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>();
                if (ocrProvider == null)
                {
                    MessageBox.Show(Texts.Editor.ObfuscateTextNoOcrProvider, Texts.Editor.ObfuscateTextTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Cursor = Cursors.WaitCursor;
                try
                {
                    var detectedLines = await ocrProvider.DoOcrAsync(_surface);
                    if (detectedLines != null && detectedLines.Any())
                    {
                        lock (_surface.CaptureDetails.Features)
                        {
                            _surface.CaptureDetails.Features.AddRange(detectedLines);
                        }
                        ocrLines = detectedLines;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error performing OCR", ex);
                    MessageBox.Show(Texts.Editor.ObfuscateTextOcrFailed + ": " + ex.Message, Texts.Editor.ObfuscateTextTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }

            if (!ocrLines.Any())
            {
                MessageBox.Show(Texts.Editor.ObfuscateTextNoText, Texts.Editor.ObfuscateTextTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dialog = new TextObfuscationWindow(_surface, ocrLines);
            dialog.ShowDialog(this);
        }

        private void Contextmenu_window_Click(object sender, EventArgs e)
        {
            var clickedItem = (ToolStripMenuItem)sender;
            AsyncCommand.Run(() => CaptureWindowIntoEditorAsync((WindowDetails)clickedItem.Tag), "Capture a window into the editor");
        }

        private async Task CaptureWindowIntoEditorAsync(WindowDetails windowToCapture)
        {
            try
            {
                ICapture capture = new Capture();
                using (Graphics graphics = Graphics.FromHwnd(Handle))
                {
                    capture.CaptureDetails.DpiX = graphics.DpiY;
                    capture.CaptureDetails.DpiY = graphics.DpiY;
                }

                var captureHelper = SimpleServiceProvider.Current.GetInstance<ICaptureHelper>();
                windowToCapture = captureHelper.SelectCaptureWindow(windowToCapture);
                if (windowToCapture != null)
                {
                    // Continues on the UI thread (the context is captured), where the surface is changed
                    capture = await captureHelper.CaptureWindowAsync(windowToCapture, capture, coreConfiguration.WindowCaptureMode).ConfigureAwait(true);
                    if (capture?.CaptureDetails != null && capture.Image != null)
                    {
                        ((Bitmap)capture.Image).SetResolution(capture.CaptureDetails.DpiX, capture.CaptureDetails.DpiY);
                        _surface.AddImageContainer((Bitmap)capture.Image, 100, 100);
                    }

                    Activate();
                    WindowDetails.ToForeground(Handle);
                }

                capture?.Dispose();
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        /// <summary>
        /// Apply the effect (calculated on the thread pool), then update the undo/redo state
        /// </summary>
        private void ApplyEffect(IEffect effect)
        {
            AsyncCommand.Run(async () =>
            {
                await _surface.ApplyBitmapEffectAsync(effect);
                UpdateUndoRedoSurfaceDependencies();
            }, $"Apply {effect.GetType().Name}");
        }

        private void AddBorderToolStripMenuItemClick(object sender, EventArgs e)
        {
            ApplyEffect(new BorderEffect());
        }

        /// <summary>
        /// Added for FEATURE-919, increasing the canvas by 25 pixels in every direction.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnlargeCanvasToolStripMenuItemClick(object sender, EventArgs e)
        {
            ApplyEffect(new ResizeCanvasEffect(25, 25, 25, 25));
        }

        /// <summary>
        /// Added for FEATURE-919, to make the capture as small as possible again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShrinkCanvasToolStripMenuItemClick(object sender, EventArgs e)
        {
            NativeRect cropRectangle;
            using (Image tmpImage = GetImageForExport())
            {
                cropRectangle = ImageHelper.FindAutoCropRectangle(tmpImage, coreConfiguration.AutoCropDifference);
            }

            if (_surface.IsCropPossible(ref cropRectangle, CropContainer.CropModes.AutoCrop))
            {
                _surface.ApplyCrop(cropRectangle);
                UpdateUndoRedoSurfaceDependencies();
            }
        }

        /// <summary>
        /// This is used when the dropshadow button is used
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">MouseEventArgs</param>
        private void AddDropshadowToolStripMenuItemMouseUp(object sender, MouseEventArgs e)
        {
            var dropShadowEffect = EditorConfiguration.DropShadowEffectSettings;
            bool apply;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    apply = true;
                    break;
                case MouseButtons.Right:
                    var result = new DropShadowSettingsWindow(dropShadowEffect).ShowDialog(this);
                    apply = result == true;
                    break;
                default:
                    return;
            }

            if (apply)
            {
                ApplyEffect(dropShadowEffect);
            }
        }


        /// <summary>
        /// Open the resize settings from, and resize if ok was pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnResizeClick(object sender, EventArgs e)
        {
            var resizeEffect = new ResizeEffect(_surface.Image.Width, _surface.Image.Height, true);
            var result = new ResizeSettingsWindow(resizeEffect).ShowDialog(this);
            if (result == true)
            {
                ApplyEffect(resizeEffect);
            }
        }

        /// <summary>
        /// This is used when the torn-edge button is used
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">MouseEventArgs</param>
        private void TornEdgesToolStripMenuItemMouseUp(object sender, MouseEventArgs e)
        {
            var tornEdgeEffect = EditorConfiguration.TornEdgeEffectSettings;
            bool apply;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    apply = true;
                    break;
                case MouseButtons.Right:
                    var result = new TornEdgeSettingsWindow(tornEdgeEffect).ShowDialog(this);
                    apply = result == true;
                    break;
                default:
                    return;
            }

            if (apply)
            {
                ApplyEffect(tornEdgeEffect);
            }
        }

        private void GrayscaleToolStripMenuItemClick(object sender, EventArgs e)
        {
            ApplyEffect(new GrayscaleEffect());
        }

        private void ClearToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.Clear(Color.Transparent);
            UpdateUndoRedoSurfaceDependencies();
        }

        private void RotateCwToolstripButtonClick(object sender, EventArgs e)
        {
            ApplyEffect(new RotateEffect(90));
        }

        private void RotateCcwToolstripButtonClick(object sender, EventArgs e)
        {
            ApplyEffect(new RotateEffect(270));
        }

        private void InvertToolStripMenuItemClick(object sender, EventArgs e)
        {
            ApplyEffect(new InvertEffect());
        }

        private void RemoveTransparencyToolStripMenuItemClick(object sender, EventArgs e)
        {
            var colorDialog = new ColorDialog
            {
                Color = Color.White
            };

            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                var removeTransparencyEffect = new RemoveTransparencyEffect
                {
                    Color = colorDialog.Color
                };
                ApplyEffect(removeTransparencyEffect);
            }
        }

        private void ImageEditorFormResize(object sender, EventArgs e)
        {
            AlignCanvasPositionAfterResize();
        }

        private void AlignCanvasPositionAfterResize()
        {
            if (Surface?.Image == null || panel1 == null)
            {
                return;
            }

            var canvas = Surface as Control;
            Size canvasSize = canvas.Size;
            Size currentClientSize = panel1.ClientSize;
            Panel panel = (Panel)canvas?.Parent;
            if (panel == null)
            {
                return;
            }

            int offsetX = -panel.HorizontalScroll.Value;
            int offsetY = -panel.VerticalScroll.Value;
            if (currentClientSize.Width > canvasSize.Width)
            {
                canvas.Left = offsetX + (currentClientSize.Width - canvasSize.Width) / 2;
            }
            else
            {
                canvas.Left = offsetX + 0;
            }

            if (currentClientSize.Height > canvasSize.Height)
            {
                canvas.Top = offsetY + (currentClientSize.Height - canvasSize.Height) / 2;
            }
            else
            {
                canvas.Top = offsetY + 0;
            }
        }

        /// <summary>
        /// Compute a size as a sum of surface size and chrome.
        /// Upper bound is working area of the screen. Lower bound is fixed value.
        /// </summary>
        private Size GetOptimalWindowSize()
        {
            var surfaceSize = (Surface as Control).Size;
            var chromeSize = GetChromeSize();
            var newWidth = chromeSize.Width + surfaceSize.Width;
            var newHeight = chromeSize.Height + surfaceSize.Height;

            // Upper bound. Don't make it bigger than the available working area.
            var maxWindowSize = GetAvailableScreenSpace();
            newWidth = Math.Min(newWidth, maxWindowSize.Width);
            newHeight = Math.Min(newHeight, maxWindowSize.Height);

            // Lower bound. Don't make it smaller than a fixed value.
            int minimumFormWidth = 650;
            int minimumFormHeight = 530;
            newWidth = Math.Max(minimumFormWidth, newWidth);
            newHeight = Math.Max(minimumFormHeight, newHeight);

            return new Size(newWidth, newHeight);
        }

        private Size GetChromeSize()
            => Size - panel1.ClientSize;

        /// <summary>
        /// Compute a size that the form can take without getting out of working area of the screen.
        /// </summary>
        private Size GetAvailableScreenSpace()
        {
            var screen = Screen.FromControl(this);
            var screenBounds = screen.Bounds;
            var workingArea = screen.WorkingArea;
            if (Left > screenBounds.Left && Top > screenBounds.Top)
            {
                return new Size(workingArea.Right - Left, workingArea.Bottom - Top);
            }
            else
            {
                return workingArea.Size;
            }
        }

        private void ZoomInMenuItemClick(object sender, EventArgs e)
        {
            var zoomValue = Surface.ZoomFactor;
            var nextIndex = Array.FindIndex(ZOOM_VALUES, v => v > zoomValue);
            var nextValue = nextIndex < 0 ? ZOOM_VALUES[ZOOM_VALUES.Length - 1] : ZOOM_VALUES[nextIndex];

            ZoomSetValue(nextValue);
        }

        private void ZoomOutMenuItemClick(object sender, EventArgs e)
        {
            var zoomValue = Surface.ZoomFactor;
            var nextIndex = Array.FindLastIndex(ZOOM_VALUES, v => v < zoomValue);
            var nextValue = nextIndex < 0 ? ZOOM_VALUES[0] : ZOOM_VALUES[nextIndex];

            ZoomSetValue(nextValue);
        }

        private void ZoomSetValueMenuItemClick(object sender, EventArgs e)
        {
            var senderMenuItem = (ToolStripMenuItem)sender;
            var nextValue = Fraction.Parse((string)senderMenuItem.Tag);

            ZoomSetValue(nextValue);
        }

        private void ZoomBestFitMenuItemClick(object sender, EventArgs e)
        {
            var maxWindowSize = GetAvailableScreenSpace();
            var chromeSize = GetChromeSize();
            var maxImageSize = maxWindowSize - chromeSize;
            var imageSize = Surface.Image.Size;

            static bool isFit(Fraction scale, int source, int boundary)
                => (int)(source * scale) <= boundary;

            var nextIndex = Array.FindLastIndex(
                ZOOM_VALUES,
                zoom => isFit(zoom, imageSize.Width, maxImageSize.Width)
                        && isFit(zoom, imageSize.Height, maxImageSize.Height)
            );
            var nextValue = nextIndex < 0 ? ZOOM_VALUES[0] : ZOOM_VALUES[nextIndex];

            ZoomSetValue(nextValue);
        }

        private void ZoomSetValue(Fraction value)
        {
            var surface = Surface as Surface;
            if (surface?.Parent is not Panel panel)
            {
                return;
            }

            if (value == Surface.ZoomFactor)
            {
                return;
            }

            // Store scroll position
            var rc = surface.GetVisibleRectangle(); // use visible rc by default
            var size = surface.Size;
            if (value > Surface.ZoomFactor) // being smart on zoom-in
            {
                var selection = surface.GetSelectionRectangle().Intersect(rc);
                if (selection != NativeRect.Empty)
                {
                    rc = selection; // zoom to visible part of selection
                }
                else
                {
                    // if image fits completely to currently visible rc and there are no things to focus on
                    // - prefer top left corner to zoom-in as less disorienting for screenshots
                    if (size.Width < rc.Width)
                    {
                        rc = rc.ChangeWidth(0);
                    }

                    if (size.Height < rc.Height)
                    {
                        rc = rc.ChangeHeight(0);
                    }
                }
            }

            var horizontalCenter = 1.0 * (rc.Left + rc.Width / 2) / size.Width;
            var verticalCenter = 1.0 * (rc.Top + rc.Height / 2) / size.Height;

            // Set the new zoom value
            Surface.ZoomFactor = value;
            Size = GetOptimalWindowSize();
            AlignCanvasPositionAfterResize();

            // Update zoom controls
            zoomStatusDropDownBtn.Text = ((int)(100 * (double)value)).ToString() + "%";
            var valueString = value.ToString();
            foreach (var item in zoomMenuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.Checked = menuItem.Tag as string == valueString;
                }
            }

            // Restore scroll position
            rc = surface.GetVisibleRectangle();
            size = surface.Size;
            panel.AutoScrollPosition = new Point(
                (int)(horizontalCenter * size.Width) - rc.Width / 2,
                (int)(verticalCenter * size.Height) - rc.Height / 2
            );
        }

        /// <summary>
        ///   Attempts to save the current surface state to the specified file path.
        /// </summary>
        /// <param name="filePath">The path to the file where the surface state will be saved. Must be a valid file path.</param>
        /// <returns>true if the surface state was saved successfully; otherwise, false.</returns>
        public bool TrySaveState(string filePath)
        {
            // Check if we even have a state
            if (!_surface.Modified)
            {
                Close();
                return false;
            }
            try
            {
                ImageIO.Save(_surface, filePath, true, new SurfaceOutputSettings(WellKnownFileFormats.Greenshot), false);
                // Make sure the user isn't asked to save
                _surface.Modified = false;
                Close();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving surface state to {filePath}", ex);
            }
            return false;
        }

        protected override void WndProc(ref Message m)
        {
            if (!WndProcDefaults.TryHandleMessage(ref m))
            {
                base.WndProc(ref m);
            }
        }
        /// <summary>
        /// The language is already applied in the constructor, applying it again when the form loads would cost time
        /// </summary>
        protected override bool InitializeLanguageOnLoad => false;

        protected override void InitializeLanguage()
        {
            // Every changed text or image size would lay out its tool strip again, do that only once at the end
            var suspendedControls = new List<Control> { this };
            foreach (var toolStrip in new ToolStrip[] { menuStrip1, toolsToolStrip, destinationsToolStrip, propertiesToolStrip, statusStrip1 })
            {
                CollectDropDowns(toolStrip, suspendedControls);
            }

            foreach (var control in suspendedControls)
            {
                control.SuspendLayout();
            }

            try
            {
                ApplyLanguage();
            }
            finally
            {
                for (int i = suspendedControls.Count - 1; i >= 0; i--)
                {
                    suspendedControls[i].ResumeLayout(true);
                }
            }
        }

        /// <summary>
        /// Add the tool strip and all drop-downs (which are tool strips too) of its items, which already have items
        /// </summary>
        private static void CollectDropDowns(ToolStrip toolStrip, List<Control> toolStrips)
        {
            if (toolStrip == null)
            {
                return;
            }

            toolStrips.Add(toolStrip);
            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is ToolStripDropDownItem { HasDropDownItems: true } dropDownItem)
                {
                    CollectDropDowns(dropDownItem.DropDown, toolStrips);
                }
            }
        }

        private void ApplyLanguage()
        {
            this.toolsToolStrip.ImageScalingSize = coreConfiguration.IconSize;
            this.menuStrip1.ImageScalingSize = coreConfiguration.IconSize;
            this.destinationsToolStrip.ImageScalingSize = coreConfiguration.IconSize;
            this.propertiesToolStrip.ImageScalingSize = coreConfiguration.IconSize;
            this.propertiesToolStrip.MinimumSize = new System.Drawing.Size(150, coreConfiguration.IconSize.Height + 10);

            this.btnCursor.Text = Texts.Editor.Cursortool;
            this.btnRect.Text = Texts.Editor.Drawrectangle;
            this.btnEllipse.Text = Texts.Editor.Drawellipse;
            this.btnLine.Text = Texts.Editor.Drawline;
            this.btnArrow.Text = Texts.Editor.Drawarrow;
            this.btnFreehand.Text = Texts.Editor.Drawfreehand;
            this.btnText.Text = Texts.Editor.Drawtextbox;
            this.btnSpeechBubble.Text = Texts.Editor.Speechbubble;
            this.btnStepLabel.Text = Texts.Editor.Counter;
            this.btnEmoji.Text = "Emoji (M)";
            this.btnHighlight.Text = Texts.Editor.Drawhighlighter;
            this.btnObfuscate.Text = Texts.Editor.Obfuscate;
            this.toolStripSplitButton1.Text = Texts.Editor.Effects;
            this.addBorderToolStripMenuItem.Text = Texts.Editor.Border;
            this.addDropshadowToolStripMenuItem.Text = Texts.Editor.ImageShadow;
            this.tornEdgesToolStripMenuItem.Text = Texts.Editor.TornEdge;
            this.grayscaleToolStripMenuItem.Text = Texts.Editor.Grayscale;
            this.invertToolStripMenuItem.Text = Texts.Editor.Invert;
            this.removeTransparencyToolStripMenuItem.Text = Texts.Editor.RemoveTransparency;
            this.btnResize.Text = Texts.Editor.Resize;
            this.btnCrop.Text = Texts.Editor.Crop;
            this.rotateCwToolstripButton.Text = Texts.Editor.Rotatecw;
            this.rotateCcwToolstripButton.Text = Texts.Editor.Rotateccw;
            this.fileStripMenuItem.Text = Texts.Editor.File;
            this.editToolStripMenuItem.Text = Texts.Editor.Edit;
            this.cutToolStripMenuItem.Text = Texts.Editor.Cuttoclipboard;
            this.copyToolStripMenuItem.Text = Texts.Editor.Copytoclipboard;
            this.pasteToolStripMenuItem.Text = Texts.Editor.Pastefromclipboard;
            this.duplicateToolStripMenuItem.Text = Texts.Editor.Duplicate;
            this.preferencesToolStripMenuItem.Text = Texts.Core.ContextmenuSettings;
            this.insert_window_toolstripmenuitem.Text = Texts.Editor.Insertwindow;
            this.obfuscateTextToolStripMenuItem.Text = Texts.Editor.ObfuscateText;
            this.objectToolStripMenuItem.Text = Texts.Editor.Object;
            this.addRectangleToolStripMenuItem.Text = Texts.Editor.Drawrectangle;
            this.addEllipseToolStripMenuItem.Text = Texts.Editor.Drawellipse;
            this.drawLineToolStripMenuItem.Text = Texts.Editor.Drawline;
            this.drawArrowToolStripMenuItem.Text = Texts.Editor.Drawarrow;
            this.drawFreehandToolStripMenuItem.Text = Texts.Editor.Drawfreehand;
            this.addTextBoxToolStripMenuItem.Text = Texts.Editor.Drawtextbox;
            this.addSpeechBubbleToolStripMenuItem.Text = Texts.Editor.Speechbubble;
            this.addCounterToolStripMenuItem.Text = Texts.Editor.Counter;
            this.selectAllToolStripMenuItem.Text = Texts.Editor.Selectall;
            this.removeObjectToolStripMenuItem.Text = Texts.Editor.Deleteelement;
            this.arrangeToolStripMenuItem.Text = Texts.Editor.Arrange;
            this.upToTopToolStripMenuItem.Text = Texts.Editor.Uptotop;
            this.upOneLevelToolStripMenuItem.Text = Texts.Editor.Uponelevel;
            this.downOneLevelToolStripMenuItem.Text = Texts.Editor.Downonelevel;
            this.downToBottomToolStripMenuItem.Text = Texts.Editor.Downtobottom;
            this.saveElementsToolStripMenuItem.Text = Texts.Editor.SaveObjects;
            this.loadElementsToolStripMenuItem.Text = Texts.Editor.LoadObjects;
            this.recipesToolStripMenuItem.Text = Texts.Core.ContextmenuRecipes ?? "Recipes";
            this.pluginToolStripMenuItem.Text = Texts.Settings.Plugins;
            this.helpToolStripMenuItem.Text = Texts.Core.ContextmenuHelp;
            this.helpToolStripMenuItem1.Text = Texts.Core.ContextmenuHelp;
            this.aboutToolStripMenuItem.Text = Texts.Core.ContextmenuAbout;
            this.btnSave.Text = Texts.Editor.Save;
            this.btnClipboard.Text = Texts.Editor.Copyimagetoclipboard;
            this.btnPrint.Text = Texts.Editor.Print;
            this.btnDelete.Text = Texts.Editor.Deleteelement;
            this.btnCut.Text = Texts.Editor.Cuttoclipboard;
            this.btnCopy.Text = Texts.Editor.Copytoclipboard;
            this.btnPaste.Text = Texts.Editor.Pastefromclipboard;
            this.btnSettings.Text = Texts.Core.ContextmenuSettings;
            this.btnHelp.Text = Texts.Core.ContextmenuHelp;
            this.obfuscateModeButton.Text = Texts.Editor.ObfuscateMode;
            this.pixelizeToolStripMenuItem.Text = Texts.Editor.ObfuscatePixelize;
            this.blurToolStripMenuItem.Text = Texts.Editor.ObfuscateBlur;
            this.cropModeButton.Text = Texts.Editor.CropMode;
            this.defaultCropModeToolStripMenuItem.Text = Texts.Editor.CropmodeDefault;
            this.verticalCropModeToolStripMenuItem.Text = Texts.Editor.CropmodeVertical;
            this.horizontalCropModeToolStripMenuItem.Text = Texts.Editor.CropmodeHorizontal;
            this.autoCropModeToolStripMenuItem.Text = Texts.Editor.CropmodeAuto;
            this.highlightModeButton.Text = Texts.Editor.HighlightMode;
            this.textHighlightMenuItem.Text = Texts.Editor.HighlightText;
            this.areaHighlightMenuItem.Text = Texts.Editor.HighlightArea;
            this.grayscaleHighlightMenuItem.Text = Texts.Editor.HighlightGrayscale;
            this.magnifyMenuItem.Text = Texts.Editor.HighlightMagnify;
            this.btnFillColor.Text = Texts.Editor.Backcolor;
            this.btnLineColor.Text = Texts.Editor.Forecolor;
            this.counterLabel.Text = Texts.Editor.CounterStartvalue;
            this.lineThicknessLabel.Text = Texts.Editor.Thickness;
            this.fontSizeLabel.Text = Texts.Editor.Fontsize;
            this.fontBoldButton.Text = Texts.Editor.Bold;
            this.fontItalicButton.Text = Texts.Editor.Italic;
            this.textVerticalAlignmentButton.Text = Texts.Editor.AlignVertical;
            this.alignTopToolStripMenuItem.Text = Texts.Editor.AlignTop;
            this.alignMiddleToolStripMenuItem.Text = Texts.Editor.AlignMiddle;
            this.alignBottomToolStripMenuItem.Text = Texts.Editor.AlignBottom;
            this.blurRadiusLabel.Text = Texts.Editor.BlurRadius;
            this.brightnessLabel.Text = Texts.Editor.Brightness;
            this.previewQualityLabel.Text = Texts.Editor.PreviewQuality;
            this.magnificationFactorLabel.Text = Texts.Editor.MagnificationFactor;
            this.pixelSizeLabel.Text = Texts.Editor.PixelSize;
            this.arrowHeadsLabel.Text = Texts.Editor.Arrowheads;
            this.arrowHeadsDropDownButton.Text = Texts.Editor.Arrowheads;
            this.arrowHeadStartMenuItem.Text = Texts.Editor.ArrowheadsStart;
            this.arrowHeadEndMenuItem.Text = Texts.Editor.ArrowheadsEnd;
            this.arrowHeadBothMenuItem.Text = Texts.Editor.ArrowheadsBoth;
            this.arrowHeadNoneMenuItem.Text = Texts.Editor.ArrowheadsNone;
            this.shadowButton.Text = Texts.Editor.Shadow;
            this.btnConfirm.Text = Texts.Editor.Confirm;
            this.btnCancel.Text = Texts.Editor.Cancel;
            this.closeAllToolStripMenuItem.Text = Texts.Editor.CloseAll;
            this.closeToolStripMenuItem.Text = Texts.Editor.Close;
            this.copyPathMenuItem.Text = Texts.Editor.Copypathtoclipboard;
            this.openDirectoryMenuItem.Text = Texts.Editor.Opendirinexplorer;
            this.textHorizontalAlignmentButton.Text = Texts.Editor.AlignHorizontal;
            this.alignLeftToolStripMenuItem.Text = Texts.Editor.AlignLeft;
            this.alignCenterToolStripMenuItem.Text = Texts.Editor.AlignCenter;
            this.alignRightToolStripMenuItem.Text = Texts.Editor.AlignRight;
            this.Text = _surface?.CaptureDetails?.Title != null
                ? _surface.CaptureDetails.Title + " - " + Texts.Editor.Title
                : Texts.Editor.Title;
        }

        /// <summary>
        /// Populates the 'Recipes' top-level menu with all registered EditorTrigger entries.
        /// </summary>
        private void UpdateRecipesMenu()
        {
            if (IsDisposed || Disposing || recipesToolStripMenuItem == null) return;

            recipesToolStripMenuItem.DropDownItems.Clear();

            var triggerManager = SimpleServiceProvider.Current.GetInstance<ITriggerManager>(isOptional: true);
            var recipeManager = SimpleServiceProvider.Current.GetInstance<IRecipeManager>(isOptional: true);

            if (triggerManager == null || recipeManager == null)
            {
                recipesToolStripMenuItem.Visible = false;
                return;
            }

            int count = 0;
            var editorTriggers = triggerManager.GetEditorTriggers();
            if (editorTriggers != null)
            {
                foreach (var trigger in editorTriggers)
                {
                    var recipe = recipeManager.GetRecipeById(trigger.TargetRecipeId);
                    if (recipe == null || !recipe.IsEnabled) continue;

                    string menuText = !string.IsNullOrWhiteSpace(trigger.MenuItemText)
                        ? trigger.MenuItemText
                        : (!string.IsNullOrWhiteSpace(trigger.Name) ? trigger.Name : recipe.Name);

                    var item = new ToolStripMenuItem(menuText);
                    item.Click += (s, ev) =>
                    {
                        trigger.Fire(this);
                    };

                    recipesToolStripMenuItem.DropDownItems.Add(item);
                    count++;
                }
            }

            var editorService = SimpleServiceProvider.Current.GetInstance<IRecipeEditorService>(isOptional: true);
            if (editorService != null)
            {
                if (count > 0)
                {
                    recipesToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
                }

                var managerItem = new ToolStripMenuItem(Texts.Core.ContextmenuManagerecipes ?? "Recipe Manager...");
                managerItem.Click += (s, ev) =>
                {
                    editorService.OpenRecipeManager();
                };
                recipesToolStripMenuItem.DropDownItems.Add(managerItem);

                var editorItem = new ToolStripMenuItem(Texts.Core.ContextmenuRecipeeditor ?? "Recipe Editor...");
                editorItem.Click += (s, ev) =>
                {
                    editorService.OpenEditor();
                };
                recipesToolStripMenuItem.DropDownItems.Add(editorItem);

                count += 2;
            }

            recipesToolStripMenuItem.Visible = count > 0;
        }
    }
}
