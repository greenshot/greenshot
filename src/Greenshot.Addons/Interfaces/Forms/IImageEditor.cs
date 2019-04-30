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

using System.Windows.Forms;
using Greenshot.Gfx;

namespace Greenshot.Addons.Interfaces.Forms
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

		/// <summary>
		/// The surface for this editor
		/// </summary>
		ISurface Surface { get; set; }

		/// <summary>
		///     Get the current Image from the Editor for Exporting (save/upload etc)
		///     This is actually a wrapper which calls Surface.GetBitmapForExport().
		///     Don't forget to call image.Dispose() when finished!!!
		/// </summary>
		/// <returns>IBitmapWithNativeSupport</returns>
		IBitmapWithNativeSupport GetImageForExport();

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