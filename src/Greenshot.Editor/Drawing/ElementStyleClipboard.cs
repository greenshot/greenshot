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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Memento;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Copy the style (line, fill, shadow, font, arrow heads, filter settings...) or the size of one element and apply it to other
    /// elements, also in another editor window. Kept until Greenshot ends, not in greenshot.ini. No UI here: the context menu,
    /// the keys and the Selection panel call it.
    /// </summary>
    public static class ElementStyleClipboard
    {
        // Not a style: what the element is (counter, confirmable) or how the crop works
        private static readonly IFieldType[] NotStyle = { FieldType.FLAGS, FieldType.CROPMODE };

        private static List<(IFieldType FieldType, object Value)> _style;
        private static Size? _size;

        /// <summary>
        /// A style or a size was copied: the "Apply" commands can change their state
        /// </summary>
        public static event EventHandler Changed;

        /// <summary>
        /// A style was copied
        /// </summary>
        public static bool HasStyle => _style != null;

        /// <summary>
        /// A size was copied
        /// </summary>
        public static bool HasSize => _size.HasValue;

        /// <summary>
        /// Style and size are copied from one element
        /// </summary>
        public static bool CanCopy(IDrawableContainerList selection) => selection?.Count == 1 && selection[0] is DrawableContainer;

        /// <summary>
        /// The copied style has a field one of the elements has
        /// </summary>
        public static bool CanApplyStyle(IEnumerable<IDrawableContainer> elements)
            => _style != null && elements != null && elements.OfType<DrawableContainer>().Any(container => _style.Any(entry => container.HasField(entry.FieldType)));

        /// <summary>
        /// A size was copied and there is an element to give it to
        /// </summary>
        public static bool CanApplySize(IEnumerable<IDrawableContainer> elements) => _size.HasValue && elements != null && elements.Any();

        /// <summary>
        /// Keep the style fields of the element, with their values
        /// </summary>
        public static bool CopyStyle(IDrawableContainer element)
        {
            if (element is not DrawableContainer container)
            {
                return false;
            }

            _style = container.GetFields()
                .Select(field => field.FieldType)
                .Where(fieldType => !NotStyle.Contains(fieldType))
                .Distinct()
                .Select(fieldType => (fieldType, container.GetField(fieldType).Value))
                .ToList();
            Changed?.Invoke(null, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Keep width and height of the element
        /// </summary>
        public static bool CopySize(IDrawableContainer element)
        {
            if (element == null)
            {
                return false;
            }

            _size = new Size(Math.Abs(element.Width), Math.Abs(element.Height));
            Changed?.Invoke(null, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Give the elements the copied style, each only the fields it has, as one undo step
        /// </summary>
        /// <returns>true when something changed</returns>
        public static bool ApplyStyle(ISurface surface, IEnumerable<IDrawableContainer> elements)
        {
            if (_style == null || surface == null || elements == null)
            {
                return false;
            }

            var changes = new List<(DrawableContainer Container, IField Field, object Value)>();
            foreach (var container in elements.OfType<DrawableContainer>())
            {
                foreach (var (fieldType, value) in _style)
                {
                    if (!container.HasField(fieldType))
                    {
                        continue;
                    }

                    var field = container.GetField(fieldType);
                    if (!Equals(field.Value, value))
                    {
                        changes.Add((container, field, value));
                    }
                }
            }

            if (changes.Count == 0)
            {
                return false;
            }

            surface.MakeUndoable(new ChangeFieldsMemento(changes.Select(change => ((IDrawableContainer)change.Container, change.Field))), false);
            var containers = changes.Select(change => change.Container).Distinct().ToList();
            containers.ForEach(container => container.Invalidate());
            foreach (var (_, field, value) in changes)
            {
                field.Value = value;
            }

            containers.ForEach(container => container.Invalidate());
            surface.Modified = true;
            // The properties tool bar shows the new values
            (surface.FieldAggregator as FieldAggregator)?.UpdateFromElements();
            return true;
        }

        /// <summary>
        /// Give the elements the copied width and height, as one undo step. Every element keeps its top left corner and the
        /// direction of a line; an element which keeps its aspect ratio (an emoji, an image) takes the width.
        /// </summary>
        /// <returns>true when something changed</returns>
        public static bool ApplySize(ISurface surface, IDrawableContainerList elements)
        {
            if (!_size.HasValue || surface == null || elements == null)
            {
                return false;
            }

            var size = _size.Value;
            var changes = elements
                .Select(element => (Element: element, Bounds: NewBounds(element, size)))
                .Where(change => change.Bounds.Width != change.Element.Width || change.Bounds.Height != change.Element.Height)
                .ToList();
            if (changes.Count == 0)
            {
                return false;
            }

            elements.MakeBoundsChangeUndoable(false);
            foreach (var (element, bounds) in changes)
            {
                element.Invalidate();
                element.ApplyBounds(bounds);
                element.Invalidate();
            }

            surface.Modified = true;
            return true;
        }

        private static NativeRectFloat NewBounds(IDrawableContainer element, Size size)
        {
            int width = size.Width;
            int height = size.Height;
            if (element is IHaveScaleOptions scalable && (scalable.GetScaleOptions() & ScaleOptions.Rational) == ScaleOptions.Rational && element.Width != 0)
            {
                height = (int)Math.Round((double)Math.Abs(element.Height) * width / Math.Abs(element.Width));
            }

            // A line or an arrow drawn to the left or up has a negative width or height: it keeps its direction
            return new NativeRectFloat(element.Left, element.Top, element.Width < 0 ? -width : width, element.Height < 0 ? -height : height);
        }

        /// <summary>
        /// For tests: nothing copied
        /// </summary>
        internal static void Clear()
        {
            _style = null;
            _size = null;
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }
}
