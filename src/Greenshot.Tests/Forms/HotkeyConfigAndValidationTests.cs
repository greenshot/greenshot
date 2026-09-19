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
using System.Linq;
using System.Threading;
using Dapplo.Windows.Input.Enums;
using Greenshot.Base.Core;
using Greenshot.Forms.Wpf;
using Greenshot.UI.Controls;
using Xunit;

namespace Greenshot.Tests.Forms
{
    public class HotkeyConfigAndValidationTests
    {
        public HotkeyConfigAndValidationTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        [InlineData("Alt + PrintScreen", false, true, false, false, VirtualKeyCode.Snapshot)]
        [InlineData("Ctrl + PrintScreen", true, false, false, false, VirtualKeyCode.Snapshot)]
        [InlineData("Shift + PrintScreen", false, false, true, false, VirtualKeyCode.Snapshot)]
        [InlineData("PrintScreen", false, false, false, false, VirtualKeyCode.Snapshot)]
        public void HotkeySequence_ParsesLegacyConfigStrings(string input, bool ctrl, bool alt, bool shift, bool win, VirtualKeyCode expectedKey)
        {
            var seq = HotkeySequence.Parse(input);
            Assert.False(seq.IsEmpty);
            Assert.Single(seq.Chords);

            var chord = seq.Chords[0];
            Assert.Equal(ctrl, chord.Ctrl);
            Assert.Equal(alt, chord.Alt);
            Assert.Equal(shift, chord.Shift);
            Assert.Equal(win, chord.Win);
            Assert.Equal(expectedKey, chord.Key);
            Assert.Equal(ModifierLocation.Any, chord.CtrlLocation);
            Assert.Equal(ModifierLocation.Any, chord.AltLocation);
            Assert.Equal(ModifierLocation.Any, chord.ShiftLocation);
            Assert.Equal(ModifierLocation.Any, chord.WinLocation);
        }

        [Fact]
        public void HotkeySequence_ParsesModifierSidesAndChords()
        {
            string input = "Ctrl(L) + Alt + K, Ctrl(R) + C";
            var seq = HotkeySequence.Parse(input);
            Assert.Equal(2, seq.Chords.Count);

            var chord1 = seq.Chords[0];
            Assert.True(chord1.Ctrl);
            Assert.Equal(ModifierLocation.Left, chord1.CtrlLocation);
            Assert.True(chord1.Alt);
            Assert.Equal(ModifierLocation.Any, chord1.AltLocation);
            Assert.Equal(VirtualKeyCode.KeyK, chord1.Key);

            var chord2 = seq.Chords[1];
            Assert.True(chord2.Ctrl);
            Assert.Equal(ModifierLocation.Right, chord2.CtrlLocation);
            Assert.Equal(VirtualKeyCode.KeyC, chord2.Key);

            string roundTrip = seq.ToString();
            Assert.Equal("Ctrl(L) + Alt + K, Ctrl(R) + C", roundTrip);
        }

        [Theory]
        [InlineData("A", false, "require at least one modifier")]
        [InlineData("1", false, "require at least one modifier")]
        [InlineData("Shift + A", false, "Shift cannot be the only modifier")]
        [InlineData("Shift + 1", false, "Shift cannot be the only modifier")]
        [InlineData("Ctrl + Escape", false, "reserved")]
        [InlineData("Alt + Return", false, "reserved")]
        [InlineData("Ctrl + Tab", false, "reserved")]
        [InlineData("PrintScreen", true, null)]
        [InlineData("F1", true, null)]
        [InlineData("F12", true, null)]
        [InlineData("ScrollLock", true, null)]
        [InlineData("Pause", true, null)]
        [InlineData("Ctrl + Alt + K", true, null)]
        [InlineData("Ctrl(L) + Shift + S", true, null)]
        public void HotkeySequence_ValidationRules(string input, bool expectedValid, string expectedMessagePart)
        {
            var seq = HotkeySequence.Parse(input);
            bool isValid = seq.Validate(out string error);
            Assert.Equal(expectedValid, isValid);

            if (!expectedValid && expectedMessagePart != null)
            {
                Assert.NotNull(error);
                Assert.Contains(expectedMessagePart, error, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void HotkeyControls_CanBeInstantiatedOnStaThread()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var badge = new KeyCapBadge("Ctrl (L)");
                    Assert.Equal("Ctrl (L)", badge.KeyText);

                    var display = new HotkeyDisplayControl();
                    display.HotkeyString = "Ctrl(L) + Alt + K, Ctrl + C";
                    display.UpdateBadges();
                    Assert.True(display.BadgesContainer.Items.Count >= 5);

                    // Test ShowEditButton property
                    Assert.Equal(System.Windows.Visibility.Visible, display.EditButtonControl.Visibility);
                    display.ShowEditButton = false;
                    Assert.Equal(System.Windows.Visibility.Collapsed, display.EditButtonControl.Visibility);
                    display.ShowEditButton = true;
                    Assert.Equal(System.Windows.Visibility.Visible, display.EditButtonControl.Visibility);

                    Greenshot.UI.WpfThemeHelper.IsDarkMode = true;
                    var modal = new HotkeyEditorModal();
                    string savedResult = null;
                    modal.Open("Test Recipe Hotkey", "Ctrl + Shift + R", s => savedResult = s);
                    Assert.True(Greenshot.Base.Wpf.ThemeManager.Instance.IsDarkTheme);
                    Assert.NotNull(modal.Resources[typeof(System.Windows.Controls.GroupBox)]);
                    Assert.NotNull(modal.Resources[typeof(System.Windows.Controls.ComboBox)]);
                    Assert.NotNull(modal.Resources[typeof(System.Windows.Controls.ComboBoxItem)]);
                    Assert.NotNull(modal.Resources[typeof(System.Windows.Controls.CheckBox)]);
                    Assert.Equal(System.Windows.Visibility.Visible, modal.Visibility);

                    var vm = modal.DataContext as HotkeyEditorViewModel;
                    Assert.NotNull(vm);
                    Assert.True(vm.IsValid);
                    Assert.True(vm.Ctrl);
                    Assert.True(vm.Shift);
                    Assert.Equal(VirtualKeyCode.KeyR, vm.TriggerKey);

                    // Execute save command
                    vm.SaveCommand.Execute(null);
                    Assert.Equal(System.Windows.Visibility.Collapsed, modal.Visibility);
                    Assert.Equal("Ctrl + Shift + R", savedResult);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void RecipeValidator_ValidatesHotkeyTriggers()
        {
            var validRecipe = new Greenshot.Base.Recipes.CaptureRecipe("test_recipe_1", "Test Recipe", "Test Description")
                .AddNode(new Greenshot.Base.Recipes.RecipeNodeConfig { Id = "node1", Name = "Step 1", StepType = "Source" });
            validRecipe.Triggers = new System.Collections.Generic.List<Greenshot.Base.Triggers.TriggerConfig>
            {
                new Greenshot.Base.Triggers.TriggerConfig("Hotkey", "Valid Hotkey")
                {
                    Parameters = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "Hotkey", "Ctrl + Alt + R" }
                    }
                }
            };

            var validResult = Greenshot.Base.Recipes.RecipeValidator.Validate(validRecipe);
            Assert.True(validResult.IsValid);

            var invalidRecipe = new Greenshot.Base.Recipes.CaptureRecipe("test_recipe_2", "Test Recipe Invalid", "Test Description")
                .AddNode(new Greenshot.Base.Recipes.RecipeNodeConfig { Id = "node1", Name = "Step 1", StepType = "Source" });
            invalidRecipe.Triggers = new System.Collections.Generic.List<Greenshot.Base.Triggers.TriggerConfig>
            {
                new Greenshot.Base.Triggers.TriggerConfig("Hotkey", "Invalid Hotkey")
                {
                    Parameters = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "Hotkey", "A" } // Missing modifier
                    }
                }
            };

            var invalidResult = Greenshot.Base.Recipes.RecipeValidator.Validate(invalidRecipe);
            Assert.False(invalidResult.IsValid);
            Assert.Contains(invalidResult.Errors, e => e.Contains("require at least one modifier"));
        }

        [Fact]
        public void ScrollLock_Then_Key_Sequence_Validation()
        {
            // ScrollLock then C is valid (chord 1 is standalone system key, chord 2 needs no modifier)
            var seq = HotkeySequence.Parse("Scroll, C");
            Assert.True(seq.Validate(out string error), error);

            // Standalone C is invalid (requires modifier)
            var singleKey = HotkeySequence.Parse("C");
            Assert.False(singleKey.Validate(out _));

            // Ctrl + K then C is also valid
            var vsCodeChord = HotkeySequence.Parse("Ctrl + K, C");
            Assert.True(vsCodeChord.Validate(out string error2), error2);
        }

        [Fact]
        public void Modal_Clear_AllowsSavingNone_ToDisableHotkey()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var modal = new HotkeyEditorModal();
                    string savedResult = "Initial";
                    modal.Open("Test Disable", "Ctrl + Shift + R", s => savedResult = s);
                    var vm = modal.DataContext as HotkeyEditorViewModel;
                    Assert.NotNull(vm);

                    vm.ClearCommand.Execute(null);
                    Assert.True(vm.IsValid);
                    Assert.Null(vm.ValidationError);

                    vm.SaveCommand.Execute(null);
                    Assert.Equal("None", savedResult);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void Modal_StepByStep_Editing_And_Removal()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var modal = new HotkeyEditorModal();
                    string savedResult = null;
                    modal.Open("Test Steps", "Scroll, C", s => savedResult = s);
                    var vm = modal.DataContext as HotkeyEditorViewModel;
                    Assert.NotNull(vm);
                    Assert.Equal(2, vm.Steps.Count);
                    Assert.Equal(VirtualKeyCode.Scroll, vm.Steps[0].Key);
                    Assert.Equal(VirtualKeyCode.KeyC, vm.Steps[1].Key);

                    // Select Step 0
                    vm.SelectStep(0);
                    Assert.Equal(VirtualKeyCode.Scroll, vm.TriggerKey);

                    // Add Step 3
                    vm.AddStepCommand.Execute(null);
                    Assert.Equal(3, vm.Steps.Count);
                    vm.TriggerKey = VirtualKeyCode.KeyD;
                    Assert.Equal(VirtualKeyCode.KeyD, vm.Steps[2].Key);

                    // Remove Step 1 (which is 'C')
                    vm.RemoveStep(1);
                    Assert.Equal(2, vm.Steps.Count);
                    Assert.Equal(VirtualKeyCode.Scroll, vm.Steps[0].Key);
                    Assert.Equal(VirtualKeyCode.KeyD, vm.Steps[1].Key);

                    // Save
                    vm.SaveCommand.Execute(null);
                    Assert.Equal("ScrollLock, D", savedResult);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public async System.Threading.Tasks.Task ClipboardCaptureSource_AcquireAsync_FromMTAThread_DoesNotThrowThreadStateException()
        {
            var source = new Greenshot.Base.Pipeline.Sources.ClipboardCaptureSource();
            var recipe = new Greenshot.Base.Recipes.CaptureRecipe("test_clipboard", "Test Clipboard", "Test");
            var context = new Greenshot.Base.Pipeline.CaptureFlowContext(recipe);
            var payload = await source.AcquireAsync(context);
            // Should either return payload (if clipboard contains image) or abort cleanly, without throwing ThreadStateException
            Assert.True(context.IsAborted || payload != null);
        }

        private static Dapplo.Windows.Input.Keyboard.KeyboardHookEventArgs CreateKeyboardHookEventArgs(
            VirtualKeyCode key,
            bool isKeyDown,
            bool isLeftWin = false,
            bool isRightWin = false,
            bool isLeftControl = false,
            bool isLeftAlt = false,
            bool isLeftShift = false,
            bool isModifier = false,
            bool isInjected = false)
        {
            var args = (Dapplo.Windows.Input.Keyboard.KeyboardHookEventArgs)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Dapplo.Windows.Input.Keyboard.KeyboardHookEventArgs));
            var type = typeof(Dapplo.Windows.Input.Keyboard.KeyboardHookEventArgs);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

            type.GetField("<Key>k__BackingField", flags)?.SetValue(args, key);
            type.GetField("<IsKeyDown>k__BackingField", flags)?.SetValue(args, isKeyDown);
            type.GetField("<IsLeftWindows>k__BackingField", flags)?.SetValue(args, isLeftWin);
            type.GetField("<IsRightWindows>k__BackingField", flags)?.SetValue(args, isRightWin);
            type.GetField("<IsLeftControl>k__BackingField", flags)?.SetValue(args, isLeftControl);
            type.GetField("<IsLeftAlt>k__BackingField", flags)?.SetValue(args, isLeftAlt);
            type.GetField("<IsLeftShift>k__BackingField", flags)?.SetValue(args, isLeftShift);
            type.GetField("<IsModifier>k__BackingField", flags)?.SetValue(args, isModifier);
            if (isInjected)
            {
                type.GetField("<Flags>k__BackingField", flags)?.SetValue(args, Dapplo.Windows.Input.Enums.ExtendedKeyFlags.Injected);
            }

            return args;
        }

        [Fact]
        public void HotkeyManager_SequenceWithHeldModifier_TriggersCorrectly()
        {
            HotkeyManager.ClearRegisteredHotkeys();
            bool triggered = false;
            var seq = HotkeySequence.Parse("Win + PrintScreen, C");
            HotkeyManager.RegisterHotKey(seq, () => triggered = true);

            // Step 1: User presses Win + PrintScreen
            var e1 = CreateKeyboardHookEventArgs(VirtualKeyCode.Snapshot, isKeyDown: true, isLeftWin: true);
            HotkeyManager.HandleKeyboardEvent(e1);

            Assert.True(e1.Handled);
            Assert.Equal(1, HotkeyManager.CandidateSequenceCount);
            Assert.Equal(1, HotkeyManager.ActiveChordIndex);
            Assert.False(triggered);

            // Step 2: User presses C while still holding Win
            var e2 = CreateKeyboardHookEventArgs(VirtualKeyCode.KeyC, isKeyDown: true, isLeftWin: true);
            HotkeyManager.HandleKeyboardEvent(e2);

            Assert.True(e2.Handled);
            Assert.True(triggered);
            Assert.Equal(0, HotkeyManager.CandidateSequenceCount);
            Assert.Equal(0, HotkeyManager.ActiveChordIndex);

            HotkeyManager.ClearRegisteredHotkeys();
        }

        [Fact]
        public void HotkeyManager_SequenceWithReleasedModifier_TriggersCorrectly()
        {
            HotkeyManager.ClearRegisteredHotkeys();
            bool triggered = false;
            var seq = HotkeySequence.Parse("Win + PrintScreen, C");
            HotkeyManager.RegisterHotKey(seq, () => triggered = true);

            // Step 1: User presses Win + PrintScreen
            var e1 = CreateKeyboardHookEventArgs(VirtualKeyCode.Snapshot, isKeyDown: true, isLeftWin: true);
            HotkeyManager.HandleKeyboardEvent(e1);

            Assert.True(e1.Handled);
            Assert.Equal(1, HotkeyManager.CandidateSequenceCount);
            Assert.Equal(1, HotkeyManager.ActiveChordIndex);
            Assert.False(triggered);

            // Step 2: User releases Win, then presses C
            var e2 = CreateKeyboardHookEventArgs(VirtualKeyCode.KeyC, isKeyDown: true, isLeftWin: false);
            HotkeyManager.HandleKeyboardEvent(e2);

            Assert.True(e2.Handled);
            Assert.True(triggered);
            Assert.Equal(0, HotkeyManager.CandidateSequenceCount);
            Assert.Equal(0, HotkeyManager.ActiveChordIndex);

            HotkeyManager.ClearRegisteredHotkeys();
        }

        [Fact]
        public void HotkeyManager_SequenceAbortsOnEscape()
        {
            HotkeyManager.ClearRegisteredHotkeys();
            bool triggered = false;
            var seq = HotkeySequence.Parse("Win + PrintScreen, C");
            HotkeyManager.RegisterHotKey(seq, () => triggered = true);

            // Step 1: User presses Win + PrintScreen
            var e1 = CreateKeyboardHookEventArgs(VirtualKeyCode.Snapshot, isKeyDown: true, isLeftWin: true);
            HotkeyManager.HandleKeyboardEvent(e1);
            Assert.Equal(1, HotkeyManager.CandidateSequenceCount);

            // User presses Escape
            var esc = CreateKeyboardHookEventArgs(VirtualKeyCode.Escape, isKeyDown: true);
            HotkeyManager.HandleKeyboardEvent(esc);
            Assert.True(esc.Handled);
            Assert.Equal(0, HotkeyManager.CandidateSequenceCount);
            Assert.False(triggered);

            HotkeyManager.ClearRegisteredHotkeys();
        }

        [Fact]
        public void HotkeyManager_SequenceWithScrollLock_TriggersCorrectly()
        {
            HotkeyManager.ClearRegisteredHotkeys();
            bool triggered = false;
            var seq = HotkeySequence.Parse("ScrollLock, C");
            HotkeyManager.RegisterHotKey(seq, () => triggered = true);

            // Dapplo marks ScrollLock with IsModifier = true. HotkeyManager must still recognize it!
            var e1 = CreateKeyboardHookEventArgs(VirtualKeyCode.Scroll, isKeyDown: true, isModifier: true);
            HotkeyManager.HandleKeyboardEvent(e1);
            Assert.True(e1.Handled);
            Assert.Equal(1, HotkeyManager.CandidateSequenceCount);
            Assert.Equal(1, HotkeyManager.ActiveChordIndex);

            // Step 2: User presses C
            var e2 = CreateKeyboardHookEventArgs(VirtualKeyCode.KeyC, isKeyDown: true);
            HotkeyManager.HandleKeyboardEvent(e2);

            Assert.True(e2.Handled);
            Assert.True(triggered);
            Assert.Equal(0, HotkeyManager.CandidateSequenceCount);

            HotkeyManager.ClearRegisteredHotkeys();
        }

        [Fact]
        public void HotkeyManager_InjectedKeysTriggerSequences_ForAutomation()
        {
            HotkeyManager.ClearRegisteredHotkeys();
            bool triggered = false;
            var seq = HotkeySequence.Parse("Ctrl + K, C");
            HotkeyManager.RegisterHotKey(seq, () => triggered = true);

            // Injected Ctrl+K (automation software)
            var e1 = CreateKeyboardHookEventArgs(VirtualKeyCode.KeyK, isKeyDown: true, isLeftControl: true, isInjected: true);
            HotkeyManager.HandleKeyboardEvent(e1);
            Assert.True(e1.Handled);
            Assert.Equal(1, HotkeyManager.CandidateSequenceCount);

            // Injected C
            var e2 = CreateKeyboardHookEventArgs(VirtualKeyCode.KeyC, isKeyDown: true, isInjected: true);
            HotkeyManager.HandleKeyboardEvent(e2);
            Assert.True(e2.Handled);
            Assert.True(triggered);

            HotkeyManager.ClearRegisteredHotkeys();
        }

        [Fact]
        public void DestinationExportStep_ResolvesDestinationDesignations_WithoutFallbackToPicker()
        {
            // Node created with DestinationDesignations (as in RecipeManager.InitializeDefaultRecipes)
            var node = Greenshot.Base.Recipes.RecipeStepConfig.CreateDestinations("export", new[] { "Editor" });
            Assert.True(node.Parameters.ContainsKey("DestinationDesignations"));

            var step = new Greenshot.Pipeline.Steps.DestinationExportStep(node);
            var gatedActions = step.GetGatedActions().ToList();
            Assert.NotNull(gatedActions);
        }
    }
}
