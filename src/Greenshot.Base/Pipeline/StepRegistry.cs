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
using System.Collections.Concurrent;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Thread-safe registry mapping step types to step factory delegates.
    /// </summary>
    public class StepRegistry : IStepRegistry
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StepRegistry));
        private readonly ConcurrentDictionary<string, Func<RecipeStepConfig, ICaptureStep>> _factories =
            new ConcurrentDictionary<string, Func<RecipeStepConfig, ICaptureStep>>(StringComparer.OrdinalIgnoreCase);

        private static StepRegistry _instance;
        public static StepRegistry Instance => _instance ??= new StepRegistry();

        public void RegisterStepFactory(string stepType, Func<RecipeStepConfig, ICaptureStep> factory)
        {
            if (string.IsNullOrEmpty(stepType)) throw new ArgumentNullException(nameof(stepType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _factories[stepType] = factory;
            Log.DebugFormat("Registered step factory for step type '{0}'", stepType);
        }

        public ICaptureStep CreateStep(RecipeStepConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.StepType)) return null;

            if (_factories.TryGetValue(config.StepType, out var factory))
            {
                return factory(config);
            }

            Log.WarnFormat("No step factory registered for step type '{0}'", config.StepType);
            return null;
        }
    }
}
