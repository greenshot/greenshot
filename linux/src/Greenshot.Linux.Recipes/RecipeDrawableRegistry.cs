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

using System.Collections.Concurrent;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes
{
    /// <summary>
    /// The annotation types besides the built-in ones of the Annotation step, with the element they create on the surface
    /// and their properties; on Windows plugins register them (QRCode and Barcode of ZXing), on Linux they are built in.
    /// </summary>
    public sealed class RecipeDrawableRegistry
    {
        private readonly ConcurrentDictionary<string, Func<EditorSurface, Dictionary<string, object>, CaptureFlowContext, DrawableElement>> _factories =
            new ConcurrentDictionary<string, Func<EditorSurface, Dictionary<string, object>, CaptureFlowContext, DrawableElement>>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, IReadOnlyList<ParameterContract>> _properties =
            new ConcurrentDictionary<string, IReadOnlyList<ParameterContract>>(StringComparer.OrdinalIgnoreCase);

        public static RecipeDrawableRegistry Instance { get; } = new RecipeDrawableRegistry();

        public void RegisterDrawableFactory(string drawableType, Func<EditorSurface, Dictionary<string, object>, CaptureFlowContext, DrawableElement> factory,
            IEnumerable<ParameterContract> properties = null)
        {
            if (string.IsNullOrEmpty(drawableType)) throw new ArgumentNullException(nameof(drawableType));
            _factories[drawableType] = factory ?? throw new ArgumentNullException(nameof(factory));
            if (properties != null)
            {
                _properties[drawableType] = AnnotationProperties.WithCommon(properties);
            }
            else
            {
                _properties.TryRemove(drawableType, out _);
            }
        }

        public bool IsRegistered(string drawableType) => !string.IsNullOrEmpty(drawableType) && _factories.ContainsKey(drawableType);

        public IReadOnlyCollection<string> RegisteredDrawableTypes => _factories.Keys.ToList();

        /// <summary>
        /// The properties of the type, the built-in types included; null when the type is unknown
        /// </summary>
        public IReadOnlyList<ParameterContract> GetProperties(string drawableType)
        {
            if (string.IsNullOrEmpty(drawableType)) return null;
            return AnnotationProperties.ForBuiltIn(drawableType) ?? (_properties.TryGetValue(drawableType, out var properties) ? properties : null);
        }

        public DrawableElement CreateDrawable(string drawableType, EditorSurface surface, Dictionary<string, object> parameters, CaptureFlowContext context) =>
            _factories.TryGetValue(drawableType, out var factory) ? factory(surface, parameters, context) : null;
    }
}
