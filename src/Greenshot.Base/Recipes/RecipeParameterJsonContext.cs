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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// The types of the core step parameters which hold more than one value (see <see cref="Contracts.StepParameterAttribute.ValueType"/>),
    /// generated at build time. A plugin with such parameters registers its own context with
    /// <see cref="Pipeline.IStepRegistry.RegisterJsonTypes"/>.
    /// </summary>
    [JsonSerializable(typeof(List<ConditionalBranch>))]
    [JsonSerializable(typeof(List<PromptChoice>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(List<Dictionary<string, object>>))]
    internal partial class RecipeParameterJsonContext : JsonSerializerContext
    {
    }
}
