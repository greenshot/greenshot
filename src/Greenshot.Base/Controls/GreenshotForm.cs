/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using log4net;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// This form is used for automatically binding the elements of the form to the language
    /// </summary>
    public class GreenshotForm : Form, IGreenshotLanguageBindable
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(GreenshotForm));
        protected static CoreConfiguration coreConfiguration;
        private static readonly IDictionary<Type, FieldInfo[]> reflectionCache = new Dictionary<Type, FieldInfo[]>();
#if DEBUG
        private IComponentChangeService m_changeService;
        private bool _isDesignModeLanguageSet;
        private IDictionary<string, Control> _designTimeControls;
        private IDictionary<string, ToolStripItem> _designTimeToolStripItems;
#endif
        private bool _applyLanguageManually;
        private bool _storeFieldsManually;

        static GreenshotForm()
        {
#if DEBUG
            if (!IsInDesignMode)
            {
#endif
                coreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
#if DEBUG
            }
#endif
        }

#if DEBUG
        [Category("Greenshot"), DefaultValue(null), Description("Specifies key of the language file to use when displaying the text.")]
#endif
        public string LanguageKey { get; set; }

#if DEBUG
        /// <summary>
        /// Used to check the designmode during a constructor
        /// </summary>
        /// <returns></returns>
        protected static bool IsInDesignMode
        {
            get
            {
                return (Application.ExecutablePath.IndexOf("devenv.exe", StringComparison.OrdinalIgnoreCase) > -1) ||
                       (Application.ExecutablePath.IndexOf("sharpdevelop.exe", StringComparison.OrdinalIgnoreCase) > -1 ||
                        (Application.ExecutablePath.IndexOf("wdexpress.exe", StringComparison.OrdinalIgnoreCase) > -1));
            }
        }
#endif

        protected bool ManualLanguageApply
        {
            get { return _applyLanguageManually; }
            set { _applyLanguageManually = value; }
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

        protected GreenshotForm()
        {
            DpiChanged += (sender, dpiChangedEventArgs) => DpiChangedHandler(dpiChangedEventArgs.DeviceDpiOld, dpiChangedEventArgs.DeviceDpiNew);
        }

        /// <summary>
        /// This is the basic DpiChangedHandler responsible for all the DPI relative changes
        /// </summary>
        /// <param name="oldDpi"></param>
        /// <param name="newDpi"></param>
        protected virtual void DpiChangedHandler(int oldDpi, int newDpi)
        {
        }

#if DEBUG
        /// <summary>
        /// Code to initialize the language etc during design time
        /// </summary>
        protected void InitializeForDesigner()
        {
            if (!DesignMode) return;
            _designTimeControls = new Dictionary<string, Control>();
            _designTimeToolStripItems = new Dictionary<string, ToolStripItem>();
            try
            {
                ITypeResolutionService typeResService = GetService(typeof(ITypeResolutionService)) as ITypeResolutionService;

                // Add a hard-path if you are using SharpDevelop
                // Language.AddLanguageFilePath(@"C:\Greenshot\Greenshot\Languages");

                // this "type"
                Assembly currentAssembly = GetType().Assembly;
                if (typeResService == null) return;

                string assemblyPath = typeResService.GetPathOfAssembly(currentAssembly.GetName());
                string assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                if (assemblyDirectory != null && !Language.AddLanguageFilePath(Path.Combine(assemblyDirectory, @"..\..\Greenshot\Languages\")))
                {
                    Language.AddLanguageFilePath(Path.Combine(assemblyDirectory, @"..\..\..\Greenshot\Languages\"));
                }

                if (assemblyDirectory != null && !Language.AddLanguageFilePath(Path.Combine(assemblyDirectory, @"..\..\Languages\")))
                {
                    Language.AddLanguageFilePath(Path.Combine(assemblyDirectory, @"..\..\..\Languages\"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// This override is only for the design-time of the form
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
#endif

        protected override void OnLoad(EventArgs e)
        {
            // Every GreenshotForm should have it's default icon
            // And it might not ne needed for a Tool Window, but still for the task manager / switcher it's important
            Icon = GreenshotResources.GetGreenshotIcon();
#if DEBUG
            if (!DesignMode)
            {
#endif
                if (!_applyLanguageManually)
                {
                    ApplyLanguage();
                }

                FillFields();
                base.OnLoad(e);
#if DEBUG
            }
            else
            {
                LOG.Info("OnLoad called from designer.");
                InitializeForDesigner();
                base.OnLoad(e);
                ApplyLanguage();
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
        /// check if the form was closed with an OK, if so store the values in the GreenshotControls
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

#if DEBUG
        /// <summary>
        /// This override allows the control to register event handlers for IComponentChangeService events
        /// at the time the control is sited, which happens only in design mode.
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

                m_changeService = (IComponentChangeService) GetService(typeof(IComponentChangeService));

                // Register event handlers for component change events.
                RegisterChangeNotifications();
            }
        }

        private void ClearChangeNotifications()
        {
            // The m_changeService value is null when not in design mode,
            // as the IComponentChangeService is only available at design time.
            m_changeService = (IComponentChangeService) GetService(typeof(IComponentChangeService));

            // Clear our the component change events to prepare for re-siting.
            if (m_changeService != null)
            {
                m_changeService.ComponentChanged -= OnComponentChanged;
                m_changeService.ComponentAdded -= OnComponentAdded;
            }
        }

        private void RegisterChangeNotifications()
        {
            // Register the event handlers for the IComponentChangeService events
            if (m_changeService != null)
            {
                m_changeService.ComponentChanged += OnComponentChanged;
                m_changeService.ComponentAdded += OnComponentAdded;
            }
        }

        /// <summary>
        /// This method handles the OnComponentChanged event to display a notification.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ce"></param>
        private void OnComponentChanged(object sender, ComponentChangedEventArgs ce)
        {
            if (((IComponent) ce.Component)?.Site == null || ce.Member == null) return;
            if (!"LanguageKey".Equals(ce.Member.Name)) return;
            if (ce.Component is Control control)
            {
                LOG.InfoFormat("Changing LanguageKey for {0} to {1}", control.Name, ce.NewValue);
                ApplyLanguage(control, (string) ce.NewValue);
            }
            else
            {
                if (ce.Component is ToolStripItem item)
                {
                    LOG.InfoFormat("Changing LanguageKey for {0} to {1}", item.Name, ce.NewValue);
                    ApplyLanguage(item, (string) ce.NewValue);
                }
                else
                {
                    LOG.InfoFormat("Not possible to changing LanguageKey for {0} to {1}", ce.Component.GetType(), ce.NewValue);
                }
            }
        }

        private void OnComponentAdded(object sender, ComponentEventArgs ce)
        {
            if (ce.Component?.Site == null) return;
            if (ce.Component is Control control)
            {
                if (!_designTimeControls.ContainsKey(control.Name))
                {
                    _designTimeControls.Add(control.Name, control);
                }
                else
                {
                    _designTimeControls[control.Name] = control;
                }
            }
            else
            {
                if (ce.Component is ToolStripItem stripItem)
                {
                    ToolStripItem item = stripItem;
                    if (!_designTimeControls.ContainsKey(item.Name))
                    {
                        _designTimeToolStripItems.Add(item.Name, item);
                    }
                    else
                    {
                        _designTimeToolStripItems[item.Name] = item;
                    }
                }
            }
        }

        // Clean up any resources being used.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearChangeNotifications();
            }

            base.Dispose(disposing);
        }
#endif

        protected void ApplyLanguage(ToolStripItem applyTo, string languageKey)
        {
            string langString;
            if (!string.IsNullOrEmpty(languageKey))
            {
                if (!Language.TryGetString(languageKey, out langString))
                {
                    LOG.DebugFormat("Unknown language key '{0}' configured for control '{1}', this might be okay.", languageKey, applyTo.Name);
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
                    LOG.DebugFormat("Greenshot control without language key: {0}", applyTo.Name);
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
            if (applyTo is not IGreenshotLanguageBindable languageBindable)
            {
                // check if it's a menu!
                if (applyTo is not ToolStrip toolStrip)
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
            if (applyTo is not (IGreenshotConfigBindable configBindable and GreenshotComboBox comboxBox)) return;
            if (string.IsNullOrEmpty(configBindable.SectionName) || string.IsNullOrEmpty(configBindable.PropertyName)) return;
            IniSection section = IniConfig.GetIniSection(configBindable.SectionName);
            if (section == null) return;
            // Only update the language, so get the actual value and than repopulate
            Enum currentValue = comboxBox.GetSelectedEnum();
            comboxBox.Populate(section.Values[configBindable.PropertyName].ValueType);
            comboxBox.SetValue(currentValue);
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
        /// Apply all the language settings to the "Greenshot" Controls on this form
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
                foreach (FieldInfo field in GetCachedFields(GetType()))
                {
                    object controlObject = field.GetValue(this);
                    if (controlObject == null)
                    {
                        LOG.DebugFormat("No value: {0}", field.Name);
                        continue;
                    }

                    if (controlObject is not Control applyToControl)
                    {
                        if (controlObject is not ToolStripItem applyToItem)
                        {
                            LOG.DebugFormat("No Control or ToolStripItem: {0}", field.Name);
                            continue;
                        }

                        ApplyLanguage(applyToItem);
                    }
                    else
                    {
                        ApplyLanguage(applyToControl);
                    }
                }
#if DEBUG
                if (DesignMode)
                {
                    foreach (Control designControl in _designTimeControls.Values)
                    {
                        ApplyLanguage(designControl);
                    }

                    foreach (ToolStripItem designToolStripItem in _designTimeToolStripItems.Values)
                    {
                        ApplyLanguage(designToolStripItem);
                    }
                }
#endif
            }
            finally
            {
                ResumeLayout();
            }
        }

        /// <summary>
        /// Apply the language text to supplied control
        /// </summary>
        protected void ApplyLanguage(Control applyTo, string languageKey)
        {
            string langString;
            if (!string.IsNullOrEmpty(languageKey))
            {
                if (!Language.TryGetString(languageKey, out langString))
                {
                    LOG.WarnFormat("Wrong language key '{0}' configured for control '{1}'", languageKey, applyTo.Name);
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
                    LOG.DebugFormat("Greenshot control without language key: {0}", applyTo.Name);
                }
            }
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

                IniSection section = IniConfig.GetIniSection(configBindable.SectionName);
                if (section == null) continue;

                if (!section.Values.TryGetValue(configBindable.PropertyName, out var iniValue))
                {
                    LOG.DebugFormat("Wrong property '{0}' configured for field '{1}'", configBindable.PropertyName, field.Name);
                    continue;
                }

                if (controlObject is CheckBox checkBox)
                {
                    checkBox.Checked = (bool) iniValue.Value;
                    checkBox.Enabled = !iniValue.IsFixed;
                    continue;
                }

                if (controlObject is RadioButton radíoButton)
                {
                    radíoButton.Checked = (bool) iniValue.Value;
                    radíoButton.Enabled = !iniValue.IsFixed;
                    continue;
                }

                if (controlObject is TextBox textBox)
                {
                    if (controlObject is HotkeyControl hotkeyControl)
                    {
                        string hotkeyValue = (string) iniValue.Value;
                        if (!string.IsNullOrEmpty(hotkeyValue))
                        {
                            hotkeyControl.SetHotkey(hotkeyValue);
                            hotkeyControl.Enabled = !iniValue.IsFixed;
                        }

                        continue;
                    }

                    textBox.Text = iniValue.ToString();
                    textBox.Enabled = !iniValue.IsFixed;
                    continue;
                }

                if (controlObject is GreenshotComboBox comboxBox)
                {
                    comboxBox.Populate(iniValue.ValueType);
                    comboxBox.SetValue((Enum) iniValue.Value);
                    comboxBox.Enabled = !iniValue.IsFixed;
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
            bool iniDirty = false;
            foreach (FieldInfo field in GetCachedFields(GetType()))
            {
                var controlObject = field.GetValue(this);
                IGreenshotConfigBindable configBindable = controlObject as IGreenshotConfigBindable;

                if (string.IsNullOrEmpty(configBindable?.SectionName) || string.IsNullOrEmpty(configBindable.PropertyName)) continue;

                IniSection section = IniConfig.GetIniSection(configBindable.SectionName);
                if (section == null) continue;

                if (!section.Values.TryGetValue(configBindable.PropertyName, out var iniValue))
                {
                    continue;
                }

                if (controlObject is CheckBox checkBox)
                {
                    iniValue.Value = checkBox.Checked;
                    iniDirty = true;
                    continue;
                }

                if (controlObject is RadioButton radioButton)
                {
                    iniValue.Value = radioButton.Checked;
                    iniDirty = true;
                    continue;
                }

                if (controlObject is TextBox textBox)
                {
                    if (controlObject is HotkeyControl hotkeyControl)
                    {
                        iniValue.Value = hotkeyControl.ToString();
                        iniDirty = true;
                        continue;
                    }

                    iniValue.UseValueOrDefault(textBox.Text);
                    iniDirty = true;
                    continue;
                }

                if (controlObject is GreenshotComboBox comboxBox)
                {
                    iniValue.Value = comboxBox.GetSelectedEnum();
                    iniDirty = true;
                }
            }

            if (iniDirty)
            {
                IniConfig.Save();
            }
        }
    }
}