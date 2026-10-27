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

using System.Text.RegularExpressions;
using Greenshot.Linux.Core.Ocr;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;
using SkiaSharp;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.TextEffect, "Text Effect", "Finds text via OCR and applies an effect (pixelize, blur, highlight, redact, magnify) to the matches.", "Effects")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required, VisualMutation = PayloadEffect.AddsAnnotations)]
    [StepParameter("Patterns", ContractDataType.Object, ValueType = typeof(List<string>), Required = true, Description = "Regular expressions to find")]
    [StepParameter("Effect", ContractDataType.Enum, DefaultValue = "Pixelize", Description = "Effect to apply", AllowedValues = new[] { "Pixelize", "Blur", "Highlight", "Redact", "Magnify" })]
    [StepParameter("Scope", ContractDataType.Enum, DefaultValue = "Auto", Description = "Apply to the matching words or whole lines")]
    [StepParameter("MatchCase", ContractDataType.Boolean, DefaultValue = false, Description = "Match case")]
    [StepParameter("FillColor", ContractDataType.String, Description = "Color (Highlight: #FFFF00, Redact: #000000)")]
    [StepParameter("BlurRadius", ContractDataType.Integer, DefaultValue = 10, Description = "Blur radius (Blur)")]
    [StepParameter("PixelSize", ContractDataType.Integer, DefaultValue = 5, Description = "Pixel size (Pixelize)")]
    [StepParameter("MagnificationFactor", ContractDataType.Integer, DefaultValue = 2, Description = "Magnification (Magnify)")]
    [StepParameter("Padding", ContractDataType.Integer, Description = "Padding around the matches")]
    [StepParameter("PaddingHorizontal", ContractDataType.Integer, Description = "Horizontal padding")]
    [StepParameter("PaddingVertical", ContractDataType.Integer, Description = "Vertical padding")]
    [StepParameter("OffsetHorizontal", ContractDataType.Integer, Description = "Horizontal offset")]
    [StepParameter("OffsetVertical", ContractDataType.Integer, Description = "Vertical offset")]
    public class TextEffectStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TextEffectStep));

        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public TextEffectStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.StepType ?? WellKnownStepTypes.TextEffect;
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            var surface = payload?.EnsureSurface();
            if (surface?.Image == null)
            {
                context.LogStep("TextEffectStep skipped: Surface or Image is null.");
                return;
            }

            var patterns = (Config.GetParameter<List<string>>("Patterns") ?? new List<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
            if (patterns.Count == 0)
            {
                context.LogStep("TextEffectStep skipped: no patterns configured.");
                return;
            }

            var options = Config.GetParameter("MatchCase", false) ? RegexOptions.None : RegexOptions.IgnoreCase;
            var regexes = new List<Regex>();
            foreach (var pattern in patterns)
            {
                try
                {
                    regexes.Add(new Regex(pattern, options, TimeSpan.FromSeconds(2)));
                }
                catch (ArgumentException ex)
                {
                    Log.Warn($"Invalid regex pattern '{pattern}' in TextEffectStep", ex);
                    context.LogStep($"Warning: Invalid regex pattern '{pattern}': {ex.Message}");
                }
            }

            if (regexes.Count == 0) return;

            context.LogStep($"Running OCR text detection for {regexes.Count} pattern(s)...");
            var lines = await OcrStep.GetLinesAsync(context, cancellationToken).ConfigureAwait(false);
            if (lines.Count == 0)
            {
                context.LogStep("OCR detected no text content on surface.");
                return;
            }

            string scope = Config.GetParameter("Scope", "Auto");
            int padH = Config.GetParameter("PaddingHorizontal", Config.GetParameter("Padding", 10));
            int padV = Config.GetParameter("PaddingVertical", Config.GetParameter("Padding", 20));
            int offH = Config.GetParameter("OffsetHorizontal", 0);
            int offV = Config.GetParameter("OffsetVertical", 0);
            var matches = FindMatchedBounds(lines, regexes, scope, padH, padV, offH, offV);
            if (matches.Count == 0)
            {
                context.LogStep("TextEffectStep found 0 pattern matches.");
                return;
            }

            string effect = Config.GetParameter("Effect", "Pixelize");
            foreach (var bounds in matches)
            {
                var element = CreateElement(effect, Config);
                element.Bounds = bounds;
                surface.Add(element, select: false);
            }

            surface.IsModified = true;
            payload.InvalidateExportSource();
            context.LogStep($"TextEffectStep applied '{effect}' to {matches.Count} matched region(s).");
            Log.InfoFormat("TextEffectStep applied '{0}' to {1} matched region(s).", effect, matches.Count);
        }

        /// <summary>
        /// The areas of the matches: of whole lines (Line), of words (Word), or of the words a match covers (Auto)
        /// </summary>
        internal static List<SKRect> FindMatchedBounds(IReadOnlyList<OcrLine> lines, List<Regex> regexes, string scope, int padH, int padV, int offH, int offV)
        {
            var results = new List<SKRect>();
            if (string.Equals(scope, "Line", StringComparison.OrdinalIgnoreCase))
            {
                results.AddRange(lines.Where(l => !string.IsNullOrEmpty(l.Text) && regexes.Any(r => r.IsMatch(l.Text))).Select(l => Pad(l.Bounds, padH, padV, offH, offV)));
                return results;
            }

            if (string.Equals(scope, "Word", StringComparison.OrdinalIgnoreCase))
            {
                results.AddRange(lines.SelectMany(l => l.Words).Where(w => !string.IsNullOrEmpty(w.Text) && regexes.Any(r => r.IsMatch(w.Text)))
                    .Select(w => Pad(w.Bounds, padH, padV, offH, offV)));
                return results;
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line.Text) || line.Words.Count == 0) continue;
                int searchIndex = 0;
                var spans = new List<(SKRectI Bounds, int Start, int End)>();
                foreach (var word in line.Words.Where(w => !string.IsNullOrEmpty(w.Text)))
                {
                    int index = line.Text.IndexOf(word.Text, searchIndex, StringComparison.OrdinalIgnoreCase);
                    if (index < 0) index = line.Text.IndexOf(word.Text, StringComparison.OrdinalIgnoreCase);
                    if (index < 0) continue;
                    spans.Add((word.Bounds, index, index + word.Text.Length));
                    searchIndex = index + word.Text.Length;
                }

                foreach (var regex in regexes)
                {
                    foreach (Match match in regex.Matches(line.Text))
                    {
                        var covered = spans.Where(w => w.End > match.Index && w.Start < match.Index + match.Length).ToList();
                        var bounds = covered.Count == 0
                            ? line.Bounds
                            : new SKRectI(covered.Min(w => w.Bounds.Left), covered.Min(w => w.Bounds.Top), covered.Max(w => w.Bounds.Right), covered.Max(w => w.Bounds.Bottom));
                        results.Add(Pad(bounds, padH, padV, offH, offV));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// The padding is in percent of the size, split over both sides like on Windows
        /// </summary>
        private static SKRect Pad(SKRectI bounds, int padHPercent, int padVPercent, int offH, int offV)
        {
            int widthPad = (int)(bounds.Width * (padHPercent / 100.0) / 2);
            int heightPad = (int)(bounds.Height * (padVPercent / 100.0) / 2);
            return SKRect.Create(bounds.Left - widthPad + offH, bounds.Top - heightPad + offV, bounds.Width + widthPad * 2, bounds.Height + heightPad * 2);
        }

        private static DrawableElement CreateElement(string effect, RecipeNodeConfig config)
        {
            switch (effect.ToLowerInvariant())
            {
                case "blur":
                    var blur = new ObfuscateElement();
                    blur.Style = blur.CreateDefaultStyle();
                    blur.Style.ObfuscateFilter = ObfuscateFilter.Blur;
                    blur.Style.BlurRadius = config.GetParameter("BlurRadius", 10);
                    return blur;
                case "highlight":
                case "magnify":
                    var highlight = new HighlightElement();
                    highlight.Style = highlight.CreateDefaultStyle();
                    bool magnify = effect.Equals("magnify", StringComparison.OrdinalIgnoreCase);
                    highlight.Style.HighlightFilter = magnify ? HighlightFilter.Magnification : HighlightFilter.TextHighlight;
                    highlight.Style.MagnificationFactor = config.GetParameter("MagnificationFactor", 2);
                    highlight.Style.FillColor = RecipeColors.Parse(config.GetParameter<string>("FillColor", "#FFFF00"), SKColors.Yellow);
                    return highlight;
                case "redact":
                    var redact = new RectangleElement();
                    redact.Style = redact.CreateDefaultStyle();
                    redact.Style.FillColor = RecipeColors.Parse(config.GetParameter<string>("FillColor", "#000000"), SKColors.Black);
                    redact.Style.LineColor = SKColors.Transparent;
                    redact.Style.LineThickness = 0;
                    redact.Style.Shadow = false;
                    return redact;
                default:
                    var pixelize = new ObfuscateElement();
                    pixelize.Style = pixelize.CreateDefaultStyle();
                    pixelize.Style.ObfuscateFilter = ObfuscateFilter.Pixelize;
                    pixelize.Style.PixelSize = config.GetParameter("PixelSize", 5);
                    return pixelize;
            }
        }
    }
}
