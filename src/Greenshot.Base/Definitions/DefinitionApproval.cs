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
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Greenshot.Base.Definitions;

/// <summary>
/// What the user sees before a definition (an external command, an uploader) is imported or approved: its icon, name and type,
/// what it does as label and value lines, the file, and notes. Shown by <see cref="DefinitionImport.Confirm(System.Windows.Window, DefinitionApproval, IReadOnlyList{string})"/>.
/// </summary>
public sealed class DefinitionApproval : INotifyPropertyChanged
{
    private ImageSource _icon;

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>The caption of the window, e.g. "Approve a command"</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The name of the definition</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What it is, e.g. "External command", under the name</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The question, e.g. "It was changed outside Greenshot. Use it?"</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>What it does, e.g. the program and its arguments, one label and value per line</summary>
    public IList<DefinitionApprovalDetail> Details { get; } = new List<DefinitionApprovalDetail>();

    /// <summary>More to know, one paragraph each, e.g. whom to trust</summary>
    public IList<string> Notes { get; } = new List<string>();

    /// <summary>
    /// The icon, e.g. of the program; null shows none. Can be set while the window is open (<see cref="LoadIcon"/>).
    /// </summary>
    public ImageSource Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasIcon)));
        }
    }

    public bool HasIcon => _icon != null;

    /// <summary>
    /// Reads the icon when the window is shown, e.g. from the program file; the window doesn't wait for it
    /// </summary>
    public Func<Task<ImageSource>> LoadIcon { get; set; }

    /// <summary>
    /// Add a line, nothing for an empty value
    /// </summary>
    public DefinitionApproval With(string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Details.Add(new DefinitionApprovalDetail(label, value));
        }

        return this;
    }

    /// <summary>
    /// The text a screen reader or a copy gets: everything in the order shown
    /// </summary>
    public override string ToString()
    {
        var lines = new List<string> { Name, Kind, Question };
        foreach (var detail in Details)
        {
            lines.Add($"{detail.Label}: {detail.Value}");
        }

        lines.AddRange(Notes);
        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// A line of <see cref="DefinitionApproval.Details"/>
/// </summary>
public sealed class DefinitionApprovalDetail
{
    public DefinitionApprovalDetail(string label, string value)
    {
        Label = label ?? string.Empty;
        Value = value ?? string.Empty;
    }

    public string Label { get; }

    public string Value { get; }
}
