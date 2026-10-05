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
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base.Core;
using log4net;
using Greenshot.Base.Threading;
using Greenshot.Base.Languages;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// This form is the base for all Greenshot forms, providing automatic icon assignment and translation support.
    /// </summary>
    public class GreenshotForm : Form
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(GreenshotForm));
        protected static ICoreConfiguration coreConfiguration => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());
        [ThreadStatic]
        private static bool _resolvingAssembly;

        static GreenshotForm()
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            }
            catch (Exception ex)
            {
                LOG.Warn("GreenshotForm static initialization fallback", ex);
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

        /// <summary>
        /// True when <see cref="InitializeLanguage"/> must be called when the form loads.
        /// A form which already calls it in its constructor can return false, so the work isn't done twice.
        /// </summary>
        protected virtual bool InitializeLanguageOnLoad => true;

        public GreenshotForm()
        {
            DpiChanged += (sender, dpiChangedEventArgs) => DpiChangedHandler(dpiChangedEventArgs.DeviceDpiOld, dpiChangedEventArgs.DeviceDpiNew);
            Texts.Config.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // The language can change on another thread
            UiDispatcher.Current.RunOnUiAsync(InitializeLanguage).FireAndLog("Initialize the language of " + GetType().Name);
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
                if (InitializeLanguageOnLoad)
                {
                    InitializeLanguage();
                }
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

        // Clean up any resources being used.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Texts.Config.LanguageChanged -= OnLanguageChanged;
            }

            base.Dispose(disposing);
        }
    }
}
