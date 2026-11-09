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
using System.Globalization;
using System.Linq;
using Greenshot.Plugin.SmartText;
using Greenshot.Plugin.SmartText.Detection;
using Xunit;

namespace Greenshot.Tests.SmartText
{
    /// <summary>
    /// The built-in detectors of Smart text: what they find, what they leave alone, and the values they give
    /// </summary>
    public class BuiltInDetectorTests
    {
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
        private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

        public BuiltInDetectorTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        internal static IReadOnlyList<SmartTextItem> Scan(string text, string id, CultureInfo culture = null)
        {
            culture ??= English;
            var builtIn = BuiltInDetectors.Find(BuiltInDetectors.Create(culture), id);
            var detector = new SmartTextDetector(builtIn.Id, builtIn.Id, builtIn.Pattern, false, culture);
            return SmartTextScanner.Scan(TextLayout.FromText(text), new[] { detector });
        }

        private static string[] Values(string text, string id, CultureInfo culture = null) => Scan(text, id, culture).Select(item => item.Value).ToArray();

        [Theory]
        [InlineData("See https://getgreenshot.org/downloads/ for more.", "https://getgreenshot.org/downloads/")]
        [InlineData("Go to www.example.com.", "https://www.example.com")]
        [InlineData("(see https://example.com/a_(b))", "https://example.com/a_(b)")]
        public void Url_IsFound(string text, string expected)
        {
            Assert.Equal(new[] { expected }, Values(text, BuiltInDetectors.Url));
        }

        [Fact]
        public void LinkTargets_GiveTheRealAddress_ForTheVisibleText()
        {
            // Text read from the application: "Download" links to the downloads, "Write us" to an e-mail address
            Greenshot.Base.Interfaces.Plugin.OcrWordInfo Word(string text, int x, string target) =>
                new(text, new Dapplo.Windows.Common.Structs.NativeRect(x, 0, text.Length * 10, 20)) { LinkTarget = target };
            var words = new List<Greenshot.Base.Interfaces.Plugin.OcrWordInfo>
            {
                Word("Download", 0, "https://getgreenshot.org/downloads/"), Word("or", 90, null),
                Word("Write", 120, "mailto:getgreenshot@gmail.com?subject=Hi"), Word("us", 180, "mailto:getgreenshot@gmail.com?subject=Hi")
            };
            var line = new Greenshot.Base.Core.DetectedOcrLine(Dapplo.Windows.Common.Structs.NativeRect.Empty, "Download or Write us", words)
            {
                Source = Greenshot.Base.Interfaces.Plugin.TextSource.UiAutomation
            };
            var detectors = new[] { BuiltInDetectors.Url, BuiltInDetectors.Email }.Select(id =>
            {
                var builtIn = BuiltInDetectors.Find(BuiltInDetectors.Create(English), id);
                return new SmartTextDetector(builtIn.Id, builtIn.Id, builtIn.Pattern, false, English);
            }).ToList();

            var items = SmartTextScanner.Scan(TextLayout.From(new[] { line }, Dapplo.Windows.Common.Structs.NativeRect.Empty), detectors);

            Assert.Equal(new[] { "https://getgreenshot.org/downloads/", "getgreenshot@gmail.com" }, items.Select(item => item.Value));
            Assert.Equal(new[] { "Download", "Write us" }, items.Select(item => item.ReadText));
            Assert.All(items, item => Assert.True(item.IsValidated));
            Assert.Equal("mailto:getgreenshot@gmail.com", items[1].ActionTarget);
        }

        [Fact]
        public void RotatedText_ItemIsOnTheTurnedWords()
        {
            // "Visit https://getgreenshot.org now" at 20 degrees: the item covers only the address, turned with the text
            var center = new System.Windows.Point(0, 0);
            Greenshot.Base.Interfaces.Plugin.OcrWordInfo Word(string text, double x, double width) =>
                new(text, Greenshot.Base.Interfaces.Plugin.OcrBox.FromRect(new System.Windows.Rect(x, 100, width, 20), 20, center));
            var words = new List<Greenshot.Base.Interfaces.Plugin.OcrWordInfo> { Word("Visit", 0, 50), Word("https://getgreenshot.org", 60, 240), Word("now", 310, 40) };
            var line = new Greenshot.Base.Core.DetectedOcrLine(default(Greenshot.Base.Interfaces.Plugin.OcrBox), "Visit https://getgreenshot.org now", words);
            var builtIn = BuiltInDetectors.Find(BuiltInDetectors.Create(English), BuiltInDetectors.Url);
            var detector = new SmartTextDetector(builtIn.Id, builtIn.Id, builtIn.Pattern, false, English);

            var item = Assert.Single(SmartTextScanner.Scan(TextLayout.From(new[] { line }, Dapplo.Windows.Common.Structs.NativeRect.Empty), new[] { detector }));

            Assert.Equal(20, item.Box.Angle, 6);
            Assert.Equal(240, item.Box.Width, 6);
            Assert.Equal(words[1].Box.Center, item.Box.Center);
        }

        [Theory]
        [InlineData("Write to info@example.com")]
        [InlineData("version 1.4.2 of example")]
        [InlineData("ftp://example.com/file")]
        [InlineData("Open readme.txt and setup.exe")]
        [InlineData("Visit getgreenshot.org.")]
        [InlineData("example.org/path?q=1")]
        public void Url_NotFound(string text)
        {
            Assert.Empty(Values(text, BuiltInDetectors.Url));
        }

        [Fact]
        public void Url_BrokenAtTheEndOfAFullLine_GoesOnInTheNextLine()
        {
            Assert.Equal(new[] { "https://example.com/very/long/path/page.html" },
                Values("Read https://example.com/very/long/\npath/page.html now", BuiltInDetectors.Url));
        }

        [Fact]
        public void Url_InAListOfLinks_DoesntTakeTheNextBullet()
        {
            // The imprint of getgreenshot.org: every link ends with /, the next line starts with a bullet
            const string text = "• our GitHub page at https://github.com/greenshot/greenshot/\n" +
                                "• our SourceForge page at https://sourceforge.net/projects/greenshot/\n" +
                                "• our Flattr page at https://flattr.com/thing/30384/";
            Assert.Equal(new[] { "https://github.com/greenshot/greenshot/", "https://sourceforge.net/projects/greenshot/", "https://flattr.com/thing/30384/" },
                Values(text, BuiltInDetectors.Url));
        }

        [Fact]
        public void Url_AtTheEndOfAShortLine_DoesntTakeTheNextWord()
        {
            Assert.Equal(new[] { "https://example.com/" }, Values("see https://example.com/\nand then read the rest of this longer line", BuiltInDetectors.Url));
        }

        [Fact]
        public void Url_HasOpenAction()
        {
            var item = Assert.Single(Scan("https://example.com/x", BuiltInDetectors.Url));
            Assert.Equal(SmartTextAction.Open, item.Action);
            Assert.True(SmartTextActions.CanOpen(item.ActionTarget));
        }

        [Theory]
        [InlineData("Mail: Info.Team@Example.COM.", "Info.Team@example.com")]
        [InlineData("<a.b+c@sub.example.de>", "a.b+c@sub.example.de")]
        public void Email_IsFound(string text, string expected)
        {
            var item = Assert.Single(Scan(text, BuiltInDetectors.Email));
            Assert.Equal(expected, item.Value);
            Assert.Equal("mailto:" + expected, item.ActionTarget);
        }

        [Theory]
        [InlineData("Call +49 30 1234567 now", "+49301234567")]
        [InlineData("Tel. 0049 (0)30 123 45-67", "+49301234567")]
        [InlineData("(030) 1234567", "0301234567")]
        [InlineData("555-123-4567", "5551234567")]
        public void Phone_IsFound(string text, string expected)
        {
            var item = Assert.Single(Scan(text, BuiltInDetectors.Phone));
            Assert.Equal(expected, item.Value);
            Assert.Equal("tel:" + expected, item.ActionTarget);
        }

        [Theory]
        [InlineData("IP 192.168.100.200")]
        [InlineData("Version 1.4.465.1234")]
        [InlineData("on 2026-11-09")]
        [InlineData("Order 12345")]
        [InlineData("1234567890123456789")]
        public void Phone_NotFound(string text)
        {
            Assert.Empty(Values(text, BuiltInDetectors.Phone));
        }

        [Theory]
        [InlineData("IBAN DE89 3704 0044 0532 0130 00", "DE89370400440532013000")]
        [InlineData("GB82WEST12345698765432", "GB82WEST12345698765432")]
        public void Iban_IsFound(string text, string expected)
        {
            var item = Assert.Single(Scan(text, BuiltInDetectors.Iban));
            Assert.Equal(expected, item.Value);
            Assert.True(item.IsValidated);
        }

        [Fact]
        public void Iban_WrongCheckDigits_NotFound()
        {
            Assert.Empty(Values("DE88 3704 0044 0532 0130 00", BuiltInDetectors.Iban));
        }

        [Theory]
        [InlineData("on 2026-11-09", "2026-11-09")]
        [InlineData("am 9.11.2026", "2026-11-09")]
        [InlineData("November 9, 2026", "2026-11-09")]
        [InlineData("11/9/2026", "2026-11-09")]
        public void Date_English(string text, string expected)
        {
            Assert.Equal(new[] { expected }, Values(text, BuiltInDetectors.Date, English));
        }

        [Theory]
        [InlineData("am 9. November 2026", "2026-11-09")]
        [InlineData("9/11/2026", "2026-11-09")]
        public void Date_German(string text, string expected)
        {
            Assert.Equal(new[] { expected }, Values(text, BuiltInDetectors.Date, German));
        }

        [Fact]
        public void Date_NotADay_NotFound()
        {
            Assert.Empty(Values("2026-13-45", BuiltInDetectors.Date));
        }

        [Theory]
        [InlineData("Fixed in GSII-26.", "GSII-26")]
        [InlineData("see BUG-3291 and", "BUG-3291")]
        public void IssueKey_IsFound(string text, string expected)
        {
            Assert.Equal(new[] { expected }, Values(text, BuiltInDetectors.IssueKey));
        }

        [Theory]
        [InlineData("UTF-8")]
        [InlineData("SHA-256")]
        [InlineData("gsii-26")]
        public void IssueKey_NotFound(string text)
        {
            Assert.Empty(Values(text, BuiltInDetectors.IssueKey));
        }

        [Theory]
        [InlineData("host 10.0.0.1 is up", BuiltInDetectors.IpAddress, "10.0.0.1")]
        [InlineData("mac 00:1a:2b:3c:4d:5e", BuiltInDetectors.MacAddress, "00:1A:2B:3C:4D:5E")]
        [InlineData("id 3F2504E0-4F89-11D3-9A0C-0305E82C3301", BuiltInDetectors.Guid, "3f2504e0-4f89-11d3-9a0c-0305e82c3301")]
        [InlineData("commit 42cc3fcf0", BuiltInDetectors.Hash, "42cc3fcf0")]
        [InlineData("failed with 0x80070005", BuiltInDetectors.ErrorCode, "0x80070005")]
        [InlineData(@"saved to C:\Users\me\Pictures\a.png", BuiltInDetectors.Path, @"C:\Users\me\Pictures\a.png")]
        [InlineData("Total: 1.234,56 €", BuiltInDetectors.Money, "1.234,56 €")]
        [InlineData("ISBN 978-3-16-148410-0", BuiltInDetectors.Isbn, "9783161484100")]
        [InlineData("EAN 4006381333931", BuiltInDetectors.Ean, "4006381333931")]
        [InlineData("card 4111 1111 1111 1111", BuiltInDetectors.CardNumber, "4111111111111111")]
        [InlineData("at 52.520008, 13.404954", BuiltInDetectors.Coordinates, "52.520008, 13.404954")]
        [InlineData("BIC DEUTDEFF500", BuiltInDetectors.Bic, "DEUTDEFF500")]
        public void OtherDetectors_AreFound(string text, string id, string expected)
        {
            Assert.Equal(new[] { expected }, Values(text, id));
        }

        [Theory]
        [InlineData("999.1.1.1", BuiltInDetectors.IpAddress)]
        [InlineData("card 4111 1111 1111 1112", BuiltInDetectors.CardNumber)]
        [InlineData("EAN 4006381333932", BuiltInDetectors.Ean)]
        [InlineData("ISBN 978-3-16-148410-1", BuiltInDetectors.Isbn)]
        [InlineData("at 95.1234, 13.4049", BuiltInDetectors.Coordinates)]
        [InlineData("ABCDXXFF", BuiltInDetectors.Bic)]
        public void OtherDetectors_WrongChecks_NotFound(string text, string id)
        {
            Assert.Empty(Values(text, id));
        }

        [Fact]
        public void EveryBuiltIn_HasAName()
        {
            foreach (var builtIn in BuiltInDetectors.Create())
            {
                string name = SmartTextDefinitions.NameOf(builtIn.Id);
                Assert.False(string.IsNullOrWhiteSpace(name));
                Assert.DoesNotContain("###", name);
            }
        }

        [Fact]
        public void DefaultsOn_AreTheRecommendedOnes()
        {
            var on = BuiltInDetectors.Create().Where(builtIn => builtIn.IsOnByDefault).Select(builtIn => builtIn.Id);
            Assert.Equal(new[] { BuiltInDetectors.Url, BuiltInDetectors.Email, BuiltInDetectors.Phone, BuiltInDetectors.Iban, BuiltInDetectors.Date, BuiltInDetectors.IssueKey }, on);
        }
    }
}
