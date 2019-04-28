// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Addons;
using Dapplo.CaliburnMicro;
using Dapplo.Log;
using Dapplo.Windows.Multimedia;
using Dapplo.Windows.Multimedia.Enums;
using Greenshot.Addons.Core;
using Greenshot.Addons.Resources;

namespace Greenshot.Components
{
	/// <summary>
	///     Create to fix the sometimes wrongly played sample, especially after first start from IDE
	///     See: http://www.codeproject.com/KB/audio-video/soundplayerbug.aspx?msg=2487569
	/// </summary>
	[Service(nameof(SoundHelper), nameof(CaliburnServices.ConfigurationService))]
	public class SoundHelper : IStartup, IShutdown
	{
	    private readonly ICoreConfiguration _coreConfiguration;
	    private static readonly LogSource Log = new LogSource();
		private GCHandle? _gcHandle;
		private byte[] _soundBuffer;

	    private static SoundHelper _instance;

        public SoundHelper(ICoreConfiguration coreConfiguration)
	    {
	        _coreConfiguration = coreConfiguration;
	        _instance = this;
        }

	    /// <inheritdoc />
		public void Startup()
		{
            if (_gcHandle != null)
		    {
		        return;
		    }

		    try
		    {
		        _soundBuffer = GreenshotResources.Instance.GetBytes("camera.wav", GetType());

		        if (_coreConfiguration.NotificationSound != null && _coreConfiguration.NotificationSound.EndsWith(".wav"))
		        {
		            try
		            {
		                if (File.Exists(_coreConfiguration.NotificationSound))
		                {
		                    _soundBuffer = File.ReadAllBytes(_coreConfiguration.NotificationSound);
		                }
		            }
		            catch (Exception ex)
		            {
		                Log.Warn().WriteLine("couldn't load {0}: {1}", _coreConfiguration.NotificationSound, ex.Message);
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

	    /// <summary>
	    /// Play the sound
	    /// </summary>
	    public static void Play()
	    {
            _instance.PlaySound();
	    }

        private void PlaySound() {
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

		public void Shutdown()
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