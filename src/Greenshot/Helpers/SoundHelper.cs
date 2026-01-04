/*
 * Greenshot - a free and open source screenshot tool
 * Copyright © 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.IO;
using Dapplo.Windows.Multimedia;
using Dapplo.Windows.Multimedia.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Create to fix the sometimes wrongly played sample, especially after first start from IDE
    /// See: https://www.codeproject.com/KB/audio-video/soundplayerbug.aspx?msg=2487569
    /// </summary>
    public static class SoundHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SoundHelper));
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
        private static GCHandle? _gcHandle;
        private static byte[] _soundBuffer;

        public static void Initialize()
        {
            if (_gcHandle == null)
            {
                try
                {
                    ResourceManager resources = new ResourceManager("Greenshot.Sounds", Assembly.GetExecutingAssembly());
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
                            Log.WarnFormat("couldn't load {0}: {1}", CoreConfig.NotificationSound, ex.Message);
                        }
                    }

                    // Pin sound so it can't be moved by the Garbage Collector, this was the cause for the bad sound
                    _gcHandle = GCHandle.Alloc(_soundBuffer, GCHandleType.Pinned);
                }
                catch (Exception e)
                {
                    Log.Error("Error initializing.", e);
                }
            }
        }

        public static void Play()
        {
            if (_soundBuffer != null)
            {
                //Thread playSoundThread = new Thread(delegate() {
                var flags = SoundSettings.Async | SoundSettings.Memory| SoundSettings.NoWait| SoundSettings.NoStop;
                try
                {
                    if (_gcHandle != null) WinMm.Play(_gcHandle.Value.AddrOfPinnedObject(), flags);
                }
                catch (Exception e)
                {
                    Log.Error("Error in play.", e);
                }

                //});
                //playSoundThread.Name = "Play camera sound";
                //playSoundThread.IsBackground = true;
                //playSoundThread.Start();
            }
        }

        public static void Deinitialize()
        {
            try
            {
                if (_gcHandle != null)
                {
                    WinMm.StopPlaying();
                    _gcHandle.Value.Free();
                    _gcHandle = null;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error in de-initialize.", e);
            }
        }
    }
}