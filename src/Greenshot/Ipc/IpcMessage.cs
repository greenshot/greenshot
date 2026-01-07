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
using System.Collections.Generic;
using System.Diagnostics;
using MessagePack;

namespace Greenshot.Ipc;

/// <summary>
/// Represents a Data Transfer Object (DTO) for inter-process communication (IPC) messages.
/// </summary>
[MessagePackObject]
public class IpcMessage
{
    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    [Key(0)]
    public Guid MessageId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Process identifier (PID) of the sender.
    /// </summary>
    [Key(1)]
    public int SenderPid { get; set; } = Process.GetCurrentProcess().Id;

    /// <summary>
    /// The list of application commands included in the message.
    /// </summary>
    [Key(2)]
    public List<KeyValuePair<CommandEnum, string>> Commands { get; set; }
}
