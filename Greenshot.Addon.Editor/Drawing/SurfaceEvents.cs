/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Drawing;

namespace Greenshot.Addon.Editor.Drawing
{

	public enum SurfaceMessageTyp
	{
		FileSaved,
		Error,
		Info,
		UploadedUri
	}

	public class SurfaceMessageEventArgs : EventArgs
	{
		public SurfaceMessageTyp MessageType
		{
			get;
			set;
		}

		public string Message
		{
			get;
			set;
		}

		public ICapture Capture
		{
			get;
			set;
		}
	}

	public class SurfaceElementEventArgs : EventArgs
	{
		public IList<IDrawableContainer> Elements
		{
			get;
			set;
		}
	}

	public class SurfaceDrawingModeEventArgs : EventArgs
	{
		public DrawingModes DrawingMode
		{
			get;
			set;
		}
	}

	public delegate void SurfaceSizeChangeEventHandler(object sender, EventArgs e);

	public delegate void SurfaceMessageEventHandler(object sender, SurfaceMessageEventArgs e);

	public delegate void SurfaceElementEventHandler(object sender, SurfaceElementEventArgs e);

	public delegate void SurfaceDrawingModeEventHandler(object sender, SurfaceDrawingModeEventArgs e);
}
