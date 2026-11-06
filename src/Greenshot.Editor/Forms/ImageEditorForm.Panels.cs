/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Collections.Generic;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Dapplo.Windows.Dpi;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Editor.Controls;
using Greenshot.Editor.Panels;
using Greenshot.Editor.Panels.Sections;

namespace Greenshot.Editor.Forms
{
    /// <summary>
    /// The panel area at the right of the canvas: created the first time a panel is shown, nothing is built when the editor opens
    /// </summary>
    public partial class ImageEditorForm
    {
        /// <summary>
        /// The canvas keeps at least this width (at 96 DPI), the panel area is hidden before the canvas gets narrower
        /// </summary>
        private const int MinimumCanvasWidth = 300;

        private EditorPanelContext _panelContext;
        private EditorPanelHost _panelHost;
        private EditorPanelDock _panelDock;
        private Splitter _panelSplitter;

        /// <summary>
        /// The panels and sections of the core and of the plugins, only the context and the logic: no UI yet
        /// </summary>
        private void InitializePanels()
        {
            var sections = new List<IEditorSection>
            {
                new SizeAndPositionSection(),
                new EmojiSection(EditorConfiguration)
            };
            var panels = new List<IEditorPanel>();
            try
            {
                sections.AddRange(SimpleServiceProvider.Current.GetAllInstances<IEditorSection>());
                panels.AddRange(SimpleServiceProvider.Current.GetAllInstances<IEditorPanel>());
            }
            catch (Exception ex)
            {
                Log.Error("Error getting the editor panels of the plugins", ex);
            }

            panels.Insert(0, new SelectionPanel(sections, EditorConfiguration));
            _panelContext = new EditorPanelContext(this, (panelId, activate) => _panelHost.Show(panelId, activate), () => FocusCanvas(fromKeyboard: false));
            _panelHost = new EditorPanelHost(_panelContext, panels, sections, EditorConfiguration.SelectedEditorPanel);
            _panelHost.Changed += (_, activate) => OnPanelHostChanged(activate);
            topToolStripContainer.ContentPanel.Resize += (_, _) => UpdatePanelDockVisibility();
        }

        /// <summary>
        /// The panel area keyboard focus: the keys stay there like in a text field
        /// </summary>
        private bool PanelDockHasFocus => _panelDock?.ContainsFocus == true;

        private void OnPanelHostChanged(bool activate)
        {
            if (_panelHost.IsOpen)
            {
                EnsurePanelDock();
                _panelDock.ShowActivePanel();
                if (_panelHost.ActivePanel != null)
                {
                    EditorConfiguration.SelectedEditorPanel = _panelHost.ActivePanel.Id;
                }
            }

            bool hadFocus = PanelDockHasFocus;
            UpdatePanelDockVisibility();
            if (activate && _panelDock?.Visible == true)
            {
                _panelDock.FocusContent();
            }
            else if (hadFocus && _panelDock?.Visible != true)
            {
                FocusCanvas(fromKeyboard: true);
            }
        }

        /// <summary>
        /// The panel area and its splitter, right of the tabs and the canvas: they dock first, so they get the whole height
        /// </summary>
        private void EnsurePanelDock()
        {
            if (_panelDock != null)
            {
                return;
            }

            var contentPanel = topToolStripContainer.ContentPanel;
            contentPanel.SuspendLayout();
            _panelSplitter = new Splitter
            {
                Dock = DockStyle.Right,
                Visible = false,
                TabStop = false
            };
            _panelDock = new EditorPanelDock(_panelHost, _panelContext, EditorConfiguration)
            {
                Dock = DockStyle.Right,
                Visible = false
            };
            // Docking goes from the last control to the first: panel, splitter, tab strip, canvas
            contentPanel.Controls.Add(_panelSplitter);
            contentPanel.Controls.Add(_panelDock);
            _panelSplitter.SplitterMoved += (_, _) =>
            {
                EditorConfiguration.EditorPanelWidth = DpiCalculator.UnscaleWithDpi(_panelDock.Width, DeviceDpi);
                AlignCanvasPositionAfterResize();
            };
            ApplyPanelDockSize();
            ApplyPanelDockTheme();
            contentPanel.ResumeLayout(true);
        }

        /// <summary>
        /// Width of the panel area and of the splitter for the DPI of the window
        /// </summary>
        private void ApplyPanelDockSize()
        {
            if (_panelDock == null)
            {
                return;
            }

            _panelDock.Width = DpiCalculator.ScaleWithDpi(EditorConfiguration.EditorPanelWidth, DeviceDpi);
            _panelSplitter.Width = DpiCalculator.ScaleWithDpi(4, DeviceDpi);
            _panelSplitter.MinSize = DpiCalculator.ScaleWithDpi(200, DeviceDpi);
            _panelSplitter.MinExtra = DpiCalculator.ScaleWithDpi(MinimumCanvasWidth, DeviceDpi);
        }

        private void ApplyPanelDockTheme()
        {
            if (_panelDock == null)
            {
                return;
            }

            _panelSplitter.BackColor = EditorTheme.Current.Border;
            _panelDock.ApplyTheme();
        }

        /// <summary>
        /// Shown when a panel is open and the canvas keeps enough room
        /// </summary>
        private void UpdatePanelDockVisibility()
        {
            if (_panelDock == null)
            {
                return;
            }

            int available = topToolStripContainer.ContentPanel.ClientSize.Width;
            bool show = _panelHost.IsOpen && available - _panelDock.Width >= DpiCalculator.ScaleWithDpi(MinimumCanvasWidth, DeviceDpi);
            if (_panelDock.Visible == show)
            {
                return;
            }

            _panelSplitter.Visible = show;
            _panelDock.Visible = show;
            AlignCanvasPositionAfterResize();
        }

        /// <summary>
        /// The panels follow the capture which is shown
        /// </summary>
        private void ShowSurfaceInPanels()
        {
            _panelContext?.SetSurface(_surface);
            _panelHost?.UpdateAutomatic();
        }

        private void SurfaceElementsChanged(object sender, EventArgs e) => _panelContext?.OnElementsChanged();

        /// <summary>
        /// An element asked for its properties (a double click on an emoji): show the Selection panel with the keyboard
        /// </summary>
        private void SurfacePropertiesRequested(object sender, EventArgs e) => _panelHost?.Show(SelectionPanel.PanelId, activate: true);
    }
}
