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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Help;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Editor.Forms;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Opens a window of Greenshot: the tray menu, the settings, the about window, the help or an empty editor
    /// </summary>
    [StepInfo(WellKnownStepTypes.OpenWindow, "Open window", "Opens a window of Greenshot: the tray menu, the settings, about, the help or an empty editor.", "Interaction")]
    [StepParameter(WindowParameter, ContractDataType.Enum, Required = true, SupportsExpressions = false, Description = "Which window to open",
        AllowedValues = new[] { TrayMenu, Settings, About, Help, EmptyEditor })]
    public class OpenWindowStep : ICaptureStep
    {
        public const string WindowParameter = "Window";

        public const string TrayMenu = "TrayMenu";
        public const string Settings = "Settings";
        public const string About = "About";
        public const string Help = "Help";
        public const string EmptyEditor = "EmptyEditor";

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        public OpenWindowStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? WellKnownStepTypes.OpenWindow;
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) return;

            string window = Config.GetParameter<string>(WindowParameter);
            var shell = SimpleServiceProvider.Current.GetInstance<IGreenshotShell>(isOptional: true);
            // Without the shell (unattended, tests) only the help and the editor can be opened
            Action open = window switch
            {
                _ when string.Equals(window, TrayMenu, StringComparison.OrdinalIgnoreCase) && shell != null => shell.ShowTrayMenu,
                _ when string.Equals(window, Settings, StringComparison.OrdinalIgnoreCase) && shell != null => () => shell.ShowSetting(),
                _ when string.Equals(window, About, StringComparison.OrdinalIgnoreCase) && shell != null => shell.ShowAbout,
                _ when string.Equals(window, Help, StringComparison.OrdinalIgnoreCase) => () => AsyncCommand.Run(HelpFileLoader.LoadHelpAsync, "Load the help"),
                _ when string.Equals(window, EmptyEditor, StringComparison.OrdinalIgnoreCase) => () => ImageEditorForm.OpenNewImage(),
                _ => null
            };

            if (open == null)
            {
                context.Abort($"The window '{window}' can't be opened here.");
                return;
            }

            await context.Ui.InvokeAsync(open, cancellationToken).ConfigureAwait(false);
            context.LogStep($"[OpenWindow] {window}");
        }
    }
}
