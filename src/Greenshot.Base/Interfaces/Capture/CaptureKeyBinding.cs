/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Text;
using System.Windows.Input;
using log4net;

namespace Greenshot.Base.Interfaces.Capture
{
    /// <summary>
    /// A key of the CaptureWindow: what it does (the description, shown by the help overlay) and who handles it.
    /// Registered with ICaptureToolHost.RegisterKey (always active) or RegisterToolKey (only while that tool is active).
    /// </summary>
    public sealed class CaptureKeyBinding
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CaptureKeyBinding));
        private readonly Func<string> _description;
        private readonly Func<ICaptureTool, bool> _activeWhen;

        public CaptureKeyBinding(object owner, ICaptureTool tool, Key key, ModifierKeys modifiers, Func<string> description, Action execute)
            : this(owner, tool, key, modifiers, description, execute, null)
        {
        }

        /// <param name="activeWhen">For a key which isn't bound to a tool: the tools it works with, e.g. only those which select pixels, null for all</param>
        public CaptureKeyBinding(object owner, ICaptureTool tool, Key key, ModifierKeys modifiers, Func<string> description, Action execute,
            Func<ICaptureTool, bool> activeWhen)
        {
            _activeWhen = activeWhen;
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _description = description ?? throw new ArgumentNullException(nameof(description));
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
            if (key == Key.None)
            {
                throw new ArgumentException("A key is needed", nameof(key));
            }
            Tool = tool;
            Key = key;
            Modifiers = modifiers;
        }

        /// <summary>
        /// Who registered the key: the CaptureWindow, a tool or an overlay
        /// </summary>
        public object Owner { get; }

        /// <summary>
        /// The tool for which the key is active, null when it is always active
        /// </summary>
        public ICaptureTool Tool { get; }

        public Key Key { get; }

        public ModifierKeys Modifiers { get; }

        /// <summary>
        /// What happens when the key is pressed
        /// </summary>
        public Action Execute { get; }

        /// <summary>
        /// What the key does, in the current language: the callback given at the registration is called every time,
        /// so the text follows a language switch
        /// </summary>
        public string Description
        {
            get
            {
                try
                {
                    return _description() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Error getting the description of the capture key {KeyText} of {Owner.GetType().FullName}", ex);
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// The key as it is shown, e.g. "Ctrl+C" or "Esc"
        /// </summary>
        public string KeyText => GetKeyText(Key, Modifiers);

        /// <summary>
        /// True when both bindings can be active at the same time with the same key:
        /// a key which is always active conflicts with every other use of the key, a tool key only with the keys
        /// which are always active and with the other keys of the same tool. Two tools can use the same key.
        /// </summary>
        public bool ConflictsWith(CaptureKeyBinding other) =>
            other != null && other.Key == Key && other.Modifiers == Modifiers && (Tool == null || other.Tool == null || Tool == other.Tool);

        /// <summary>
        /// True when the key is active while the given tool is
        /// </summary>
        public bool IsActiveFor(ICaptureTool activeTool) => Tool == null ? _activeWhen?.Invoke(activeTool) ?? true : Tool == activeTool;

        public override string ToString() => $"{KeyText} ({Owner.GetType().Name})";

        private static readonly Dictionary<Key, string> KeyNames = new Dictionary<Key, string>
        {
            { Key.Return, "Enter" },
            { Key.Escape, "Esc" },
            { Key.Space, "Space" },
            { Key.Up, "↑" },
            { Key.Down, "↓" },
            { Key.Left, "←" },
            { Key.Right, "→" },
            { Key.LeftShift, "Shift" },
            { Key.RightShift, "Shift" },
            { Key.Back, "Backspace" },
            { Key.Delete, "Del" },
            { Key.Insert, "Ins" },
            { Key.PageUp, "PgUp" },
            { Key.PageDown, "PgDn" },
            { Key.OemPlus, "+" },
            { Key.OemMinus, "-" },
            { Key.OemComma, "," },
            { Key.OemPeriod, "." },
            { Key.Add, "Num +" },
            { Key.Subtract, "Num -" },
            { Key.Multiply, "Num *" },
            { Key.Divide, "Num /" }
        };

        /// <summary>
        /// The text for a key, e.g. "Ctrl+Shift+C"
        /// </summary>
        public static string GetKeyText(Key key, ModifierKeys modifiers)
        {
            var text = new StringBuilder();
            if ((modifiers & ModifierKeys.Control) != 0)
            {
                text.Append("Ctrl+");
            }
            if ((modifiers & ModifierKeys.Alt) != 0)
            {
                text.Append("Alt+");
            }
            if ((modifiers & ModifierKeys.Shift) != 0)
            {
                text.Append("Shift+");
            }
            if ((modifiers & ModifierKeys.Windows) != 0)
            {
                text.Append("Win+");
            }
            if (KeyNames.TryGetValue(key, out var name))
            {
                text.Append(name);
            }
            else if (key >= Key.D0 && key <= Key.D9)
            {
                text.Append((char)('0' + (key - Key.D0)));
            }
            else if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                text.Append("Num ").Append((char)('0' + (key - Key.NumPad0)));
            }
            else
            {
                text.Append(key);
            }
            return text.ToString();
        }
    }

    /// <summary>
    /// A key was registered which is already used, see CaptureKeyBinding.ConflictsWith
    /// </summary>
    public class CaptureKeyConflictException : InvalidOperationException
    {
        public CaptureKeyConflictException(CaptureKeyBinding requested, CaptureKeyBinding existing)
            : base($"The key {requested.KeyText} of {requested.Owner.GetType().FullName} is already used by {existing.Owner.GetType().FullName} ({existing.Description})")
        {
            Requested = requested;
            Existing = existing;
        }

        /// <summary>
        /// The binding which could not be registered
        /// </summary>
        public CaptureKeyBinding Requested { get; }

        /// <summary>
        /// The binding which has the key
        /// </summary>
        public CaptureKeyBinding Existing { get; }
    }
}
