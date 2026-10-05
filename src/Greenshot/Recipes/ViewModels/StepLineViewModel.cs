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

using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Recipes;

namespace Greenshot.Recipes.ViewModels
{
    /// <summary>
    /// A line of "What it does"
    /// </summary>
    public sealed class StepLineViewModel
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public Thickness Margin { get; set; }
        public Brush Foreground { get; set; }
        public FontWeight FontWeight { get; set; } = FontWeights.Normal;
    }
}
