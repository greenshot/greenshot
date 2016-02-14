/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Linq;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// This Class is used to pass details about the capture around.
	/// The time the Capture was taken and the Title of the window (or a region of) that is captured
	/// </summary>
	public class CaptureDetails : ICaptureDetails
	{
		public string Title
		{
			get;
			set;
		}

		public string Filename
		{
			get;
			set;
		}

		public DateTime DateTime
		{
			get;
			set;
		}

		public float DpiX
		{
			get;
			set;
		}

		public float DpiY
		{
			get;
			set;
		}

		public Dictionary<string, string> MetaData
		{
			get;
		} = new Dictionary<string, string>();

		public void AddMetaData(string key, string value)
		{
			if (MetaData.ContainsKey(key))
			{
				MetaData[key] = value;
			}
			else
			{
				MetaData.Add(key, value);
			}
		}

		public CaptureMode CaptureMode
		{
			get;
			set;
		}

		public IList<IDestination> CaptureDestinations
		{
			get;
			set;
		} = new List<IDestination>();

		public Uri StoredAt
		{
			get;
			set;
		}

		public void ClearDestinations()
		{
			CaptureDestinations.Clear();
		}

		public void RemoveDestination(IDestination destination)
		{
			if (CaptureDestinations.Contains(destination))
			{
				CaptureDestinations.Remove(destination);
			}
		}

		public void AddDestination(IDestination captureDestination)
		{
			if (!CaptureDestinations.Contains(captureDestination))
			{
				CaptureDestinations.Add(captureDestination);
			}
		}

		public bool HasDestination(string designation)
		{
			return CaptureDestinations.Any(destination => designation.Equals(destination.Designation));
		}

		public CaptureDetails()
		{
			DateTime = DateTime.Now;
		}
	}
}