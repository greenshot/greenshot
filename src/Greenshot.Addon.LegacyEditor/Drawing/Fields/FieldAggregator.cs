﻿#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Greenshot.Addon.LegacyEditor.Configuration.Impl;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Drawing;

#endregion

namespace Greenshot.Addon.LegacyEditor.Drawing.Fields
{
	/// <summary>
	///     Represents the current set of properties for the editor.
	///     When one of EditorProperties' properties is updated, the change will be promoted
	///     to all bound elements.
	///     * If an element is selected:
	///     This class represents the element's properties
	///     * I n>1 elements are selected:
	///     This class represents the properties of all elements.
	///     Properties that do not apply for ALL selected elements are null (or 0 respectively)
	///     If the property values of the selected elements differ, the value of the last bound element wins.
	/// </summary>
	[Serializable]
	public sealed class FieldAggregator : AbstractFieldHolder
	{
		private readonly IDrawableContainerList _boundContainers;
		private bool _internalUpdateRunning;

		public FieldAggregator(ISurface parent, IEditorConfiguration editorConfiguration) : base(editorConfiguration) 
		{
			foreach (var fieldType in FieldTypes.Values)
			{
				var field = new Field(fieldType, GetType());
				AddField(field);
			}
			_boundContainers = new DrawableContainerList
			{
				Parent = parent
			};
		}

		public override void AddField(IField field)
		{
			base.AddField(field);
			field.PropertyChanged += OwnPropertyChanged;
		}

		public void BindElements(IDrawableContainerList dcs)
		{
			foreach (var dc in dcs)
			{
				BindElement(dc);
			}
		}

		public void BindElement(IDrawableContainer dc)
		{
		    if (!(dc is DrawableContainer container) || _boundContainers.Contains(container))
			{
				return;
			}
			_boundContainers.Add(container);
			container.ChildrenChanged += (sender, args) => UpdateFromBoundElements();
			UpdateFromBoundElements();
		}

		public void BindAndUpdateElement(IDrawableContainer dc)
		{
			UpdateElement(dc);
			BindElement(dc);
		}

		public void UpdateElement(IDrawableContainer dc)
		{
		    if (!(dc is DrawableContainer container))
			{
				return;
			}
			_internalUpdateRunning = true;
			foreach (var field in GetFields())
			{
				if (container.HasField(field.FieldType) && field.HasValue)
				{
					//if(LOG.IsDebugEnabled) Log.Debug().WriteLine("   "+field+ ": "+field.Value);
					container.SetFieldValue(field.FieldType, field.Value);
				}
			}
			_internalUpdateRunning = false;
		}

		public void UnbindElement(IDrawableContainer dc)
		{
			if (_boundContainers.Contains(dc))
			{
				_boundContainers.Remove(dc);
				UpdateFromBoundElements();
			}
		}

		public void Clear()
		{
			ClearFields();
			_boundContainers.Clear();
			UpdateFromBoundElements();
		}

		/// <summary>
		///     sets all field values to null, however does not remove fields
		/// </summary>
		private void ClearFields()
		{
			_internalUpdateRunning = true;
			foreach (var field in GetFields())
			{
				field.Value = null;
			}
			_internalUpdateRunning = false;
		}

		/// <summary>
		///     Updates this instance using the respective fields from the bound elements.
		///     Fields that do not apply to every bound element are set to null, or 0 respectively.
		///     All other fields will be set to the field value of the least bound element.
		/// </summary>
		private void UpdateFromBoundElements()
		{
			ClearFields();
			_internalUpdateRunning = true;
			foreach (var field in FindCommonFields())
			{
				SetFieldValue(field.FieldType, field.Value);
			}
			_internalUpdateRunning = false;
		}

		private IList<IField> FindCommonFields()
		{
		    if (_boundContainers.Count <= 0)
		    {
		        return new List<IField>();
		    }

		    // take all fields from the least selected container...
		    if (!(_boundContainers[_boundContainers.Count - 1] is DrawableContainer leastSelectedContainer))
		    {
		        return new List<IField>();
		    }

		    var returnFields = leastSelectedContainer.GetFields();
		    for (var i = 0; i < _boundContainers.Count - 1; i++)
		    {
		        if (!(_boundContainers[i] is DrawableContainer dc))
		        {
		            continue;
		        }

		        IList<IField> fieldsToRemove = new List<IField>();
		        foreach (var field in returnFields)
		        {
		            // ... throw out those that do not apply to one of the other containers
		            if (!dc.HasField(field.FieldType))
		            {
		                fieldsToRemove.Add(field);
		            }
		        }
		        foreach (var field in fieldsToRemove)
		        {
		            returnFields.Remove(field);
		        }
		    }
		    return returnFields ?? new List<IField>();
		}

        /// <summary>
        /// This is called when a property changes
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="propertyChangedEventArgs">PropertyChangedEventArgs</param>
		public void OwnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			var field = (IField) sender;
			if (_internalUpdateRunning || field.Value == null)
			{
				return;
			}
			foreach (var drawableContainer1 in _boundContainers.ToList())
			{
				var drawableContainer = (DrawableContainer) drawableContainer1;
				if (!drawableContainer.HasField(field.FieldType))
				{
					continue;
				}
				var drawableContainerField = drawableContainer.GetField(field.FieldType);
				// Notify before change, so we can e.g. invalidate the area
				drawableContainer.BeforeFieldChange(drawableContainerField, field.Value);

				drawableContainerField.Value = field.Value;
				// update last used from DC field, so that scope is honored
				_editorConfiguration.UpdateLastFieldValue(drawableContainerField);
			}
		}
	}
}