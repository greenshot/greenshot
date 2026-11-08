/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Greenshot.Base.Wpf;
using Microsoft.Win32;

namespace Greenshot.Base.Definitions;

/// <summary>
/// The steps of an import every kind of definition goes through, each plugin on its own settings page: pick the file, read
/// and check it, show the user what it does and let them confirm, then store it. The plugin gives the file type, the check
/// and the text; nothing is stored before the user confirmed.
/// </summary>
public static class DefinitionImport
{
    /// <summary>
    /// The buttons of the confirmation react only after this time, so a double click doesn't import
    /// </summary>
    public static readonly TimeSpan ArmDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Let the user pick a file
    /// </summary>
    /// <param name="owner">the settings window</param>
    /// <param name="filter">the file types, e.g. "Uploader files (*.sxcu)|*.sxcu"</param>
    /// <returns>the path, null when the user cancelled</returns>
    public static string PickFile(Window owner, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false
        };
        return (owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog()) == true ? dialog.FileName : null;
    }

    /// <summary>
    /// Read the file (UTF-8) and check it with the parser of the kind.
    /// </summary>
    /// <param name="path">the file</param>
    /// <param name="maximumLength">larger files aren't read</param>
    /// <param name="parse">parses and checks the text, throws <see cref="DefinitionFileException"/> with the reason when it can't be used</param>
    /// <param name="tooLargeError">the reason for a file over <paramref name="maximumLength"/></param>
    /// <param name="error">why it can't be imported, null when it can</param>
    /// <returns>the checked definition, null with <paramref name="error"/></returns>
    public static T ReadFile<T>(string path, long maximumLength, Func<string, T> parse, string tooLargeError, out string error) where T : class
    {
        if (parse == null) throw new ArgumentNullException(nameof(parse));
        error = null;
        try
        {
            if (new FileInfo(path).Length > maximumLength)
            {
                error = tooLargeError;
                return null;
            }

            return parse(File.ReadAllText(path, Encoding.UTF8));
        }
        catch (DefinitionFileException ex)
        {
            error = ex.Message;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error = ex.Message;
        }

        return null;
    }

    /// <summary>
    /// Show what the definition does and let the user decide. The last button is Cancel: the default of Enter and Escape,
    /// and the result when the box is closed. The buttons react only after <see cref="ArmDelay"/>.
    /// </summary>
    /// <param name="owner">the settings window</param>
    /// <param name="title">the caption</param>
    /// <param name="question">what the definition does, e.g. where the captures go</param>
    /// <param name="buttons">the choices, the last one cancels</param>
    /// <returns>the index of the chosen button, -1 or the last index for Cancel</returns>
    public static int Confirm(Window owner, string title, string question, IReadOnlyList<string> buttons)
    {
        if (buttons == null || buttons.Count < 2) throw new ArgumentException("At least one choice and Cancel", nameof(buttons));
        int cancel = buttons.Count - 1;
        return ThemedMessageBox.ShowChoice(owner, title, question, MessageBoxImage.Warning, buttons, defaultIndex: cancel, cancelIndex: cancel, armDelay: ArmDelay);
    }
}
