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
using System.Runtime.CompilerServices;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Document.Editor
{
    /// <summary>
    /// What the editor doesn't hold itself: the id of the element each container was made from, so ids survive a trip
    /// through the editor, and the elements the editor can't edit, kept with the container which shows them (a plugin element without its plugin)
    /// or with the surface (an element of an unknown type), so they are written back unchanged
    /// </summary>
    internal static class KeptElements
    {
        internal sealed class Kept
        {
            public Kept(Element element, IReadOnlyList<Resource> resources)
            {
                Element = element;
                Resources = resources;
            }

            public Element Element { get; }

            public IReadOnlyList<Resource> Resources { get; }
        }

        internal static readonly ConditionalWeakTable<IDrawableContainer, StrongBox<Guid>> Ids = new();

        internal static readonly ConditionalWeakTable<IDrawableContainer, Kept> ByContainer = new();

        internal static readonly ConditionalWeakTable<ISurface, List<Kept>> BySurface = new();
    }
}
