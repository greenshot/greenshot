/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Windows.Forms;

namespace Greenshot.Plugin {
	/// <summary>
	/// The capture mode for Greenshot
	/// </summary>
	public enum CaptureMode { None, Region, FullScreen, ActiveWindow, Window, LastRegion, Clipboard, File, IE };
	
	/// <summary>
	/// The destinations for the capture, these will be set during capture and can be modified by plugins
	/// </summary>
	public enum CaptureDestination {File, FileWithDialog, Clipboard, Printer, Editor, EMail};

	/// <summary>
	/// Handler for the MakeCapture in ICaptureHost
	/// </summary>
	public delegate void CaptureHandler(object sender, CaptureTakenEventArgs e);

	/// <summary>
	/// The ICaptureHost is more or less the Interface that "Greenshot" itself implements (the MainForm)
	/// over this Interface it is possible to register to the main ContextMenu or pass a Bitmap for processing
	/// </summary>
	public interface ICaptureHost {
		/// <summary>
		/// Process a bitmap like it was captured
		/// </summary>
		void HandleCapture(Bitmap bitmap);

		/// <summary>
		/// Make Capture with default destinations
		/// </summary>
		/// <param name="mode">CaptureMode</param>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		void MakeCapture(CaptureMode mode, bool captureMouseCursor);
		
		/// <summary>
		/// Make Capture with specified destinations
		/// </summary>
		/// <param name="mode">CaptureMode</param>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		/// <param name="captureDestinations">List<CaptureDestination> with destinations</param>
		void MakeCapture(CaptureMode mode, bool captureMouseCursor, List<CaptureDestination> captureDestinations);
		
		/// <summary>
		/// Make Capture with specified Handler
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		/// <param name="captureHandler">CaptureHandler delegate</param>
		void MakeCapture(CaptureMode mode, bool captureMouseCursor, CaptureHandler captureHandler);

		/// <summary>
		/// Make capture of window
		/// </summary>
		/// <param name="window">WindowDetails of the window to capture</param>
		//void MakeCapture(WindowDetails window);
		
		/// <summary>
		/// Make capture of window
		/// </summary>
		/// <param name="window">WindowDetails of the window to capture</param>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		//void MakeCapture(WindowDetails window,  CaptureHandler captureHandler);
	}
	
	/// <summary>
	/// Details for the capture, like the window title and date/time etc.
	/// </summary>
	public interface ICaptureDetails {
		string Filename {
			get;
			set;
		}
		string Title {
			get;
			set;
		}
		
		DateTime DateTime {
			get;
			set;
		}
		
		List<CaptureDestination> CaptureDestinations {
			get;
			set;
		}
		
		Dictionary<string, string> MetaData {
			get;
		}
		
		/// <summary>
		/// Helper method to prevent complex code which needs to check every key
		/// </summary>
		/// <param name="key">The key for the meta-data</param>
		/// <param name="value">The value for the meta-data</param>
		void AddMetaData(string key, string value);
		
		void ClearDestinations();
		void RemoveDestination(CaptureDestination captureDestination);
		void AddDestination(CaptureDestination captureDestination);

		CaptureHandler CaptureHandler {
			get;
			set;
		}
				
		CaptureMode CaptureMode {
			get;
			set;
		}
		
		float DpiX {
			get;
			set;
		}
		float DpiY {
			get;
			set;
		}
	}

	/// <summary>
	/// The interface to the Capture object, so Plugins can use it.
	/// </summary>
	public interface ICapture : IDisposable {
		// The Capture Details
		ICaptureDetails CaptureDetails {
			get;
			set;
		}

		// The captured Image
		Image Image {
			get;
			set;
		}
		
		Rectangle ScreenBounds {
			get;
			set;
		}
		
		Icon Cursor {
			get;
			set;
		}
		
		// Boolean to specify if the cursor is available
		bool CursorVisible {
			get;
			set;
		}
		
		Point CursorLocation {
			get;
			set;
		}
		
		Point Location {
			get;
			set;
		}
		
		/// <summary>
		/// Crops the capture to the specified rectangle (with Bitmap coordinates!)
		/// </summary>
		/// <param name="cropRectangle">Rectangle with bitmap coordinates</param>
		bool Crop(Rectangle cropRectangle);

		/// <summary>
		/// Apply a translate to the mouse location.
		/// e.g. needed for crop
		/// </summary>
		/// <param name="x">x coordinates to move the mouse</param>
		/// <param name="y">y coordinates to move the mouse</param>
		void MoveMouseLocation(int x, int y);
	}

}
