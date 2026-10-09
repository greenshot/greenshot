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
using System.Linq;
using System.Text;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// A line based diff of two texts (e.g. two versions of a recipe), shown when approving a changed recipe.
    /// </summary>
    public static class RecipeTextDiff
    {
        /// <summary>
        /// Above this number of lines (per text) no diff is made, the comparison is quadratic
        /// </summary>
        public const int MaxLines = 3000;

        /// <summary>
        /// One line of the diff
        /// </summary>
        public readonly struct DiffLine
        {
            public DiffLine(char kind, string text)
            {
                Kind = kind;
                Text = text;
            }

            /// <summary>
            /// ' ' unchanged, '-' only in the old text, '+' only in the new text
            /// </summary>
            public char Kind { get; }

            public string Text { get; }

            public override string ToString() => $"{Kind}{Text}";
        }

        /// <summary>
        /// The lines of both texts in order, marked as unchanged, removed or added. Null when a text has more than <see cref="MaxLines"/> lines.
        /// </summary>
        public static IReadOnlyList<DiffLine> Compare(string oldText, string newText)
        {
            var a = SplitLines(oldText);
            var b = SplitLines(newText);
            if (a.Length > MaxLines || b.Length > MaxLines)
            {
                return null;
            }

            // Longest common subsequence, from the end so the walk below can go forward
            var lcs = new int[a.Length + 1, b.Length + 1];
            for (int i = a.Length - 1; i >= 0; i--)
            {
                for (int j = b.Length - 1; j >= 0; j--)
                {
                    lcs[i, j] = string.Equals(a[i], b[j], StringComparison.Ordinal) ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                }
            }

            var result = new List<DiffLine>(a.Length + b.Length);
            int x = 0, y = 0;
            while (x < a.Length && y < b.Length)
            {
                if (string.Equals(a[x], b[y], StringComparison.Ordinal))
                {
                    result.Add(new DiffLine(' ', a[x]));
                    x++;
                    y++;
                }
                else if (lcs[x + 1, y] >= lcs[x, y + 1])
                {
                    result.Add(new DiffLine('-', a[x++]));
                }
                else
                {
                    result.Add(new DiffLine('+', b[y++]));
                }
            }
            while (x < a.Length)
            {
                result.Add(new DiffLine('-', a[x++]));
            }
            while (y < b.Length)
            {
                result.Add(new DiffLine('+', b[y++]));
            }
            return result;
        }

        /// <summary>
        /// The diff as text: the changed lines with "-" / "+" and some unchanged lines around them, "@@" between the parts.
        /// </summary>
        public static string ToUnifiedText(string oldText, string newText, int context = 3)
        {
            var lines = Compare(oldText, newText);
            if (lines == null)
            {
                return $"// The recipes are too large to compare (more than {MaxLines} lines).";
            }
            if (lines.All(l => l.Kind == ' '))
            {
                return "// No changes.";
            }

            var show = new bool[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Kind == ' ')
                {
                    continue;
                }
                for (int k = Math.Max(0, i - context); k <= Math.Min(lines.Count - 1, i + context); k++)
                {
                    show[k] = true;
                }
            }

            var builder = new StringBuilder();
            bool skipped = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!show[i])
                {
                    skipped = true;
                    continue;
                }
                if (skipped)
                {
                    builder.AppendLine("@@");
                }
                skipped = false;
                builder.Append(lines[i].Kind).Append(' ').AppendLine(lines[i].Text);
            }
            return builder.ToString();
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Array.Empty<string>();
            }
            return text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n').Split('\n');
        }
    }
}
