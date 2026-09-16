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

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Categories of recipe operations that require explicit user consent.
    /// </summary>
    public enum RecipeGateType
    {
        ExternalCommand,
        NetworkAccess,
        FileSystemAccess,
        Custom
    }

    /// <summary>
    /// Represents a security-gated action within a capture recipe (e.g. process execution).
    /// </summary>
    public class RecipeGatedAction : IEquatable<RecipeGatedAction>
    {
        public RecipeGateType GateType { get; set; }
        public string Target { get; set; }
        public string DescriptionKey { get; set; }

        public RecipeGatedAction()
        {
        }

        public RecipeGatedAction(RecipeGateType gateType, string target, string descriptionKey = null)
        {
            GateType = gateType;
            Target = target;
            DescriptionKey = descriptionKey ?? (gateType == RecipeGateType.ExternalCommand ? "recipe_gate_external_command" : "recipe_gate_custom");
        }

        public bool Equals(RecipeGatedAction other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return GateType == other.GateType &&
                   string.Equals(Target, other.Target, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) => Equals(obj as RecipeGatedAction);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + GateType.GetHashCode();
                hash = hash * 23 + (Target != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Target) : 0);
                return hash;
            }
        }

        public override string ToString() => string.IsNullOrWhiteSpace(Target) ? GateType.ToString() : $"{GateType}: {Target}";
    }

    /// <summary>
    /// Implemented by capture pipeline steps or export destinations that perform gated or sensitive actions
    /// (e.g. launching external processes, sending data over networks), requiring user review and consent.
    /// </summary>
    public interface IRequiresRecipeAuthorization
    {
        /// <summary>
        /// Returns the gated actions requested by this component.
        /// </summary>
        IEnumerable<RecipeGatedAction> GetGatedActions();
    }
}
