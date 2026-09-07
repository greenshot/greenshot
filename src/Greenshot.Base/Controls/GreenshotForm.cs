/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */


#if DEBUG
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Ini.Interfaces;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// This form is the base for all Greenshot forms, providing automatic icon assignment, 
    /// configuration binding, and translation support.
    /// </summary>
    public class GreenshotForm : Form
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(GreenshotForm));
        protected static ICoreConfiguration coreConfiguration;
        private static readonly IDictionary<Type, FieldInfo[]> reflectionCache = new Dictionary<Type, FieldInfo[]>();

        private bool _storeFieldsManually;

        [ThreadStatic]
        private static bool _resolvingAssembly;

        static GreenshotForm()
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
                IniConfigHelper.EnsureInitialized();
                coreConfiguration = IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());
            }
            catch (Exception ex)
            {
                LOG.Warn("GreenshotForm static initialization fallback", ex);
                coreConfiguration ??= new CoreConfigurationImpl();
            }
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            if (_resolvingAssembly)
            {
                return null;
            }
            _resolvingAssembly = true;
            try
            {
                var requestedName = new AssemblyName(args.Name);
                if (requestedName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                // 1. If an assembly with the same simple name is already loaded in the AppDomain, return it
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(asm.GetName().Name, requestedName.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return asm;
                    }
                }

                // 2. Try to find the dll in the same directory as typeof(GreenshotForm).Assembly
                string baseDir = Path.GetDirectoryName(typeof(GreenshotForm).Assembly.Location);
                if (!string.IsNullOrEmpty(baseDir))
                {
                    string candidate = Path.Combine(baseDir, requestedName.Name + ".dll");
                    if (File.Exists(candidate))
                    {
                        return Assembly.LoadFrom(candidate);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                _resolvingAssembly = false;
            }
        }


        protected bool ManualStoreFields
        {
            get { return _storeFieldsManually; }
            set { _storeFieldsManually = value; }
        }

        /// <summary>
        /// When this is set, the form will be brought to the foreground as soon as it is shown.
        /// </summary>
        protected bool ToFront { get; set; }

        /// <summary>
        /// This method should be used to set all translated texts for the form and its controls.
        /// It is called on form load (or in derived constructors) and whenever the language changes at runtime.
        /// </summary>
        protected virtual void InitializeLanguage()
        {
        }

        public GreenshotForm()
        {
            DpiChanged += (sender, dpiChangedEventArgs) => DpiChangedHandler(dpiChangedEventArgs.DeviceDpiOld, dpiChangedEventArgs.DeviceDpiNew);
            Language.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(InitializeLanguage));
            }
            else
            {
                InitializeLanguage();
            }
        }

        /// <summary>
        /// This is the basic DpiChangedHandler responsible for all the DPI relative changes
        /// </summary>
        /// <param name="oldDpi"></param>
        /// <param name="newDpi"></param>
        protected virtual void DpiChangedHandler(int oldDpi, int newDpi)
        {
        }

        protected override void OnLoad(EventArgs e)
        {
            // Every GreenshotForm should have it's default icon
            Icon = GreenshotResources.GetGreenshotIcon();
#if DEBUG
            if (!DesignMode)
            {
#endif
                InitializeLanguage();
                FillFields();
                base.OnLoad(e);
#if DEBUG
            }
            else
            {
                base.OnLoad(e);
            }
#endif
        }

        /// <summary>
        /// Make sure the form is visible, if this is wanted
        /// </summary>
        /// <param name="e">EventArgs</param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (ToFront)
            {
                WindowDetails.ToForeground(Handle);
            }
        }

        /// <summary>
        /// check if the form was closed with an OK, if so store the values in the configuration
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            if (!DesignMode && !_storeFieldsManually)
            {
                if (DialogResult == DialogResult.OK)
                {
                    LOG.Info("Form was closed with OK: storing field values.");
                    StoreFields();
                }
            }

            base.OnClosed(e);
        }

        // Clean up any resources being used.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Language.LanguageChanged -= OnLanguageChanged;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Helper method to cache the fieldinfo values, so we don't need to reflect all the time!
        /// </summary>
        /// <param name="typeToGetFieldsFor"></param>
        /// <returns></returns>
        private static FieldInfo[] GetCachedFields(Type typeToGetFieldsFor)
        {
            if (!reflectionCache.TryGetValue(typeToGetFieldsFor, out var fields))
            {
                fields = typeToGetFieldsFor.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                reflectionCache.Add(typeToGetFieldsFor, fields);
            }

            return fields;
        }

        /// <summary>
        /// Fill all GreenshotControls with the values from the configuration
        /// </summary>
        private void FillFields()
        {
            foreach (FieldInfo field in GetCachedFields(GetType()))
            {
                var controlObject = field.GetValue(this);
                IGreenshotConfigBindable configBindable = controlObject as IGreenshotConfigBindable;
                if (string.IsNullOrEmpty(configBindable?.SectionName) || string.IsNullOrEmpty(configBindable.PropertyName)) continue;

                IIniSection section = IniConfigRegistry.Get()?.GetSection(configBindable.SectionName);
                if (section == null) continue;

                var propertyInfo = section.GetType().GetProperty(configBindable.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                {
                    LOG.DebugFormat("Wrong property '{0}' configured for field '{1}'", configBindable.PropertyName, field.Name);
                    continue;
                }
                var propertyValue = propertyInfo.GetValue(section);
                bool isFixed = section.IsConstant(configBindable.PropertyName);

                if (controlObject is CheckBox checkBox)
                {
                    checkBox.Checked = (bool) propertyValue;
                    checkBox.Enabled = !isFixed;
                    continue;
                }

                if (controlObject is RadioButton radíoButton)
                {
                    radíoButton.Checked = (bool) propertyValue;
                    radíoButton.Enabled = !isFixed;
                    continue;
                }

                if (controlObject is TextBox textBox)
                {
                    if (controlObject is HotkeyControl hotkeyControl)
                    {
                        string hotkeyValue = propertyValue as string;
                        if (!string.IsNullOrEmpty(hotkeyValue))
                        {
                            hotkeyControl.SetHotkey(hotkeyValue);
                            hotkeyControl.Enabled = !isFixed;
                        }

                        continue;
                    }

                    textBox.Text = propertyValue?.ToString() ?? string.Empty;
                    textBox.Enabled = !isFixed;
                    continue;
                }

                if (controlObject is GreenshotComboBox comboxBox)
                {
                    comboxBox.Populate(propertyInfo.PropertyType);
                    comboxBox.SetValue((Enum) propertyValue);
                    comboxBox.Enabled = !isFixed;
                }
            }

            OnFieldsFilled();
        }

        protected virtual void OnFieldsFilled()
        {
        }

        /// <summary>
        /// Store all GreenshotControl values to the configuration
        /// </summary>
        protected void StoreFields()
        {
            foreach (FieldInfo field in GetCachedFields(GetType()))
            {
                var controlObject = field.GetValue(this);
                IGreenshotConfigBindable configBindable = controlObject as IGreenshotConfigBindable;

                if (string.IsNullOrEmpty(configBindable?.SectionName) || string.IsNullOrEmpty(configBindable.PropertyName)) continue;

                IIniSection section = IniConfigRegistry.Get()?.GetSection(configBindable.SectionName);
                if (section == null) continue;

                var propertyInfo = section.GetType().GetProperty(configBindable.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null || !propertyInfo.CanWrite)
                {
                    continue;
                }

                if (controlObject is CheckBox checkBox)
                {
                    propertyInfo.SetValue(section, checkBox.Checked);
                    continue;
                }

                if (controlObject is RadioButton radioButton)
                {
                    propertyInfo.SetValue(section, radioButton.Checked);
                    continue;
                }

                if (controlObject is TextBox textBox)
                {
                    if (controlObject is HotkeyControl hotkeyControl)
                    {
                        propertyInfo.SetValue(section, hotkeyControl.ToString());
                        continue;
                    }

                    section.SetRawValue(configBindable.PropertyName, string.IsNullOrEmpty(textBox.Text) ? null : textBox.Text);
                    continue;
                }

                if (controlObject is GreenshotComboBox comboxBox)
                {
                    propertyInfo.SetValue(section, comboxBox.GetSelectedEnum());
                }
            }
        }
    }
}
