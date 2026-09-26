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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Helpers.Ipc;
using Greenshot.Recipes;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    public class IpcSecurityDispatcherTests
    {
        [Fact]
        public async Task SecurityDispatcher_UnwhitelistedCommand_IsRejected()
        {
            var envelope = new IpcEnvelope
            {
                Command = "EXECUTE_ARBITRARY_CODE",
                RawInput = "calc.exe"
            };

            using (var ms = new MemoryStream())
            {
                var context = new IpcRequestContext(envelope, ms);

                bool exitCalled = false;
                bool reloadCalled = false;
                bool openFileCalled = false;

                await IpcSecurityDispatcher.DispatchAsync(
                    context,
                    null,
                    () => exitCalled = true,
                    () => reloadCalled = true,
                    () => { },
                    path => openFileCalled = true);

                Assert.False(exitCalled);
                Assert.False(reloadCalled);
                Assert.False(openFileCalled);

                // Security dispatcher replies with structured rejection error
                Assert.True(ms.Length > 4);
                ms.Position = 0;
                byte[] lenBytes = new byte[4];
                ms.Read(lenBytes, 0, 4);
                uint length = BitConverter.ToUInt32(lenBytes, 0);

                byte[] payloadBytes = new byte[length];
                ms.Read(payloadBytes, 0, (int)length);
                string json = Encoding.UTF8.GetString(payloadBytes);
                JObject jobj = JObject.Parse(json);

                Assert.Equal("error", jobj.Value<string>("status"));
                Assert.Equal(1, jobj.Value<int>("exit_code"));
                Assert.Contains("[SECURITY]", jobj.Value<string>("stderr"));
            }
        }

        [Fact]
        public async Task SecurityDispatcher_Handshake_ReturnsConfigAndVersion()
        {
            var envelope = new IpcEnvelope
            {
                Command = "HANDSHAKE",
                ExtensionVersion = "1.0.0",
                Browser = "chrome"
            };

            using (var ms = new MemoryStream())
            {
                var context = new IpcRequestContext(envelope, ms);

                await IpcSecurityDispatcher.DispatchAsync(
                    context,
                    null,
                    null,
                    null,
                    null,
                    null);

                Assert.True(ms.Length > 4);

                ms.Position = 0;
                byte[] lenBytes = new byte[4];
                ms.Read(lenBytes, 0, 4);
                uint length = BitConverter.ToUInt32(lenBytes, 0);

                byte[] payloadBytes = new byte[length];
                ms.Read(payloadBytes, 0, (int)length);

                string json = Encoding.UTF8.GetString(payloadBytes);
                JObject jobj = JObject.Parse(json);

                Assert.Equal("ready", jobj.Value<string>("status"));
                Assert.True(jobj.Value<bool>("greenshot_running"));
                Assert.NotNull(jobj["config"]);
                Assert.True(jobj["config"]?.Value<bool>("track_tab_urls"));
            }
        }

        [Fact]
        public async Task SecurityDispatcher_TabChanged_UpdatesBrowserContextTracker()
        {
            var envelope = new IpcEnvelope
            {
                Command = "TAB_CHANGED",
                Url = "https://jira.company.org/browse/QA-4567?filter=all",
                Title = "QA-4567: Critical regression in login flow"
            };

            using (var ms = new MemoryStream())
            {
                var context = new IpcRequestContext(envelope, ms);

                await IpcSecurityDispatcher.DispatchAsync(
                    context,
                    null,
                    null,
                    null,
                    null,
                    null);

                Assert.Equal("jira.company.org", BrowserContextTracker.Instance.CurrentDomain);
                Assert.Equal("QA-4567", BrowserContextTracker.Instance.CurrentTicket);
                Assert.Equal("QA-4567", BrowserContextTracker.Instance.GetSafeDestinationSubfolder());
            }
        }

        [Fact]
        public async Task SecurityDispatcher_OpenFile_RejectsExecutableFiles()
        {
            string tempScript = Path.GetTempFileName() + ".bat";
            File.WriteAllText(tempScript, "@echo off");

            try
            {
                var envelope = IpcEnvelope.CreateOpenFile(tempScript);

                using (var ms = new MemoryStream())
                {
                    var context = new IpcRequestContext(envelope, ms);

                    bool openInvoked = false;
                    await IpcSecurityDispatcher.DispatchAsync(
                        context,
                        null,
                        null,
                        null,
                        null,
                        path => openInvoked = true);

                    Assert.False(openInvoked, "Security dispatcher must reject .bat and executable files.");
                }
            }
            finally
            {
                if (File.Exists(tempScript))
                {
                    File.Delete(tempScript);
                }
            }
        }

        [Fact]
        public async Task SecurityDispatcher_OpenFile_AllowsWhitelistedImageFiles()
        {
            string tempImage = Path.GetTempFileName() + ".png";
            File.WriteAllBytes(tempImage, new byte[] { 0x89, 0x50, 0x4E, 0x47 });

            try
            {
                var envelope = IpcEnvelope.CreateOpenFile(tempImage);

                using (var ms = new MemoryStream())
                {
                    var context = new IpcRequestContext(envelope, ms);

                    string openedPath = null;
                    await IpcSecurityDispatcher.DispatchAsync(
                        context,
                        null,
                        null,
                        null,
                        null,
                        path => openedPath = path);

                    Assert.True(ms.Length > 4);
                    ms.Position = 0;
                    byte[] lenBytes = new byte[4];
                    ms.Read(lenBytes, 0, 4);
                    uint length = BitConverter.ToUInt32(lenBytes, 0);

                    byte[] payloadBytes = new byte[length];
                    ms.Read(payloadBytes, 0, (int)length);
                    string json = Encoding.UTF8.GetString(payloadBytes);
                    JObject jobj = JObject.Parse(json);

                    Assert.Equal("ok", jobj.Value<string>("status"));
                    Assert.Equal(0, jobj.Value<int>("exit_code"));
                }
            }
            finally
            {
                if (File.Exists(tempImage))
                {
                    File.Delete(tempImage);
                }
            }
        }

        [Fact]
        public async Task SecurityDispatcher_ListRecipes_ReturnsConfiguredCommandlineRecipes()
        {
            var recipe = new CaptureRecipe("recipe_ocr", "OCR Recipe", "Extracts text")
            {
                IsEnabled = true
            };
            var trig = new TriggerConfig(TriggerConfig.TypeCommandline, "CLI Trigger")
            {
                Enabled = true
            };
            trig.Parameters["Command"] = "ocr";
            recipe.Triggers.Add(trig);
            RecipeManager.Instance.RegisterRecipe(recipe);

            var envelope = IpcEnvelope.CreateListRecipes();

            using (var ms = new MemoryStream())
            {
                var context = new IpcRequestContext(envelope, ms);

                await IpcSecurityDispatcher.DispatchAsync(
                    context,
                    null,
                    null,
                    null,
                    null,
                    null);

                Assert.True(ms.Length > 4);
                ms.Position = 0;
                byte[] lenBytes = new byte[4];
                ms.Read(lenBytes, 0, 4);
                uint length = BitConverter.ToUInt32(lenBytes, 0);

                byte[] payloadBytes = new byte[length];
                ms.Read(payloadBytes, 0, (int)length);
                string json = Encoding.UTF8.GetString(payloadBytes);
                JObject jobj = JObject.Parse(json);

                Assert.Equal("ok", jobj.Value<string>("status"));
                Assert.Equal(0, jobj.Value<int>("exit_code"));
                Assert.NotNull(jobj["recipes"]);

                var recipes = jobj["recipes"] as JArray;
                Assert.NotNull(recipes);
                Assert.Contains(recipes, r => r.Value<string>("command") == "ocr" || r.Value<string>("id") == "recipe_ocr");
            }
        }

        [Fact]
        public async Task SecurityDispatcher_OpenFile_WithCwd_ResolvesRelativePath()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "GreenshotTestCwd_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string testFile = Path.Combine(tempDir, "relative_test.png");
            File.WriteAllBytes(testFile, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            try
            {
                string openedFile = null;
                var envelope = new IpcEnvelope
                {
                    Version = 1,
                    Source = "open_with",
                    Command = "OPEN_FILE",
                    Cwd = tempDir,
                    Files = new List<string> { "relative_test.png" }
                };

                using (var ms = new MemoryStream())
                {
                    var context = new IpcRequestContext(envelope, ms);
                    await IpcSecurityDispatcher.DispatchAsync(
                        context,
                        null,
                        null,
                        null,
                        null,
                        file => openedFile = file);

                    Assert.True(ms.Length > 4);
                    ms.Position = 0;
                    byte[] lenBytes = new byte[4];
                    ms.Read(lenBytes, 0, 4);
                    uint length = BitConverter.ToUInt32(lenBytes, 0);

                    byte[] payloadBytes = new byte[length];
                    ms.Read(payloadBytes, 0, (int)length);
                    string json = Encoding.UTF8.GetString(payloadBytes);
                    JObject jobj = JObject.Parse(json);

                    Assert.Equal("ok", jobj.Value<string>("status"));
                    Assert.Equal(0, jobj.Value<int>("exit_code"));
                    Assert.Contains("Opened 1 file(s)", jobj.Value<string>("stdout"));
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void SecurityDispatcher_PathSanitizer_SupportsUnicodeAndInternationalPaths()
        {
            string cwd = @"C:\Users\GreenshotUser\Documents";

            // Japanese
            Assert.True(IpcSecurityDispatcher.TrySanitizeAndResolvePath(@"日本語_スクリーンショット.png", cwd, "cli", out string fullJap, out string errJap));
            Assert.Equal(@"C:\Users\GreenshotUser\Documents\日本語_スクリーンショット.png", fullJap);
            Assert.Null(errJap);

            // Accented German / French
            Assert.True(IpcSecurityDispatcher.TrySanitizeAndResolvePath(@"München_Über_café.png", cwd, "cli", out string fullGer, out string errGer));
            Assert.Equal(@"C:\Users\GreenshotUser\Documents\München_Über_café.png", fullGer);
            Assert.Null(errGer);

            // Cyrillic
            Assert.True(IpcSecurityDispatcher.TrySanitizeAndResolvePath(@"снимок_экрана.png", cwd, "cli", out string fullCyr, out string errCyr));
            Assert.Equal(@"C:\Users\GreenshotUser\Documents\снимок_экрана.png", fullCyr);
            Assert.Null(errCyr);

            // Relative with dots and subfolders
            Assert.True(IpcSecurityDispatcher.TrySanitizeAndResolvePath(@"./subfolder/../image.png", cwd, "cli", out string fullRel, out string errRel));
            Assert.Equal(@"C:\Users\GreenshotUser\Documents\image.png", fullRel);
            Assert.Null(errRel);
        }

        [Fact]
        public void SecurityDispatcher_PathSanitizer_RejectsMaliciousPatterns()
        {
            string cwd = @"C:\Greenshot";

            // 1. Control character / null byte
            Assert.False(IpcSecurityDispatcher.TrySanitizeAndResolvePath("test\0.png", cwd, "cli", out _, out string errCtrl));
            Assert.Contains("control characters", errCtrl);

            // 2. Unicode Bidi override (U+202E) spoofing
            Assert.False(IpcSecurityDispatcher.TrySanitizeAndResolvePath("test\u202Egnp.exe", cwd, "cli", out _, out string errBidi));
            Assert.Contains("forbidden Unicode formatting", errBidi);

            // 3. Alternate Data Stream (ADS colon)
            Assert.False(IpcSecurityDispatcher.TrySanitizeAndResolvePath(@"C:\Greenshot\test.png:hidden.exe", cwd, "cli", out _, out string errAds));
            Assert.Contains("Alternate Data Stream", errAds);

            // 4. DOS Reserved device names
            Assert.False(IpcSecurityDispatcher.TrySanitizeAndResolvePath(@"CON.png", cwd, "cli", out _, out string errCon));
            Assert.Contains("reserved device name", errCon);

            Assert.False(IpcSecurityDispatcher.TrySanitizeAndResolvePath(@"aux.jpg", cwd, "cli", out _, out string errAux));
            Assert.Contains("reserved device name", errAux);

            Assert.False(IpcSecurityDispatcher.TrySanitizeAndResolvePath(@"nul", cwd, "cli", out _, out string errNul));
            Assert.Contains("reserved device name", errNul);

            Assert.False(IpcSecurityDispatcher.TrySanitizeAndResolvePath(@"sub\COM1.png", cwd, "cli", out _, out string errCom));
            Assert.Contains("reserved device name", errCom);

            // 5. UNC network share from untrusted url_scheme source
            Assert.False(IpcSecurityDispatcher.TrySanitizeAndResolvePath(@"\\evil.attacker.com\share\image.png", cwd, "url_scheme", out _, out string errUnc));
            Assert.Contains("Network (UNC) paths are not permitted", errUnc);
        }

        [Fact]
        public async Task SecurityDispatcher_OpenFile_WithUnicodeFilename_IsAccepted()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "GreenshotUnicode_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string testFile = Path.Combine(tempDir, "тест_日本語_München.png");
            File.WriteAllBytes(testFile, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            try
            {
                var envelope = new IpcEnvelope
                {
                    Version = 1,
                    Source = "open_with",
                    Command = "OPEN_FILE",
                    Cwd = tempDir,
                    Files = new List<string> { "тест_日本語_München.png" }
                };

                using (var ms = new MemoryStream())
                {
                    var context = new IpcRequestContext(envelope, ms);
                    await IpcSecurityDispatcher.DispatchAsync(
                        context,
                        null,
                        null,
                        null,
                        null,
                        null);

                    ms.Position = 0;
                    byte[] lenBytes = new byte[4];
                    ms.Read(lenBytes, 0, 4);
                    uint length = BitConverter.ToUInt32(lenBytes, 0);

                    byte[] payloadBytes = new byte[length];
                    ms.Read(payloadBytes, 0, (int)length);
                    string json = Encoding.UTF8.GetString(payloadBytes);
                    JObject jobj = JObject.Parse(json);

                    Assert.Equal("ok", jobj.Value<string>("status"));
                    Assert.Equal(0, jobj.Value<int>("exit_code"));
                    Assert.Contains("Opened 1 file(s)", jobj.Value<string>("stdout"));
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task SecurityDispatcher_RunRecipe_UnconfiguredRecipe_IsRejected()
        {
            var envelope = IpcEnvelope.CreateRunRecipe("malicious_or_nonexistent_recipe");

            using (var ms = new MemoryStream())
            {
                var context = new IpcRequestContext(envelope, ms);

                await IpcSecurityDispatcher.DispatchAsync(
                    context,
                    null,
                    null,
                    null,
                    null,
                    null);

                Assert.True(ms.Length > 4);
                ms.Position = 0;
                byte[] lenBytes = new byte[4];
                ms.Read(lenBytes, 0, 4);
                uint length = BitConverter.ToUInt32(lenBytes, 0);

                byte[] payloadBytes = new byte[length];
                ms.Read(payloadBytes, 0, (int)length);
                string json = Encoding.UTF8.GetString(payloadBytes);
                JObject jobj = JObject.Parse(json);

                Assert.Equal("error", jobj.Value<string>("status"));
                Assert.Equal(1, jobj.Value<int>("exit_code"));
                Assert.Contains("not found", jobj.Value<string>("stderr"));
            }
        }

        [Fact]
        public void BrowserContextTracker_ExtractsTicketsAndSanitizesPaths()
        {
            var tracker = BrowserContextTracker.Instance;

            tracker.UpdateContext("https://github.com/greenshot/greenshot/issues/42", "Issue 42: Bug in Greenshot");
            Assert.Equal("github.com", tracker.CurrentDomain);
            Assert.Equal(string.Empty, tracker.CurrentTicket);
            Assert.Equal("github.com", tracker.GetSafeDestinationSubfolder());

            tracker.UpdateContext("https://corp.jira.com/browse/PROJ-123", "PROJ-123: Fix payment portal");
            Assert.Equal("corp.jira.com", tracker.CurrentDomain);
            Assert.Equal("PROJ-123", tracker.CurrentTicket);
            Assert.Equal("PROJ-123", tracker.GetSafeDestinationSubfolder());
        }

        [Fact]
        public void BrowserContextTracker_DecoupledUrlAndTitleUpdates_ClearsStaleTitleAndUpdatesCleanly()
        {
            var tracker = BrowserContextTracker.Instance;

            // 1. Initial page loaded
            tracker.UpdateContext("https://corp.jira.com/browse/PROJ-123", "PROJ-123: Fix payment portal");
            Assert.Equal("corp.jira.com", tracker.CurrentDomain);
            Assert.Equal("PROJ-123", tracker.CurrentTicket);
            Assert.Equal("PROJ-123: Fix payment portal", tracker.CurrentTitle);

            // 2. Navigation to new URL started before title loads: sends URL with empty title
            tracker.UpdateContext("https://github.com/greenshot/greenshot", "");
            Assert.Equal("github.com", tracker.CurrentDomain);
            Assert.Equal("https://github.com/greenshot/greenshot", tracker.CurrentUrl);
            Assert.Equal(string.Empty, tracker.CurrentTitle); // Stale title from Jira was cleared!
            Assert.Equal(string.Empty, tracker.CurrentTicket);

            // 3. Title finishes loading on the new page
            tracker.UpdateContext("https://github.com/greenshot/greenshot", "Greenshot GitHub Repository");
            Assert.Equal("github.com", tracker.CurrentDomain);
            Assert.Equal("Greenshot GitHub Repository", tracker.CurrentTitle);
        }

        [Fact]
        public void TriggerItemViewModel_CommandlineAndOpenFile_ExposesProperties()
        {
            var cliConfig = new TriggerConfig(TriggerConfig.TypeCommandline, "CLI Trigger");
            cliConfig.Parameters["Command"] = "ocr";
            cliConfig.Parameters["Description"] = "OCR image file";
            cliConfig.Parameters["FireAndForget"] = true;

            var cliVm = new Greenshot.Plugin.RecipeEditor.ViewModels.TriggerItemViewModel(cliConfig);
            Assert.True(cliVm.IsCommandline);
            Assert.Equal("ocr", cliVm.Command);
            Assert.Equal("OCR image file", cliVm.Description);
            Assert.True(cliVm.FireAndForget);
            Assert.Contains("ocr", cliVm.DisplayTitle);

            cliVm.Command = "ocr-test";
            Assert.Equal("ocr-test", cliConfig.Parameters["Command"]);

            var openFileConfig = new TriggerConfig(TriggerConfig.TypeOpenFile, "Open File");
            openFileConfig.Parameters["Filter"] = "*.png;*.jpg";
            var openVm = new Greenshot.Plugin.RecipeEditor.ViewModels.TriggerItemViewModel(openFileConfig);
            Assert.True(openVm.IsOpenFile);
            Assert.Equal("*.png;*.jpg", openVm.Filter);
            Assert.Contains("*.png;*.jpg", openVm.DisplayTitle);
        }

        [Fact]
        public void RecipeEditorViewModel_RecipeIdProperty_IsExposedAndNotified()
        {
            var recipe = new CaptureRecipe("test_recipe_id", "Test Workflow");
            var vm = new Greenshot.Plugin.RecipeEditor.ViewModels.RecipeEditorViewModel();
            vm.ActiveRecipe = recipe;

            Assert.Equal("test_recipe_id", vm.RecipeId);

            bool idChanged = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.RecipeId)) idChanged = true;
            };

            vm.RecipeId = "updated_recipe_id";
            Assert.True(idChanged);
            Assert.Equal("updated_recipe_id", recipe.Id);
            Assert.NotNull(vm.CopyRecipeIdCommand);
        }

        [Theory]
        [InlineData("greenshot:settings", "SETTINGS", null)]
        [InlineData("greenshot:settings?tab=capture", "SETTINGS", "capture")]
        [InlineData("greenshot://settings?tab=general", "SETTINGS", "general")]
        [InlineData("greenshot:about", "ABOUT", null)]
        [InlineData("greenshot://about", "ABOUT", null)]
        [InlineData("greenshot:self-service?section=debug", "SELF_SERVICE", "debug")]
        [InlineData("greenshot://recipe-editor?recipe=ocr", "RECIPE_EDITOR", "ocr")]
        [InlineData("greenshot:recipe/my-task?destination=clipboard", "RUN_RECIPE", null)]
        public async Task SecurityDispatcher_UrlScheme_ParsesAndExecutesCorrectly(string url, string expectedCommand, string expectedParam)
        {
            if (url.Contains("my-task"))
            {
                var testRecipe = new CaptureRecipe("recipe_my_task", "My Task")
                    .AddNode(new RecipeNodeConfig { Id = "s1", StepType = WellKnownStepTypes.Source, Parameters = new Dictionary<string, object> { ["SourceType"] = CaptureSourceType.Clipboard } })
                    .AddTrigger(TriggerConfig.CreateCommandline("my-task", fireAndForget: true));
                RecipeManager.Instance.RegisterRecipe(testRecipe);
            }

            var envelope = new IpcEnvelope
            {
                Source = "url_scheme",
                RawInput = url
            };

            using (var ms = new MemoryStream())
            {
                var context = new IpcRequestContext(envelope, ms);
                await IpcSecurityDispatcher.DispatchAsync(context, null, () => { }, () => { }, () => { }, f => { });

                Assert.True(ms.Length > 4);
                ms.Position = 0;
                byte[] lenBytes = new byte[4];
                ms.Read(lenBytes, 0, 4);
                uint length = BitConverter.ToUInt32(lenBytes, 0);

                byte[] payloadBytes = new byte[length];
                ms.Read(payloadBytes, 0, (int)length);
                string json = Encoding.UTF8.GetString(payloadBytes);
                var resp = JObject.Parse(json);
                Assert.True(!string.IsNullOrEmpty(expectedCommand));
                Assert.Equal("ok", resp["status"]?.ToString());
                Assert.Equal(0, resp["exit_code"]?.Value<int>());

                if (!string.IsNullOrEmpty(expectedParam))
                {
                    Assert.True(envelope.Parameters.ContainsValue(expectedParam) ||
                                (envelope.Parsed != null && envelope.Parsed.Parameters.ContainsValue(expectedParam)));
                }
            }
        }
    }
}
