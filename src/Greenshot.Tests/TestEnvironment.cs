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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing;

namespace Greenshot.Tests
{
    public static class TestEnvironment
    {
        private static readonly object SyncLock = new object();
        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized) return;

            lock (SyncLock)
            {
                if (_initialized) return;

                IniConfigHelper.EnsureInitialized();
                IniConfigHelper.EnsureSection<IEditorConfiguration>(() => new EditorConfigurationImpl());

                CapturePayload.DefaultSurfaceFactory = capture => new Surface(capture) { Modified = true };

                if (SimpleServiceProvider.Current.GetInstance<Func<ISurface>>(isOptional: true) == null)
                {
                    SimpleServiceProvider.Current.AddService<Func<ISurface>>(() => new Surface());
                }

                _initialized = true;
            }
        }
    }
}
