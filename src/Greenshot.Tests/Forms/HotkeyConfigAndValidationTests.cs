using System;
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
    }
}
