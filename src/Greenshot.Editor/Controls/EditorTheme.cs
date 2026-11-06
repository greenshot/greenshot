/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using Greenshot.Editor.Configuration;
using WpfBrush = System.Windows.Media.SolidColorBrush;

namespace Greenshot.Editor.Controls
{
    /// <summary>
    /// The colors of the editor window (toolbars, menus, tab strip, the area around the canvas), from the editor's own theme setting.
    /// Light and high contrast use the Windows system colors, as the editor always did; dark uses the dark palette of the WPF windows.
    /// The image and the elements on it are never themed.
    /// </summary>
    public sealed class EditorTheme
    {
        private EditorTheme()
        {
        }

        private static readonly IEditorConfiguration EditorConfiguration = IniConfigHelper.EnsureSection<IEditorConfiguration>(() => new EditorConfigurationImpl());
        private static EditorTheme _current;
        private static bool _listening;

        /// <summary>
        /// Raised on the UI thread when the colors of the editor changed: the editor setting, Greenshot's theme or the Windows mode
        /// </summary>
        public static event EventHandler Changed;

        /// <summary>
        /// The colors for the editor theme setting (Editor.EditorTheme, default light), cached: it's read on every paint
        /// </summary>
        public static EditorTheme Current
        {
            get
            {
                if (_current == null)
                {
                    StartListening();
                    _current = Create();
                }

                return _current;
            }
        }

        private static EditorTheme Create()
        {
            var palette = ThemeManager.Instance.GetPalette(EditorConfiguration.EditorTheme);
            return palette.IsDark && !palette.IsHighContrast ? FromPalette(palette) : System;
        }

        private static void StartListening()
        {
            if (_listening)
            {
                return;
            }

            _listening = true;
            // Theme is raised on every refresh (also a Windows mode change with Greenshot on light), CurrentPalette when its colors change
            ThemeManager.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(ThemeManager.Theme) or nameof(ThemeManager.CurrentPalette))
                {
                    Refresh();
                }
            };
            if (EditorConfiguration is INotifyPropertyChanged notifyingConfiguration)
            {
                notifyingConfiguration.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(IEditorConfiguration.EditorTheme))
                    {
                        Refresh();
                    }
                };
            }
        }

        /// <summary>
        /// Create the colors again, Changed is raised when they differ
        /// </summary>
        public static void Refresh()
        {
            var updated = Create();
            if (_current != null && _current.IsDark == updated.IsDark && _current.Background == updated.Background && _current.Highlight == updated.Highlight)
            {
                return;
            }

            _current = updated;
            Changed?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// True when the editor uses the dark colors
        /// </summary>
        public bool IsDark { get; private set; }

        /// <summary>Toolbars, menu bar, status bar, tab strip</summary>
        public Color Background { get; private set; }

        /// <summary>Texts and glyphs</summary>
        public Color Foreground { get; private set; }

        /// <summary>Disabled texts and secondary information</summary>
        public Color Muted { get; private set; }

        /// <summary>Opened menus</summary>
        public Color MenuBackground { get; private set; }

        /// <summary>Borders and separators</summary>
        public Color Border { get; private set; }

        /// <summary>An item under the mouse</summary>
        public Color Hover { get; private set; }

        /// <summary>A pressed or checked item</summary>
        public Color Pressed { get; private set; }

        /// <summary>The selected tab and text boxes</summary>
        public Color Window { get; private set; }

        /// <summary>Text on the selected tab and in text boxes</summary>
        public Color WindowText { get; private set; }

        /// <summary>Highlights, e.g. the drop place of a tab</summary>
        public Color Highlight { get; private set; }

        /// <summary>An error message in the status bar</summary>
        public Color Error { get; private set; }

        /// <summary>A tab or the new tab button under the mouse</summary>
        public Color HotTab { get; private set; }

        /// <summary>The close button of a tab under the mouse</summary>
        public Color HotClose { get; private set; }

        /// <summary>
        /// The area around the canvas
        /// </summary>
        public Color CanvasSurround { get; private set; }

        private static readonly EditorTheme System = new()
        {
            IsDark = false,
            Background = SystemColors.Control,
            Foreground = SystemColors.ControlText,
            Muted = SystemColors.GrayText,
            MenuBackground = SystemColors.Control,
            Border = SystemColors.ControlDark,
            Hover = SystemColors.GradientInactiveCaption,
            Pressed = SystemColors.GradientActiveCaption,
            Window = SystemColors.Window,
            WindowText = SystemColors.WindowText,
            Highlight = SystemColors.Highlight,
            Error = Color.DarkRed,
            HotTab = SystemColors.ControlLightLight,
            HotClose = SystemColors.ControlDark,
            CanvasSurround = SystemColors.Control
        };

        private static EditorTheme FromPalette(ThemePalette palette) => new()
        {
            IsDark = true,
            Background = ToColor(palette.BackgroundBrush),
            Foreground = ToColor(palette.ForegroundBrush),
            Muted = ToColor(palette.MutedBrush),
            MenuBackground = ToColor(palette.CardBackground),
            Border = ToColor(palette.BorderBrush),
            Hover = ToColor(palette.ButtonHoverBrush),
            Pressed = ToColor(palette.ButtonPressedBrush),
            Window = ToColor(palette.ControlBackgroundBrush),
            WindowText = ToColor(palette.ForegroundBrush),
            Highlight = ToColor(palette.AccentBrush),
            Error = ToColor(palette.ErrorBrush),
            HotTab = ToColor(palette.ButtonHoverBrush),
            HotClose = ToColor(palette.ButtonPressedBrush),
            CanvasSurround = ToColor(palette.TitleBarBrush)
        };

        /// <summary>
        /// The menu bar, the status bar and context menus use the system renderer in light and high contrast (as before),
        /// the themed renderer in dark
        /// </summary>
        /// <param name="toolStrip">ToolStrip</param>
        public static void ApplyRenderer(ToolStrip toolStrip)
        {
            if (Current.IsDark)
            {
                if (toolStrip.Renderer is not CustomToolStripProfessionalRenderer)
                {
                    toolStrip.Renderer = new CustomToolStripProfessionalRenderer();
                }
            }
            else if (toolStrip.RenderMode != ToolStripRenderMode.ManagerRenderMode)
            {
                toolStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            }
        }

        private static Color ToColor(WpfBrush brush)
            => Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
    }
}
