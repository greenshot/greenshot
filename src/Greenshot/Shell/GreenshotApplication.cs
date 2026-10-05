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


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Dapplo.Ini;
using Dapplo.Windows.Kernel32;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Helpers;
using Greenshot.Ipc;
using Greenshot.Ipc.Cli;
using Greenshot.ViewModels;
using Greenshot.Views;
using log4net;

namespace Greenshot.Shell
{
    /// <summary>
    /// The start of Greenshot: only one Greenshot runs per user session, a second start forwards its command to the running one.
    /// </summary>
    internal static class GreenshotApplication
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotApplication));
        private static ResourceMutex _applicationMutex;

        /// <summary>
        /// Application entry-point, called from <see cref="GreenshotMain"/> after the
        /// <see cref="IniConfigRegistry"/> has been set up and command-line arguments
        /// have been parsed.
        /// </summary>
        public static void Start(CommandLineOptions options)
        {
            try
            {
                // Fix for Bug 2495900, Multi-user Environment
                // check whether there's an local instance running already
                _applicationMutex = ResourceMutex.Create("F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08", "Greenshot", false);

                var isAlreadyRunning = !_applicationMutex.IsLocked;

                // A command (e.g. a file, --recipe, --reload, --exit) is handled exactly like one from greenshot-cli.exe:
                // the unparsed arguments are sent as a CLI request and parsed by the running Greenshot
                IpcEnvelope startupCommand = null;
                if (options.CommandArguments.Length > 0)
                {
                    var parsed = CliCommandParser.Parse(options.CommandArguments, IpcSources.Cli, Environment.CurrentDirectory);
                    if (!parsed.Success)
                    {
                        Log.Warn($"Invalid command line: {parsed.Error}");
                        GreenshotCommandLine.ReportError(parsed.Error);
                        FreeMutex();
                        return;
                    }

                    startupCommand = IpcEnvelope.CreateCli(options.CommandArguments, IpcSources.Cli, Environment.CurrentDirectory);
                    if (isAlreadyRunning)
                    {
                        Log.Info($"Sending the command '{parsed.Envelope.Command}' to the running Greenshot.");
                        NamedPipeClient.SendMessage(startupCommand);
                        FreeMutex();
                        return;
                    }

                    // Nothing to exit or to reload when Greenshot is not running
                    if (parsed.Envelope.Command is "EXIT" or "RELOAD_CONFIG")
                    {
                        FreeMutex();
                        return;
                    }
                    // Otherwise Greenshot starts and runs the command itself, see the GreenshotShell constructor
                }

                if (options.NoRun)
                {
                    // Make an exit possible
                    FreeMutex();
                    return;
                }

                if (isAlreadyRunning)
                {
                    var instances = new List<RunningInstanceViewModel>();
                    bool matchedThisProcess = false;
                    int index = 1;
                    int currentProcessId;
                    using (Process currentProcess = Process.GetCurrentProcess())
                    {
                        currentProcessId = currentProcess.Id;
                    }

                    foreach (Process greenshotProcess in Process.GetProcessesByName("greenshot"))
                    {
                        try
                        {
                            string path = Kernel32Api.GetProcessPath(greenshotProcess.Id);
                            instances.Add(new RunningInstanceViewModel
                            {
                                Index = index++,
                                ProcessId = greenshotProcess.Id,
                                Path = path
                            });
                            if (currentProcessId == greenshotProcess.Id)
                            {
                                matchedThisProcess = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex);
                        }

                        greenshotProcess.Dispose();
                    }

                    if (!matchedThisProcess)
                    {
                        using Process currentProcess = Process.GetCurrentProcess();
                        instances.Add(new RunningInstanceViewModel
                        {
                            Index = index,
                            ProcessId = currentProcess.Id,
                            Path = Kernel32Api.GetProcessPath(currentProcess.Id)
                        });
                    }

                    var instanceWindow = new InstanceRunningWindow(instances);
                    instanceWindow.ShowDialog();

                    FreeMutex();
                    Application.Exit();
                    return;
                }

                // This is the Greenshot instance which runs: read greenshot.ini now, before anything (the language, the plugins,
                // the shell) uses the configuration. The plugins add their sections later, they are filled from the loaded content.
                IniConfigRegistry.Get().Load();

                // Apply the command line language before the language is used the first time
                if (options.Language != null)
                {
                    IniConfigRegistry.GetSection<ICoreConfiguration>().Language = options.Language;
                }

                // The texts in the configured language, before anything shows one (also when a text was used before greenshot.ini was read)
                Texts.Initialize();
                Texts.SetLanguage(IniConfigRegistry.GetSection<ICoreConfiguration>().Language);

                // Make sure we handle END Session correctly
                RestartManagerHelper.RegisterForRestart(IniConfigRegistry.Get().OverrideDirectory);

                // Make sure we can use forms
                WindowsFormsHost.EnableWindowsFormsInterop();

                // From here on we continue starting Greenshot
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.ApplicationExit += Application_ApplicationExit;

                CreateWpfApplication();

                Application.Run(new GreenshotShell(options, startupCommand));
            }
            catch (Exception ex)
            {
                Log.Error("Exception in startup.", ex);
                GreenshotMain.Application_ThreadException(Form.ActiveForm, new ThreadExceptionEventArgs(ex));
            }
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            FreeMutex();
        }

        /// <summary>
        /// The WPF Application of the UI thread, it isn't run: the message loop stays the WinForms one of <see cref="GreenshotShell"/>
        /// (the editor is a WinForms form). It gives the WPF code Application.Current, its Windows (owners of dialogs) and a Dispatcher,
        /// it never shuts down by itself when a window closes.
        /// </summary>
        private static void CreateWpfApplication()
        {
            if (System.Windows.Application.Current != null)
            {
                return;
            }

            var wpfApplication = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
            // An exception in a WPF dispatcher operation is reported like one of a WinForms window, and Greenshot continues
            wpfApplication.Dispatcher.UnhandledException += (sender, args) =>
            {
                GreenshotMain.Application_ThreadException(sender, new ThreadExceptionEventArgs(args.Exception));
                args.Handled = true;
            };
        }


        internal static void FreeMutex()
        {
            // Remove the application mutex
            if (_applicationMutex == null)
            {
                return;
            }
            try
            {
                _applicationMutex.Dispose();
                _applicationMutex = null;
            }
            catch (Exception ex)
            {
                Log.Error("Error releasing Mutex!", ex);
            }
        }
    }
}
