using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using System.ComponentModel;

namespace GreenshotPlugin.Controls {
	public class GreenshotForm : Form , IGreenshotLanguageBindable {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(GreenshotForm));
		protected ILanguage language;

		[Category("Greenshot"), DefaultValue(null), Description("Specifies key of the language file to use when displaying the text.")]
		public string LanguageKey {
			get;
			set;
		}

		public GreenshotForm() : base() {
		}

		protected override void OnLoad(EventArgs e) {
			if (!this.DesignMode) {
				ApplyLanguage();
				FillFields();
			}
			base.OnLoad(e);
		}

		/// <summary>
		/// check if the form was closed with an OK, if so store the values in the GreenshotControls
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosed(EventArgs e) {
			if (DialogResult == DialogResult.OK) {
				LOG.Info("Form was closed with OK: storing field values.");
				StoreFields();
			}
			base.OnClosed(e);
		}

		/// <summary>
		/// Apply all the language settings to the "Greenshot" Controls on this form
		/// </summary>
		protected void ApplyLanguage() {
			if (language == null) {
				throw new ArgumentNullException("Language not set!!");
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
				if (typeof(IGreenshotLanguageBindable).IsAssignableFrom(field.FieldType)) {
					IGreenshotLanguageBindable languageBindable = controlObject as IGreenshotLanguageBindable;
					if (!string.IsNullOrEmpty(languageBindable.LanguageKey)) {
						if (!language.hasKey(languageBindable.LanguageKey)) {
							LOG.WarnFormat("Wrong language key '{0}' configured for field '{1}'", languageBindable.LanguageKey, field.Name);
							continue;
						}
						Control control = controlObject as Control;
						control.Text = language.GetString(languageBindable.LanguageKey);
					} else {
						LOG.WarnFormat("Greenshot control without language key: {0}", field.Name);
					}
				}
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
