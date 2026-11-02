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
using System.Drawing;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Recipes.Pipeline;

namespace Greenshot.Recipes.Views
{
    /// <summary>
    /// The capture shown in the destination window, as the editor which the editor recipes (Editor trigger) work on: like in the
    /// editor, a recipe takes the capture (source CurrentEditor) and puts the changed one in its place (TargetEditor CurrentEditor).
    /// It owns its surface, a copy; the surface is used on the UI thread.
    /// </summary>
    public sealed class EditableCapture : IImageEditor, IDisposable
    {
        private ISurface _surface;
        private bool _disposed;

        /// <param name="surface">A copy of the capture, owned by this from now on</param>
        public EditableCapture(ISurface surface)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        }

        /// <summary>
        /// A recipe changed the capture
        /// </summary>
        public bool IsChanged { get; private set; }

        /// <summary>
        /// The capture was replaced by a recipe, on the UI thread
        /// </summary>
        public event EventHandler Changed;

        public ISurface Surface
        {
            get => _surface;
            set
            {
                if (value == null || ReferenceEquals(value, _surface))
                {
                    return;
                }

                if (_disposed)
                {
                    // The window is gone, nobody uses the result
                    value.Dispose();
                    return;
                }

                var previous = _surface;
                _surface = value;
                IsChanged = true;
                previous.Dispose();
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public ICaptureDetails CaptureDetails => _surface.CaptureDetails;

        public Image GetImageForExport() => _surface.GetImageForExport();

        public bool TrySaveState(string filePath) => false;

        /// <summary>
        /// The editor recipes, as the editor shows them in its recipe menu
        /// </summary>
        public static IReadOnlyList<IEditorTrigger> GetEditorRecipes()
        {
            var triggerManager = SimpleServiceProvider.Current.GetInstance<ITriggerManager>(isOptional: true);
            var recipeManager = SimpleServiceProvider.Current.GetInstance<IRecipeManager>(isOptional: true);
            if (triggerManager == null || recipeManager == null)
            {
                return Array.Empty<IEditorTrigger>();
            }

            return (triggerManager.GetEditorTriggers() ?? Array.Empty<IEditorTrigger>())
                .Where(trigger => recipeManager.GetRecipeById(trigger.TargetRecipeId)?.IsEnabled == true)
                .OrderBy(trigger => trigger.Group, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(trigger => trigger.Order)
                .ThenBy(trigger => DisplayName(trigger), StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The text of the editor recipe, like in the editor menu
        /// </summary>
        public static string DisplayName(IEditorTrigger trigger) =>
            !string.IsNullOrWhiteSpace(trigger.MenuItemText) ? trigger.MenuItemText : trigger.Name;

        /// <summary>
        /// Runs the editor recipe on this capture, like a click in the editor's recipe menu
        /// </summary>
        /// <returns>The running flow, or null when the recipe is gone or switched off</returns>
        public CaptureFlowHandle RunRecipe(IEditorTrigger trigger)
        {
            var recipeManager = SimpleServiceProvider.Current.GetInstance<IRecipeManager>(isOptional: true);
            var recipe = recipeManager?.GetRecipeById(trigger.TargetRecipeId);
            if (recipe?.IsEnabled != true)
            {
                return null;
            }

            return CaptureFlowRunner.Current.Start(TriggerRecipePreparer.Prepare(recipe, trigger), FlowTriggerContext.Capture(trigger),
                context => context.Properties["EditorForm"] = this);
        }

        /// <summary>
        /// Hands the capture over, e.g. to the flow: the caller owns it from now on
        /// </summary>
        public ISurface Release()
        {
            var surface = _surface;
            _surface = null;
            _disposed = true;
            return surface;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _surface?.Dispose();
        }
    }
}
