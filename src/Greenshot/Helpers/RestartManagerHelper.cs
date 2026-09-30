/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Reactive.Linq;
using System.Windows.Forms;
using System.Windows.Threading;
using Dapplo.Windows.AppRestartManager;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Destinations;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using log4net;
using Greenshot.Base.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core.Export;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Helper class for integrating Greenshot with the Windows Restart Manager.
    /// The Restart Manager can shut down and restart applications gracefully during
    /// software updates or system maintenance.
    /// </summary>
    internal static class RestartManagerHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RestartManagerHelper));

        /// <summary>
        /// Directory where editor state is stored for restore after a system restart.
        /// </summary>
        public static string StateDirectory => Path.Combine(Path.GetTempPath(), "Greenshot", "RestartState");

        /// <summary>
        /// How long the end of the session waits for the editors to save their state
        /// </summary>
        private static readonly TimeSpan SaveStateTimeout = TimeSpan.FromSeconds(4);

        private static IDisposable _endSessionSubscription;

        /// <summary>
        /// Registers Greenshot for automatic restart by the Windows Restart Manager.
        /// When the Restart Manager restarts Greenshot, it will use the <c>--restore</c> argument
        /// so that Greenshot can restore any open image editors.
        /// </summary>
        public static void RegisterForRestart()
        {
            // Register with the Windows Restart Manager so it can restart us after updates
            // Don't restart if the application crashes
            ApplicationRestartManager.RegisterForRestart(commandLineArgs: "--restore");

            // WM_QUERYENDSESSION is not answered, which allows the session to end (an update will take place).
            // OnNext is called on the SharedMessageWindow thread, not on the UI thread.
            _endSessionSubscription?.Dispose();
            _endSessionSubscription = ApplicationRestartManager.ListenForEndSession()
                .Where(endSessionMessage => endSessionMessage.IsSessionEnding)
                .Subscribe(OnSessionEnding, ex => Log.Error("Error in the end session stream", ex));
        }

        /// <summary>
        /// The session really ends, the process can be terminated as soon as this returns: save the state synchronously and exit Greenshot
        /// </summary>
        /// <param name="endSessionMessage">EndSessionMessage</param>
        private static void OnSessionEnding(EndSessionMessage endSessionMessage)
        {
            Log.InfoFormat("Shutting down the application due to {0}", endSessionMessage.EndSessionReason);
            SaveEditorState();
            // Don't wait for the exit, the editors might want to ask the user something
            UiDispatcher.Current.RunOnUiAsync(() =>
            {
                Application.Exit();
                Environment.Exit(0);
            }).FireAndLog("Exit after the end of the session", Log);
        }

        /// <summary>
        /// Unregisters Greenshot from automatic restart by the Windows Restart Manager.
        /// </summary>
        public static void UnregisterForRestart()
        {
            try
            {
                ApplicationRestartManager.UnregisterForRestart();
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to unregister from automatic restart.", ex);
            }
        }

        /// <summary>
        /// Saves the state of all currently open image editors to the restart state directory
        /// as <c>.greenshot</c> files, so they can be restored after a Restart Manager restart.
        /// Any previously saved state is replaced.
        /// </summary>
        public static void SaveEditorState()
        {
            try
            {
                string stateDir = StateDirectory;
                Directory.CreateDirectory(stateDir);

                // Remove stale .greenshot files from any previous session
                foreach (string oldFile in Directory.GetFiles(stateDir, "*.greenshot"))
                {
                    try
                    {
                        File.Delete(oldFile);
                    }
                    catch (Exception ex)
                    {
                        Log.WarnFormat("Could not remove stale state file '{0}': {1}", oldFile, ex.Message);
                    }
                }

                // The editors live on the UI thread, but the end of the session is reported on the SharedMessageWindow thread
                // and the process can be terminated as soon as it was handled: wait (limited) until the state is saved.
                var saveTask = UiDispatcher.Current.RunOnUiAsync(() =>
                {
                    foreach (var editor in ImageEditorForm.Editors.ToArray())
                    {
                        try
                        {
                            string filename = Path.Combine(stateDir, editor.Surface.ID + ".greenshot");
                            if (editor.TrySaveState(filename))
                            {
                                Log.InfoFormat("Saved editor state to: {0}", filename);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warn("Failed to save state for one editor.", ex);
                        }
                    }
                });
                // R1 exception: this runs inside the window procedure of the SharedMessageWindow for WM_ENDSESSION,
                // Windows can terminate the process as soon as it returns, so there is nothing to await on.
#pragma warning disable RS0030, VSTHRD002
                bool saved = saveTask.Wait(SaveStateTimeout);
#pragma warning restore RS0030, VSTHRD002
                if (!saved)
                {
                    Log.WarnFormat("Saving the editor state didn't finish within {0}.", SaveStateTimeout);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to save editor states for restart.", ex);
            }
        }

        /// <summary>
        /// Open an editor with the saved state, the state file is removed when the editor shows it.
        /// </summary>
        private static async Task RestoreEditorAsync(string filePath)
        {
            try
            {
                ISurface surface = new Surface();
                surface = ImageIO.LoadGreenshotSurface(filePath, surface);
                surface.CaptureDetails = new CaptureDetails();
                var result = await DestinationExporter.ExportAsync(DestinationHelper.GetDestination(EditorDestination.DESIGNATION), surface, surface.CaptureDetails, true);
                if (result.IsSucceeded)
                {
                    File.Delete(filePath);
                }
                else
                {
                    Log.WarnFormat("Couldn't open an editor with state file {0}: {1}", filePath, result.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't open an editor with state file: " + filePath, ex);
            }
        }

        /// <summary>
        /// Restores any <c>.greenshot</c> state files saved by <see cref="SaveEditorState"/>
        /// so that the editors will be restored when Greenshot starts with the <c>--restore</c> argument.
        /// </summary>
        public static void RestoreState()
        {
            Log.InfoFormat("Greenshot started with a request to restore state.");
            try
            {
                string stateDir = StateDirectory;
                if (!Directory.Exists(stateDir))
                {
                    return;
                }

                foreach (string filePath in Directory.GetFiles(stateDir, "*.greenshot"))
                {
                    // Called on the UI thread (startup), the surface is created there
                    RestoreEditorAsync(filePath).FireAndLog("Restore an editor", Log);
                    Log.InfoFormat("Queued restore of editor state from: {0}", filePath);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to queue editor state restore.", ex);
            }
        }
    }
}
