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
        /// The canvas keeps at least this size (at 96 DPI): the editor window can't get smaller than that, plus the panel area when it's open.
        /// A small window for a capture is the viewer's job.
        /// </summary>
        private const int MinimumCanvasWidth = 300;
        private const int MinimumCanvasHeight = 200;

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
                new StyleAndSizeSection(),
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
            // The tool bars change their size with the DPI and when they wrap: the minimum follows
            topToolStripContainer.ContentPanel.Resize += (_, _) => UpdateMinimumSize();
            UpdateMinimumSize();
        }

        /// <summary>
        /// A menu item for every panel which has a menu text, with its keys: it shows the panel with the keyboard
        /// </summary>
        private void AddPanelMenuItems(ToolStripMenuItem menu)
        {
            if (_panelHost == null)
            {
                return;
            }

            foreach (var panel in _panelHost.Panels)
            {
                if (string.IsNullOrEmpty(panel.MenuText))
                {
                    continue;
                }

                var item = new ToolStripMenuItem(panel.MenuText);
                var keys = ParseShortcut(panel.Shortcut);
                if (keys != Keys.None)
                {
                    item.ShortcutKeys = keys;
                }

                string panelId = panel.Id;
                item.Click += (_, _) => _panelHost.Show(panelId, activate: true);
                menu.DropDownItems.Add(item);
            }
        }

        /// <summary>
        /// "Ctrl+Shift+K" to keys, Keys.None for nothing or what isn't a valid shortcut
        /// </summary>
        internal static Keys ParseShortcut(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
            {
                return Keys.None;
            }

            try
            {
                var keys = (Keys)new KeysConverter().ConvertFromInvariantString(shortcut.Replace(" ", string.Empty));
                return ToolStripManager.IsValidShortcut(keys) ? keys : Keys.None;
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException or NotSupportedException or InvalidCastException)
            {
                Log.WarnFormat("The panel shortcut {0} is not valid", shortcut);
                return Keys.None;
            }
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
                UpdateMinimumSize();
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
        /// Shown when a panel is open. The window gets wider if the canvas would get less than its minimum width.
        /// </summary>
        private void UpdatePanelDockVisibility()
        {
            if (_panelDock == null)
            {
                return;
            }

            bool show = _panelHost.IsOpen;
            if (_panelDock.Visible == show)
            {
                return;
            }

            _panelSplitter.Visible = show;
            _panelDock.Visible = show;
            UpdateMinimumSize();
            AlignCanvasPositionAfterResize();
        }

        /// <summary>
        /// The smallest window: the tool bars and the frame, the minimum size of the canvas and the panel area when it's shown.
        /// A window which is smaller grows to it (WinForms does that when the minimum size is set).
        /// </summary>
        private void UpdateMinimumSize()
        {
            var contentPanel = topToolStripContainer.ContentPanel;
            int width = Width - contentPanel.ClientSize.Width + DpiCalculator.ScaleWithDpi(MinimumCanvasWidth, DeviceDpi);
            if (_panelDock?.Visible == true)
            {
                width += _panelDock.Width + _panelSplitter.Width;
            }

            int height = Height - contentPanel.ClientSize.Height + DpiCalculator.ScaleWithDpi(MinimumCanvasHeight, DeviceDpi);
            var minimumSize = new System.Drawing.Size(width, height);
            if (MinimumSize != minimumSize)
            {
                MinimumSize = minimumSize;
            }
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
