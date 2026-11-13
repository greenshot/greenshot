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
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.Editor.Standalone.Editing;

namespace Greenshot.Editor.Standalone.ViewModels
{
    /// <summary>
    /// What the menus, the keys and the panels do with the document of the tab
    /// </summary>
    public sealed partial class DocumentTabViewModel
    {
        // A pasted or duplicated element is moved a bit, so it doesn't hide the original
        private const double PasteOffset = 10;

        public void SelectAll()
        {
            _tool.Cancel();
            Select(HitTesting.Selectable(_committed).Select(element => element.Id));
        }

        public void DeleteSelection()
        {
            if (!HasSelection)
            {
                return;
            }

            var ids = DocumentEdits.WithSets(_committed, _selection);
            Commit(DocumentEdits.Remove(_committed, ids), EditorTexts.Text.UndoDelete);
            Select(Array.Empty<Guid>());
        }

        /// <summary>
        /// Copies the selected elements (and the images they show) for a paste in any tab
        /// </summary>
        public bool CopySelection()
        {
            var elements = SelectedElements;
            if (elements.Count == 0)
            {
                return false;
            }

            var frames = _committed.Layers.SelectMany(layer => layer.Elements).OfType<FrameElement>().Where(frame => frame.Target is Guid target && _selection.Contains(target) && !_selection.Contains(frame.Id));
            var copied = elements.Concat(frames).ToList();
            var resources = copied.SelectMany(_committed.Links).Distinct().Select(id => _committed.Resources.TryGetValue(id, out var resource) ? resource : null).Where(resource => resource != null).ToList();
            Session.CopiedElements = (copied, resources);
            return true;
        }

        public void CutSelection()
        {
            if (CopySelection())
            {
                var ids = DocumentEdits.WithSets(_committed, _selection);
                Commit(DocumentEdits.Remove(_committed, ids), EditorTexts.Text.UndoCut);
                Select(Array.Empty<Guid>());
            }
        }

        public bool CanPasteElements => Session.CopiedElements != null;

        public void PasteElements()
        {
            if (Session.CopiedElements is not var (elements, resources))
            {
                return;
            }

            // Pasted into the document they were copied from: next to the originals
            bool sameDocument = elements.Any(element => DocumentEdits.Find(_committed, element.Id) != null);
            Insert(elements, resources, sameDocument ? PasteOffset : 0, EditorTexts.Text.UndoPaste);
        }

        public void DuplicateSelection()
        {
            var elements = SelectedElements;
            if (elements.Count > 0)
            {
                Insert(elements, Array.Empty<Resource>(), PasteOffset, EditorTexts.Text.UndoDuplicate);
            }
        }

        /// <summary>
        /// Adds copies of the elements with new ids; frames follow their copied target, links to elements which weren't copied are dropped
        /// </summary>
        private void Insert(IReadOnlyList<Element> elements, IReadOnlyList<Resource> resources, double offset, string description)
        {
            var ids = elements.ToDictionary(element => element.Id, _ => Uuid7.CreateVersion7());
            var copies = new List<Element>();
            foreach (var element in elements)
            {
                var copy = ElementShapes.Unanchored(element) with { Id = ids[element.Id] };
                if (copy is FrameElement frame)
                {
                    if (frame.Target is not Guid target || !ids.TryGetValue(target, out var newTarget))
                    {
                        continue;
                    }

                    copy = frame with { Target = newTarget };
                }

                copies.Add(offset == 0 ? copy : Anchors.Move(copy, offset, offset));
            }

            var missing = resources.Where(resource => !_committed.Resources.ContainsKey(resource.Id));
            Commit(DocumentEdits.Add(_committed, copies, missing), description);
            Select(copies.Where(copy => copy is not FrameElement).Select(copy => copy.Id));
        }

        /// <summary>
        /// Adds an image (pasted, dropped or opened) as element in the middle of the area which is visible
        /// </summary>
        public void AddImage(Resource resource, int width, int height, PointD center)
        {
            var image = new ImageElement(Uuid7.CreateVersion7(), new RectD(Math.Round(center.X - width / 2.0), Math.Round(center.Y - height / 2.0), width, height), resource.Id);
            Commit(DocumentEdits.Add(_committed, new[] { image }, new[] { resource }), EditorTexts.Text.UndoAdd);
            ToolKind = Tools.ToolKind.Select;
            Select(new[] { image.Id });
        }

        public void Arrange(Arrangement arrangement)
        {
            if (HasSelection)
            {
                Commit(DocumentEdits.Arrange(_committed, _selection.ToList(), arrangement), EditorTexts.Text.UndoArrange);
            }
        }

        /// <summary>
        /// Turns the whole document by 90, 180 or 270 degrees
        /// </summary>
        public void Turn(int degrees)
        {
            _tool.Cancel();
            Commit(CaptureTurn.Apply(_committed, degrees, keepTextUpright: true), EditorTexts.Text.UndoTurn);
        }

        /// <summary>
        /// Scales the canvas and all elements to the size
        /// </summary>
        public void Resize(int width, int height)
        {
            var area = _committed.CanvasArea();
            if (width <= 0 || height <= 0 || area.Width <= 0 || area.Height <= 0)
            {
                return;
            }

            _tool.Cancel();
            Commit(DocumentEdits.Scale(_committed, width / area.Width, height / area.Height), EditorTexts.Text.UndoResize);
        }

        /// <summary>
        /// Crops away the border of one color around the content
        /// </summary>
        public bool AutoCrop()
        {
            if (AutoCropping.ContentArea(_committed) is not RectD content)
            {
                return false;
            }

            _tool.Cancel();
            Commit(DocumentEdits.Crop(_committed, content), EditorTexts.Text.UndoCrop);
            return true;
        }

        /// <summary>
        /// Changes the selected elements, e.g. their style; with a merge key the changes in a row are one undo step
        /// </summary>
        public void ChangeSelected(Func<Element, Element> change, string description, string mergeKey = null)
        {
            var elements = SelectedElements;
            if (elements.Count == 0)
            {
                return;
            }

            var changed = elements.Select(change).Where(element => element != null).ToList();
            Commit(DocumentEdits.Replace(_committed, changed), description, mergeKey);
        }

        /// <summary>
        /// Changes the whole document, e.g. its canvas or a layer
        /// </summary>
        public void ChangeDocument(Func<GreenshotDocument, GreenshotDocument> change, string description, string mergeKey = null)
        {
            _tool.Cancel();
            Commit(change(_committed), description, mergeKey);
        }

        /// <summary>
        /// Moves the selection by the keys, like the select tool does
        /// </summary>
        public bool Key(Tools.ToolKey key, bool shift, bool control)
        {
            if (_editingText != null)
            {
                return false;
            }

            return _tool.Key(key, shift, control);
        }
    }
}
