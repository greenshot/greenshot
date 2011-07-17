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
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

using Greenshot.UnmanagedHelpers;

/// <summary>
/// Create to fix the sometimes wrongly played sample, especially after first start from IDE
/// See: http://www.codeproject.com/KB/audio-video/soundplayerbug.aspx?msg=2487569
/// </summary>
namespace Greenshot.Helpers {
	/// <summary>
	/// Description of SoundHelper.
	/// </summary>
	public class SoundHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SoundHelper));

		private static GCHandle? gcHandle = null;
	    private static byte[] soundBuffer = null;

		private SoundHelper() {
			// Prevent instanciating
		}
		
		public static void Initialize() {
	    	try {
				ResourceManager resources = new ResourceManager("Greenshot.Sounds", Assembly.GetExecutingAssembly());
				soundBuffer = (byte[])resources.GetObject("camera");
				// Pin sound so it can't be moved by the Garbage Collector, this was the cause for the bad sound
				gcHandle = GCHandle.Alloc(soundBuffer, GCHandleType.Pinned);
	    	} catch (Exception e) {
	    		LOG.Error("Error initializing.", e);
	    	}
		}
		
		public static void Play() {
			SoundFlags flags = SoundFlags.SND_ASYNC | SoundFlags.SND_MEMORY;
			
			try {
				if (soundBuffer != null) {
					WinMM.PlaySound(gcHandle.Value.AddrOfPinnedObject(), (UIntPtr)0, (uint)flags);
				} else {
					WinMM.PlaySound((byte[])null, (UIntPtr)0, (uint)flags);
				}
			} catch (Exception e) {
	    		LOG.Error("Error in play.", e);
	    	}
	    }

	    public static void Deinitialize() {
	    	try {
				if (gcHandle != null) {
					WinMM.PlaySound((byte[])null, (UIntPtr)0, (uint)0);
					gcHandle.Value.Free();
					gcHandle = null;
				}
	    	} catch (Exception e) {
	    		LOG.Error("Error in deinitialize.", e);
	    	}
	    }

		[Flags]
		public enum SoundFlags : int {
			SND_SYNC = 0x0000,			// play synchronously (default)
			SND_ASYNC = 0x0001,			// play asynchronously
			SND_NODEFAULT = 0x0002,		// silence (!default) if sound not found
			SND_MEMORY = 0x0004,		// pszSound points to a memory file
			SND_LOOP = 0x0008,			// loop the sound until next sndPlaySound
			SND_NOSTOP = 0x0010,		// don't stop any currently playing sound
			SND_NOWAIT = 0x00002000,	// don't wait if the driver is busy
			SND_ALIAS = 0x00010000,		// name is a registry alias
			SND_ALIAS_ID = 0x00110000,	// alias is a predefined id
			SND_FILENAME = 0x00020000,	// name is file name
		}
	}
}
