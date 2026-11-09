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

using System.Collections.Generic;
using System.Linq;
using Greenshot.Base.Definitions;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.SmartText;
using Greenshot.Plugin.SmartText.Detection;
using Greenshot.Plugin.SmartText.Recipes;
using Greenshot.Plugin.SmartText.ViewModels;
using Xunit;

namespace Greenshot.Tests.SmartText
{
    /// <summary>
    /// Own patterns: labels, look-alike digits, areas, overlaps, the file format and the checks
    /// </summary>
    public class SmartTextPatternTests
    {
        public SmartTextPatternTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static IReadOnlyList<SmartTextItem> Scan(string text, params SmartTextPattern[] patterns) =>
            SmartTextScanner.Scan(TextLayout.FromText(text), patterns.Select(pattern => new SmartTextDetector(pattern.Name, pattern.Name, pattern, true)));

        private static SmartTextPattern CustomerNumber(string labelPosition = null) => new SmartTextPattern
        {
            Name = "Customer number",
            Pattern = @"\b\d{4}-?\d{4}\b",
            Label = @"Kunden(nummer|-?nr\.?)",
            LabelPosition = labelPosition,
            DigitLookAlikes = true,
            Normalize = SmartTextNormalize.RemoveSeparators
        };

        [Fact]
        public void Label_Right_TakesTheNumberNextToIt()
        {
            var items = Scan("Auftrag 1111-2222\nKundennummer: 4711-0815\nTelefon 12345678", CustomerNumber());
            Assert.Equal(new[] { "47110815" }, items.Select(item => item.Value));
        }

        [Fact]
        public void Label_Below_TakesTheNumberUnderIt()
        {
            var items = Scan("Kunden-Nr.\n4711-0815\n\n\n\n\n\n9999-8888", CustomerNumber(SmartTextLabelPosition.Below));
            Assert.Equal(new[] { "47110815" }, items.Select(item => item.Value));
        }

        [Fact]
        public void Label_Missing_FindsNothing()
        {
            Assert.Empty(Scan("Auftrag 4711-0815", CustomerNumber()));
        }

        [Fact]
        public void DigitLookAlikes_AreRead()
        {
            var item = Assert.Single(Scan("Kundennummer: 47l1-O8I5", CustomerNumber()));
            Assert.Equal("47110815", item.Value);
            Assert.Equal("47l1-O8I5", item.ReadText);
        }

        [Fact]
        public void DigitLookAlikes_OnlyInNumbers()
        {
            Assert.Equal("Hello", SmartTextDetector.FixDigitLookAlikes("Hello"));
            Assert.Equal("4711", SmartTextDetector.FixDigitLookAlikes("47l1"));
        }

        [Fact]
        public void ValueGroup_IsTheValue()
        {
            var pattern = new SmartTextPattern { Name = "Order", Pattern = @"Order #(?<id>\d+)", ValueGroup = "id" };
            Assert.Equal(new[] { "1234" }, Scan("Order #1234 shipped", pattern).Select(item => item.Value));
        }

        [Fact]
        public void Area_OnlyFindsInside()
        {
            // FromText: every line 30 pixels high; the second half of the capture holds the last two lines
            var pattern = new SmartTextPattern { Name = "Number", Pattern = @"\d{4}", Area = new SmartTextArea { Top = 0.5, Height = 0.5 } };
            Assert.Equal(new[] { "3333", "4444" }, Scan("1111\n2222\n3333\n4444", pattern).Select(item => item.Value));
        }

        [Fact]
        public void Overlap_OwnPatternWins()
        {
            var own = new SmartTextPattern { Name = "Ticket", Pattern = @"[A-Z]+-\d+" };
            var builtIn = BuiltInDetectors.Find(BuiltInDetectors.Create(), BuiltInDetectors.IssueKey);
            var detectors = new[]
            {
                new SmartTextDetector(builtIn.Id, builtIn.Id, builtIn.Pattern, false),
                new SmartTextDetector(own.Name, own.Name, own, true)
            };
            var item = Assert.Single(SmartTextScanner.Scan(TextLayout.FromText("see GSII-26"), detectors));
            Assert.Equal("Ticket", item.DetectorId);
        }

        [Fact]
        public void SameValueTwice_IsOneItem()
        {
            var pattern = new SmartTextPattern { Name = "Number", Pattern = @"\d{4}" };
            Assert.Single(Scan("1234 and again 1234", pattern));
        }

        [Fact]
        public void Url_BrokenOverTwoLines_IsJoined()
        {
            var builtIn = BuiltInDetectors.Find(BuiltInDetectors.Create(), BuiltInDetectors.Url);
            var detector = new SmartTextDetector(builtIn.Id, builtIn.Id, builtIn.Pattern, false);
            var item = Assert.Single(SmartTextScanner.Scan(TextLayout.FromText("https://example.com/some/long/\npath/page.html"), new[] { detector }));
            Assert.Equal("https://example.com/some/long/path/page.html", item.Value);
        }

        [Theory]
        [InlineData("https://crm.example.com/customer?id={value}", null)]
        [InlineData("https://{value}.example.com/", "server")]
        [InlineData("https://crm.example.com/customer", "{value}")]
        [InlineData("file:///c:/{value}", "http")]
        public void OpenUrl_IsChecked(string openUrl, string problemPart)
        {
            string problem = SmartTextPatternCheck.OpenUrlProblem(openUrl);
            if (problemPart == null)
            {
                Assert.Null(problem);
            }
            else
            {
                Assert.NotNull(problem);
                Assert.Contains(problemPart, problem, System.StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void OpenUrl_EncodesTheValue()
        {
            Assert.Equal("https://crm.example.com/c?id=a%20b%2Fc%26d", SmartTextPatternCheck.RenderOpenUrl("https://crm.example.com/c?id={value}", "a b/c&d"));
        }

        [Fact]
        public void OpenUrl_NotInARecipe()
        {
            var pattern = new SmartTextPattern { Name = "x", Pattern = "x", OpenUrl = "https://example.com/?q={value}" };
            Assert.Empty(SmartTextPatternCheck.Problems(pattern));
            Assert.NotEmpty(SmartTextPatternCheck.Problems(pattern, allowOpenUrl: false));
        }

        [Theory]
        [InlineData("(")]
        [InlineData("a{2,1}")]
        public void BadRegex_IsAProblem(string regex)
        {
            Assert.NotEmpty(SmartTextPatternCheck.Problems(new SmartTextPattern { Name = "x", Pattern = regex }));
        }

        [Fact]
        public void SlowRegex_TimesOutInsteadOfHanging()
        {
            var pattern = new SmartTextPattern { Name = "Slow", Pattern = @"^(a+)+$" };
            var items = Scan(new string('a', 40) + "!", pattern);
            Assert.Empty(items);
        }

        [Fact]
        public void File_RoundTrips()
        {
            var pattern = CustomerNumber(SmartTextLabelPosition.Right);
            pattern.Area = new SmartTextArea { Left = 0.5, Width = 0.5 };
            string json = SmartTextKind.Instance.Write(pattern);
            Assert.Contains("\"kind\": \"SmartText\"", json);
            Assert.Contains("\"digitLookAlikes\": true", json);
            var read = SmartTextKind.Parse(json);
            Assert.Equal(pattern.Pattern, read.Pattern);
            Assert.Equal(pattern.Label, read.Label);
            Assert.Equal(0.5, read.Area.Left);
        }

        [Theory]
        [InlineData("{\"kind\": \"Command\", \"name\": \"x\", \"pattern\": \"x\"}")]
        [InlineData("{\"kind\": \"SmartText\", \"version\": 99, \"name\": \"x\", \"pattern\": \"x\"}")]
        [InlineData("{\"kind\": \"SmartText\", \"name\": \"x\", \"pattern\": \"(\"}")]
        [InlineData("not json")]
        public void File_Wrong_IsRefused(string json)
        {
            Assert.Throws<DefinitionFileException>(() => SmartTextKind.Parse(json));
        }

        [Fact]
        public void RecipeValue_IsReadStrictlyAsInTheFile()
        {
            RecipeParameterTypes.AddJsonTypes(SmartTextStep.JsonTypes);
            var value = new List<object>
            {
                new Dictionary<string, object> { ["name"] = "Customer number", ["pattern"] = @"\d{8}", ["digitLookAlikes"] = true }
            };
            Assert.True(RecipeParameterTypes.TryRead(value, typeof(List<SmartTextPattern>), out var typed, out string error), error);
            var pattern = Assert.Single((List<SmartTextPattern>)typed);
            Assert.True(pattern.DigitLookAlikes);

            var unknown = new List<object> { new Dictionary<string, object> { ["name"] = "x", ["pattern"] = "x", ["id"] = "x" } };
            Assert.False(RecipeParameterTypes.TryRead(unknown, typeof(List<SmartTextPattern>), out _, out _));
        }

        [Theory]
        [InlineData("0.5; 0; 0.5; 1", 0.5, 0, 0.5, 1)]
        [InlineData("0.5, 0.25, 0.5, 0.5", 0.5, 0.25, 0.5, 0.5)]
        public void Editor_ReadsTheArea(string text, double left, double top, double width, double height)
        {
            using var culture = new CultureScope("en-US");
            var area = SmartTextPatternViewModel.ParseArea(text);
            Assert.Equal(new[] { left, top, width, height }, new[] { area.Left, area.Top, area.Width, area.Height });
        }

        [Theory]
        [InlineData("1; 2; 3")]
        [InlineData("0; 0; 2; 1")]
        [InlineData("0; 0; 0; 1")]
        public void Editor_WrongArea_IsNull(string text)
        {
            using var culture = new CultureScope("en-US");
            Assert.Null(SmartTextPatternViewModel.ParseArea(text));
        }

        [Fact]
        public void Editor_TestText_ShowsWhatIsFound()
        {
            var viewModel = new SmartTextPatternViewModel(CustomerNumber(), null) { TestText = "Kundennummer 4711-0815" };
            Assert.False(viewModel.HasProblems, viewModel.Problems);
            Assert.Contains("47110815", viewModel.TestResult);
        }

        /// <summary>The current culture for a test</summary>
        private sealed class CultureScope : System.IDisposable
        {
            private readonly System.Globalization.CultureInfo _previous = System.Globalization.CultureInfo.CurrentCulture;

            public CultureScope(string name) => System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(name);

            public void Dispose() => System.Globalization.CultureInfo.CurrentCulture = _previous;
        }
    }
}
