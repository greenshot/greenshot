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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Document.Editor
{
    /// <summary>
    /// A plugin registers one of these (SimpleServiceProvider) to map its containers to a PluginElement and back
    /// </summary>
    public interface IPluginElementConverter
    {
        /// <summary>
        /// The stable name of the element, e.g. "greenshot.barcode", never a CLR type name
        /// </summary>
        string Kind { get; }

        /// <summary>
        /// The version of the data this converter writes, it reads all versions up to this one
        /// </summary>
        int Version { get; }

        bool CanConvert(IDrawableContainer container);

        /// <summary>
        /// The element for the container: the given element (with id, area, kind, version and fallback) with the data
        /// of the container. Resources the data links to are added to resources and listed in Resources.
        /// </summary>
        PluginElement ToElement(IDrawableContainer container, PluginElement element, IDictionary<Guid, Resource> resources);

        IDrawableContainer ToContainer(ISurface surface, PluginElement element, IReadOnlyDictionary<Guid, Resource> resources);
    }
}
