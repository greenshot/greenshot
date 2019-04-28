// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Addons.Core;
using Greenshot.Gfx;
using Greenshot.Gfx.Stitching;

namespace Greenshot.Helpers
{
    /// <summary>
    /// This takes care of taking a scrolling capture
    /// </summary>
    public class ScrollingCapture
    {
        private static readonly LogSource Log = new LogSource();
        private readonly WindowScroller _windowScroller;

        /// <summary>
        /// The delay between captures
        /// </summary>
        public int Delay { get; set; } = 150;

        /// <summary>
        /// Constructor, this needs a WindowScroller
        /// </summary>
        /// <param name="windowScroller">WindowScroller</param>
        public ScrollingCapture(WindowScroller windowScroller)
        {
            _windowScroller = windowScroller;

            // Set scrollmode to windows message, which is the default but still...
            windowScroller.ScrollMode = ScrollModes.WindowsMessage;
        }

        /// <summary>
        /// Start the capture
        /// </summary>
        /// <returns>Bitmap</returns>
        public IBitmapWithNativeSupport Capture()
        {
            if (_windowScroller.NeedsFocus())
            {
                User32Api.SetForegroundWindow(_windowScroller.ScrollBarWindow.Handle);
                Application.DoEvents();
                Thread.Sleep(Delay);
                Application.DoEvents();
            }

            // Find the area which is scrolling

            // 1. Take the client bounds
            var clientBounds = _windowScroller.ScrollBarWindow.GetInfo().ClientBounds;

            // Use a region for steps 2 and 3
            using (var region = new Region(clientBounds))
            {
                // 2. exclude the children, if any
                foreach (var interopWindow in _windowScroller.ScrollBarWindow.GetChildren())
                {
                    region.Exclude(interopWindow.GetInfo().Bounds);
                }
                // 3. exclude the scrollbar, if it can be found
                if (_windowScroller.ScrollBar.HasValue)
                {
                    region.Exclude(_windowScroller.ScrollBar.Value.Bounds);
                }
                // Get the bounds of the region
                using (var screenGraphics = Graphics.FromHwnd(User32Api.GetDesktopWindow()))
                {
                    var rectangleF = region.GetBounds(screenGraphics);
                    clientBounds = new NativeRect((int)rectangleF.X, (int)rectangleF.Y, (int)rectangleF.Width, (int)rectangleF.Height);
                }
            }

            if (clientBounds.Width * clientBounds.Height <= 0)
            {
                return null;
            }
            // Move the window to the start
            _windowScroller.Start();

            // Register a keyboard hook to make it possible to ESC the capturing
            var breakScroll = false;
            var keyboardHook = KeyboardHook.KeyboardEvents
                .Where(args => args.Key == VirtualKeyCode.Escape)
                .Subscribe(args =>
                {
                    args.Handled = true;
                    breakScroll = true;
                });
            IBitmapWithNativeSupport resultImage = null;
            try
            {
                // A delay to make the window move
                Application.DoEvents();
                Thread.Sleep(Delay);
                Application.DoEvents();

                if (_windowScroller.IsAtStart)
                {
                    using (var bitmapStitcher = new BitmapStitcher())
                    {
                        bitmapStitcher.AddBitmap(WindowCapture.CaptureRectangle(clientBounds));

                        // Loop as long as we are not at the end yet
                        while (!_windowScroller.IsAtEnd && !breakScroll)
                        {
                            // Next "page"
                            _windowScroller.Next();
                            // Wait a bit, so the window can update
                            Application.DoEvents();
                            Thread.Sleep(Delay);
                            Application.DoEvents();
                            // Capture inside loop
                            bitmapStitcher.AddBitmap(WindowCapture.CaptureRectangle(clientBounds));
                        }
                        resultImage = bitmapStitcher.Result();
                    }

                }
                else
                {
                    resultImage = WindowCapture.CaptureRectangle(clientBounds);
                }
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex);
            }
            finally
            {
                // Remove hook for escape
                keyboardHook.Dispose();
                // Try to reset location
                _windowScroller.Reset();
            }
                
            return resultImage;
        }
    }
}
