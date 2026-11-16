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

using Avalonia.Media;
using Greenshot.Editor.Standalone.Tools;

namespace Greenshot.Editor.Standalone.ViewModels
{
    /// <summary>
    /// A button of the tool rail: its icon, its name with the key, and whether it is the tool in use
    /// </summary>
    public sealed class ToolViewModel : ObservableObject
    {
        private readonly EditorWindowViewModel _window;

        public ToolViewModel(EditorWindowViewModel window, ToolKind kind)
        {
            _window = window;
            Kind = kind;
        }

        public ToolKind Kind { get; }

        public string Name => EditorTexts.NameOf(Kind);

        public string ToolTip => Kind switch
        {
            ToolKind.Rectangle => EditorTexts.Text.ToolRectangle,
            ToolKind.Ellipse => EditorTexts.Text.ToolEllipse,
            ToolKind.Line => EditorTexts.Text.ToolLine,
            ToolKind.Arrow => EditorTexts.Text.ToolArrow,
            ToolKind.Freehand => EditorTexts.Text.ToolFreehand,
            ToolKind.Text => EditorTexts.Text.ToolText,
            ToolKind.SpeechBubble => EditorTexts.Text.ToolSpeechBubble,
            ToolKind.StepLabel => EditorTexts.Text.ToolStepLabel,
            ToolKind.Emoji => EditorTexts.Text.ToolEmoji,
            ToolKind.Highlight => EditorTexts.Text.ToolHighlight,
            ToolKind.Obfuscate => EditorTexts.Text.ToolObfuscate,
            ToolKind.CutMark => EditorTexts.Text.ToolCutMark,
            ToolKind.Crop => EditorTexts.Text.ToolCrop,
            _ => EditorTexts.Text.ToolSelect
        };

        public IImage Icon => EditorIcons.Get(Kind.ToString());

        public bool IsSelected
        {
            get => _window.SelectedTab?.ToolKind == Kind;
            set
            {
                if (value && _window.SelectedTab != null)
                {
                    _window.SelectedTab.ToolKind = Kind;
                }
            }
        }

        public void Refresh() => OnPropertyChanged(nameof(IsSelected));

        public override string ToString() => Name;
    }
}
