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
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Dapplo.CaliburnMicro.Dapp;
using Dapplo.Ini.Converters;
using Dapplo.Language;
using Dapplo.Log;
using Dapplo.Log.Loggers;
using Dapplo.Windows.Kernel32;
using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Point = System.Drawing.Point;
#endregion

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
        public static void Main(string[] arguments)
        {
            // TODO: Set via build
            StringEncryptionTypeConverter.RgbIv = "dlgjowejgogkklwj";
            StringEncryptionTypeConverter.RgbKey = "lsjvkwhvwujkagfauguwcsjgu2wueuff";

#if DEBUG
            // Initialize a debug logger for Dapplo packages
            LogSettings.RegisterDefaultLogger<DebugLogger>(LogLevels.Verbose);
#endif
            var application = new Dapplication("Greenshot", "F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08")
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
                return;
            }

            // Load the assemblies, and run the application
            application.Bootstrapper.FindAndLoadAssemblies("Dapplo.*");
            // Make sure the non-plugin DLLs are also loaded, so exports are available.
            application.Bootstrapper.FindAndLoadAssemblies("Greenshot*");
            application.Bootstrapper.FindAndLoadAssemblies("Greenshot*", extensions: new[] { "gsp" });
            application.Run();
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

            // Placehold for the Extension
            IGreenshotLanguage language = null;

            // A dirty fix to make sure the messagebox is visible as a Greenshot window on the taskbar
            using (var multiInstanceForm = new Form
            {
                Icon = GreenshotResources.GetGreenshotIcon(),
                ShowInTaskbar = true,
                MaximizeBox = false,
                MinimizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Location = new Point(int.MinValue, int.MinValue),
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
                    Height = 20
                };
                flowLayoutPanel.Controls.Add(cancelButton);
                multiInstanceForm.Controls.Add(flowLayoutPanel);

                multiInstanceForm.CancelButton = cancelButton;

                multiInstanceForm.ShowDialog();
            }
        }

    }
}
