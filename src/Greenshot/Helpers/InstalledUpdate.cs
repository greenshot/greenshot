/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026  Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Ini;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Another version of Greenshot was installed while this one runs (issue #732): the setup renamed the files in use to *.old
    /// and installed the new ones, see code.iss. A deployment tool which runs the setup in another session (as SYSTEM, from a service)
    /// can't tell this Greenshot, so it notices the new Greenshot.exe itself, and restarts into it when nothing is open.
    /// Until then it runs on with its own files: those it hasn't loaded yet are already the new ones.
    /// </summary>
    internal static class InstalledUpdate
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(InstalledUpdate));

        /// <summary>
        /// The setup creates this while it runs (SetupMutex in setup-header.iss): a Greenshot.exe which is new already doesn't mean the rest is
        /// </summary>
        private const string SetupMutexName = @"Global\GreenshotSetup";

        /// <summary>
        /// The id of the Greenshot which restarts into the new one: the new one waits until it exited, it keeps greenshot.ini locked until then
        /// </summary>
        private const string PreviousProcessVariable = "GREENSHOT_RESTARTED_FROM";

        private static string _executablePath;
        private static string _startVersion;

        /// <summary>
        /// Greenshot exits to start the new version, see <see cref="StartNewVersion"/>
        /// </summary>
        public static bool RestartRequested { get; set; }

        /// <summary>
        /// At the start, before greenshot.ini is read: wait for the Greenshot which restarted into this one,
        /// and remember the version of Greenshot.exe (its name can change while it runs, when the setup renames it)
        /// </summary>
        public static void Initialize()
        {
            WaitForPreviousProcess();
            _executablePath = Application.ExecutablePath;
            _startVersion = ReadVersion(_executablePath);
        }

        /// <summary>
        /// A different Greenshot.exe is in the directory of this one, and no setup is running anymore
        /// </summary>
        public static bool IsInstalled()
        {
            if (_startVersion == null)
            {
                return false;
            }

            string version = ReadVersion(_executablePath);
            if (version == null || version == _startVersion)
            {
                return false;
            }

            if (IsSetupRunning())
            {
                Log.Info($"Greenshot {version} is being installed, this is {_startVersion}: waiting until the setup is done.");
                return false;
            }

            Log.Info($"Greenshot {version} was installed, this is {_startVersion}.");
            return true;
        }

        /// <summary>
        /// After the exit, when <see cref="RestartRequested"/>: start the installed Greenshot
        /// </summary>
        public static void StartNewVersion()
        {
            if (!RestartRequested)
            {
                return;
            }

            try
            {
                // Like a restart by the Windows Restart Manager: the same greenshot.ini, and no second Greenshot if one was started meanwhile
                var startInfo = new ProcessStartInfo(_executablePath, RestartManagerHelper.CreateRestartArguments(IniConfigRegistry.Get().OverrideDirectory))
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(_executablePath) ?? string.Empty
                };
                using var currentProcess = Process.GetCurrentProcess();
                startInfo.EnvironmentVariables[PreviousProcessVariable] = currentProcess.Id.ToString();
                using (Process.Start(startInfo))
                {
                    Log.Info("Started the installed Greenshot.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't start the installed Greenshot.", ex);
            }
        }

        private static string ReadVersion(string path)
        {
            try
            {
                return File.Exists(path) ? FileVersionInfo.GetVersionInfo(path).FileVersion : null;
            }
            catch (Exception ex)
            {
                Log.Debug($"Couldn't read the version of {path}", ex);
                return null;
            }
        }

        private static bool IsSetupRunning()
        {
            try
            {
                if (!Mutex.TryOpenExisting(SetupMutexName, MutexRights.Synchronize, out var setupMutex))
                {
                    return false;
                }

                setupMutex.Dispose();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // It exists, created by the account the setup runs with (SYSTEM)
                return true;
            }
        }

        private static void WaitForPreviousProcess()
        {
            string previousProcessId = Environment.GetEnvironmentVariable(PreviousProcessVariable);
            if (!int.TryParse(previousProcessId, out int processId))
            {
                return;
            }

            // Not passed on to the programs this Greenshot starts
            Environment.SetEnvironmentVariable(PreviousProcessVariable, null);
            try
            {
                using var previousProcess = Process.GetProcessById(processId);
                // Before the first window, there is nothing to keep responsive; the previous Greenshot is at its very end
                if (!previousProcess.WaitForExit(30000))
                {
                    Log.Warn($"The previous Greenshot (process {processId}) didn't exit within 30 seconds.");
                }
            }
            catch (ArgumentException)
            {
                // It exited already
            }
        }
    }
}
