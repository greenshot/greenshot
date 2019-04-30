// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autofac.Features.OwnedInstances;
using Dapplo.Log;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Greenshot.Addon.LegacyEditor.Controls;
using Greenshot.Addon.LegacyEditor.Drawing;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addon.LegacyEditor.Drawing.Fields.Binding;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Addons.Interfaces.Forms;
using Greenshot.Addons.Resources;
using Greenshot.Gfx;
using Greenshot.Gfx.Effects;

namespace Greenshot.Addon.LegacyEditor.Forms
{
    /// <summary>
    ///     Description of ImageEditorForm.
    /// </summary>
    public partial class ImageEditorForm : GreenshotForm, IImageEditor
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly List<string> IgnoreDestinations = new List<string> { "Picker", "Editor"};
        private static readonly string[] SupportedClipboardFormats = { typeof(string).FullName, "Text", typeof(IDrawableContainerList).FullName };
        private readonly IEditorConfiguration _editorConfiguration;
        private readonly IEditorLanguage _editorLanguage;
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly IGreenshotLanguage _greenshotLanguage;
        private readonly Func<IBitmapWithNativeSupport, Bitmap> _converter = bitmap => bitmap?.NativeBitmap;

        // whether part of the editor controls are disabled depending on selected item(s)
        private bool _controlsDisabledDueToConfirmable;
        private bool _originalBoldCheckState;
        private bool _originalItalicCheckState;
        private Surface _surface;
        private GreenshotToolStripButton[] _toolbarButtons;
        private BitmapScaleHandler<IDestination, IBitmapWithNativeSupport> _destinationScaleHandler;
        private readonly IDisposable _clipboardSubscription;
        private readonly EditorFactory _editorFactory;
        private readonly DestinationHolder _destinationHolder;
        private readonly Func<ResizeEffect, Owned<ResizeSettingsForm>> _resizeSettingsFormFactory;
        private readonly Func<TornEdgeEffect, Owned<TornEdgeSettingsForm>> _tornEdgeSettingsFormFactory;
        private readonly Func<DropShadowEffect, Owned<DropShadowSettingsForm>> _dropShadowSettingsFormFactory;
        private CompositeDisposable _disposables;

        public ImageEditorForm(
            IEditorConfiguration editorConfiguration,
            IEditorLanguage editorLanguage,
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            EditorFactory editorFactory,
            DestinationHolder destinationHolder,
            Func<ResizeEffect, Owned<ResizeSettingsForm>> resizeSettingsFormFactory,
            Func<TornEdgeEffect, Owned<TornEdgeSettingsForm>> tornEdgeSettingsFormFactory,
            Func<DropShadowEffect, Owned<DropShadowSettingsForm>> dropShadowSettingsFormFactory
            ) : base(editorLanguage)
        {
            _editorConfiguration = editorConfiguration;
            _editorLanguage = editorLanguage;
            _coreConfiguration = coreConfiguration;
            _greenshotLanguage = greenshotLanguage;
            _editorFactory = editorFactory;
            _destinationHolder = destinationHolder;
            _resizeSettingsFormFactory = resizeSettingsFormFactory;
            _tornEdgeSettingsFormFactory = tornEdgeSettingsFormFactory;
            _dropShadowSettingsFormFactory = dropShadowSettingsFormFactory;
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            ManualLanguageApply = true;
            InitializeComponent();

            SetupBitmapScaleHandler();

            Load += async (sender, args) =>
            {
                // Make sure the editor is placed on the same location as the last editor was on close
                // But only if this still exists, else it will be reset (BUG-1812)
                var editorWindowPlacement = _editorConfiguration.GetEditorPlacement();
                var screenbounds = DisplayInfo.ScreenBounds;
                if (!screenbounds.Contains(editorWindowPlacement.NormalPosition))
                {
                    _editorConfiguration.ResetEditorPlacement();
                }
                var placement = _editorConfiguration.GetEditorPlacement();
                User32Api.SetWindowPlacement(Handle, ref placement);

                await AddDestinationsAsync();
                await InteropWindowFactory.CreateFor(Handle).ToForegroundAsync();
            };


            UpdateUi();

            // Workaround: As the cursor is (mostly) selected on the surface a funny artifact is visible, this fixes it.
            HideToolstripItems();

            // Make the clipboard buttons update
            _clipboardSubscription = ClipboardNative.OnUpdate.Subscribe(args =>
            {
                UpdateClipboardSurfaceDependencies();
            });
        }

        private void SetupBitmapScaleHandler()
        {
            // Create a BitmapScaleHandler which knows how to locate the icons for the destinations
            _destinationScaleHandler = BitmapScaleHandler.Create<IDestination, IBitmapWithNativeSupport>(FormDpiHandler, (destination, dpi) => destination.GetDisplayIcon(dpi), (bitmap, dpi) => bitmap.ScaleIconForDisplaying(dpi));

            FormDpiHandler.OnDpiChanged.Subscribe(info =>
            {
                // Change the ImageScalingSize before setting the bitmaps
                var size =  DpiHandler.ScaleWithDpi(_coreConfiguration.IconSize, info.NewDpi);
                SuspendLayout();
                toolsToolStrip.ImageScalingSize = size;
                menuStrip1.ImageScalingSize = size;
                destinationsToolStrip.ImageScalingSize = size;
                propertiesToolStrip.ImageScalingSize = size;
                propertiesToolStrip.MinimumSize = new Size(150, size.Width + 10);
                // Redraw the form
                ResumeLayout(true);
                Refresh();
            });

            
            // Use the GreenshotForm ScaleHandler to locate the icons and get them scaled
            ScaleHandler.AddTarget(btnCursor, "btnCursor.Image", _converter);
            ScaleHandler.AddTarget(btnRect, "btnRect.Image", _converter);
            ScaleHandler.AddTarget(btnEllipse, "btnEllipse.Image", _converter);
            ScaleHandler.AddTarget(btnLine, "btnLine.Image", _converter);
            ScaleHandler.AddTarget(btnArrow, "btnArrow.Image", _converter);

            ScaleHandler.AddTarget(btnFreehand, "btnFreehand.Image", _converter);
            ScaleHandler.AddTarget(btnText, "btnText.Image", _converter);
            ScaleHandler.AddTarget(btnSpeechBubble, "addSpeechBubbleToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(btnHighlight, "btnHighlight.Image", _converter);
            ScaleHandler.AddTarget(btnObfuscate, "btnObfuscate.Image", _converter);

            ScaleHandler.AddTarget(toolStripSplitButton1, "toolStripSplitButton1.Image", _converter);
            ScaleHandler.AddTarget(btnResize, "btnResize.Image", _converter);
            ScaleHandler.AddTarget(btnCrop, "btnCrop.Image", _converter);
            ScaleHandler.AddTarget(rotateCwToolstripButton, "rotateCwToolstripButton.Image", _converter);
            ScaleHandler.AddTarget(rotateCcwToolstripButton, "rotateCcwToolstripButton.Image", _converter);
            ScaleHandler.AddTarget(undoToolStripMenuItem, "undoToolStripMenuItem.Image", _converter);

            ScaleHandler.AddTarget(redoToolStripMenuItem, "redoToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(cutToolStripMenuItem, "cutToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(copyToolStripMenuItem, "copyToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(pasteToolStripMenuItem, "pasteToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(preferencesToolStripMenuItem, "preferencesToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(addRectangleToolStripMenuItem, "addRectangleToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(addEllipseToolStripMenuItem, "addEllipseToolStripMenuItem.Image", _converter);

            ScaleHandler.AddTarget(drawLineToolStripMenuItem, "drawLineToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(drawArrowToolStripMenuItem, "drawArrowToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(drawFreehandToolStripMenuItem, "drawFreehandToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(addTextBoxToolStripMenuItem, "addTextBoxToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(addSpeechBubbleToolStripMenuItem, "addSpeechBubbleToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(addCounterToolStripMenuItem, "addCounterToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(removeObjectToolStripMenuItem, "removeObjectToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(helpToolStripMenuItem1, "helpToolStripMenuItem1.Image", _converter);
            ScaleHandler.AddTarget(btnSave, "btnSave.Image", _converter);
            ScaleHandler.AddTarget(btnClipboard, "btnClipboard.Image", _converter);
            ScaleHandler.AddTarget(btnPrint, "btnPrint.Image", _converter);
            ScaleHandler.AddTarget(btnDelete, "btnDelete.Image", _converter);
            ScaleHandler.AddTarget(btnCut, "btnCut.Image", _converter);
            ScaleHandler.AddTarget(btnCopy, "btnCopy.Image", _converter);
            ScaleHandler.AddTarget(btnPaste, "btnPaste.Image", _converter);
            ScaleHandler.AddTarget(btnUndo, "btnUndo.Image", _converter);
            ScaleHandler.AddTarget(btnRedo, "btnRedo.Image", _converter);
            ScaleHandler.AddTarget(btnSettings, "btnSettings.Image", _converter);
            ScaleHandler.AddTarget(btnHelp, "btnHelp.Image", _converter);
            ScaleHandler.AddTarget(obfuscateModeButton, "obfuscateModeButton.Image", _converter);
            ScaleHandler.AddTarget(pixelizeToolStripMenuItem, "pixelizeToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(blurToolStripMenuItem, "blurToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(highlightModeButton, "highlightModeButton.Image", _converter);
            ScaleHandler.AddTarget(textHighlightMenuItem, "textHighlightMenuItem.Image", _converter);
            ScaleHandler.AddTarget(areaHighlightMenuItem, "areaHighlightMenuItem.Image", _converter);
            ScaleHandler.AddTarget(grayscaleHighlightMenuItem, "grayscaleHighlightMenuItem.Image", _converter);
            ScaleHandler.AddTarget(magnifyMenuItem, "magnifyMenuItem.Image", _converter);
            ScaleHandler.AddTarget(btnFillColor, "btnFillColor.Image", _converter);
            ScaleHandler.AddTarget(btnLineColor, "btnLineColor.Image", _converter);
            ScaleHandler.AddTarget(fontBoldButton, "fontBoldButton.Image", _converter);
            ScaleHandler.AddTarget(fontItalicButton, "fontItalicButton.Image", _converter);
            ScaleHandler.AddTarget(textVerticalAlignmentButton, "btnAlignMiddle.Image", _converter);
            ScaleHandler.AddTarget(alignTopToolStripMenuItem, "btnAlignTop.Image", _converter);
            ScaleHandler.AddTarget(alignMiddleToolStripMenuItem, "btnAlignMiddle.Image", _converter);
            ScaleHandler.AddTarget(alignBottomToolStripMenuItem, "btnAlignBottom.Image", _converter);
            ScaleHandler.AddTarget(arrowHeadsDropDownButton, "arrowHeadsDropDownButton.Image", _converter);
            ScaleHandler.AddTarget(shadowButton, "shadowButton.Image", _converter);
            ScaleHandler.AddTarget(btnConfirm, "btnConfirm.Image", _converter);
            ScaleHandler.AddTarget(btnCancel, "btnCancel.Image", _converter);
            ScaleHandler.AddTarget(closeToolStripMenuItem, "closeToolStripMenuItem.Image", _converter);
            ScaleHandler.AddTarget(textHorizontalAlignmentButton, "btnAlignCenter.Image", _converter);
            ScaleHandler.AddTarget(alignLeftToolStripMenuItem, "btnAlignLeft.Image", _converter);
            ScaleHandler.AddTarget(alignCenterToolStripMenuItem, "btnAlignCenter.Image", _converter);
            ScaleHandler.AddTarget(alignRightToolStripMenuItem, "btnAlignRight.Image", _converter);
        }

        /// <summary>
        ///     An Implementation for the IImageEditor, this way Plugins have access to the HWND handles wich can be used with
        ///     Win32 API calls.
        /// </summary>
        public IWin32Window WindowHandle => this;

        public ISurface Surface
        {
            get { return _surface; }
            set { SetSurface(value); }
        }

        /// <summary>
        ///     Remove the current surface
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
        ///     Change the surface
        /// </summary>
        /// <param name="newSurface"></param>
        private void SetSurface(ISurface newSurface)
        {
            if (Surface != null && Surface.Modified)
            {
                throw new ApplicationException("Surface modified");
            }

            _disposables?.Dispose();
            RemoveSurface();

            panel1.Height = 10;
            panel1.Width = 10;
            _surface = newSurface as Surface;
            if (_surface != null)
            {
                panel1.Controls.Add(_surface);
            }
            var backgroundForTransparency = GreenshotResources.Instance.GetBitmap("Checkerboard.Image");
            if (_surface != null)
            {
                _surface.TransparencyBackgroundBrush = new TextureBrush(backgroundForTransparency.NativeBitmap, WrapMode.Tile);

                _surface.MovingElementChanged += (sender, args) => RefreshEditorControls();
                _surface.DrawingModeChanged += SurfaceDrawingModeChanged;
                _surface.SurfaceSizeChanged += SurfaceSizeChanged;
                _surface.SurfaceMessage += SurfaceMessageReceived;
                _surface.FieldAggregator.FieldChanged += FieldAggregatorFieldChanged;
                SurfaceSizeChanged(Surface, null);

                BindFieldControls();
                RefreshEditorControls();
                // Fix title
                if (_surface?.CaptureDetails?.Title != null)
                {
                    Text = $"{_surface.CaptureDetails.Title} - {_editorLanguage.EditorTitle}";
                }
            }
            // Make sure the value is set correctly when starting
            counterUpDown.Value = newSurface.CounterStart;

            Activate();
            // TODO: Await?
            _ = InteropWindowFactory.CreateFor(Handle).ToForegroundAsync();
        }

        private void UpdateUi()
        {
            // Disable access to the settings, for feature #3521446
            preferencesToolStripMenuItem.Visible = !_coreConfiguration.DisableSettings;
            toolStripSeparator12.Visible = !_coreConfiguration.DisableSettings;
            toolStripSeparator11.Visible = !_coreConfiguration.DisableSettings;
            btnSettings.Visible = !_coreConfiguration.DisableSettings;

            // Make sure Double-buffer is enabled
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // resizing the panel is futile, since it is docked. however, it seems
            // to fix the bug (?) with the vscrollbar not being able to shrink to
            // a smaller size than the initial panel size (as set by the forms designer)
            panel1.Height = 10;

            fontFamilyComboBox.PropertyChanged += FontPropertyChanged;

            obfuscateModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;
            highlightModeButton.DropDownItemClicked += FilterPresetDropDownItemClicked;

            _toolbarButtons = new[] {btnCursor, btnRect, btnEllipse, btnText, btnLine, btnArrow, btnFreehand, btnHighlight, btnObfuscate, btnCrop, btnStepLabel, btnSpeechBubble};
            //toolbarDropDownButtons = new ToolStripDropDownButton[]{btnBlur, btnPixeliate, btnTextHighlighter, btnAreaHighlighter, btnMagnifier};

            pluginToolStripMenuItem.Visible = pluginToolStripMenuItem.DropDownItems.Count > 0;

            // Workaround: for the MouseWheel event which doesn't get to the panel
            MouseWheel += PanelMouseWheel;
            ApplyLanguage();
        }

        /// <summary>
        ///     Workaround for having a border around the dropdown
        ///     See: http://stackoverflow.com/questions/9560812/change-border-of-toolstripcombobox-with-flat-style
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertiesToolStrip_Paint(object sender, PaintEventArgs e)
        {
            using (var cbBorderPen = new Pen(SystemColors.ActiveBorder))
            {
                // Loop over all items in the propertiesToolStrip
                foreach (ToolStripItem item in propertiesToolStrip.Items)
                {
                    // Only ToolStripComboBox that are visible
                    if (!(item is ToolStripComboBox cb) || !cb.Visible)
                    {
                        continue;
                    }
                    // Calculate the rectangle
                    if (cb.ComboBox != null)
                    {
                        var r = new Rectangle(cb.ComboBox.Location.X - 1, cb.ComboBox.Location.Y - 1, cb.ComboBox.Size.Width + 1, cb.ComboBox.Size.Height + 1);

                        // Draw the rectangle
                        e.Graphics.DrawRectangle(cbBorderPen, r);
                    }
                }
            }
        }

        /// <summary>
        ///     Get all the destinations and display them in the file menu and the buttons
        /// </summary>
        private async Task AddDestinationsAsync()
        {
            await Task.Run(() =>
            {
                // Create export buttons 
                foreach (var destination in _destinationHolder.AllDestinations
                    .Where(destination => destination.Metadata.Priority > 2 && !IgnoreDestinations.Contains(destination.Metadata.Designation) && destination.Value.IsActive)
                    .OrderBy(destination => destination.Metadata.Priority).ThenBy(destination => destination.Value.Description)
                    .Select(d => d.Value))
                {
                    try
                    {
                        if (InvokeRequired)
                        {
                            Invoke((Action<IDestination>) AddDestinationButton, destination);
                        }
                        else
                        {
                            AddDestinationButton(destination);
                        }
                    }
                    catch (Exception addingException)
                    {
                        Log.Warn().WriteLine("Problem adding destination {0}", destination.Designation);
                        Log.Warn().WriteLine(addingException, "Exception: ");
                    }
                }
            });
        }

        private void AddDestinationButton(IDestination toolstripDestination)
        {
            if (!toolstripDestination.HasDisplayIcon)
            {
                return;
            }
            if (toolstripDestination.IsDynamic)
            {
                var icon = toolstripDestination.GetDisplayIcon(FormDpiHandler.Dpi);
                var destinationButton = new ToolStripSplitButton
                {
                    DropDownButtonWidth = FormDpiHandler.ScaleWithCurrentDpi(8),
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    Text = toolstripDestination.Description,
                    Image = icon.ScaleIconForDisplaying(FormDpiHandler.Dpi).NativeBitmap
                };

                if (!Equals(icon.NativeBitmap, destinationButton.Image))
                {
                    destinationButton.Disposed += (sender, args) =>
                    {
                        destinationButton.Image.Dispose();
                    };
                }

                //ToolStripDropDownButton destinationButton = new ToolStripDropDownButton();

                icon = toolstripDestination.GetDisplayIcon(FormDpiHandler.Dpi);
                var defaultItem = new ToolStripMenuItem(toolstripDestination.Description)
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    Tag = toolstripDestination,
                    Image = icon.ScaleIconForDisplaying(FormDpiHandler.Dpi).NativeBitmap
                };
                if (!Equals(icon.NativeBitmap, defaultItem.Image))
                {
                    defaultItem.Disposed += (sender, args) => defaultItem.Image.Dispose();
                }
                defaultItem.Click += async (sender, args) => await toolstripDestination.ExportCaptureAsync(true, _surface, _surface.CaptureDetails);

                // The ButtonClick, this is for the icon, gets the current default item
                destinationButton.ButtonClick += async (sender, args) => await toolstripDestination.ExportCaptureAsync(true, _surface, _surface.CaptureDetails);

                // Generate the entries for the drop down
                destinationButton.DropDownOpening += (sender, args) =>
                {
                    ClearItems(destinationButton.DropDownItems);
                    destinationButton.DropDownItems.Add(defaultItem);

                    foreach (var subDestination in toolstripDestination.DynamicDestinations().OrderBy(destination => destination.Description))
                    {
                        icon = subDestination.GetDisplayIcon(FormDpiHandler.Dpi);
                        var destinationMenuItem = new ToolStripMenuItem(subDestination.Description)
                        {
                            Tag = subDestination,
                            Image = icon.ScaleIconForDisplaying(96).NativeBitmap
                        };
                        if (!Equals(icon.NativeBitmap, destinationMenuItem.Image))
                        {
                            // Dispose of the newly generated icon
                            destinationMenuItem.Disposed += (o, a) =>
                            {
                                destinationMenuItem.Image.Dispose();
                            };
                        }

                        destinationMenuItem.Click += async (o, a) => await subDestination.ExportCaptureAsync(true, _surface, _surface.CaptureDetails);
                        destinationButton.DropDownItems.Add(destinationMenuItem);
                    }
                };

                destinationsToolStrip.Items.Insert(destinationsToolStrip.Items.IndexOf(toolStripSeparator16), destinationButton);
            }
            else
            {
                var icon = toolstripDestination.GetDisplayIcon(FormDpiHandler.Dpi);
                var destinationButton = new ToolStripButton
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    Text = toolstripDestination.Description,
                    Image = icon.ScaleIconForDisplaying(FormDpiHandler.Dpi).NativeBitmap
                };
                destinationButton.Click += async (sender, args) => await toolstripDestination.ExportCaptureAsync(true, _surface, _surface.CaptureDetails);
                if (!Equals(icon.NativeBitmap, destinationButton.Image))
                {
                    destinationButton.Disposed += (sender, args) =>
                    {
                        destinationButton.Image.Dispose();
                    };
                }
                destinationsToolStrip.Items.Insert(destinationsToolStrip.Items.IndexOf(toolStripSeparator16), destinationButton);
            }
        }

        /// <summary>
        ///     According to some information I found, the clear doesn't work correctly when the shortcutkeys are set?
        ///     This helper method takes care of this.
        /// </summary>
        /// <param name="items"></param>
        private static void ClearItems(ToolStripItemCollection items)
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
            foreach (var destination in _destinationHolder.AllDestinations
                .Where(destination => !IgnoreDestinations.Contains(destination.Metadata.Designation) && destination.Value.IsActive)
                .OrderBy(destination => destination.Metadata.Priority).ThenBy(destination => destination.Value.Description)
                .Select(d => d.Value))
            {
                var item = destination.GetMenuItem(true, null, DestinationToolStripMenuItemClick, _destinationScaleHandler);
                if (item == null)
                {
                    continue;
                }

                var icon = destination.GetDisplayIcon(FormDpiHandler.Dpi);
                item.Text = destination.Description;
                item.Image = icon.ScaleIconForDisplaying(FormDpiHandler.Dpi).NativeBitmap;
                item.ShortcutKeys = destination.EditorShortcutKeys;
                fileStripMenuItem.DropDownItems.Add(item);
            }
            // add the elements after the destinations
            fileStripMenuItem.DropDownItems.Add(toolStripSeparator9);
            fileStripMenuItem.DropDownItems.Add(closeToolStripMenuItem);
        }

        /// <summary>
        ///     This is the SufraceMessageEvent receiver which display a message in the status bar if the
        ///     surface is exported. It also updates the title to represent the filename, if there is one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void SurfaceMessageReceived(object sender, SurfaceMessageEventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                Invoke(new SurfaceMessageReceivedThreadSafeDelegate(SurfaceMessageReceived), sender, eventArgs);
            }
            else
            {
                var dateTime = DateTime.Now.ToLongTimeString();
                // TODO: Fix that we only open files, like in the tooltip
                switch (eventArgs.MessageType)
                {
                    case SurfaceMessageTyp.FileSaved:
                        // Put the event message on the status label and attach the context menu
                        UpdateStatusLabel(dateTime + " - " + eventArgs.Message, fileSavedStatusContextMenu);
                        // Change title
                        Text = eventArgs.Surface.LastSaveFullPath + " - " + _editorLanguage.EditorTitle;
                        break;
                    default:
                        // Put the event message on the status label
                        UpdateStatusLabel(dateTime + " - " + eventArgs.Message);
                        break;
                }
            }
        }

        /// <summary>
        ///     This is called when the size of the surface chances, used for resizing and displaying the size information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SurfaceSizeChanged(object sender, EventArgs e)
        {
            if (_editorConfiguration.MatchSizeToCapture)
            {
                // Set editor's initial size to the size of the surface plus the size of the chrome
                var imageSize = Surface.Screenshot.Size;
                var currentFormSize = Size;
                var currentImageClientSize = panel1.ClientSize;
                var minimumFormWidth = 650;
                var minimumFormHeight = 530;
                var newWidth = Math.Max(minimumFormWidth, currentFormSize.Width - currentImageClientSize.Width + imageSize.Width);
                var newHeight = Math.Max(minimumFormHeight, currentFormSize.Height - currentImageClientSize.Height + imageSize.Height);
                Size = new Size(newWidth, newHeight);
            }
            dimensionsLabel.Text = Surface.Screenshot.Width + "x" + Surface.Screenshot.Height;
            ImageEditorFormResize(sender, new EventArgs());
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
            UpdateStatusLabel(string.Format(_editorLanguage.EditorImagesaved, fullpath), fileSavedStatusContextMenu);
            Text = Path.GetFileName(fullpath) + " - " + _editorLanguage.EditorTitle;
        }

        private void SurfaceDrawingModeChanged(object source, SurfaceDrawingModeEventArgs eventArgs)
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
            }
        }

        private void BindFieldControls()
        {
            _disposables = new CompositeDisposable
            {
                new BidirectionalBinding(btnFillColor, "SelectedColor", _surface.FieldAggregator.GetField(FieldTypes.FILL_COLOR), "Value", NotNullValidator.GetInstance()),
                new BidirectionalBinding(btnLineColor, "SelectedColor", _surface.FieldAggregator.GetField(FieldTypes.LINE_COLOR), "Value", NotNullValidator.GetInstance()),
                new BidirectionalBinding(lineThicknessUpDown, "Value", _surface.FieldAggregator.GetField(FieldTypes.LINE_THICKNESS), "Value", DecimalIntConverter.GetInstance(),NotNullValidator.GetInstance()),
                new BidirectionalBinding(blurRadiusUpDown, "Value", _surface.FieldAggregator.GetField(FieldTypes.BLUR_RADIUS), "Value", DecimalIntConverter.GetInstance(),NotNullValidator.GetInstance()),
                new BidirectionalBinding(magnificationFactorUpDown, "Value", _surface.FieldAggregator.GetField(FieldTypes.MAGNIFICATION_FACTOR), "Value",DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance()),
                new BidirectionalBinding(pixelSizeUpDown, "Value", _surface.FieldAggregator.GetField(FieldTypes.PIXEL_SIZE), "Value", DecimalIntConverter.GetInstance(),NotNullValidator.GetInstance()),
                new BidirectionalBinding(brightnessUpDown, "Value", _surface.FieldAggregator.GetField(FieldTypes.BRIGHTNESS), "Value", DecimalDoublePercentageConverter.GetInstance(), NotNullValidator.GetInstance()),
                new BidirectionalBinding(fontFamilyComboBox, "Text", _surface.FieldAggregator.GetField(FieldTypes.FONT_FAMILY), "Value", NotNullValidator.GetInstance()),
                new BidirectionalBinding(fontSizeUpDown, "Value", _surface.FieldAggregator.GetField(FieldTypes.FONT_SIZE), "Value", DecimalFloatConverter.GetInstance(),NotNullValidator.GetInstance()),
                new BidirectionalBinding(fontBoldButton, "Checked", _surface.FieldAggregator.GetField(FieldTypes.FONT_BOLD), "Value", NotNullValidator.GetInstance()),
                new BidirectionalBinding(fontItalicButton, "Checked", _surface.FieldAggregator.GetField(FieldTypes.FONT_ITALIC), "Value", NotNullValidator.GetInstance()),
                new BidirectionalBinding(textHorizontalAlignmentButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldTypes.TEXT_HORIZONTAL_ALIGNMENT), "Value",NotNullValidator.GetInstance()),
                new BidirectionalBinding(textVerticalAlignmentButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldTypes.TEXT_VERTICAL_ALIGNMENT), "Value",NotNullValidator.GetInstance()),
                new BidirectionalBinding(shadowButton, "Checked", _surface.FieldAggregator.GetField(FieldTypes.SHADOW), "Value", NotNullValidator.GetInstance()),
                new BidirectionalBinding(obfuscateModeButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldTypes.PREPARED_FILTER_OBFUSCATE), "Value"),
                new BidirectionalBinding(highlightModeButton, "SelectedTag", _surface.FieldAggregator.GetField(FieldTypes.PREPARED_FILTER_HIGHLIGHT), "Value"),
                new BidirectionalBinding(counterUpDown, "Value", _surface, "CounterStart", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance())
            };
        }

        /// <summary>
        ///     shows/hides field controls (2nd toolbar on top) depending on fields of selected elements
        /// </summary>
        private void RefreshFieldControls()
        {
            propertiesToolStrip.SuspendLayout();
            if (_surface.HasSelectedElements || _surface.DrawingMode != DrawingModes.None)
            {
                var props = _surface.FieldAggregator;
                btnFillColor.Visible = props.HasFieldValue(FieldTypes.FILL_COLOR);
                btnLineColor.Visible = props.HasFieldValue(FieldTypes.LINE_COLOR);
                lineThicknessLabel.Visible = lineThicknessUpDown.Visible = props.HasFieldValue(FieldTypes.LINE_THICKNESS);
                blurRadiusLabel.Visible = blurRadiusUpDown.Visible = props.HasFieldValue(FieldTypes.BLUR_RADIUS);
                magnificationFactorLabel.Visible = magnificationFactorUpDown.Visible = props.HasFieldValue(FieldTypes.MAGNIFICATION_FACTOR);
                pixelSizeLabel.Visible = pixelSizeUpDown.Visible = props.HasFieldValue(FieldTypes.PIXEL_SIZE);
                brightnessLabel.Visible = brightnessUpDown.Visible = props.HasFieldValue(FieldTypes.BRIGHTNESS);
                arrowHeadsLabel.Visible = arrowHeadsDropDownButton.Visible = props.HasFieldValue(FieldTypes.ARROWHEADS);
                fontFamilyComboBox.Visible = props.HasFieldValue(FieldTypes.FONT_FAMILY);
                fontSizeLabel.Visible = fontSizeUpDown.Visible = props.HasFieldValue(FieldTypes.FONT_SIZE);
                fontBoldButton.Visible = props.HasFieldValue(FieldTypes.FONT_BOLD);
                fontItalicButton.Visible = props.HasFieldValue(FieldTypes.FONT_ITALIC);
                textHorizontalAlignmentButton.Visible = props.HasFieldValue(FieldTypes.TEXT_HORIZONTAL_ALIGNMENT);
                textVerticalAlignmentButton.Visible = props.HasFieldValue(FieldTypes.TEXT_VERTICAL_ALIGNMENT);
                shadowButton.Visible = props.HasFieldValue(FieldTypes.SHADOW);
                counterLabel.Visible = counterUpDown.Visible = props.HasFieldValue(FieldTypes.FLAGS)
                                                               && ((FieldFlag) props.GetFieldValue(FieldTypes.FLAGS) & FieldFlag.Counter) == FieldFlag.Counter;
                btnConfirm.Visible = btnCancel.Visible = props.HasFieldValue(FieldTypes.FLAGS)
                                                         && ((FieldFlag) props.GetFieldValue(FieldTypes.FLAGS) & FieldFlag.Confirmable) == FieldFlag.Confirmable;

                obfuscateModeButton.Visible = props.HasFieldValue(FieldTypes.PREPARED_FILTER_OBFUSCATE);
                highlightModeButton.Visible = props.HasFieldValue(FieldTypes.PREPARED_FILTER_HIGHLIGHT);
            }
            else
            {
                HideToolstripItems();
            }
            propertiesToolStrip.ResumeLayout(true);
        }

        private void HideToolstripItems()
        {
            foreach (ToolStripItem toolStripItem in propertiesToolStrip.Items)
            {
                toolStripItem.Visible = false;
            }
        }

        /// <summary>
        ///     refreshes all editor controls depending on selected elements and their fields
        /// </summary>
        private void RefreshEditorControls()
        {
            var stepLabels = _surface.CountStepLabels(null);
            if (stepLabels <= 20)
            {
                ScaleHandler.AddTarget(btnStepLabel, $"btnStepLabel{stepLabels:00}.Image", _converter, FormDpiHandler.Dpi > 0);
                ScaleHandler.AddTarget(addCounterToolStripMenuItem, $"btnStepLabel{stepLabels:00}.Image", _converter, FormDpiHandler.Dpi > 0);
            }
            else
            {
                ScaleHandler.AddTarget(btnStepLabel, $"btnStepLabel20+.Image", _converter, FormDpiHandler.Dpi > 0);
                ScaleHandler.AddTarget(addCounterToolStripMenuItem, $"btnStepLabel20+.Image", _converter, FormDpiHandler.Dpi > 0);
            }

            var props = _surface.FieldAggregator;
            // if a confirmable element is selected, we must disable most of the controls
            // since we demand confirmation or cancel for confirmable element
            if (props.HasFieldValue(FieldTypes.FLAGS) && ((FieldFlag) props.GetFieldValue(FieldTypes.FLAGS) & FieldFlag.Confirmable) == FieldFlag.Confirmable)
            {
                // disable most controls
                if (!_controlsDisabledDueToConfirmable)
                {
                    ToolStripItemEndisabler.Disable(menuStrip1);
                    ToolStripItemEndisabler.Disable(destinationsToolStrip);
                    ToolStripItemEndisabler.Disable(toolsToolStrip);
                    ToolStripItemEndisabler.Enable(closeToolStripMenuItem);
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

            // en/disablearrage controls depending on hierarchy of selected elements
            var actionAllowedForSelection = _surface.HasSelectedElements && !_controlsDisabledDueToConfirmable;
            var push = actionAllowedForSelection && _surface.CanPushSelectionDown();
            var pull = actionAllowedForSelection && _surface.CanPullSelectionUp();
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


        private void ArrowHeadsToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.FieldAggregator.GetField(FieldTypes.ARROWHEADS).Value = (ArrowContainer.ArrowHeadCombination) ((ToolStripMenuItem) sender).Tag;
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

            var boldAvailable = fontFamily.IsStyleAvailable(FontStyle.Bold);
            if (fontBoldButton != null)
            {
                if (!boldAvailable)
                {
                    _originalBoldCheckState = fontBoldButton.Checked;
                    fontBoldButton.Checked = false;
                }
                fontBoldButton.Enabled = boldAvailable;
            }

            var italicAvailable = fontFamily.IsStyleAvailable(FontStyle.Italic);
            if (fontItalicButton != null)
            {
                if (!italicAvailable)
                {
                    fontItalicButton.Checked = false;
                }
                fontItalicButton.Enabled = italicAvailable;
            }

            var regularAvailable = fontFamily.IsStyleAvailable(FontStyle.Regular);
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
            else if (italicAvailable && fontItalicButton != null)
            {
                fontItalicButton.Checked = true;
            }
        }

        private void FieldAggregatorFieldChanged(object sender, FieldChangedEventArgs e)
        {
            // in addition to selection, deselection of elements, we need to
            // refresh toolbar if prepared filter mode is changed
            if (Equals(e.Field.FieldType, FieldTypes.PREPARED_FILTER_HIGHLIGHT))
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
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Greenshot templates (*.gst)|*.gst",
                FileName = FilenameHelper.GetFilenameWithoutExtensionFromPattern(_coreConfiguration.OutputFileFilenamePattern, _surface.CaptureDetails)
            };
            var dialogResult = saveFileDialog.ShowDialog();
            if (!dialogResult.Equals(DialogResult.OK))
            {
                return;
            }
            using (Stream streamWrite = File.OpenWrite(saveFileDialog.FileName))
            {
                _surface.SaveElementsToStream(streamWrite);
            }
        }

        private void LoadElementsToolStripMenuItemClick(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Greenshot templates (*.gst)|*.gst"
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            using (Stream streamRead = File.OpenRead(openFileDialog.FileName))
            {
                _surface.LoadElementsFromStream(streamRead);
            }
            _surface.Refresh();
        }

        private void DestinationToolStripMenuItemClick(object sender, EventArgs e)
        {
            IDestination clickedDestination = null;
            if (sender is Control control)
            {
                var clickedControl = control;
                if (clickedControl.ContextMenuStrip != null)
                {
                    clickedControl.ContextMenuStrip.Show(Cursor.Position);
                    return;
                }
                clickedDestination = (IDestination) clickedControl.Tag;
            }
            else
            {
                if (sender is ToolStripMenuItem item)
                {
                    var clickedMenuItem = item;
                    clickedDestination = (IDestination) clickedMenuItem.Tag;
                }
            }
            var exportInformation = clickedDestination?.ExportCaptureAsync(true, _surface, _surface.CaptureDetails).Result;
            if (exportInformation != null && exportInformation.ExportMade)
            {
                _surface.Modified = false;
            }
        }

        protected void FilterPresetDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            RefreshFieldControls();
            Invalidate(true);
        }

        private void SelectAllToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.SelectAllElements();
        }


        private void BtnConfirmClick(object sender, EventArgs e)
        {
            _surface.ConfirmSelectedConfirmableElements(true);
            RefreshFieldControls();
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            _surface.ConfirmSelectedConfirmableElements(false);
            RefreshFieldControls();
        }

        private void Insert_window_toolstripmenuitemMouseEnter(object sender, EventArgs e)
        {
            var captureWindowMenuItem = (ToolStripMenuItem) sender;
            // TODO: Fix showing windows
            // MainForm.Instance.AddCaptureWindowMenuItems(captureWindowMenuItem, Contextmenu_window_Click);
        }
/*

        private void Contextmenu_window_Click(object sender, EventArgs e)
        {
            var clickedItem = (ToolStripMenuItem) sender;
            try
            {
                var windowToCapture = (IInteropWindow) clickedItem.Tag;
                ICapture capture = new Capture();
                using (var graphics = Graphics.FromHwnd(Handle))
                {
                    capture.CaptureDetails.DpiX = graphics.DpiY;
                    capture.CaptureDetails.DpiY = graphics.DpiY;
                }
                windowToCapture = CaptureHelper.SelectCaptureWindow(windowToCapture);
                if (windowToCapture != null)
                {
                    capture = CaptureHelper.CaptureWindow(windowToCapture, capture, coreConfiguration.WindowCaptureMode);
                    if (capture?.CaptureDetails != null && capture.Bitmap != null)
                    {
                        capture.Bitmap.SetResolution(capture.CaptureDetails.DpiX, capture.CaptureDetails.DpiY);
                        _surface.AddImageContainer(capture.Bitmap, 100, 100);
                    }
                    Activate();
                    // TODO: Await?
                    InteropWindowFactory.CreateFor(Handle).ToForegroundAsync();
                }

                capture?.Dispose();
            }
            catch (Exception exception)
            {
                Log.Error().WriteLine(exception);
            }
        }
*/

        private void AutoCropToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_surface.AutoCrop())
            {
                RefreshFieldControls();
            }
        }

        private void AddBorderToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.ApplyBitmapEffect(new BorderEffect());
            UpdateUndoRedoSurfaceDependencies();
        }

        /// <summary>
        ///     Added for FEATURE-919, increasing the canvas by 25 pixels in every direction.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnlargeCanvasToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.ApplyBitmapEffect(new ResizeCanvasEffect(25, 25, 25, 25));
            UpdateUndoRedoSurfaceDependencies();
        }

        /// <summary>
        ///     Added for FEATURE-919, to make the capture as small as possible again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShrinkCanvasToolStripMenuItemClick(object sender, EventArgs e)
        {
            NativeRect cropRectangle;
            using (var tmpImage = GetImageForExport())
            {
                cropRectangle = tmpImage.FindAutoCropRectangle(_coreConfiguration.AutoCropDifference);
            }
            if (!_surface.IsCropPossible(ref cropRectangle))
            {
                return;
            }
            _surface.ApplyCrop(cropRectangle);
            UpdateUndoRedoSurfaceDependencies();
        }

        /// <summary>
        ///     This is used when the drop shadow button is used
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">MouseEventArgs</param>
        private void AddDropshadowToolStripMenuItemMouseUp(object sender, MouseEventArgs e)
        {
            var dropShadowEffect = _editorConfiguration.DropShadowEffectSettings;
            bool apply;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    apply = true;
                    break;
                case MouseButtons.Right:
                    using (var dropShadowSettingsForm = _dropShadowSettingsFormFactory(dropShadowEffect))
                    {
                        var result = dropShadowSettingsForm.Value.ShowDialog(this);
                        apply = result == DialogResult.OK;
                    }
                    break;
                default:
                    return;
            }

            if (apply)
            {
                _surface.ApplyBitmapEffect(dropShadowEffect);
                UpdateUndoRedoSurfaceDependencies();
            }
        }


        /// <summary>
        ///     Open the resize settings from, and resize if ok was pressed
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        private void BtnResizeClick(object sender, EventArgs e)
        {
            var resizeEffect = new ResizeEffect(_surface.Screenshot.Width, _surface.Screenshot.Height, true);
            using (var resizeSettingsForm = _resizeSettingsFormFactory(resizeEffect))
            {
                var result = resizeSettingsForm.Value.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    _surface.ApplyBitmapEffect(resizeEffect);
                    UpdateUndoRedoSurfaceDependencies();
                }
            }
        }

        /// <summary>
        ///     This is used when the torn-edge button is used
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">MouseEventArgs</param>
        private void TornEdgesToolStripMenuItemMouseUp(object sender, MouseEventArgs e)
        {
            var tornEdgeEffect = _editorConfiguration.TornEdgeEffectSettings;
            bool apply;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    apply = true;
                    break;
                case MouseButtons.Right:
                    using (var ownedForm = _tornEdgeSettingsFormFactory(tornEdgeEffect))
                    {
                        var result = ownedForm.Value.ShowDialog(this);
                        apply = result == DialogResult.OK;
                    }                    
                    break;
                default:
                    return;
            }

            if (apply)
            {
                _surface.ApplyBitmapEffect(tornEdgeEffect);
                UpdateUndoRedoSurfaceDependencies();
            }
        }

        private void GrayscaleToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.ApplyBitmapEffect(new GrayscaleEffect());
            UpdateUndoRedoSurfaceDependencies();
        }

        private void ClearToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.Clear(Color.Transparent);
            UpdateUndoRedoSurfaceDependencies();
        }

        private void RotateCwToolstripButtonClick(object sender, EventArgs e)
        {
            _surface.ApplyBitmapEffect(new RotateEffect(90));
            UpdateUndoRedoSurfaceDependencies();
        }

        private void RotateCcwToolstripButtonClick(object sender, EventArgs e)
        {
            _surface.ApplyBitmapEffect(new RotateEffect(270));
            UpdateUndoRedoSurfaceDependencies();
        }

        private void InvertToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.ApplyBitmapEffect(new InvertEffect());
            UpdateUndoRedoSurfaceDependencies();
        }

        private void ImageEditorFormResize(object sender, EventArgs e)
        {
            if (Surface?.Screenshot == null || panel1 == null)
            {
                return;
            }
            var imageSize = Surface.Screenshot.Size;
            var currentClientSize = panel1.ClientSize;
            var canvas = Surface as Control;
            var panel = (Panel) canvas?.Parent;
            if (panel == null)
            {
                return;
            }
            var offsetX = -panel.HorizontalScroll.Value;
            var offsetY = -panel.VerticalScroll.Value;
            if (currentClientSize.Width > imageSize.Width)
            {
                canvas.Left = offsetX + (currentClientSize.Width - imageSize.Width) / 2;
            }
            else
            {
                canvas.Left = offsetX + 0;
            }
            if (currentClientSize.Height > imageSize.Height)
            {
                canvas.Top = offsetY + (currentClientSize.Height - imageSize.Height) / 2;
            }
            else
            {
                canvas.Top = offsetY + 0;
            }
        }

        private delegate void SurfaceMessageReceivedThreadSafeDelegate(object sender, SurfaceMessageEventArgs eventArgs);

        /**
         * Interfaces for plugins, see GreenshotInterface for more details!
         */

        public IBitmapWithNativeSupport GetImageForExport()
        {
            return _surface.GetBitmapForExport();
        }

        public ICaptureDetails CaptureDetails => _surface.CaptureDetails;

        public ToolStripMenuItem GetPluginMenuItem()
        {
            return pluginToolStripMenuItem;
        }

        public ToolStripMenuItem GetFileMenuItem()
        {
            return fileStripMenuItem;
        }

        private void BtnSaveClick(object sender, EventArgs e)
        {
            var destinationDesignation = "FileNoDialog";
            if (_surface.LastSaveFullPath == null)
            {
                destinationDesignation = "FileDialog";
            }
            _destinationHolder.AllDestinations.Find(destinationDesignation)?.ExportCaptureAsync(true, _surface, _surface.CaptureDetails);
        }

        private void BtnClipboardClick(object sender, EventArgs e)
        {
            _destinationHolder.AllDestinations.Find("Clipboard")?.ExportCaptureAsync(true, _surface, _surface.CaptureDetails);
        }

        private void BtnPrintClick(object sender, EventArgs e)
        {
            // The BeginInvoke is a solution for the printdialog not having focus
            BeginInvoke((MethodInvoker) delegate
            {
                _destinationHolder.AllDestinations.Find("Printer")?.ExportCaptureAsync(true, _surface, _surface.CaptureDetails);
            });
        }

        private void CloseToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
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
            _surface.DrawingMode = DrawingModes.Crop;
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
                foreach (var butt in _toolbarButtons)
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
        }

        private void BtnCutClick(object sender, EventArgs e)
        {
            CutToolStripMenuItemClick(sender, e);
        }

        private void CopyToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.CopySelectedElements();
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
            // TODO: Fix Help
            // HelpFileLoader.LoadHelp();
        }

        private void AboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO: Fix About
            // MainForm.Instance.ShowAbout();
        }

        private void PreferencesToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO: Fix settings
            // MainForm.Instance.ShowSetting();
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
            var interopWindow = InteropWindowFactory.CreateFor(Handle);
            if (_surface.Modified && !_editorConfiguration.SuppressSaveDialogAtClose)
            {
                // Make sure the editor is visible
                // TODO: Await?
                interopWindow.ToForegroundAsync();

                var buttons = MessageBoxButtons.YesNoCancel;
                // Dissallow "CANCEL" if the application needs to shutdown
                if (e.CloseReason == CloseReason.ApplicationExitCall || e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing)
                {
                    buttons = MessageBoxButtons.YesNo;
                }
                var result = MessageBox.Show(_editorLanguage.EditorCloseOnSave, _editorLanguage.EditorCloseOnSaveTitle, buttons, MessageBoxIcon.Question);
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
            _editorConfiguration.SetEditorPlacement(interopWindow.GetPlacement());
            // remove from the editor list
            _editorFactory.Remove(this);

            _surface.Dispose();

            GC.Collect();
            if (_coreConfiguration.MinimizeWorkingSetSize)
            {
                PsApi.EmptyWorkingSet();
            }
        }

        private void ImageEditorFormKeyDown(object sender, KeyEventArgs e)
        {
            // Log.Debug().WriteLine("Got key event "+e.KeyCode + ", " + e.Modifiers);
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
                    case Keys.Delete: // Grayscale Ctrl + Delete
                        ClearToolStripMenuItemClick(sender, e);
                        break;
                    case Keys.Oemcomma: // Rotate CCW Ctrl + ,
                        RotateCcwToolstripButtonClick(sender, e);
                        break;
                    case Keys.OemPeriod: // Rotate CW Ctrl + .
                        RotateCwToolstripButtonClick(sender, e);
                        break;
                    case Keys.Add: // Ctrl + +
                    case Keys.Oemplus: // Ctrl + +
                        EnlargeCanvasToolStripMenuItemClick(sender, e);
                        break;
                    case Keys.Subtract: // Ctrl + -
                    case Keys.OemMinus: // Ctrl + -
                        ShrinkCanvasToolStripMenuItemClick(sender, e);
                        break;
                }
            }
        }

        /// <summary>
        ///     This is a "work-around" for the MouseWheel event which doesn't get to the panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PanelMouseWheel(object sender, MouseEventArgs e)
        {
            panel1.Focus();
        }

        /// <inheritdoc />
        protected override bool ProcessKeyPreview(ref Message msg)
        {
            // disable default key handling if surface has requested a lock
            if (_surface.KeysLocked)
            {
                return false;
            }

            return base.ProcessKeyPreview(ref msg);
        }

        /// <inheritdoc />
        protected override bool ProcessCmdKey(ref Message msg, Keys keys)
        {
            // disable default key handling if surface has requested a lock
            if (_surface.KeysLocked)
            {
                return false;
            }

            // Go through the destinations to check the EditorShortcut Keys
            // this way the menu entries don't need to be enabled.
            // This also fixes bugs #3526974 & #3527020
            foreach (var destination in _destinationHolder.SortedActiveDestinations)
            {
                if (destination.EditorShortcutKeys != keys)
                {
                    continue;
                }

                destination.ExportCaptureAsync(true, _surface, _surface.CaptureDetails);
                return true;
            }

            if (_surface.ProcessCmdKey(keys))
            {
                return false;
            }

            return base.ProcessCmdKey(ref msg, keys);
        }

        private void UpdateUndoRedoSurfaceDependencies()
        {
            if (_surface == null)
            {
                return;
            }
            var canUndo = _surface.CanUndo;
            btnUndo.Enabled = canUndo;
            undoToolStripMenuItem.Enabled = canUndo;
            var undoText = canUndo ? _editorLanguage.EditorUndo : "";
            btnUndo.Text = undoText;
            undoToolStripMenuItem.Text = undoText;

            var canRedo = _surface.CanRedo;
            btnRedo.Enabled = canRedo;
            redoToolStripMenuItem.Enabled = canRedo;
            var redoText = canUndo ? _editorLanguage.EditorRedo : "";
            btnRedo.Text = redoText;
            redoToolStripMenuItem.Text = redoText;
        }

        /// <summary>
        /// Take care of updating copy/paste/cut buttons or menu entries
        /// </summary>
        private void UpdateClipboardSurfaceDependencies()
        {
            if (_surface == null)
            {
                return;
            }
            // check dependencies for the Surface
            var hasItems = _surface.HasSelectedElements;
            var actionAllowedForSelection = hasItems && !_controlsDisabledDueToConfirmable;

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
            var hasClipboard = ClipboardHelper.ContainsFormat(SupportedClipboardFormats) || ClipboardHelper.ContainsImage();
            btnPaste.Enabled = hasClipboard && !_controlsDisabledDueToConfirmable;
            pasteToolStripMenuItem.Enabled = hasClipboard && !_controlsDisabledDueToConfirmable;
        }

        private void UpdateStatusLabel(string text, ContextMenuStrip contextMenu = null)
        {
            statusLabel.Text = text;
            statusStrip1.ContextMenuStrip = contextMenu;
        }

        private void StatusLabelClicked(object sender, MouseEventArgs e)
        {
            ToolStrip ss = (StatusStrip) ((ToolStripStatusLabel) sender).Owner;
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
    }
}