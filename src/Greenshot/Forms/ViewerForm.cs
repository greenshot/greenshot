/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows.Forms;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;
using Greenshot.Destinations;
using log4net;

namespace Greenshot.Forms
{
    /// <summary>
    /// Simple image viewer form
    /// </summary>
    public partial class ViewerForm : BaseForm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ViewerForm));
        private static readonly ViewerConfiguration ViewerConfig = IniConfig.GetIniSection<ViewerConfiguration>();
        private static readonly List<ViewerForm> OpenViewers = new List<ViewerForm>();

        private readonly ISurface _surface;
        private readonly ICaptureDetails _captureDetails;
        private Image _displayImage;
        private float _zoomFactor = 1.0f;
        private Point _panOffset = Point.Empty;
        private Point _lastMousePosition;
        private bool _isPanning;
        private const int MinWindowSize = 50;

        public ViewerForm(ISurface surface, ICaptureDetails captureDetails)
        {
            _surface = surface;
            _captureDetails = captureDetails;
            
            InitializeComponent();
            
            // Set up the form
            Text = captureDetails.Title ?? "Image Viewer";
            KeyPreview = true;
            
            // Create display image with or without cursor
            UpdateDisplayImage();
            
            // Calculate and set initial window size
            CalculateAndSetSize();
            
            // Apply configuration
            TopMost = ViewerConfig.AlwaysOnTop;
            FormBorderStyle = ViewerConfig.ShowTitle ? FormBorderStyle.Sizable : FormBorderStyle.SizableToolWindow;
            
            // Show first usage message if needed
            if (!ViewerConfig.FirstUsageShown)
            {
                ViewerConfig.FirstUsageShown = true;
                IniConfig.Save();
                MessageBox.Show(
                    Language.GetString(LangKey.viewer_first_usage_message),
                    Language.GetString(LangKey.viewer_first_usage_title),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            
            // Add to open viewers list
            OpenViewers.Add(this);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(800, 600);
            Name = "ViewerForm";
            StartPosition = FormStartPosition.CenterScreen;
            
            ResumeLayout(false);
            
            // Set up events
            Paint += ViewerForm_Paint;
            KeyDown += ViewerForm_KeyDown;
            MouseDown += ViewerForm_MouseDown;
            MouseMove += ViewerForm_MouseMove;
            MouseUp += ViewerForm_MouseUp;
            MouseWheel += ViewerForm_MouseWheel;
            FormClosing += ViewerForm_FormClosing;
            ContextMenuStrip = CreateContextMenu();
        }

        private void UpdateDisplayImage()
        {
            _displayImage?.Dispose();
            
            if (ViewerConfig.ShowCursor && _captureDetails.CursorVisible)
            {
                // Create image with cursor
                _displayImage = _surface.GetImageForExport();
            }
            else
            {
                // Create image without cursor
                _displayImage = ImageHelper.Clone(_surface.Image);
            }
        }

        private void CalculateAndSetSize()
        {
            if (_displayImage == null) return;
            
            int maxWidth = ViewerConfig.MaxWidth;
            int maxHeight = ViewerConfig.MaxHeight;
            
            // Calculate size maintaining aspect ratio
            float aspectRatio = (float)_displayImage.Width / _displayImage.Height;
            int windowWidth = _displayImage.Width;
            int windowHeight = _displayImage.Height;
            
            if (windowWidth > maxWidth)
            {
                windowWidth = maxWidth;
                windowHeight = (int)(windowWidth / aspectRatio);
            }
            
            if (windowHeight > maxHeight)
            {
                windowHeight = maxHeight;
                windowWidth = (int)(windowHeight * aspectRatio);
            }
            
            // Ensure minimum size
            if (windowWidth < MinWindowSize) windowWidth = MinWindowSize;
            if (windowHeight < MinWindowSize) windowHeight = MinWindowSize;
            
            ClientSize = new Size(windowWidth, windowHeight);
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();
            
            // Process again
            var processAgainItem = new ToolStripMenuItem(Language.GetString(LangKey.viewer_process_again));
            processAgainItem.Click += ProcessAgainItem_Click;
            menu.Items.Add(processAgainItem);
            
            menu.Items.Add(new ToolStripSeparator());
            
            // Always on top
            var alwaysOnTopItem = new ToolStripMenuItem(Language.GetString(LangKey.viewer_alwaysontop))
            {
                Checked = ViewerConfig.AlwaysOnTop,
                CheckOnClick = true
            };
            alwaysOnTopItem.CheckedChanged += (s, e) =>
            {
                ViewerConfig.AlwaysOnTop = alwaysOnTopItem.Checked;
                TopMost = ViewerConfig.AlwaysOnTop;
                IniConfig.Save();
            };
            menu.Items.Add(alwaysOnTopItem);
            
            // Show/hide cursor
            var cursorItem = new ToolStripMenuItem(
                ViewerConfig.ShowCursor ? Language.GetString(LangKey.viewer_hide_cursor) : Language.GetString(LangKey.viewer_show_cursor));
            cursorItem.Click += (s, e) =>
            {
                ViewerConfig.ShowCursor = !ViewerConfig.ShowCursor;
                IniConfig.Save();
                UpdateDisplayImage();
                Invalidate();
            };
            menu.Items.Add(cursorItem);
            
            // Show/hide title
            var titleItem = new ToolStripMenuItem(
                ViewerConfig.ShowTitle ? Language.GetString(LangKey.viewer_hide_title) : Language.GetString(LangKey.viewer_show_title));
            titleItem.Click += (s, e) =>
            {
                ViewerConfig.ShowTitle = !ViewerConfig.ShowTitle;
                FormBorderStyle = ViewerConfig.ShowTitle ? FormBorderStyle.Sizable : FormBorderStyle.SizableToolWindow;
                IniConfig.Save();
            };
            menu.Items.Add(titleItem);
            
            menu.Items.Add(new ToolStripSeparator());
            
            // Reset zoom/pan
            var resetItem = new ToolStripMenuItem(Language.GetString(LangKey.viewer_reset_zoom));
            resetItem.Click += (s, e) => ResetZoomPan();
            menu.Items.Add(resetItem);
            
            menu.Items.Add(new ToolStripSeparator());
            
            // Save
            var saveItem = new ToolStripMenuItem(Language.GetString(LangKey.viewer_save));
            saveItem.Click += SaveItem_Click;
            menu.Items.Add(saveItem);
            
            // Save as
            var saveAsItem = new ToolStripMenuItem(Language.GetString(LangKey.viewer_saveas));
            saveAsItem.Click += SaveAsItem_Click;
            menu.Items.Add(saveAsItem);
            
            menu.Items.Add(new ToolStripSeparator());
            
            // Close all
            var closeAllItem = new ToolStripMenuItem(Language.GetString(LangKey.viewer_close_all));
            closeAllItem.Click += (s, e) => CloseAllViewers();
            menu.Items.Add(closeAllItem);
            
            // Exit
            var exitItem = new ToolStripMenuItem(Language.GetString("editor_close"));
            exitItem.Click += (s, e) => Close();
            menu.Items.Add(exitItem);
            
            return menu;
        }

        private void ProcessAgainItem_Click(object sender, EventArgs e)
        {
            try
            {
                var pickerDestination = DestinationHelper.GetDestination(nameof(WellKnownDestinations.Picker));
                if (pickerDestination != null)
                {
                    pickerDestination.ExportCapture(true, _surface, _captureDetails);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error processing image again", ex);
            }
        }

        private void SaveItem_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDestination = DestinationHelper.GetDestination(nameof(WellKnownDestinations.FileNoDialog));
                if (fileDestination != null)
                {
                    fileDestination.ExportCapture(true, _surface, _captureDetails);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error saving image", ex);
            }
        }

        private void SaveAsItem_Click(object sender, EventArgs e)
        {
            try
            {
                var fileDialogDestination = DestinationHelper.GetDestination(nameof(WellKnownDestinations.FileDialog));
                if (fileDialogDestination != null)
                {
                    fileDialogDestination.ExportCapture(true, _surface, _captureDetails);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error saving image as", ex);
            }
        }

        private void ResetZoomPan()
        {
            _zoomFactor = 1.0f;
            _panOffset = Point.Empty;
            CalculateAndSetSize();
            Invalidate();
        }

        private static void CloseAllViewers()
        {
            // Create a copy of the list to avoid modification during iteration
            var viewers = new List<ViewerForm>(OpenViewers);
            foreach (var viewer in viewers)
            {
                viewer.Close();
            }
        }

        private void ViewerForm_Paint(object sender, PaintEventArgs e)
        {
            if (_displayImage == null) return;
            
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            
            // Calculate the display rectangle
            int scaledWidth = (int)(_displayImage.Width * _zoomFactor);
            int scaledHeight = (int)(_displayImage.Height * _zoomFactor);
            
            int x = (ClientSize.Width - scaledWidth) / 2 + _panOffset.X;
            int y = (ClientSize.Height - scaledHeight) / 2 + _panOffset.Y;
            
            e.Graphics.DrawImage(_displayImage, x, y, scaledWidth, scaledHeight);
        }

        private void ViewerForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    break;
                    
                case Keys.Add:
                case Keys.Oemplus:
                    if (e.Control)
                    {
                        ZoomIn();
                        e.Handled = true;
                    }
                    break;
                    
                case Keys.Subtract:
                case Keys.OemMinus:
                    if (e.Control)
                    {
                        ZoomOut();
                        e.Handled = true;
                    }
                    break;
                    
                case Keys.D0:
                case Keys.NumPad0:
                    if (e.Control)
                    {
                        ResetZoomPan();
                        e.Handled = true;
                    }
                    break;
                    
                case Keys.Left:
                    _panOffset.X += 10;
                    Invalidate();
                    e.Handled = true;
                    break;
                    
                case Keys.Right:
                    _panOffset.X -= 10;
                    Invalidate();
                    e.Handled = true;
                    break;
                    
                case Keys.Up:
                    _panOffset.Y += 10;
                    Invalidate();
                    e.Handled = true;
                    break;
                    
                case Keys.Down:
                    _panOffset.Y -= 10;
                    Invalidate();
                    e.Handled = true;
                    break;
            }
        }

        private void ViewerForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _lastMousePosition = e.Location;
                Cursor = Cursors.SizeAll;
            }
        }

        private void ViewerForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                int dx = e.X - _lastMousePosition.X;
                int dy = e.Y - _lastMousePosition.Y;
                _panOffset.X += dx;
                _panOffset.Y += dy;
                _lastMousePosition = e.Location;
                Invalidate();
            }
        }

        private void ViewerForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
        }

        private void ViewerForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                ZoomIn();
            }
            else if (e.Delta < 0)
            {
                ZoomOut();
            }
        }

        private void ZoomIn()
        {
            _zoomFactor *= 1.1f;
            if (_zoomFactor > 10.0f) _zoomFactor = 10.0f;
            Invalidate();
        }

        private void ZoomOut()
        {
            _zoomFactor /= 1.1f;
            if (_zoomFactor < 0.1f) _zoomFactor = 0.1f;
            Invalidate();
        }

        private void ViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            OpenViewers.Remove(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _displayImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
