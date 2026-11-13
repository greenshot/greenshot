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
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editing;

namespace Greenshot.Editor.Standalone.Editing
{
    /// <summary>
    /// The effects around the capture (border, drop shadow, torn edges) are one frame element which follows the capture.
    /// They stay editable: a change of the frame is a change of its values, never of the pixels.
    /// </summary>
    public static class CaptureFrames
    {
        public static FrameElement FrameOf(GreenshotDocument document) =>
            DocumentEdits.CaptureOf(document) is ImageElement capture ? document.FrameOf(capture) : null;

        /// <summary>
        /// The document with the frame of the capture changed, a frame is added when there is none.
        /// The canvas grows with the frame, so a band or shadow outside of the capture is shown.
        /// </summary>
        public static GreenshotDocument WithFrame(GreenshotDocument document, Func<FrameElement, FrameElement> change)
        {
            if (DocumentEdits.CaptureOf(document) is not ImageElement capture)
            {
                return document;
            }

            var frame = document.FrameOf(capture);
            var changed = change(frame ?? new FrameElement(Uuid7.CreateVersion7(), capture.Area, new Random().Next())
            {
                Target = capture.Id,
                Shape = new EdgeShape(EdgeStyle.None),
                Placement = FramePlacement.Outside,
                Style = new Style { Shadow = new ShadowStyle { Enabled = false } }
            });
            var result = frame == null ? DocumentEdits.Add(document, new[] { changed }) : DocumentEdits.Replace(document, changed);
            return result.Canvas.Fit == CanvasFit.Capture ? result with { Canvas = result.Canvas with { Fit = CanvasFit.Content } } : result;
        }

        /// <summary>
        /// The document without the frame of the capture, the canvas is the capture again
        /// </summary>
        public static GreenshotDocument WithoutFrame(GreenshotDocument document)
        {
            if (FrameOf(document) is not FrameElement frame)
            {
                return document;
            }

            var result = DocumentEdits.Remove(document, new[] { frame.Id });
            bool otherFrames = result.Layers.SelectMany(layer => layer.Elements).OfType<FrameElement>().Any();
            return result.Canvas.Fit == CanvasFit.Content && !otherFrames ? result with { Canvas = result.Canvas with { Fit = CanvasFit.Capture } } : result;
        }
    }
}
