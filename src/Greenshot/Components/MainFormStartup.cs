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
using System.Linq;
using System.Windows.Forms;
using Autofac.Features.OwnedInstances;
using Dapplo.Addons;
using Dapplo.CaliburnMicro;
using Dapplo.Log;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Forms;

namespace Greenshot.Components
{
    /// <summary>
    /// This startup action starts the MainForm
    /// </summary>
    [Service(nameof(MainFormStartup), nameof(FormsStartup), nameof(CaliburnServices.ConfigurationService), TaskSchedulerName = "ui")]
    public class MainFormStartup : IStartup, IShutdown
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly MainForm _mainForm;
        private readonly Func<Owned<LanguageDialog>> _languageDialogFactory;
        private readonly WindowHandle _windowHandle;

        public MainFormStartup(
            ICoreConfiguration coreConfiguration,
            MainForm mainForm,
            Func<Owned<LanguageDialog>> languageDialogFactory,
            WindowHandle windowHandle)
        {
            _coreConfiguration = coreConfiguration;
            _mainForm = mainForm;
            _languageDialogFactory = languageDialogFactory;
            _windowHandle = windowHandle;
        }

        /// <inheritdoc />
        public void Startup()
        {
            Log.Debug().WriteLine($"Starting MainForm, current language {_coreConfiguration.Language}");

            // if language is not set, show language dialog
            if (string.IsNullOrEmpty(_coreConfiguration.Language))
            {
                using (var ownedLanguageDialog = _languageDialogFactory())
                {
                    ownedLanguageDialog.Value.ShowDialog();
                    _coreConfiguration.Language = ownedLanguageDialog.Value.SelectedLanguage;
                }
            }

            // This makes sure the MainForm can initialize, calling show first would create the "Handle" and causing e.g. the DPI Handler to be to late.
            _mainForm.Initialize();
            _mainForm.Show();
            _windowHandle.Handle = _mainForm.Handle;
            Log.Debug().WriteLine("Started MainForm");
        }

        public void Shutdown()
        {
            Log.Debug().WriteLine("Stopping MainForm");

            // Close all open forms, use a separate List to make sure we don't get a "InvalidOperationException: Collection was modified"
            foreach (var form in Application.OpenForms.Cast<Form>().ToList())
            {
                try
                {
                    Log.Info().WriteLine("Closing form: {0}", form.Name);
                    form.Close();
                    form.Dispose();
                }
                catch (Exception e)
                {
                    Log.Error().WriteLine(e, "Error closing form!");
                }
            }
        }
    }
}
