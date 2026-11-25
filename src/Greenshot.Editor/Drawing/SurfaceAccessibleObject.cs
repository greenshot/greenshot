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

using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Languages;
using Greenshot.Editor.Drawing.Emoji;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// The canvas for a screen reader: one child per element in z-order, named by its type and text, described by its size and position
    /// </summary>
    internal sealed class SurfaceAccessibleObject : Control.ControlAccessibleObject
    {
        private readonly Surface _surface;

        /// <summary>
        /// One accessible object per element, so a screen reader sees the same object every time it asks
        /// </summary>
        private readonly ConditionalWeakTable<IDrawableContainer, DrawableContainerAccessibleObject> _children = new ConditionalWeakTable<IDrawableContainer, DrawableContainerAccessibleObject>();

        public SurfaceAccessibleObject(Surface surface) : base(surface)
        {
            _surface = surface;
        }

        public override AccessibleRole Role => _surface.AccessibleRole != AccessibleRole.Default ? _surface.AccessibleRole : AccessibleRole.Client;

        public override string Name => _surface.AccessibleName ?? Texts.Editor.Canvas;

        public override string Description
        {
            get
            {
                if (_surface.AccessibleDescription != null || _surface.Image == null)
                {
                    return _surface.AccessibleDescription;
                }

                return string.Format(Texts.Editor.CanvasSize, _surface.Image.Width, _surface.Image.Height);
            }
        }

        public override int GetChildCount() => _surface.Elements.Count;

        public override AccessibleObject GetChild(int index)
        {
            var elements = _surface.Elements;
            return index >= 0 && index < elements.Count ? GetChild(elements[index]) : null;
        }

        /// <summary>
        /// The accessible object of an element on the canvas
        /// </summary>
        public DrawableContainerAccessibleObject GetChild(IDrawableContainer element)
            => _children.GetValue(element, key => new DrawableContainerAccessibleObject(this, _surface, key));

        public override AccessibleObject GetFocused()
        {
            if (!_surface.Focused)
            {
                return null;
            }

            var focusedElement = _surface.FocusedElement;
            return focusedElement != null ? GetChild(focusedElement) : this;
        }

        public override AccessibleObject GetSelected()
        {
            var selectedElements = _surface.SelectedElements;
            return selectedElements.Count > 0 ? GetChild(selectedElements[selectedElements.Count - 1]) : null;
        }

        public override AccessibleObject HitTest(int x, int y)
        {
            if (!_surface.IsHandleCreated)
            {
                return null;
            }

            var clientPoint = _surface.PointToClient(new Point(x, y));
            if (!_surface.ClientRectangle.Contains(clientPoint))
            {
                return null;
            }

            var imagePoint = _surface.ToImageCoordinates(clientPoint);
            var element = _surface.Elements.ClickableElementAt(imagePoint.X, imagePoint.Y);
            return element != null ? GetChild(element) : this;
        }

        /// <summary>
        /// The translated name of the type of the element, like "Rectangle"
        /// </summary>
        public static string GetTypeName(IDrawableContainer element) => element switch
        {
            SpeechbubbleContainer => Texts.Editor.ElementSpeechbubble,
            TextContainer => Texts.Editor.ElementText,
            RectangleContainer => Texts.Editor.ElementRectangle,
            EllipseContainer => Texts.Editor.ElementEllipse,
            ArrowContainer => Texts.Editor.ElementArrow,
            LineContainer => Texts.Editor.ElementLine,
            PolylineContainer => Texts.Editor.ElementPolyline,
            FreehandContainer => Texts.Editor.ElementFreehand,
            HighlightContainer => Texts.Editor.ElementHighlight,
            ObfuscateContainer => Texts.Editor.ElementObfuscate,
            StepLabelContainer => Texts.Editor.ElementCounter,
            EmojiContainer => Texts.Editor.ElementEmoji,
            CursorContainer => Texts.Editor.ElementCursor,
            IconContainer => Texts.Editor.ElementIcon,
            ImageContainer or VectorGraphicsContainer => Texts.Editor.ElementImage,
            CropContainer => Texts.Editor.ElementCrop,
            CutMarkContainer => Texts.Editor.ElementCutMark,
            TornEdgeContainer => Texts.Editor.TornEdge,
            _ => Texts.Editor.Object
        };
    }

    /// <summary>
    /// An element on the canvas for a screen reader, e.g. "Text: Hello", "120 × 40 at 10, 20"
    /// </summary>
    internal sealed class DrawableContainerAccessibleObject : AccessibleObject
    {
        private readonly SurfaceAccessibleObject _parent;
        private readonly Surface _surface;
        private readonly IDrawableContainer _element;

        public DrawableContainerAccessibleObject(SurfaceAccessibleObject parent, Surface surface, IDrawableContainer element)
        {
            _parent = parent;
            _surface = surface;
            _element = element;
        }

        public override AccessibleObject Parent => _parent;

        public override AccessibleRole Role => AccessibleRole.Graphic;

        public override string Name
        {
            get
            {
                string typeName = SurfaceAccessibleObject.GetTypeName(_element);
                string text = _element switch
                {
                    ITextContainer textContainer => textContainer.Text,
                    StepLabelContainer => _surface.CountStepLabels(_element).ToString(),
                    EmojiContainer emoji => emoji.Emoji,
                    _ => null
                };
                return string.IsNullOrWhiteSpace(text) ? typeName : string.Format(Texts.Editor.ElementWithText, typeName, text);
            }
        }

        /// <summary>
        /// Size and position in pixels of the image
        /// </summary>
        public override string Description
        {
            get
            {
                var bounds = _element.Bounds;
                return string.Format(Texts.Editor.ElementBounds, bounds.Width, bounds.Height, bounds.Left, bounds.Top);
            }
        }

        public override Rectangle Bounds
            => _surface.IsHandleCreated ? _surface.RectangleToScreen(_surface.ToSurfaceCoordinates(_element.Bounds)) : Rectangle.Empty;

        public override AccessibleStates State
        {
            get
            {
                var state = AccessibleStates.Selectable | AccessibleStates.Focusable;
                if (_element.Selected)
                {
                    state |= AccessibleStates.Selected;
                }

                if (_surface.Focused && ReferenceEquals(_surface.FocusedElement, _element))
                {
                    state |= AccessibleStates.Focused;
                }

                if (!_surface.Elements.Contains(_element) || !_surface.IsVisibleOnCanvas(_element))
                {
                    state |= AccessibleStates.Offscreen;
                }

                return state;
            }
        }

        public override AccessibleObject Navigate(AccessibleNavigation navigationDirection)
        {
            int index = _surface.Elements.IndexOf(_element);
            if (index < 0)
            {
                return null;
            }

            return navigationDirection switch
            {
                AccessibleNavigation.Next or AccessibleNavigation.Right or AccessibleNavigation.Down => _parent.GetChild(index + 1),
                AccessibleNavigation.Previous or AccessibleNavigation.Left or AccessibleNavigation.Up => _parent.GetChild(index - 1),
                _ => null
            };
        }

        public override void Select(AccessibleSelection flags)
        {
            if (!_surface.Elements.Contains(_element))
            {
                return;
            }

            if ((flags & AccessibleSelection.TakeFocus) != 0 && _surface.CanFocus)
            {
                _surface.Focus();
            }

            if ((flags & (AccessibleSelection.TakeSelection | AccessibleSelection.TakeFocus)) != 0)
            {
                _surface.DeselectAllElements();
                _surface.SelectElement(_element);
            }
            else if ((flags & AccessibleSelection.AddSelection) != 0)
            {
                _surface.SelectElement(_element);
            }
            else if ((flags & AccessibleSelection.RemoveSelection) != 0)
            {
                _surface.DeselectElement(_element);
            }
        }
    }
}
