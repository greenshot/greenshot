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

using Greenshot.Linux.Editor.Effects;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;
using SkiaSharp;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.Effect, "Image Effect", "Applies an image effect (border, drop shadow, torn edge, grayscale, resize, ...) to the capture.", "Effects")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required, VisualMutation = PayloadEffect.MutatesPixels)]
    [StepParameter("Effect", ContractDataType.Enum, DefaultValue = "Border", Description = "The effect", AllowedValues = new[] { "Border", "DropShadow", "TornEdge", "Invert", "Grayscale", "Monochrome", "Adjust", "Rotate", "Resize", "ResizeCanvas", "ReduceColors", "RemoveTransparency" })]
    [StepParameter("Width", ContractDataType.Integer, Description = "Border width (Border), new width (Resize, instead of Percentage)")]
    [StepParameter("Height", ContractDataType.Integer, Description = "New height (Resize, instead of Percentage)")]
    [StepParameter("Color", ContractDataType.String, Description = "Border color (Border)")]
    [StepParameter("Darkness", ContractDataType.Decimal, Description = "Shadow darkness (DropShadow, TornEdge)")]
    [StepParameter("ShadowSize", ContractDataType.Integer, Description = "Shadow size (DropShadow, TornEdge)")]
    [StepParameter("ShadowOffsetX", ContractDataType.Integer, Description = "Shadow offset (DropShadow)")]
    [StepParameter("ShadowOffsetY", ContractDataType.Integer, Description = "Shadow offset (DropShadow)")]
    [StepParameter("ToothHeight", ContractDataType.Integer, Description = "Tooth height (TornEdge)")]
    [StepParameter("HorizontalToothRange", ContractDataType.Integer, Description = "Horizontal tooth range (TornEdge)")]
    [StepParameter("VerticalToothRange", ContractDataType.Integer, Description = "Vertical tooth range (TornEdge)")]
    [StepParameter("GenerateShadow", ContractDataType.Boolean, Description = "Add a shadow (TornEdge)")]
    [StepParameter("Edges", ContractDataType.Object, ValueType = typeof(List<string>), AllowedValues = new[] { "Top", "Right", "Bottom", "Left" }, Description = "The torn edges, a list of Top, Right, Bottom, Left; without it all (TornEdge)")]
    [StepParameter("Threshold", ContractDataType.Integer, DefaultValue = 128, Description = "Threshold (Monochrome)")]
    [StepParameter("Brightness", ContractDataType.Decimal, Description = "Brightness (Adjust)")]
    [StepParameter("Contrast", ContractDataType.Decimal, Description = "Contrast (Adjust)")]
    [StepParameter("Gamma", ContractDataType.Decimal, Description = "Gamma (Adjust)")]
    [StepParameter("Angle", ContractDataType.Integer, DefaultValue = 90, Description = "Angle (Rotate)")]
    [StepParameter("Percentage", ContractDataType.Decimal, Description = "Size in percent (Resize, instead of Width and Height)")]
    [StepParameter("MaintainAspectRatio", ContractDataType.Boolean, DefaultValue = true, Description = "Keep the aspect ratio (Resize)")]
    [StepParameter("Left", ContractDataType.Integer, Description = "Pixels added on the left (ResizeCanvas)")]
    [StepParameter("Right", ContractDataType.Integer, Description = "Pixels added on the right (ResizeCanvas)")]
    [StepParameter("Top", ContractDataType.Integer, Description = "Pixels added at the top (ResizeCanvas)")]
    [StepParameter("Bottom", ContractDataType.Integer, Description = "Pixels added at the bottom (ResizeCanvas)")]
    [StepParameter("BackgroundColor", ContractDataType.String, Description = "Background color (ResizeCanvas, RemoveTransparency)")]
    [StepParameter("Colors", ContractDataType.Integer, Description = "Number of colors (ReduceColors)")]
    public class EffectCaptureStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EffectCaptureStep));

        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public EffectCaptureStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.StepType ?? "EffectCaptureStep";
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            if (payload?.RawCapture == null)
            {
                context.LogStep("EffectStep skipped: Payload or RawCapture is null.");
                return Task.CompletedTask;
            }

            var surface = payload.EnsureSurface();
            string effectName = Config.GetParameter<string>("Effect");
            if (string.IsNullOrEmpty(effectName))
            {
                effectName = "Border";
            }

            context.LogStep($"Applying effect '{effectName}'");
            try
            {
                var result = Apply(effectName, surface.Image);
                if (result != null)
                {
                    surface.ReplaceImage(result.Image, result.ElementTransform);
                    surface.IsModified = true;
                    payload.InvalidateExportSource();
                }
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                Log.Error($"Failed to apply effect {effectName}", ex);
                context.LogStep($"Warning: Failed to apply effect {effectName}: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private EffectResult Apply(string effectName, SKBitmap image)
        {
            switch (effectName.ToLowerInvariant())
            {
                case "border":
                    return ImageEffects.Border(image, Math.Max(1, Config.GetParameter("Width", 2)), RecipeColors.Parse(Config.GetParameter<string>("Color", "#000000"), SKColors.Black));
                case "dropshadow":
                    return ImageEffects.DropShadow(image, Config.GetParameter("Darkness", 0.6f), Config.GetParameter("ShadowSize", 7),
                        new SKPointI(Config.GetParameter("ShadowOffsetX", -1), Config.GetParameter("ShadowOffsetY", -1)));
                case "tornedge":
                    var edges = Config.GetParameter<List<string>>("Edges");
                    bool[] sides = edges == null
                        ? new[] { true, true, true, true }
                        : new[] { "Top", "Right", "Bottom", "Left" }.Select(e => edges.Contains(e, StringComparer.OrdinalIgnoreCase)).ToArray();
                    return ImageEffects.TornEdge(image, Config.GetParameter("ToothHeight", 12), Config.GetParameter("HorizontalToothRange", 20),
                        Config.GetParameter("VerticalToothRange", 20), sides, Config.GetParameter("GenerateShadow", true));
                case "invert":
                    return ImageEffects.Invert(image);
                case "grayscale":
                    return ImageEffects.Grayscale(image);
                case "monochrome":
                    return ImageEffects.Monochrome(image, (byte)Math.Clamp(Config.GetParameter("Threshold", 128), 0, 255));
                case "adjust":
                    return ImageEffects.Adjust(image, Config.GetParameter("Brightness", 1f), Config.GetParameter("Contrast", 1f), Config.GetParameter("Gamma", 1f));
                case "rotate":
                    return ImageEffects.Rotate(image, Config.GetParameter("Angle", 90));
                case "resize":
                    return Resize(image);
                case "resizecanvas":
                    var canvas = ImageEffects.ResizeCanvas(image, Config.GetParameter("Left", 0), Config.GetParameter("Top", 0), Config.GetParameter("Right", 0),
                        Config.GetParameter("Bottom", 0));
                    string background = Config.GetParameter<string>("BackgroundColor", null);
                    if (!string.IsNullOrEmpty(background))
                    {
                        var filled = ImageEffects.RemoveTransparency(canvas.Image, RecipeColors.Parse(background, SKColors.White));
                        canvas.Image.Dispose();
                        return filled with { ElementTransform = canvas.ElementTransform };
                    }

                    return canvas;
                case "reducecolors":
                    return ImageEffects.ReduceColors(image, Config.GetParameter("Colors", 256));
                case "removetransparency":
                    return ImageEffects.RemoveTransparency(image, RecipeColors.Parse(Config.GetParameter<string>("BackgroundColor", null), SKColors.White));
                default:
                    Log.WarnFormat("Unknown effect '{0}'", effectName);
                    return null;
            }
        }

        private EffectResult Resize(SKBitmap image)
        {
            int width = Config.GetParameter("Width", 0);
            int height = Config.GetParameter("Height", 0);
            float percentage = Config.GetParameter("Percentage", 0f);
            if (percentage > 0)
            {
                width = (int)Math.Round(image.Width * (percentage / 100f));
                height = (int)Math.Round(image.Height * (percentage / 100f));
            }
            else if (Config.GetParameter("MaintainAspectRatio", true))
            {
                // One side follows the other, with both the image fits in them
                if (width <= 0 && height > 0) width = (int)Math.Round(image.Width * (height / (double)image.Height));
                else if (height <= 0 && width > 0) height = (int)Math.Round(image.Height * (width / (double)image.Width));
                else if (width > 0 && height > 0)
                {
                    double scale = Math.Min(width / (double)image.Width, height / (double)image.Height);
                    width = (int)Math.Round(image.Width * scale);
                    height = (int)Math.Round(image.Height * scale);
                }
            }

            if (width <= 0 || height <= 0)
            {
                return null;
            }

            return ImageEffects.Resize(image, width, height);
        }
    }
}
