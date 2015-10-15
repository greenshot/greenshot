/*
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

using Dapplo.Config.Ini;
using GreenshotPlugin.Configuration;
using log4net;
using System;
using System.IO;
using System.Media;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Helpers
{
	/// <summary>
	/// Soundhelper
	/// Create to fix the sometimes wrongly played sample, especially after first start from IDE
	/// See: http://www.codeproject.com/KB/audio-video/soundplayerbug.aspx?msg=2487569
	/// </summary>
	public static class SoundHelper
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (SoundHelper));
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static SoundPlayer soundPlayer;

		public static void Initialize()
		{
			try
			{
				if (conf.NotificationSound != null && conf.NotificationSound.EndsWith(".wav"))
				{
					if (File.Exists(conf.NotificationSound))
					{
						soundPlayer = new SoundPlayer(conf.NotificationSound);
						return;
					}
				}
				var resources = new ResourceManager("Greenshot.Sounds", Assembly.GetExecutingAssembly());

				using (var stream = new MemoryStream((byte[])resources.GetObject("camera")))
				{
					soundPlayer = new SoundPlayer(stream);
				}
			}
			catch (Exception e)
			{
				LOG.Error("Error initializing.", e);
			}
		}

		/// <summary>
		/// Play the sound async (is wrapeed)
		/// </summary>
		/// <returns></returns>
		public static void Play(CancellationToken token = default(CancellationToken))
		{
			if (soundPlayer != null)
			{
				soundPlayer.Play();
			}
		}

		public static void Deinitialize()
		{
			soundPlayer.Dispose();
			soundPlayer = null;
		}
	}
}