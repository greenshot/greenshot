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

using Greenshot.Linux.Core.Ipc;
using Greenshot.Linux.Recipes.Ipc;

namespace Greenshot.Linux.Tests.Ipc;

/// <summary>
/// The app for the dispatcher in the tests: records what it is asked to do
/// </summary>
internal sealed class FakeIpcApp : IIpcApp
{
    public List<string> Calls { get; } = new();

    public Task Ready => Task.CompletedTask;

    public string Version => "1.4.0-test";

    public void Run(AppCommand command) => Calls.Add("run " + command.ToWire());

    public Task ShowSettingsAsync() => Record("settings");

    public Task ShowAboutAsync() => Record("about");

    public Task ShowLogAsync() => Record("log");

    public Task OpenRecipeEditorAsync(string? recipeId) => Record("editor " + recipeId);

    public Task OpenRecipeManagerAsync() => Record("manager");

    public Task ReloadConfigurationAsync() => Record("reload");

    public void Exit() => Calls.Add("exit");

    private Task Record(string call)
    {
        Calls.Add(call);
        return Task.CompletedTask;
    }
}
