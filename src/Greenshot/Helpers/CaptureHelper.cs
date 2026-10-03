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
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Pipeline;
using Greenshot.Recipes;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Starts the built-in recipes for the tray menu, the hotkeys and the command line.
    /// Every capture is a recipe flow: the recipe decides how the image is acquired, selected and where it goes.
    /// </summary>
    public static class CaptureHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CaptureHelper));

        /// <summary>
        /// Start the built-in recipe through the flow runner, snapshotting the trigger situation now.
        /// </summary>
        /// <param name="recipeId">Id of the built-in recipe</param>
        /// <param name="captureMouse">Capture the mouse cursor, null to let the recipe decide</param>
        /// <param name="destination">Destination which replaces the destinations of the recipe, null to use the recipe's</param>
        /// <param name="configure">Sets more properties of the flow</param>
        private static void Start(string recipeId, bool? captureMouse = null, IDestination destination = null, Action<CaptureFlowContext> configure = null)
        {
            Start(RecipeManager.Instance.GetRecipeById(recipeId), ctx =>
            {
                if (captureMouse.HasValue)
                {
                    ctx.Properties["CaptureMouseCursor"] = captureMouse.Value;
                }
                if (destination != null)
                {
                    ctx.Properties["OverrideDestinations"] = new List<string> { destination.Designation };
                }
                configure?.Invoke(ctx);
            });
        }

        private static void Start(CaptureRecipe recipe, Action<CaptureFlowContext> configure)
        {
            if (recipe == null)
            {
                Log.Error("Can't start the capture, the recipe was not found.");
                return;
            }

            CaptureFlowRunner.Current.Start(recipe, FlowTriggerContext.Capture(), configure);
        }

        public static void CaptureClipboard(IDestination destination = null) => Start(RecipeManager.RecipeIdClipboard, destination: destination);

        public static void CaptureRegion(bool captureMouse, IDestination destination = null) => Start(RecipeManager.RecipeIdRegion, captureMouse, destination);

        /// <summary>
        /// Capture the region without asking the user to select it
        /// </summary>
        public static void CaptureRegion(bool captureMouse, NativeRect region) => Start(RecipeManager.RecipeIdRegion, captureMouse, configure: ctx => ctx.Properties["PreSuppliedRegion"] = region);

        public static void CaptureFullscreen(bool captureMouse, ScreenCaptureMode screenCaptureMode) =>
            Start(RecipeManager.RecipeIdFullScreen, captureMouse, configure: ctx => ctx.Properties["ScreenCaptureMode"] = screenCaptureMode);

        public static void CaptureLastRegion(bool captureMouse) => Start(RecipeManager.RecipeIdLastRegion, captureMouse);

        /// <summary>
        /// Capture the active window
        /// </summary>
        public static void CaptureWindow(bool captureMouse) => Start(RecipeManager.RecipeIdActiveWindow, captureMouse);

        /// <summary>
        /// Capture the window, e.g. one picked from the tray menu
        /// </summary>
        public static void CaptureWindow(WindowDetails windowToCapture) => Start(RecipeManager.RecipeIdActiveWindow, configure: ctx => ctx.Properties["TargetWindow"] = windowToCapture);

        /// <summary>
        /// Let the user select the window to capture
        /// </summary>
        public static void CaptureWindowInteractive(bool captureMouse) => Start(RecipeManager.RecipeIdWindow, captureMouse);

        public static void CaptureFile(string filename, IDestination destination = null) =>
            Start(RecipeManager.RecipeIdFile, destination: destination, configure: ctx => ctx.Properties["Filename"] = filename);

        /// <summary>
        /// Process a capture of the browser extension with the first active recipe of an extension trigger for the browser
        /// </summary>
        public static void ImportExtensionCapture(ICapture captureToImport, string browser = null)
        {
            var recipe = RecipeManager.Instance.GetAllRecipes()?.FirstOrDefault(r => r != null && r.IsEnabled && r.Triggers != null && r.Triggers.Any(t =>
                t.IsActive &&
                string.Equals(t.TriggerType, TriggerConfig.TypeExtension, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(t.GetParameter<string>("Browser")) ||
                 string.Equals(t.GetParameter<string>("Browser"), browser, StringComparison.OrdinalIgnoreCase))));
            recipe ??= RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdExtension);

            Start(recipe, ctx =>
            {
                ctx.Payload = new CapturePayload(captureToImport);
                ctx.Properties["Capture"] = captureToImport;
                if (!string.IsNullOrEmpty(browser))
                {
                    ctx.Properties["Browser"] = browser;
                }
            });
        }
    }
}
