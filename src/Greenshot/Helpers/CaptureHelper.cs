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
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Pipeline;
using Greenshot.Recipes;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Backward-compatible facade delegating capture operations to CapturePipeline and RecipeManager.
    /// Preserves existing public API for plugins and legacy callers.
    /// </summary>
    public class CaptureHelper : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CaptureHelper));

        private WindowDetails _selectedCaptureWindow;
        private ICapture _capture;
        private CaptureMode _captureMode;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _selectedCaptureWindow = null;
            _capture = null;
        }

        public static void CaptureClipboard(IDestination destination = null)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdClipboard);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                if (destination != null)
                {
                    ctx.Properties["OverrideDestinations"] = new List<string> { destination.Designation };
                }
            });
        }

        public static void CaptureRegion(bool captureMouse)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdRegion);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                ctx.Properties["CaptureMouseCursor"] = captureMouse;
            });
        }

        public static void CaptureRegion(bool captureMouse, IDestination destination)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdRegion);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                ctx.Properties["CaptureMouseCursor"] = captureMouse;
                if (destination != null)
                {
                    ctx.Properties["OverrideDestinations"] = new List<string> { destination.Designation };
                }
            });
        }

        public static void CaptureRegion(bool captureMouse, NativeRect region)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdRegion);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                ctx.Properties["CaptureMouseCursor"] = captureMouse;
                ctx.Properties["PreSuppliedRegion"] = region;
            });
        }

        public static void CaptureFullscreen(bool captureMouse, ScreenCaptureMode screenCaptureMode)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdFullScreen);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                ctx.Properties["CaptureMouseCursor"] = captureMouse;
                ctx.Properties["ScreenCaptureMode"] = screenCaptureMode;
            });
        }

        public static void CaptureLastRegion(bool captureMouse)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdLastRegion);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                ctx.Properties["CaptureMouseCursor"] = captureMouse;
            });
        }

        public static void CaptureWindow(bool captureMouse)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdActiveWindow);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                ctx.Properties["CaptureMouseCursor"] = captureMouse;
            });
        }

        public static void CaptureWindow(WindowDetails windowToCapture)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdActiveWindow);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                ctx.Properties["TargetWindow"] = windowToCapture;
            });
        }

        public static void CaptureWindowInteractive(bool captureMouse)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdWindow);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                ctx.Properties["CaptureMouseCursor"] = captureMouse;
            });
        }

        public static void CaptureFile(string filename, IDestination destination = null)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdFile);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                ctx.Properties["Filename"] = filename;
                if (destination != null)
                {
                    ctx.Properties["OverrideDestinations"] = new List<string> { destination.Designation };
                }
            });
        }

        public static void ImportCapture(ICapture captureToImport)
        {
            var recipe = RecipeManager.Instance.GetRecipeById(RecipeManager.RecipeIdClipboard);
            _ = CapturePipeline.Instance.ExecuteAsync(recipe, null, ctx =>
            {
                ctx.Payload = new CapturePayload(captureToImport);
            });
        }

        public static WindowDetails SelectCaptureWindow(WindowDetails windowToCapture)
        {
            return WindowCaptureHelper.SelectCaptureWindow(windowToCapture);
        }

        public static ICapture CaptureWindow(WindowDetails windowToCapture, ICapture captureForWindow, WindowCaptureMode windowCaptureMode)
        {
            return WindowCaptureHelper.CaptureWindow(windowToCapture, captureForWindow, windowCaptureMode);
        }

        public CaptureHelper AddDestination(IDestination destination)
        {
            _capture?.CaptureDetails?.AddDestination(destination);
            return this;
        }

        public CaptureHelper(CaptureMode captureMode)
        {
            _captureMode = captureMode;
            _capture = new Capture();
        }

        public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor) : this(captureMode)
        {
        }

        public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor, ScreenCaptureMode screenCaptureMode) : this(captureMode)
        {
        }

        public CaptureHelper(CaptureMode captureMode, bool captureMouseCursor, IDestination destination) : this(captureMode, captureMouseCursor)
        {
            _capture.CaptureDetails.AddDestination(destination);
        }

        public WindowDetails SelectedCaptureWindow
        {
            get => _selectedCaptureWindow;
            set => _selectedCaptureWindow = value;
        }
    }
}