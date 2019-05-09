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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Permissions;
using System.Windows.Forms;
using Dapplo.CaliburnMicro;
using Greenshot.Helpers;
using Dapplo.Log;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons;
using Greenshot.Addons.Animation;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Gfx;
using Dapplo.Windows.User32;
using Dapplo.Windows.Dpi;
using System.Text;

namespace Greenshot.Forms
{
    /// <summary>
    ///     The about form
    /// </summary>
    public sealed partial class AboutForm : AnimatingForm
    {
        private readonly IGreenshotLanguage _greenshotlanguage;
        private readonly IVersionProvider _versionProvider;

        private static readonly LogSource Log = new LogSource();
        // Variables are used to define the location of the dots
        private const int w = 13;
        private const int p1 = 7;
        private const int p2 = p1 + w;
        private const int p3 = p2 + w;
        private const int p4 = p3 + w;
        private const int p5 = p4 + w;
        private const int p6 = p5 + w;
        private const int p7 = p6 + w;
        private readonly Color _backColor = Color.FromArgb(61, 61, 61);
        private readonly ColorAnimator _backgroundAnimation;
        private readonly IList<Color> _colorFlow = new List<Color>();
        private readonly IDisposable _dpiSubscription;

        //     0  1  2  3  4
        //  5  6
        //  7  8
        //  9 10       11 12 13
        // 14 15          16 17
        // 18 19 20 21 22 23

        // The order in which we draw the dots & flow the collors.
        private readonly IList<int> _flowOrder = new List<int> {4, 3, 2, 1, 0, 5, 6, 7, 8, 9, 10, 14, 15, 18, 19, 20, 21, 22, 23, 16, 17, 13, 12, 11};

        /// <summary>
        ///     The location of every dot in the "G"
        /// </summary>
        private readonly IList<Point> _gSpots = new List<Point>
        {
            // Top row
            new Point(p2, p1), // 0
            new Point(p3, p1), // 1
            new Point(p4, p1), // 2
            new Point(p5, p1), // 3
            new Point(p6, p1), // 4

            // Second row
            new Point(p1, p2), // 5
            new Point(p2, p2), // 6

            // Third row
            new Point(p1, p3), // 7
            new Point(p2, p3), // 8

            // Fourth row
            new Point(p1, p4), // 9
            new Point(p2, p4), // 10
            new Point(p5, p4), // 11
            new Point(p6, p4), // 12
            new Point(p7, p4), // 13

            // Fifth row
            new Point(p1, p5), // 14
            new Point(p2, p5), // 15
            new Point(p6, p5), // 16
            new Point(p7, p5), // 17

            // Sixth row
            new Point(p1, p6), // 18
            new Point(p2, p6), // 19
            new Point(p3, p6), // 20
            new Point(p4, p6), // 21
            new Point(p5, p6), // 22
            new Point(p6, p6) // 23
        };

        private readonly Color _pixelColor = Color.FromArgb(138, 255, 0);
        private readonly IList<Color> _pixelColors = new List<Color>();
        private readonly IList<RectangleAnimator> _pixels = new List<RectangleAnimator>();
        private readonly Random _rand = new Random();
        private IBitmapWithNativeSupport _bitmap;
        private int _colorIndex;
        private bool _hasAnimationsLeft;
        private int _scrollCount;

        // Variables used for the color-cycle
        private int _waitFrames;

        /// <summary>
        ///     Constructor
        /// </summary>
        public AboutForm(
            ICoreConfiguration coreConfiguration, 
            IGreenshotLanguage greenshotlanguage,
            IVersionProvider versionProvider
            ) : base(coreConfiguration, greenshotlanguage)
        {
            _greenshotlanguage = greenshotlanguage;
            _versionProvider = versionProvider;
            // Make sure our resources are removed again.
            Disposed += Cleanup;
            FormClosing += Cleanup;

            // Enable animation for this form, when we don't set this the timer doesn't start as soon as the form is loaded.
            EnableAnimation = true;
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();

            // Use the self drawn image, first we create the background to be the backcolor (as we animate from this)
            _bitmap = BitmapFactory.CreateEmpty(90, 90, PixelFormat.Format24bppRgb, BackColor);
            pictureBox1.Image = _bitmap?.NativeBitmap;

            _dpiSubscription = FormDpiHandler.OnDpiChanged.Subscribe(info =>
                {
                    pictureBox1.Size = FormDpiHandler.ScaleWithCurrentDpi(new NativeSize(90,90));
                });

            var versionInfo = $@"Greenshot {versionProvider.CurrentVersion} {(SingleExeHelper.IsRunningAsSingleExe? "SE " : "")}({OsInfo.Bits} bit)";
            if (versionProvider.IsUpdateAvailable)
            {
                versionInfo += $" latest is: {versionProvider.LatestVersion}";
            }
            lblTitle.Text = versionInfo;
            // Number of frames the pixel animation takes
            var frames = FramesForMillis(2000);
            // The number of frames the color-cycle waits before it starts
            _waitFrames = FramesForMillis(6000);

            // Every pixel is created after pixelWaitFrames frames, which is increased in the loop.
            var pixelWaitFrames = FramesForMillis(2000);
            // Create pixels
            for (var index = 0; index < _gSpots.Count; index++)
            {
                // Read the pixels in the order of the flow
                var gSpot = _gSpots[_flowOrder[index]];
                // Create the animation, first we do nothing (on the final destination)
                RectangleAnimator pixelAnimation;

                // Make the pixel grom from the middle, if this offset isn't used it looks like it's shifted
                var offset = (w - 2) / 2;

                // If the optimize for Terminal Server is set we make the animation without much ado
                if (IsTerminalServerSession)
                {
                    // No animation
                    pixelAnimation = new RectangleAnimator(new Rectangle(gSpot.X, gSpot.Y, w - 2, w - 2), new Rectangle(gSpot.X, gSpot.Y, w - 2, w - 2), 1, EasingTypes.Cubic);
                }
                else
                {
                    // Create the animation, first we do nothing (on the final destination)
                    var standingStill = new Rectangle(gSpot.X + offset, gSpot.Y + offset, 0, 0);
                    pixelAnimation = new RectangleAnimator(standingStill, standingStill, pixelWaitFrames, EasingTypes.Quintic);
                    // And than we size to the wanted size.
                    pixelAnimation.QueueDestinationLeg(new Rectangle(gSpot.X, gSpot.Y, w - 2, w - 2), frames);
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
            var pixelColorAnimator = new ColorAnimator(_pixelColor, Color.FromArgb(255, 255, 255), 6, EasingTypes.Quadratic);
            pixelColorAnimator.QueueDestinationLeg(_pixelColor, 6, EasingTypes.Quadratic, EasingModes.EaseOut);
            do
            {
                _colorFlow.Add(pixelColorAnimator.Current);
                pixelColorAnimator.Next();
            } while (pixelColorAnimator.HasNext);

            // color animation for the background
            _backgroundAnimation = new ColorAnimator(BackColor, _backColor, FramesForMillis(5000));
        }

        /// <summary>
        ///     Cleanup all the allocated resources
        /// </summary>
        private void Cleanup(object sender, EventArgs e)
        {
            if (_bitmap != null)
            {
                _bitmap.Dispose();
                _bitmap = null;
            }
            _dpiSubscription.Dispose();

        }

        /// <summary>
        ///     This is called when a link is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LinkLabelClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!(sender is LinkLabel linkLabel))
            {
                return;
            }
            try
            {
                linkLabel.LinkVisited = true;
                var processStartInfo = new ProcessStartInfo(linkLabel.Text)
                {
                    CreateNoWindow = true,
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
            }
            catch (Exception)
            {
                MessageBox.Show(string.Format(_greenshotlanguage.ErrorOpenlink, linkLabel.Text), _greenshotlanguage.Error);
            }
        }

        /// <summary>
        ///     Called from the AnimatingForm, for every frame
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
                    for (var index = _pixelColors.Count - 1; index > 0; index--)
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
            using (var graphics = Graphics.FromImage(_bitmap.NativeBitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.Clear(_backgroundAnimation.Next());

                graphics.TranslateTransform(2, -2);
                graphics.RotateTransform(20);

                using (var brush = new SolidBrush(_pixelColor))
                {
                    var index = 0;
                    // We asume there is nothing to animate in the next Animate loop
                    _hasAnimationsLeft = false;
                    // Pixels of the G
                    foreach (var pixel in _pixels)
                    {
                        brush.Color = _pixelColors[index++];
                        graphics.FillEllipse(brush, pixel.Current);
                        // If a pixel still has frames left, the hasAnimationsLeft will be true
                        _hasAnimationsLeft = _hasAnimationsLeft || pixel.HasNext;
                        pixel.Next();
                    }
                }
            }
            pictureBox1.Invalidate();
        }

        /// <summary>
        ///     CmdKey handler
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
                        var info = new StringBuilder(EnvironmentInfo.EnvironmentToString(true));
                        var screenboundsSize = DisplayInfo.ScreenBounds.Size;
                        info.AppendLine();
                        info.AppendFormat("Screen: {0} at {1}%", $"{screenboundsSize.Width} x {screenboundsSize.Height}", FormDpiHandler.ScaleWithCurrentDpi(100));
                        MessageBox.Show(info.ToString(), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    case Keys.L:
                        // TODO: Open the log file
/*
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
                                MessageBox.Show("Greenshot can't find the logfile, it should have been here: " + MainForm.LogFileLocation);
                            }
                        }

                        catch (Exception)
                        {
                            MessageBox.Show("Couldn't open the greenshot.log, it's located here: " + MainForm.LogFileLocation, "Error opening greeenshot.log", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        }
*/
                        break;
                    // TODO: Open configuration location
      //              case Keys.I:
                        //try
                        //{
                        //    using (Process.Start("\"" + IniConfig.Current.IniLocation + "\""))
                        //    {
      //                          // Ignore
                        //    }
                        //}
                        //catch (Exception)
                        //{
                        //    MessageBox.Show("Couldn't open the greenshot.ini, it's located here: " + IniConfig.Current.IniLocation, "Error opening greeenshot.ini", MessageBoxButtons.OK,
                        //        MessageBoxIcon.Asterisk);
                        //}
                        //break;
                    default:
                        return base.ProcessCmdKey(ref msg, keyData);
                }
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, $"Error handling key '{keyData}'");
            }
            return true;
        }
    }
}