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
using System.ComponentModel;
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
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Effects;
using Greenshot.Base.Help;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Controls;
using Greenshot.Editor.Destinations;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Fields.Binding;
using Greenshot.Editor.Helpers;
using Greenshot.Editor.Tabs;
using Greenshot.Base.Threading;
using Greenshot.Editor.Views;
using log4net;
using System.Threading.Tasks;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Languages;

namespace Greenshot.Editor.Forms
{
    /// <summary>
    /// The ImageEditorForm is the editor for Greenshot
    /// </summary>
    public partial class ImageEditorForm : EditorForm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImageEditorForm));
        private static readonly IEditorConfiguration EditorConfiguration = IniConfigHelper.EnsureSection<IEditorConfiguration>(() => new EditorConfigurationImpl());
        private static readonly ICoreConfiguration CoreConfiguration = IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());

        private static readonly List<string> IgnoreDestinations = new()
        {
            nameof(WellKnownDestinations.Picker),
            EditorDestination.DESIGNATION
        };

        // The open editor windows, the tabs are changed under the same lock: Editors can be read from other threads
        private static readonly List<ImageEditorForm> OpenWindowList = new();
        private static readonly object OpenWindowsLock = new();
        private static ImageEditorForm _lastActiveWindow;

        // The surface of the selected tab
        private Surface _surface;
        private List<BidirectionalBinding> _fieldBindings = new();
        private ToolStripButton[] _toolbarButtons;
        private bool _showWithoutActivation;

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

        /// <summary>
        /// The captures in the open editor windows (every tab is an editor), sorted by title
        /// </summary>
        public static List<IImageEditor> Editors
        {
            get
            {
                lock (OpenWindowsLock)
                {
                    return OpenWindowList.SelectMany(window => window.tabStrip.Tabs)
                        .OrderBy(tab => tab.CaptureDetails?.Title, StringComparer.Ordinal)
                        .Cast<IImageEditor>()
                        .ToList();
                }
            }
        }

        /// <summary>
        /// The open editor windows
        /// </summary>
        internal static IReadOnlyList<ImageEditorForm> OpenWindows
        {
            get
            {
                lock (OpenWindowsLock)
                {
                    return OpenWindowList.ToList();
                }
            }
        }

        /// <summary>
        /// Show the capture in an editor: as a new tab of the editor window which was active last when an open editor
        /// should be used, else in a new editor window.
        /// </summary>
        /// <param name="surface">The capture, the editor takes it over</param>
        /// <param name="useOpenEditor">true to add a tab to an open editor window, when there is one; null to use the setting</param>
        /// <param name="matchSizeToCapture">null to use the setting</param>
        /// <returns>The tab of the capture</returns>
        public static EditorTab Open(ISurface surface, bool? useOpenEditor = null, bool? matchSizeToCapture = null)
        {
            ThreadAssert.IsUi(nameof(Open));
            // Without tabs every capture gets its own window
            bool addTab = (useOpenEditor ?? EditorConfiguration.OpenCaptureAsTab) && EditorConfiguration.EditorTabs;
            var window = addTab ? _lastActiveWindow ?? OpenWindows.LastOrDefault() : null;
            // A window which is closing (its question or its saves are running) doesn't get new captures
            if (window == null || window.IsDisposed || window._closing)
            {
                window = new ImageEditorForm(surface, !surface.Modified, matchSizeToCapture);
                window.Show();
                window.Activate();
                return window.SelectedTab;
            }

            var tab = window.AddTab(surface, !surface.Modified, fitWindow: true);
            if (window.WindowState == FormWindowState.Minimized)
            {
                window.WindowState = FormWindowState.Normal;
            }

            window.BringToFront();
            window.Activate();
            return tab;
        }

        /// <summary>
        /// The tabs of this window, from left to right
        /// </summary>
        internal IReadOnlyList<EditorTab> Tabs => tabStrip.Tabs;

        /// <summary>
        /// The tab which is shown
        /// </summary>
        internal EditorTab SelectedTab => tabStrip.SelectedTab;

        internal EditorTabStrip TabStrip => tabStrip;

        /// <summary>
        /// The question about the changed captures is shown or their saves run, the window closes afterwards
        /// </summary>
        internal bool IsClosing => _closing;

        /// <summary>
        /// A window for a tab which is dragged out of another window is shown without taking the focus (which would end the drag)
        /// </summary>
        protected override bool ShowWithoutActivation => _showWithoutActivation;

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
            foreach (var tab in Tabs)
            {
                tab.Surface.AdjustToDpi(newDpi);
            }

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
        if (_fitToCaptureAfterDpiChange && WindowState == FormWindowState.Normal)
        {
            // Toolbar heights don't scale exactly with the DPI, fit the window to the capture once more
            PerformLayout();
            Size = GetOptimalWindowSize();
        }

        _fitToCaptureAfterDpiChange = false;
        AlignCanvasPositionAfterResize();
    }));
}
        }

        private bool? _matchSizeToCapture;
        // The user answered the question about the changed captures, the window closes without asking again
        private bool _closeConfirmed;
        // The question about the changed captures is shown, or their saves run
        private bool _closing;
        private bool MatchSizeToCapture => _matchSizeToCapture ?? EditorConfiguration.MatchSizeToCapture;

        /// <summary>
        /// Open a new image (the size and the color are in the settings) like a capture: as a tab or in its own window
        /// </summary>
        public static EditorTab OpenNewImage() => Open(CreateNewImage());

        /// <summary>
        /// A new image, with the size and the color from the settings
        /// </summary>
        private static Surface CreateNewImage()
        {
            var size = EditorConfiguration.DefaultEditorSize;
            var image = ImageHelper.CreateEmpty(Math.Max(1, size.Width), Math.Max(1, size.Height), PixelFormat.Format32bppArgb, EditorConfiguration.NewImageColor, 96f, 96f);
            var surface = new Surface(image);
            surface.CaptureDetails.Title = Texts.Editor.NewImage;
            return surface;
        }

        public ImageEditorForm(ISurface surface, bool outputMade, bool? matchSizeToCapture = null)
        {
            _matchSizeToCapture = matchSizeToCapture;
            Initialize(useStoredPlacement: true);
            AddTab(surface, outputMade, fitWindow: true);
            // Workaround: As the cursor is (mostly) selected on the surface a funny artifact is visible, this fixes it.
            HideToolstripItems();
        }

        /// <summary>
        /// A window for a tab which was dragged out of another window, it is shown at the bounds without taking the focus
        /// </summary>
        private ImageEditorForm(EditorTab tab, Rectangle bounds)
        {
            _showWithoutActivation = true;
            Initialize(useStoredPlacement: false);
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            InsertTab(tab, 0);
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
            btnCropDefault.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnCrop.Image");
            btnCropVertical.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "CropVertical.Image");
            btnCropHorizontal.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "CropHorizontal.Image");
            btnCropAuto.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "AutoCrop.Image");
            foreach (ToolStripItem cutMarkItem in cutMarkStyleButton.DropDownItems)
            {
                cutMarkItem.Image = CreateCutMarkPreview((CutMarkStyle)cutMarkItem.Tag);
                // The pictures fill the whole item, this keeps them apart
                cutMarkItem.Padding = new Padding(0, 3, 0, 3);
            }
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
            btnApplyToImage.Image = EmbeddedResources.GetImage(typeof(ImageEditorForm), "btnConfirm.Image");
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

        private void Initialize(bool useStoredPlacement)
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

            tabStrip.TabSelected += (s, e) => SelectTab(e.Tab);
            tabStrip.TabCloseRequested += (s, e) => CloseTabAsync(e.Tab).FireAndLog("Close a tab of the editor", Log);
            // Nothing to save before something is drawn on the new image
            tabStrip.NewTabRequested += (s, e) => AddTab(CreateNewImage(), outputMade: true, fitWindow: false);
            Activated += (s, e) => _lastActiveWindow = this;
            FormClosed += (s, e) => CloseAllTabs();
            // Also when the window is disposed without closing it
            Disposed += (s, e) => RemoveFromOpenWindows();

            timing.Mark("Events");
            if (useStoredPlacement)
            {
                // Make sure the editor is placed on the same location as the last editor was on close
                // But only if this still exists, else it will be reset (BUG-1812)
                WindowPlacement editorWindowPlacement = EditorConfigurationHelper.GetEditorPlacement(EditorConfiguration);
                NativeRect screenBounds = DisplayInfo.ScreenBounds;
                if (!screenBounds.Contains(editorWindowPlacement.NormalPosition))
                {
                    EditorConfigurationHelper.ResetEditorPlacement(EditorConfiguration);
                }

                timing.Mark("ScreenBounds");
                ApplyStoredPlacement();
                timing.Mark("Placement");
            }

            UpdateUi();
            timing.Mark("UpdateUi");

            // Workaround: for the MouseWheel event which doesn't get to the panel
            MouseWheel += PanelMouseWheel;

            lock (OpenWindowsLock)
            {
                OpenWindowList.Add(this);
            }

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
            InteropWindowFactory.CreateFor(Handle).SetPlacement(placement);
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
        /// Add a capture as a new tab and show it
        /// </summary>
        /// <param name="surface">The capture, the tab takes it over</param>
        /// <param name="outputMade">true when the capture was already exported, closing it doesn't ask to save it then</param>
        /// <param name="fitWindow">true to fit the window to the capture, when the setting asks for it</param>
        internal EditorTab AddTab(ISurface surface, bool outputMade, bool fitWindow)
        {
            var editorSurface = (Surface)surface;
            editorSurface.Modified = !outputMade;
            editorSurface.TransparencyBackgroundBrush = new TextureBrush(GreenshotResources.GetImage("Checkerboard.Image"), WrapMode.Tile);
            var tab = new EditorTab(editorSurface);
            InsertTab(tab, Tabs.Count);
            if (fitWindow && MatchSizeToCapture)
            {
                Size = GetOptimalWindowSize();
                AlignCanvasPositionAfterResize();
            }

            // Use best fit, for those capture modes where we can get huge images
            if (editorSurface.CaptureDetails.CaptureMode is CaptureMode.File or CaptureMode.Clipboard)
            {
                ZoomBestFitMenuItemClick(this, EventArgs.Empty);
            }

            return tab;
        }

        /// <summary>
        /// Insert a tab, a new one or one from another window, and show it
        /// </summary>
        private void InsertTab(EditorTab tab, int index)
        {
            lock (OpenWindowsLock)
            {
                tabStrip.Insert(index, tab);
            }

            tab.Window = this;
            tab.Changed += TabChanged;
            UpdateTabStripVisibility();
            SelectTab(tab);
        }

        /// <summary>
        /// Without tabs in the settings the tabs are hidden, unless the window has more than one (e.g. the setting was just changed)
        /// </summary>
        private void UpdateTabStripVisibility()
        {
            tabStrip.Visible = EditorConfiguration.EditorTabs || Tabs.Count > 1;
        }

        /// <summary>
        /// Take the tab out of this window, its capture stays open. The tab to the right (or left) of it is shown instead.
        /// </summary>
        private void RemoveTab(EditorTab tab)
        {
            int index = tabStrip.IndexOf(tab);
            if (tab == SelectedTab)
            {
                HideSelectedTab();
            }

            tab.Changed -= TabChanged;
            tab.Window = null;
            lock (OpenWindowsLock)
            {
                tabStrip.Remove(tab);
            }

            UpdateTabStripVisibility();
            if (Tabs.Count > 0 && SelectedTab == null)
            {
                SelectTab(Tabs[Math.Min(index, Tabs.Count - 1)]);
            }
        }

        /// <summary>
        /// Move the tab into another editor window, at the index. This window closes when it was its last tab.
        /// </summary>
        internal void MoveTab(EditorTab tab, ImageEditorForm target, int index)
        {
            RemoveTab(tab);
            if (tab.Surface.CurrentDpi != target.DeviceDpi && DeviceDpi != target.DeviceDpi)
            {
                tab.Surface.AdjustToDpi(target.DeviceDpi);
            }

            target.InsertTab(tab, index);
            if (Tabs.Count == 0)
            {
                Close();
            }
        }

        /// <summary>
        /// Move the tab into a new editor window at the location, with the size of this window
        /// </summary>
        internal ImageEditorForm DetachTab(EditorTab tab, Point location)
        {
            var size = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;
            RemoveTab(tab);
            var window = new ImageEditorForm(tab, new Rectangle(location, size));
            window.Show();
            // Activating the window later shows it in front of the others
            window._showWithoutActivation = false;
            return window;
        }

        /// <summary>
        /// Close the tab and its capture, without asking. The window closes with its last tab.
        /// </summary>
        /// <param name="tab">EditorTab of this window</param>
        internal void CloseTab(EditorTab tab)
        {
            RemoveTab(tab);
            tab.Dispose();
            if (Tabs.Count == 0)
            {
                Close();
            }
        }

        /// <summary>
        /// Close the tab like the user does: a changed capture is saved first when the user wants it, the user can cancel
        /// </summary>
        private async Task CloseTabAsync(EditorTab tab)
        {
            if (_closing || !await ConfirmCloseAsync(new[] { tab }))
            {
                return;
            }

            if (tab.Window == this)
            {
                CloseTab(tab);
            }
        }

        /// <summary>
        /// Close the window like the user does: the changed captures are saved first when the user wants it, the user can cancel.
        /// </summary>
        /// <returns>true when the window is closed</returns>
        public async Task<bool> CloseAsync()
        {
            if (_closing)
            {
                // The question or a save is running already
                return false;
            }

            _closing = true;
            try
            {
                // Not inside the FormClosing event, the window is closed at the end
                await Task.Yield();
                if (IsDisposed)
                {
                    return true;
                }

                if (!await ConfirmCloseAsync(Tabs.ToList()))
                {
                    return false;
                }

                if (!IsDisposed)
                {
                    _closeConfirmed = true;
                    Close();
                }

                return IsDisposed;
            }
            finally
            {
                _closing = false;
            }
        }

        /// <summary>
        /// Close all editor windows like the user does, one after the other
        /// </summary>
        /// <returns>false when the user cancelled, a window stays open</returns>
        public static async Task<bool> CloseAllAsync()
        {
            foreach (var window in OpenWindows)
            {
                if (!window.IsDisposed && !await window.CloseAsync())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Ask what happens with the changed captures of the tabs and save them when the user wants it:
        /// for one capture "Do you want to save the screenshot?", for more one question with the list of them: save all, discard all or cancel.
        /// Saving stops at a capture which isn't saved (e.g. its file dialog was cancelled).
        /// </summary>
        /// <returns>true when the tabs can be closed, false when the user cancelled or a capture wasn't saved</returns>
        private async Task<bool> ConfirmCloseAsync(IReadOnlyList<EditorTab> tabs)
        {
            var changedTabs = tabs.Where(tab => tab.Surface.Modified).ToList();
            if (changedTabs.Count == 0 || EditorConfiguration.SuppressSaveDialogAtClose)
            {
                return true;
            }

            // Show the (first) changed capture
            SelectTab(changedTabs[0]);
            WindowHelper.ToForeground(Handle);
            bool save;
            if (changedTabs.Count == 1)
            {
                DialogResult result = MessageBox.Show(this, Texts.Editor.CloseOnSave, Texts.Editor.CloseOnSaveTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Cancel)
                {
                    return false;
                }

                save = result == DialogResult.Yes;
            }
            else
            {
                const int saveAll = 0, cancel = 2;
                const int maximumListed = 10;
                var titles = changedTabs.Take(maximumListed).Select(tab => "• " + tab.Title);
                if (changedTabs.Count > maximumListed)
                {
                    titles = titles.Concat(new[] { "…" });
                }

                string text = Texts.Editor.CloseOnSaveAll + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, titles);
                int choice = Greenshot.Base.Wpf.ThemedMessageBox.ShowChoice(Handle, Texts.Editor.CloseOnSaveAllTitle, text, System.Windows.MessageBoxImage.Question,
                    new[] { Texts.Editor.SaveAll, Texts.Editor.DiscardAll, Texts.Core.Cancel }, saveAll, cancel);
                if (choice == cancel)
                {
                    return false;
                }

                save = choice == saveAll;
            }

            if (!save)
            {
                return true;
            }

            foreach (var tab in changedTabs)
            {
                if (tab.Window != this || !tab.Surface.Modified)
                {
                    // Closed, moved or saved in the meantime
                    continue;
                }

                SelectTab(tab);
                if (!await SaveAsync(tab.Surface) || tab.Surface.Modified)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Save the capture to its file, or with the file dialog when it has none
        /// </summary>
        /// <returns>true when it was saved</returns>
        private static async Task<bool> SaveAsync(Surface surface)
        {
            var designation = surface.LastSaveFullPath == null ? WellKnownDestinations.FileDialog : WellKnownDestinations.FileNoDialog;
            var destination = DestinationHelper.GetDestination(designation.ToString());
            if (destination == null)
            {
                return false;
            }

            var result = await DestinationExporter.ExportAsync(destination, surface, surface.CaptureDetails, manuallyInitiated: true);
            return result.IsSucceeded;
        }

        /// <summary>
        /// Close the captures of the window
        /// </summary>
        private void CloseAllTabs()
        {
            RemoveFromOpenWindows();
            HideSelectedTab();
            foreach (var tab in Tabs.ToList())
            {
                tab.Changed -= TabChanged;
                tab.Window = null;
                lock (OpenWindowsLock)
                {
                    tabStrip.Remove(tab);
                }

                tab.Dispose();
            }

            if (coreConfiguration.MinimizeWorkingSetSize)
            {
                GC.Collect();
                PsApi.EmptyWorkingSet();
            }
        }

        private void RemoveFromOpenWindows()
        {
            lock (OpenWindowsLock)
            {
                OpenWindowList.Remove(this);
            }

            if (_lastActiveWindow == this)
            {
                _lastActiveWindow = null;
            }
        }

        /// <summary>
        /// Show the tab
        /// </summary>
        internal void SelectTab(EditorTab tab)
        {
            if (tab == SelectedTab && _surface == tab.Surface)
            {
                return;
            }

            HideSelectedTab();
            tabStrip.SelectedTab = tab;
            ShowSelectedTab();
        }

        private void SelectTabAt(int index)
        {
            if (Tabs.Count > 1)
            {
                SelectTab(Tabs[(index + Tabs.Count) % Tabs.Count]);
            }
        }

        /// <summary>
        /// Show the surface of the selected tab, with its tools and status
        /// </summary>
        private void ShowSelectedTab()
        {
            var tab = SelectedTab;
            _surface = tab.Surface;
            panel1.Controls.Add(_surface);
            BindSurface();

            dimensionsLabel.Text = _surface.Image.Width + "x" + _surface.Image.Height;
            UpdateZoomControls(_surface.ZoomFactor);
            AlignCanvasPositionAfterResize();
            panel1.AutoScrollPosition = tab.ScrollPosition;

            ShowDrawingMode(_surface.DrawingMode);
            UpdateDestinationButtons();
            ShowTabState(tab);
            RefreshEditorControls();
            if (ContainsFocus)
            {
                // The keys go to the shown surface, not to a tool bar control which got the focus from the hidden surface
                _surface.Focus();
            }
        }

        /// <summary>
        /// Take the surface of the selected tab out of the window, the tab keeps the scroll position
        /// </summary>
        private void HideSelectedTab()
        {
            var tab = SelectedTab;
            if (tab == null || _surface == null)
            {
                return;
            }

            tab.ScrollPosition = new Point(-panel1.AutoScrollPosition.X, -panel1.AutoScrollPosition.Y);
            UnbindSurface();
            // Removing the surface can move the focus (e.g. a text being edited loses it), the handlers mustn't change the hidden surface
            var surface = _surface;
            _surface = null;
            panel1.Controls.Remove(surface);
            tabStrip.SelectedTab = null;
        }

        private void BindSurface()
        {
            _surface.MovingElementChanged += SurfaceMovingElementChanged;
            _surface.DrawingModeChanged += Surface_DrawingModeChanged;
            _surface.SurfaceSizeChanged += SurfaceSizeChanged;
            _surface.SurfaceExpanded += SurfaceExpanded;
            _surface.ForegroundColorChanged += ForegroundColorChanged;
            _surface.BackgroundColorChanged += BackgroundColorChanged;
            _surface.LineThicknessChanged += LineThicknessChanged;
            _surface.ShadowChanged += ShadowChanged;
            _surface.FieldAggregator.FieldChanged += FieldAggregatorFieldChanged;
            BindFieldControls();
        }

        private void UnbindSurface()
        {
            _surface.MovingElementChanged -= SurfaceMovingElementChanged;
            _surface.DrawingModeChanged -= Surface_DrawingModeChanged;
            _surface.SurfaceSizeChanged -= SurfaceSizeChanged;
            _surface.SurfaceExpanded -= SurfaceExpanded;
            _surface.ForegroundColorChanged -= ForegroundColorChanged;
            _surface.BackgroundColorChanged -= BackgroundColorChanged;
            _surface.LineThicknessChanged -= LineThicknessChanged;
            _surface.ShadowChanged -= ShadowChanged;
            _surface.FieldAggregator.FieldChanged -= FieldAggregatorFieldChanged;
            foreach (var binding in _fieldBindings)
            {
                binding.Dispose();
            }

            _fieldBindings.Clear();
        }

        /// <summary>
        /// Replace the surface of the tab (used to show another capture in the same tab)
        /// </summary>
        internal void ReplaceSurface(EditorTab tab, Surface surface)
        {
            if (tab.Surface.Modified)
            {
                throw new InvalidOperationException("The capture of the tab is modified, it can't be replaced.");
            }

            bool selected = tab == SelectedTab;
            if (selected)
            {
                HideSelectedTab();
            }

            surface.TransparencyBackgroundBrush = new TextureBrush(GreenshotResources.GetImage("Checkerboard.Image"), WrapMode.Tile);
            tab.ReplaceSurface(surface);
            tab.ScrollPosition = Point.Empty;
            if (selected)
            {
                SelectTab(tab);
            }

            Activate();
            WindowHelper.ToForeground(Handle);
        }

        /// <summary>
        /// The title or the status of a tab changed
        /// </summary>
        private void TabChanged(object sender, EventArgs e)
        {
            tabStrip.Invalidate();
            if (sender == SelectedTab)
            {
                ShowTabState(SelectedTab);
            }
        }

        /// <summary>
        /// Show the title and the status message of the tab
        /// </summary>
        private void ShowTabState(EditorTab tab)
        {
            string description = tab.Surface.LastSaveFullPath ?? tab.Surface.CaptureDetails?.Title;
            Text = string.IsNullOrEmpty(description) ? Texts.Editor.Title : description + " - " + Texts.Editor.Title;
            UpdateStatusLabel(tab.StatusText ?? string.Empty, tab.StatusIsFileSaved ? fileSavedStatusContextMenu : null, tab.StatusIsError);
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
                    plugin.InitializeEditor(this, pluginToolStripMenuItem, () => _surface);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error initializing editor plugins", ex);
            }

            pluginToolStripMenuItem.Visible = pluginToolStripMenuItem.DropDownItems.Count > 0;
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

            UpdateDestinationButtons();
        }

        /// <summary>
        /// Show the destination buttons which are available for the capture of the selected tab
        /// </summary>
        private void UpdateDestinationButtons()
        {
            foreach (ToolStripItem item in destinationsToolStrip.Items)
            {
                if (item.Tag is IDestination destination)
                {
                    item.Visible = destination.IsAvailableFor(_surface.CaptureDetails);
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
                    Tag = toolstripDestination,
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
                destinationButton.Tag = toolstripDestination;
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
            if (OpenWindows.Count > 1)
            {
                fileStripMenuItem.DropDownItems.Add(closeAllToolStripMenuItem);
            }
            // Closing the only tab is the same as closing the window
            if (Tabs.Count > 1)
            {
                fileStripMenuItem.DropDownItems.Add(closeTabToolStripMenuItem);
                closeTabToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.W;
            }
            fileStripMenuItem.DropDownItems.Add(closeToolStripMenuItem);
            // reassign the close shortcuts besause ClearItems above removes them
            closeToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
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

            dimensionsLabel.Text = _surface.Image.Width + "x" + _surface.Image.Height;
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

        private void SurfaceMovingElementChanged(object sender, SurfaceElementEventArgs e)
        {
            RefreshEditorControls();
        }

        private void Surface_DrawingModeChanged(object source, SurfaceDrawingModeEventArgs eventArgs)
        {
            ShowDrawingMode(eventArgs.DrawingMode);
            RefreshEditorControls();
        }

        /// <summary>
        /// Check the tool button of the drawing mode
        /// </summary>
        private void ShowDrawingMode(DrawingModes drawingMode)
        {
            switch (drawingMode)
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
        }

        private void BtnSaveClick(object sender, EventArgs e)
        {
            Save(_surface);
        }

        /// <summary>
        /// Save the capture to its file, or ask for the file when it wasn't saved yet (the save runs in the background)
        /// </summary>
        private static void Save(Surface surface)
        {
            var destinationDesignation = surface.LastSaveFullPath == null ? WellKnownDestinations.FileDialog : WellKnownDestinations.FileNoDialog;
            DestinationHelper.StartExport(destinationDesignation, surface);
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

        private void CloseTabToolStripMenuItemClick(object sender, EventArgs e)
        {
            CloseTabAsync(SelectedTab).FireAndLog("Close a tab of the editor", Log);
        }

        /// <summary>
        /// Closes this editor window or all open editor windows
        /// </summary>
        /// <param name="closeAllOpenEditors"></param>
        private void CloseEditor(bool closeAllOpenEditors)
        {
            if (closeAllOpenEditors)
            {
                CloseAllAsync().FireAndLog("Close all editor windows", Log);
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
            var shell = SimpleServiceProvider.Current.GetInstance<IGreenshotShell>();
            shell.ShowAbout();
        }

        private void PreferencesToolStripMenuItemClick(object sender, EventArgs e)
        {
            var shell = SimpleServiceProvider.Current.GetInstance<IGreenshotShell>();
            shell.ShowSetting();
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
            // When the application must end there is no time to ask and to save, at the end of the session the editor state is saved
            // (see RestartManagerHelper), the exit of Greenshot closes the editors with CloseAllAsync first.
            bool mustClose = e.CloseReason is CloseReason.ApplicationExitCall or CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing;
            if (!_closeConfirmed && !mustClose && (_closing || Tabs.Any(tab => tab.Surface.Modified)))
            {
                // The saves run in the background: the window closes when they are done
                e.Cancel = true;
                CloseAsync().FireAndLog("Close the editor with changed captures", Log);
                return;
            }

            // persist our geometry string.
            EditorConfigurationHelper.SetEditorPlacement(EditorConfiguration, InteropWindowFactory.CreateFor(Handle).GetPlacement());
            IniConfigRegistry.Get().Save();
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
                        if (_surface.DrawingMode == DrawingModes.Crop)
                        {
                            CycleCropMode();
                        }
                        else
                        {
                            BtnCropClick(sender, e);
                        }
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
            if (_surface?.KeysLocked != true)
            {
                return base.ProcessKeyPreview(ref msg);
            }

            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keys)
        {
            // Escape cancels the drag of a tab
            if (keys == Keys.Escape && tabStrip.CancelDrag())
            {
                return true;
            }

            // disable default key handling if surface has requested a lock
            if (_surface?.KeysLocked == false)
            {
                if (ProcessTabKey(keys))
                {
                    return true;
                }

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

        /// <summary>
        /// The keys of the tabs: Ctrl+W closes the tab, Ctrl+Tab and Ctrl+Page Down show the next tab, with Shift or Page Up the previous one
        /// </summary>
        /// <returns>true when the key was handled</returns>
        private bool ProcessTabKey(Keys keys)
        {
            int index = tabStrip.IndexOf(SelectedTab);
            switch (keys)
            {
                case Keys.Control | Keys.W:
                    CloseTabAsync(SelectedTab).FireAndLog("Close a tab of the editor", Log);
                    return true;
                case Keys.Control | Keys.Tab:
                case Keys.Control | Keys.PageDown:
                    SelectTabAt(index + 1);
                    return true;
                case Keys.Control | Keys.Shift | Keys.Tab:
                case Keys.Control | Keys.PageUp:
                    SelectTabAt(index - 1);
                    return true;
                default:
                    return false;
            }
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

        /// <summary>
        /// Bind the controls of the properties tool bar to the fields of the surface, the controls show the values of the surface
        /// </summary>
        private void BindFieldControls()
        {
            Bind(btnFillColor, "SelectedColor", _surface.FieldAggregator.GetField(FieldType.FILL_COLOR), "Value", NotNullValidator.GetInstance());
            Bind(btnLineColor, "SelectedColor", _surface.FieldAggregator.GetField(FieldType.LINE_COLOR), "Value", NotNullValidator.GetInstance());
            Bind(lineThicknessUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.LINE_THICKNESS), "Value", DecimalIntConverter.GetInstance(),
                NotNullValidator.GetInstance());
            Bind(toothHeightUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.TOOTH_HEIGHT), "Value", DecimalIntConverter.GetInstance(),
                NotNullValidator.GetInstance());
            Bind(toothRangeUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.TOOTH_RANGE), "Value", DecimalIntConverter.GetInstance(),
                NotNullValidator.GetInstance());
            Bind(blurRadiusUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.BLUR_RADIUS), "Value", DecimalIntConverter.GetInstance(),
                NotNullValidator.GetInstance());
            Bind(magnificationFactorUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.MAGNIFICATION_FACTOR), "Value",
                DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
            Bind(pixelSizeUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.PIXEL_SIZE), "Value", DecimalIntConverter.GetInstance(),
                NotNullValidator.GetInstance());
            Bind(brightnessUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.BRIGHTNESS), "Value", DecimalDoublePercentageConverter.GetInstance(),
                NotNullValidator.GetInstance());
            Bind(fontFamilyComboBox, "Text", _surface.FieldAggregator.GetField(FieldType.FONT_FAMILY), "Value", NotNullValidator.GetInstance());
            Bind(fontSizeUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.FONT_SIZE), "Value", DecimalFloatConverter.GetInstance(),
                NotNullValidator.GetInstance());
            Bind(fontBoldButton, "Checked", _surface.FieldAggregator.GetField(FieldType.FONT_BOLD), "Value", NotNullValidator.GetInstance());
            Bind(fontItalicButton, "Checked", _surface.FieldAggregator.GetField(FieldType.FONT_ITALIC), "Value", NotNullValidator.GetInstance());
            Bind(textHorizontalAlignmentButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.TEXT_HORIZONTAL_ALIGNMENT), "Value",
                NotNullValidator.GetInstance());
            Bind(textVerticalAlignmentButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.TEXT_VERTICAL_ALIGNMENT), "Value",
                NotNullValidator.GetInstance());
            Bind(shadowButton, "Checked", _surface.FieldAggregator.GetField(FieldType.SHADOW), "Value", NotNullValidator.GetInstance());
            Bind(previewQualityUpDown, "Value", _surface.FieldAggregator.GetField(FieldType.PREVIEW_QUALITY), "Value",
                DecimalDoublePercentageConverter.GetInstance(), NotNullValidator.GetInstance());
            Bind(obfuscateModeButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.PREPARED_FILTER_OBFUSCATE), "Value");
            Bind(cutMarkStyleButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.CUT_MARK_STYLE), "Value");
            Bind(highlightModeButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.PREPARED_FILTER_HIGHLIGHT), "Value");
            Bind(arrowHeadsDropDownButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldType.ARROWHEADS), "Value",
                NotNullValidator.GetInstance());
            Bind(counterUpDown, "Value", _surface, "CounterStart", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
        }

        private void Bind(INotifyPropertyChanged control, string controlPropertyName, INotifyPropertyChanged field, string fieldPropertyName,
            IBindingValidator validator = null) =>
            Bind(control, controlPropertyName, field, fieldPropertyName, null, validator);

        private void Bind(INotifyPropertyChanged control, string controlPropertyName, INotifyPropertyChanged field, string fieldPropertyName,
            IBindingConverter converter, IBindingValidator validator)
        {
            var binding = new BidirectionalBinding(control, controlPropertyName, field, fieldPropertyName, converter, validator);
            try
            {
                binding.UpdateControl();
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't show the value of {fieldPropertyName} in {controlPropertyName}", ex);
            }

            _fieldBindings.Add(binding);
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
                toothHeightLabel.Visible = toothHeightUpDown.Visible = props.HasFieldValue(FieldType.TOOTH_HEIGHT);
                toothRangeLabel.Visible = toothRangeUpDown.Visible = props.HasFieldValue(FieldType.TOOTH_RANGE);
                btnApplyToImage.Visible = _surface.SelectedElements?.Any(element => element is CutMarkContainer or TornEdgeContainer) == true;
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
                bool cropping = props.HasFieldValue(FieldType.CROPMODE);
                var cropMode = cropping ? (CropContainer.CropModes)props.GetFieldValue(FieldType.CROPMODE) : CropContainer.CropModes.Default;
                foreach (var cropModeButton in new[] { btnCropDefault, btnCropVertical, btnCropHorizontal, btnCropAuto })
                {
                    cropModeButton.Visible = cropping;
                    cropModeButton.Checked = cropping && Equals(cropModeButton.Tag, cropMode);
                }

                // The cut edges are for crop and crop out, or for selected cut or torn edges
                cutMarkLabel.Visible = cutMarkStyleButton.Visible = props.HasFieldValue(FieldType.CUT_MARK_STYLE);
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
        private string _stepLabelIconName;
        private Image _stepLabelIcon;

        private void RefreshEditorControls()
        {
            if (IsDisposed || Disposing) return;
            int stepLabels = _surface.CountStepLabels(null);
            string stepLabelIconName = stepLabels <= 20 ? $"btnStepLabel{stepLabels:00}.Image" : "btnStepLabel20+.Image";
            // This runs on every selection change, only decode the icon when the number changed
            if (stepLabelIconName != _stepLabelIconName)
            {
                var previousIcon = _stepLabelIcon;
                _stepLabelIcon = EmbeddedResources.GetImage(typeof(ImageEditorForm), stepLabelIconName);
                _stepLabelIconName = stepLabelIconName;
                btnStepLabel.Image = _stepLabelIcon;
                addCounterToolStripMenuItem.Image = _stepLabelIcon;
                previousIcon?.Dispose();
            }

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
            if (_surface != null)
            {
                _surface.KeysLocked = true;
            }
        }

        private void ToolBarFocusableElementLostFocus(object sender, EventArgs e)
        {
            if (_surface != null)
            {
                _surface.KeysLocked = false;
            }
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

        private void CropModeButtonClick(object sender, EventArgs e)
        {
            SelectCropMode((CropContainer.CropModes)((ToolStripItem)sender).Tag);
        }

        /// <summary>
        /// Pressing C again while cropping goes to the next crop mode
        /// </summary>
        private void CycleCropMode()
        {
            var cropMode = (CropContainer.CropModes)_surface.FieldAggregator.GetField(FieldType.CROPMODE).Value;
            SelectCropMode(cropMode switch
            {
                CropContainer.CropModes.Default => CropContainer.CropModes.Vertical,
                CropContainer.CropModes.Vertical => CropContainer.CropModes.Horizontal,
                CropContainer.CropModes.Horizontal => CropContainer.CropModes.AutoCrop,
                _ => CropContainer.CropModes.Default
            });
        }

        /// <summary>
        /// A small picture of the cut mark style for the drop-down: two image parts with their edges and the gap between them
        /// </summary>
        /// <summary>
        /// A picture for a cut edge style: two parts with the edges of the style, the gap between them shows the transparency checker pattern.
        /// There is a transparent border, so the pictures in the drop down don't touch.
        /// </summary>
        private static Bitmap CreateCutMarkPreview(CutMarkStyle cutMarkStyle)
        {
            const int size = 16;
            const int border = 2;
            const int inner = size - 2 * border;
            var preview = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(preview);
            graphics.Clear(Color.Transparent);
            graphics.TranslateTransform(border, border);
            graphics.FillRectangle(Brushes.White, 0, 0, inner, inner);
            for (int y = 0; y < inner; y += 2)
            {
                for (int x = (y / 2) % 2 * 2; x < inner; x += 4)
                {
                    graphics.FillRectangle(Brushes.Silver, x, y, 2, 2);
                }
            }

            if (cutMarkStyle == CutMarkStyle.None)
            {
                graphics.FillRectangle(Brushes.SteelBlue, 0, 0, inner, inner);
                return preview;
            }

            const int toothHeight = 2;
            const int bandTop = 2;
            const int bandHeight = 8;
            var random = new Random(16);
            var firstEdge = CutOutHelper.CreateEdge(cutMarkStyle, inner, toothHeight, 4, random);
            var secondEdge = CutOutHelper.CreateEdge(cutMarkStyle, inner, toothHeight, 4, random);
            var before = new List<PointF> { new PointF(0, 0), new PointF(inner, 0) };
            before.AddRange(Enumerable.Reverse(firstEdge).Select(p => new PointF(p.X, bandTop + toothHeight - p.Y)));
            var after = secondEdge.Select(p => new PointF(p.X, bandTop + bandHeight - toothHeight + p.Y)).ToList();
            after.Add(new PointF(inner, inner));
            after.Add(new PointF(0, inner));
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillPolygon(Brushes.SteelBlue, before.ToArray());
            graphics.FillPolygon(Brushes.SteelBlue, after.ToArray());
            return preview;
        }

        private void SelectCropMode(CropContainer.CropModes cropMode)
        {
            _surface.FieldAggregator.GetField(FieldType.CROPMODE).Value = cropMode;
            InitCropMode(cropMode);

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

        /// <summary>
        /// Draw the selected cut edges or torn edges into the image
        /// </summary>
        private void BtnApplyToImageClick(object sender, EventArgs e)
        {
            foreach (var element in _surface.SelectedElements.Where(element => element is CutMarkContainer or TornEdgeContainer).ToList())
            {
                _surface.ApplyElementToImage(element);
            }

            UpdateUndoRedoSurfaceDependencies();
            RefreshFieldControls();
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            _surface.Confirm(false);
            RefreshEditorControls();
        }

        private readonly CaptureWindowMenuBuilder _captureWindowMenuBuilder = new CaptureWindowMenuBuilder();

        private void Insert_window_toolstripmenuitemMouseEnter(object sender, EventArgs e)
        {
            ToolStripMenuItem captureWindowMenuItem = (ToolStripMenuItem)sender;
            _captureWindowMenuBuilder.Fill(captureWindowMenuItem, Contextmenu_window_Click);
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
            AsyncCommand.Run(() => CaptureWindowIntoEditorAsync((IInteropWindow)clickedItem.Tag), "Capture a window into the editor");
        }

        private async Task CaptureWindowIntoEditorAsync(IInteropWindow windowToCapture)
        {
            try
            {
                ICapture capture = new Capture();
                using (Graphics graphics = Graphics.FromHwnd(Handle))
                {
                    capture.CaptureDetails.DpiX = graphics.DpiY;
                    capture.CaptureDetails.DpiY = graphics.DpiY;
                }

                windowToCapture = WindowCapture.SelectCaptureWindow(windowToCapture);
                if (windowToCapture != null)
                {
                    // Continues on the UI thread (the context is captured), where the surface is changed
                    capture = await WindowCapture.CaptureWindowAsync(windowToCapture, capture).ConfigureAwait(true);
                    if (capture?.CaptureDetails != null && capture.Image != null)
                    {
                        ((Bitmap)capture.Image).SetResolution(capture.CaptureDetails.DpiX, capture.CaptureDetails.DpiY);
                        _surface.AddImageContainer((Bitmap)capture.Image, 100, 100);
                    }

                    Activate();
                    WindowHelper.ToForeground(Handle);
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
            using (Image tmpImage = _surface.GetImageForExport())
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
                // The torn edges are an element, so they can be changed or removed later
                _surface.AddTornEdges(tornEdgeEffect);
                UpdateUndoRedoSurfaceDependencies();
                RefreshFieldControls();
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
            if (_surface?.Image == null || panel1 == null)
            {
                return;
            }

            Control canvas = _surface;
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
            var surfaceSize = _surface.Size;
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
            var zoomValue = _surface.ZoomFactor;
            var nextIndex = Array.FindIndex(ZOOM_VALUES, v => v > zoomValue);
            var nextValue = nextIndex < 0 ? ZOOM_VALUES[ZOOM_VALUES.Length - 1] : ZOOM_VALUES[nextIndex];

            ZoomSetValue(nextValue);
        }

        private void ZoomOutMenuItemClick(object sender, EventArgs e)
        {
            var zoomValue = _surface.ZoomFactor;
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
            var imageSize = _surface.Image.Size;

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
            var surface = _surface;
            if (surface?.Parent is not Panel panel)
            {
                return;
            }

            if (value == surface.ZoomFactor)
            {
                return;
            }

            // Store scroll position
            var rc = surface.GetVisibleRectangle(); // use visible rc by default
            var size = surface.Size;
            if (value > surface.ZoomFactor) // being smart on zoom-in
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
            surface.ZoomFactor = value;
            Size = GetOptimalWindowSize();
            AlignCanvasPositionAfterResize();
            UpdateZoomControls(value);

            // Restore scroll position
            rc = surface.GetVisibleRectangle();
            size = surface.Size;
            panel.AutoScrollPosition = new Point(
                (int)(horizontalCenter * size.Width) - rc.Width / 2,
                (int)(verticalCenter * size.Height) - rc.Height / 2
            );
        }

        /// <summary>
        /// Show the zoom factor in the status bar and the zoom menu
        /// </summary>
        private void UpdateZoomControls(Fraction value)
        {
            zoomStatusDropDownBtn.Text = ((int)(100 * (double)value)).ToString() + "%";
            var valueString = value.ToString();
            foreach (var item in zoomMenuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.Checked = menuItem.Tag as string == valueString;
                }
            }
        }

        /// <summary>
        /// Sent before WM_DPICHANGED, the window can choose its own size for the new DPI
        /// </summary>
        private const int WmGetDpiScaledSize = 0x02E4;

        /// <summary>
        /// Set when the window was fitted to the capture before a DPI change, so it is fitted again afterwards
        /// </summary>
        private bool _fitToCaptureAfterDpiChange;

        /// <summary>
        /// Windows scales the whole window on a DPI change, including the canvas area.
        /// The capture keeps its size in pixels, so this would add empty space around it.
        /// Scale only the toolbars, menus and borders, and keep the canvas area the same size in pixels.
        /// </summary>
        /// <param name="m">Message WM_GETDPISCALEDSIZE, wParam has the new DPI, lParam points to a SIZE to fill</param>
        /// <returns>true if the size was set</returns>
        private bool TryHandleGetDpiScaledSize(ref Message m)
        {
            int oldDpi = DeviceDpi;
            int newDpi = (int)(m.WParam.ToInt64() & 0xFFFF);
            if (WindowState != FormWindowState.Normal || oldDpi <= 0 || newDpi <= 0 || newDpi == oldDpi || m.LParam == IntPtr.Zero)
            {
                return false;
            }

            _fitToCaptureAfterDpiChange = Size == GetOptimalWindowSize();
            var chromeSize = GetChromeSize();
            var canvasAreaSize = panel1.ClientSize;
            int width = (int)Math.Round(chromeSize.Width * (double)newDpi / oldDpi) + canvasAreaSize.Width;
            int height = (int)Math.Round(chromeSize.Height * (double)newDpi / oldDpi) + canvasAreaSize.Height;
            System.Runtime.InteropServices.Marshal.WriteInt32(m.LParam, 0, width);
            System.Runtime.InteropServices.Marshal.WriteInt32(m.LParam, 4, height);
            m.Result = new IntPtr(1);
            return true;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmGetDpiScaledSize && TryHandleGetDpiScaledSize(ref m))
            {
                return;
            }

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
            this.btnCropDefault.Text = Texts.Editor.CropmodeDefault;
            this.btnCropVertical.Text = Texts.Editor.CropmodeVertical;
            this.btnCropHorizontal.Text = Texts.Editor.CropmodeHorizontal;
            this.btnCropAuto.Text = Texts.Editor.CropmodeAuto;
            this.cutMarkLabel.Text = Texts.Editor.CutMark;
            this.cutMarkStyleButton.ToolTipText = Texts.Editor.CutMark;
            this.cutMarkNoneMenuItem.Text = Texts.Editor.CutMarkNone;
            this.cutMarkLineMenuItem.Text = Texts.Editor.CutMarkLine;
            this.cutMarkZigZagMenuItem.Text = Texts.Editor.CutMarkZigzag;
            this.cutMarkWaveMenuItem.Text = Texts.Editor.CutMarkWave;
            this.cutMarkTornMenuItem.Text = Texts.Editor.CutMarkTorn;
            // The button shows the picture of the selected style
            this.cutMarkStyleButton.SelectedTag = this.cutMarkStyleButton.SelectedTag;
            this.highlightModeButton.Text = Texts.Editor.HighlightMode;
            this.textHighlightMenuItem.Text = Texts.Editor.HighlightText;
            this.areaHighlightMenuItem.Text = Texts.Editor.HighlightArea;
            this.grayscaleHighlightMenuItem.Text = Texts.Editor.HighlightGrayscale;
            this.magnifyMenuItem.Text = Texts.Editor.HighlightMagnify;
            this.btnFillColor.Text = Texts.Editor.Backcolor;
            this.btnLineColor.Text = Texts.Editor.Forecolor;
            this.counterLabel.Text = Texts.Editor.CounterStartvalue;
            this.lineThicknessLabel.Text = Texts.Editor.Thickness;
            this.toothHeightLabel.Text = Texts.Editor.TornedgeToothsize;
            this.toothRangeLabel.Text = Texts.Editor.CutMarkToothRange;
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
            this.btnApplyToImage.Text = Texts.Editor.ApplyToImage;
            this.btnCancel.Text = Texts.Core.Cancel;
            this.closeAllToolStripMenuItem.Text = Texts.Editor.CloseAll;
            this.closeToolStripMenuItem.Text = Texts.Editor.Close;
            this.copyPathMenuItem.Text = Texts.Editor.Copypathtoclipboard;
            this.openDirectoryMenuItem.Text = Texts.Editor.Opendirinexplorer;
            this.textHorizontalAlignmentButton.Text = Texts.Editor.AlignHorizontal;
            this.alignLeftToolStripMenuItem.Text = Texts.Editor.AlignLeft;
            this.alignCenterToolStripMenuItem.Text = Texts.Editor.AlignCenter;
            this.alignRightToolStripMenuItem.Text = Texts.Editor.AlignRight;
            this.closeTabToolStripMenuItem.Text = Texts.Editor.CloseTab;
            tabStrip.NewTabText = Texts.Editor.NewImage;
            if (SelectedTab != null)
            {
                ShowTabState(SelectedTab);
            }
            else
            {
                this.Text = Texts.Editor.Title;
            }
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
            var menuGroups = new Dictionary<string, ToolStripMenuItem>(StringComparer.OrdinalIgnoreCase);
            if (editorTriggers != null)
            {
                var menuEntries = editorTriggers
                    .Select(trigger => new
                    {
                        Trigger = trigger,
                        Recipe = recipeManager.GetRecipeById(trigger.TargetRecipeId),
                        Group = string.IsNullOrWhiteSpace(trigger.Group) ? "Recipes" : trigger.Group.Trim()
                    })
                    .Where(entry => entry.Recipe != null && entry.Recipe.IsEnabled)
                    .OrderBy(entry => string.Equals(entry.Group, "Recipes", StringComparison.OrdinalIgnoreCase) ? 0 : 10)
                    .ThenBy(entry => entry.Group, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(entry => entry.Trigger.Order)
                    .ThenBy(entry => entry.Recipe.Name, StringComparer.CurrentCultureIgnoreCase);

                foreach (var entry in menuEntries)
                {
                    var trigger = entry.Trigger;
                    string menuText = !string.IsNullOrWhiteSpace(trigger.MenuItemText)
                        ? trigger.MenuItemText
                        : (!string.IsNullOrWhiteSpace(trigger.Name) ? trigger.Name : entry.Recipe.Name);

                    ToolStripItemCollection parentItems = recipesToolStripMenuItem.DropDownItems;
                    string group = entry.Group;
                    if (!string.Equals(group, "Recipes", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!menuGroups.TryGetValue(group, out var groupItem))
                        {
                            groupItem = new ToolStripMenuItem(group);
                            menuGroups.Add(group, groupItem);
                            recipesToolStripMenuItem.DropDownItems.Add(groupItem);
                        }
                        parentItems = groupItem.DropDownItems;
                    }

                    var item = new ToolStripMenuItem(menuText);
                    item.Click += (s, ev) =>
                    {
                        trigger.Fire(SelectedTab);
                    };

                    parentItems.Add(item);
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
