/*
* Greenshot - a free and open source screenshot tool
* Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot {
       /// <summary>
       /// The about form
       /// </summary>
       public partial class AboutForm : BaseForm {
             private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AboutForm));
             private Bitmap gBitmap = new Bitmap(90, 90, PixelFormat.Format32bppRgb);
             private ColorAnimator backgroundColor;
             private List<RectangleAnimator> pixels = new List<RectangleAnimator>();
             private IntAnimator angleAnimator;
             
             private const int w = 13;
             private const int p1 = 7;
             private const int p2 = p1+w;
             private const int p3 = p2+w;
             private const int p4 = p3+w;
             private const int p5 = p4+w;
             private const int p6 = p5+w;
             private const int p7 = p6+w;
             
             private List<Point> gSpots = new List<Point>() {
             	// Top row
             	new Point(p2, p1),
             	new Point(p3, p1),
             	new Point(p4, p1),
             	new Point(p5, p1),
             	new Point(p6, p1),

             	// Second row
             	new Point(p1, p2),
             	new Point(p2, p2),

             	// Third row
             	new Point(p1, p3),
             	new Point(p2, p3),

             	// Fourth row
             	new Point(p1, p4),
             	new Point(p2, p4),
             	new Point(p5, p4),
             	new Point(p6, p4),
             	new Point(p7, p4),

             	// Fifth row
             	new Point(p1, p5),
             	new Point(p2, p5),
             	new Point(p6, p5),
             	new Point(p7, p5),

             	// Sixth row
             	new Point(p1, p6),
             	new Point(p2, p6),
             	new Point(p3, p6),
             	new Point(p4, p6),
             	new Point(p5, p6),
             	new Point(p6, p6)
             };

             public AboutForm() {
                    EnableAnimation = true;
                    //
                    // The InitializeComponent() call is required for Windows Forms designer support.
                    //
                    InitializeComponent();
                    DoubleBuffered = !OptimizeForTerminalServer;

                    // Not needed for a Tool Window, but still for the task manager it's important
                    this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();

                    // Use the self drawn image
                    this.pictureBox1.Image = gBitmap;
                    Version v = Assembly.GetExecutingAssembly().GetName().Version;

                    // Format is like this:  AssemblyVersion("Major.Minor.Build.Revision")]
                    lblTitle.Text = "Greenshot " + v.Major + "." + v.Minor + "." + v.Build + " Build " + v.Revision + (IniConfig.IsPortable?" Portable":"") + (" (" + OSInfo.Bits +" bit)");

                    //Random rand = new Random();
                    
                    // Number of frames the "fade-in" takes
                    int frames = CalculateFrames(3000);

                    // Create pixels
                    foreach (Point gSpot in gSpots) {
                           RectangleAnimator pixelAnimation = new RectangleAnimator(new Rectangle(p4, p3, 0, 0), new Rectangle(gSpot.X, gSpot.Y, w-2, w-2), frames, EasingType.Sine, EasingMode.EaseIn);
                           pixels.Add(pixelAnimation);
                    }
                    
                    // color animation
                    backgroundColor = new ColorAnimator(this.BackColor, Color.FromArgb(61, 61, 61), frames, EasingType.Linear, EasingMode.EaseIn);
                    // Angle animation
                    angleAnimator = new IntAnimator(-30, 20, frames, EasingType.Sine, EasingMode.EaseIn);
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
 
             protected override void Animate() {
                    using (Graphics graphics = Graphics.FromImage(gBitmap)) {
                           graphics.SmoothingMode = SmoothingMode.HighQuality;
                           graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                           graphics.CompositingQuality = CompositingQuality.HighQuality;
                           graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                           graphics.Clear(backgroundColor.Next());
                           
                           graphics.TranslateTransform(2, -2);
                           graphics.RotateTransform(angleAnimator.Next());

                           using (SolidBrush brush = new SolidBrush(Color.FromArgb(138, 255, 0))) {
                                  foreach (RectangleAnimator pixel in pixels) {
                                        graphics.FillEllipse(brush, pixel.Current);
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
                                               if (File.Exists( MainForm.LogFileLocation)) {
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