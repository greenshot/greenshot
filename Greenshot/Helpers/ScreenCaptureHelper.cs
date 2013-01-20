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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using Greenshot;
using Greenshot.Configuration;
using Greenshot.Plugin;
using GreenshotPlugin.UnmanagedHelpers;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of ScreenCaptureHelper.
	/// </summary>
	public class ScreenCaptureHelper {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ScreenCaptureHelper));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private const int ALIGNMENT = 8;
		private IntPtr hWndDesktop = IntPtr.Zero;
		private IntPtr hDCDesktop = IntPtr.Zero;
		private IntPtr hDCDest = IntPtr.Zero;
		private IntPtr hDIBSection = IntPtr.Zero;
		private IntPtr hOldObject = IntPtr.Zero;
		private int framesPerSecond;
		private Thread backgroundTask;
		private bool stop = false;
		private AVIWriter aviWriter;
		private WindowDetails recordingWindow;
		private Rectangle recordingRectangle;
		public bool RecordMouse = false;
		private Size recordingSize;
		private IntPtr bits0 = IntPtr.Zero; //pointer to the raw bits that make up the bitmap.
		private Bitmap GDIBitmap;
		private string filename = null;
		private Stopwatch stopwatch = new Stopwatch();
		private bool disabledDWM = false;

		private ScreenCaptureHelper() {
			if (DWM.isDWMEnabled()) {
				// with DWM Composition disabled the capture goes ~2x faster
				DWM.DisableComposition();
				disabledDWM = true;
			}
		}
		public ScreenCaptureHelper(Rectangle recordingRectangle) : this() {
			this.recordingRectangle = recordingRectangle;
		}
		public ScreenCaptureHelper(WindowDetails recordingWindow) : this() {
			this.recordingWindow = recordingWindow;
		}

		/// <summary>
		/// Helper method to create an exception that might explain what is wrong while capturing
		/// </summary>
		/// <param name="method">string with current method</param>
		/// <param name="captureBounds">Rectangle of what we want to capture</param>
		/// <returns></returns>
		private static Exception CreateCaptureException(string method, Size size) {
			Exception exceptionToThrow = User32.CreateWin32Exception(method);
			if (size != Size.Empty) {
				exceptionToThrow.Data.Add("Height", size.Height);
				exceptionToThrow.Data.Add("Width", size.Width);
			}
			return exceptionToThrow;
		}
		
		/// <summary>
		/// Start the recording
		/// </summary>
		/// <param name="framesPerSecond"></param>
		/// <returns></returns>
		public bool Start(int framesPerSecond) {
			if (recordingWindow != null) {
				string windowTitle = Regex.Replace(recordingWindow.Text, @"[^\x20\d\w]", "");
				if (string.IsNullOrEmpty(windowTitle)) {
					windowTitle = "greenshot-recording";
				}
				filename = Path.Combine(conf.OutputFilePath, windowTitle + ".avi");
				
			} else {
				filename = Path.Combine(conf.OutputFilePath, "greenshot-recording.avi");
			}
			if (File.Exists(filename)) {
				try {
					File.Delete(filename);
				} catch {}
			}
			LOG.InfoFormat("Capturing to {0}", filename);
						
			if (recordingWindow != null) {
				LOG.InfoFormat("Starting recording Window '{0}', {1}", recordingWindow.Text, recordingWindow.WindowRectangle);
				recordingSize = recordingWindow.WindowRectangle.Size;
			} else {
				LOG.InfoFormat("Starting recording rectangle {0}", recordingRectangle);
				recordingSize = recordingRectangle.Size;
			}
			//if (recordingSize.Width % ALIGNMENT > 0) {
			//	LOG.InfoFormat("Correcting width to be factor alignment, {0} => {1}", recordingSize.Width, recordingSize.Width + (ALIGNMENT - (recordingSize.Width % ALIGNMENT)));
			//	recordingSize = new Size(recordingSize.Width + (ALIGNMENT - (recordingSize.Width % ALIGNMENT)), recordingSize.Height);
			//}
			//if (recordingSize.Height % ALIGNMENT > 0) {
			//	LOG.InfoFormat("Correcting Height to be factor alignment, {0} => {1}", recordingSize.Height, recordingSize.Height + (ALIGNMENT - (recordingSize.Height % ALIGNMENT)));
			//	recordingSize = new Size(recordingSize.Width, recordingSize.Height + (ALIGNMENT - (recordingSize.Height % ALIGNMENT)));
			//}
			this.framesPerSecond = framesPerSecond;
			// "P/Invoke" Solution for capturing the screen
			hWndDesktop = User32.GetDesktopWindow();
			// get te hDC of the target window
			hDCDesktop = User32.GetWindowDC(hWndDesktop);
			// Make sure the last error is set to 0
			Win32.SetLastError(0);

			// create a device context we can copy to
			hDCDest = GDI32.CreateCompatibleDC(hDCDesktop);
			// Check if the device context is there, if not throw an error with as much info as possible!
			if (hDCDest == IntPtr.Zero) {
				// Get Exception before the error is lost
				Exception exceptionToThrow = CreateCaptureException("CreateCompatibleDC", recordingSize);
				// Cleanup
				User32.ReleaseDC(hWndDesktop, hDCDesktop);
				// throw exception
				throw exceptionToThrow;
			}

			// Create BitmapInfoHeader for CreateDIBSection
			BitmapInfoHeader bitmapInfoHeader = new BitmapInfoHeader(recordingSize.Width, recordingSize.Height, 32);

			// Make sure the last error is set to 0
			Win32.SetLastError(0);

			// create a bitmap we can copy it to, using GetDeviceCaps to get the width/height
			hDIBSection = GDI32.CreateDIBSection(hDCDesktop, ref bitmapInfoHeader, BitmapInfoHeader.DIB_RGB_COLORS, out bits0, IntPtr.Zero, 0);

			if (hDIBSection == IntPtr.Zero) {
				// Get Exception before the error is lost
				Exception exceptionToThrow = CreateCaptureException("CreateDIBSection", recordingSize);
				exceptionToThrow.Data.Add("hdcDest", hDCDest.ToInt32());
				exceptionToThrow.Data.Add("hdcSrc", hDCDesktop.ToInt32());
				
				// clean up
				GDI32.DeleteDC(hDCDest);
				User32.ReleaseDC(hWndDesktop, hDCDesktop);

				// Throw so people can report the problem
				throw exceptionToThrow;
			}
			// Create a GDI Bitmap so we can use GDI and GDI+ operations on the same memory
			GDIBitmap = new Bitmap(recordingSize.Width, recordingSize.Height, 32, PixelFormat.Format32bppArgb, bits0);
			// select the bitmap object and store the old handle
			hOldObject = GDI32.SelectObject(hDCDest, hDIBSection);
			stop = false;
			
			aviWriter = new AVIWriter();
			// Comment the following 2 lines to make the user select it's own codec
			aviWriter.Codec = "msvc";
			aviWriter.Quality = 10000;

			aviWriter.FrameRate = framesPerSecond;
			if (aviWriter.Open(filename, recordingSize.Width, recordingSize.Height)) {
				// Start update check in the background
				backgroundTask = new Thread (new ThreadStart(CaptureFrame));
				backgroundTask.IsBackground = true;
				backgroundTask.Name = "Capture video";
				backgroundTask.Start();
				return true;
			} else {
				// Cancel
				aviWriter.Dispose();
				aviWriter = null;
			}
			return false;
		}
		
		/// <summary>
		/// Do the actual frame capture
		/// </summary>
		private void CaptureFrame() {
			int MSBETWEENCAPTURES = 1000/framesPerSecond;
			int msToNextCapture = MSBETWEENCAPTURES;
			stopwatch.Reset();
			while (!stop)  {
				stopwatch.Start();
				Point captureLocation;
				if (recordingWindow != null) {
					recordingWindow.Reset();
					captureLocation = recordingWindow.Location;
				} else {
					captureLocation = new Point(recordingRectangle.X,  recordingRectangle.Y);
				}
				// "Capture"
				GDI32.BitBlt(hDCDest, 0, 0, recordingSize.Width, recordingSize.Height, hDCDesktop, captureLocation.X,  captureLocation.Y, CopyPixelOperation.SourceCopy);
				//GDI32.BitBlt(hDCDest, 0, 0, recordingSize.Width, recordingSize.Height, hDCDesktop, captureLocation.X, captureLocation.Y, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

				// Mouse
				if (RecordMouse) {
					CursorInfo cursorInfo = new CursorInfo(); 
					cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
					Point mouseLocation = Cursor.Position;
					mouseLocation.Offset(-captureLocation.X, -captureLocation.Y);
					if (User32.GetCursorInfo(out cursorInfo)) {
						User32.DrawIcon(hDCDest, mouseLocation.X, mouseLocation.Y, cursorInfo.hCursor);
					}
				}
				// add to avi
				try {
					aviWriter.AddLowLevelFrame(bits0);
				} catch (Exception) {
					LOG.Error("Error adding frame to avi, stopping capturing.");
					break;
				}

				int restTime = (int)(msToNextCapture - stopwatch.ElapsedMilliseconds);

				// Set time to next capture, we correct it if needed later.
				msToNextCapture = MSBETWEENCAPTURES;
				if (restTime > 0) {
					// We were fast enough, we wait for next capture
					Thread.Sleep(restTime);
				} else if (restTime < 0) {
					// Compensating, as we took to long
					int framesToSkip = ((-restTime) / MSBETWEENCAPTURES);
					int leftoverMillis = (-restTime) % MSBETWEENCAPTURES;
					//LOG.InfoFormat("Adding {0} empty frames to avi, leftover millis is {1}, sleeping {2} (of {3} total)", framesToSkip, leftover, sleepMillis, MSBETWEENCAPTURES);
					aviWriter.AddEmptyFrames(framesToSkip);

					// check how bad it is, if we only missed our target by a few millis we hope the next capture corrects this
					if (leftoverMillis > 0 && leftoverMillis <= 2) {
						// subtract the leftover from the millis to next capture, do nothing else
						msToNextCapture -= leftoverMillis;
					} else if (leftoverMillis > 0) {
						// it's more, we add an empty frame
						aviWriter.AddEmptyFrames(1);
						// we sleep to the next time and
						int sleepMillis = MSBETWEENCAPTURES - leftoverMillis;
						// Sleep to next capture
						Thread.Sleep(sleepMillis);
					}
				}
				stopwatch.Reset();
			}
			Cleanup();
		}
		
		/// <summary>
		/// Stop the recording, after the next frame
		/// </summary>
		public void Stop() {
			stop = true;
			if (backgroundTask != null) {
				backgroundTask.Join();
			}
			Cleanup();
		}

		/// <summary>
		///  Free resources
		/// </summary>
		private void Cleanup() {
			if (hOldObject != IntPtr.Zero && hDCDest != IntPtr.Zero) {
				// restore selection (old handle)
				GDI32.SelectObject(hDCDest, hOldObject);
				GDI32.DeleteDC(hDCDest);
			}
			if (hDCDesktop != IntPtr.Zero) {
				User32.ReleaseDC(hWndDesktop, hDCDesktop);
			}
			if (hDIBSection != IntPtr.Zero) {
				// free up the Bitmap object
				GDI32.DeleteObject(hDIBSection);
			}

			if (disabledDWM) {
				DWM.EnableComposition();
			}
			if (aviWriter != null) {
				aviWriter.Dispose();
				aviWriter = null;

				string ffmpegexe = PluginUtils.GetExePath("ffmpeg.exe");
				if (ffmpegexe != null) {
					try {
						string webMFile = filename.Replace(".avi", ".webm");
						string arguments = "-i \"" + filename + "\" -codec:v libvpx -quality good -cpu-used 0 -b:v 1000k -qmin 10 -qmax 42 -maxrate 1000k -bufsize 4000k -threads 4 \"" + webMFile + "\"";
						LOG.DebugFormat("Starting {0} with arguments {1}", ffmpegexe, arguments);
						ProcessStartInfo processStartInfo = new ProcessStartInfo(ffmpegexe, arguments);
						processStartInfo.CreateNoWindow = false;
						processStartInfo.RedirectStandardOutput = false;
						processStartInfo.UseShellExecute = false;
						Process process = Process.Start(processStartInfo);
						process.WaitForExit();
						if (process.ExitCode == 0) {
							MessageBox.Show("Recording written to " + webMFile);
						}
					} catch (Exception ex) {
						MessageBox.Show("Recording written to " + filename + " couldn't convert due to an error: " + ex.Message);
					}
				} else {
					MessageBox.Show("Recording written to " + filename);
				}
			}
		}
	}
}
