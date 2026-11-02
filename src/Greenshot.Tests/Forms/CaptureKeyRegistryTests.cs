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
using System.Linq;
using System.Windows.Input;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Capturing;
using Greenshot.Capturing.Overlays;
using Xunit;

namespace Greenshot.Tests.Forms;

/// <summary>
/// The keys of the capture window: registration, conflicts, lookup and the rows of the help
/// </summary>
public class CaptureKeyRegistryTests
{
    private sealed class FakeTool : CaptureTool
    {
        public override Greenshot.Base.Interfaces.CaptureMode Mode => Greenshot.Base.Interfaces.CaptureMode.Region;
    }

    private static readonly object Window = new object();
    private static readonly object Overlay = new object();

    private static CaptureKeyBinding Always(object owner, Key key, string description = "x", ModifierKeys modifiers = ModifierKeys.None) =>
        new CaptureKeyBinding(owner, null, key, modifiers, () => description, () => { });

    private static CaptureKeyBinding ForTool(ICaptureTool tool, Key key, string description = "x") =>
        new CaptureKeyBinding(tool, tool, key, ModifierKeys.None, () => description, () => { });

    [Fact]
    public void SameKey_Twice_IsAConflict_WithBothBindings()
    {
        var registry = new CaptureKeyRegistry();
        var first = registry.Register(Always(Window, Key.M));
        var second = Always(Overlay, Key.M);

        var conflict = Assert.Throws<CaptureKeyConflictException>(() => registry.Register(second));
        Assert.Same(first, conflict.Existing);
        Assert.Same(second, conflict.Requested);
        Assert.Single(registry.Bindings);
    }

    [Fact]
    public void OtherModifiers_AreAnotherKey()
    {
        var registry = new CaptureKeyRegistry();
        registry.Register(Always(Window, Key.Up));
        registry.Register(Always(Window, Key.Up, modifiers: ModifierKeys.Control));
        Assert.Equal(2, registry.Bindings.Count);
    }

    [Fact]
    public void TwoTools_CanUseTheSameKey_ButNotOneThatIsAlwaysActive()
    {
        var registry = new CaptureKeyRegistry();
        var region = new FakeTool();
        var window = new FakeTool();
        registry.Register(ForTool(region, Key.Return));
        registry.Register(ForTool(window, Key.Return));
        registry.Register(Always(Window, Key.Escape));

        Assert.Throws<CaptureKeyConflictException>(() => registry.Register(ForTool(region, Key.Escape)));
        Assert.Throws<CaptureKeyConflictException>(() => registry.Register(Always(Overlay, Key.Return)));
        Assert.Throws<CaptureKeyConflictException>(() => registry.Register(ForTool(region, Key.Return)));
    }

    [Fact]
    public void Ctrl_Alone_CannotBeRegistered()
    {
        var registry = new CaptureKeyRegistry();
        Assert.Throws<ArgumentException>(() => registry.Register(Always(Overlay, Key.LeftCtrl)));
    }

    [Fact]
    public void Find_TakesTheKeyOfTheActiveTool()
    {
        var registry = new CaptureKeyRegistry();
        var region = new FakeTool();
        var window = new FakeTool();
        var regionEnter = registry.Register(ForTool(region, Key.Return));
        var windowEnter = registry.Register(ForTool(window, Key.Return));

        Assert.Same(regionEnter, registry.Find(Key.Return, ModifierKeys.None, region));
        Assert.Same(windowEnter, registry.Find(Key.Return, ModifierKeys.None, window));
        Assert.Null(registry.Find(Key.Return, ModifierKeys.None, new FakeTool()));
    }

    [Fact]
    public void KeyForSomeTools_OnlyWorksAndShowsForThem()
    {
        var registry = new CaptureKeyRegistry();
        var region = new FakeTool();
        var window = new FakeTool();
        var zoomer = registry.Register(new CaptureKeyBinding(Window, null, Key.Z, ModifierKeys.None, () => "zoomer", () => { }, tool => tool == region));

        Assert.Same(zoomer, registry.Find(Key.Z, ModifierKeys.None, region));
        Assert.Null(registry.Find(Key.Z, ModifierKeys.None, window));
        Assert.True(zoomer.IsActiveFor(region));
        Assert.False(zoomer.IsActiveFor(window));
    }

    [Fact]
    public void Find_WithShift_FallsBackToTheKeyWithoutShift()
    {
        var registry = new CaptureKeyRegistry();
        var up = registry.Register(Always(Window, Key.Up));
        var ctrlUp = registry.Register(Always(Window, Key.Up, modifiers: ModifierKeys.Control));
        var shift = registry.Register(Always(Window, Key.LeftShift));

        Assert.Same(up, registry.Find(Key.Up, ModifierKeys.Shift, null));
        Assert.Same(ctrlUp, registry.Find(Key.Up, ModifierKeys.Shift | ModifierKeys.Control, null));
        // Pressing Shift reports Shift as modifier
        Assert.Same(shift, registry.Find(Key.LeftShift, ModifierKeys.Shift, null));
        Assert.Null(registry.Find(Key.Up, ModifierKeys.Alt, null));
    }

    [Fact]
    public void Find_NumberPadDigit_DoesWhatTheDigitDoes_UnlessItHasItsOwn()
    {
        var registry = new CaptureKeyRegistry();
        var tool = new FakeTool();
        var one = registry.Register(ForTool(tool, Key.D1, "Pick"));
        var two = registry.Register(ForTool(tool, Key.D2, "Pick"));
        var numPadTwo = registry.Register(ForTool(tool, Key.NumPad2, "Own"));

        Assert.Same(one, registry.Find(Key.NumPad1, ModifierKeys.None, tool));
        Assert.Same(numPadTwo, registry.Find(Key.NumPad2, ModifierKeys.None, tool));
        Assert.Same(two, registry.Find(Key.D2, ModifierKeys.None, tool));
        Assert.Null(registry.Find(Key.NumPad3, ModifierKeys.None, tool));
        Assert.Null(registry.Find(Key.NumPad1, ModifierKeys.None, new FakeTool()));
    }

    [Fact]
    public void Description_IsReadWhenAsked()
    {
        string text = "before";
        var binding = new CaptureKeyBinding(Window, null, Key.M, ModifierKeys.None, () => text, () => { });
        text = "after";
        Assert.Equal("after", binding.Description);
    }

    [Fact]
    public void KeyText_ShowsModifiersAndShortNames()
    {
        Assert.Equal("Ctrl+Shift+C", CaptureKeyBinding.GetKeyText(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.Equal("Esc", CaptureKeyBinding.GetKeyText(Key.Escape, ModifierKeys.None));
        Assert.Equal("5", CaptureKeyBinding.GetKeyText(Key.D5, ModifierKeys.None));
        Assert.Equal("F1", CaptureKeyBinding.GetKeyText(Key.F1, ModifierKeys.None));
    }

    [Fact]
    public void HelpRows_ToolKeysFirst_SameDescriptionSharesARow()
    {
        var registry = new CaptureKeyRegistry();
        var region = new FakeTool();
        var window = new FakeTool();
        registry.Register(Always(Window, Key.Up, "Move"));
        registry.Register(Always(Window, Key.Down, "Move"));
        registry.Register(Always(Window, Key.LeftShift, "Direction"));
        registry.Register(Always(Window, Key.RightShift, "Direction"));
        registry.Register(ForTool(region, Key.Return, "Select"));
        registry.Register(ForTool(window, Key.Return, "Window"));

        var rows = HelpOverlay.GetRows(registry.Bindings, region);

        Assert.Equal(new[] { "Select", "Move", "Direction" }, rows.Select(row => row.Description));
        Assert.Equal(new[] { "↑", "↓" }, rows[1].Keys);
        Assert.Equal(new[] { "Shift" }, rows[2].Keys);
    }

    [Fact]
    public void HelpRows_TheKeyWhichSwitchesToTheActiveTool_IsNotShown()
    {
        var registry = new CaptureKeyRegistry();
        var fixedSize = new FakeTool();
        var region = new FakeTool();
        registry.Register(Always(fixedSize, Key.F, "Fixed size"));
        registry.Register(Always(Window, Key.Escape, "Cancel"));

        Assert.Equal(new[] { "Cancel" }, HelpOverlay.GetRows(registry.Bindings, fixedSize).Select(row => row.Description));
        Assert.Equal(new[] { "Fixed size", "Cancel" }, HelpOverlay.GetRows(registry.Bindings, region).Select(row => row.Description));
    }

    [Fact]
    public void HelpRows_ManyKeysWithTheSameDescription_ShowAsARange()
    {
        var registry = new CaptureKeyRegistry();
        var tool = new FakeTool();
        for (int i = 0; i < 9; i++)
        {
            registry.Register(ForTool(tool, Key.D1 + i, "Pick"));
        }

        var rows = HelpOverlay.GetRows(registry.Bindings, tool);

        Assert.Equal(new[] { "1–9" }, rows.Single().Keys);
    }

    [Fact]
    public void HelpRows_ZeroAfterNine_ShowsAfterTheRange()
    {
        var registry = new CaptureKeyRegistry();
        var tool = new FakeTool();
        for (int i = 0; i < 9; i++)
        {
            registry.Register(ForTool(tool, Key.D1 + i, "Pick"));
        }
        registry.Register(ForTool(tool, Key.D0, "Pick"));

        var rows = HelpOverlay.GetRows(registry.Bindings, tool);

        Assert.Equal(new[] { "1–9", "0" }, rows.Single().Keys);
    }
}
