// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Runtime.Serialization;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addon.LegacyEditor.Drawing.Filters;
using Greenshot.Addons.Interfaces.Drawing;

namespace Greenshot.Addon.LegacyEditor.Drawing
{
	/// <summary>
	///     Description of ObfuscateContainer.
	/// </summary>
	[Serializable]
	public class HighlightContainer : FilterContainer
	{
		public HighlightContainer(Surface parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
		{
			Init();
		}

		/// <summary>
		///     Use settings from base, extend with our own field
		/// </summary>
		protected override void InitializeFields()
		{
			base.InitializeFields();
			AddField(GetType(), FieldTypes.PREPARED_FILTER_HIGHLIGHT, PreparedFilter.TextHightlight);
		}

		protected override void OnDeserialized(StreamingContext context)
		{
			Init();
		}

		private void Init()
		{
			FieldChanged += HighlightContainer_OnFieldChanged;
			ConfigurePreparedFilters();
		}

		protected void HighlightContainer_OnFieldChanged(object sender, FieldChangedEventArgs e)
		{
			if (!sender.Equals(this))
			{
				return;
			}
			if (Equals(e.Field.FieldType, FieldTypes.PREPARED_FILTER_HIGHLIGHT))
			{
				ConfigurePreparedFilters();
			}
		}

		private void ConfigurePreparedFilters()
		{
			var preset = (PreparedFilter) GetFieldValue(FieldTypes.PREPARED_FILTER_HIGHLIGHT);
			while (Filters.Count > 0)
			{
				Remove(Filters[0]);
			}
			switch (preset)
			{
				case PreparedFilter.TextHightlight:
					Add(new HighlightFilter(this, _editorConfiguration));
					break;
				case PreparedFilter.AreaHighlight:
					var brightnessFilter = new BrightnessFilter(this, _editorConfiguration)
					{
						Invert = true
					};
					Add(brightnessFilter);
					var blurFilter = new BlurFilter(this, _editorConfiguration)
					{
						Invert = true
					};
					Add(blurFilter);
					break;
				case PreparedFilter.Grayscale:
                    AbstractFilter f = new GrayscaleFilter(this, _editorConfiguration)
                    {
                        Invert = true
                    };
                    Add(f);
					break;
				case PreparedFilter.Magnification:
					Add(new MagnifierFilter(this, _editorConfiguration));
					break;
			}
		}
	}
}