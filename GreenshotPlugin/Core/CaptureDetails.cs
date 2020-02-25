using System;
using System.Collections.Generic;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Ocr;
using ZXing;

namespace GreenshotPlugin.Core
{
    /// <summary>
    /// This Class is used to pass details about the capture around.
    /// The time the Capture was taken and the Title of the window (or a region of) that is captured
    /// </summary>
    public class CaptureDetails : ICaptureDetails {

        /// <inheritdoc />
        public string Title {
            get;
            set;
        }

        /// <inheritdoc />
        public string Filename {
            get;
            set;
        }

        /// <inheritdoc />
        public DateTime DateTime {
            get;
            set;
        }

        /// <inheritdoc />
        public float DpiX {
            get;
            set;
        }

        /// <inheritdoc />
        public float DpiY {
            get;
            set;
        }

        /// <inheritdoc />
        public OcrInformation OcrInformation { get; set; }

        /// <inheritdoc />
        public Result QrResult { get; set; }

        /// <inheritdoc />
        public Dictionary<string, string> MetaData { get; } = new Dictionary<string, string>();

        /// <inheritdoc />
        public void AddMetaData(string key, string value) {
            if (MetaData.ContainsKey(key)) {
                MetaData[key] = value;
            } else {
                MetaData.Add(key, value);
            }
        }

        /// <inheritdoc />
        public CaptureMode CaptureMode {
            get;
            set;
        }

        /// <inheritdoc />
        public List<IDestination> CaptureDestinations { get; set; } = new List<IDestination>();

        /// <inheritdoc />
        public void ClearDestinations() {
            CaptureDestinations.Clear();
        }

        /// <inheritdoc />
        public void RemoveDestination(IDestination destination) {
            if (CaptureDestinations.Contains(destination)) {
                CaptureDestinations.Remove(destination);
            }
        }

        /// <inheritdoc />
        public void AddDestination(IDestination captureDestination) {
            if (!CaptureDestinations.Contains(captureDestination)) {
                CaptureDestinations.Add(captureDestination);
            }
        }

        /// <inheritdoc />
        public bool HasDestination(string designation) {
            foreach(IDestination destination in CaptureDestinations) {
                if (designation.Equals(destination.Designation)) {
                    return true;
                }
            }
            return false;
        }

        public CaptureDetails() {
            DateTime = DateTime.Now;
        }
    }
}