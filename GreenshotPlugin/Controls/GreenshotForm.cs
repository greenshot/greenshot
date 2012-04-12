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
		protected ILanguage language;
		private IComponentChangeService m_changeService;

		[Category("Greenshot"), DefaultValue(null), Description("Specifies key of the language file to use when displaying the text.")]
		public string LanguageKey {
			get;
			set;
		}

		protected abstract string LanguagePattern { get; }

		protected void InitializeForDesigner() {
			if (language == null && this.DesignMode) {
				try {
					if (!IniConfig.IsInited) {
						IniConfig.Init("greenshot", "greenshot");
					}

					ITypeResolutionService typeResService = GetService(typeof(ITypeResolutionService)) as ITypeResolutionService;
					Assembly currentAssembly = this.GetType().Assembly;
					string assemblyPath = typeResService.GetPathOfAssembly(currentAssembly.GetName());
					string designTimeLanguagePath = Path.Combine(Path.GetDirectoryName(assemblyPath), "../../Languages/");
					language = new LanguageContainer(LanguagePattern, designTimeLanguagePath);
				} catch (Exception ex) {
					MessageBox.Show(ex.ToString());
				}
			}
		}

		protected override void OnLoad(EventArgs e) {
			if (!this.DesignMode) {
				ApplyLanguage();
				FillFields();
			} else {
				InitializeForDesigner();
			}
			base.OnLoad(e);
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

		// This override allows the control to register event handlers for IComponentChangeService events
		// at the time the control is sited, which happens only in design mode.
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
			}
		}

		private void RegisterChangeNotifications() {
			// Register the event handlers for the IComponentChangeService events
			if (m_changeService != null) {
				m_changeService.ComponentChanged += new ComponentChangedEventHandler(OnComponentChanged);
			}
		}

		/* This method handles the OnComponentChanged event to display a notification. */
		private void OnComponentChanged(object sender, ComponentChangedEventArgs ce) {
			if (ce.Component != null && ((IComponent)ce.Component).Site != null && ce.Member != null) {
				//OnUserChange("The " + ce.Member.Name + " member of the " + ((IComponent)ce.Component).Site.Name + " component has been changed.");
				if ("LanguageKey".Equals(ce.Member.Name)) {
					ApplyLanguage(ce.Component as Control, (string)ce.NewValue);
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

		/// <summary>
		/// Apply all the language settings to the "Greenshot" Controls on this form
		/// </summary>
		protected void ApplyLanguage() {
			if (language == null) {
				MessageBox.Show("Language not set!! Please use 'language = Language.GetInstance()' in your form constructor!");
				return;
			}
			// Set title of the form
			if (!string.IsNullOrEmpty(LanguageKey)) {
				this.Text = language.GetString(LanguageKey);
			}
			// Reset the text values for all GreenshotControls
			foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (!field.FieldType.IsSubclassOf(typeof(Control))) {
					continue;
				}
				Object controlObject = field.GetValue(this);
				if (controlObject == null) {
					continue;
				}
				Control applyTo = controlObject as Control;
				if (applyTo == null) {
					// not a control
					continue;
				}
				IGreenshotLanguageBindable languageBindable = applyTo as IGreenshotLanguageBindable;
				if (languageBindable == null) {
					continue;
				}
				string languageKey = languageBindable.LanguageKey;
				// Apply language text to the control
				ApplyLanguage(applyTo, languageKey);
				// Repopulate the combox boxes
				if (typeof(IGreenshotConfigBindable).IsAssignableFrom(field.FieldType)) {
					if (typeof(GreenshotComboBox).IsAssignableFrom(field.FieldType)) {
						IGreenshotConfigBindable configBindable = controlObject as IGreenshotConfigBindable;
						if (!string.IsNullOrEmpty(configBindable.SectionName) && !string.IsNullOrEmpty(configBindable.PropertyName)) {
							IniSection section = IniConfig.GetIniSection(configBindable.SectionName);
							if (section != null) {
								GreenshotComboBox comboxBox = controlObject as GreenshotComboBox;
								// Only update the language, so get the actual value and than repopulate
								object currentValue = comboxBox.GetSelectedEnum(language, section.Values[configBindable.PropertyName].ValueType);
								comboxBox.Populate(language, section.Values[configBindable.PropertyName].ValueType, currentValue);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Apply the language text to supplied control
		/// </summary>
		protected void ApplyLanguage(Control applyTo, string languageKey) {
			if (!string.IsNullOrEmpty(languageKey)) {
				if (!language.hasKey(languageKey)) {
					MessageBox.Show(string.Format("Wrong language key '{0}' configured for control '{1}'", languageKey, applyTo.Name));
					return;
				}
				applyTo.Text = language.GetString(languageKey);
			} else {
				MessageBox.Show(string.Format("Greenshot control without language key: {0}", applyTo.Name));
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
							} else if (typeof(TextBox).IsAssignableFrom(field.FieldType)) {
								TextBox textBox = controlObject as TextBox;
								textBox.Text = (string)section.Values[configBindable.PropertyName].Value;
							} else if (typeof(GreenshotComboBox).IsAssignableFrom(field.FieldType)) {
								GreenshotComboBox comboxBox = controlObject as GreenshotComboBox;
								comboxBox.Populate(language, section.Values[configBindable.PropertyName].ValueType, (Enum)section.Values[configBindable.PropertyName].Value);
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
							section.Values[configBindable.PropertyName].Value = comboxBox.GetSelectedEnum(language, section.Values[configBindable.PropertyName].ValueType);
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
