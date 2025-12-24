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
using log4net;
using MessagePack;

namespace Greenshot.Ipc;

/// <summary>
/// Provides helper methods and constants for inter-process communication (IPC) between Greenshot processes.
/// </summary>
internal static class IpcHelper
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(IpcHelper));

    public const string PipePrefix = "Greenshot_Pipe_ID";
    public const string TargetProcessName = "Greenshot";

    /// <summary>
    /// Specifies the maximum number of bytes that is allowed to be received in a single IPC message.
    /// </summary>
    public const int MaxAllowedReceivedBytes = 1 * 1024 * 1024; // 1MB

    public static string GetPipeName(int processId) => PipePrefix + processId;

    /// <summary>
    /// Deserializes the specified byte array into an instance of the AppCommands class.
    /// </summary>
    /// <remarks>Using MessagePack for deserialization <see cref="IpcMessage"/>.</remarks>
    /// <param name="rawData">The raw binary data representing a serialized IpcMessage.</param>
    public static AppCommands Deserialize(byte[] rawData)
    {
        AppCommands appCommands;
        try
        {
            var msg = MessagePackSerializer.Deserialize<IpcMessage>(rawData);
            appCommands = new AppCommands(msg?.Commands ?? []);
        }
        catch (Exception e)
        {
            Log.ErrorFormat("Could not deserialize data: {0}", e.Message);
            throw;
        }
        return appCommands;
    }

    /// <summary>
    /// Serializes the specified set of application commands into a binary format.
    /// </summary>
    /// <remarks>Using MessagePack for serialization <see cref="IpcMessage"/>.</remarks>
    /// <param name="appCommands">The collection of application commands to serialize. Cannot be null.</param>
    /// <returns>A byte array containing the serialized IpcMessage</returns>
    public static byte[] Serialize(AppCommands appCommands)
    {
        byte[] bytes;
        try
        {
            var dto = new IpcMessage { Commands = appCommands.Commands }; 
            bytes = MessagePackSerializer.Serialize(dto);
        }
        catch (Exception e)
        {
            Log.ErrorFormat("Could not serialize appCommands: {0}", e.Message);
            throw;
        }
        return bytes;
    }

}
