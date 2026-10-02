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

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// What the recipe manager shows about a recipe: what it does in plain words, its approval, and the changes against the
    /// built-in recipe it replaces.
    /// </summary>
    public sealed class RecipeDetails
    {
        /// <summary>
        /// The steps in plain words (written by Greenshot from the recipe), uploads, programs and file access included
        /// </summary>
        public IReadOnlyList<string> WhatItDoes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The triggers in plain words, with their state
        /// </summary>
        public IReadOnlyList<string> Triggers { get; set; } = Array.Empty<string>();

        /// <summary>
        /// When the user approved the file, null for a built-in recipe or an unapproved file
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// SHA-256 of the approved file content
        /// </summary>
        public string ApprovedHash { get; set; }

        /// <summary>
        /// The file on disk is the approved content
        /// </summary>
        public bool IsApprovalCurrent { get; set; }

        /// <summary>
        /// The permissions given in the approval (uploads, external commands, file access)
        /// </summary>
        public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The AI tool which wrote the file, null otherwise
        /// </summary>
        public string ProposedBy { get; set; }

        /// <summary>
        /// The changed lines against the built-in recipe it replaces, null when it doesn't replace one
        /// </summary>
        public string BuiltInDiff { get; set; }

        /// <summary>
        /// A recipe: the automatic steps (extensions) which change it now
        /// </summary>
        public IReadOnlyList<string> ChangedBy { get; set; } = Array.Empty<string>();

        /// <summary>
        /// An automatic step: which recipes it changes, where and when; empty for a recipe
        /// </summary>
        public IReadOnlyList<string> Reach { get; set; } = Array.Empty<string>();

        public bool IsExtension => Reach.Count > 0;
    }
}
