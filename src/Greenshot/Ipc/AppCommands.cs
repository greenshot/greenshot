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

using System.Collections.Generic;
using System.Linq;

namespace Greenshot.Ipc;

/// <summary>
/// Lst of all possible commands
/// </summary>
public enum CommandEnum
{
    OpenFile,
    Exit,
    FirstLaunch,
    ReloadConfig
};

/// <summary>
/// Represents a collection of application commands with optional string data.
/// </summary>
public class AppCommands
{
    private readonly List<KeyValuePair<CommandEnum, string>> _commands;
    public List<KeyValuePair<CommandEnum, string>> Commands => _commands;

    public AppCommands()
    {
        _commands = new List<KeyValuePair<CommandEnum, string>>();
    }

    public AppCommands(CommandEnum command) : this()
    {
        AddCommand(command, null);
    }

    public AppCommands(CommandEnum command, string commandData) : this()
    {
        AddCommand(command, commandData);
    }

    public AppCommands(List<KeyValuePair<CommandEnum, string>> commands) : this()
    {
        foreach (var command in commands)
        {
            AddCommand(command.Key, command.Value);
        }
    }

    public void AddCommand(CommandEnum command)
    {
        _commands.Add(new KeyValuePair<CommandEnum, string>(command, null));
    }

    public void AddCommand(CommandEnum command, string commandData)
    {
        _commands.Add(new KeyValuePair<CommandEnum, string>(command, commandData));
    }

    public override string ToString()
    {
        return "[" + string.Join(", ", _commands.Select(c => c.Key.ToString())) + "]";
    }
}

