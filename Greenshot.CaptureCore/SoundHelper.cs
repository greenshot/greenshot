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
using System.IO;
using System.Media;
using Dapplo.Config.Ini;
using Dapplo.Log;
using Dapplo.Utils.Embedded;
using Greenshot.Addon.Configuration;

namespace Greenshot.CaptureCore
{
	/// <summary>
	/// Soundhelper
	/// Create to fix the sometimes wrongly played sample, especially after first start from IDE
	/// See: http://www.codeproject.com/KB/audio-video/soundplayerbug.aspx?msg=2487569
	/// </summary>
	public static class SoundHelper
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static SoundPlayer _soundPlayer;

		public static void Initialize()
		{
			try
			{
				if (conf.NotificationSound != null && conf.NotificationSound.EndsWith(".wav"))
				{
					if (File.Exists(conf.NotificationSound))
					{
						_soundPlayer = new SoundPlayer
						{
							SoundLocation = conf.NotificationSound
						};
						_soundPlayer.LoadAsync();
						return;
					}
				}

				_soundPlayer = new SoundPlayer
				{
					Stream = typeof(SoundHelper).Assembly.GetEmbeddedResourceAsStream("camera.wav")
				};
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Error initializing.");
			}
		}

		/// <summary>
		/// Play the sound async (is wrapeed)
		/// </summary>
		/// <returns></returns>
		public static void Play()
		{
			_soundPlayer?.Play();
		}

		public static void Deinitialize()
		{
			_soundPlayer.Dispose();
			_soundPlayer = null;
		}
	}
}