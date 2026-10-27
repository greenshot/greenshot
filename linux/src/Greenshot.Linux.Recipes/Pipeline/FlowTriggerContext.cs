/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

#nullable disable

using Greenshot.Linux.Recipes.Triggers;

namespace Greenshot.Linux.Recipes.Pipeline
{
    /// <summary>
    /// What was there when a trigger fired. Windows keeps the foreground window and the cursor; Wayland tells an
    /// application neither, so on Linux it is the title of the active window when the compositor knows it.
    /// </summary>
    public sealed class FlowTriggerContext
    {
        public FlowTriggerContext(ITrigger trigger, string activeWindowTitle, DateTimeOffset triggeredAt)
        {
            Trigger = trigger;
            ActiveWindowTitle = activeWindowTitle;
            TriggeredAt = triggeredAt;
        }

        public ITrigger Trigger { get; }

        public string ActiveWindowTitle { get; }

        public DateTimeOffset TriggeredAt { get; }

        public static FlowTriggerContext Empty(ITrigger trigger = null) => new FlowTriggerContext(trigger, null, DateTimeOffset.Now);
    }
}
