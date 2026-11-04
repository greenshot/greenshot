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
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Bitmap = System.Drawing.Bitmap;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The HDR images of the HDR displays of a screen capture, not joined into one HDR image yet: e.g. of the frozen screen, while the region
    /// isn't selected. Only what the displays delivered is kept (nothing of SDR displays); once the area is known, only that area is joined
    /// with the 8-bit image (see <see cref="Join"/>, Capture.Crop). Owned by the capture (ICapture.HdrParts), which disposes it.
    /// </summary>
    public sealed class HdrParts : IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(HdrParts));
        private readonly List<(HdrImage Part, NativePoint Location)> _parts;
        private WeakReference<System.Drawing.Image> _belongsTo;

        /// <param name="parts">The HDR image of each HDR display with its place in the capture: taken over, the parts dispose them</param>
        public HdrParts(IEnumerable<(HdrImage Part, NativePoint Location)> parts)
        {
            _parts = (parts ?? Enumerable.Empty<(HdrImage, NativePoint)>()).Where(part => part.Item1 != null).ToList();
        }

        /// <summary>
        /// The places of the HDR displays in the capture
        /// </summary>
        public IReadOnlyList<NativeRect> Areas => _parts.Select(part => new NativeRect(part.Location, new NativeSize(part.Part.Width, part.Part.Height))).ToList();

        /// <summary>
        /// HDR was asked for explicitly (see HdrImage.IsRequested): the joined HDR image gets it
        /// </summary>
        public bool IsRequested { get; set; }

        /// <summary>
        /// Mark the 8-bit image these parts belong to, like HdrImage.BelongTo
        /// </summary>
        public void BelongTo(System.Drawing.Image image) => _belongsTo = image == null ? null : new WeakReference<System.Drawing.Image>(image);

        /// <summary>
        /// True when these parts belong to exactly this image (see <see cref="BelongTo"/>)
        /// </summary>
        public bool BelongsTo(System.Drawing.Image image) => image != null && _belongsTo != null && _belongsTo.TryGetTarget(out var own) && ReferenceEquals(own, image);

        /// <summary>
        /// The HDR image of an area of the capture: the parts of the HDR displays in it, and the rest from the 8-bit image of the area at the SDR
        /// white level (HdrImage.Combine). The parts stay as they are.
        /// </summary>
        /// <param name="sdrImage">The 8-bit image of the area</param>
        /// <param name="area">The area in the coordinates of the capture</param>
        /// <returns>The HDR image, null when no HDR display is in the area or nothing of it has HDR content (no memory is spent then)</returns>
        public HdrImage Join(Bitmap sdrImage, NativeRect area)
        {
            if (sdrImage == null) throw new ArgumentNullException(nameof(sdrImage));
            var inArea = new List<(HdrImage, NativePoint)>();
            try
            {
                foreach (var (part, location) in _parts)
                {
                    var placed = new NativeRect(location, new NativeSize(part.Width, part.Height));
                    var overlap = placed.Intersect(area);
                    if (overlap.Width <= 0 || overlap.Height <= 0)
                    {
                        continue;
                    }
                    var cropped = part.Crop(new NativeRect(overlap.X - location.X, overlap.Y - location.Y, overlap.Width, overlap.Height));
                    if (cropped != null)
                    {
                        inArea.Add((cropped, new NativePoint(overlap.X - area.X, overlap.Y - area.Y)));
                    }
                }
                if (inArea.Count == 0)
                {
                    Log.Info($"Selection {area.Width}x{area.Height} at {area.X},{area.Y}: not on an HDR display, no HDR image.");
                    return null;
                }
                // E.g. only a normal window on an HDR display: nothing an 8-bit image can't show, no HDR image
                if (!inArea.Any(part => part.Item1.HasHdrContent))
                {
                    Log.Info($"Selection {area.Width}x{area.Height} at {area.X},{area.Y}: no HDR content (nothing brighter than SDR white or outside sRGB), no HDR image.");
                    return null;
                }
                var joined = HdrImage.Combine(sdrImage, inArea);
                if (joined != null)
                {
                    joined.IsRequested |= IsRequested;
                    joined.BelongTo(sdrImage);
                    Log.Info($"Selection {area.Width}x{area.Height} at {area.X},{area.Y}: joined {joined.Width}x{joined.Height} with HDR content of {inArea.Count} HDR display(s).");
                }
                return joined;
            }
            finally
            {
                foreach (var (part, _) in inArea)
                {
                    part.Dispose();
                }
            }
        }

        /// <summary>
        /// Mark that HDR was asked for explicitly, on the capture's HDR image or on its parts which aren't joined yet (without joining them)
        /// </summary>
        public static void MarkRequested(ICapture capture)
        {
            if (capture?.HdrParts != null)
            {
                capture.HdrParts.IsRequested = true;
            }
            else if (capture?.HdrImage != null)
            {
                capture.HdrImage.IsRequested = true;
            }
        }

        public void Dispose()
        {
            foreach (var (part, _) in _parts)
            {
                part.Dispose();
            }
            _parts.Clear();
        }
    }
}
