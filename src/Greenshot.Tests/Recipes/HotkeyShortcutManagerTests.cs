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
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Helpers;
using Greenshot.Recipes;
using Greenshot.Recipes.Pipeline;
using Greenshot.Recipes.Steps;
using Greenshot.Recipes.Triggers;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// Covers HotkeyShortcutManager (validation, capture function/destination lookups, registration with the trigger
    /// manager) and the execution of a hotkey shortcut with several destinations through the existing
    /// DestinationExportStep/"OverrideDestinations" mechanism - no parallel hotkey or destination engine is involved.
    /// Shares the RecipeManager collection: several tests here touch the global RecipeManager.Instance and/or the
    /// shared ICoreConfiguration section, which must not run concurrently with other tests doing the same.
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class HotkeyShortcutManagerTests
    {
        public HotkeyShortcutManagerTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private sealed class RecordingTriggerManager : ITriggerManager
        {
            public List<ITrigger> Registered { get; } = new List<ITrigger>();
            public List<string> Unregistered { get; } = new List<string>();

            public IReadOnlyList<ITrigger> GetAllTriggers() => Registered;
            public IReadOnlyList<IEditorTrigger> GetEditorTriggers() => Array.Empty<IEditorTrigger>();
            public void RegisterTrigger(ITrigger trigger) => Registered.Add(trigger);
            public bool UnregisterTrigger(string triggerId) { Unregistered.Add(triggerId); return true; }
            public void StartAll() { }
            public void StopAll() { }
            public event EventHandler<TriggerEventArgs> TriggerFired;
            public void Dispose() { }
        }

        private sealed class FakeDestination : IDestination
        {
            public FakeDestination(string designation) => Designation = designation;
            public string Designation { get; }
            public DestinationDescriptor Descriptor => new DestinationDescriptor(Designation);
            public bool IsAvailableFor(ICaptureDetails metadata) => true;
            public ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken)
                => new ValueTask<IReadOnlyList<IDestination>>(Array.Empty<IDestination>());
            public Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken) => Task.FromResult(ExportResult.Succeeded());
        }

        private sealed class RecordingDispatcher : IDestinationDispatcher
        {
            public List<IDestination> Dispatched { get; } = new List<IDestination>();

            public Task DispatchAsync(CaptureFlowContext context, IEnumerable<IDestination> destinations, CancellationToken cancellationToken = default)
            {
                Dispatched.AddRange(destinations);
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void Validate_DuplicateHotkeyAmongOtherEntries_IsRejected()
        {
            var a = HotkeyShortcutConfig.Create("Ctrl + Alt + R", HotkeyCaptureFunctions.Region, new[] { "Picker" });
            var b = HotkeyShortcutConfig.Create("Ctrl + Alt + R", HotkeyCaptureFunctions.Fullscreen, new[] { "Editor" });

            bool valid = HotkeyShortcutManager.Validate(a, new[] { b }, out string error);

            Assert.False(valid);
            Assert.Contains("already assigned", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Validate_SameEntryComparedWithItself_IsNotFlaggedAsDuplicate()
        {
            var a = HotkeyShortcutConfig.Create("Ctrl + Alt + R", HotkeyCaptureFunctions.Region, new[] { "Picker" });

            bool valid = HotkeyShortcutManager.Validate(a, new[] { a }, out string error);

            Assert.True(valid, error);
        }

        [Fact]
        public void Validate_DifferentHotkeys_AreNotFlaggedAsDuplicates()
        {
            var a = HotkeyShortcutConfig.Create("Ctrl + Alt + R", HotkeyCaptureFunctions.Region, new[] { "Picker" });
            var b = HotkeyShortcutConfig.Create("Ctrl + Alt + S", HotkeyCaptureFunctions.Fullscreen, new[] { "Editor" });

            Assert.True(HotkeyShortcutManager.Validate(a, new[] { b }, out string errorA), errorA);
            Assert.True(HotkeyShortcutManager.Validate(b, new[] { a }, out string errorB), errorB);
        }

        [Theory]
        [InlineData("A")]
        [InlineData("Shift + A")]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_InvalidHotkeySequence_IsRejected(string hotkey)
        {
            var entry = HotkeyShortcutConfig.Create(hotkey, HotkeyCaptureFunctions.Region, new[] { "Picker" });

            bool valid = HotkeyShortcutManager.Validate(entry, null, out string error);

            Assert.False(valid);
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void Validate_NoDestinations_IsRejected()
        {
            var entry = HotkeyShortcutConfig.Create("Ctrl + Alt + Q", HotkeyCaptureFunctions.Region, Array.Empty<string>());

            bool valid = HotkeyShortcutManager.Validate(entry, null, out string error);

            Assert.False(valid);
            Assert.Contains("destination", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Validate_UnknownCaptureFunction_IsRejected()
        {
            var entry = HotkeyShortcutConfig.Create("Ctrl + Alt + Z", "NotARealFunction", new[] { "Picker" });

            Assert.False(HotkeyShortcutManager.Validate(entry, null, out string error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void Validate_WellFormedEntry_IsAccepted()
        {
            var entry = HotkeyShortcutConfig.Create("Ctrl + Alt + Shift + R", HotkeyCaptureFunctions.Region, new[] { "Picker" });

            bool valid = HotkeyShortcutManager.Validate(entry, null, out string error);

            Assert.True(valid, error);
        }

        [Fact]
        public void ResolveRecipeId_KnownFunctions_ResolveToExistingBuiltInRecipes()
        {
            foreach (var functionId in HotkeyCaptureFunctions.All)
            {
                string recipeId = HotkeyShortcutManager.ResolveRecipeId(functionId);
                Assert.False(string.IsNullOrEmpty(recipeId));
                Assert.NotNull(RecipeManager.Instance.GetRecipeById(recipeId));
            }
        }

        [Fact]
        public void ResolveRecipeId_UnknownOrEmptyFunction_ReturnsNull()
        {
            Assert.Null(HotkeyShortcutManager.ResolveRecipeId("DoesNotExist"));
            Assert.Null(HotkeyShortcutManager.ResolveRecipeId(null));
            Assert.Null(HotkeyShortcutManager.ResolveRecipeId(string.Empty));
        }

        [Fact]
        public void GetAvailableCaptureFunctions_ReturnsAllFiveKnownFunctions()
        {
            var functions = HotkeyShortcutManager.GetAvailableCaptureFunctions();
            var ids = functions.Select(f => f.Id).ToList();

            foreach (var expected in HotkeyCaptureFunctions.All)
            {
                Assert.Contains(expected, ids);
            }
            Assert.All(functions, f => Assert.False(string.IsNullOrEmpty(f.DisplayName)));
        }

        [Fact]
        public void GetAvailableDestinations_NeverThrows_ReturnsDesignationAndDisplayNamePairs()
        {
            var destinations = HotkeyShortcutManager.GetAvailableDestinations();

            Assert.NotNull(destinations);
            Assert.All(destinations, d => Assert.False(string.IsNullOrEmpty(d.Designation)));
            Assert.All(destinations, d => Assert.False(string.IsNullOrEmpty(d.DisplayName)));
        }

        [Fact]
        public void GetShortcuts_SaveShortcuts_RoundTripsThroughTheSharedConfiguration()
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            string original = config.HotkeyShortcutsJson;
            try
            {
                var entries = new List<HotkeyShortcutConfig>
                {
                    HotkeyShortcutConfig.Create("Ctrl + Alt + Shift + T", HotkeyCaptureFunctions.Clipboard, new[] { "Editor", "Clipboard" })
                };

                HotkeyShortcutManager.SaveShortcuts(entries);
                var loaded = HotkeyShortcutManager.GetShortcuts();

                Assert.Single(loaded);
                Assert.Equal("Ctrl + Alt + Shift + T", loaded[0].Hotkey);
                Assert.Equal(HotkeyCaptureFunctions.Clipboard, loaded[0].CaptureFunctionId);
                Assert.Equal(new List<string> { "Editor", "Clipboard" }, loaded[0].Destinations);
            }
            finally
            {
                config.HotkeyShortcutsJson = original;
            }
        }

        [Fact]
        public void GetDisplayHotkeyFor_ReturnsMatchingEntry()
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            string original = config.HotkeyShortcutsJson;
            try
            {
                var entry = HotkeyShortcutConfig.Create("Ctrl + Alt + E", HotkeyCaptureFunctions.Fullscreen, new[] { "Picker" });
                HotkeyShortcutManager.SaveShortcuts(new List<HotkeyShortcutConfig> { entry });

                string displayHotkey = HotkeyShortcutManager.GetDisplayHotkeyFor(HotkeyCaptureFunctions.Fullscreen, "fallback");

                Assert.Equal("Ctrl + Alt + E", displayHotkey);
            }
            finally
            {
                config.HotkeyShortcutsJson = original;
            }
        }

        [Fact]
        public void GetDisplayHotkeyFor_NoMatch_ReturnsFallback()
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            string original = config.HotkeyShortcutsJson;
            try
            {
                HotkeyShortcutManager.SaveShortcuts(new List<HotkeyShortcutConfig>());

                string displayHotkey = HotkeyShortcutManager.GetDisplayHotkeyFor(HotkeyCaptureFunctions.Region, "fallback-value");

                Assert.Equal("fallback-value", displayHotkey);
            }
            finally
            {
                config.HotkeyShortcutsJson = original;
            }
        }

        [Fact]
        public void RegisterAll_RegistersValidEntries_SkipsUnknownFunctions_NeverThrows()
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            string original = config.HotkeyShortcutsJson;
            try
            {
                var valid = HotkeyShortcutConfig.Create("Ctrl + Alt + Shift + Z", HotkeyCaptureFunctions.Region, new[] { "Picker" });
                var unknownFunction = HotkeyShortcutConfig.Create("Ctrl + Alt + Shift + Y", "NotARealFunction", new[] { "Picker" });
                HotkeyShortcutManager.SaveShortcuts(new List<HotkeyShortcutConfig> { valid, unknownFunction });

                var triggerManager = new RecordingTriggerManager();

                var exception = Record.Exception(() => HotkeyShortcutManager.RegisterAll(triggerManager));
                Assert.Null(exception);

                var registeredHotkeyTriggers = triggerManager.Registered.OfType<HotkeyTrigger>().ToList();
                Assert.Single(registeredHotkeyTriggers);
                Assert.Equal("Ctrl + Alt + Shift + Z", registeredHotkeyTriggers[0].HotkeyString);
                Assert.Equal(RecipeManager.RecipeIdRegion, registeredHotkeyTriggers[0].TargetRecipeId);
                Assert.NotNull(registeredHotkeyTriggers[0].ExtraParameters);
                Assert.True(registeredHotkeyTriggers[0].ExtraParameters.ContainsKey("OverrideDestinations"));

                Assert.Contains(triggerManager.Unregistered, id => id.EndsWith(unknownFunction.Id, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                config.HotkeyShortcutsJson = original;
            }
        }

        [Fact]
        public void RegisterAll_NullTriggerManager_DoesNotThrow()
        {
            var exception = Record.Exception(() => HotkeyShortcutManager.RegisterAll(null));
            Assert.Null(exception);
        }

        [Fact]
        public async Task DestinationExportStep_OverrideDestinations_DispatchesToAllResolvedDestinations_SkipsUnknownOneGracefully()
        {
            string designationA = "TestDestA_" + Guid.NewGuid().ToString("N");
            string designationB = "TestDestB_" + Guid.NewGuid().ToString("N");
            string unknownDesignation = "TestDestMissing_" + Guid.NewGuid().ToString("N");

            var fakeA = new FakeDestination(designationA);
            var fakeB = new FakeDestination(designationB);
            SimpleServiceProvider.Current.AddService<IDestination>(fakeA);
            SimpleServiceProvider.Current.AddService<IDestination>(fakeB);
            try
            {
                var node = RecipeStepConfig.CreateDestinations("export");
                var dispatcher = new RecordingDispatcher();
                var step = new DestinationExportStep(node, dispatcher);

                var recipe = new CaptureRecipe("test_recipe_multi_dest_" + Guid.NewGuid().ToString("N"), "Test Recipe");
                using var context = new CaptureFlowContext(recipe);
                context.Properties["OverrideDestinations"] = new List<string> { designationA, unknownDesignation, designationB };

                await step.ExecuteAsync(context);

                Assert.Equal(2, dispatcher.Dispatched.Count);
                Assert.Contains(dispatcher.Dispatched, d => d.Designation == designationA);
                Assert.Contains(dispatcher.Dispatched, d => d.Designation == designationB);
                Assert.DoesNotContain(dispatcher.Dispatched, d => d.Designation == unknownDesignation);
            }
            finally
            {
                SimpleServiceProvider.Current.RemoveService<IDestination>(fakeA, fakeB);
            }
        }

        [Fact]
        public async Task DestinationExportStep_OverrideDestinations_AllUnknown_DispatchesToNoneWithoutThrowing()
        {
            var node = RecipeStepConfig.CreateDestinations("export");
            var dispatcher = new RecordingDispatcher();
            var step = new DestinationExportStep(node, dispatcher);

            var recipe = new CaptureRecipe("test_recipe_multi_dest_" + Guid.NewGuid().ToString("N"), "Test Recipe");
            using var context = new CaptureFlowContext(recipe);
            context.Properties["OverrideDestinations"] = new List<string> { "TestDestMissingA_" + Guid.NewGuid().ToString("N"), "TestDestMissingB_" + Guid.NewGuid().ToString("N") };

            var exception = await Record.ExceptionAsync(() => step.ExecuteAsync(context));

            Assert.Null(exception);
            Assert.Empty(dispatcher.Dispatched);
        }
    }
}
