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
using System.Collections.ObjectModel;
using System.Linq;
using Greenshot.Ai;
using Greenshot.Ai.ViewModels;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The AI tools tab (greenshot-mcp) and the approved recipes. Not in Greenshot Light.
    /// </summary>
    public partial class SettingsViewModel
    {
        private void InitializeAiTools()
        {
            // Programs allowed to use Greenshot through greenshot-mcp
            AiToolsAllowedClients = new ObservableCollection<AiToolClientViewModel>((CoreConfiguration.AiToolsAllowedClients ?? new List<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => new AiToolClientViewModel(path.Trim())));
            AiToolsAllowedClients.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasNoAiToolClients));
            foreach (var client in AiToolsAllowedClients)
            {
                // The signature check can take a moment, the list shows right away
                client.LoadDetailsAsync().FireAndLog("AI tool details", Log);
            }
            DeniedAiToolClients = new ObservableCollection<AiToolClientViewModel>(AiToolAccess.GetDeniedClients().Select(path => new AiToolClientViewModel(path)));
            DeniedAiToolClients.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasDeniedAiToolClients));
            foreach (var client in DeniedAiToolClients)
            {
                client.LoadDetailsAsync().FireAndLog("AI tool details", Log);
            }
            AiToolsExcludedProcesses = new ObservableCollection<string>();
            foreach (string name in CoreConfiguration.AiToolsExcludedProcesses ?? new List<string>())
            {
                AddExcludedProcess(name);
            }
            McpServers = McpServerStatus.Find();
            RefreshApprovedRecipes();
        }

        /// <summary>
        /// Programs (full paths) the user allowed to use Greenshot through greenshot-mcp, written back on save
        /// </summary>
        public ObservableCollection<AiToolClientViewModel> AiToolsAllowedClients { get; private set; }

        /// <summary>
        /// AI tools may use Greenshot at all (opt-in); everything else on the AI tools tab only matters when this is on
        /// </summary>
        public bool AiToolsEnabled
        {
            get => CoreConfiguration.AiToolsEnabled;
            set
            {
                if (CoreConfiguration.AiToolsEnabled != value)
                {
                    CoreConfiguration.AiToolsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Processes whose windows are never shared with AI tools, written back on save
        /// </summary>
        public ObservableCollection<string> AiToolsExcludedProcesses { get; private set; }

        private string _newExcludedProcess;

        /// <summary>
        /// The text of the "add an application" box
        /// </summary>
        public string NewExcludedProcess
        {
            get => _newExcludedProcess;
            set
            {
                if (_newExcludedProcess != value)
                {
                    _newExcludedProcess = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Adds a process name (".exe" is removed, case doesn't matter, duplicates are ignored); false when nothing was added
        /// </summary>
        public bool AddExcludedProcess(string processName)
        {
            string name = (processName ?? string.Empty).Trim().Trim('"');
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 4).Trim();
            }
            if (name.Length == 0 || AiToolsExcludedProcesses.Any(e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            AiToolsExcludedProcesses.Add(name);
            return true;
        }

        /// <summary>
        /// greenshot-mcp.exe: where it is looked for, and whether it is there
        /// </summary>
        public IReadOnlyList<McpServerStatus> McpServers { get; private set; }

        public bool IsMcpServerFound => McpServers.Any(m => m.Exists);

        /// <summary>
        /// Programs the user didn't allow in this Greenshot run, they aren't asked again until a restart
        /// </summary>
        public ObservableCollection<AiToolClientViewModel> DeniedAiToolClients { get; private set; }

        public bool HasDeniedAiToolClients => DeniedAiToolClients.Count > 0;

        /// <summary>
        /// Allow a program which was denied in this run (saved with the settings)
        /// </summary>
        public void AllowDeniedClient(AiToolClientViewModel client)
        {
            if (client == null) return;
            DeniedAiToolClients.Remove(client);
            AiToolAccess.ForgetDenied(client.Path);
            if (!AiToolsAllowedClients.Any(c => string.Equals(c.Path, client.Path, StringComparison.OrdinalIgnoreCase)))
            {
                AiToolsAllowedClients.Add(client);
            }
        }

        /// <summary>
        /// Ask again the next time the program connects
        /// </summary>
        public void AskAgain(AiToolClientViewModel client)
        {
            if (client == null) return;
            DeniedAiToolClients.Remove(client);
            AiToolAccess.ForgetDenied(client.Path);
        }

        public bool HasNoAiToolClients => AiToolsAllowedClients.Count == 0;

        /// <summary>
        /// The recipes from files, with their approval: shown in core, so approvals can be seen and revoked without the recipe editor
        /// </summary>
        public ObservableCollection<ApprovedRecipeViewModel> ApprovedRecipes { get; } = new ObservableCollection<ApprovedRecipeViewModel>();

        private ApprovedRecipeViewModel _selectedApprovedRecipe;

        public ApprovedRecipeViewModel SelectedApprovedRecipe
        {
            get => _selectedApprovedRecipe;
            set
            {
                if (_selectedApprovedRecipe != value)
                {
                    _selectedApprovedRecipe = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedApprovedRecipe));
                }
            }
        }

        public bool HasSelectedApprovedRecipe => _selectedApprovedRecipe != null;

        public void RefreshApprovedRecipes()
        {
            string selectedId = _selectedApprovedRecipe?.RecipeId;
            ApprovedRecipes.Clear();
            var manager = Greenshot.Recipes.RecipeManager.Instance;
            foreach (var recipe in manager.GetAllRecipes().Where(r => !string.IsNullOrEmpty(r.FilePath)).OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                var details = manager.GetRecipeDetails(recipe.Id);
                ApprovedRecipes.Add(new ApprovedRecipeViewModel(recipe, details));
            }
            // Automatic steps from files have their own approval
            foreach (var extension in manager.GetAllExtensions().Where(e => !string.IsNullOrEmpty(e.FilePath)).OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                ApprovedRecipes.Add(new ApprovedRecipeViewModel(extension.AsRecipeView(), manager.GetRecipeDetails(extension.Id)));
            }
            SelectedApprovedRecipe = ApprovedRecipes.FirstOrDefault(r => string.Equals(r.RecipeId, selectedId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Writes the AI tool settings, which aren't bound directly, back to the configuration
        /// </summary>
        public void SaveAiToolSettings()
        {
            // Programs allowed to use Greenshot through greenshot-mcp
            CoreConfiguration.AiToolsAllowedClients = AiToolsAllowedClients.Select(c => c.Path).ToList();
            if (!CoreConfiguration.IsConstant(nameof(ICoreConfiguration.AiToolsExcludedProcesses)))
            {
                CoreConfiguration.AiToolsExcludedProcesses = AiToolsExcludedProcesses.ToList();
            }
        }
    }
}
