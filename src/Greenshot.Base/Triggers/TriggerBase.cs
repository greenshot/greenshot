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
using System.Collections.Generic;

namespace Greenshot.Base.Triggers
{
    /// <summary>
    /// Abstract base class for ITrigger implementations providing standard event handling and lifecycle.
    /// </summary>
    public abstract class TriggerBase : ITrigger
    {
        private bool _disposed;

        public string Id { get; protected set; }
        public string Name { get; set; }
        public string TargetRecipeId { get; set; }
        public bool IsEnabled { get; set; } = true;

        public event EventHandler<TriggerEventArgs> Triggered;

        protected TriggerBase(string id, string name, string targetRecipeId)
        {
            Id = id ?? Guid.NewGuid().ToString("N");
            Name = name;
            TargetRecipeId = targetRecipeId;
        }

        public abstract void Start();
        public abstract void Stop();

        /// <summary>
        /// Fires the Triggered event to start the pipeline.
        /// </summary>
        protected virtual void OnTriggered(IDictionary<string, object> parameters = null)
        {
            if (!IsEnabled) return;
            Triggered?.Invoke(this, new TriggerEventArgs(TargetRecipeId, parameters));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                Stop();
            }
        }
    }
}
