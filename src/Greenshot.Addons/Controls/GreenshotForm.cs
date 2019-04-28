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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Config.Ini;
using Dapplo.Config.Interfaces;
using Dapplo.Config.Language;
using Dapplo.Log;
using Dapplo.Utils;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Forms;
using Greenshot.Addons.Resources;
using Greenshot.Gfx;

namespace Greenshot.Addons.Controls
{
	/// <summary>
	///     This form is used for automatically binding the elements of the form to the language
	/// </summary>
	public class GreenshotForm : DpiAwareForm, IGreenshotLanguageBindable
	{
	    private static readonly LogSource Log = new LogSource();
		private static readonly IDictionary<Type, FieldInfo[]> ReflectionCache = new Dictionary<Type, FieldInfo[]>();
	    private readonly ILanguage _language;
		
        /// <summary>
        /// This is the bitmap scale handler
        /// </summary>
		protected readonly BitmapScaleHandler<string, IBitmapWithNativeSupport> ScaleHandler;

#if DEBUG
        public GreenshotForm()
	    {

	    }
#endif
        /// <summary>
        /// Default constructor, for default functionality like DPI awareness
        /// </summary>
        protected GreenshotForm(ILanguage language)
        {
            _language = language;
            // Add the Dapplo.Windows DPI change handler
            ScaleHandler = BitmapScaleHandler.Create<string, IBitmapWithNativeSupport>(FormDpiHandler, (imageName, dpi) => GreenshotResources.Instance.GetBitmap(imageName, GetType()), (bitmap, dpi) => bitmap.ScaleIconForDisplaying(dpi));
        }

        /// <summary>
        /// manually apply the language
        /// </summary>
	    protected bool ManualLanguageApply { get; set; }

        /// <summary>
        /// Manually apply the field values
        /// </summary>
		protected bool ManualStoreFields { get; set; }

		/// <summary>
		///     When this is set, the form will be brought to the foreground as soon as it is shown.
		/// </summary>
		protected bool ToFront { get; set; }

        /// <summary>
        /// The kex for the translation
        /// </summary>
		[Category("Greenshot")]
		[DefaultValue(null)]
		[Description("Specifies key of the language file to use when displaying the text.")]
		public string LanguageKey { get; set; }

        /// <inheritdoc />
		protected override void OnLoad(EventArgs e)
		{
			// Every GreenshotForm should have it's default icon
			// And it might not ne needed for a Tool Window, but still for the task manager / switcher it's important
			Icon = GreenshotResources.Instance.GetGreenshotIcon();
			if (!ManualLanguageApply)
			{
				ApplyLanguage();
			}
			FillFields();
			base.OnLoad(e);
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
				await InteropWindowFactory.CreateFor(Handle).ToForegroundAsync().ConfigureAwait(false);
			}
		}

		/// <summary>
		///     check if the form was closed with an OK, if so store the values in the GreenshotControls
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosed(EventArgs e)
		{
			if (!ManualStoreFields)
			{
				if (DialogResult == DialogResult.OK)
				{
					Log.Info().WriteLine("Form was closed with OK: storing field values.");
					StoreFields();
				}
			}
			base.OnClosed(e);
		}

        /// <summary>
        /// 	    Clean up any resources being used.
        /// </summary>
		protected override void Dispose(bool disposing)
		{
			ScaleHandler.Dispose();
			base.Dispose(disposing);
		}

	    private void ApplyLanguage(ToolStripItem applyTo, string languageKey)
		{
		    if (_language == null)
		    {
		        return;
		    }

		    string translation;
		    if (!string.IsNullOrEmpty(languageKey))
		    {
		        if (_language.TryGetTranslation(languageKey, out translation))
		        {
		            applyTo.Text = translation;
		            return;
		        }

		        var dotIndex = languageKey.IndexOf('.');
		        if (dotIndex >= 0)
		        {
		            var alternativeKey = languageKey.Substring(dotIndex + 1);
		            if (_language.TryGetTranslation(alternativeKey, out translation))
		            {
		                applyTo.Text = translation;
		                return;
		            }
		        }
            }

		    if (_language.TryGetTranslation(applyTo.Name, out translation))
		    {
		        applyTo.Text = translation;
		        return;
		    }

		    Log.Warn().WriteLine("Unknown language key '{0}' configured for control '{1}'", languageKey, applyTo.Name);
		}

	    private void ApplyLanguage(ToolStripItem applyTo)
		{
		    if (applyTo is IGreenshotLanguageBindable languageBindable)
			{
				ApplyLanguage(applyTo, languageBindable.LanguageKey);
			}
		}

	    private void ApplyLanguage(Control applyTo)
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

            // TODO: Fix this
		    IIniSection section = null; // IniConfig.Current[configBindable.SectionName];
		    if (section == null)
		    {
		        return;
		    }

		    // Only update the language, so get the actual value and than repopulate
		    var currentValue = comboxBox.GetSelectedEnum();
		    comboxBox.Populate(section.GetIniValue(configBindable.PropertyName).ValueType);
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
		    if (_language == null)
		    {
		        return;
		    }
			SuspendLayout();
			try
			{
				// Set title of the form
			    if (!string.IsNullOrEmpty(LanguageKey) && _language.Keys().Contains(LanguageKey, AbcComparer.Instance))
				{
                    Text = _language[LanguageKey];
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

				    if (controlObject is Control applyToControl)
					{
					    ApplyLanguage(applyToControl);
					    continue;
                    }

				    if (controlObject is ToolStripItem applyToItem)
				    {
				        ApplyLanguage(applyToItem);
				        continue;
				    }
				    Log.Debug().WriteLine("No Control or ToolStripItem: {0}", field.Name);

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
		private void ApplyLanguage(Control applyTo, string languageKey)
		{
		    if (_language == null)
		    {
		        return;
		    }

		    string translation;
		    if (!string.IsNullOrEmpty(languageKey))
		    {
		        if (_language.TryGetTranslation(languageKey, out translation))
		        {
		            applyTo.Text = translation;
		            return;
		        }

		        var dotIndex = languageKey.IndexOf('.');
		        if (dotIndex >= 0)
		        {
		            var alternativeKey = languageKey.Substring(dotIndex + 1);
		            if (_language.TryGetTranslation(alternativeKey, out translation))
		            {
		                applyTo.Text = translation;
		                return;
		            }
		        }
		    }

		    if (_language.TryGetTranslation(applyTo.Name, out translation))
		    {
		        applyTo.Text = translation;
		        return;
		    }
		    Log.Warn().WriteLine("Wrong language key '{0}' configured for control '{1}'", languageKey, applyTo.Name);
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

                // TODO: Fix this
			    IIniSection section = null;//IniConfig.Current[configBindable.SectionName];
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

        /// <summary>
        /// This is called when the fields are filled
        /// </summary>
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

                // TODO: Fix this
			    IIniSection section = null;//IniConfig.Current[configBindable.SectionName];
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