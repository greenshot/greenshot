#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System.Drawing;
using System.Windows.Forms;

#endregion

namespace GreenshotPlugin.Interfaces.Forms
{
	/// <summary>
	///     The IImageEditor is the Interface that the Greenshot ImageEditor has to implement
	/// </summary>
	public interface IImageEditor
	{
		/// <summary>
		///     Return the IWin32Window, this way Plugins have access to the HWND handles wich can be used with Win32 API calls.
		/// </summary>
		IWin32Window WindowHandle { get; }

		/// <summary>
		///     Make the ICaptureDetails from the current Surface in the EditorForm available.
		/// </summary>
		ICaptureDetails CaptureDetails { get; }

		ISurface Surface { get; set; }

		/// <summary>
		///     Get the current Image from the Editor for Exporting (save/upload etc)
		///     This is actually a wrapper which calls Surface.GetImageForExport().
		///     Don't forget to call image.Dispose() when finished!!!
		/// </summary>
		/// <returns>Bitmap</returns>
		Image GetImageForExport();

		/// <summary>
		///     Get the ToolStripMenuItem where plugins can place their Menu entrys
		/// </summary>
		/// <returns>ToolStripMenuItem</returns>
		ToolStripMenuItem GetPluginMenuItem();

		/// <summary>
		///     Get the File ToolStripMenuItem
		/// </summary>
		/// <returns>ToolStripMenuItem</returns>
		ToolStripMenuItem GetFileMenuItem();
	}
}