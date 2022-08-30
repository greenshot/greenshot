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
    /// This is a FilterContainer for the obfuscator filters like blur and pixelate.
    /// </summary>
    [Serializable]
    public class ObfuscateContainer : FilterContainer
    {
        public ObfuscateContainer(ISurface parent) : base(parent)
        {
            Init();
        }

        protected override void InitializeFields()
        {
            base.InitializeFields();
            AddField(GetType(), FieldType.PREPARED_FILTER_OBFUSCATE, PreparedFilter.PIXELIZE);
        }

        protected override void OnDeserialized(StreamingContext context)
        {
            Init();
        }

        private void Init()
        {
            FieldChanged += ObfuscateContainer_OnFieldChanged;
            ConfigurePreparedFilters();
            CreateDefaultAdorners();
        }

        protected void ObfuscateContainer_OnFieldChanged(object sender, FieldChangedEventArgs e)
        {
            if (sender.Equals(this))
            {
                if (Equals(e.Field.FieldType, FieldType.PREPARED_FILTER_OBFUSCATE))
                {
                    ConfigurePreparedFilters();
                }
            }
        }

        private void ConfigurePreparedFilters()
        {
            PreparedFilter preset = (PreparedFilter) GetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE);
            while (Filters.Count > 0)
            {
                Remove(Filters[0]);
            }

            switch (preset)
            {
                case PreparedFilter.BLUR:
                    Add(new BlurFilter(this));
                    break;
                case PreparedFilter.PIXELIZE:
                    Add(new PixelizationFilter(this));
                    break;
            }
        }
    }
}