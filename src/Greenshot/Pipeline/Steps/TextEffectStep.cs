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
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step that scans text via OCR, locates occurrences matching regex patterns,
    /// and applies effects (Blur, Pixelize, Highlight, Redact, Magnify) to matched bounding boxes.
    /// </summary>
    public class TextEffectStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TextEffectStep));

        public string Name { get; }
        public RecipeStepConfig Config { get; }

        public TextEffectStep(RecipeStepConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.StepType ?? WellKnownStepTypes.TextEffect;
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            if (payload == null) return;

            var surface = payload.EnsureSurface();
            if (surface?.Image == null) return;

            // Retrieve registered OCR provider
            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);
            if (ocrProvider == null)
            {
                Log.Warn("No IOcrProvider registered in SimpleServiceProvider. Skipping TextEffectStep.");
                context.LogStep("Warning: No OCR provider available to perform text effects.");
                return;
            }

            // Extract regex patterns from step config
            var patterns = ExtractPatterns();
            if (patterns.Count == 0)
            {
                Log.Warn("TextEffectStep has no patterns configured.");
                context.LogStep("TextEffectStep skipped: no patterns configured.");
                return;
            }

            bool matchCase = Config.GetParameter("MatchCase", Config.GetParameter("CaseSensitive", false));
            var regexOptions = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;

            var compiledRegexes = new List<Regex>();
            foreach (var p in patterns)
            {
                try
                {
                    compiledRegexes.Add(new Regex(p, regexOptions));
                }
                catch (Exception ex)
                {
                    Log.Warn($"Invalid regex pattern '{p}' in TextEffectStep", ex);
                    context.LogStep($"Warning: Invalid regex pattern '{p}': {ex.Message}");
                }
            }

            if (compiledRegexes.Count == 0) return;

            context.LogStep($"Running OCR text detection for {compiledRegexes.Count} pattern(s)...");
            Log.InfoFormat("Running OCR for TextEffectStep with {0} regex pattern(s)", compiledRegexes.Count);

            var captureDetails = surface.CaptureDetails ?? payload.RawCapture?.CaptureDetails;
            if (captureDetails?.ProcessingTask != null)
            {
                try
                {
                    await captureDetails.ProcessingTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warn("Error waiting for background OCR processing in TextEffectStep", ex);
                }
            }

            List<IOcrLineFeature> ocrLines = null;
            if (captureDetails?.Features != null)
            {
                lock (captureDetails.Features)
                {
                    ocrLines = captureDetails.Features.OfType<IOcrLineFeature>().ToList();
                }
            }

            if (ocrLines == null || !ocrLines.Any())
            {
                try
                {
                    ocrLines = await ocrProvider.DoOcrAsync(surface).ConfigureAwait(false);
                    if (ocrLines != null && ocrLines.Any() && captureDetails?.Features != null)
                    {
                        lock (captureDetails.Features)
                        {
                            captureDetails.Features.AddRange(ocrLines);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("OCR processing failed during TextEffectStep", ex);
                    context.LogStep($"Warning: OCR processing failed: {ex.Message}");
                    return;
                }
            }

            if (ocrLines == null || !ocrLines.Any())
            {
                context.LogStep("OCR detected no text content on surface.");
                Log.Info("TextEffectStep: OCR detected no text content on surface. Continuing pipeline.");
                return;
            }

            // Match bounding boxes based on search scope
            string scope = Config.GetParameter("Scope", "Auto");
            int padH = Config.GetParameter("PaddingHorizontal", Config.GetParameter("Padding", 10));
            int padV = Config.GetParameter("PaddingVertical", Config.GetParameter("Padding", 20));
            int offH = Config.GetParameter("OffsetHorizontal", 0);
            int offV = Config.GetParameter("OffsetVertical", 0);

            Log.InfoFormat("TextEffectStep: OCR recognized {0} line(s) of text. Searching matches across scope '{1}'...", ocrLines.Count, scope);
            var matchedBounds = FindMatchedBounds(ocrLines, compiledRegexes, scope, padH, padV, offH, offV);
            if (matchedBounds.Count == 0)
            {
                context.LogStep("TextEffectStep found 0 pattern matches.");
                Log.Info("TextEffectStep: 0 pattern matches found. Continuing pipeline.");
                return;
            }

            string effectType = Config.GetParameter("Effect", "Pixelize");
            var containers = new DrawableContainerList();

            foreach (var bounds in matchedBounds)
            {
                var container = CreateContainer(surface, effectType, bounds, Config);
                if (container != null)
                {
                    containers.Add(container);
                }
            }

            if (containers.Count > 0)
            {
                surface.AddElements(containers, true);
                surface.Modified = true;

                if (payload.SharedRenderedBitmap != null)
                {
                    payload.SharedRenderedBitmap.Dispose();
                    payload.SharedRenderedBitmap = null;
                }

                context.LogStep($"TextEffectStep applied '{effectType}' to {containers.Count} matched region(s).");
                Log.InfoFormat("TextEffectStep applied '{0}' to {1} matched region(s).", effectType, containers.Count);
            }
        }

        private List<string> ExtractPatterns()
        {
            var patterns = new List<string>();

            string single = Config.GetParameter<string>("Pattern")
                ?? Config.GetParameter<string>("Regex")
                ?? Config.GetParameter<string>("SearchPattern");
            if (!string.IsNullOrWhiteSpace(single))
            {
                patterns.Add(single);
            }

            var multiple = Config.GetParameter<List<string>>("Patterns")
                ?? Config.GetParameter<List<string>>("Regexes");
            if (multiple != null)
            {
                foreach (var p in multiple)
                {
                    if (!string.IsNullOrWhiteSpace(p) && !patterns.Contains(p))
                    {
                        patterns.Add(p);
                    }
                }
            }

            return patterns;
        }

        private static List<NativeRect> FindMatchedBounds(
            IEnumerable<IOcrLineFeature> ocrLines,
            List<Regex> regexes,
            string scope,
            int padH,
            int padV,
            int offH,
            int offV)
        {
            var results = new List<NativeRect>();

            if (string.Equals(scope, "Line", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var line in ocrLines)
                {
                    if (string.IsNullOrEmpty(line?.Text)) continue;
                    if (regexes.Any(r => r.IsMatch(line.Text)))
                    {
                        results.Add(ApplyPadding(line.Bounds, padH, padV, offH, offV));
                    }
                }
                return results;
            }

            if (string.Equals(scope, "Word", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var line in ocrLines)
                {
                    if (line?.Words == null) continue;
                    foreach (var word in line.Words)
                    {
                        if (string.IsNullOrEmpty(word?.Text)) continue;
                        if (regexes.Any(r => r.IsMatch(word.Text)))
                        {
                            results.Add(ApplyPadding(word.Bounds, padH, padV, offH, offV));
                        }
                    }
                }
                return results;
            }

            // Default: Auto / Smart Scope
            // Searches lines for regex matches, mapping matched text to exact word bounding boxes.
            foreach (var line in ocrLines)
            {
                if (string.IsNullOrEmpty(line?.Text) || line.Words == null || line.Words.Count == 0) continue;

                // Index words in line text
                int searchIdx = 0;
                var wordSpans = new List<(NativeRect Bounds, int Start, int End)>();
                foreach (var word in line.Words)
                {
                    if (string.IsNullOrEmpty(word?.Text)) continue;
                    int idx = line.Text.IndexOf(word.Text, searchIdx, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        wordSpans.Add((word.Bounds, idx, idx + word.Text.Length));
                        searchIdx = idx + word.Text.Length;
                    }
                    else
                    {
                        idx = line.Text.IndexOf(word.Text, StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                        {
                            wordSpans.Add((word.Bounds, idx, idx + word.Text.Length));
                        }
                    }
                }

                foreach (var regex in regexes)
                {
                    var matches = regex.Matches(line.Text);
                    foreach (Match match in matches)
                    {
                        if (!match.Success) continue;
                        int mStart = match.Index;
                        int mEnd = match.Index + match.Length;

                        var overlapping = wordSpans.Where(w => w.End > mStart && w.Start < mEnd).ToList();
                        if (overlapping.Count > 0)
                        {
                            int left = overlapping.Min(w => w.Bounds.Left);
                            int top = overlapping.Min(w => w.Bounds.Top);
                            int right = overlapping.Max(w => w.Bounds.Right);
                            int bottom = overlapping.Max(w => w.Bounds.Bottom);
                            var union = new NativeRect(left, top, right - left, bottom - top);
                            results.Add(ApplyPadding(union, padH, padV, offH, offV));
                        }
                        else
                        {
                            results.Add(ApplyPadding(line.Bounds, padH, padV, offH, offV));
                        }
                    }
                }
            }

            return results;
        }

        private static NativeRect ApplyPadding(NativeRect bounds, int padHPercent, int padVPercent, int offH, int offV)
        {
            int widthPad = (int)(bounds.Width * (padHPercent / 100.0) / 2);
            int heightPad = (int)(bounds.Height * (padVPercent / 100.0) / 2);
            return new NativeRect(
                bounds.Left - widthPad + offH,
                bounds.Top - heightPad + offV,
                bounds.Width + (widthPad * 2),
                bounds.Height + (heightPad * 2)
            );
        }

        private static DrawableContainer CreateContainer(
            ISurface surface,
            string effectType,
            NativeRect bounds,
            RecipeStepConfig config)
        {
            DrawableContainer container = null;

            if (string.Equals(effectType, "Blur", StringComparison.OrdinalIgnoreCase))
            {
                var obf = new ObfuscateContainer(surface);
                obf.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, FilterContainer.PreparedFilter.BLUR);
                int blurRadius = config.GetParameter("BlurRadius", 10);
                obf.SetFieldValue(FieldType.BLUR_RADIUS, blurRadius);
                container = obf;
            }
            else if (string.Equals(effectType, "Highlight", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(effectType, "TextHighlight", StringComparison.OrdinalIgnoreCase))
            {
                var hl = new HighlightContainer(surface);
                hl.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT);
                string colorStr = config.GetParameter<string>("FillColor", config.GetParameter<string>("Color", "#FFFF00"));
                hl.SetFieldValue(FieldType.FILL_COLOR, ParseColor(colorStr, Color.Yellow));
                container = hl;
            }
            else if (string.Equals(effectType, "Magnify", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(effectType, "Magnification", StringComparison.OrdinalIgnoreCase))
            {
                var hl = new HighlightContainer(surface);
                hl.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, FilterContainer.PreparedFilter.MAGNIFICATION);
                int factor = config.GetParameter("MagnificationFactor", 2);
                hl.SetFieldValue(FieldType.MAGNIFICATION_FACTOR, factor);
                container = hl;
            }
            else if (string.Equals(effectType, "Redact", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(effectType, "Blackout", StringComparison.OrdinalIgnoreCase))
            {
                var rect = new RectangleContainer(surface);
                string colorStr = config.GetParameter<string>("FillColor", config.GetParameter<string>("Color", "#000000"));
                rect.SetFieldValue(FieldType.FILL_COLOR, ParseColor(colorStr, Color.Black));
                rect.SetFieldValue(FieldType.LINE_COLOR, Color.Transparent);
                rect.SetFieldValue(FieldType.LINE_THICKNESS, 0);
                rect.SetFieldValue(FieldType.SHADOW, false);
                container = rect;
            }
            else // Default: Pixelize
            {
                var obf = new ObfuscateContainer(surface);
                obf.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, FilterContainer.PreparedFilter.PIXELIZE);
                int pixelSize = config.GetParameter("PixelSize", 5);
                obf.SetFieldValue(FieldType.PIXEL_SIZE, pixelSize);
                container = obf;
            }

            if (container != null)
            {
                container.Left = bounds.Left;
                container.Top = bounds.Top;
                container.Width = bounds.Width;
                container.Height = bounds.Height;
            }

            return container;
        }

        private static Color ParseColor(string colorStr, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(colorStr)) return fallback;
            try
            {
                return ColorTranslator.FromHtml(colorStr);
            }
            catch
            {
                try
                {
                    var named = Color.FromName(colorStr);
                    return named.IsKnownColor ? named : fallback;
                }
                catch
                {
                    return fallback;
                }
            }
        }
    }
}
