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

using System.Linq;
using Greenshot.Document;

namespace Greenshot.WpfEditor.ViewModels
{
    /// <summary>
    /// A layer in the layers list: shown or hidden, locked or not
    /// </summary>
    public sealed class LayerViewModel : ObservableObject
    {
        private readonly DocumentTabViewModel _tab;

        public LayerViewModel(DocumentTabViewModel tab, Layer layer)
        {
            _tab = tab;
            Layer = layer;
        }

        public Layer Layer { get; }

        public string Name => Layer.Name;

        public int Count => Layer.Elements.Count;

        public bool Visible
        {
            get => Layer.Visible;
            set => Change(layer => layer with { Visible = value });
        }

        public bool Locked
        {
            get => Layer.Locked;
            set => Change(layer => layer with { Locked = value });
        }

        private void Change(System.Func<Layer, Layer> change)
        {
            var changed = change(Layer);
            _tab.ChangeDocument(document => document with
            {
                Layers = document.Layers.Select(layer => layer.Id == Layer.Id ? change(layer) : layer).ToList()
            }, EditorTexts.Wpf.UndoLayer);
            if (!changed.Visible || changed.Locked)
            {
                _tab.Select(_tab.Selection.Where(id => !Layer.Elements.Any(element => element.Id == id)));
            }
        }

        public override string ToString() => Name;
    }
}
