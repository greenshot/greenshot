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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Encapsulates all step label management: registration, counting, mode and group tracking.
    /// </summary>
    public class StepLabelService : IStepLabelService
    {
        private readonly List<StepLabelContainer> _stepLabels = new List<StepLabelContainer>();
        private readonly Action _invalidate;
        private readonly Func<IDrawableContainer, bool> _isOnSurface;

        private int _counterStart = 1;
        private StepLabelMode _mode = StepLabelMode.Number;
        private int _counterGroup;

        public event PropertyChangedEventHandler PropertyChanged;

        public StepLabelService(Action invalidate, Func<IDrawableContainer, bool> isOnSurface)
        {
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
            _isOnSurface = isOnSurface ?? throw new ArgumentNullException(nameof(isOnSurface));
        }

        public int CounterStart
        {
            get => _counterStart;
            set
            {
                if (_counterStart == value) return;
                _counterStart = value;
                _invalidate();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CounterStart)));
            }
        }

        public StepLabelMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;
                _mode = value;
                _invalidate();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mode)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseLetterCounter)));
            }
        }

        public bool UseLetterCounter
        {
            get => _mode == StepLabelMode.Letter;
            set => Mode = value ? StepLabelMode.Letter : StepLabelMode.Number;
        }

        public int CounterGroup => _counterGroup;

        public void ResetCounter()
        {
            _counterGroup++;
            _invalidate();
        }

        public void AddStepLabel(IDrawableContainer label)
        {
            if (label is StepLabelContainer sl && !_stepLabels.Contains(sl))
            {
                _stepLabels.Add(sl);
            }
        }

        public void RemoveStepLabel(IDrawableContainer label)
        {
            if (label is StepLabelContainer sl)
            {
                _stepLabels.Remove(sl);
            }
        }

        /// <summary>
        /// Count all visible step labels up to the supplied one (regardless of mode/group)
        /// </summary>
        public int CountStepLabels(IDrawableContainer stopAt)
        {
            int number = _counterStart;
            foreach (var sl in _stepLabels)
            {
                if (sl.Equals(stopAt)) break;
                if (_isOnSurface(sl)) number++;
            }
            return number;
        }

        /// <summary>
        /// Count visible step labels of the specified mode and group, up to the supplied one
        /// </summary>
        public int CountStepLabels(IDrawableContainer stopAt, StepLabelMode mode, int counterGroup)
        {
            int number = _counterStart;
            foreach (var sl in _stepLabels)
            {
                if (sl.Equals(stopAt)) break;
                if (_isOnSurface(sl) && sl.LabelMode == mode && sl.CounterGroup == counterGroup)
                {
                    number++;
                }
            }
            return number;
        }

        public void SortByNumber()
        {
            _stepLabels.Sort((a, b) => a.Number.CompareTo(b.Number));
        }
    }
}
