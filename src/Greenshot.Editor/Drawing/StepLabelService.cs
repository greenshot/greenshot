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
    /// Global singleton service that manages all step label registration, counting, mode and group tracking.
    /// Registered via SimpleServiceProvider. Each label stores its own assigned value; this service
    /// handles assignment and renumbering.
    /// </summary>
    public class StepLabelService : IStepLabelService
    {
        private readonly List<StepLabelContainer> _stepLabels = new List<StepLabelContainer>();

        private int _counterStart = 1;
        private StepLabelMode _mode = StepLabelMode.Number;
        private int _counterGroup;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler LabelsChanged;

        public int CounterStart
        {
            get => _counterStart;
            set
            {
                if (_counterStart == value) return;
                _counterStart = value;
                Renumber();
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
                Renumber();
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
            Renumber();
        }

        public void AddStepLabel(IDrawableContainer label)
        {
            if (label is StepLabelContainer sl && !_stepLabels.Contains(sl))
            {
                _stepLabels.Add(sl);
                Renumber();
            }
        }

        public void RemoveStepLabel(IDrawableContainer label)
        {
            if (label is StepLabelContainer sl && _stepLabels.Remove(sl))
            {
                Renumber();
            }
        }

        /// <summary>
        /// Count all registered step labels up to the supplied one (regardless of mode/group).
        /// Used by toolbar icon count display.
        /// </summary>
        public int CountStepLabels(IDrawableContainer stopAt)
        {
            int number = _counterStart;
            foreach (var sl in _stepLabels)
            {
                if (sl.Equals(stopAt)) break;
                number++;
            }
            return number;
        }

        /// <summary>
        /// Reassign values to all registered labels based on current settings.
        /// Groups labels by (LabelMode, CounterGroup) and assigns sequential values starting from CounterStart.
        /// </summary>
        public void Renumber()
        {
            var counters = new Dictionary<(StepLabelMode mode, int group), int>();

            foreach (var sl in _stepLabels)
            {
                if (sl.HasFixedValue)
                {
                    // Pasted or loaded label - keep its preserved value
                    continue;
                }

                var key = (sl.LabelMode, sl.CounterGroup);
                if (!counters.TryGetValue(key, out int current))
                {
                    current = _counterStart;
                }

                sl.AssignedValue = current;
                counters[key] = current + 1;
            }

            LabelsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clear fixed-value flags on all labels so they participate in renumbering
        /// </summary>
        public void ClearFixedValues()
        {
            foreach (var sl in _stepLabels)
            {
                sl.HasFixedValue = false;
            }
        }

        /// <summary>
        /// Sort step labels by their stored number (used after deserialization to restore order)
        /// </summary>
        public void SortByNumber()
        {
            _stepLabels.Sort((a, b) => a.Number.CompareTo(b.Number));
        }
    }
}
