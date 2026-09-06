/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Windows.Threading;
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.Messages.Enumerations;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Destinations;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using log4net;

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
        private static Dispatcher _dispatcher;

        /// <summary>
        /// Directory where editor state is stored for restore after a system restart.
        /// </summary>
        public static string StateDirectory => Path.Combine(Path.GetTempPath(), "Greenshot", "RestartState");

        /// <summary>
        /// Registers Greenshot for automatic restart by the Windows Restart Manager.
        /// When the Restart Manager restarts Greenshot, it will use the <c>--restore</c> argument
        /// so that Greenshot can restore any open image editors.
        /// </summary>
        public static void RegisterForRestart()
        {
            // Capture the current dispatcher for use in saving editor state during shutdown
            _dispatcher = Dispatcher.CurrentDispatcher;

            // Register with the Windows Restart Manager so it can restart us after updates
            // Don't restart if the application crashes
            ApplicationRestartManager.RegisterForRestart(commandLineArgs: "--restore");

            ApplicationRestartManager.ListenForEndSession(
                onQuerySession: (endSessionReason) => {
                    // Accept that an update will take place and allow the session to end
                    return true;
                },
                onEndSession: (endSessionReason) =>
                {
                    // Do the work, save state and exit Greenshot
                    Debug.WriteLine($"Shutting down application due to {endSessionReason}");
                    SaveEditorState();
                    return true;
                }
                ).Subscribe(endSessionMessage =>
                {
                    Debug.WriteLine($"{endSessionMessage.Msg} with session reason: {endSessionMessage.EndSessionReason}");
                });
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

                var editors = ImageEditorForm.Editors.ToArray();
                _dispatcher.Invoke(() =>
                {
                    foreach (var editor in editors)
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
                    // Make sure the application exits after saving state
                    Application.Exit();
                    Environment.Exit(0);
                });
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to save editor states for restart.", ex);
            }
        }

        /// <summary>
        /// Adds any <c>.greenshot</c> state files saved by <see cref="SaveEditorState"/> to the
        /// supplied <paramref name="transport"/> as <see cref="CommandEnum.OpenFile"/> commands, so
        /// that the editors will be restored when Greenshot starts with the <c>--restore</c> argument.
        /// </summary>
        /// <param name="transport">Transport object to which restore commands are added.</param>
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
                    _dispatcher.Invoke(() => {
                        ISurface surface = new Surface();
                        surface = ImageIO.LoadGreenshotSurface(filePath, surface);
                        surface.CaptureDetails = new CaptureDetails();
                        try
                        {
                            DestinationHelper.GetDestination(EditorDestination.DESIGNATION).ExportCapture(true, surface, surface.CaptureDetails);
                            File.Delete(filePath);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Couldn't open an editor with state file: " + filePath, ex);
                        }
                    });
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
