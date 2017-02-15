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

using System;
using System.Collections.Generic;
using GreenshotPlugin.Interfaces;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     This Class is used to pass details about the capture around.
	///     The time the Capture was taken and the Title of the window (or a region of) that is captured
	/// </summary>
	public class CaptureDetails : ICaptureDetails
	{
		public CaptureDetails()
		{
			DateTime = DateTime.Now;
		}

		public string Title { get; set; }

		public string Filename { get; set; }

		public DateTime DateTime { get; set; }

		public float DpiX { get; set; }

		public float DpiY { get; set; }

		public Dictionary<string, string> MetaData { get; } = new Dictionary<string, string>();

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

		public CaptureMode CaptureMode { get; set; }

		public List<IDestination> CaptureDestinations { get; set; } = new List<IDestination>();

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
			foreach (var destination in CaptureDestinations)
			{
				if (designation.Equals(destination.Designation))
				{
					return true;
				}
			}
			return false;
		}
	}
}