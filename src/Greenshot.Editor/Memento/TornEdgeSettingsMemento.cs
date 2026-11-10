/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Memento
{
    /// <summary>
    /// Undo and redo of a change of the settings of the torn edges, e.g. in the Effects panel
    /// </summary>
    public sealed class TornEdgeSettingsMemento : IMemento
    {
        private TornEdgeContainer _tornEdges;
        private TornEdgeEffect _settings;

        /// <summary>
        /// Remember the settings of the torn edges before they change
        /// </summary>
        public TornEdgeSettingsMemento(TornEdgeContainer tornEdges)
        {
            _tornEdges = tornEdges;
            _settings = tornEdges.GetSettings();
        }

        public void Dispose()
        {
            _tornEdges = null;
            _settings = null;
        }

        /// <summary>
        /// Every change is its own undo step
        /// </summary>
        public bool Merge(IMemento otherMemento) => false;

        public IMemento Restore()
        {
            var redo = new TornEdgeSettingsMemento(_tornEdges);
            _tornEdges.ApplySettings(_settings);
            _tornEdges.SetSeed(_settings.Seed);
            _tornEdges.Parent?.Invalidate();
            return redo;
        }
    }
}
