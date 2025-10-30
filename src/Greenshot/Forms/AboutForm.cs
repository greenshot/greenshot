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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Permissions;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Configuration;
using log4net;

namespace Greenshot.Forms
{
    /// <summary>
    /// The about form
    /// </summary>
    public sealed partial class AboutForm : AnimatingBaseForm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AboutForm));
        private Bitmap _bitmap;
        private readonly ColorAnimator _backgroundAnimation;
        private readonly List<RectangleAnimator> _pixels = new();
        private readonly List<Color> _colorFlow = new();
        private readonly List<Color> _pixelColors = new();
        private readonly Random _rand = new();
        private readonly Color _backColor = Color.FromArgb(61, 61, 61);
        private readonly Color _pixelColor = Color.FromArgb(138, 255, 0);

        // Variables used for the color-cycle
        private int _waitFrames;
        private int _colorIndex;
        private int _scrollCount;
        private bool _hasAnimationsLeft;

        // Variables are used to define the location of the dots
        private const int W = 13;
        private const int P1 = 7;
        private const int P2 = P1 + W;
        private const int P3 = P2 + W;
        private const int P4 = P3 + W;
        private const int P5 = P4 + W;
        private const int P6 = P5 + W;
        private const int P7 = P6 + W;

        /// <summary>
        /// The location of every dot in the "G"
        /// </summary>
        private readonly List<NativePoint> _gSpots = new List<NativePoint>
        {
            // Top row
            new(P2, P1), // 0
            new(P3, P1), // 1
            new(P4, P1), // 2
            new(P5, P1), // 3
            new(P6, P1), // 4

            // Second row
            new(P1, P2), // 5
            new(P2, P2), // 6

            // Third row
            new(P1, P3), // 7
            new(P2, P3), // 8

            // Fourth row
            new(P1, P4), // 9
            new(P2, P4), // 10
            new(P5, P4), // 11
            new(P6, P4), // 12
            new(P7, P4), // 13

            // Fifth row
            new(P1, P5), // 14
            new(P2, P5), // 15
            new(P6, P5), // 16
            new(P7, P5), // 17

            // Sixth row
            new(P1, P6), // 18
            new(P2, P6), // 19
            new(P3, P6), // 20
            new(P4, P6), // 21
            new(P5, P6), // 22
            new(P6, P6) // 23
        };

        //     0  1  2  3  4
        //  5  6
        //  7  8
        //  9 10       11 12 13
        // 14 15          16 17
        // 18 19 20 21 22 23

        // The order in which we draw the dots & flow the colors.
        private readonly List<int> _flowOrder = new() { 4, 3, 2, 1, 0, 5, 6, 7, 8, 9, 10, 14, 15, 18, 19, 20, 21, 22, 23, 16, 17, 13, 12, 11 };

        /// <summary>
        /// Cleanup all the allocated resources
        /// </summary>
        private void Cleanup(object sender, EventArgs e)
        {
            if (_bitmap == null) return;
            _bitmap.Dispose();
            _bitmap = null;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public AboutForm()
        {
            // Make sure our resources are removed again.
            Disposed += Cleanup;
            FormClosing += Cleanup;

            // Enable animation for this form, when we don't set this the timer doesn't start as soon as the form is loaded.
            EnableAnimation = true;
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();

            // Only use double-buffering when we are NOT in a Terminal Server session
            DoubleBuffered = !IsTerminalServerSession;

            // Use the self drawn image, first we create the background to be the back-color (as we animate from this)

            _bitmap = ImageHelper.CreateEmpty(90, 90, PixelFormat.Format24bppRgb, BackColor, 96, 96);
            pictureBox1.Image = _bitmap;

            lblTitle.Text = $@"Greenshot {EnvironmentInfo.GetGreenshotVersion()} {(IniConfig.IsPortable ? " Portable" : "")} ({OsInfo.Bits} bit)";

            // Number of frames the pixel animation takes
            int frames = FramesForMillis(2000);
            // The number of frames the color-cycle waits before it starts
            _waitFrames = FramesForMillis(6000);

            // Every pixel is created after pixelWaitFrames frames, which is increased in the loop.
            int pixelWaitFrames = FramesForMillis(2000);
            // Create pixels
            for (int index = 0; index < _gSpots.Count; index++)
            {
                // Read the pixels in the order of the flow
                NativePoint gSpot = _gSpots[_flowOrder[index]];
                // Create the animation, first we do nothing (on the final destination)
                RectangleAnimator pixelAnimation;

                // Make the pixel grow from the middle, if this offset isn't used it looks like it's shifted
                int offset = (W - 2) / 2;

                // If the optimize for Terminal Server is set we make the animation without much ado
                if (IsTerminalServerSession)
                {
                    // No animation
                    pixelAnimation = new RectangleAnimator(new NativeRect(gSpot.X, gSpot.Y, W - 2, W - 2), new NativeRect(gSpot.X, gSpot.Y, W - 2, W - 2), 1, EasingType.Cubic, EasingMode.EaseIn);
                }
                else
                {
                    // Create the animation, first we do nothing (on the final destination)
                    NativeRect standingStill = new NativeRect(gSpot.X + offset, gSpot.Y + offset, 0, 0);
                    pixelAnimation = new RectangleAnimator(standingStill, standingStill, pixelWaitFrames, EasingType.Quintic, EasingMode.EaseIn);
                    // And than we size to the wanted size.
                    pixelAnimation.QueueDestinationLeg(new NativeRect(gSpot.X, gSpot.Y, W - 2, W - 2), frames);
                }

                // Increase the wait frames
                pixelWaitFrames += FramesForMillis(100);
                // Add to the list of to be animated pixels
                _pixels.Add(pixelAnimation);
                // Add a color to the list for this pixel.
                _pixelColors.Add(_pixelColor);
            }

            // Make sure the frame "loop" knows we have to animate
            _hasAnimationsLeft = true;

            // Pixel Color cycle colors, here we use a pre-animated loop which stores the values.
            ColorAnimator pixelColorAnimator = new ColorAnimator(_pixelColor, Color.FromArgb(255, 255, 255), 6, EasingType.Quadratic, EasingMode.EaseIn);
            pixelColorAnimator.QueueDestinationLeg(_pixelColor, 6, EasingType.Quadratic, EasingMode.EaseOut);
            do
            {
                _colorFlow.Add(pixelColorAnimator.Current);
                pixelColorAnimator.Next();
            } while (pixelColorAnimator.HasNext);

            // color animation for the background
            _backgroundAnimation = new ColorAnimator(BackColor, _backColor, FramesForMillis(5000), EasingType.Linear, EasingMode.EaseIn);
        }

        /// <summary>
        /// This is called when a link is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LinkLabelClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is not LinkLabel linkLabel) return;
            var link = linkLabel.Tag?.ToString() ?? linkLabel.Text;
            try
            {
                linkLabel.LinkVisited = true;
                Process.Start(link);
            }
            catch (Exception)
            {
                MessageBox.Show(Language.GetFormattedString(LangKey.error_openlink, link), Language.GetString(LangKey.error));
            }
        }

        /// <summary>
        /// Called from the AnimatingForm, for every frame
        /// </summary>
        protected override void Animate()
        {
            if (_bitmap == null)
            {
                return;
            }

            if (!IsTerminalServerSession)
            {
                // Color cycle
                if (_waitFrames != 0)
                {
                    _waitFrames--;
                    // Check if there is something else to do, if not we return so we don't occupy the CPU
                    if (!_hasAnimationsLeft)
                    {
                        return;
                    }
                }
                else if (_scrollCount < _pixelColors.Count + _colorFlow.Count)
                {
                    // Scroll colors, the scrollCount is the amount of pixels + the amount of colors to cycle.
                    for (int index = _pixelColors.Count - 1; index > 0; index--)
                    {
                        _pixelColors[index] = _pixelColors[index - 1];
                    }

                    // Keep adding from  the colors to cycle until there is nothing left
                    if (_colorIndex < _colorFlow.Count)
                    {
                        _pixelColors[0] = _colorFlow[_colorIndex++];
                    }

                    _scrollCount++;
                }
                else
                {
                    // Reset values, wait X time for the next one
                    _waitFrames = FramesForMillis(3000 + _rand.Next(35000));
                    _colorIndex = 0;
                    _scrollCount = 0;
                    // Check if there is something else to do, if not we return so we don't occupy the CPU
                    if (!_hasAnimationsLeft)
                    {
                        return;
                    }
                }
            }
            else if (!_hasAnimationsLeft)
            {
                return;
            }

            // Draw the "G"
            using (Graphics graphics = Graphics.FromImage(_bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.Clear(_backgroundAnimation.Next());

                graphics.TranslateTransform(2, -2);
                graphics.RotateTransform(20);

                using SolidBrush brush = new SolidBrush(_pixelColor);
                int index = 0;
                // We assume there is nothing to animate in the next Animate loop
                _hasAnimationsLeft = false;
                // Pixels of the G
                foreach (RectangleAnimator pixel in _pixels)
                {
                    brush.Color = _pixelColors[index++];
                    graphics.FillEllipse(brush, pixel.Current);
                    // If a pixel still has frames left, the hasAnimationsLeft will be true
                    _hasAnimationsLeft |= pixel.HasNext;
                    pixel.Next();
                }
            }

            pictureBox1.Invalidate();
        }

        /// <summary>
        /// CmdKey handler
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                switch (keyData)
                {
                    case Keys.Escape:
                        DialogResult = DialogResult.Cancel;
                        break;
                    case Keys.E:
                        MessageBox.Show(EnvironmentInfo.EnvironmentToString(true));
                        break;
                    case Keys.L:
                        try
                        {
                            if (File.Exists(MainForm.LogFileLocation))
                            {
                                using (Process.Start("\"" + MainForm.LogFileLocation + "\""))
                                {
                                    // nothing to do, just using dispose to cleanup
                                }
                            }
                            else
                            {
                                MessageBox.Show(@"Greenshot can't find the logfile, it should have been here: " + MainForm.LogFileLocation);
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show(@"Couldn't open the greenshot.log, it's located here: " + MainForm.LogFileLocation, @"Error opening greenshot.log",
                                MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        }

                        break;
                    case Keys.I:
                        try
                        {
                            using (Process.Start("\"" + IniConfig.ConfigLocation + "\""))
                            {
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show(@"Couldn't open the greenshot.ini, it's located here: " + IniConfig.ConfigLocation, @"Error opening greenshot.ini",
                                MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        }

                        break;
                    default:
                        return base.ProcessCmdKey(ref msg, keyData);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling key '{keyData}'", ex);
            }

            return true;
        }
    }
}