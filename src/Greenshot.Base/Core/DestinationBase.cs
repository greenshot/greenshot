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
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Base class for destinations (replaces AbstractDestination): the defaults of <see cref="IDestination"/>.
    /// </summary>
    public abstract class DestinationBase : IDestination
    {
        private static readonly IReadOnlyList<IDestination> NoDestinations = Array.Empty<IDestination>();

        protected static ICoreConfiguration CoreConfiguration => IniConfigRegistry.GetSection<ICoreConfiguration>();

        public abstract string Designation { get; }

        public abstract DestinationDescriptor Descriptor { get; }

        /// <summary>
        /// Shortcut for the display name of the descriptor.
        /// </summary>
        public string DisplayName => Descriptor?.DisplayName ?? Designation;

        /// <summary>
        /// True when the user excluded the destination in the configuration.
        /// </summary>
        protected bool IsExcludedByConfiguration
        {
            get
            {
                var excluded = CoreConfiguration?.ExcludeDestinations;
                return excluded != null && excluded.Contains(Designation);
            }
        }

        public virtual bool IsAvailableFor(ICaptureDetails metadata) => !IsExcludedByConfiguration;

        public virtual ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken)
        {
            return new ValueTask<IReadOnlyList<IDestination>>(NoDestinations);
        }

        public abstract Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken);

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Sort order of destinations: priority, then display name (was IComparable on the destination).
    /// </summary>
    public sealed class DestinationComparer : IComparer<IDestination>
    {
        public static DestinationComparer Instance { get; } = new DestinationComparer();

        public int Compare(IDestination x, IDestination y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return 1;
            if (y == null) return -1;
            int byPriority = (x.Descriptor?.Priority ?? 10).CompareTo(y.Descriptor?.Priority ?? 10);
            return byPriority != 0 ? byPriority : string.Compare(x.Descriptor?.DisplayName, y.Descriptor?.DisplayName, StringComparison.Ordinal);
        }
    }
}
