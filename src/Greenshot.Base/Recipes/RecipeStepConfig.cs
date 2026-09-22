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

        public static RecipeNodeConfig CreateAnnotation(string id = "annotation", string annotationType = "Text", Dictionary<string, object> parameters = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Annotation, $"Add {annotationType}");
            node.Set("AnnotationType", annotationType);
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

        public static RecipeNodeConfig CreateProcessors(string id = "processors", IEnumerable<string> processorIds = null, ProcessorTiming? timing = null, string ocrLanguage = null)
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
            if (!string.IsNullOrEmpty(ocrLanguage))
            {
                node.Set("OcrLanguage", ocrLanguage);
            }
            return node;
        }

        public static RecipeNodeConfig CreateSaveFile(
            string id = "save_file",
            string saveDirectory = null,
            string filenamePattern = null,
            OutputFormat? format = null,
            bool? allowOverwrite = null,
            int? jpegQuality = null,
            bool? reduceColors = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.SaveFile, "Save to File");
            if (!string.IsNullOrEmpty(saveDirectory)) node.Set("SaveDirectory", saveDirectory);
            if (!string.IsNullOrEmpty(filenamePattern)) node.Set("FilenamePattern", filenamePattern);
            if (format.HasValue) node.Set("Format", format.Value.ToString());
            if (allowOverwrite.HasValue) node.Set("AllowOverwrite", allowOverwrite.Value);
            if (jpegQuality.HasValue) node.Set("JpegQuality", jpegQuality.Value);
            if (reduceColors.HasValue) node.Set("ReduceColors", reduceColors.Value);
            return node;
        }

        public static RecipeNodeConfig CreateClipboard(string id = "clipboard", string clipboardMode = "ImageOnly", IEnumerable<ClipboardFormat> formats = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Clipboard, "Copy to Clipboard");
            if (!string.IsNullOrEmpty(clipboardMode)) node.Set("ClipboardMode", clipboardMode);
            if (formats != null)
            {
                foreach (var f in formats)
                {
                    node.Set($"ClipboardFormat{f}", true);
                }
            }
            return node;
        }

        public static RecipeNodeConfig CreateEditor(string id = "editor", bool? matchSizeToCapture = null, TargetEditor? targetEditor = null, bool? suppressSaveDialog = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Editor, "Open in Editor");
            if (matchSizeToCapture.HasValue) node.Set("MatchSizeToCapture", matchSizeToCapture.Value);
            if (targetEditor.HasValue) node.Set("TargetEditor", targetEditor.Value.ToString());
            if (suppressSaveDialog.HasValue) node.Set("SuppressSaveDialog", suppressSaveDialog.Value);
            return node;
        }

        public static RecipeNodeConfig CreatePrinter(
            string id = "printer",
            string printerName = null,
            bool? promptOptions = null,
            bool? allowRotate = null,
            bool? allowEnlarge = null,
            bool? allowShrink = null,
            bool? center = null,
            string colorMode = null,
            bool? printFooter = null,
            string footerPattern = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Printer, "Send to Printer");
            if (!string.IsNullOrEmpty(printerName)) node.Set("PrinterName", printerName);
            if (promptOptions.HasValue) node.Set("ShowPrintDialog", promptOptions.Value);
            if (allowRotate.HasValue) node.Set("AllowRotate", allowRotate.Value);
            if (allowEnlarge.HasValue) node.Set("AllowEnlarge", allowEnlarge.Value);
            if (allowShrink.HasValue) node.Set("AllowShrink", allowShrink.Value);
            if (center.HasValue) node.Set("Center", center.Value);
            if (!string.IsNullOrEmpty(colorMode)) node.Set("ColorMode", colorMode);
            if (printFooter.HasValue) node.Set("PrintFooter", printFooter.Value);
            if (!string.IsNullOrEmpty(footerPattern)) node.Set("FooterPattern", footerPattern);
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

        public static RecipeNodeConfig CreateConditional(string id = "conditional", IEnumerable<KeyValuePair<string, string>> branches = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.Conditional, "Decision Branch");
            var branchList = new List<Dictionary<string, string>>();
            if (branches != null)
            {
                foreach (var b in branches)
                {
                    branchList.Add(new Dictionary<string, string>
                    {
                        ["Key"] = b.Key,
                        ["Expression"] = b.Value
                    });
                }
            }
            else
            {
                branchList.Add(new Dictionary<string, string> { ["Key"] = "A", ["Expression"] = "${payload.width > 800}" });
                branchList.Add(new Dictionary<string, string> { ["Key"] = "B", ["Expression"] = "else" });
            }
            node.Set("Branches", branchList);
            return node;
        }

        public static RecipeNodeConfig CreateUserPrompt(
            string id = "user_prompt",
            string title = "User Decision",
            string message = "Please choose how to proceed with this capture:",
            bool showPreview = true,
            IEnumerable<Dictionary<string, object>> choices = null,
            int timeoutSeconds = 0,
            string defaultChoice = null)
        {
            var node = new RecipeNodeConfig(id, WellKnownStepTypes.UserPrompt, title ?? "User Decision");
            node.Set("Title", title ?? "User Decision");
            node.Set("Message", message ?? "Please choose how to proceed with this capture:");
            node.Set("ShowPreview", showPreview);
            if (timeoutSeconds > 0) node.Set("TimeoutSeconds", timeoutSeconds);
            if (!string.IsNullOrEmpty(defaultChoice)) node.Set("DefaultChoice", defaultChoice);

            var choiceList = new List<Dictionary<string, object>>();
            if (choices != null)
            {
                choiceList.AddRange(choices);
            }
            else
            {
                choiceList.Add(new Dictionary<string, object> { ["Key"] = "Yes", ["Label"] = "Yes, Proceed", ["Style"] = "Primary", ["IsDefault"] = true });
                choiceList.Add(new Dictionary<string, object> { ["Key"] = "No", ["Label"] = "No, Cancel", ["Style"] = "Secondary", ["IsCancel"] = true });
            }
            node.Set("Choices", choiceList);
            return node;
        }

        #endregion
    }
}
