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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using Xunit;
using Xunit.Abstractions;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// Measures how long it takes to open the editor. The numbers only mean something for the first editor of a process,
    /// so run these tests on their own, e.g. with the filter "FullyQualifiedName~EditorStartupTimingTests.ColdAndWarmEditor".
    /// The timings are written to the test output, the tests only fail when the editor can't be opened at all.
    /// </summary>
    [Trait("Category", "Performance")]
    public class EditorStartupTimingTests
    {
        private readonly ITestOutputHelper _output;
        private readonly log4net.Appender.MemoryAppender _logAppender = new();

        public EditorStartupTimingTests(ITestOutputHelper output)
        {
            _output = output;
            TestEnvironment.EnsureInitialized();
            EnsureEmojiDataFile();

            // The editor logs the time its startup phases took
            var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository(typeof(ImageEditorForm).Assembly);
            var logger = (log4net.Repository.Hierarchy.Logger)hierarchy.GetLogger(typeof(ImageEditorForm).FullName);
            logger.Level = log4net.Core.Level.Debug;
            logger.AddAppender(_logAppender);
            hierarchy.Configured = true;
        }

        [InteractiveDesktopFact]
        public void ColdAndWarmEditor()
        {
            RunOnSta(() =>
            {
                for (int i = 1; i <= 3; i++)
                {
                    var (construct, shown) = OpenEditor();
                    _output.WriteLine($"Editor #{i}: constructor {construct} ms, until shown and painted {shown} ms, total {construct + shown} ms");
                }
            });
        }

        [InteractiveDesktopFact]
        public void PrewarmedEditor()
        {
            var stopwatch = Stopwatch.StartNew();
            Greenshot.Editor.EditorPrewarm.PrewarmAsync(TimeSpan.Zero).GetAwaiter().GetResult();
            _output.WriteLine($"Prewarm (in the background): {stopwatch.ElapsedMilliseconds} ms");
            RunOnSta(() =>
            {
                for (int i = 1; i <= 2; i++)
                {
                    var (construct, shown) = OpenEditor();
                    _output.WriteLine($"Editor #{i}: constructor {construct} ms, until shown and painted {shown} ms, total {construct + shown} ms");
                }
            });
        }

        [InteractiveDesktopFact]
        public void PrewarmedComponents()
        {
            Greenshot.Editor.EditorPrewarm.PrewarmAsync(TimeSpan.Zero).GetAwaiter().GetResult();
            RunOnSta(() =>
            {
                Measure("Form with a ToolStrip, MenuStrip and StatusStrip", () =>
                {
                    using var form = new Form();
                    var menuStrip = new MenuStrip();
                    menuStrip.Items.Add(new ToolStripMenuItem("File", null, new ToolStripMenuItem("Close")));
                    var toolStrip = new ToolStrip();
                    toolStrip.Items.Add(new ToolStripButton("Button"));
                    toolStrip.Items.Add(new ToolStripDropDownButton("DropDown"));
                    toolStrip.Items.Add(new ToolStripSplitButton("Split"));
                    var statusStrip = new StatusStrip();
                    statusStrip.Items.Add(new ToolStripStatusLabel("Status"));
                    form.Controls.Add(toolStrip);
                    form.Controls.Add(menuStrip);
                    form.Controls.Add(statusStrip);
                    _ = form.Handle;
                });
                Measure("Static constructor GreenshotForm", () => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Greenshot.Base.Controls.GreenshotForm).TypeHandle));
                Measure("Static constructor EditorForm", () => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(EditorForm).TypeHandle));
                Measure("Static constructor ImageEditorForm", () => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ImageEditorForm).TypeHandle));
                Measure("ComponentResourceManager", () => _ = new System.ComponentModel.ComponentResourceManager(typeof(ImageEditorForm)));
                var (construct, shown) = OpenEditor();
                _output.WriteLine($"Editor after the components: constructor {construct} ms, until shown and painted {shown} ms");
            });
        }

        [InteractiveDesktopFact]
        public void ColdComponents()
        {
            RunOnSta(() =>
            {
                var editorAssembly = typeof(ImageEditorForm).Assembly;
                var emojiData = editorAssembly.GetType("Greenshot.Editor.Controls.Emoji.EmojiData", true);
                var emojiRenderer = editorAssembly.GetType("Greenshot.Editor.Drawing.Emoji.EmojiRenderer", true);
                var getBitmap = emojiRenderer.GetMethod("GetBitmap", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                var load = emojiData.GetMethod("Load", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                Measure("All embedded images of the ImageEditorForm", () =>
                {
                    foreach (string name in EmbeddedResources.GetNames(typeof(ImageEditorForm)))
                    {
                        EmbeddedResources.GetImage(typeof(ImageEditorForm), name)?.Dispose();
                    }
                });
                if (load != null)
                {
                    Measure("EmojiData.Load #1", () => load.Invoke(null, null));
                    Measure("EmojiData.Load #2", () => load.Invoke(null, null));
                }
                Measure("EmojiRenderer.GetBitmap #1", () => ((Image)getBitmap.Invoke(null, new object[] { "😊", 32 })).Dispose());
                Measure("EmojiRenderer.GetBitmap #2", () => ((Image)getBitmap.Invoke(null, new object[] { "😊", 32 })).Dispose());
                Measure("FontFamily.Families #1", () => _ = FontFamily.Families.Length);
                Measure("FontFamily.Families #2", () => _ = FontFamily.Families.Length);
                Measure("Static constructor GreenshotForm", () => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Greenshot.Base.Controls.GreenshotForm).TypeHandle));
                Measure("Static constructor EditorForm", () => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(EditorForm).TypeHandle));
                Measure("Static constructor ImageEditorForm", () => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ImageEditorForm).TypeHandle));
                Measure("ComponentResourceManager", () => _ = new System.ComponentModel.ComponentResourceManager(typeof(ImageEditorForm)));
                var (construct, shown) = OpenEditor();
                _output.WriteLine($"Editor after the components: constructor {construct} ms, until shown and painted {shown} ms");
            });
        }

        private (long construct, long shown) OpenEditor()
        {
            var image = new Bitmap(1280, 800, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(image))
            {
                graphics.Clear(Color.CornflowerBlue);
            }
            var surface = new Surface(image);

            var stopwatch = Stopwatch.StartNew();
            var editor = new ImageEditorForm(surface, true);
            long construct = stopwatch.ElapsedMilliseconds;
            foreach (var loggingEvent in _logAppender.PopAllEvents())
            {
                _output.WriteLine("  " + loggingEvent.RenderedMessage);
            }

            bool painted = false;
            editor.Shown += (_, _) => editor.BeginInvoke(new MethodInvoker(() => painted = true));
            stopwatch.Restart();
            editor.Show();
            var timeout = Stopwatch.StartNew();
            while (!painted && timeout.ElapsedMilliseconds < 30000)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            editor.Update();
            long shown = stopwatch.ElapsedMilliseconds;
            Assert.True(painted, "The editor was not shown within 30 seconds");

            // Dispose instead of Close: closing stores the editor placement in greenshot.ini, which doesn't exist in the tests,
            // the exception would show the WinForms exception dialog
            surface.Modified = false;
            editor.Hide();
            editor.Dispose();
            Application.DoEvents();
            return (construct, shown);
        }

        private void Measure(string what, Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            _output.WriteLine($"{what}: {stopwatch.ElapsedMilliseconds} ms");
        }

        private static void RunOnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromMinutes(2)), "Timed out");
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        /// The editor loads emojis.xml and the Twemoji font from the folder of Greenshot.Base.dll, which is a shadow copy
        /// folder when the tests run. Copy the files there, emojis.xml is generated by the build of the Greenshot project.
        /// </summary>
        private static void EnsureEmojiDataFile()
        {
            var targetFolder = EnvironmentInfo.GetApplicationFolder();
            // ...\src\Greenshot.Tests\bin\<Configuration>\net480
            var testFolder = new DirectoryInfo(Path.GetDirectoryName(new Uri(typeof(EditorStartupTimingTests).Assembly.CodeBase).LocalPath));
            var srcFolder = testFolder.Parent?.Parent?.Parent?.Parent;
            var candidates = new List<string>
            {
                Path.Combine(testFolder.FullName, "emojis.xml"),
                Path.Combine(testFolder.FullName, "Twemoji.Mozilla.ttf")
            };
            if (srcFolder != null)
            {
                var greenshotOutput = Path.Combine(srcFolder.FullName, "Greenshot", "bin", testFolder.Parent.Name, testFolder.Name);
                candidates.Add(Path.Combine(greenshotOutput, "emojis.xml"));
                candidates.Add(Path.Combine(greenshotOutput, "Twemoji.Mozilla.ttf"));
            }

            foreach (var source in candidates)
            {
                var target = Path.Combine(targetFolder, Path.GetFileName(source));
                if (File.Exists(target) || !File.Exists(source))
                {
                    continue;
                }
                try
                {
                    File.Copy(source, target);
                }
                catch (IOException)
                {
                    // Another test copied it already
                }
            }
        }
    }
}
