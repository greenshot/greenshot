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

namespace Greenshot.WpfEditor.Rendering
{
    /// <summary>
    /// What has to be drawn again after a change of the document
    /// </summary>
    public static class DocumentChanges
    {
        /// <summary>
        /// The area which changed, in canvas pixels; null when everything has to be drawn again (the canvas, the layers, the
        /// groups or the numbering of step labels changed)
        /// </summary>
        public static RectD? ChangedArea(GreenshotDocument before, GreenshotDocument after)
        {
            if (before == null || after == null || !Equals(before.Canvas, after.Canvas) || !ReferenceEquals(before.Groups, after.Groups) && !before.Groups.SequenceEqual(after.Groups)
                || before.Layers.Count != after.Layers.Count || !ReferenceEquals(before.Resources, after.Resources) && before.Resources.Count != after.Resources.Count)
            {
                return null;
            }

            RectD? changed = null;
            void Add(GreenshotDocument document, Element element)
            {
                var bounds = document.VisualBounds(element);
                if (document.FrameOf(element) is FrameElement frame)
                {
                    bounds = RectD.Union(bounds, document.VisualBounds(frame));
                }

                changed = changed is RectD known ? RectD.Union(known, bounds) : bounds;
            }

            for (int i = 0; i < before.Layers.Count; i++)
            {
                Layer old = before.Layers[i], now = after.Layers[i];
                if (ReferenceEquals(old, now))
                {
                    continue;
                }

                if (old.Visible != now.Visible || old.Id != now.Id)
                {
                    return null;
                }

                var oldById = old.Elements.ToDictionary(element => element.Id);
                var nowById = now.Elements.ToDictionary(element => element.Id);
                bool orderChanged = !old.Elements.Select(element => element.Id).Where(nowById.ContainsKey).SequenceEqual(now.Elements.Select(element => element.Id).Where(oldById.ContainsKey));
                bool stepsChanged = old.Elements.OfType<StepLabelElement>().Count() != now.Elements.OfType<StepLabelElement>().Count()
                                    || orderChanged && now.Elements.OfType<StepLabelElement>().Any();
                if (stepsChanged || HasWholeCanvasEffect(old) || HasWholeCanvasEffect(now))
                {
                    // Numbers of other step labels change, or an effect works on everything
                    return null;
                }

                foreach (var element in old.Elements)
                {
                    if (!nowById.TryGetValue(element.Id, out var replacement) || !ReferenceEquals(replacement, element) || orderChanged)
                    {
                        Add(before, element);
                    }
                }

                foreach (var element in now.Elements)
                {
                    if (!oldById.TryGetValue(element.Id, out var original) || !ReferenceEquals(original, element) || orderChanged)
                    {
                        Add(after, element);
                    }
                }
            }

            return changed;
        }

        private static bool HasWholeCanvasEffect(Layer layer) =>
            layer.Elements.Any(element => element is HighlightElement { Mode: HighlightMode.Area or HighlightMode.Grayscale } or FrameElement { Target: null });
    }
}
