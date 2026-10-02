/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using Dapplo.Windows.Multimedia;
using Greenshot.Base.Core;
using Dapplo.Ini;
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
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
        /// <summary>
        /// The name of the embedded resource with the default sound (sounds\camera.wav)
        /// </summary>
        private const string CameraSoundResource = "Greenshot.Sounds.camera";
        private static byte[] _soundBuffer;

        public static void Initialize()
        {
            if (_soundBuffer == null)
            {
                try
                {
                    _soundBuffer = EmbeddedResources.GetBytes(typeof(SoundHelper).Assembly, CameraSoundResource);

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
                }
                catch (Exception e)
                {
                    Log.Error("Error initializing.", e);
                }
            }
        }

        public static void Play()
        {
            if (_soundBuffer == null)
            {
                return;
            }

            try
            {
                // PlayWave copies the wave data and keeps it alive while the sound plays, so the buffer doesn't need to be pinned
                if (!WinMm.PlayWave(_soundBuffer))
                {
                    Log.Warn("Couldn't play the notification sound.");
                }
            }
            catch (Exception e)
            {
                Log.Error("Error in play.", e);
            }
        }

        public static void PlayFile(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    using (var player = new System.Media.SoundPlayer(filePath))
                    {
                        player.Play();
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Could not play sound file '{0}': {1}", filePath, ex.Message);
            }
            Play();
        }

        public static void Deinitialize()
        {
            try
            {
                // Stops the sound and frees the copy of the wave data
                WinMm.StopPlaying();
                _soundBuffer = null;
            }
            catch (Exception e)
            {
                Log.Error("Error in de-initialize.", e);
            }
        }
    }
}