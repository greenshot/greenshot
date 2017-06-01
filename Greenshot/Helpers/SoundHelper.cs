﻿#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using Dapplo.Log;
using Dapplo.Windows.Multimedia;

#endregion

namespace Greenshot.Helpers
{
	/// <summary>
	///     Create to fix the sometimes wrongly played sample, especially after first start from IDE
	///     See: http://www.codeproject.com/KB/audio-video/soundplayerbug.aspx?msg=2487569
	/// </summary>
	public static class SoundHelper
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
		private static GCHandle? _gcHandle;
		private static byte[] _soundBuffer;

		public static void Initialize()
		{
			if (_gcHandle == null)
			{
				try
				{
					var resources = new ResourceManager("Greenshot.Sounds", Assembly.GetExecutingAssembly());
					_soundBuffer = (byte[]) resources.GetObject("camera");

					if (CoreConfig.NotificationSound != null && CoreConfig.NotificationSound.EndsWith(".wav"))
					{
						try
						{
							if (File.Exists(CoreConfig.NotificationSound))
							{
								_soundBuffer = File.ReadAllBytes(CoreConfig.NotificationSound);
							}
						}
						catch (Exception ex)
						{
							Log.Warn().WriteLine("couldn't load {0}: {1}", CoreConfig.NotificationSound, ex.Message);
						}
					}
					// Pin sound so it can't be moved by the Garbage Collector, this was the cause for the bad sound
					_gcHandle = GCHandle.Alloc(_soundBuffer, GCHandleType.Pinned);
				}
				catch (Exception e)
				{
					Log.Error().WriteLine(e, "Error initializing.");
				}
			}
		}

		public static void Play()
		{
			if (_soundBuffer == null)
			{
				return;
			}
			var soundFlags = SoundSettings.Async | SoundSettings.Memory | SoundSettings.NoWait | SoundSettings.NoStop;
			try
			{
				if (_gcHandle != null)
				{
					WinMm.Play(_gcHandle.Value.AddrOfPinnedObject(), soundFlags);
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Error in play.");
			}
		}

		public static void Deinitialize()
		{
			try
			{
			    if (_gcHandle == null)
			    {
			        return;
			    }
			    WinMm.StopPlaying();
			    _gcHandle.Value.Free();
			    _gcHandle = null;
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Error in deinitialize.");
			}
		}
	}
}