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
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Autofac;
using Autofac.Features.OwnedInstances;
using Caliburn.Micro;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.CaliburnMicro.Dapp;
using Dapplo.Config.Ini.Converters;
using Dapplo.Config.Language;
using Dapplo.Log;
#if DEBUG
using Dapplo.Log.Loggers;
#endif
using Dapplo.Utils;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi.Forms;
using Dapplo.Windows.Kernel32;
using Greenshot.Addons;
using Greenshot.Addons.Resources;
using Greenshot.Ui.Misc.ViewModels;

namespace Greenshot
{
    /// <summary>
    ///     Greenshot application startup
    /// </summary>
    public static class Startup
    {
        /// <summary>
        ///     Start Greenshot application
        /// </summary>
        [STAThread]
        public static int Main(string[] arguments)
        {
            // TODO: Set via build
            StringEncryptionTypeConverter.RgbIv = "dlgjowejgogkklwj";
            StringEncryptionTypeConverter.RgbKey = "lsjvkwhvwujkagfauguwcsjgu2wueuff";

            // Make sure the exceptions in the log are readable, uses Ben.Demystifier
            //LogSettings.ExceptionToStacktrace = exception => exception.ToStringDemystified();
#if DEBUG
            // Initialize a debug logger for Dapplo packages
            LogSettings.RegisterDefaultLogger<DebugLogger>(LogLevels.Debug);
#endif
            var applicationConfig = ApplicationConfigBuilder
                .Create()
                .WithApplicationName("Greenshot")
                .WithMutex("F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08")
                .WithCaliburnMicro()
                .WithoutCopyOfEmbeddedAssemblies()
#if !NETCOREAPP3_0
                .WithoutCopyOfAssembliesToProbingPath()
#endif
                .WithAssemblyPatterns("Greenshot.Addon.*")
                .BuildApplicationConfig();

            var application = new Dapplication(applicationConfig)
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };

            // Prevent multiple instances
            if (application.WasAlreadyRunning)
            {
                // TODO: Call the running instance, pass the commandline
                ShowInstances();
                // Don't start the dapplication, exit with 0
                application.Shutdown(0);
                return -1;
            }

            //RegisterErrorHandlers(application);

            application.Run();
            return 0;
        }

        /// <summary>
        /// Make sure all exception handlers are hooked
        /// </summary>
        /// <param name="application">Dapplication</param>
        private static void RegisterErrorHandlers(Dapplication application)
        {
            application.ObserveUnhandledTaskException = true;
            application.OnUnhandledAppDomainException += (exception, b) => DisplayErrorViewModel(application, exception);
            application.OnUnhandledDispatcherException += exception => DisplayErrorViewModel(application, exception);
            application.OnUnhandledTaskException += exception => DisplayErrorViewModel(application, exception);
        }

        /// <summary>
        /// Show the exception
        /// </summary>
        /// <param name="application">Dapplication</param>
        /// <param name="exception">Exception</param>
        private static async void DisplayErrorViewModel(Dapplication application, Exception exception)
        {
            var windowManager = application.Bootstrapper.Container?.Resolve<IWindowManager>();
            if (windowManager == null)
            {
                Debugger.Break();
                return;
            }
            using (var errorViewModel = application.Bootstrapper.Container.Resolve<Owned<ErrorViewModel>>())
            {
                if (errorViewModel == null)
                {
                    return;
                }
                errorViewModel.Value.SetExceptionToDisplay(exception);
                if (!UiContext.HasUiAccess)
                {
                    await UiContext.RunOn(() => windowManager.ShowDialog(errorViewModel.Value));
                }
                else
                {
                    windowManager.ShowDialog(errorViewModel.Value);
                }
            }
        }

        /// <summary>
        ///     Show all the running instances
        /// </summary>
        private static void ShowInstances()
        {
            var instanceInfo = new StringBuilder();
            var index = 1;
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.ProcessName.ToLowerInvariant().Contains("greenshot"))
                    {
                        instanceInfo.AppendFormat("{0} : {1} (pid {2})", index++, Kernel32Api.GetProcessPath(process.Id), process.Id);
                        instanceInfo.Append(Environment.NewLine);
                    }
                }
                catch (Exception)
                {
                    //Log.Debug().WriteLine(ex);
                }
                process.Dispose();
            }

            var greenshotResources = new GreenshotResources(new ManifestResources(null));
            // Placehold for the Extension
            IGreenshotLanguage language = null;

            // A dirty fix to make sure the messagebox is visible as a Greenshot window on the taskbar
            using (var multiInstanceForm = new DpiAwareForm
            {

                // TODO: Fix a problem that in this case instance is null 
                Icon = greenshotResources.GetGreenshotIcon(),
                ShowInTaskbar = true,
                MaximizeBox = false,
                MinimizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Location = new NativePoint(int.MinValue, int.MinValue),
                Text = language.TranslationOrDefault(l => l.Error),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                StartPosition = FormStartPosition.CenterScreen
            })
            {
                var flowLayoutPanel = new FlowLayoutPanel
                {
                    AutoScroll = true,
                    FlowDirection = System.Windows.Forms.FlowDirection.TopDown,
                    WrapContents = false,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                var internalFlowLayoutPanel = new FlowLayoutPanel
                {
                    AutoScroll = true,
                    FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight,
                    WrapContents = false,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };
                var pictureBox = new PictureBox
                {
                    Dock = DockStyle.Left,
                    Image = SystemIcons.Error.ToBitmap(),
                    SizeMode = PictureBoxSizeMode.AutoSize
                };
                internalFlowLayoutPanel.Controls.Add(pictureBox);
                var textbox = new Label
                {
                    Text = language.TranslationOrDefault(l => l.ErrorMultipleinstances) + Environment.NewLine + instanceInfo,
                    AutoSize = true
                };
                internalFlowLayoutPanel.Controls.Add(textbox);
                flowLayoutPanel.Controls.Add(internalFlowLayoutPanel);
                var cancelButton = new Button
                {
                    Text = language.TranslationOrDefault(l => l.BugreportCancel),
                    Dock = DockStyle.Bottom,
                    Height = 25
                };
                flowLayoutPanel.Controls.Add(cancelButton);
                multiInstanceForm.Controls.Add(flowLayoutPanel);

                multiInstanceForm.CancelButton = cancelButton;

                multiInstanceForm.ShowDialog();
            }
        }

    }
}
