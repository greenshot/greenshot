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
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Configuration helper for recipe steps / nodes.
    /// </summary>
    public class RecipeStepConfig : RecipeNodeConfig
    {
        public RecipeStepConfig()
        {
        }

        public RecipeStepConfig(string stepType, string name = null) : base(Guid.NewGuid().ToString("N").Substring(0, 8), stepType, name)
        {
        }

        public RecipeStepConfig(string id, string stepType, string name) : base(id, stepType, name)
        {
        }

        #region Factory Helpers

        public static RecipeNodeConfig CreateSource(
            string id = "source",
            CaptureSourceType sourceType = CaptureSourceType.Region,
            bool? captureMouse = null,
            int? delayMs = null,
            ScreenCaptureMode? screenMode = null,
            WindowCaptureMode? windowMode = null,
            bool? alignDpi = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Source, $"Acquire {sourceType}");
            node.Set("SourceType", sourceType);
            if (captureMouse.HasValue) node.Set("CaptureMouseCursor", captureMouse.Value);
            if (delayMs.HasValue) node.Set("DelayMs", delayMs.Value);
            if (screenMode.HasValue) node.Set("ScreenCaptureMode", screenMode.Value);
            if (windowMode.HasValue) node.Set("WindowCaptureMode", windowMode.Value);
            if (alignDpi.HasValue) node.Set("AlignDpi", alignDpi.Value);
            return node;
        }

        public static RecipeNodeConfig CreateSelection(string id = "selection", CaptureMode mode = CaptureMode.Region, bool allowWindowSnapping = true)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.InteractiveSelection, $"Select {mode}");
            node.Set("SelectionMode", mode);
            node.Set("AllowWindowSnapping", allowWindowSnapping);
            return node;
        }

        public static RecipeNodeConfig CreateBorder(string id = "border", int width = 2, string color = "#000000")
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Border, "Add Border");
            node.Set("Width", width);
            node.Set("Color", color ?? "#000000");
            return node;
        }

        public static RecipeNodeConfig CreateEffect(string id = "effect", string effect = "DropShadow", Dictionary<string, object> parameters = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Effect, $"Apply {effect}");
            node.Set("Effect", effect);
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    node.Set(kvp.Key, kvp.Value);
                }
            }
            return node;
        }

        public static RecipeNodeConfig CreateDrawable(string id = "drawable", string drawableType = "Text", Dictionary<string, object> parameters = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Drawable, $"Add {drawableType}");
            node.Set("DrawableType", drawableType);
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    node.Set(kvp.Key, kvp.Value);
                }
            }
            return node;
        }

        public static RecipeNodeConfig CreateSetVariable(string id = "set_var", string variable = null, object value = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.SetVariable, "Set Variable");
            if (variable != null) node.Set("Variable", variable);
            if (value != null) node.Set("Value", value);
            return node;
        }

        public static RecipeNodeConfig CreateFeedback(string id = "feedback", bool? playSound = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.ImmediateFeedback, "Capture Feedback");
            if (playSound.HasValue) node.Set("PlaySound", playSound.Value);
            return node;
        }

        public static RecipeNodeConfig CreateProcessors(string id = "processors", IEnumerable<string> processorIds = null, ProcessorTiming? timing = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Processors, "Run Processors");
            if (processorIds != null)
            {
                node.Set("ProcessorIds", new List<string>(processorIds));
            }
            if (timing.HasValue)
            {
                node.Set("Timing", timing.Value.ToString());
            }
            return node;
        }

        public static RecipeNodeConfig CreateDestinations(string id = "destinations", IEnumerable<string> destinationDesignations = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Destinations, "Export Destinations");
            if (destinationDesignations != null)
            {
                node.Set("DestinationDesignations", new List<string>(destinationDesignations));
            }
            return node;
        }

        public static RecipeNodeConfig CreateNotification(string id = "notification", bool? showNotification = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Notification, "Completion Notification");
            if (showNotification.HasValue) node.Set("ShowNotification", showNotification.Value);
            return node;
        }

        #endregion
    }
}
