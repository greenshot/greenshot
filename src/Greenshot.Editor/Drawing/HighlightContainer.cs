/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Runtime.Serialization;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Description of ObfuscateContainer.
    /// </summary>
    [Serializable]
    public class HighlightContainer : FilterContainer
    {
        public HighlightContainer(ISurface parent) : base(parent)
        {
            Init();
        }

        /// <summary>
        /// Use settings from base, extend with our own field
        /// </summary>
        protected override void InitializeFields()
        {
            base.InitializeFields();
            AddField(GetType(), FieldType.PREPARED_FILTER_HIGHLIGHT, PreparedFilter.TEXT_HIGHTLIGHT);
        }

        protected override void OnDeserialized(StreamingContext context)
        {
            Init();
        }

        private void Init()
        {
            FieldChanged += HighlightContainer_OnFieldChanged;
            ConfigurePreparedFilters();
            CreateDefaultAdorners();
        }

        protected void HighlightContainer_OnFieldChanged(object sender, FieldChangedEventArgs e)
        {
            if (!sender.Equals(this))
            {
                return;
            }

            if (Equals(e.Field.FieldType, FieldType.PREPARED_FILTER_HIGHLIGHT))
            {
                ConfigurePreparedFilters();
            }
        }

        private void ConfigurePreparedFilters()
        {
            object fieldValue = GetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT);

            // Guard against null value which can occur after undo operations
            if (fieldValue == null)
            {
                return;
            }

            PreparedFilter preset = (PreparedFilter)fieldValue;
            while (Filters.Count > 0)
            {
                Remove(Filters[0]);
            }

            switch (preset)
            {
                case PreparedFilter.TEXT_HIGHTLIGHT:
                    Add(new HighlightFilter(this));
                    break;
                case PreparedFilter.AREA_HIGHLIGHT:
                    var brightnessFilter = new BrightnessFilter(this)
                    {
                        Invert = true
                    };
                    Add(brightnessFilter);
                    var blurFilter = new BlurFilter(this)
                    {
                        Invert = true
                    };
                    Add(blurFilter);
                    break;
                case PreparedFilter.GRAYSCALE:
                    AbstractFilter f = new GrayscaleFilter(this)
                    {
                        Invert = true
                    };
                    Add(f);
                    break;
                case PreparedFilter.MAGNIFICATION:
                    Add(new MagnifierFilter(this));
                    break;
            }
        }
    }
}