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
using System.Linq;
using Greenshot.Base.Interfaces.Editor;

namespace Greenshot.Editor.Panels
{
    /// <summary>
    /// Which panel the panel area of one editor window shows, and whether it is open. No UI: the dock control shows what this decides.
    /// The area opens by itself when a choice is needed: a section which asks for it starts to apply (e.g. the Emoji tool is picked).
    /// It never closes by itself, so it doesn't jump while the user clicks around; closed by the user it opens again at the next choice.
    /// When it switched from another panel for the choice, that panel comes back once no choice is needed any more.
    /// </summary>
    internal sealed class EditorPanelHost
    {
        private readonly IEditorPanelContext _context;
        private readonly IReadOnlyList<IEditorSection> _sections;
        private bool _automaticSectionApplied;
        // The panel the user had open before the Selection panel was shown for a choice, null when the user changed the area since
        private IEditorPanel _panelBeforeChoice;

        /// <param name="context">The context of the editor window</param>
        /// <param name="panels">All panels, the Selection panel included</param>
        /// <param name="sections">All sections of the Selection panel, to know when the area opens by itself</param>
        /// <param name="selectedPanelId">The panel which was shown last, from the settings</param>
        public EditorPanelHost(IEditorPanelContext context, IEnumerable<IEditorPanel> panels, IEnumerable<IEditorSection> sections, string selectedPanelId)
        {
            _context = context;
            Panels = panels.OrderBy(panel => panel.Order).ToList();
            _sections = sections.ToList();
            ActivePanel = Find(selectedPanelId) ?? Find(SelectionPanel.PanelId) ?? AvailablePanels.FirstOrDefault();
        }

        /// <summary>
        /// All panels, sorted
        /// </summary>
        public IReadOnlyList<IEditorPanel> Panels { get; }

        /// <summary>
        /// The panels which can be used now, sorted
        /// </summary>
        public IReadOnlyList<IEditorPanel> AvailablePanels => Panels.Where(IsAvailable).ToList();

        /// <summary>
        /// The panel which is shown when the area is open
        /// </summary>
        public IEditorPanel ActivePanel { get; private set; }

        /// <summary>
        /// The area is shown
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Open, closed or another panel. The bool is true when the panel should get the keyboard.
        /// </summary>
        public event EventHandler<bool> Changed;

        /// <summary>
        /// Show the panel, by the user or for an element (double click on an emoji)
        /// </summary>
        /// <param name="panelId">The panel</param>
        /// <param name="activate">The keyboard moves into it</param>
        /// <returns>false when there is no such panel or it can't be used now</returns>
        public bool Show(string panelId, bool activate)
        {
            var panel = Find(panelId);
            if (panel == null || !IsAvailable(panel))
            {
                return false;
            }

            _panelBeforeChoice = null;
            ActivePanel = panel;
            IsOpen = true;
            Changed?.Invoke(this, activate);
            return true;
        }

        /// <summary>
        /// The user closed the area
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _panelBeforeChoice = null;
            IsOpen = false;
            Changed?.Invoke(this, false);
        }

        /// <summary>
        /// Call when the selection, the tool or the shown capture changed: opens the Selection panel when a section which asks for it
        /// starts to apply (a choice is needed), and goes back to the panel it replaced when the choice isn't needed any more.
        /// Never takes the keyboard, never closes the area.
        /// </summary>
        public void UpdateAutomatic()
        {
            bool applies = _context.CurrentSurface != null && _sections.Any(section => section.ShowsPanel && Applies(section));
            bool started = applies && !_automaticSectionApplied;
            bool ended = !applies && _automaticSectionApplied;
            _automaticSectionApplied = applies;
            if (ended)
            {
                var before = _panelBeforeChoice;
                _panelBeforeChoice = null;
                if (before != null && IsOpen && IsAvailable(before))
                {
                    ActivePanel = before;
                    Changed?.Invoke(this, false);
                }

                return;
            }

            if (started)
            {
                // Only when it starts to apply: a user who closed the area, or went to another panel (e.g. Stamps), while the
                // emoji is selected keeps it like that
                var selection = Find(SelectionPanel.PanelId);
                if (selection == null)
                {
                    return;
                }

                if (!IsOpen || ActivePanel != selection)
                {
                    // Only a panel which was open comes back: a closed area stays open, see above
                    _panelBeforeChoice = IsOpen ? ActivePanel : null;
                    ActivePanel = selection;
                    IsOpen = true;
                    Changed?.Invoke(this, false);
                }
            }
        }

        private bool Applies(IEditorSection section)
        {
            try
            {
                return section.AppliesTo(_context);
            }
            catch (Exception)
            {
                // A broken section of a plugin doesn't break the editor
                return false;
            }
        }

        private bool IsAvailable(IEditorPanel panel)
        {
            try
            {
                return panel.IsAvailable(_context);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private IEditorPanel Find(string panelId) => string.IsNullOrEmpty(panelId) ? null : Panels.FirstOrDefault(panel => panel.Id == panelId);
    }
}
