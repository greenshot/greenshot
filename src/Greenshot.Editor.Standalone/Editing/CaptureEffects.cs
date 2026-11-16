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
using System.IO;
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.Editor.Standalone.Files;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Editor.Standalone.Editing
{
    /// <summary>
    /// The effects which change the pixels of the capture (grayscale, invert), like in the old editor: the capture gets a
    /// new image, what is drawn on it stays as it is. An undo brings the image back.
    /// </summary>
    public static class CaptureEffects
    {
        public static GreenshotDocument Grayscale(GreenshotDocument document) => Apply(document, context => context.Grayscale());

        public static GreenshotDocument Invert(GreenshotDocument document) => Apply(document, context => context.Invert());

        /// <summary>
        /// The document with the effect on the image of its capture; the document itself when it has no capture image
        /// </summary>
        public static GreenshotDocument Apply(GreenshotDocument document, Action<IImageProcessingContext> effect)
        {
            if (DocumentEdits.CaptureOf(document) is not ImageElement capture || !document.Resources.TryGetValue(capture.Resource, out var resource))
            {
                return document;
            }

            byte[] png;
            using (var stream = resource.OpenRead())
            using (var image = Image.Load<Rgba32>(stream))
            using (var changed = new MemoryStream())
            {
                image.Mutate(effect);
                image.SaveAsPng(changed);
                png = changed.ToArray();
            }

            var replacement = DocumentFiles.ImageResource(png, out _, out _);
            var result = DocumentEdits.Replace(document, capture with { Resource = replacement.Id });
            // The image before is only kept by the undo steps, not by the document
            bool stillUsed = result.Layers.SelectMany(layer => layer.Elements).OfType<ImageElement>().Any(element => element.Resource == resource.Id);
            return result with
            {
                Resources = result.Resources.Where(pair => stillUsed || pair.Key != resource.Id).Append(new(replacement.Id, replacement)).ToDictionary(pair => pair.Key, pair => pair.Value)
            };
        }
    }
}
