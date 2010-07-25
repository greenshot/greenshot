/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
using System;
using System.Drawing;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using Greenshot.Configuration;
using Greenshot.Drawing.Filters;
using Greenshot.Helpers;

namespace Greenshot.Drawing.Fields {
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
	public class FieldAggregator : AbstractFieldHolder {
		
		private List<DrawableContainer> boundContainers;
		private bool internalUpdateRunning = false;
		
		enum Status {IDLE, BINDING, UPDATING};
		
		private Status status;
		
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(FieldAggregator));
		
		public FieldAggregator() {
			List<Field> fields = FieldFactory.GetDefaultFields();
			foreach(Field field in fields) {
				AddField(field);
			}
			boundContainers = new List<DrawableContainer>();
		}
		
		
		public override void AddField(IField field) {
			base.AddField(field);
			field.PropertyChanged += new PropertyChangedEventHandler(OwnPropertyChanged);
		}
		
		public void BindElements(DrawableContainerList dcs) {
			foreach(DrawableContainer dc in dcs) {
				BindElement(dc);
			}
		}
		public void BindElement(DrawableContainer dc) {
			status = Status.BINDING;
			if(LOG.IsDebugEnabled) LOG.Debug("Binding element of type "+dc.GetType());
			if(!boundContainers.Contains(dc)) {
				boundContainers.Add(dc);
				status = Status.IDLE;
				dc.ChildrenChanged += delegate { UpdateFromBoundElements(); };
				UpdateFromBoundElements();
			}
		}
		
		public void BindAndUpdateElement(DrawableContainer dc) {
			UpdateElement(dc);
			BindElement(dc);
		}
		
		public void UpdateElement(DrawableContainer dc) {
			if(LOG.IsDebugEnabled) LOG.Debug("Updating element of type "+dc.GetType());
			internalUpdateRunning = true;
			foreach(Field field in GetFields()) {
				if(dc.HasField(field.FieldType) && field.HasValue) {
					//if(LOG.IsDebugEnabled) LOG.Debug("   "+field+ ": "+field.Value);
					dc.SetFieldValue(field.FieldType, field.Value);
				}
			}
			internalUpdateRunning = false;
		}
				
		public void UnbindElement(DrawableContainer dc) {
			if(boundContainers.Contains(dc)) {
				if(LOG.IsDebugEnabled) LOG.Debug("Unbinding element of type "+dc.GetType());
				boundContainers.Remove(dc);
				UpdateFromBoundElements();
			}
		}
		
		public void Clear() {
			ClearFields();
			boundContainers.Clear();			
			UpdateFromBoundElements();
		}
		
		/// <summary>
		/// sets all field values to null, however does not remove fields
		/// </summary>
		private void ClearFields() {
			internalUpdateRunning = true;
			//if(LOG.IsDebugEnabled) LOG.Debug("Clearing fields internally");
			foreach(Field field in GetFields()) {
				field.Value = null;
				//if(LOG.IsDebugEnabled) LOG.Debug("   "+field.GetType()+ ": "+field.Value);
			}
			internalUpdateRunning = false;
		}
		
		/// <summary>
		/// Updates this instance using the respective fields from the bound elements.
		/// Fields that do not apply to every bound element are set to null, or 0 respectively.
		/// All other fields will be set to the field value of the least bound element.
		/// </summary>
		private void UpdateFromBoundElements() {
			if(LOG.IsDebugEnabled) LOG.Debug("Updating from bound elements, status = "+status);			
			status = Status.UPDATING;
			ClearFields();
			internalUpdateRunning = true;
			foreach(Field f in FindCommonFields()) {
				SetFieldValue(f.FieldType,f.Value);
				//if(LOG.IsDebugEnabled) LOG.Debug("Updating own field: "+f.FieldType+": "+GetField(f.FieldType).Value);
			}
			internalUpdateRunning = false;
			status = Status.IDLE;
		}
		
		private List<IField> FindCommonFields() {
			List<IField> ret = null;
			if(boundContainers.Count > 0) {
				// take all fields from the least selected container...
				ret = boundContainers[boundContainers.Count-1].GetFields();
				for(int i=0;i<boundContainers.Count-1; i++) {
					DrawableContainer dc = boundContainers[i];
					List<IField> fieldsToRemove = new List<IField>();
					foreach(IField f in ret) {
						// ... throw out those that do not apply to one of the other containers
						if(!dc.HasField(f.FieldType)) fieldsToRemove.Add(f);
					}
					foreach(IField f in fieldsToRemove) {
						ret.Remove(f);
					}
				}
			}
			if(ret == null) ret = new List<IField>();
			return ret;
		}
		
		public void OwnPropertyChanged(object sender, PropertyChangedEventArgs ea) {
			Field f = (Field) sender;
			if(!internalUpdateRunning && f.Value!=null) {
				
				foreach(DrawableContainer dc in boundContainers) {
					if(f.Scope == null || dc.GetType().FullName.Equals(f.Scope)) {
						if(LOG.IsDebugEnabled) LOG.Debug("Updating field: "+f.FieldType+": "+f.Value);
						if(dc.HasField(f.FieldType)) {
							IField dcf = dc.GetField(f.FieldType);
							dcf.Value = f.Value;
							// update last used from DC field, so that scope is honored
							AppConfig.GetInstance().UpdateLastUsedFieldValue(dcf);
						}
						
					}
				}
			}
		}

	}	
}
