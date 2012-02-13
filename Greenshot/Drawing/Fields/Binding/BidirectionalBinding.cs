/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Reflection;

namespace Greenshot.Drawing.Fields.Binding {
	/// <summary>
	/// Bidirectional binding of properties of two INotifyPropertyChanged instances.
	/// This implementation synchronizes null values, too. If you do not want this
	/// behavior (e.g. when binding to a 
	/// </summary>
	public class BidirectionalBinding
	{
		private INotifyPropertyChanged object1;
		private INotifyPropertyChanged object2;
		private string property1;
		private string property2;
		private bool updatingObject1 = false;
		private bool updatingObject2 = false;
		private IBindingConverter converter;
		private IBindingValidator validator;
		
		/// <summary>
		/// Whether or not null values are passed on to the other object.
		/// </summary>
		protected bool AllowSynchronizeNull = true;
		
		/// <summary>
		/// Bind properties of two objects bidirectionally
		/// </summary>
		/// <param name="object1">Object containing 1st property to bind</param>
		/// <param name="property1">Property of 1st object to bind</param>
		/// <param name="object2">Object containing 2nd property to bind</param>
		/// <param name="property2">Property of 2nd object to bind</param>
		public BidirectionalBinding(INotifyPropertyChanged object1, string property1, INotifyPropertyChanged object2, string property2)
		{
			this.object1 = object1;
			this.object2 = object2;
			this.property1 = property1;
			this.property2 = property2;
			
			this.object1.PropertyChanged += new PropertyChangedEventHandler(Property1Changed);
			this.object2.PropertyChanged += new PropertyChangedEventHandler(Property2Changed);
		}
		
		/// <summary>
		/// Bind properties of two objects bidirectionally, converting the values using a converter
		/// </summary>
		/// <param name="object1">Object containing 1st property to bind</param>
		/// <param name="property1">Property of 1st object to bind</param>
		/// <param name="object2">Object containing 2nd property to bind</param>
		/// <param name="property2">Property of 2nd object to bind</param>
		/// <param name="converter">taking care of converting the synchronzied value to the correct target format and back</param>
		public BidirectionalBinding(INotifyPropertyChanged object1, string property1, INotifyPropertyChanged object2, string property2, IBindingConverter converter) : this(object1, property1, object2, property2)
		{
			this.converter = converter;
		}
		
		/// <summary>
		/// Bind properties of two objects bidirectionally, converting the values using a converter.
		/// Synchronization can be intercepted by adding a validator.
		/// </summary>
		/// <param name="object1">Object containing 1st property to bind</param>
		/// <param name="property1">Property of 1st object to bind</param>
		/// <param name="object2">Object containing 2nd property to bind</param>
		/// <param name="property2">Property of 2nd object to bind</param>
		/// <param name="validator">validator to intercept synchronisation if the value does not match certain criteria</param>
		public BidirectionalBinding(INotifyPropertyChanged object1, string property1, INotifyPropertyChanged object2, string property2, IBindingValidator validator) : this(object1, property1, object2, property2)
		{
			this.validator = validator;
		}
		
		/// <summary>
		/// Bind properties of two objects bidirectionally, converting the values using a converter.
		/// Synchronization can be intercepted by adding a validator.
		/// </summary>
		/// <param name="object1">Object containing 1st property to bind</param>
		/// <param name="property1">Property of 1st object to bind</param>
		/// <param name="object2">Object containing 2nd property to bind</param>
		/// <param name="property2">Property of 2nd object to bind</param>
		/// <param name="converter">taking care of converting the synchronzied value to the correct target format and back</param>
		/// <param name="validator">validator to intercept synchronisation if the value does not match certain criteria</param>
		public BidirectionalBinding(INotifyPropertyChanged object1, string property1, INotifyPropertyChanged object2, string property2, IBindingConverter converter, IBindingValidator validator) : this(object1, property1, object2, property2, converter)
		{
			this.validator = validator;
		}
		
		public void Property1Changed(object sender, PropertyChangedEventArgs e)
		{
			if(!updatingObject1 && e.PropertyName.Equals(property1))
			{
				updatingObject2 = true;
				synchronize(object1, property1, object2, property2);
				updatingObject2 = false;
			}
		}
		
		public void Property2Changed(object sender, PropertyChangedEventArgs e)
		{
			if(!updatingObject2 && e.PropertyName.Equals(property2))
			{
				updatingObject1 = true;
				synchronize(object2, property2, object1, property1);
				updatingObject1 = false;
			}
		}
		
		private void synchronize(INotifyPropertyChanged sourceObject, string sourceProperty, INotifyPropertyChanged targetObject, string targetProperty)
		{
			PropertyInfo targetPropertyInfo = resolvePropertyInfo(targetObject, targetProperty);
			PropertyInfo sourcePropertyInfo = resolvePropertyInfo(sourceObject, sourceProperty);
			
			if(sourcePropertyInfo != null && targetPropertyInfo != null && targetPropertyInfo.CanWrite)
			{
				object bValue = sourcePropertyInfo.GetValue(sourceObject, null);
				if(converter != null && bValue != null) bValue = converter.convert(bValue);
				try {
					if(validator == null || validator.validate(bValue)) {
						targetPropertyInfo.SetValue(targetObject, bValue, null);
					}
				} catch (Exception e) {
					throw new MemberAccessException("Could not set property '"+targetProperty+"' to '"+bValue+"' ["+((bValue!=null)?bValue.GetType().Name:"")+"] on "+targetObject+". Probably other type than expected, IBindingCoverter to the rescue.", e);
				}
				
			}
		}
		
		private PropertyInfo resolvePropertyInfo(object obj, string property) {
			PropertyInfo ret = null;
			string[] properties = property.Split(".".ToCharArray());
			for(int i=0; i<properties.Length; i++) {
				string prop = properties[i];
				ret = obj.GetType().GetProperty(prop);
				if(ret != null && ret.CanRead && i<prop.Length-1) {
					obj = ret.GetValue(obj, null);
				}
			}
			return ret;
		
		}
		
		public IBindingConverter Converter {
			get { return converter; }
			set { converter = value; }
		}
		
	}	
	
}
