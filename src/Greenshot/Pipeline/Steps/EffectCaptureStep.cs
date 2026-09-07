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
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Effects;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step that applies an image effect (border, drop shadow, torn edge, grayscale, invert,
    /// monochrome, adjust colors, rotate, resize, resize canvas, reduce colors, remove transparency)
    /// to the captured surface directly during flow execution, without displaying modal UI dialogs.
    /// </summary>
    public class EffectCaptureStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EffectCaptureStep));

        public string Name { get; }
        public RecipeStepConfig Config { get; }

        public EffectCaptureStep(RecipeStepConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.StepType ?? "EffectCaptureStep";
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            if (payload?.RawCapture == null) return Task.CompletedTask;

            var surface = payload.EnsureSurface();
            if (surface?.Image == null) return Task.CompletedTask;

            IEffect effect = ResolveEffect(surface.Image);
            if (effect == null) return Task.CompletedTask;

            context.LogStep($"Applying effect '{effect.GetType().Name}'");
            Log.InfoFormat("Applying effect {0} to surface image", effect.GetType().Name);

            try
            {
                using (var matrix = new Matrix())
                {
                    var newImage = effect.Apply(surface.Image, matrix);
                    if (newImage != null)
                    {
                        surface.Image = newImage;
                        surface.Elements?.Transform(matrix);
                        surface.Modified = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to apply effect {effect.GetType().Name}", ex);
                context.LogStep($"Warning: Failed to apply effect {effect.GetType().Name}: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private IEffect ResolveEffect(Image currentImage)
        {
            string effectName = Config.GetParameter<string>("Effect");

            // If step type is Border, or effect parameter is "Border", apply BorderEffect
            if (string.Equals(Config.StepType, WellKnownStepTypes.Border, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(effectName, "Border", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(effectName))
            {
                int width = Config.GetParameter("Width", 2);
                if (width < 1) width = 1;

                Color color = ParseColor(Config.GetParameter<string>("Color", "#000000"));

                return new BorderEffect
                {
                    Width = width,
                    Color = color
                };
            }

            if (string.Equals(effectName, "DropShadow", StringComparison.OrdinalIgnoreCase))
            {
                var dropShadow = new DropShadowEffect();
                dropShadow.Darkness = Config.GetParameter("Darkness", dropShadow.Darkness);
                dropShadow.ShadowSize = Config.GetParameter("ShadowSize", dropShadow.ShadowSize);
                int offX = Config.GetParameter("ShadowOffsetX", dropShadow.ShadowOffset.X);
                int offY = Config.GetParameter("ShadowOffsetY", dropShadow.ShadowOffset.Y);
                dropShadow.ShadowOffset = new Point(offX, offY);
                return dropShadow;
            }

            if (string.Equals(effectName, "TornEdge", StringComparison.OrdinalIgnoreCase))
            {
                var tornEdge = new TornEdgeEffect();
                tornEdge.ToothHeight = Config.GetParameter("ToothHeight", tornEdge.ToothHeight);
                tornEdge.HorizontalToothRange = Config.GetParameter("HorizontalToothRange", tornEdge.HorizontalToothRange);
                tornEdge.VerticalToothRange = Config.GetParameter("VerticalToothRange", tornEdge.VerticalToothRange);
                tornEdge.GenerateShadow = Config.GetParameter("GenerateShadow", Config.GetParameter("Shadow", tornEdge.GenerateShadow));
                tornEdge.ShadowSize = Config.GetParameter("ShadowSize", tornEdge.ShadowSize);
                tornEdge.Darkness = Config.GetParameter("Darkness", Config.GetParameter("ShadowDarkness", tornEdge.Darkness));

                var edgesList = Config.GetParameter<List<bool>>("Edges");
                if (edgesList != null && edgesList.Count == 4)
                {
                    tornEdge.Edges = edgesList.ToArray();
                }
                else
                {
                    string edgesStr = Config.GetParameter<string>("Edges");
                    if (!string.IsNullOrEmpty(edgesStr))
                    {
                        tornEdge.Edges = new[]
                        {
                            edgesStr.IndexOf("top", StringComparison.OrdinalIgnoreCase) >= 0,
                            edgesStr.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0,
                            edgesStr.IndexOf("bottom", StringComparison.OrdinalIgnoreCase) >= 0,
                            edgesStr.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0
                        };
                    }
                }
                return tornEdge;
            }

            if (string.Equals(effectName, "Invert", StringComparison.OrdinalIgnoreCase))
            {
                return new InvertEffect();
            }

            if (string.Equals(effectName, "Grayscale", StringComparison.OrdinalIgnoreCase))
            {
                return new GrayscaleEffect();
            }

            if (string.Equals(effectName, "Monochrome", StringComparison.OrdinalIgnoreCase))
            {
                byte threshold = (byte)Config.GetParameter("Threshold", 128);
                return new MonochromeEffect(threshold);
            }

            if (string.Equals(effectName, "Adjust", StringComparison.OrdinalIgnoreCase))
            {
                var adjust = new AdjustEffect();
                adjust.Brightness = Config.GetParameter("Brightness", adjust.Brightness);
                adjust.Contrast = Config.GetParameter("Contrast", adjust.Contrast);
                adjust.Gamma = Config.GetParameter("Gamma", adjust.Gamma);
                return adjust;
            }

            if (string.Equals(effectName, "Rotate", StringComparison.OrdinalIgnoreCase))
            {
                int angle = Config.GetParameter("Angle", 90);
                return new RotateEffect(angle);
            }

            if (string.Equals(effectName, "Resize", StringComparison.OrdinalIgnoreCase))
            {
                int width = Config.GetParameter("Width", 0);
                int height = Config.GetParameter("Height", 0);
                float percentage = Config.GetParameter("Percentage", 0f);
                if (percentage > 0 && currentImage != null)
                {
                    width = (int)Math.Round(currentImage.Width * (percentage / 100f));
                    height = (int)Math.Round(currentImage.Height * (percentage / 100f));
                }

                bool maintainAspectRatio = Config.GetParameter("MaintainAspectRatio", true);
                return new ResizeEffect(width, height, maintainAspectRatio);
            }

            if (string.Equals(effectName, "ResizeCanvas", StringComparison.OrdinalIgnoreCase))
            {
                int margin = Config.GetParameter("Margin", 0);
                int left = Config.GetParameter("Left", margin);
                int right = Config.GetParameter("Right", margin);
                int top = Config.GetParameter("Top", margin);
                int bottom = Config.GetParameter("Bottom", margin);

                var canvas = new ResizeCanvasEffect(left, right, top, bottom);
                string bgColor = Config.GetParameter<string>("BackgroundColor", Config.GetParameter<string>("Color", null));
                if (!string.IsNullOrEmpty(bgColor))
                {
                    canvas.BackgroundColor = ParseColor(bgColor);
                }
                return canvas;
            }

            if (string.Equals(effectName, "ReduceColors", StringComparison.OrdinalIgnoreCase))
            {
                var reduce = new ReduceColorsEffect();
                reduce.Colors = Config.GetParameter("Colors", reduce.Colors);
                return reduce;
            }

            if (string.Equals(effectName, "RemoveTransparency", StringComparison.OrdinalIgnoreCase))
            {
                var removeTrans = new RemoveTransparencyEffect();
                string colorStr = Config.GetParameter<string>("Color", null);
                if (!string.IsNullOrEmpty(colorStr))
                {
                    removeTrans.Color = ParseColor(colorStr);
                }
                return removeTrans;
            }

            return null;
        }

        private static Color ParseColor(string colorStr)
        {
            if (string.IsNullOrWhiteSpace(colorStr)) return Color.Black;

            try
            {
                return ColorTranslator.FromHtml(colorStr);
            }
            catch
            {
                try
                {
                    var named = Color.FromName(colorStr);
                    return named.IsKnownColor ? named : Color.Black;
                }
                catch
                {
                    return Color.Black;
                }
            }
        }
    }
}
