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
using GreenshotPlugin.Interfaces;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     Description of AbstractProcessor.
	/// </summary>
	public abstract class AbstractProcessor : IProcessor
	{
		public virtual int CompareTo(object obj)
		{
			var other = obj as IProcessor;
			if (other == null)
			{
				return 1;
			}
			if (Priority == other.Priority)
			{
				return Description.CompareTo(other.Description);
			}
			return Priority - other.Priority;
		}

		public abstract string Designation { get; }

		public abstract string Description { get; }

		public virtual int Priority
		{
			get { return 10; }
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public virtual bool isActive
		{
			get { return true; }
		}

		public abstract bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails);

		protected virtual void Dispose(bool disposing)
		{
			//if (disposing) {}
		}
	}
}