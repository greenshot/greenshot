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

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Categories of the Windows policy templates (ADMX) besides the one per section, used with [PolicyCategory].
    /// See docs/vNext/policies.md
    /// </summary>
    public static class PolicyCategories
    {
        /// <summary>
        /// The settings administrators set most: plugins and destinations, access to the settings, updates, AI tools, output.
        /// A setting with this category is only in this folder of the policy editor, not in the one of its section.
        /// </summary>
        public const string Common = "Common settings";
    }
}
