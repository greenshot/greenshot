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
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Win10;
using Xunit;

namespace Greenshot.Tests.Ocr
{
    /// <summary>
    /// The chosen OCR engine, and what happens when it can't be used anymore
    /// </summary>
    public class OcrEngineSelectorTests
    {
        private sealed class FakeEngine : IOcrEngine
        {
            public FakeEngine(string id, bool ready)
            {
                Id = id;
                IsReady = ready;
            }

            public string Id { get; }
            public string DisplayName => Id;
            public bool IsReady { get; set; }
            public bool BreaksWhileReading { get; set; }
            public bool AnswerOfTheQuestion { get; set; }
            public List<bool> Questions { get; } = new();
            public int Reads { get; private set; }

            public Task<bool> PrepareAsync(IntPtr owner, bool isChosen, CancellationToken cancellationToken)
            {
                Questions.Add(isChosen);
                if (AnswerOfTheQuestion) IsReady = true;
                return Task.FromResult(AnswerOfTheQuestion);
            }

            public Task<List<IOcrLineFeature>> DoOcrAsync(Image image, string languageTag = null)
            {
                Reads++;
                if (BreaksWhileReading)
                {
                    IsReady = false;
                    return Task.FromResult(new List<IOcrLineFeature>());
                }
                return Task.FromResult(new List<IOcrLineFeature> { new Greenshot.Base.Core.DetectedOcrLine(Dapplo.Windows.Common.Structs.NativeRect.Empty, Id, new List<OcrWordInfo>()) });
            }

            public Task<List<IOcrLineFeature>> DoOcrAsync(ISurface surface, string languageTag = null) => DoOcrAsync((Image)null, languageTag);

            public IList<OcrLanguage> GetAvailableLanguages() => new List<OcrLanguage>();
        }

        private static (OcrEngineSelector Selector, Func<string> Configured) Create(FakeEngine windows, FakeEngine other, string configured)
        {
            string id = configured;
            var selector = new OcrEngineSelector(() => new IOcrEngine[] { windows, other }, () => id, () => InlineUiDispatcher.Instance, () => id = Win10OcrProvider.EngineId);
            return (selector, () => id);
        }

        [Fact]
        public async Task ChosenAndReady_ReadsWithoutAsking()
        {
            var windows = new FakeEngine(Win10OcrProvider.EngineId, true);
            var other = new FakeEngine("Other", true);
            var (selector, _) = Create(windows, other, "Other");

            var lines = await selector.DoOcrAsync((Image)null);

            Assert.Equal("Other", Assert.Single(lines).Text);
            Assert.Empty(other.Questions);
        }

        [Fact]
        public async Task ChosenFilesMissing_WindowsReads_AndDecliningChoosesWindows()
        {
            var windows = new FakeEngine(Win10OcrProvider.EngineId, true);
            var other = new FakeEngine("Other", false);
            var (selector, configured) = Create(windows, other, "Other");

            var lines = await selector.DoOcrAsync((Image)null);
            await selector.Repairing;

            Assert.Equal(Win10OcrProvider.EngineId, Assert.Single(lines).Text);
            Assert.Equal(new[] { true }, other.Questions);
            Assert.Equal(Win10OcrProvider.EngineId, configured());
        }

        [Fact]
        public async Task ChosenFilesMissing_DownloadingAgain_KeepsTheChoice()
        {
            var windows = new FakeEngine(Win10OcrProvider.EngineId, true);
            var other = new FakeEngine("Other", false) { AnswerOfTheQuestion = true };
            var (selector, configured) = Create(windows, other, "Other");

            await selector.DoOcrAsync((Image)null);
            await selector.Repairing;
            var lines = await selector.DoOcrAsync((Image)null);

            Assert.Equal("Other", configured());
            Assert.Equal("Other", Assert.Single(lines).Text);
        }

        [Fact]
        public async Task ChosenFindsItsFilesChanged_WindowsReadsThisCapture_AndAsks()
        {
            var windows = new FakeEngine(Win10OcrProvider.EngineId, true);
            var other = new FakeEngine("Other", true) { BreaksWhileReading = true };
            var (selector, configured) = Create(windows, other, "Other");

            var lines = await selector.DoOcrAsync((Image)null);
            await selector.Repairing;

            Assert.Equal(Win10OcrProvider.EngineId, Assert.Single(lines).Text);
            Assert.Equal(new[] { true }, other.Questions);
            Assert.Equal(Win10OcrProvider.EngineId, configured());
        }

        [Fact]
        public async Task WindowsChosen_NeverAsks()
        {
            var windows = new FakeEngine(Win10OcrProvider.EngineId, true);
            var other = new FakeEngine("Other", false);
            var (selector, _) = Create(windows, other, Win10OcrProvider.EngineId);

            await selector.DoOcrAsync((Image)null);

            Assert.Empty(other.Questions);
        }
    }
}
