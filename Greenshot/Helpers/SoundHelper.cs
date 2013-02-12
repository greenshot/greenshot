/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

using GreenshotPlugin.UnmanagedHelpers;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using System.IO;

/// <summary>
/// Create to fix the sometimes wrongly played sample, especially after first start from IDE
/// See: http://www.codeproject.com/KB/audio-video/soundplayerbug.aspx?msg=2487569
/// </summary>
namespace Greenshot.Helpers {
	/// <summary>
	/// Description of SoundHelper.
	/// </summary>
	public static class SoundHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SoundHelper));
        private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static GCHandle? gcHandle = null;
	    private static byte[] soundBuffer = null;
		
		public static void Initialize() {
			if (gcHandle == null) {
				try {
					ResourceManager resources = new ResourceManager("Greenshot.Sounds", Assembly.GetExecutingAssembly());
					soundBuffer = (byte[])resources.GetObject("camera");

					if (conf.NotificationSound != null && conf.NotificationSound.EndsWith(".wav")) {
						try {
							if (File.Exists(conf.NotificationSound)) {
								soundBuffer = File.ReadAllBytes(conf.NotificationSound);
							}
						} catch (Exception ex) {
							LOG.WarnFormat("couldn't load {0}: {1}", conf.NotificationSound, ex.Message);
						}
					}
					// Pin sound so it can't be moved by the Garbage Collector, this was the cause for the bad sound
					gcHandle = GCHandle.Alloc(soundBuffer, GCHandleType.Pinned);
				} catch (Exception e) {
					LOG.Error("Error initializing.", e);
				}
			}
		}
		
		public static void Play() {
            if (soundBuffer != null) {
                //Thread playSoundThread = new Thread(delegate() {
                SoundFlags flags = SoundFlags.SND_ASYNC | SoundFlags.SND_MEMORY | SoundFlags.SND_NOWAIT | SoundFlags.SND_NOSTOP;
                try {
                    WinMM.PlaySound(gcHandle.Value.AddrOfPinnedObject(), (UIntPtr)0, (uint)flags);
                } catch (Exception e) {
                    LOG.Error("Error in play.", e);
                }
                //});
                //playSoundThread.Name = "Play camera sound";
                //playSoundThread.IsBackground = true;
                //playSoundThread.Start();
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
	}
}
