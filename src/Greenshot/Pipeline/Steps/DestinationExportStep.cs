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
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Editor.Destinations;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step that resolves and dispatches captures to destinations.
    /// Evaluates target destinations dynamically from global configuration if not pre-defined.
    /// </summary>
    public class DestinationExportStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DestinationExportStep));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        private readonly IDestinationDispatcher _dispatcher;

        public string Name { get; }
        public RecipeStepConfig Config { get; }

        public DestinationExportStep(RecipeStepConfig config, IDestinationDispatcher dispatcher = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "DestinationExportStep";
            _dispatcher = dispatcher ?? new DestinationDispatcher();
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            context.State = CaptureFlowState.Exporting;

            var designations = ResolveDestinationDesignations(context).ToList();
            List<IDestination> destinations = new List<IDestination>();

            foreach (var designation in designations)
            {
                if (string.IsNullOrWhiteSpace(designation)) continue;

                // Case-insensitive destination matching
                var dest = DestinationHelper.GetAllDestinations()
                    .FirstOrDefault(d => string.Equals(d.Designation, designation, StringComparison.OrdinalIgnoreCase));

                if (dest == null)
                {
                    dest = SimpleServiceProvider.Current.GetAllInstances<IDestination>()
                        .FirstOrDefault(d => string.Equals(d.Designation, designation, StringComparison.OrdinalIgnoreCase));
                }

                if (dest == null && string.Equals(designation, EditorDestination.DESIGNATION, StringComparison.OrdinalIgnoreCase))
                {
                    dest = new EditorDestination();
                }

                if (dest != null && dest.IsActive)
                {
                    destinations.Add(dest);
                }
                else if (dest != null && !dest.IsActive)
                {
                    Log.WarnFormat("Destination '{0}' was resolved but is marked inactive in configuration.", dest.Designation);
                }
            }

            if (destinations.Count > 0)
            {
                Log.InfoFormat("DestinationExportStep: Resolved {0} destination(s): [{1}]",
                    destinations.Count, string.Join(", ", destinations.Select(d => d.Designation)));
                context.LogStep($"Exporting capture to {destinations.Count} destination(s): {string.Join(", ", destinations.Select(d => d.Designation))}");
            }
            else
            {
                Log.WarnFormat("DestinationExportStep: No active destinations resolved for designations: [{0}]", string.Join(", ", designations));
                context.LogStep($"Warning: No active destinations resolved for designations: {string.Join(", ", designations)}");
            }

            await _dispatcher.DispatchAsync(context, destinations, cancellationToken).ConfigureAwait(false);
        }

        private IEnumerable<string> ResolveDestinationDesignations(CaptureFlowContext context)
        {
            // Priority 1: Context property override (e.g. from trigger or caller)
            if (context.Properties.TryGetValue("OverrideDestinations", out var ctxVal) && ctxVal != null)
            {
                if (ctxVal is IEnumerable<string> ctxDests) return ctxDests;
                if (ctxVal is string ctxStr && !string.IsNullOrWhiteSpace(ctxStr)) return new[] { ctxStr };
            }

            // Priority 2: Explicit step parameter configuration (check list variants)
            var stepDests = Config.GetParameter<List<string>>("DestinationDesignations")
                ?? Config.GetParameter<List<string>>("destinationDesignations")
                ?? Config.GetParameter<List<string>>("Destinations")
                ?? Config.GetParameter<List<string>>("destinations");
            if (stepDests != null && stepDests.Count > 0)
            {
                return stepDests;
            }

            // Priority 2b: Single string designation
            string singleDest = Config.GetParameter<string>("Destination")
                ?? Config.GetParameter<string>("destination")
                ?? Config.GetParameter<string>("DestinationDesignation")
                ?? Config.GetParameter<string>("destinationDesignation")
                ?? Config.GetParameter<string>("Destinations")
                ?? Config.GetParameter<string>("destinations")
                ?? Config.GetParameter<string>("DestinationDesignations")
                ?? Config.GetParameter<string>("destinationDesignations");
            if (!string.IsNullOrWhiteSpace(singleDest))
            {
                return new[] { singleDest };
            }

            // Priority 3: Dynamic user configuration evaluation
            return (IEnumerable<string>)CoreConfig.OutputDestinations ?? Array.Empty<string>();
        }
    }
}
