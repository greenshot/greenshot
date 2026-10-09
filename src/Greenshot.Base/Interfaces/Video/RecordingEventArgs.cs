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

namespace Greenshot.Base.Interfaces.Video
{
    /// <summary>
    /// Event arguments for recording session state changes.
    /// </summary>
    public class RecordingStateChangedEventArgs : EventArgs
    {
        public RecordingState OldState { get; }
        public RecordingState NewState { get; }

        public RecordingStateChangedEventArgs(RecordingState oldState, RecordingState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>
    /// Event arguments for recording duration updates.
    /// </summary>
    public class RecordingDurationChangedEventArgs : EventArgs
    {
        public TimeSpan Duration { get; }

        public RecordingDurationChangedEventArgs(TimeSpan duration)
        {
            Duration = duration;
        }
    }

    /// <summary>
    /// Event arguments for non-fatal or fatal recording errors.
    /// </summary>
    public class RecordingErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public bool IsFatal { get; }

        public RecordingErrorEventArgs(Exception exception, bool isFatal)
        {
            Exception = exception;
            IsFatal = isFatal;
        }
    }
}
