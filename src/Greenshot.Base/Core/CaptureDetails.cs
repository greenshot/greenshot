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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// This Class is used to pass details about the capture around.
    /// The time the Capture was taken and the Title of the window (or a region of) that is captured
    /// </summary>
    public class CaptureDetails : ICaptureDetails
    {
        /// <inheritdoc />
        public string Title { get; set; }

        /// <inheritdoc />
        public string Filename { get; set; }

        /// <inheritdoc />
        public DateTime DateTime { get; set; }

        /// <inheritdoc />
        public float DpiX { get; set; }

        /// <inheritdoc />
        public float DpiY { get; set; }

        /// <inheritdoc />
        public List<IDetectedFeature> Features { get; } = new List<IDetectedFeature>();

        /// <inheritdoc />
        public Dictionary<string, string> MetaData { get; } = new Dictionary<string, string>();

        /// <inheritdoc />
        public void AddMetaData(string key, string value)
        {
            if (MetaData.ContainsKey(key))
            {
                MetaData[key] = value;
            }
            else
            {
                MetaData.Add(key, value);
            }
        }

        /// <inheritdoc />
        public CaptureMode CaptureMode { get; set; }

        /// <inheritdoc />
        public List<IDestination> CaptureDestinations { get; set; } = new List<IDestination>();

        /// <inheritdoc />
        public void ClearDestinations()
        {
            CaptureDestinations.Clear();
        }

        /// <inheritdoc />
        public void RemoveDestination(IDestination destination)
        {
            if (CaptureDestinations.Contains(destination))
            {
                CaptureDestinations.Remove(destination);
            }
        }

        /// <inheritdoc />
        public void AddDestination(IDestination captureDestination)
        {
            if (!CaptureDestinations.Contains(captureDestination))
            {
                CaptureDestinations.Add(captureDestination);
            }
        }

        /// <inheritdoc />
        public bool HasDestination(string designation)
        {
            foreach (IDestination destination in CaptureDestinations)
            {
                if (designation.Equals(destination.Designation))
                {
                    return true;
                }
            }

            return false;
        }

        public CaptureDetails()
        {
            DateTime = DateTime.Now;
        }

        /// <inheritdoc />
        public NativeRect CropArea { get; private set; } = NativeRect.Empty;

        /// <inheritdoc />
        public void CropFeatures(NativeRect cropRectangle)
        {
            lock (Features)
            {
                CropArea = new NativeRect(CropArea.Location.Offset(cropRectangle.Location.X, cropRectangle.Location.Y), cropRectangle.Size);
                foreach (var feature in Features)
                {
                    feature.Offset(-cropRectangle.X, -cropRectangle.Y);
                }
                Features.RemoveAll(feature => !IsInImage(feature, cropRectangle.Size));
            }
        }

        /// <inheritdoc />
        public void AddFeatures(IEnumerable<IDetectedFeature> features, NativeRect cropAreaWhenFound)
        {
            lock (Features)
            {
                int dx = CropArea.X - cropAreaWhenFound.X;
                int dy = CropArea.Y - cropAreaWhenFound.Y;
                foreach (var feature in features)
                {
                    if (dx != 0 || dy != 0)
                    {
                        feature.Offset(-dx, -dy);
                    }
                    if (CropArea.IsEmpty || IsInImage(feature, CropArea.Size))
                    {
                        Features.Add(feature);
                    }
                }
            }
        }

        /// <summary>
        /// A feature is in the image when any part of it is: a text line cut by a text selection still gives the words which were selected.
        /// Windows always stay, they are where the selection snaps to.
        /// </summary>
        private static bool IsInImage(IDetectedFeature feature, NativeSize imageSize) =>
            feature is WindowFeature || feature.Bounds.IntersectsWith(new NativeRect(0, 0, imageSize.Width, imageSize.Height));

        /// <inheritdoc />
        public System.Threading.Tasks.Task ProcessingTask { get; set; }

        /// <inheritdoc />
        public HashSet<string> StartedProcessors { get; } = new HashSet<string>();

        /// <inheritdoc />
        public event EventHandler FeaturesChanged;

        /// <summary>
        /// Creates a deep copy of the capture details.
        /// </summary>
        public CaptureDetails Clone()
        {
            var clone = new CaptureDetails
            {
                Title = Title,
                Filename = Filename,
                DateTime = DateTime,
                DpiX = DpiX,
                DpiY = DpiY,
                CaptureMode = CaptureMode,
                CropArea = CropArea
            };

            if (MetaData != null)
            {
                foreach (var kvp in MetaData)
                {
                    clone.MetaData[kvp.Key] = kvp.Value;
                }
            }

            if (Features != null)
            {
                lock (Features)
                {
                    clone.Features.AddRange(Features);
                }
            }

            if (CaptureDestinations != null)
            {
                clone.CaptureDestinations.AddRange(CaptureDestinations);
            }

            return clone;
        }

        ICaptureDetails ICaptureDetails.Clone() => Clone();

        /// <summary>
        /// Helper to raise the FeaturesChanged event.
        /// </summary>
        public void NotifyFeaturesChanged()
        {
            FeaturesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}