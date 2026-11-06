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
using Greenshot.Base.Expressions;
using Xunit;

namespace Greenshot.Tests.Expressions
{
    public class LegacyTemplateConverterTests
    {
        private static readonly DateTime Time = new DateTime(2026, 3, 7, 8, 5, 9);

        public LegacyTemplateConverterTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        // The defaults of 1.3
        [InlineData("${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}-${title}", "${payload.time:yyyy-MM-dd HH_mm_ss}-${payload.title}")]
        [InlineData("${capturetime:d\"D\"} ${capturetime:d\"T\"} - ${title}", "${payload.time:D} ${payload.time:T} - ${payload.title}")]
        [InlineData("${title}", "${payload.title}")]
        // Other names and options
        [InlineData("${capturetime}", "${payload.time:yyyy-MM-dd HH-mm-ss}")]
        [InlineData("${YYYY}-${MM}-${DD} ${hh}${mm}${ss}", "${payload.time:yyyy}-${payload.time:MM}-${payload.time:dd} ${payload.time:HH}${payload.time:mm}${payload.time:ss}")]
        [InlineData("${NUM}", "${counter.global:000000}")]
        [InlineData("${NUM:p3,0}", "${padRight(counter.global, 3, '0')}")]
        [InlineData("${NUM:p-3,0}", "${padLeft(counter.global, 3, '0')}")]
        [InlineData("${user}@${hostname}", "${user.name}@${machine.name}")]
        [InlineData("${MyPictures}\\Greenshots", "${user.pictures}\\Greenshots")]
        [InlineData("${RRRR}", "${random(4)}")]
        [InlineData("${title:s0,10}", "${substring(payload.title, 0, 10)}")]
        [InlineData("${title:r ,_}", "${replace(payload.title, ' ', '_')}")]
        [InlineData("${capturetime:d\"HH:mm\"}", "${payload.time:HH:mm}")]
        [InlineData("${capturetime:d\" yyyy\"}", "${payload.time:' yyyy'}")]
        [InlineData("${USERNAME}", "${env.USERNAME}")]
        [InlineData("no variables", "no variables")]
        public void Convert_FileNamePatterns(string pattern, string expected)
        {
            Assert.Equal(expected, LegacyTemplateConverter.Convert(pattern, false));
            Assert.DoesNotContain(VariableResolver.Instance.Validate(expected, TemplatePurpose.FileName), e => !e.IsWarning);
        }

        [Theory]
        [InlineData("%USERPROFILE%\\Pictures", "${env.USERPROFILE}\\Pictures")]
        [InlineData("%ProgramFiles(x86)%\\x", "${env.ProgramFiles_x86_}\\x")]
        [InlineData("100% sure", "100% sure")]
        public void Convert_PercentVariables(string pattern, string expected)
        {
            Assert.Equal(expected, LegacyTemplateConverter.Convert(pattern, true));
        }

        [Fact]
        public void Convert_PercentStaysInFileNames()
        {
            Assert.Equal("%USERNAME%", LegacyTemplateConverter.Convert("%USERNAME%", false));
        }

        [Fact]
        public void Converted_DefaultFileName_GivesTheSameName()
        {
            string template = LegacyTemplateConverter.Convert("${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}-${title}", false);
            var scope = new VariableScope
            {
                Values = new Dictionary<string, object> { ["payload.title"] = "Some: Window", ["payload.time"] = Time }
            };
            Assert.Equal("2026-03-07 08_05_09-Some_ Window", VariableResolver.Instance.Render(template, TemplatePurpose.FileName, scope));
        }

        [Fact]
        public void Converted_PaddedTimePart_KeepsItsPadding()
        {
            string template = LegacyTemplateConverter.Convert("${MM:p-3,x}", false);
            var scope = new VariableScope { Values = new Dictionary<string, object> { ["payload.time"] = Time } };
            Assert.Equal("xx3", VariableResolver.Instance.Render(template, TemplatePurpose.Text, scope));
        }

        [Theory]
        [InlineData("${capturetime}", false, true)]
        [InlineData("${payload.time:yyyy}", false, false)]
        [InlineData("%TEMP%\\x", true, true)]
        [InlineData("%TEMP%\\x", false, false)]
        [InlineData("plain", true, false)]
        public void NeedsConversion(string pattern, bool percent, bool expected)
        {
            Assert.Equal(expected, LegacyTemplateConverter.NeedsConversion(pattern, percent));
        }
    }
}
