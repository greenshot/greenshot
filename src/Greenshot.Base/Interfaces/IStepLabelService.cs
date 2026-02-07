/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.ComponentModel;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Base.Interfaces
{
    public enum StepLabelMode
    {
        Number = 0,
        Letter = 1
    }

    public interface IStepLabelService : INotifyPropertyChanged
    {
        /// <summary>
        /// Start value of the counter
        /// </summary>
        int CounterStart { get; set; }

        /// <summary>
        /// Current step label mode (Number or Letter)
        /// </summary>
        StepLabelMode Mode { get; set; }

        /// <summary>
        /// Convenience property: true when Mode is Letter
        /// </summary>
        bool UseLetterCounter { get; set; }

        /// <summary>
        /// Current counter group, incremented on reset
        /// </summary>
        int CounterGroup { get; }

        /// <summary>
        /// Reset the counter by starting a new counter group
        /// </summary>
        void ResetCounter();

        /// <summary>
        /// Register a step label
        /// </summary>
        void AddStepLabel(IDrawableContainer label);

        /// <summary>
        /// Unregister a step label
        /// </summary>
        void RemoveStepLabel(IDrawableContainer label);

        /// <summary>
        /// Count all visible step labels up to the supplied one
        /// </summary>
        int CountStepLabels(IDrawableContainer stopAt);

        /// <summary>
        /// Count visible step labels of the specified mode and group, up to the supplied one
        /// </summary>
        int CountStepLabels(IDrawableContainer stopAt, StepLabelMode mode, int counterGroup);

        /// <summary>
        /// Sort step labels by their stored number (used after deserialization)
        /// </summary>
        void SortByNumber();
    }
}
