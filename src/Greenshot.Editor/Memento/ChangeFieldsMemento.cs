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

using System.Collections.Generic;
using System.Linq;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Memento
{
    /// <summary>
    /// Undo and redo of a change of several fields of one or more elements as one step, e.g. Apply style
    /// </summary>
    public sealed class ChangeFieldsMemento : IMemento
    {
        private List<(IDrawableContainer Container, IField Field, object Value)> _states;

        /// <summary>
        /// Remember the values of the fields before they change
        /// </summary>
        public ChangeFieldsMemento(IEnumerable<(IDrawableContainer Container, IField Field)> fields)
        {
            _states = fields.Select(entry => (entry.Container, entry.Field, entry.Field.Value)).ToList();
        }

        public void Dispose()
        {
            _states = null;
        }

        /// <summary>
        /// Every apply is its own undo step
        /// </summary>
        public bool Merge(IMemento otherMemento) => false;

        public IMemento Restore()
        {
            var redo = new ChangeFieldsMemento(_states.Select(state => (state.Container, state.Field)));
            foreach (var container in _states.Select(state => state.Container).Distinct())
            {
                container.Invalidate();
            }

            foreach (var (_, field, value) in _states)
            {
                field.Value = value;
            }

            foreach (var container in _states.Select(state => state.Container).Distinct())
            {
                container.Invalidate();
            }

            return redo;
        }
    }
}
