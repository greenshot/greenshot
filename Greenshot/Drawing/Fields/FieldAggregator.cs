/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Greenshot.Configuration;
using Greenshot.IniFile;
using Greenshot.Plugin;
using Greenshot.Plugin.Drawing;
using GreenshotPlugin.Interfaces.Drawing;
using log4net;
using System.Collections.Generic;
using System.ComponentModel;

namespace Greenshot.Drawing.Fields
{
	/// <summary>
	/// Represents the current set of properties for the editor.
	/// When one of EditorProperties' properties is updated, the change will be promoted
	/// to all bound elements.
	///  * If an element is selected:
	///    This class represents the element's properties
	///  * I n>1 elements are selected:
	///    This class represents the properties of all elements.
	///    Properties that do not apply for ALL selected elements are null (or 0 respectively)
	///    If the property values of the selected elements differ, the value of the last bound element wins.
	/// </summary>
	public class FieldAggregator : AbstractFieldHolder
	{

		private IDrawableContainerList boundContainers;
		private bool internalUpdateRunning = false;

		enum Status { IDLE, BINDING, UPDATING };

		private static readonly ILog LOG = LogManager.GetLogger(typeof(FieldAggregator));
		private static EditorConfiguration editorConfiguration = IniConfig.GetIniSection<EditorConfiguration>();

		public FieldAggregator(ISurface parent)
		{
			foreach (FieldType fieldType in FieldType.Values)
			{
				Field field = new Field(fieldType, GetType());
				AddField(field);
			}
			boundContainers = new DrawableContainerList();
			boundContainers.Parent = parent;
		}

		public override void AddField(IField field)
		{
			base.AddField(field);
			field.PropertyChanged += OwnPropertyChanged;
		}

		public void BindElements(IDrawableContainerList dcs)
		{
			foreach (DrawableContainer dc in dcs)
			{
				BindElement(dc);
			}
		}

		public void BindElement(IDrawableContainer dc)
		{
			DrawableContainer container = dc as DrawableContainer;
			if (container != null && !boundContainers.Contains(container))
			{
				boundContainers.Add(container);
				container.ChildrenChanged += delegate {
					UpdateFromBoundElements();
				};
				UpdateFromBoundElements();
			}
		}

		public void BindAndUpdateElement(IDrawableContainer dc)
		{
			UpdateElement(dc);
			BindElement(dc);
		}

		public void UpdateElement(IDrawableContainer dc)
		{
			DrawableContainer container = dc as DrawableContainer;
			if (container == null)
			{
				return;
			}
			internalUpdateRunning = true;
			foreach (Field field in GetFields())
			{
				if (container.HasField(field.FieldType) && field.HasValue)
				{
					//if(LOG.IsDebugEnabled) LOG.Debug("   "+field+ ": "+field.Value);
					container.SetFieldValue(field.FieldType, field.Value);
				}
			}
			internalUpdateRunning = false;
		}

		public void UnbindElement(IDrawableContainer dc)
		{
			if (boundContainers.Contains(dc))
			{
				boundContainers.Remove(dc);
				UpdateFromBoundElements();
			}
		}

		public void Clear()
		{
			ClearFields();
			boundContainers.Clear();
			UpdateFromBoundElements();
		}

		/// <summary>
		/// sets all field values to null, however does not remove fields
		/// </summary>
		private void ClearFields()
		{
			internalUpdateRunning = true;
			foreach (Field field in GetFields())
			{
				field.Value = null;
			}
			internalUpdateRunning = false;
		}

		/// <summary>
		/// Updates this instance using the respective fields from the bound elements.
		/// Fields that do not apply to every bound element are set to null, or 0 respectively.
		/// All other fields will be set to the field value of the least bound element.
		/// </summary>
		private void UpdateFromBoundElements()
		{
			ClearFields();
			internalUpdateRunning = true;
			foreach (Field field in FindCommonFields())
			{
				SetFieldValue(field.FieldType, field.Value);
			}
			internalUpdateRunning = false;
		}

		private IList<IField> FindCommonFields()
		{
			IList<IField> returnFields = null;
			if (boundContainers.Count > 0)
			{
				// take all fields from the least selected container...
				DrawableContainer leastSelectedContainer = boundContainers[boundContainers.Count - 1] as DrawableContainer;
				if (leastSelectedContainer != null)
				{
					returnFields = leastSelectedContainer.GetFields();
					for (int i = 0; i < boundContainers.Count - 1; i++)
					{
						DrawableContainer dc = boundContainers[i] as DrawableContainer;
						if (dc != null)
						{
							IList<IField> fieldsToRemove = new List<IField>();
							foreach (IField field in returnFields)
							{
								// ... throw out those that do not apply to one of the other containers
								if (!dc.HasField(field.FieldType))
								{
									fieldsToRemove.Add(field);
								}
							}
							foreach (IField field in fieldsToRemove)
							{
								returnFields.Remove(field);
							}
						}
					}
				}
			}
			if (returnFields == null)
			{
				returnFields = new List<IField>();
			}
			return returnFields;
		}

		public void OwnPropertyChanged(object sender, PropertyChangedEventArgs ea)
		{
			IField field = (IField)sender;
			if (!internalUpdateRunning && field.Value != null)
			{
				foreach (DrawableContainer drawableContainer in boundContainers)
				{
					if (drawableContainer.HasField(field.FieldType))
					{
						IField drawableContainerField = drawableContainer.GetField(field.FieldType);
						// Notify before change, so we can e.g. invalidate the area
						drawableContainer.BeforeFieldChange(drawableContainerField, field.Value);

						drawableContainerField.Value = field.Value;
						// update last used from DC field, so that scope is honored
						editorConfiguration.UpdateLastFieldValue(drawableContainerField);
					}
				}
			}
		}

	}
}
