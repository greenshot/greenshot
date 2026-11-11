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

namespace Greenshot.Document.Editing
{
    /// <summary>
    /// How selected elements change their place in the drawing order of their layer
    /// </summary>
    public enum Arrangement
    {
        ToFront,
        Forward,
        Backward,
        ToBack
    }

    /// <summary>
    /// The changes an editor makes to a document. Each gives a new document, the old one stays as it was (for undo).
    /// </summary>
    public static class DocumentEdits
    {
        /// <summary>
        /// The element with the id, null when the document has none
        /// </summary>
        public static Element Find(GreenshotDocument document, Guid id) => document.Layers.SelectMany(layer => layer.Elements).FirstOrDefault(element => element.Id == id);

        /// <summary>
        /// The capture: the first image of the capture layer, null when the document has none
        /// </summary>
        public static ImageElement CaptureOf(GreenshotDocument document) =>
            document.Layers.Where(layer => layer.Kind == LayerKind.Capture).SelectMany(layer => layer.Elements).OfType<ImageElement>().FirstOrDefault();

        /// <summary>
        /// A new document with one capture layer showing the image, and an empty annotations layer
        /// </summary>
        public static GreenshotDocument FromImage(Resource image, int width, int height)
        {
            var capture = new ImageElement(Uuid7.CreateVersion7(), new RectD(0, 0, width, height), image.Id);
            var layers = new List<Layer>
            {
                new(Uuid7.CreateVersion7(), "Capture", LayerKind.Capture, new List<Element> { capture }) { Locked = true },
                new(Uuid7.CreateVersion7(), "Annotations", LayerKind.Annotations, new List<Element>())
            };
            return new GreenshotDocument(Uuid7.CreateVersion7(), new Canvas(), layers, new Dictionary<Guid, Resource> { [image.Id] = image });
        }

        /// <summary>
        /// The elements with the same ids replaced by the changed ones
        /// </summary>
        public static GreenshotDocument Replace(GreenshotDocument document, IEnumerable<Element> changed)
        {
            var byId = changed.ToDictionary(element => element.Id);
            if (byId.Count == 0)
            {
                return document;
            }

            return document with
            {
                Layers = document.Layers.Select(layer => layer.Elements.Any(element => byId.ContainsKey(element.Id))
                    ? layer with { Elements = layer.Elements.Select(element => byId.TryGetValue(element.Id, out var replacement) ? replacement : element).ToList() }
                    : layer).ToList()
            };
        }

        public static GreenshotDocument Replace(GreenshotDocument document, Element changed) => Replace(document, new[] { changed });

        /// <summary>
        /// The elements added on top of the annotations layer, which is created when the document has none; the resources
        /// they link to are added too
        /// </summary>
        public static GreenshotDocument Add(GreenshotDocument document, IEnumerable<Element> added, IEnumerable<Resource> resources = null)
        {
            var elements = added.ToList();
            var layers = document.Layers.ToList();
            int index = layers.FindLastIndex(layer => layer.Kind == LayerKind.Annotations && !layer.Locked);
            if (index < 0)
            {
                // Above the capture, below an overlay (the mouse cursor)
                int overlay = layers.FindIndex(layer => layer.Kind == LayerKind.Overlay);
                index = overlay < 0 ? layers.Count : overlay;
                layers.Insert(index, new Layer(Uuid7.CreateVersion7(), "Annotations", LayerKind.Annotations, Array.Empty<Element>()));
            }

            layers[index] = layers[index] with { Elements = layers[index].Elements.Concat(elements).ToList() };
            var allResources = document.Resources;
            if (resources != null)
            {
                var merged = new Dictionary<Guid, Resource>(document.Resources.ToDictionary(pair => pair.Key, pair => pair.Value));
                foreach (var resource in resources)
                {
                    merged[resource.Id] = resource;
                }

                allResources = merged;
            }

            return document with { Layers = layers, Resources = allResources };
        }

        /// <summary>
        /// The elements removed, with the frames around them; they are no longer members of their groups, a group of the
        /// types set or style without members is removed
        /// </summary>
        public static GreenshotDocument Remove(GreenshotDocument document, ICollection<Guid> ids)
        {
            if (ids.Count == 0)
            {
                return document;
            }

            bool Removed(Element element) => ids.Contains(element.Id) || element is FrameElement { Target: Guid target } && ids.Contains(target);
            return document with
            {
                Layers = document.Layers.Select(layer => layer.Elements.Any(Removed) ? layer with { Elements = layer.Elements.Where(element => !Removed(element)).ToList() } : layer).ToList(),
                Groups = document.Groups
                    .Select(group => group.Members.Any(member => member is Guid id && ids.Contains(id))
                        ? group with { Members = group.Members.Where(member => member is not Guid id || !ids.Contains(id)).ToList() }
                        : group)
                    .Where(group => group.Members.Any(member => member != null) || group.Type is not (GroupTypes.Set or GroupTypes.Style))
                    .ToList()
            };
        }

        /// <summary>
        /// The elements moved in the drawing order of their layer, the others keep their order
        /// </summary>
        public static GreenshotDocument Arrange(GreenshotDocument document, ICollection<Guid> ids, Arrangement arrangement)
        {
            if (ids.Count == 0)
            {
                return document;
            }

            return document with
            {
                Layers = document.Layers.Select(layer =>
                {
                    if (!layer.Elements.Any(element => ids.Contains(element.Id)))
                    {
                        return layer;
                    }

                    var elements = layer.Elements.ToList();
                    switch (arrangement)
                    {
                        case Arrangement.ToFront:
                            elements = elements.Where(element => !ids.Contains(element.Id)).Concat(elements.Where(element => ids.Contains(element.Id))).ToList();
                            break;
                        case Arrangement.ToBack:
                            elements = elements.Where(element => ids.Contains(element.Id)).Concat(elements.Where(element => !ids.Contains(element.Id))).ToList();
                            break;
                        case Arrangement.Forward:
                            for (int i = elements.Count - 2; i >= 0; i--)
                            {
                                if (ids.Contains(elements[i].Id) && !ids.Contains(elements[i + 1].Id))
                                {
                                    (elements[i], elements[i + 1]) = (elements[i + 1], elements[i]);
                                }
                            }

                            break;
                        case Arrangement.Backward:
                            for (int i = 1; i < elements.Count; i++)
                            {
                                if (ids.Contains(elements[i].Id) && !ids.Contains(elements[i - 1].Id))
                                {
                                    (elements[i], elements[i - 1]) = (elements[i - 1], elements[i]);
                                }
                            }

                            break;
                    }

                    return layer with { Elements = elements };
                }).ToList()
            };
        }

        /// <summary>
        /// The canvas cut to the area: a fixed canvas of its size, everything moved so the area starts at 0, 0. Nothing is
        /// removed, what is outside of the canvas isn't drawn or exported, an undo brings it back.
        /// </summary>
        public static GreenshotDocument Crop(GreenshotDocument document, RectD area)
        {
            double x = Math.Round(area.X), y = Math.Round(area.Y);
            int width = Math.Max(1, (int)Math.Round(area.Width)), height = Math.Max(1, (int)Math.Round(area.Height));
            return document with
            {
                Canvas = document.Canvas with { Fit = CanvasFit.Fixed, Width = width, Height = height, Padding = 0 },
                Layers = document.Layers.Select(layer => layer with { Elements = layer.Elements.Select(element => Anchors.Move(element, -x, -y)).ToList() }).ToList()
            };
        }

        /// <summary>
        /// Everything scaled by the factors from 0, 0: areas, points and the size of a fixed canvas; lines keep their width
        /// </summary>
        public static GreenshotDocument Scale(GreenshotDocument document, double factorX, double factorY)
        {
            RectD Area(RectD area) => new(area.X * factorX, area.Y * factorY, area.Width * factorX, area.Height * factorY);
            PointD Point(PointD point) => new(point.X * factorX, point.Y * factorY);
            Element Scaled(Element element) => element switch
            {
                LineElement e => e with { Start = Point(e.Start), End = Point(e.End) },
                ArrowElement e => e with { Start = Point(e.Start), End = Point(e.End) },
                FreehandElement e => e with { Points = e.Points.Select(Point).ToList() },
                SpeechBubbleElement e => e with { Area = Area(e.Area), Tail = Point(e.Tail) },
                UnknownElement => element,
                _ when ElementShapes.AreaOf(element) is RectD area => ElementShapes.WithArea(element, Area(area)),
                _ => element
            };
            var canvas = document.Canvas;
            return document with
            {
                Canvas = canvas.Fit == CanvasFit.Fixed
                    ? canvas with { Width = (int?)Math.Round((canvas.Width ?? 0) * factorX), Height = (int?)Math.Round((canvas.Height ?? 0) * factorY) }
                    : canvas,
                Layers = document.Layers.Select(layer => layer with { Elements = layer.Elements.Select(Scaled).ToList() }).ToList()
            };
        }

        /// <summary>
        /// The ids of all elements of the sets the elements are in, with the elements: a set is selected as a whole
        /// </summary>
        public static ISet<Guid> WithSets(GreenshotDocument document, IEnumerable<Guid> ids)
        {
            var result = new HashSet<Guid>(ids);
            foreach (var set in document.Groups.Where(group => group.Type == GroupTypes.Set && group.Members.Any(member => member is Guid id && result.Contains(id))).ToList())
            {
                result.UnionWith(set.Members.OfType<Guid>());
            }

            return result;
        }
    }
}
