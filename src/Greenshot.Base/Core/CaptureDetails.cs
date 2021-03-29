/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using Greenshot.Base.Interfaces.Ocr;

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
        public OcrInformation OcrInformation { get; set; }

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
    }
}