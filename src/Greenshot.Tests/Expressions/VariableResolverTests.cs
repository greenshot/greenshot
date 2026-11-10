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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Greenshot.Base.Expressions;
using Xunit;

namespace Greenshot.Tests.Expressions
{
    public sealed class VariableResolverTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "greenshot-expressions-" + Guid.NewGuid().ToString("N"));
        private long _global;
        private readonly VariableResolver _resolver;
        private static readonly DateTime Time = new DateTime(2026, 11, 6, 14, 5, 9);

        public VariableResolverTests()
        {
            TestEnvironment.EnsureInitialized();
            Directory.CreateDirectory(_folder);
            _resolver = new VariableResolver(() => _global, v => _global = v, () => Path.Combine(_folder, "counters.txt"));
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_folder, true);
            }
            catch (IOException)
            {
            }
        }

        private VariableScope Scope(string title = "Window", IDictionary<string, object> values = null)
        {
            var all = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["payload.title"] = title,
                ["payload.time"] = Time
            };
            if (values != null)
            {
                foreach (var pair in values) all[pair.Key] = pair.Value;
            }
            return new VariableScope { Values = all, TargetFolder = _folder, FixedNow = Time };
        }

        private string Text(string template, VariableScope scope = null) => _resolver.Render(template, TemplatePurpose.Value, scope ?? Scope());

        private void Touch(string relative)
        {
            string path = Path.Combine(_folder, relative.Replace('\\', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "x");
        }

        [Theory]
        [InlineData("plain text", "plain text")]
        [InlineData("$${payload.title}", "${payload.title}")]
        [InlineData("${payload.title}", "Window")]
        [InlineData("a ${'}'} b", "a } b")]
        [InlineData("${1 + 2 * 3}", "7")]
        [InlineData("${(1 + 2) * 3}", "9")]
        [InlineData("${7 / 2}", "3.5")]
        [InlineData("${7 % 4}", "3")]
        [InlineData("${-3 + 1}", "-2")]
        [InlineData("${'a' + 1}", "a1")]
        [InlineData("${missing.value ?? 'none'}", "none")]
        [InlineData("${(2 > 1 ? 'yes' : 'no')}", "yes")]
        [InlineData("${payload.title == 'window'}", "true")]
        [InlineData("${!(1 == 1) || 1 < 2 && 'b' >= 'a'}", "true")]
        [InlineData("${1 / 0 ?? 'n/a'}", "n/a")]
        public void Render_Expressions(string template, string expected)
        {
            // missing.value: an unknown namespace is an error for validation, rendering as Value only logs it
            Assert.Equal(expected, Text(template));
        }

        [Theory]
        [InlineData("${payload.time:yyyy-MM-dd}", "2026-11-06")]
        [InlineData("${payload.time:HH:mm:ss}", "14:05:09")]
        [InlineData("${payload.time:'HH:mm'}", "14:05")]
        [InlineData("${payload.time:\"yyyy\"}", "2026")]
        [InlineData("${3:000}", "003")]
        [InlineData("${1234.5:0.00}", "1234.50")]
        [InlineData("${(1 > 0 ? 5 : 6):00}", "05")]
        public void Render_Formats(string template, string expected)
        {
            Assert.Equal(expected, Text(template));
        }

        [Theory]
        [InlineData("${left('abcdef', 2)}", "ab")]
        [InlineData("${right('abcdef', 2)}", "ef")]
        [InlineData("${substring('abcdef', -3, 2)}", "de")]
        [InlineData("${replace('a-b-c', '-', '+')}", "a+b+c")]
        [InlineData("${upper('abc')}", "ABC")]
        [InlineData("${trim('  x ')}", "x")]
        [InlineData("${padLeft('7', 3, '0')}", "007")]
        [InlineData("${length('abc')}", "3")]
        [InlineData("${contains('Hello', 'ELL')}", "true")]
        [InlineData("${match('Issue GS-123 open', '[A-Z]+-\\d+')}", "GS-123")]
        [InlineData("${match('GS-123', '([A-Z]+)-(?<n>\\d+)', 'n')}", "123")]
        [InlineData("${match('GS-123', '([A-Z]+)-(\\d+)', 1)}", "GS")]
        [InlineData("${replaceMatch('a1b22', '\\d+', '#')}", "a#b#")]
        [InlineData("${urlEncode('a b/c?d=1&e')}", "a%20b%2Fc%3Fd%3D1%26e")]
        [InlineData("${fileNameWithoutExtension('c:/x/shot.png')}", "shot")]
        [InlineData("${extension('shot.png')}", ".png")]
        [InlineData("${round(2.5)}", "3")]
        [InlineData("${round(2.345, 2)}", "2.35")]
        [InlineData("${max(2, 7)}", "7")]
        [InlineData("${isoWeek(date('2026-01-01', 'yyyy-MM-dd'))}", "1")]
        [InlineData("${addDays(payload.time, 1):yyyy-MM-dd}", "2026-11-07")]
        [InlineData("${number('12') + 1}", "13")]
        [InlineData("${empty('')}", "true")]
        [InlineData("${length(random(8))}", "8")]
        public void Render_Functions(string template, string expected)
        {
            Assert.Equal(expected, Text(template));
        }

        [Fact]
        public void Evaluate_SingleExpression_KeepsItsType()
        {
            Assert.Equal(6L, _resolver.Evaluate("${2 * 3}", Scope()));
            Assert.Equal(Time, _resolver.Evaluate("${payload.time}", Scope()));
            Assert.Equal(true, _resolver.Evaluate("${payload.title}", Scope(), TemplatePurpose.Condition));
            Assert.Equal(false, _resolver.Evaluate("${payload.title}", Scope(""), TemplatePurpose.Condition));
        }

        [Theory]
        [InlineData("${payload.title")]
        [InlineData("${1 +}")]
        [InlineData("${'open}")]
        [InlineData("${unknown(1)}")]
        [InlineData("${left('a')}")]
        [InlineData("${title}")]
        [InlineData("${session.issue}")]
        public void Validate_Errors(string template)
        {
            Assert.Contains(_resolver.Validate(template, TemplatePurpose.Text), e => !e.IsWarning);
        }

        [Fact]
        public void Validate_ChoiceWithFormat_AsksForParentheses()
        {
            var errors = _resolver.Validate("${1 > 0 ? 'a' : 'b'}", TemplatePurpose.Text);
            Assert.Contains(errors, e => e.Message.Contains("parentheses"));
        }

        [Fact]
        public void Validate_OldName_GivesTheNewOne()
        {
            var errors = _resolver.Validate("${capturetime}", TemplatePurpose.Text);
            Assert.Contains(errors, e => e.Message.Contains("payload.time"));
        }

        [Theory]
        [InlineData("${payload.title}", TemplatePurpose.Folder)]
        [InlineData("${payload.title}", TemplatePurpose.Command)]
        [InlineData("${var.anything}", TemplatePurpose.Argument)]
        [InlineData("${counter.global}", TemplatePurpose.Text)]
        [InlineData("${counter.scope('x')}", TemplatePurpose.Folder)]
        public void Validate_NotAllowedForPurpose(string template, TemplatePurpose purpose)
        {
            Assert.Contains(_resolver.Validate(template, purpose), e => !e.IsWarning);
            if (purpose != TemplatePurpose.Text)
            {
                Assert.Throws<TemplateException>(() => _resolver.Render(template, purpose, Scope()));
            }
        }

        [Theory]
        [InlineData("${payload.title} ${counter.global}")]
        [InlineData("${payload.time:yyyy-MM-dd}\\${counter.folder:000} - ${payload.title}")]
        [InlineData("${payload.title}_${counter.name:000}")]
        [InlineData("${payload.title}-${counter.scope(payload.title)}")]
        public void Validate_FileNameTemplates_AreValid(string template)
        {
            Assert.DoesNotContain(_resolver.Validate(template, TemplatePurpose.FileName), e => !e.IsWarning);
        }

        [Theory]
        [InlineData("${counter.folder:000}\\${payload.title}")]
        [InlineData("${payload.title}${counter.folder:000}${payload.title}")]
        [InlineData("${counter.folder + 1}")]
        [InlineData("${counter.folder} ${counter.name}")]
        public void Validate_FolderCounter_Rules(string template)
        {
            Assert.Contains(_resolver.Validate(template, TemplatePurpose.FileName), e => !e.IsWarning);
        }

        [Fact]
        public void FileName_ValuesAreMadeSafe()
        {
            var name = _resolver.RenderFileName("${payload.title}", ".png", Scope("a\\b/c:d*?" + new string('x', 200)));
            Assert.DoesNotContain("\\", name.Name);
            Assert.DoesNotContain("/", name.Name);
            Assert.DoesNotContain(":", name.Name);
            Assert.True(name.Name.Length <= VariableResolver.MaxFileNameValueLength + 4);
            Assert.Equal("_.png", _resolver.RenderFileName("${payload.title}", ".png", Scope("..")).Name.Substring(1));
        }

        [Fact]
        public void FileName_FixedSubFolders_AreKept()
        {
            var name = _resolver.RenderFileName("${payload.time:yyyy}\\${payload.title}", ".png", Scope());
            Assert.Equal(Path.Combine(_folder, "2026", "Window.png"), name.FullPath);
        }

        [Fact]
        public void FileName_UsesTheCurrentCultureOfThisThread()
        {
            var old = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                Assert.Equal("1,5", _resolver.RenderFileName("${1.5}", null, Scope()).Name);
                Assert.Equal("1.5", _resolver.Render("${1.5}", TemplatePurpose.Value, Scope()));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = old;
            }
        }

        [Fact]
        public void GlobalCounter_PreviewKeepsIt_CommitStoresIt()
        {
            _global = 41;
            Assert.Equal("42", _resolver.Render("${counter.global}", TemplatePurpose.FileName, Scope()));
            Assert.Equal(41, _global);

            var name = _resolver.RenderFileName("${counter.global}-${counter.global}", ".png", Scope());
            Assert.Equal("42-42.png", name.Name);
            Assert.Equal(41, _global);
            name.Commit();
            name.Commit();
            Assert.Equal(42, _global);
        }

        [Fact]
        public void FolderCounter_CountsInTheDayFolder()
        {
            const string template = "${payload.time:yyyy-MM-dd}\\${counter.folder:000} - ${payload.title}";
            Assert.Equal("001 - Window.png", Path.GetFileName(_resolver.RenderFileName(template, ".png", Scope()).FullPath));
            Touch("2026-11-06\\001 - Window.png");
            Touch("2026-11-06\\004 - Other.png");
            Touch("2026-11-06\\notes.txt");
            Touch("2026-11-05\\009 - Window.png");
            Assert.Equal("005 - Window.png", Path.GetFileName(_resolver.RenderFileName(template, ".png", Scope()).FullPath));
            // Another extension doesn't count
            Assert.Equal("001 - Window.jpg", Path.GetFileName(_resolver.RenderFileName(template, ".jpg", Scope()).FullPath));
        }

        [Fact]
        public void FolderCounter_FixedTextAfterTheTitle()
        {
            const string template = "${payload.title} ${counter.folder:000} REPORT";
            Touch("Window 002 REPORT.png");
            Touch("Other 007 REPORT.png");
            Touch("Other 099 DRAFT.png");
            Assert.Equal("Window 008 REPORT.png", _resolver.RenderFileName(template, ".png", Scope()).Name);
        }

        [Fact]
        public void NameCounter_CountsPerName()
        {
            const string template = "${payload.title}_${counter.name:000}";
            Touch("Window_003.png");
            Touch("Other_007.png");
            Assert.Equal("Window_004.png", _resolver.RenderFileName(template, ".png", Scope()).Name);
            Assert.Equal("Other_008.png", _resolver.RenderFileName(template, ".png", Scope("Other")).Name);
            Assert.Equal("New_001.png", _resolver.RenderFileName(template, ".png", Scope("New")).Name);
        }

        [Fact]
        public void ScopeCounter_IsStoredPerKey()
        {
            const string template = "${payload.title}-${counter.scope(payload.title):00}";
            var first = _resolver.RenderFileName(template, ".png", Scope());
            Assert.Equal("Window-01.png", first.Name);
            Assert.Equal("Window-01.png", _resolver.RenderFileName(template, ".png", Scope()).Name);
            first.Commit();
            Assert.Equal("Window-02.png", _resolver.RenderFileName(template, ".png", Scope()).Name);
            Assert.Equal("Other-01.png", _resolver.RenderFileName(template, ".png", Scope("Other")).Name);

            // Another resolver reads the same file
            var again = new VariableResolver(() => 0, v => { }, () => Path.Combine(_folder, "counters.txt"));
            Assert.Equal("Window-02.png", again.RenderFileName(template, ".png", Scope()).Name);
        }

        [Fact]
        public void CounterStore_RemovesKeysUnusedFor90Days()
        {
            string path = Path.Combine(_folder, "old-counters.txt");
            long old = (DateTime.UtcNow - TimeSpan.FromDays(100)).Ticks;
            File.WriteAllText(path, $"old\t5\t{old}\r\nbroken line\r\n");
            var store = new CounterStore(() => path);
            Assert.Equal(5, store.Peek("old"));
            store.Commit("new", 3);
            Assert.Equal(0, store.Peek("old"));
            Assert.Equal(3, store.Peek("new"));
            store.Commit("new", 1);
            Assert.Equal(3, store.Peek("new"));
        }

        [Fact]
        public void FileName_CantLeaveTheTargetFolder()
        {
            Assert.Throws<TemplateException>(() => _resolver.RenderFileName("..\\${payload.title}", ".png", Scope()));
        }

        [Fact]
        public void Describe_LeavesOutWhatThePurposeCantUse()
        {
            var folder = _resolver.Describe(TemplatePurpose.Folder).Select(d => d.Name).ToList();
            Assert.Contains("user.pictures", folder);
            Assert.DoesNotContain("payload.title", folder);
            Assert.DoesNotContain("counter.global", folder);
            Assert.Contains("counter.global", _resolver.Describe(TemplatePurpose.FileName).Select(d => d.Name));
        }

        [Fact]
        public void Providers_CanBeAddedByPlugins()
        {
            var provider = new VariableProvider("test");
            provider.Add<string>("value", VariableTrust.Trusted, "A test value", s => "hello");
            provider.AddWildcard<string>("echo", VariableTrust.Untrusted, "Echo", (rest, s) => rest);
            _resolver.Register(provider);
            Assert.Equal("hello abc", Text("${test.value} ${test.echo.abc}"));
            Assert.Throws<InvalidOperationException>(() => _resolver.Register(new VariableProvider("test")));
            Assert.Contains(_resolver.Validate("${test.echo.x}", TemplatePurpose.Folder), e => !e.IsWarning);
        }

        [Fact]
        public void Environment_Values()
        {
            Environment.SetEnvironmentVariable("GREENSHOT_EXPRESSION_TEST", "set");
            Assert.Equal("set", Text("${env.GREENSHOT_EXPRESSION_TEST}"));
            Assert.Equal(string.Empty, Text("${env.GREENSHOT_EXPRESSION_NOT_SET}"));
        }
    }
}
