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
using System.Windows.Forms.Integration;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Wpf;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Controls;
using Greenshot.Editor.Panels.Views;
using log4net;

namespace Greenshot.Editor.Panels
{
    /// <summary>
    /// The panel area at the right of the canvas in the WinForms editor window: one ElementHost for all panels, so there is one
    /// place for the focus, the keys, the theme and the DPI. It shows what the <see cref="EditorPanelHost"/> decides.
    /// The panels are WPF views; in a WPF editor window the same views go into the window directly.
    /// </summary>
    internal sealed class EditorPanelDock : UserControl
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EditorPanelDock));
        private readonly EditorPanelHost _panelHost;
        private readonly IEditorPanelContext _context;
        private readonly IEditorConfiguration _configuration;
        private readonly ElementHost _elementHost;
        private readonly PanelDockView _view;
        private readonly Dictionary<string, (object ViewModel, object View)> _created = new();

        public EditorPanelDock(EditorPanelHost panelHost, IEditorPanelContext context, IEditorConfiguration configuration)
        {
            _panelHost = panelHost;
            _context = context;
            _configuration = configuration;
            _view = new PanelDockView();
            _view.CloseRequested += (_, _) => _panelHost.Close();
            _view.PanelPicked += (_, panelId) => _panelHost.Show(panelId, activate: true);
            _elementHost = new ElementHost
            {
                Dock = DockStyle.Fill,
                Child = _view
            };
            Controls.Add(_elementHost);
            ApplyTheme();
        }

        /// <summary>
        /// Show the active panel, its view model and view are created the first time
        /// </summary>
        public void ShowActivePanel()
        {
            var panel = _panelHost.ActivePanel;
            object view = null;
            if (panel != null)
            {
                if (!_created.TryGetValue(panel.Id, out var created))
                {
                    try
                    {
                        var viewModel = panel.CreateViewModel(_context);
                        created = (viewModel, panel.CreateView(viewModel));
                    }
                    catch (Exception ex)
                    {
                        // A broken panel of a plugin doesn't break the editor
                        Log.Error($"Can't create the editor panel {panel.Id}", ex);
                        created = (null, null);
                    }

                    _created[panel.Id] = created;
                }

                view = created.View;
            }

            _view.ShowPanel(_panelHost.AvailablePanels, panel, view);
        }

        /// <summary>
        /// Move the keyboard into the shown panel
        /// </summary>
        public void FocusContent()
        {
            _elementHost.Select();
            _view.FocusContent();
        }

        /// <summary>
        /// The colors of the editor theme setting, which can differ from Greenshot's theme
        /// </summary>
        public void ApplyTheme()
        {
            var theme = EditorTheme.Current;
            BackColor = theme.Background;
            var palette = ThemeManager.Instance.GetPalette(_configuration.EditorTheme);
            // The views of the panel area and the shared styles in them (e.g. Stamps) take their colors from it
            ThemeScope.SetTheme(_view, palette);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var created in _created.Values)
                {
                    (created.ViewModel as IDisposable)?.Dispose();
                }

                _created.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
