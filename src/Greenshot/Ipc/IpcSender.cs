/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO.Pipes;
using log4net;

namespace Greenshot.Ipc;

internal static class IpcSender
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(IpcListener));

    /// <inheritdoc cref="Broadcast(AppCommands)"/>
    public static void Send(AppCommands appCommands)
    {
        Broadcast(appCommands);
    }

    /// <summary>
    /// Sends the application commands to all running Greenshot processes (Broadcast) excluding the current process.
    /// </summary>
    /// <param name="commands"></param>
    private static void Broadcast(AppCommands commands)
    {
        var datatoSend = IpcHelper.Serialize(commands);
        var currentPid = Process.GetCurrentProcess().Id;

        // try to send to all processes with the same name
        foreach (var process in Process.GetProcessesByName(IpcHelper.TargetProcessName))
        {
            try
            {
                if (process.Id == currentPid)
                {
                    continue; // do not send to current process itself
                }
                var pipeName = IpcHelper.GetPipeName(process.Id);
                Log.DebugFormat("Sending AppCommands {0} to Pipe {1}", commands, pipeName);

                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
                client.Connect(100); // max. warten 100ms
                using var writer = new BinaryWriter(client);
                writer.Write(datatoSend);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Broadcast-Fehler: {0}", ex.Message);
            }
        }
    }

}

