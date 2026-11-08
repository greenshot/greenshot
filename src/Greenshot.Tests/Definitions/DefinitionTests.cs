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
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Definitions;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes.Pipeline;
using Xunit;

namespace Greenshot.Tests.Definitions
{
    /// <summary>
    /// A definition as a plugin would have one
    /// </summary>
    public sealed class TestDefinition
    {
        public string Name { get; set; }

        public string Program { get; set; }
    }

    /// <summary>
    /// Reads only definitions with a name and a program, like a plugin checks again what it reads
    /// </summary>
    public sealed class TestKind : IDefinitionKind<TestDefinition>
    {
        public static TestKind Instance { get; } = new TestKind();

        public string Designation => "TestKind";

        public string NameOf(TestDefinition definition) => definition.Name;

        public TestDefinition Read(string json)
        {
            try
            {
                var definition = JsonSerializer.Deserialize<TestDefinition>(json);
                return string.IsNullOrEmpty(definition?.Name) || string.IsNullOrEmpty(definition.Program) ? null : definition;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public string Write(TestDefinition definition) => JsonSerializer.Serialize(definition);

        public string Describe(TestDefinition definition) => definition.Program;

        public RecipeGatedAction CreateGatedAction(TestDefinition definition) => new RecipeGatedAction(RecipeGateType.ExternalCommand, $"{definition.Name} ({definition.Program})");
    }

    public class DefinitionStoreTests
    {
        private readonly Dictionary<string, string> _definitions = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _icons = new Dictionary<string, string>();
        private int _changes;

        private DefinitionStore<TestDefinition> CreateStore(bool encrypt = false) =>
            encrypt
                ? new DefinitionStore<TestDefinition>(TestKind.Instance, _definitions, () => _changes++, _icons)
                : new DefinitionStore<TestDefinition>(TestKind.Instance, _definitions, () => _changes++, _icons, text => "#" + text, text => text.StartsWith("#", StringComparison.Ordinal) ? text.Substring(1) : null);

        private static TestDefinition Definition(string name, string program = "tool.exe") => new TestDefinition { Name = name, Program = program };

        [Fact]
        public void Add_StoresProtected_AndReadsBack()
        {
            var store = CreateStore();

            string id = store.Add(Definition("Optimize", "optimizer.exe"));

            Assert.Equal(1, _changes);
            Assert.StartsWith("#", _definitions[id]);
            var stored = Assert.Single(CreateStore().GetAll());
            Assert.Equal(id, stored.Id);
            Assert.Equal("Optimize", stored.Name);
            Assert.Equal("optimizer.exe", stored.Definition.Program);
        }

        [Fact]
        public void Add_Encrypted_HidesTheDefinition()
        {
            TestEnvironment.EnsureInitialized();
            var store = CreateStore(encrypt: true);

            string id = store.Add(Definition("Optimize", "optimizer.exe"));

            Assert.DoesNotContain("optimizer", _definitions[id]);
            Assert.Equal("optimizer.exe", Assert.Single(CreateStore(encrypt: true).GetReadable()).Program);
        }

        [Fact]
        public void Add_SameNameOtherCase_ReplacesIt_AndDropsTheIcon()
        {
            var store = CreateStore();
            string id = store.Add(Definition("Optimize"));
            store.SetIcon(id, new byte[] { 1, 2 });

            string again = store.Add(Definition("optimize", "other.exe"));

            Assert.Equal(id, again);
            Assert.Equal("other.exe", Assert.Single(store.GetReadable()).Program);
            Assert.Null(store.GetIcon(id));
        }

        [Fact]
        public void Add_WithoutName_Throws()
        {
            Assert.Throws<ArgumentException>(() => CreateStore().Add(Definition(" ")));
            Assert.Throws<ArgumentNullException>(() => CreateStore().Add(null));
            Assert.Equal(0, _changes);
        }

        [Fact]
        public void GetAll_SortedByName_UnreadableLast()
        {
            var store = CreateStore();
            store.Add(Definition("b"));
            store.Add(Definition("A"));
            _definitions["broken"] = "not protected";
            _definitions["unchecked"] = "#{\"Name\":\"c\"}";

            var all = store.GetAll();

            Assert.Equal(new[] { "A", "b", null, null }, all.Select(stored => stored.Name));
            Assert.Equal(new[] { "A", "b" }, store.GetReadable().Select(definition => definition.Name));
            Assert.Null(store.Find("c"));
        }

        [Fact]
        public void FindOrOnly_ByName_OrTheOnlyOne()
        {
            var store = CreateStore();
            Assert.Null(store.FindOrOnly(null));

            store.Add(Definition("One"));
            Assert.Equal("One", store.FindOrOnly(null).Name);
            Assert.Equal("One", store.FindOrOnly(" ").Name);
            Assert.Equal("One", store.FindOrOnly("one").Name);
            Assert.Null(store.FindOrOnly("two"));

            store.Add(Definition("Two"));
            Assert.Null(store.FindOrOnly(null));
            Assert.Equal("Two", store.FindOrOnly("Two").Name);
        }

        [Fact]
        public void Remove_RemovesTheDefinitionAndItsIcon()
        {
            var store = CreateStore();
            string id = store.Add(Definition("One"));
            store.SetIcon(id, new byte[] { 1 });

            Assert.True(store.Remove(id));
            Assert.False(store.Remove(id));
            Assert.False(store.Remove(null));

            Assert.Empty(store.GetAll());
            Assert.Empty(_icons);
            // add, icon, remove
            Assert.Equal(3, _changes);
        }

        [Fact]
        public void SetIcon_OnlyForAStoredDefinition()
        {
            var store = CreateStore();
            string id = store.Add(Definition("One"));

            store.SetIcon("removed", new byte[] { 1 });
            store.SetIcon(id, null);
            Assert.Empty(_icons);

            store.SetIcon(id, new byte[] { 7, 8 });
            Assert.Equal(new byte[] { 7, 8 }, CreateStore().GetIcon(id));
            _icons[id] = "not base64!";
            Assert.Null(store.GetIcon(id));
        }
    }

    public class DefinitionDestinationTests
    {
        private sealed class TestItemDestination : DefinitionItemDestination<TestDefinition>
        {
            public TestItemDestination(TestDefinition definition) : base(TestKind.Instance, definition)
            {
            }

            public List<ExportRequest> Exports { get; } = new List<ExportRequest>();

            public override Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
            {
                Exports.Add(request);
                return Task.FromResult(ExportResult.Succeeded(target: Definition.Name));
            }
        }

        private sealed class TestDestination : DefinitionDestination<TestDefinition>
        {
            private readonly TestDefinition[] _definitions;

            public TestDestination(params TestDefinition[] definitions) : base(TestKind.Instance)
            {
                _definitions = definitions;
            }

            public List<TestItemDestination> Created { get; } = new List<TestItemDestination>();

            public override DestinationDescriptor Descriptor => new DestinationDescriptor("Test", iconKey: DefinitionIcons.GenericKeyFor(Kind), hasDynamicDestinations: true, useDynamicsOnly: true);

            protected override IReadOnlyList<TestDefinition> GetDefinitions() => _definitions;

            protected override DefinitionItemDestination<TestDefinition> CreateDestination(TestDefinition definition)
            {
                var destination = new TestItemDestination(definition);
                Created.Add(destination);
                return destination;
            }
        }

        /// <summary>
        /// Picks the destination with the name, or none
        /// </summary>
        private sealed class PickingUserInteraction : IUserInteraction
        {
            private readonly string _pick;

            public PickingUserInteraction(string pick)
            {
                _pick = pick;
            }

            public bool IsInteractive => true;

            public Task<IDestination> PickDestinationAsync(IReadOnlyList<IDestination> choices, ICaptureDetails captureDetails, CancellationToken cancellationToken) =>
                Task.FromResult(choices.FirstOrDefault(choice => choice.Designation == _pick));

            public Task<SurfaceOutputSettings> PromptOutputSettingsAsync(SurfaceOutputSettings current, CancellationToken cancellationToken) => Task.FromResult(current);

            public Task NotifyAsync(Notification notification) => Task.CompletedTask;

            public Task<string> PickOpenFileAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public Task<string> PickSaveFileAsync(SaveFileRequest request, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public Task<T> RunWithProgressAsync<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> work, CancellationToken cancellationToken) => work(new Progress<ProgressInfo>(), cancellationToken);

            public Task<bool?> ConfirmAsync(string title, string message, bool isError, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");
        }

        /// <summary>
        /// Never rendered: the test destinations don't look at the capture
        /// </summary>
        private sealed class NoSource : IExportSource
        {
            public bool IsModified => false;

            public Task<IImageLease> RenderAsync(SurfaceOutputSettings settings, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public Task<EncodedImage> EncodeAsync(SurfaceOutputSettings settings, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public Task<T> UseSurfaceAsync<T>(Func<ISurface, T> use, CancellationToken cancellationToken) => throw new InvalidOperationException("Not expected");

            public void Dispose()
            {
            }
        }

        private static TestDefinition Definition(string name) => new TestDefinition { Name = name, Program = name + ".exe" };

        private static ExportRequest Request(IUserInteraction ui) => new ExportRequest(new NoSource(), new CaptureDetails(), true, ui);

        public DefinitionDestinationTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void ItemDestination_IsNamedAfterTheKindAndTheDefinition()
        {
            var destination = new TestItemDestination(Definition("Optimize"));

            Assert.Equal("TestKind:Optimize", destination.Designation);
            Assert.Equal("Optimize", destination.Descriptor.DisplayName);
            Assert.Equal("testkind:Optimize", destination.Descriptor.IconKey);
            var gated = Assert.Single(destination.GetGatedActions());
            Assert.Equal(new RecipeGatedAction(RecipeGateType.ExternalCommand, "Optimize (Optimize.exe)"), gated);
        }

        [Fact]
        public async Task Destination_HasOneSubDestinationPerDefinition_AndAllTheirApprovals()
        {
            var destination = new TestDestination(Definition("A"), Definition("B"));

            var children = await destination.GetDynamicDestinationsAsync(new CaptureDetails(), CancellationToken.None);

            Assert.Equal("TestKind", destination.Designation);
            Assert.Equal(new[] { "TestKind:A", "TestKind:B" }, children.Select(child => child.Designation));
            Assert.Equal(new[] { "A (A.exe)", "B (B.exe)" }, destination.GetGatedActions().Select(action => action.Target));
        }

        [Fact]
        public async Task Export_OneDefinition_UsesIt()
        {
            var destination = new TestDestination(Definition("Only"));

            var result = await destination.ExportAsync(Request(HeadlessUserInteraction.Instance), CancellationToken.None);

            Assert.True(result.IsSucceeded);
            Assert.Equal("Only", result.Target);
            Assert.Single(Assert.Single(destination.Created).Exports);
        }

        [Fact]
        public async Task Export_SeveralWithoutTheUser_IsDeclined()
        {
            var destination = new TestDestination(Definition("A"), Definition("B"));

            var result = await destination.ExportAsync(Request(HeadlessUserInteraction.Instance), CancellationToken.None);

            Assert.Equal(ExportResult.Declined.Status, result.Status);
            Assert.All(destination.Created, child => Assert.Empty(child.Exports));
        }

        [Fact]
        public async Task Export_None_IsDeclined()
        {
            var result = await new TestDestination().ExportAsync(Request(new PickingUserInteraction("TestKind:A")), CancellationToken.None);

            Assert.Equal(ExportResult.Declined.Status, result.Status);
        }

        [Fact]
        public async Task Export_Several_TheUserPicks()
        {
            var destination = new TestDestination(Definition("A"), Definition("B"));

            var result = await destination.ExportAsync(Request(new PickingUserInteraction("TestKind:B")), CancellationToken.None);

            Assert.Equal("B", result.Target);
            Assert.Equal("TestKind:B", result.ExportedBy.Designation);
            Assert.Single(destination.Created.Single(child => child.Definition.Name == "B").Exports);

            var cancelled = await destination.ExportAsync(Request(new PickingUserInteraction(null)), CancellationToken.None);
            Assert.Equal(ExportResult.Declined.Status, cancelled.Status);
        }
    }

    public class DefinitionImportTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "greenshot-definitions-" + Guid.NewGuid().ToString("N"));

        public DefinitionImportTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose()
        {
            Directory.Delete(_folder, true);
        }

        private string Write(string name, string text)
        {
            string path = Path.Combine(_folder, name);
            File.WriteAllText(path, text);
            return path;
        }

        /// <summary>
        /// Like a plugin's parser: the reason in a DefinitionFileException
        /// </summary>
        private static TestDefinition Parse(string json) => TestKind.Instance.Read(json) ?? throw new DefinitionFileException("Not a test definition");

        [Fact]
        public void ReadFile_Checked_GivesTheDefinition()
        {
            string path = Write("good.json", "{\"Name\":\"Optimize\",\"Program\":\"optimizer.exe\"}");

            var definition = DefinitionImport.ReadFile<TestDefinition>(path, 1024, Parse, "too large", out string error);

            Assert.Null(error);
            Assert.Equal("optimizer.exe", definition.Program);
        }

        [Fact]
        public void ReadFile_FailsTheCheck_GivesTheReason()
        {
            string path = Write("bad.json", "{\"Name\":\"Optimize\"}");

            Assert.Null(DefinitionImport.ReadFile<TestDefinition>(path, 1024, Parse, "too large", out string error));
            Assert.Equal("Not a test definition", error);
        }

        [Fact]
        public void ReadFile_TooLarge_IsNotRead()
        {
            string path = Write("large.json", new string(' ', 2048));

            Assert.Null(DefinitionImport.ReadFile<TestDefinition>(path, 1024, _ => throw new InvalidOperationException("Not read"), "too large", out string error));
            Assert.Equal("too large", error);
        }

        [Fact]
        public void ReadFile_Missing_GivesTheReason()
        {
            Assert.Null(DefinitionImport.ReadFile<TestDefinition>(Path.Combine(_folder, "missing.json"), 1024, Parse, "too large", out string error));
            Assert.False(string.IsNullOrEmpty(error));
        }
    }
}
