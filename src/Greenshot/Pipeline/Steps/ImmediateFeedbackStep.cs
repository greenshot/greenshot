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
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Helpers;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step providing immediate capture acquisition feedback (e.g. camera sound, flash).
    /// Dynamic configuration evaluation allows live settings to dictate feedback.
    /// </summary>
    public class ImmediateFeedbackStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImmediateFeedbackStep));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        public string Name { get; }
        public RecipeStepConfig Config { get; }

        public ImmediateFeedbackStep(RecipeStepConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ImmediateFeedbackStep";
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            if (payload?.RawCapture == null) return Task.CompletedTask;

            // Resolve shutter sound setting dynamically
            bool playSound = ResolvePlaySound(context);
            if (playSound)
            {
                SoundHelper.Play();
            }

            return Task.CompletedTask;
        }

        private bool ResolvePlaySound(CaptureFlowContext context)
        {
            // Priority 1: Runtime context override
            if (context.Properties.TryGetValue("PlayShutterSound", out var ctxVal) && ctxVal is bool ctxBool)
            {
                return ctxBool;
            }

            // Priority 2: Step parameter pre-definition
            if (Config.Parameters.TryGetValue("PlaySound", out var stepVal) && stepVal != null)
            {
                if (stepVal is bool stepBool) return stepBool;
                if (bool.TryParse(stepVal.ToString(), out bool parsedBool)) return parsedBool;
            }

            // Priority 3: Dynamic user configuration evaluation
            return CoreConfig.PlayCameraSound;
        }
    }
}
