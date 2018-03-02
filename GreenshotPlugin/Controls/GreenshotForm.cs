#region Greenshot GNU General Public License

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
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using GreenshotPlugin.Core;
using Dapplo.Ini;
using Dapplo.InterfaceImpl.Extensions;
using Dapplo.Log;
using Dapplo.Windows.Dpi.Forms;
using Greenshot.Gfx;

#endregion

namespace GreenshotPlugin.Controls
{
	/// <summary>
	///     This form is used for automatically binding the elements of the form to the language
	/// </summary>
	public class GreenshotForm : DpiAwareForm, IGreenshotLanguageBindable
	{
		private static readonly LogSource Log = new LogSource();
		protected static readonly ICoreConfiguration coreConfiguration;
		private static readonly IDictionary<Type, FieldInfo[]> ReflectionCache = new Dictionary<Type, FieldInfo[]>();
		private IDictionary<string, Control> _designTimeControls;
		private IDictionary<string, ToolStripItem> _designTimeToolStripItems;
		private bool _isDesignModeLanguageSet;
		private IComponentChangeService _componentChangeService;

		protected readonly BitmapScaleHandler<string> ScaleHandler;

		static GreenshotForm()
		{
			if (!IsInDesignMode)
			{
				coreConfiguration = IniConfig.Current.Get<ICoreConfiguration>();
			}
		}

		/// <summary>
		/// Default constructor, for default functionality like DPI awareness
		/// </summary>
		protected GreenshotForm()
		{
			// Add the Dapplo.Windows DPI change handler
			ScaleHandler = BitmapScaleHandler.WithComponentResourceManager(DpiHandler, GetType(), (bitmap, dpi) => bitmap.ScaleIconForDisplaying(dpi));
		}

		/// <summary>
		///     Used to check the designmode during a constructor
		/// </summary>
		/// <returns></returns>
		protected static bool IsInDesignMode => Application.ExecutablePath.IndexOf("devenv.exe", StringComparison.OrdinalIgnoreCase) > -1 ||
		                                        Application.ExecutablePath.IndexOf("sharpdevelop.exe", StringComparison.OrdinalIgnoreCase) > -1 ||
		                                        Application.ExecutablePath.IndexOf("wdexpress.exe", StringComparison.OrdinalIgnoreCase) > -1;

	    protected bool ManualLanguageApply { get; set; }

		protected bool ManualStoreFields { get; set; }

		/// <summary>
		///     When this is set, the form will be brought to the foreground as soon as it is shown.
		/// </summary>
		protected bool ToFront { get; set; }

		/// <summary>
		///     This override allows the control to register event handlers for IComponentChangeService events
		///     at the time the control is sited, which happens only in design mode.
		/// </summary>
		public override ISite Site
		{
			get { return base.Site; }
			set
			{
				// Clear any component change event handlers.
				ClearChangeNotifications();

				// Set the new Site value.
				base.Site = value;

				_componentChangeService = (IComponentChangeService) GetService(typeof(IComponentChangeService));

				// Register event handlers for component change events.
				RegisterChangeNotifications();
			}
		}

		[Category("Greenshot")]
		[DefaultValue(null)]
		[Description("Specifies key of the language file to use when displaying the text.")]
		public string LanguageKey { get; set; }

		/// <summary>
		///     Code to initialize the language etc during design time
		/// </summary>
		protected void InitializeForDesigner()
		{
			if (DesignMode)
			{
				_designTimeControls = new Dictionary<string, Control>();
				_designTimeToolStripItems = new Dictionary<string, ToolStripItem>();
				try
				{
					var typeResService = GetService(typeof(ITypeResolutionService)) as ITypeResolutionService;

					// Add a hard-path if you are using SharpDevelop
					// Language.AddLanguageFilePath(@"C:\Greenshot\Greenshot\Languages");

					// this "type"
					var currentAssembly = GetType().Assembly;
					if (typeResService != null)
					{
						var assemblyPath = typeResService.GetPathOfAssembly(currentAssembly.GetName());
						var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
						if (assemblyDirectory != null && !Language.AddLanguageFilePath(Path.Combine(assemblyDirectory, @"..\..\Greenshot\Languages\")))
						{
							Language.AddLanguageFilePath(Path.Combine(assemblyDirectory, @"..\..\..\Greenshot\Languages\"));
						}
						if (assemblyDirectory != null && !Language.AddLanguageFilePath(Path.Combine(assemblyDirectory, @"..\..\Languages\")))
						{
							Language.AddLanguageFilePath(Path.Combine(assemblyDirectory, @"..\..\..\Languages\"));
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}
			}
		}

		/// <summary>
		///     This override is only for the design-time of the form
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (DesignMode)
			{
				if (!_isDesignModeLanguageSet)
				{
					_isDesignModeLanguageSet = true;
					try
					{
						ApplyLanguage();
					}
					catch (Exception)
					{
						// ignored
					}
				}
			}
			base.OnPaint(e);
		}

		protected override void OnLoad(EventArgs e)
		{
			// Every GreenshotForm should have it's default icon
			// And it might not ne needed for a Tool Window, but still for the task manager / switcher it's important
			Icon = GreenshotResources.GetGreenshotIcon();
			if (!DesignMode)
			{
				if (!ManualLanguageApply)
				{
					ApplyLanguage();
				}
				FillFields();
				base.OnLoad(e);
			}
			else
			{
				Log.Info().WriteLine("OnLoad called from designer.");
				InitializeForDesigner();
				base.OnLoad(e);
				ApplyLanguage();
			}
		}

		/// <summary>
		///     Make sure the form is visible, if this is wanted
		/// </summary>
		/// <param name="e">EventArgs</param>
		protected override async void OnShown(EventArgs e)
		{
			base.OnShown(e);
			if (ToFront)
			{
				await InteropWindowFactory.CreateFor(Handle).ToForegroundAsync();
			}
		}

		/// <summary>
		///     check if the form was closed with an OK, if so store the values in the GreenshotControls
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosed(EventArgs e)
		{
			if (!DesignMode && !ManualStoreFields)
			{
				if (DialogResult == DialogResult.OK)
				{
					Log.Info().WriteLine("Form was closed with OK: storing field values.");
					StoreFields();
				}
			}
			base.OnClosed(e);
		}

		private void ClearChangeNotifications()
		{
			// The m_changeService value is null when not in design mode, 
			// as the IComponentChangeService is only available at design time.	
			_componentChangeService = (IComponentChangeService) GetService(typeof(IComponentChangeService));

			// Clear our the component change events to prepare for re-siting.				
		    if (_componentChangeService == null)
		    {
		        return;
		    }

		    _componentChangeService.ComponentChanged -= OnComponentChanged;
		    _componentChangeService.ComponentAdded -= OnComponentAdded;
		}

		private void RegisterChangeNotifications()
		{
			// Register the event handlers for the IComponentChangeService events
		    if (_componentChangeService == null)
		    {
		        return;
		    }

		    _componentChangeService.ComponentChanged += OnComponentChanged;
		    _componentChangeService.ComponentAdded += OnComponentAdded;
		}

		/// <summary>
		///     This method handles the OnComponentChanged event to display a notification.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="ce"></param>
		private void OnComponentChanged(object sender, ComponentChangedEventArgs ce)
		{
		    if (((IComponent) ce.Component)?.Site == null || ce.Member == null)
		    {
		        return;
		    }

		    if (!"LanguageKey".Equals(ce.Member.Name))
		    {
		        return;
		    }

		    switch (ce.Component)
		    {
		        case Control control:
		            Log.Info().WriteLine("Changing LanguageKey for {0} to {1}", control.Name, ce.NewValue);
		            ApplyLanguage(control, (string) ce.NewValue);
		            break;
		        case ToolStripItem item:
		            Log.Info().WriteLine("Changing LanguageKey for {0} to {1}", item.Name, ce.NewValue);
		            ApplyLanguage(item, (string) ce.NewValue);
		            break;
		        default:
		            Log.Info().WriteLine("Not possible to changing LanguageKey for {0} to {1}", ce.Component.GetType(), ce.NewValue);
		            break;
		    }
		}

		private void OnComponentAdded(object sender, ComponentEventArgs ce)
		{
		    if (ce.Component?.Site == null)
		    {
		        return;
		    }

		    switch (ce.Component)
		    {
		        case Control control:
		            if (!_designTimeControls.ContainsKey(control.Name))
		            {
		                _designTimeControls.Add(control.Name, control);
		            }
		            else
		            {
		                _designTimeControls[control.Name] = control;
		            }

		            break;
		        case ToolStripItem stripItem:
		            var item = stripItem;
		            if (!_designTimeControls.ContainsKey(item.Name))
		            {
		                _designTimeToolStripItems.Add(item.Name, item);
		            }
		            else
		            {
		                _designTimeToolStripItems[item.Name] = item;
		            }

		            break;
		    }
		}

        /// <summary>
        /// 	    Clean up any resources being used.
        /// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ClearChangeNotifications();
			}
			ScaleHandler.Dispose();
			base.Dispose(disposing);
		}

		protected void ApplyLanguage(ToolStripItem applyTo, string languageKey)
		{
			string langString;
			if (!string.IsNullOrEmpty(languageKey))
			{
				if (!Language.TryGetString(languageKey, out langString))
				{
					Log.Warn().WriteLine("Unknown language key '{0}' configured for control '{1}', this might be okay.", languageKey, applyTo.Name);
					return;
				}
				applyTo.Text = langString;
			}
			else
			{
				// Fallback to control name!
				if (Language.TryGetString(applyTo.Name, out langString))
				{
					applyTo.Text = langString;
					return;
				}
				if (!DesignMode)
				{
					Log.Debug().WriteLine("Greenshot control without language key: {0}", applyTo.Name);
				}
			}
		}

		protected void ApplyLanguage(ToolStripItem applyTo)
		{
		    if (applyTo is IGreenshotLanguageBindable languageBindable)
			{
				ApplyLanguage(applyTo, languageBindable.LanguageKey);
			}
		}

		protected void ApplyLanguage(Control applyTo)
		{
		    if (!(applyTo is IGreenshotLanguageBindable languageBindable))
			{
				// check if it's a menu!
			    if (!(applyTo is ToolStrip toolStrip))
				{
					return;
				}
				foreach (ToolStripItem item in toolStrip.Items)
				{
					ApplyLanguage(item);
				}
				return;
			}

			// Apply language text to the control
			ApplyLanguage(applyTo, languageBindable.LanguageKey);

			// Repopulate the combox boxes
		    if (!(applyTo is IGreenshotConfigBindable configBindable) || !(applyTo is GreenshotComboBox comboxBox))
		    {
		        return;
		    }

		    if (string.IsNullOrEmpty(configBindable.SectionName) || string.IsNullOrEmpty(configBindable.PropertyName))
		    {
		        return;
		    }

		    var section = IniConfig.Current[configBindable.SectionName];
		    if (section == null)
		    {
		        return;
		    }

		    // Only update the language, so get the actual value and than repopulate
		    var currentValue = comboxBox.GetSelectedEnum();
		    comboxBox.Populate(section[configBindable.PropertyName].ValueType);
		    comboxBox.SetValue(currentValue);
		}

		/// <summary>
		///     Helper method to cache the fieldinfo values, so we don't need to reflect all the time!
		/// </summary>
		/// <param name="typeToGetFieldsFor"></param>
		/// <returns></returns>
		private static FieldInfo[] GetCachedFields(Type typeToGetFieldsFor)
		{
		    if (!ReflectionCache.TryGetValue(typeToGetFieldsFor, out var fields))
			{
				fields = typeToGetFieldsFor.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				ReflectionCache.Add(typeToGetFieldsFor, fields);
			}
			return fields;
		}

		/// <summary>
		///     Apply all the language settings to the "Greenshot" Controls on this form
		/// </summary>
		protected void ApplyLanguage()
		{
			SuspendLayout();
			try
			{
				// Set title of the form
			    if (!string.IsNullOrEmpty(LanguageKey) && Language.TryGetString(LanguageKey, out var langString))
				{
					Text = langString;
				}

				// Reset the text values for all GreenshotControls
				foreach (var field in GetCachedFields(GetType()))
				{
					var controlObject = field.GetValue(this);
					if (controlObject == null)
					{
						Log.Debug().WriteLine("No value: {0}", field.Name);
						continue;
					}

				    if (!(controlObject is Control applyToControl))
					{
					    if (!(controlObject is ToolStripItem applyToItem))
						{
							Log.Debug().WriteLine("No Control or ToolStripItem: {0}", field.Name);
							continue;
						}
						ApplyLanguage(applyToItem);
					}
					else
					{
						ApplyLanguage(applyToControl);
					}
				}

			    if (!DesignMode)
			    {
			        return;
			    }

			    foreach (var designControl in _designTimeControls.Values)
			    {
			        ApplyLanguage(designControl);
			    }
			    foreach (var designToolStripItem in _designTimeToolStripItems.Values)
			    {
			        ApplyLanguage(designToolStripItem);
			    }
			}
			finally
			{
				ResumeLayout();
			}
		}

		/// <summary>
		///     Apply the language text to supplied control
		/// </summary>
		protected void ApplyLanguage(Control applyTo, string languageKey)
		{
			string langString;
			if (!string.IsNullOrEmpty(languageKey))
			{
				if (!Language.TryGetString(languageKey, out langString))
				{
					Log.Warn().WriteLine("Wrong language key '{0}' configured for control '{1}'", languageKey, applyTo.Name);
					return;
				}
				applyTo.Text = langString;
			}
			else
			{
				// Fallback to control name!
				if (Language.TryGetString(applyTo.Name, out langString))
				{
					applyTo.Text = langString;
					return;
				}
				if (!DesignMode)
				{
					Log.Debug().WriteLine("Greenshot control without language key: {0}", applyTo.Name);
				}
			}
		}

		/// <summary>
		///     Fill all GreenshotControls with the values from the configuration
		/// </summary>
		protected void FillFields()
		{
			foreach (var field in GetCachedFields(GetType()))
			{
				var controlObject = field.GetValue(this);
				var configBindable = controlObject as IGreenshotConfigBindable;
			    if (string.IsNullOrEmpty(configBindable?.SectionName) || string.IsNullOrEmpty(configBindable.PropertyName))
			    {
			        continue;
			    }

			    var section = IniConfig.Current[configBindable.SectionName];
			    if (section == null)
			    {
			        continue;
			    }

			    if (!section.TryGetIniValue(configBindable.PropertyName, out var iniValue))
			    {
			        Log.Debug().WriteLine("Wrong property '{0}' configured for field '{1}'", configBindable.PropertyName, field.Name);
			        continue;
			    }

			    var isWriteProtected = (section as IWriteProtectProperties)?.IsWriteProtected(iniValue.PropertyName) ?? false;

                switch (controlObject)
			    {
			        case CheckBox checkBox:
			            checkBox.Checked = (bool) iniValue.Value;
			            checkBox.Enabled = !isWriteProtected;
			            continue;
			        case RadioButton radíoButton:
			            radíoButton.Checked = (bool) iniValue.Value;
			            radíoButton.Enabled = !isWriteProtected;
			            continue;
			        case TextBox textBox:
			            if (controlObject is HotkeyControl hotkeyControl)
			            {
			                var hotkeyValue = (string) iniValue.Value;
			                if (!string.IsNullOrEmpty(hotkeyValue))
			                {
			                    hotkeyControl.SetHotkey(hotkeyValue);
			                    hotkeyControl.Enabled = !isWriteProtected;
			                }
			                continue;
			            }
			            textBox.Text = iniValue.ToString();
			            textBox.Enabled = !isWriteProtected;
			            continue;
			        case GreenshotComboBox comboxBox:
			            comboxBox.Populate(iniValue.ValueType);
			            comboxBox.SetValue((Enum) iniValue.Value);
			            comboxBox.Enabled = !isWriteProtected;
			            break;
			    }
			}
			OnFieldsFilled();
		}

		protected virtual void OnFieldsFilled()
		{
		}

		/// <summary>
		///     Store all GreenshotControl values to the configuration
		/// </summary>
		protected void StoreFields()
		{
			foreach (var field in GetCachedFields(GetType()))
			{
				var controlObject = field.GetValue(this);
				var configBindable = controlObject as IGreenshotConfigBindable;

			    if (string.IsNullOrEmpty(configBindable?.SectionName) || string.IsNullOrEmpty(configBindable.PropertyName))
			    {
			        continue;
			    }

			    var section = IniConfig.Current[configBindable.SectionName];
			    if (section == null)
			    {
			        continue;
			    }

			    if (!section.TryGetIniValue(configBindable.PropertyName, out var iniValue))
			    {
			        continue;
			    }

			    switch (controlObject)
			    {
			        case CheckBox checkBox:
			            iniValue.Value = checkBox.Checked;
			            continue;
			        case RadioButton radioButton:
			            iniValue.Value = radioButton.Checked;
			            continue;
			        case TextBox textBox:
			            if (controlObject is HotkeyControl hotkeyControl)
			            {
			                iniValue.Value = hotkeyControl.ToString();
			                continue;
			            }

                        // TODO: Does this make sense?
			            iniValue.Value = textBox.Text;
			            if (iniValue.HasValue)
			            {
			                iniValue.ResetToDefault();
                        }
			            continue;
			        case GreenshotComboBox comboxBox:
			            iniValue.Value = comboxBox.GetSelectedEnum();
			            break;
			    }
			}
		}
	}
}