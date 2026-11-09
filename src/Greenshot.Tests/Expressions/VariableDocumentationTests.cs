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
using System.IO;
using Greenshot.Base.Expressions;
using Xunit;

namespace Greenshot.Tests.Expressions
{
    /// <summary>
    /// docs/vNext/variables.md lists every value the resolver has, as the code describes it
    /// </summary>
    public class VariableDocumentationTests
    {
        [Fact]
        public void VariablesDoc_ValueTable_MatchesTheCode()
        {
            string doc = FindDoc();
            Assert.True(doc != null, "docs/vNext/variables.md not found from the test directory");
            string text = File.ReadAllText(doc).Replace("\r\n", "\n");
            int start = text.IndexOf(VariableDocumentation.StartMarker, StringComparison.Ordinal);
            int end = text.IndexOf(VariableDocumentation.EndMarker, StringComparison.Ordinal);
            Assert.True(start >= 0 && end > start, "The markers of the value table are missing in variables.md");
            string documented = text.Substring(start + VariableDocumentation.StartMarker.Length, end - start - VariableDocumentation.StartMarker.Length).Trim('\n') + "\n";

            // A resolver of its own: the shared one may hold values a plugin or another test registered
            long counter = 0;
            var resolver = new VariableResolver(() => counter, v => counter = v, () => Path.Combine(Path.GetTempPath(), "greenshot-doc-test-counters.txt"));
            string expected = VariableDocumentation.ValueTable(resolver);

            Assert.True(expected == documented, "The value table in docs/vNext/variables.md is out of date, replace it with:\n" + expected);
        }

        private static string FindDoc()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "docs", "vNext", "variables.md");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                directory = directory.Parent;
            }
            return null;
        }
    }
}
