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

using System;
using System.Collections.Generic;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// A provider built from a list of names with a function each; names ending in ".*" get the rest of the name.
    /// Plugins can use it for their own namespace.
    /// </summary>
    public class VariableProvider : IVariableProvider
    {
        private readonly Dictionary<string, (VariableDescriptor Descriptor, Func<VariableScope, object> Get)> _values =
            new Dictionary<string, (VariableDescriptor, Func<VariableScope, object>)>(StringComparer.OrdinalIgnoreCase);

        private readonly List<(VariableDescriptor Descriptor, string Prefix, Func<string, VariableScope, object> Get)> _wildcards =
            new List<(VariableDescriptor, string, Func<string, VariableScope, object>)>();

        private readonly List<VariableDescriptor> _descriptors = new List<VariableDescriptor>();

        public VariableProvider(string ns)
        {
            Namespace = ns ?? throw new ArgumentNullException(nameof(ns));
        }

        public string Namespace { get; }

        public IEnumerable<VariableDescriptor> Variables => _descriptors;

        /// <summary>Add "namespace.name"</summary>
        public VariableDescriptor Add<T>(string name, VariableTrust trust, string description, Func<VariableScope, object> get)
        {
            var descriptor = new VariableDescriptor($"{Namespace}.{name}", typeof(T), trust, description);
            _values[descriptor.Name] = (descriptor, get);
            _descriptors.Add(descriptor);
            return descriptor;
        }

        /// <summary>Add "namespace.prefix.*": the function gets what follows the prefix</summary>
        public VariableDescriptor AddWildcard<T>(string prefix, VariableTrust trust, string description, Func<string, VariableScope, object> get)
        {
            string fullPrefix = string.IsNullOrEmpty(prefix) ? Namespace + "." : $"{Namespace}.{prefix}.";
            var descriptor = new VariableDescriptor(fullPrefix + "*", typeof(T), trust, description);
            _wildcards.Add((descriptor, fullPrefix, get));
            _descriptors.Add(descriptor);
            return descriptor;
        }

        public bool TryGetValue(string name, VariableScope scope, out object value)
        {
            if (_values.TryGetValue(name, out var entry))
            {
                value = entry.Get(scope);
                return true;
            }
            // The longest prefix wins: "user.env.X" before a "user.*"
            (VariableDescriptor Descriptor, string Prefix, Func<string, VariableScope, object> Get) best = default;
            foreach (var wildcard in _wildcards)
            {
                if (name.Length > wildcard.Prefix.Length && name.StartsWith(wildcard.Prefix, StringComparison.OrdinalIgnoreCase)
                    && (best.Prefix == null || wildcard.Prefix.Length > best.Prefix.Length))
                {
                    best = wildcard;
                }
            }
            if (best.Prefix != null)
            {
                value = best.Get(name.Substring(best.Prefix.Length), scope);
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>The descriptor of a full name, null when unknown</summary>
        public VariableDescriptor Find(string name)
        {
            if (_values.TryGetValue(name, out var entry)) return entry.Descriptor;
            VariableDescriptor best = null;
            foreach (var wildcard in _wildcards)
            {
                if (name.Length > wildcard.Prefix.Length && name.StartsWith(wildcard.Prefix, StringComparison.OrdinalIgnoreCase)
                    && (best == null || wildcard.Prefix.Length > best.Name.Length - 1))
                {
                    best = wildcard.Descriptor;
                }
            }
            return best;
        }
    }
}
