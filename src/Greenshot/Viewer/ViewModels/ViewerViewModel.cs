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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Recipes.Pipeline;

namespace Greenshot.Viewer.ViewModels
{
    /// <summary>
    /// The capture of a viewer window and its options. Owns the surface (a copy of the capture), only used on the UI thread.
    /// The options start with the settings, a change is stored as the default for the next viewers.
    /// </summary>
    public sealed class ViewerViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IViewerConfiguration _configuration;
        // The cursor elements while the mouse pointer is hidden, they are added again when it's shown
        private readonly List<IDrawableContainer> _hiddenCursors = new List<IDrawableContainer>();
        private ISurface _surface;
        private BitmapSource _image;
        private bool _alwaysOnTop;
        private bool _showTitleBar;
        private bool _showCursor;
        private bool _showHelp;

        public ViewerViewModel(ISurface surface, ICaptureDetails captureDetails, IViewerConfiguration configuration)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            var title = captureDetails?.Title ?? surface.CaptureDetails?.Title;
            Title = string.IsNullOrWhiteSpace(title) ? Texts.Settings.DestinationViewer : title;
            _alwaysOnTop = configuration.AlwaysOnTop;
            _showTitleBar = configuration.ShowTitleBar;
            _showCursor = configuration.ShowCursor;
            if (!_showCursor)
            {
                HideCursors();
            }

            Render();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The title of the capture
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The capture as shown: with the elements, and the mouse pointer unless it's hidden
        /// </summary>
        public BitmapSource Image
        {
            get => _image;
            private set
            {
                _image = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// True when the capture has a mouse pointer, which can be shown or hidden
        /// </summary>
        public bool HasCursor => _hiddenCursors.Count > 0 || (_surface?.Elements.OfType<ICursorContainer>().Any() ?? false);

        /// <summary>
        /// Keep the viewer on top of the other windows
        /// </summary>
        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                if (_alwaysOnTop == value) return;
                _alwaysOnTop = value;
                _configuration.AlwaysOnTop = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Show the title bar and the border, without them only the capture is shown
        /// </summary>
        public bool ShowTitleBar
        {
            get => _showTitleBar;
            set
            {
                if (_showTitleBar == value) return;
                _showTitleBar = value;
                _configuration.ShowTitleBar = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Show the help with the keys, only for this viewer
        /// </summary>
        public bool ShowHelp
        {
            get => _showHelp;
            set
            {
                if (_showHelp == value) return;
                _showHelp = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Show the captured mouse pointer
        /// </summary>
        public bool ShowCursor
        {
            get => _showCursor;
            set
            {
                if (_showCursor == value || _surface == null) return;
                _showCursor = value;
                _configuration.ShowCursor = value;
                if (value)
                {
                    foreach (var cursor in _hiddenCursors)
                    {
                        _surface.AddElement(cursor, false, false);
                    }

                    _hiddenCursors.Clear();
                }
                else
                {
                    HideCursors();
                }

                Render();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Export the capture: the user chooses the destination (or a recipe), a failed export shows the choice again with the error.
        /// The destination or recipe gets a copy, so one which keeps the capture (the editor) has its own and the viewer stays as it is.
        /// </summary>
        /// <param name="choose">Shows the destinations and the error of the last try (or null), returns the chosen destination or recipe, or nothing</param>
        public async Task ExportAsync(Func<IReadOnlyList<IDestination>, string, (IDestination Destination, CaptureRecipe Recipe)> choose)
        {
            string error = null;
            while (_surface != null)
            {
                var captureDetails = _surface.CaptureDetails;
                var destinations = DestinationHelper.GetAllDestinations()
                    .Where(d => !nameof(WellKnownDestinations.Picker).Equals(d.Designation, StringComparison.OrdinalIgnoreCase) && d.IsAvailableFor(captureDetails))
                    .ToList();
                var (destination, recipe) = choose(destinations, error);
                if (_surface == null)
                {
                    return;
                }

                if (recipe != null)
                {
                    ForwardToRecipe(recipe);
                    return;
                }

                if (destination == null)
                {
                    return;
                }

                var copy = _surface.Clone();
                ExportResult result = null;
                try
                {
                    result = await DestinationExporter.ExportAsync(destination, copy, copy.CaptureDetails, manuallyInitiated: true);
                }
                finally
                {
                    // Back on the UI thread
                    if (result?.KeepsCapture != true)
                    {
                        copy.Dispose();
                    }
                }

                if (result.Status != ExportStatus.Failed)
                {
                    return;
                }

                error = result.Error;
            }
        }

        /// <summary>
        /// Run the recipe with a copy of the capture, like a capture it made itself; the flow owns the copy
        /// </summary>
        private void ForwardToRecipe(CaptureRecipe recipe)
        {
            var copy = _surface.Clone();
            copy.CaptureDetails ??= new CaptureDetails();
            var payload = new CapturePayload
            {
                Surface = copy,
                RawCapture = new Capture(copy.GetImageForExport())
                {
                    CaptureDetails = copy.CaptureDetails
                }
            };
            CaptureFlowRunner.Current.Start(recipe, configure: context =>
            {
                context.Payload = payload;
                // The capture is handed over: the recipe must not capture or select again
                context.IsPayloadPreSupplied = true;
            });
        }

        private void HideCursors()
        {
            foreach (var cursor in _surface.Elements.OfType<ICursorContainer>().ToList())
            {
                _surface.RemoveElement(cursor, false, false, false);
                _hiddenCursors.Add(cursor);
            }
        }

        private void Render()
        {
            using var image = _surface.GetImageForExport();
            Image = image.ToBitmapSource();
        }

        /// <summary>
        /// The window is closed. An export which still runs has its own copy.
        /// </summary>
        public void Dispose()
        {
            if (_surface == null)
            {
                return;
            }

            // The hidden cursors are not on the surface, it doesn't dispose them
            foreach (var cursor in _hiddenCursors)
            {
                cursor.Dispose();
            }

            _hiddenCursors.Clear();
            _surface.Dispose();
            _surface = null;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
