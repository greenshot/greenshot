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

namespace Greenshot.Base.Triggers
{
    /// <summary>
    /// Trigger that allows a capture recipe to receive and process screenshots sent from the
    /// Greenshot companion browser extension via Native Messaging (Chrome, Edge, Firefox).
    /// </summary>
    public class ExtensionTrigger : TriggerBase
    {
        public string Browser { get; }
        public bool FireAndForget { get; }

        public ExtensionTrigger(
            string id,
            string name,
            string targetRecipeId,
            string browser = null,
            bool fireAndForget = false)
            : base(id, name, targetRecipeId)
        {
            Browser = browser;
            FireAndForget = fireAndForget;
        }

        public ExtensionTrigger(string targetRecipeId, TriggerConfig config)
            : base(System.Guid.NewGuid().ToString(), config?.Name ?? "Browser Extension Trigger", targetRecipeId)
        {
            Browser = config?.GetParameter<string>("Browser");
            FireAndForget = config?.GetParameter<bool>("FireAndForget", false) ?? false;
        }

        public override string TriggerType => TriggerConfig.TypeExtension;

        public override void Start() { }
        public override void Stop() { }
    }
}
