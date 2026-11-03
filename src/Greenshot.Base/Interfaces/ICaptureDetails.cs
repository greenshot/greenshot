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
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// Details for the capture, like the window title and date/time etc.
    /// </summary>
    public interface ICaptureDetails
    {
        /// <summary>
        /// If the capture comes from a file, this contains the filename
        /// </summary>
        string Filename { get; set; }

        /// <summary>
        /// The title of the capture
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// When was the capture taken (or loaded)
        /// </summary>
        DateTime DateTime { get; set; }

        /// <summary>
        /// Destinations to where this capture goes or went
        /// </summary>
        List<IDestination> CaptureDestinations { get; set; }

        /// <summary>
        /// The meta data of the capture
        /// </summary>
        Dictionary<string, string> MetaData { get; }

        /// <summary>
        /// Helper method to prevent complex code which needs to check every key
        /// </summary>
        /// <param name="key">The key for the meta-data</param>
        /// <param name="value">The value for the meta-data</param>
        void AddMetaData(string key, string value);

        void ClearDestinations();
        void RemoveDestination(IDestination captureDestination);
        void AddDestination(IDestination captureDestination);
        bool HasDestination(string designation);

        CaptureMode CaptureMode { get; set; }

        float DpiX { get; set; }

        float DpiY { get; set; }

        /// <summary>
        /// Store the detected features for this capture
        /// </summary>
        List<IDetectedFeature> Features { get; }

        /// <summary>
        /// The part of the image as it was captured which is left after cropping, empty when it wasn't cropped.
        /// A processor which still looks at the image as captured hands what it found to <see cref="AddFeatures"/> with the crop area of that moment.
        /// </summary>
        Dapplo.Windows.Common.Structs.NativeRect CropArea { get; }

        /// <summary>
        /// The image was cropped: move the features with it and remove the ones which are no longer in the image
        /// </summary>
        /// <param name="cropRectangle">The new image, in coordinates of the image before the crop</param>
        void CropFeatures(Dapplo.Windows.Common.Structs.NativeRect cropRectangle);

        /// <summary>
        /// Add features found on the image while the crop area was <paramref name="cropAreaWhenFound"/>: when it was cropped since,
        /// they are moved with it and the ones which are no longer in the image are left out
        /// </summary>
        /// <param name="features">What was found, in coordinates of the image which was looked at</param>
        /// <param name="cropAreaWhenFound">The <see cref="CropArea"/> when the image was taken</param>
        void AddFeatures(IEnumerable<IDetectedFeature> features, Dapplo.Windows.Common.Structs.NativeRect cropAreaWhenFound);

        /// <summary>
        /// A Task that completes when all background capture processors (like OCR and QR) have finished.
        /// </summary>
        System.Threading.Tasks.Task ProcessingTask { get; set; }

        /// <summary>
        /// Keeps track of which processors have been started for this capture session.
        /// </summary>
        System.Collections.Generic.HashSet<string> StartedProcessors { get; }

        /// <summary>
        /// Creates a deep copy of the capture details.
        /// </summary>
        ICaptureDetails Clone();

        /// <summary>
        /// Event fired when features have been updated.
        /// </summary>
        event EventHandler FeaturesChanged;
    }
}