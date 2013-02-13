/*
* Greenshot - a free and open source screenshot tool
* Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
*
* For more information see: http://getgreenshot.org/
* The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Reflection;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Drawing.Drawing2D;

using Greenshot.Helpers;
using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using GreenshotPlugin.Controls;
using System.Security.Permissions;

namespace Greenshot {
	/// <summary>
	/// The about form
	/// </summary>
	public partial class AboutForm : AnimatingBaseForm {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AboutForm));
		private Bitmap gBitmap;
		private ColorAnimator backgroundAnimation;
		private List<RectangleAnimator> pixels = new List<RectangleAnimator>();
		private List<Color> colorFlow = new List<Color>();
		private List<Color> pixelColors = new List<Color>();
		private Random rand = new Random();
		private readonly Color backColor = Color.FromArgb(61, 61, 61);
		private readonly Color pixelColor = Color.FromArgb(138, 255, 0);

		// Variables used for the color-cycle
		private int waitFrames = 0;
		private int colorIndex = 0;
		private int scrollCount = 0;
		private bool hasAnimationsLeft;

		// Variables are used to define the location of the dots
		private const int w = 13;
		private const int p1 = 7;
		private const int p2 = p1 + w;
		private const int p3 = p2 + w;
		private const int p4 = p3 + w;
		private const int p5 = p4 + w;
		private const int p6 = p5 + w;
		private const int p7 = p6 + w;

		/// <summary>
		/// The location of every dot in the "G"
		/// </summary>
		private List<Point> gSpots = new List<Point>() {
             	// Top row
             	new Point(p2, p1),	// 0
             	new Point(p3, p1),  // 1
             	new Point(p4, p1),  // 2
             	new Point(p5, p1),	// 3
             	new Point(p6, p1),	// 4

             	// Second row
             	new Point(p1, p2),	// 5
             	new Point(p2, p2),	// 6

             	// Third row
             	new Point(p1, p3),	// 7
             	new Point(p2, p3),	// 8

             	// Fourth row
             	new Point(p1, p4),	// 9
             	new Point(p2, p4),	// 10
             	new Point(p5, p4),	// 11
             	new Point(p6, p4),	// 12
             	new Point(p7, p4),	// 13

             	// Fifth row
             	new Point(p1, p5),	// 14
             	new Point(p2, p5),	// 15
             	new Point(p6, p5),	// 16
             	new Point(p7, p5),	// 17

             	// Sixth row
             	new Point(p1, p6),	// 18
             	new Point(p2, p6),	// 19
             	new Point(p3, p6),	// 20
             	new Point(p4, p6),	// 21
             	new Point(p5, p6),	// 22
             	new Point(p6, p6)	// 23
             };

		//     0  1  2  3  4
		//  5  6
		//  7  8
		//  9 10       11 12 13
		// 14 15          16 17
		// 18 19 20 21 22 23

		// The order in which we draw the dots & flow the collors.
		List<int> flowOrder = new List<int>() { 4, 3, 2, 1, 0, 5, 6, 7, 8, 9, 10, 14, 15, 18, 19, 20, 21, 22, 23, 16, 17, 13, 12, 11 };

		/// <summary>
		/// Cleanup all the allocated resources
		/// </summary>
		private void Cleanup() {
			if (gBitmap != null) {
				gBitmap.Dispose();
				gBitmap = null;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public AboutForm() {
			// Make sure our resources are removed again.
			this.Disposed += delegate {
				Cleanup();
			};
			this.FormClosing += delegate {
				Cleanup();
			};

			// Enable animation for this form, when we don't set this the timer doesn't start as soon as the form is loaded.
			EnableAnimation = true;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			// Only use double-buffering when we are NOT in a Terminal Server session
			DoubleBuffered = !isTerminalServerSession;

			// Not needed for a Tool Window, but still for the task manager it's important
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();

			// Use the self drawn image, first we create the background to be the backcolor (as we animate from this)
			gBitmap = ImageHelper.CreateEmpty(90, 90, PixelFormat.Format24bppRgb, this.BackColor, 96, 96);
			this.pictureBox1.Image = gBitmap;
			Version v = Assembly.GetExecutingAssembly().GetName().Version;

			// Format is like this:  AssemblyVersion("Major.Minor.Build.Revision")]
			lblTitle.Text = "Greenshot " + v.Major + "." + v.Minor + "." + v.Build + " Build " + v.Revision + (IniConfig.IsPortable ? " Portable" : "") + (" (" + OSInfo.Bits + " bit)");

			//Random rand = new Random();

			// Number of frames the pixel animation takes
			int frames = FramesForMillis(2000);
			// The number of frames the color-cycle waits before it starts
			waitFrames = FramesForMillis(6000);

			// Every pixel is created after pixelWaitFrames frames, which is increased in the loop.
			int pixelWaitFrames = FramesForMillis(2000);
			// Create pixels
			for (int index = 0; index < gSpots.Count; index++) {
				// Read the pixels in the order of the flow
				Point gSpot = gSpots[flowOrder[index]];
				// Create the animation, first we do nothing (on the final destination)
				RectangleAnimator pixelAnimation;

				// Make the pixel grom from the middle, if this offset isn't used it looks like it's shifted
				int offset = (w - 2) / 2;

				// If the optimize for Terminal Server is set we make the animation without much ado
				if (isTerminalServerSession) {
					// No animation
					pixelAnimation = new RectangleAnimator(new Rectangle(gSpot.X, gSpot.Y, w - 2, w - 2), new Rectangle(gSpot.X, gSpot.Y, w - 2, w - 2), 1, EasingType.Cubic, EasingMode.EaseIn);
				} else {
					// Create the animation, first we do nothing (on the final destination)
					Rectangle standingStill = new Rectangle(gSpot.X + offset, gSpot.Y + offset, 0, 0);
					pixelAnimation = new RectangleAnimator(standingStill, standingStill, pixelWaitFrames, EasingType.Quintic, EasingMode.EaseIn);
					// And than we size to the wanted size.
					pixelAnimation.QueueDestinationLeg(new Rectangle(gSpot.X, gSpot.Y, w - 2, w - 2), frames);
				}
				// Increase the wait frames
				pixelWaitFrames += FramesForMillis(100);
				// Add to the list of to be animated pixels
				pixels.Add(pixelAnimation);
				// Add a color to the list for this pixel.
				pixelColors.Add(pixelColor);
			}
			// Make sure the frame "loop" knows we have to animate
			hasAnimationsLeft = true;

			// Pixel Color cycle colors, here we use a pre-animated loop which stores the values.
			ColorAnimator pixelColorAnimator = new ColorAnimator(pixelColor, Color.FromArgb(255, 255, 255), 6, EasingType.Quadratic, EasingMode.EaseIn);
			pixelColorAnimator.QueueDestinationLeg(pixelColor, 6, EasingType.Quadratic, EasingMode.EaseOut);
			do {
				colorFlow.Add(pixelColorAnimator.Current);
				pixelColorAnimator.Next();
			} while (pixelColorAnimator.hasNext);

			// color animation for the background
			backgroundAnimation = new ColorAnimator(this.BackColor, backColor, FramesForMillis(5000), EasingType.Linear, EasingMode.EaseIn);
		}

		/// <summary>
		/// This is called when a link is clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void LinkLabelClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
			LinkLabel linkLabel = sender as LinkLabel;
			if (linkLabel != null) {
				try {
					linkLabel.LinkVisited = true;
					System.Diagnostics.Process.Start(linkLabel.Text);
				} catch (Exception) {
					MessageBox.Show(Language.GetFormattedString(LangKey.error_openlink, linkLabel.Text), Language.GetString(LangKey.error));
				}
			}
		}

		/// <summary>
		/// Called from the AnimatingForm, for every frame
		/// </summary>
		protected override void Animate() {
			if (gBitmap == null) {
				return;
			}
			if (!isTerminalServerSession) {
				// Color cycle
				if (waitFrames != 0) {
					waitFrames--;
					// Check if there is something else to do, if not we return so we don't occupy the CPU
					if (!hasAnimationsLeft) {
						return;
					}
				} else if (scrollCount < (pixelColors.Count + colorFlow.Count)) {
					// Scroll colors, the scrollCount is the amount of pixels + the amount of colors to cycle.
					for (int index = pixelColors.Count - 1; index > 0; index--) {
						pixelColors[index] = pixelColors[index - 1];
					}
					// Keep adding from  the colors to cycle until there is nothing left
					if (colorIndex < colorFlow.Count) {
						pixelColors[0] = colorFlow[colorIndex++];
					}
					scrollCount++;
				} else {
					// Reset values, wait X time for the next one
					waitFrames = FramesForMillis(3000 + rand.Next(35000));
					colorIndex = 0;
					scrollCount = 0;
					// Check if there is something else to do, if not we return so we don't occupy the CPU
					if (!hasAnimationsLeft) {
						return;
					}
				}
			} else if (!hasAnimationsLeft) {
				return;
			}

			// Draw the "G"
			using (Graphics graphics = Graphics.FromImage(gBitmap)) {
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				graphics.Clear(backgroundAnimation.Next());

				graphics.TranslateTransform(2, -2);
				graphics.RotateTransform(20);

				using (SolidBrush brush = new SolidBrush(pixelColor)) {
					int index = 0;
					// We asume there is nothing to animate in the next Animate loop
					hasAnimationsLeft = false;
					// Pixels of the G
					foreach (RectangleAnimator pixel in pixels) {
						brush.Color = pixelColors[index++];
						graphics.FillEllipse(brush, pixel.Current);
						// If a pixel still has frames left, the hasAnimationsLeft will be true
						hasAnimationsLeft = hasAnimationsLeft | pixel.hasNext;
						pixel.Next();
					}
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
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
			try {
				switch (keyData) {
					case Keys.Escape:
						DialogResult = DialogResult.Cancel;
						break;
					case Keys.E:
						MessageBox.Show(EnvironmentInfo.EnvironmentToString(true));
						break;
					case Keys.L:
						try {
							if (File.Exists(MainForm.LogFileLocation)) {
								System.Diagnostics.Process.Start("\"" + MainForm.LogFileLocation + "\"");
							} else {
								MessageBox.Show("Greenshot can't find the logfile, it should have been here: " + MainForm.LogFileLocation);
							}
						} catch (Exception) {
							MessageBox.Show("Couldn't open the greenshot.log, it's located here: " + MainForm.LogFileLocation, "Error opening greeenshot.log", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						}
						break;
					case Keys.I:
						try {
							System.Diagnostics.Process.Start("\"" + IniFile.IniConfig.ConfigLocation + "\"");
						} catch (Exception) {
							MessageBox.Show("Couldn't open the greenshot.ini, it's located here: " + IniFile.IniConfig.ConfigLocation, "Error opening greeenshot.ini", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						}
						break;
					default:
						return base.ProcessCmdKey(ref msg, keyData);
				}
			} catch (Exception ex) {
				LOG.Error(string.Format("Error handling key '{0}'", keyData), ex);
			}
			return true;
		}
	}
}