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

using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Plugin.Zxing.Drawing
{
    /// <summary>
    /// Undo and redo of a change of the content or look of a barcode, e.g. in the Barcode section of the Selection panel.
    /// Changes of the same field one after the other (typing) are one step.
    /// </summary>
    public sealed class BarcodeChangeMemento : IMemento
    {
        private BarcodeContainer _container;
        private ZxingModel _state;
        private readonly string _field;

        /// <summary>
        /// Remember the barcode before it changes
        /// </summary>
        /// <param name="container">the barcode</param>
        /// <param name="field">the name of the field which changes, changes of the same field are merged; null: never merged</param>
        public BarcodeChangeMemento(BarcodeContainer container, string field)
        {
            _container = container;
            _state = container.Model.Clone();
            _field = field;
        }

        public void Dispose()
        {
            _container = null;
            _state = null;
        }

        /// <summary>
        /// The next change of the same field of the same barcode is part of this step: the state before stays this one's
        /// </summary>
        public bool Merge(IMemento otherMemento)
            => _field != null && otherMemento is BarcodeChangeMemento other && ReferenceEquals(other._container, _container) && other._field == _field;

        public IMemento Restore()
        {
            var redo = new BarcodeChangeMemento(_container, null);
            _container.Invalidate();
            // The same model stays the one of the barcode, the Barcode section keeps showing it
            _container.Model.CopyFrom(_state);
            _container.Margin = _state.Margin;
            _container.Invalidate();
            return redo;
        }
    }
}
