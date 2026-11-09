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
using System.ComponentModel;
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Definitions;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;

using Greenshot.Base.Languages;
using Greenshot.Plugin.ExternalCommand.Destinations;
using Greenshot.Plugin.ExternalCommand.Recipes;
using Greenshot.Plugin.ExternalCommand.Running;
using Greenshot.Plugin.ExternalCommand.ViewModels;
using Greenshot.Plugin.ExternalCommand.Views;
using Greenshot.Base.Expressions;

namespace Greenshot.Plugin.ExternalCommand;

/// <summary>
/// An Plugin to run commands after an image was written
/// </summary>
public class ExternalCommandPlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ExternalCommandPlugin));

    public ValueTask DisposeAsync()
    {
        return default;
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "ExternalCommand";

    /// <summary>
    /// The program of the command can be started now: a file, or a program on the PATH (also an App Execution Alias of a Windows App)
    /// </summary>
    private static bool CanRun(CommandDefinition command)
    {
        string program = OutputTemplates.RenderCommand(command.Program, VariableScope.Empty, out string problem);
        if (program == null)
        {
            Log.WarnFormat("The program {0} of the command {1} can't be used, the command is left out until the next start: {2}", command.Program, command.Name, problem);
            return false;
        }

        if (WindowsAppLauncher.AppIdOf(program) is { } appId)
        {
            if (WindowsAppLauncher.IsInstalled(appId)) return true;
            Log.WarnFormat("The Windows App {0} of the command {1} isn't installed, the command is left out until the next start", appId, command.Name);
            return false;
        }

        if (!File.Exists(program) && PluginUtils.GetExePath(program) == null)
        {
            Log.WarnFormat("The program {0} of the command {1} isn't found, the command is left out until the next start", command.Program, command.Name);
            return false;
        }

        return true;
    }

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IExternalCommandLanguage>(new ExternalCommandLanguageImpl());
        var externalCommandSection = new ExternalCommandConfigurationImpl();
        services.AddConfiguration(externalCommandSection);

        services.AddService<IIconProvider>(new ExternalCommandIconProvider());
        // The destinations come from the loaded configuration
        services.AddServices(CreateDestinations);
        services.AddRecipeStepProvider(this);
        services.AddSettingsView<CommandSettingsViewModel>(viewModel => new CommandSettingsView(viewModel));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => new CommandSettingsViewModel(CommandDefinitions.CreateStore());

    /// <summary>
    /// The destinations of the commands which can run now. A command whose program isn't found (a USB stick or network drive which
    /// isn't there yet) stays in the settings, it only gets no destination until the next start. Commands added in the settings get
    /// theirs with the next start too, an approved one at once, a change of a command applies at once.
    /// </summary>
    private static IEnumerable<IDestination> CreateDestinations()
    {
        return CommandDefinitions.CreateStore().GetReadable()
            .Where(command => !CommandDefinitions.IsTurnedOff(command.Name) && CanRun(command))
            .Select(command => (IDestination)new ExternalCommandDestination(command, readsSettings: true))
            .ToList();
    }

    /// <summary>
    /// Registers recipe step factories provided by the ExternalCommand plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.Register<ExternalCommandStep>(config => new ExternalCommandStep(config));
    }

    /// <summary>
    /// Like the recipe files: a command file added or changed outside Greenshot is left out at the start, once Greenshot runs
    /// the user sees what it runs and decides whether to use it
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        DefinitionApprovalAtStartup.AskWhenIdle(CommandDefinitions.CreateStore, ConfirmAtStartup, AddDestination);
        return Task.CompletedTask;
    }

    private static bool ConfirmAtStartup(CommandDefinition command)
    {
        var language = Texts.Get<IExternalCommandLanguage>();
        return DefinitionImport.Confirm(null, language.ApproveTitle, CommandSettingsViewModel.DescribeApproval(command),
            new[] { language.ApproveButton, language.ApproveLater }, onTop: true) == 0;
    }

    /// <summary>
    /// The destination of a command which was approved after the start, when its program can be started
    /// </summary>
    internal static void AddDestination(CommandDefinition command)
    {
        if (command != null && !CommandDefinitions.IsTurnedOff(command.Name) && CanRun(command)
            && !SimpleServiceProvider.Current.GetAllInstances<IDestination>().Any(destination => destination.Designation == ExternalCommandDestination.DesignationOf(command.Name)))
        {
            SimpleServiceProvider.Current.AddService<IDestination>(new ExternalCommandDestination(command, readsSettings: true));
        }
    }

    /// <summary>
    /// A command which was turned off: its destination goes at once, from the menus and the destination settings
    /// </summary>
    internal static void RemoveDestination(string name)
    {
        string designation = ExternalCommandDestination.DesignationOf(name);
        var destinations = SimpleServiceProvider.Current.GetAllInstances<IDestination>().OfType<ExternalCommandDestination>()
            .Where(destination => destination.Designation == designation)
            .Cast<IDestination>()
            .ToList();
        SimpleServiceProvider.Current.RemoveService(destinations);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
