﻿/*
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

using Greenshot.Plugin;
using GreenshotPlugin.Interfaces;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of AbstractProcessor.
	/// </summary>
	public abstract class AbstractProcessor : IProcessor {
		
		public virtual int CompareTo(object obj) {
			IProcessor other = obj as IProcessor;
			if (other == null) {
				return 1;
			}
			if (Priority == other.Priority) {
				return Description.CompareTo(other.Description);
			}
			return Priority - other.Priority;
		}

		public abstract string Designation {
			get;
		}

		public abstract string Description {
			get;
		}

		public virtual int Priority {
			get {
				return 10;
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			//if (disposing) {}
		}

		public virtual bool isActive {
			get {
				return true;
			}
		}

		public abstract bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails);
	}
}
