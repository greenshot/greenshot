/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Structs;
using Greenshot.Base;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Base.Help;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Destinations;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Fields.Binding;
using Greenshot.Editor.Helpers;
using log4net;

namespace Greenshot.Editor.Forms
{
    /// <summary>
    /// The ImageEditorForm is the editor for Greenshot
    /// </summary>
    public partial class ImageEditorForm : EditorForm, IImageEditor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImageEditorForm));
        private static readonly EditorConfiguration EditorConfiguration = IniConfig.GetIniSection<EditorConfiguration>();

        private static readonly List<string> IgnoreDestinations = new()
        {
            nameof(WellKnownDestinations.Picker),
            EditorDestination.DESIGNATION
        };

        private static readonly List<IImageEditor> EditorList = new();

        private Surface _surface;
        private GreenshotToolStripButton[] _toolbarButtons;

        private static readonly string[] SupportedClipboardFormats =
        {
            typeof(string).FullName, "Text", typeof(IDrawableContainerList).FullName
        };

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
            UpdateUi();
        }

        public ImageEditorForm()
        {
            var image = ImageHelper.CreateEmpty(EditorConfiguration.DefaultEditorSize.Width, EditorConfiguration.DefaultEditorSize.Height, PixelFormat.Format32bppArgb, Color.White, 96f, 96f);
            ISurface surface = new Surface(image);
            Initialize(surface, false);
        }

        public ImageEditorForm(ISurface surface, bool outputMade)
        {
            Initialize(surface, outputMade);
        }

        private void Initialize(ISurface surface, bool outputMade)
        {
            EditorList.Add(this);

            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            ManualLanguageApply = true;
            InitializeComponent();
            // Make sure we change the icon size depending on the scaling
            Load += delegate
            {
                var thread = new Thread(AddDestinations)
                {
                    Name = "add destinations"
                };
                thread.Start();
            };

            // Make sure the editor is placed on the same location as the last editor was on close
            // But only if this still exists, else it will be reset (BUG-1812)
            WindowPlacement editorWindowPlacement = EditorConfiguration.GetEditorPlacement();
            NativeRect screenBounds = DisplayInfo.ScreenBounds;
            if (!screenBounds.Contains(editorWindowPlacement.NormalPosition))
            {
                EditorConfiguration.ResetEditorPlacement();
            }

            // ReSharper disable once UnusedVariable
            WindowDetails thisForm = new(Handle)
            {
                WindowPlacement = EditorConfiguration.GetEditorPlacement()
            };

            // init surface
            Surface = surface;
            // Initial "saved" flag for asking if the image needs to be save
            _surface.Modified = !outputMade;

            UpdateUi();

            // Workaround: for the MouseWheel event which doesn't get to the panel
            MouseWheel += PanelMouseWheel;

            // Use best fit, for those capture modes where we can get huge images
            bool useBestFit = _surface.CaptureDetails.CaptureMode switch
            {
                CaptureMode.File => true,
                CaptureMode.Clipboard => true,
                CaptureMode.IE => true,
                _ => false
            };

            if (useBestFit)
            {
                ZoomBestFitMenuItemClick(this, EventArgs.Empty);
            }

            // Workaround: As the cursor is (mostly) selected on the surface a funny artifact is visible, this fixes it.
            HideToolstripItems();
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
                    Text = _surface.CaptureDetails.Title + " - " + Language.GetString(LangKey.editor_title);
                }
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

            // Make sure Double-buffer is enabled
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // resizing the panel is futile, since it is docked. however, it seems
            // to fix the bug (?) with the vscrollbar not being able to shrink to
            // a smaller size than the initial panel size (as set by the forms designer)
            panel1.Height = 10;

            _toolbarButtons = new[]
            {
                btnCursor, btnRect, btnEllipse, btnText, btnLine, btnArrow, btnFreehand, btnHighlight, btnObfuscate, btnCrop, btnStepLabel, btnSpeechBubble
            };
            //toolbarDropDownButtons = new ToolStripDropDownButton[]{btnBlur, btnPixeliate, btnTextHighlighter, btnAreaHighlighter, btnMagnifier};

            pluginToolStripMenuItem.Visible = pluginToolStripMenuItem.DropDownItems.Count > 0;

            // Make sure the value is set correctly when starting
            if (Surface != null)
            {
                counterUpDown.Value = Surface.CounterStart;
            }

            ApplyLanguage();
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
            Invoke((MethodInvoker) delegate
            {
                // Create export buttons
                foreach (IDestination destination in DestinationHelper.GetAllDestinations())
                {
                    if (destination.Priority <= 2)
                    {
                        continue;
                    }

                    if (!destination.IsActive)
                    {
                        continue;
                    }

                    if (destination.DisplayIcon == null)
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
            });
        }

        private void AddDestinationButton(IDestination toolstripDestination)
        {
            if (toolstripDestination.IsDynamic)
            {
                ToolStripSplitButton destinationButton = new()
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    Size = new Size(23, 22),
                    Text = toolstripDestination.Description,
                    Image = toolstripDestination.DisplayIcon
                };
                //ToolStripDropDownButton destinationButton = new ToolStripDropDownButton();

                ToolStripMenuItem defaultItem = new ToolStripMenuItem(toolstripDestination.Description)
                {
                    Tag = toolstripDestination,
                    Image = toolstripDestination.DisplayIcon
                };
                defaultItem.Click += delegate { toolstripDestination.ExportCapture(true, _surface, _surface.CaptureDetails); };

                // The ButtonClick, this is for the icon, gets the current default item
                destinationButton.ButtonClick += delegate { toolstripDestination.ExportCapture(true, _surface, _surface.CaptureDetails); };

                // Generate the entries for the drop down
                destinationButton.DropDownOpening += delegate
                {
                    ClearItems(destinationButton.DropDownItems);
                    destinationButton.DropDownItems.Add(defaultItem);

                    List<IDestination> subDestinations = new List<IDestination>();
                    subDestinations.AddRange(toolstripDestination.DynamicDestinations());
                    if (subDestinations.Count > 0)
                    {
                        subDestinations.Sort();
                        foreach (IDestination subDestination in subDestinations)
                        {
                            IDestination closureFixedDestination = subDestination;
                            ToolStripMenuItem destinationMenuItem = new ToolStripMenuItem(closureFixedDestination.Description)
                            {
                                Tag = closureFixedDestination,
                                Image = closureFixedDestination.DisplayIcon
                            };
                            destinationMenuItem.Click += delegate { closureFixedDestination.ExportCapture(true, _surface, _surface.CaptureDetails); };
                            destinationButton.DropDownItems.Add(destinationMenuItem);
                        }
                    }
                };

                destinationsToolStrip.Items.Insert(destinationsToolStrip.Items.IndexOf(toolStripSeparator16), destinationButton);
            }
            else
            {
                ToolStripButton destinationButton = new ToolStripButton();
                destinationsToolStrip.Items.Insert(destinationsToolStrip.Items.IndexOf(toolStripSeparator16), destinationButton);
                destinationButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
                destinationButton.Size = new Size(23, 22);
                destinationButton.Text = toolstripDestination.Description;
                destinationButton.Image = toolstripDestination.DisplayIcon;
                destinationButton.Click += delegate { toolstripDestination.ExportCapture(true, _surface, _surface.CaptureDetails); };
            }
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

                if (!destination.IsActive)
                {
                    continue;
                }

                ToolStripMenuItem item = destination.GetMenuItem(true, null, DestinationToolStripMenuItemClick);
                if (item != null)
                {
                    item.ShortcutKeys = destination.EditorShortcutKeys;
                    fileStripMenuItem.DropDownItems.Add(item);
                }
            }

            // add the elements after the destinations
            fileStripMenuItem.DropDownItems.Add(toolStripSeparator9);
            fileStripMenuItem.DropDownItems.Add(closeToolStripMenuItem);
        }

        private delegate void SurfaceMessageReceivedThreadSafeDelegate(object sender, SurfaceMessageEventArgs eventArgs);

        /// <summary>
        /// This is the SurfaceMessageEvent receiver which display a message in the status bar if the
        /// surface is exported. It also updates the title to represent the filename, if there is one.
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
                string dateTime = DateTime.Now.ToLongTimeString();
                // TODO: Fix that we only open files, like in the tooltip
                switch (eventArgs.MessageType)
                {
                    case SurfaceMessageTyp.FileSaved:
                        // Put the event message on the status label and attach the context menu
                        UpdateStatusLabel(dateTime + " - " + eventArgs.Message, fileSavedStatusContextMenu);
                        // Change title
                        Text = eventArgs.Surface.LastSaveFullPath + " - " + Language.GetString(LangKey.editor_title);
                        break;
                    default:
                        // Put the event message on the status label
                        UpdateStatusLabel(dateTime + " - " + eventArgs.Message);
                        break;
                }
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
            if (EditorConfiguration.MatchSizeToCapture)
            {
                Size = GetOptimalWindowSize();
            }

            dimensionsLabel.Text = Surface.Image.Width + "x" + Surface.Image.Height;
            AlignCanvasPositionAfterResize();
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

            UpdateStatusLabel(Language.GetFormattedString(LangKey.editor_imagesaved, fullpath), fileSavedStatusContextMenu);
            Text = Path.GetFileName(fullpath) + " - " + Language.GetString(LangKey.editor_title);
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
            }
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

            DestinationHelper.ExportCapture(true, destinationDesignation, _surface, _surface.CaptureDetails);
        }

        private void BtnClipboardClick(object sender, EventArgs e)
        {
            DestinationHelper.ExportCapture(true, WellKnownDestinations.Clipboard, _surface, _surface.CaptureDetails);
        }

        private void BtnPrintClick(object sender, EventArgs e)
        {
            // The BeginInvoke is a solution for the printdialog not having focus
            BeginInvoke((MethodInvoker) delegate { DestinationHelper.ExportCapture(true, WellKnownDestinations.Printer, _surface, _surface.CaptureDetails); });
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
                foreach (GreenshotToolStripButton butt in _toolbarButtons)
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
            HelpFileLoader.LoadHelp();
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
                // Dissallow "CANCEL" if the application needs to shutdown
                if (e.CloseReason == CloseReason.ApplicationExitCall || e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing)
                {
                    buttons = MessageBoxButtons.YesNo;
                }

                DialogResult result = MessageBox.Show(Language.GetString(LangKey.editor_close_on_save), Language.GetString(LangKey.editor_close_on_save_title), buttons,
                    MessageBoxIcon.Question);
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
            EditorConfiguration.SetEditorPlacement(new WindowDetails(Handle).WindowPlacement);
            IniConfig.Save();

            // remove from the editor list
            EditorList.Remove(this);

            _surface.Dispose();

            GC.Collect();
            if (coreConfiguration.MinimizeWorkingSetSize)
            {
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

                    if (!destination.IsActive)
                    {
                        continue;
                    }

                    if (destination.EditorShortcutKeys == keys)
                    {
                        destination.ExportCapture(true, _surface, _surface.CaptureDetails);
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
            string undoAction = string.Empty;
            if (canUndo)
            {
                if (_surface.UndoActionLanguageKey != LangKey.none)
                {
                    undoAction = Language.GetString(_surface.UndoActionLanguageKey);
                }
            }

            string undoText = Language.GetFormattedString(LangKey.editor_undo, undoAction);
            btnUndo.Text = undoText;
            undoToolStripMenuItem.Text = undoText;

            bool canRedo = _surface.CanRedo;
            btnRedo.Enabled = canRedo;
            redoToolStripMenuItem.Enabled = canRedo;
            string redoAction = string.Empty;
            if (canRedo)
            {
                if (_surface.RedoActionLanguageKey != LangKey.none)
                {
                    redoAction = Language.GetString(_surface.RedoActionLanguageKey);
                }
            }

            string redoText = Language.GetFormattedString(LangKey.editor_redo, redoAction);
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
            bool hasClipboard = ClipboardHelper.ContainsFormat(SupportedClipboardFormats) || ClipboardHelper.ContainsImage();
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
            new BidirectionalBinding(counterUpDown, "Value", _surface, "CounterStart", DecimalIntConverter.GetInstance(), NotNullValidator.GetInstance());
        }

        /// <summary>
        /// shows/hides field controls (2nd toolbar on top) depending on fields of selected elements
        /// </summary>
        private void RefreshFieldControls()
        {
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
                fontFamilyComboBox.Visible = props.HasFieldValue(FieldType.FONT_FAMILY);
                fontSizeLabel.Visible = fontSizeUpDown.Visible = props.HasFieldValue(FieldType.FONT_SIZE);
                fontBoldButton.Visible = props.HasFieldValue(FieldType.FONT_BOLD);
                fontItalicButton.Visible = props.HasFieldValue(FieldType.FONT_ITALIC);
                textHorizontalAlignmentButton.Visible = props.HasFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT);
                textVerticalAlignmentButton.Visible = props.HasFieldValue(FieldType.TEXT_VERTICAL_ALIGNMENT);
                shadowButton.Visible = props.HasFieldValue(FieldType.SHADOW);
                counterLabel.Visible = counterUpDown.Visible = props.HasFieldValue(FieldType.FLAGS) && ((FieldFlag)props.GetFieldValue(FieldType.FLAGS)).HasFlag(FieldFlag.COUNTER);

                btnConfirm.Visible = btnCancel.Visible = props.HasFieldValue(FieldType.FLAGS) && ((FieldFlag) props.GetFieldValue(FieldType.FLAGS)).HasFlag(FieldFlag.CONFIRMABLE);
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
            int stepLabels = _surface.CountStepLabels(null);
            Image icon;
            if (stepLabels <= 20)
            {
                icon = (Image) resources.GetObject($"btnStepLabel{stepLabels:00}.Image");
            }
            else
            {
                icon = (Image) resources.GetObject("btnStepLabel20+.Image");
            }

            btnStepLabel.Image = icon;
            addCounterToolStripMenuItem.Image = icon;

            FieldAggregator props = (FieldAggregator)_surface.FieldAggregator;
            // if a confirmable element is selected, we must disable most of the controls
            // since we demand confirmation or cancel for confirmable element
            if (props.HasFieldValue(FieldType.FLAGS) && ((FieldFlag) props.GetFieldValue(FieldType.FLAGS) & FieldFlag.CONFIRMABLE) == FieldFlag.CONFIRMABLE)
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


        private void ArrowHeadsToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.FieldAggregator.GetField(FieldType.ARROWHEADS).Value = (ArrowContainer.ArrowHeadCombination) ((ToolStripMenuItem) sender).Tag;
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

                clickedDestination = (IDestination) clickedControl.Tag;
            }
            else
            {
                if (sender is ToolStripMenuItem item)
                {
                    ToolStripMenuItem clickedMenuItem = item;
                    clickedDestination = (IDestination) clickedMenuItem.Tag;
                }
            }

            ExportInformation exportInformation = clickedDestination?.ExportCapture(true, _surface, _surface.CaptureDetails);
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
                    this.statusLabel.Text = Language.GetString(LangKey.editor_autocrop_not_possible);
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
            ToolStripMenuItem captureWindowMenuItem = (ToolStripMenuItem) sender;
            var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>();
            mainForm.AddCaptureWindowMenuItems(captureWindowMenuItem, Contextmenu_window_Click);
        }

        private void Contextmenu_window_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem) sender;
            try
            {
                WindowDetails windowToCapture = (WindowDetails) clickedItem.Tag;
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
                    capture = captureHelper.CaptureWindow(windowToCapture, capture, coreConfiguration.WindowCaptureMode);
                    if (capture?.CaptureDetails != null && capture.Image != null)
                    {
                        ((Bitmap) capture.Image).SetResolution(capture.CaptureDetails.DpiX, capture.CaptureDetails.DpiY);
                        _surface.AddImageContainer((Bitmap) capture.Image, 100, 100);
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

        private void AddBorderToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.ApplyBitmapEffect(new BorderEffect());
            UpdateUndoRedoSurfaceDependencies();
        }

        /// <summary>
        /// Added for FEATURE-919, increasing the canvas by 25 pixels in every direction.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnlargeCanvasToolStripMenuItemClick(object sender, EventArgs e)
        {
            _surface.ApplyBitmapEffect(new ResizeCanvasEffect(25, 25, 25, 25));
            UpdateUndoRedoSurfaceDependencies();
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
                    var result = new DropShadowSettingsForm(dropShadowEffect).ShowDialog(this);
                    apply = result == DialogResult.OK;
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
        /// Open the resize settings from, and resize if ok was pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnResizeClick(object sender, EventArgs e)
        {
            var resizeEffect = new ResizeEffect(_surface.Image.Width, _surface.Image.Height, true);
            var result = new ResizeSettingsForm(resizeEffect).ShowDialog(this);
            if (result == DialogResult.OK)
            {
                _surface.ApplyBitmapEffect(resizeEffect);
                UpdateUndoRedoSurfaceDependencies();
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
                    var result = new TornEdgeSettingsForm(tornEdgeEffect).ShowDialog(this);
                    apply = result == DialogResult.OK;
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
            Panel panel = (Panel) canvas?.Parent;
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
            var senderMenuItem = (ToolStripMenuItem) sender;
            var nextValue = Fraction.Parse((string) senderMenuItem.Tag);

            ZoomSetValue(nextValue);
        }

        private void ZoomBestFitMenuItemClick(object sender, EventArgs e)
        {
            var maxWindowSize = GetAvailableScreenSpace();
            var chromeSize = GetChromeSize();
            var maxImageSize = maxWindowSize - chromeSize;
            var imageSize = Surface.Image.Size;

            static bool isFit(Fraction scale, int source, int boundary)
                => (int) (source * scale) <= boundary;

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
            zoomStatusDropDownBtn.Text = ((int) (100 * (double) value)).ToString() + "%";
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
                (int) (horizontalCenter * size.Width) - rc.Width / 2,
                (int) (verticalCenter * size.Height) - rc.Height / 2
            );
        }
    }
}