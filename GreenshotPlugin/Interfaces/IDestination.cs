/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Greenshot.Plugin {
	public class ExportInformation {
		public string DestinationDesignation {
			get;
			set;
		}
		public string DestinationDescription {
			get;
			set;
		}

		/// <summary>
		/// Set to true to specify if the export worked.
		/// </summary>
		public bool ExportMade {
			get;
			set;
		}

		public Uri ExportedToUri {
			get;
			set;
		}

		public string ErrorMessage {
			get;
			set;
		}

		public string Filepath {
			get;
			set;
		}
	}

	/// <summary>
	/// Description of IDestination.
	/// </summary>
	public interface IDestination : IDisposable, IComparable {
		/// <summary>
		/// Simple "designation" like "File", "Editor" etc, used to store the configuration
		/// </summary>
		string Designation {
			get;
		}

		/// <summary>
		/// Description which will be shown in the settings form, destination picker etc
		/// </summary>
		string Description {
			get;
		}

		/// <summary>
		/// Priority, used for sorting
		/// </summary>
		int Priority {
			get;
		}

		/// <summary>
		/// Gets an icon for the destination
		/// </summary>
		Image DisplayIcon {
			get;
		}

		/// <summary>
		/// Returns if the destination is active
		/// </summary>
		bool IsActive {
			get;
		}

		/// <summary>
		/// Return a menu item
		/// </summary>
		/// <param name="addDynamics">Resolve the dynamic destinations too?</param>
		/// <param name="destinationClickHandler">Handler which is called when clicked</param>
		/// <returns>ToolStripMenuItem</returns>
		ToolStripMenuItem CreateMenuItem(bool addDynamics, EventHandler destinationClickHandler);

		/// <summary>
		/// Gets the ShortcutKeys for the Editor
		/// </summary>
		Keys EditorShortcutKeys {
			get;
		}
		
		/// <summary>
		/// Gets the dynamic destinations
		/// </summary>
		IEnumerable<IDestination> DynamicDestinations();

		/// <summary>
		/// Returns true if this destination can be dynamic
		/// </summary>
		bool IsDynamic {
			get;
		}

		/// <summary>
		/// Async version of the ExportCapture
		/// </summary>
		/// <param name="manuallyInitiated">true if the user selected this destination from a GUI, false if it was called as part of a process</param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>DestinationExportInformation with information, like if the destination has "exported" the capture</returns>
		Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken));
	}
}
