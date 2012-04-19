using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;

namespace GreenshotPlugin.Controls {
	public abstract class GreenshotForm : Form, IGreenshotLanguageBindable {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(GreenshotForm));
		private IComponentChangeService m_changeService;
		private bool isLanguageSet = false;
		private IDictionary<string, Control> designTimeControls;
		private IDictionary<string, ToolStripItem> designTimeToolStripItems;
		[Category("Greenshot"), DefaultValue(null), Description("Specifies key of the language file to use when displaying the text.")]
		public string LanguageKey {
			get;
			set;
		}

		/// <summary>
		/// Code to initialize the language etc during design time
		/// </summary>
		protected void InitializeForDesigner() {
			if (this.DesignMode) {
				designTimeControls = new Dictionary<string, Control>();
				designTimeToolStripItems = new Dictionary<string, ToolStripItem>();
				try {
					ITypeResolutionService typeResService = GetService(typeof(ITypeResolutionService)) as ITypeResolutionService;
					Assembly currentAssembly = this.GetType().Assembly;
					string assemblyPath = typeResService.GetPathOfAssembly(currentAssembly.GetName());
					if (!Language.AddLanguageFilePath(Path.Combine(Path.GetDirectoryName(assemblyPath), @"..\..\Greenshot\Languages\"))) {
						Language.AddLanguageFilePath(Path.Combine(Path.GetDirectoryName(assemblyPath), @"..\..\..\Greenshot\Languages\"));
					}
					if (!Language.AddLanguageFilePath(Path.Combine(Path.GetDirectoryName(assemblyPath), @"..\..\Languages\"))) {
						Language.AddLanguageFilePath(Path.Combine(Path.GetDirectoryName(assemblyPath), @"..\..\..\Languages\"));
					}
				} catch (Exception ex) {
					MessageBox.Show(ex.ToString());
				}
			}
		}

		/// <summary>
		/// This override is only for the design-time of the form
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e) {
			if (this.DesignMode) {
				if (!isLanguageSet) {
					isLanguageSet = true;
					try {
						ApplyLanguage();
					} catch (Exception ex) {
						MessageBox.Show(ex.ToString());
					}
				}
			}
			base.OnPaint(e);
		}

		protected override void OnLoad(EventArgs e) {
			if (!this.DesignMode) {
				ApplyLanguage();
				FillFields();
				base.OnLoad(e);
			} else {
				LOG.Info("OnLoad called from designer.");
				InitializeForDesigner();
				base.OnLoad(e);
				ApplyLanguage();
			}
		}

		/// <summary>
		/// check if the form was closed with an OK, if so store the values in the GreenshotControls
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosed(EventArgs e) {
			if (!this.DesignMode) {
				if (DialogResult == DialogResult.OK) {
					LOG.Info("Form was closed with OK: storing field values.");
					StoreFields();
				}
			}
			base.OnClosed(e);
		}

		/// <summary>
		/// This override allows the control to register event handlers for IComponentChangeService events
		/// at the time the control is sited, which happens only in design mode.
		/// </summary>
		public override ISite Site {
			get {
				return base.Site;
			}
			set {
				// Clear any component change event handlers.
				ClearChangeNotifications();

				// Set the new Site value.
				base.Site = value;

				m_changeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));

				// Register event handlers for component change events.
				RegisterChangeNotifications();
			}
		}

		private void ClearChangeNotifications() {
			// The m_changeService value is null when not in design mode, 
			// as the IComponentChangeService is only available at design time.	
			m_changeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));

			// Clear our the component change events to prepare for re-siting.				
			if (m_changeService != null) {
				m_changeService.ComponentChanged -= new ComponentChangedEventHandler(OnComponentChanged);
				m_changeService.ComponentAdded -= new ComponentEventHandler(OnComponentAdded);
			}
		}

		private void RegisterChangeNotifications() {
			// Register the event handlers for the IComponentChangeService events
			if (m_changeService != null) {
				m_changeService.ComponentChanged += new ComponentChangedEventHandler(OnComponentChanged);
				m_changeService.ComponentAdded += new ComponentEventHandler(OnComponentAdded);
			}
		}

		/// <summary>
		/// This method handles the OnComponentChanged event to display a notification.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="ce"></param>
		private void OnComponentChanged(object sender, ComponentChangedEventArgs ce) {
			if (ce.Component != null && ((IComponent)ce.Component).Site != null && ce.Member != null) {
				if ("LanguageKey".Equals(ce.Member.Name)) {
					Control control = ce.Component as Control;
					if (control != null) {
						LOG.InfoFormat("Changing LanguageKey for {0} to {1}", control.Name, ce.NewValue);
						ApplyLanguage(control, (string)ce.NewValue);
					} else {
						ToolStripItem item = ce.Component as ToolStripItem;
						if (item != null) {
							LOG.InfoFormat("Changing LanguageKey for {0} to {1}", item.Name, ce.NewValue);
							ApplyLanguage(item, (string)ce.NewValue);
						} else {
							LOG.InfoFormat("Not possible to changing LanguageKey for {0} to {1}", ce.Component.GetType(), ce.NewValue);
						}
					}
				}
			}
		}

		private void OnComponentAdded(object sender, ComponentEventArgs ce) {
			if (ce.Component != null && ((IComponent)ce.Component).Site != null) {
				Control control = ce.Component as Control;
				if (control != null) {
					if (!designTimeControls.ContainsKey(control.Name)) {
						designTimeControls.Add(control.Name, control);
					} else {
						designTimeControls[control.Name] = control;
					}
				} else if (ce.Component is ToolStripItem) {
					ToolStripItem item = ce.Component as ToolStripItem;
					if (!designTimeControls.ContainsKey(item.Name)) {
						designTimeToolStripItems.Add(item.Name, item);
					} else {
						designTimeToolStripItems[item.Name] = item;
					}
				}
			}
		}

		// Clean up any resources being used.
		protected override void Dispose(bool disposing) {
			if (disposing) {
				ClearChangeNotifications();
			}
			base.Dispose(disposing);
		}

		protected void ApplyLanguage(ToolStripItem applyTo, string languageKey) {
			if (!string.IsNullOrEmpty(languageKey)) {
				if (!Language.hasKey(languageKey)) {
					LOG.WarnFormat("Wrong language key '{0}' configured for control '{1}'", languageKey, applyTo.Name);
					if (DesignMode) {
						MessageBox.Show(string.Format("Wrong language key '{0}' configured for control '{1}'", languageKey, applyTo.Name));
					}
					return;
				}
				applyTo.Text = Language.GetString(languageKey);
			} else {
				// Fallback to control name!
				if (Language.hasKey(applyTo.Name)) {
					applyTo.Text = Language.GetString(applyTo.Name);
					return;
				}
				if (this.DesignMode) {
					MessageBox.Show(string.Format("Greenshot control without language key: {0}", applyTo.Name));
				} else {
					LOG.DebugFormat("Greenshot control without language key: {0}", applyTo.Name);
				}
			}
		}

		protected void ApplyLanguage(ToolStripItem applyTo) {
			IGreenshotLanguageBindable languageBindable = applyTo as IGreenshotLanguageBindable;
			if (languageBindable != null) {
				ApplyLanguage(applyTo, languageBindable.LanguageKey);
			}
		}

		protected void ApplyLanguage(Control applyTo) {
			IGreenshotLanguageBindable languageBindable = applyTo as IGreenshotLanguageBindable;
			if (languageBindable == null) {
				// check if it's a menu!
				if (applyTo is ToolStrip) {
					ToolStrip toolStrip = applyTo as ToolStrip;
					foreach (ToolStripItem item in toolStrip.Items) {
						ApplyLanguage(item);
					}
				}
				return;
			}

			string languageKey = languageBindable.LanguageKey;
			// Apply language text to the control
			ApplyLanguage(applyTo, languageKey);
			// Repopulate the combox boxes
			if (typeof(IGreenshotConfigBindable).IsAssignableFrom(applyTo.GetType())) {
				if (typeof(GreenshotComboBox).IsAssignableFrom(applyTo.GetType())) {
					IGreenshotConfigBindable configBindable = applyTo as IGreenshotConfigBindable;
					if (!string.IsNullOrEmpty(configBindable.SectionName) && !string.IsNullOrEmpty(configBindable.PropertyName)) {
						IniSection section = IniConfig.GetIniSection(configBindable.SectionName);
						if (section != null) {
							GreenshotComboBox comboxBox = applyTo as GreenshotComboBox;
							// Only update the language, so get the actual value and than repopulate
							Enum currentValue = (Enum)comboxBox.GetSelectedEnum();
							comboxBox.Populate(section.Values[configBindable.PropertyName].ValueType);
							comboxBox.SetValue(currentValue);
						}
					}
				}
			}
		}
		/// <summary>
		/// Apply all the language settings to the "Greenshot" Controls on this form
		/// </summary>
		protected void ApplyLanguage() {
			// Set title of the form
			if (!string.IsNullOrEmpty(LanguageKey)) {
				this.Text = Language.GetString(LanguageKey);
			}
			// Reset the text values for all GreenshotControls
			foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (!field.FieldType.IsSubclassOf(typeof(Control))) {
					LOG.DebugFormat("No control: {0}", field.Name);
					continue;
				}
				Object controlObject = field.GetValue(this);
				if (controlObject == null) {
					LOG.DebugFormat("No value: {0}", field.Name);
					continue;
				}
				Control applyTo = controlObject as Control;
				if (applyTo == null) {
					// not a control
					LOG.DebugFormat("No control: {0}", field.Name);
					continue;
				}
				ApplyLanguage(applyTo);
			}

			if (DesignMode) {
				foreach (Control designControl in designTimeControls.Values) {
					ApplyLanguage(designControl);
				}
				foreach (ToolStripItem designToolStripItem in designTimeToolStripItems.Values) {
					ApplyLanguage(designToolStripItem);
				}
			}
		}

		/// <summary>
		/// Apply the language text to supplied control
		/// </summary>
		protected void ApplyLanguage(Control applyTo, string languageKey) {
			if (!string.IsNullOrEmpty(languageKey)) {
				if (!Language.hasKey(languageKey)) {
					LOG.WarnFormat("Wrong language key '{0}' configured for control '{1}'", languageKey, applyTo.Name);
					MessageBox.Show(string.Format("Wrong language key '{0}' configured for control '{1}'", languageKey, applyTo.Name));
					return;
				}
				applyTo.Text = Language.GetString(languageKey);
			} else {
				// Fallback to control name!
				if (Language.hasKey(applyTo.Name)) {
					applyTo.Text = Language.GetString(applyTo.Name);
					return;
				}
				if (this.DesignMode) {
					MessageBox.Show(string.Format("Greenshot control without language key: {0}", applyTo.Name));
				} else {
					LOG.DebugFormat("Greenshot control without language key: {0}", applyTo.Name);
				}
			}
		}

		/// <summary>
		/// Fill all GreenshotControls with the values from the configuration
		/// </summary>
		protected void FillFields() {
			foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (!field.FieldType.IsSubclassOf(typeof(Control))) {
					continue;
				}
				Object controlObject = field.GetValue(this);
				if (typeof(IGreenshotConfigBindable).IsAssignableFrom(field.FieldType)) {
					IGreenshotConfigBindable configBindable = controlObject as IGreenshotConfigBindable;
					if (!string.IsNullOrEmpty(configBindable.SectionName) && !string.IsNullOrEmpty(configBindable.PropertyName)) {
						IniSection section = IniConfig.GetIniSection(configBindable.SectionName);
						if (section != null) {
							if (!section.Values.ContainsKey(configBindable.PropertyName)) {
								LOG.WarnFormat("Wrong property '{0}' configured for field '{1}'",configBindable.PropertyName,field.Name);
								continue;
							}
							if (typeof(CheckBox).IsAssignableFrom(field.FieldType)) {
								CheckBox checkBox = controlObject as CheckBox;
								checkBox.Checked = (bool)section.Values[configBindable.PropertyName].Value;
							} else if (typeof(HotkeyControl).IsAssignableFrom(field.FieldType)) {
								HotkeyControl hotkeyControl = controlObject as HotkeyControl;
								string hotkeyValue = (string)section.Values[configBindable.PropertyName].Value;
								if (!string.IsNullOrEmpty(hotkeyValue)) {
									hotkeyControl.SetHotkey(hotkeyValue);
								}
							} else if (typeof(TextBox).IsAssignableFrom(field.FieldType)) {
								TextBox textBox = controlObject as TextBox;
								textBox.Text = (string)section.Values[configBindable.PropertyName].Value;
							} else if (typeof(GreenshotComboBox).IsAssignableFrom(field.FieldType)) {
								GreenshotComboBox comboxBox = controlObject as GreenshotComboBox;
								comboxBox.Populate(section.Values[configBindable.PropertyName].ValueType);
								comboxBox.SetValue((Enum)section.Values[configBindable.PropertyName].Value);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Store all GreenshotControl values to the configuration
		/// </summary>
		protected void StoreFields() {
			bool iniDirty = false;
			foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (!field.FieldType.IsSubclassOf(typeof(Control))) {
					continue;
				}
				if (!typeof(IGreenshotConfigBindable).IsAssignableFrom(field.FieldType)) {
					continue;
				}
				Object controlObject = field.GetValue(this);
				IGreenshotConfigBindable configBindable = controlObject as IGreenshotConfigBindable;

				if (!string.IsNullOrEmpty(configBindable.SectionName) && !string.IsNullOrEmpty(configBindable.PropertyName)) {
					IniSection section = IniConfig.GetIniSection(configBindable.SectionName);
					if (section != null) {
						if (typeof(CheckBox).IsAssignableFrom(field.FieldType)) {
							CheckBox checkBox = controlObject as CheckBox;
							section.Values[configBindable.PropertyName].Value = checkBox.Checked;
							iniDirty = true;
						} else if (typeof(HotkeyControl).IsAssignableFrom(field.FieldType)) {
							HotkeyControl hotkeyControl = controlObject as HotkeyControl;
							section.Values[configBindable.PropertyName].Value = hotkeyControl.ToString();
							iniDirty = true;
						} else if (typeof(TextBox).IsAssignableFrom(field.FieldType)) {
							TextBox textBox = controlObject as TextBox;
							section.Values[configBindable.PropertyName].Value = textBox.Text;
							iniDirty = true;
						} else if (typeof(GreenshotComboBox).IsAssignableFrom(field.FieldType)) {
							GreenshotComboBox comboxBox = controlObject as GreenshotComboBox;
							section.Values[configBindable.PropertyName].Value = comboxBox.GetSelectedEnum();
						}
					}
				}
			}
			if (iniDirty) {
				IniConfig.Save();
			}
		}
	}
}
