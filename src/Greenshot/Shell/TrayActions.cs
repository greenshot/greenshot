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
using System.Diagnostics;
using System.IO;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Recipes;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Shell
{
    /// <summary>
    /// The actions of the fixed items of the tray menu
    /// </summary>
    internal static class TrayActions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TrayActions));

        /// <summary>
        /// Open the "Support Greenshot" page
        /// </summary>
        public static void OpenDonatePage()
        {
            Process.Start("https://getgreenshot.org/support/?version=" + EnvironmentInfo.GetGreenshotVersion(true));
        }

        /// <summary>
        /// Open the recipe editor of the Recipe Editor plugin, when it is there
        /// </summary>
        public static void OpenRecipeEditor()
        {
            try
            {
                var editorService = SimpleServiceProvider.Current.GetInstance<IRecipeEditorService>(isOptional: true);
                editorService?.OpenEditor();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to open recipe editor window.", ex);
            }
        }

        /// <summary>
        /// Let the user pick a recipe file, approve it and add it to the recipe files
        /// </summary>
        public static void ImportRecipe()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = Texts.Recipe.ImportTitle ?? "Import Capture Recipe",
                Filter = RecipeSerializer.RecipeFileFilter,
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() != true || !File.Exists(openFileDialog.FileName))
            {
                return;
            }

            string recipePath = Path.GetFullPath(openFileDialog.FileName);
            var result = RecipeManager.Instance.LoadRecipeFromFile(recipePath, interactiveApproval: true, forceApprovalPrompt: true);
            if (!result.IsValid)
            {
                return;
            }

            var recipeConfig = RecipeConfigHelper.TryGetRecipeConfiguration();
            string existing = recipeConfig?.RecipeFiles ?? "";
            var configuredPaths = new List<string>();
            var currentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string configuredPath in existing.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string normalizedPath = Path.GetFullPath(configuredPath.Trim());
                    if (currentPaths.Add(normalizedPath))
                    {
                        configuredPaths.Add(normalizedPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Could not normalize configured recipe path '{configuredPath}'.", ex);
                }
            }

            if (currentPaths.Add(recipePath))
            {
                configuredPaths.Add(recipePath);
                if (recipeConfig != null)
                {
                    recipeConfig.RecipeFiles = string.Join(";", configuredPaths);
                }
                IniConfigRegistry.Get()?.Save();
            }
        }
    }
}
