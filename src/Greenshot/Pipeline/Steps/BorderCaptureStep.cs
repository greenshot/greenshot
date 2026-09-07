/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
    /// Pipeline step that adds a border or applies an image effect (drop shadow, torn edge, grayscale, invert)
    /// to the captured surface directly during flow execution, without displaying modal UI dialogs.
    /// </summary>
    public class BorderCaptureStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BorderCaptureStep));

        public string Name { get; }
        public RecipeStepConfig Config { get; }

        public BorderCaptureStep(RecipeStepConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.StepType ?? "BorderCaptureStep";
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            if (payload?.RawCapture == null) return Task.CompletedTask;

            var surface = payload.EnsureSurface();
            if (surface?.Image == null) return Task.CompletedTask;

            IEffect effect = ResolveEffect();
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

        private IEffect ResolveEffect()
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
                return new DropShadowEffect();
            }

            if (string.Equals(effectName, "TornEdge", StringComparison.OrdinalIgnoreCase))
            {
                return new TornEdgeEffect();
            }

            if (string.Equals(effectName, "Invert", StringComparison.OrdinalIgnoreCase))
            {
                return new InvertEffect();
            }

            if (string.Equals(effectName, "Grayscale", StringComparison.OrdinalIgnoreCase))
            {
                return new GrayscaleEffect();
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
