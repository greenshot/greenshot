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

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;

namespace Greenshot.Plugin.RecipeEditor.Dialogs
{
    /// <summary>
    /// Shows the details of a recipe in the recipe manager: what it does, its triggers, its approval and the changes against the
    /// built-in recipe it replaces.
    /// </summary>
    public sealed class RecipeDetailsWindow : Window
    {
        public RecipeDetailsWindow(CaptureRecipe recipe, RecipeDetails details)
        {
            Title = $"Recipe details: {recipe.Name ?? recipe.Id}";
            Width = 720;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = WpfThemeHelper.WindowBackground;

            var text = new TextBox
            {
                Text = Format(recipe, details),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Padding = new Thickness(10),
                BorderThickness = new Thickness(0),
                Background = WpfThemeHelper.CardBackground,
                Foreground = WpfThemeHelper.TextPrimary
            };
            Content = text;
        }

        internal static string Format(CaptureRecipe recipe, RecipeDetails details)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{recipe.Name} ({recipe.Id})");
            if (!string.IsNullOrWhiteSpace(recipe.Description))
            {
                builder.AppendLine(recipe.Description);
            }
            if (!string.IsNullOrEmpty(details.ProposedBy))
            {
                builder.AppendLine($"Written by the AI tool {details.ProposedBy}, approved by you.");
            }

            builder.AppendLine().AppendLine("What it does");
            foreach (var line in details.WhatItDoes)
            {
                builder.AppendLine("  " + line);
            }

            builder.AppendLine().AppendLine("What starts it");
            if (details.Triggers.Count == 0)
            {
                builder.AppendLine("  Only from the recipe list");
            }
            foreach (var trigger in details.Triggers)
            {
                builder.AppendLine("  " + trigger);
            }

            builder.AppendLine().AppendLine("Approval");
            if (string.IsNullOrEmpty(recipe.FilePath))
            {
                builder.AppendLine("  Built into Greenshot, no approval needed.");
            }
            else if (details.ApprovedAt == null)
            {
                builder.AppendLine("  Not approved.");
            }
            else
            {
                builder.AppendLine($"  File: {recipe.FilePath}");
                builder.AppendLine($"  Approved: {details.ApprovedAt:yyyy-MM-dd HH:mm:ss}");
                builder.AppendLine($"  SHA-256: {details.ApprovedHash}");
                builder.AppendLine(details.IsApprovalCurrent ? "  The file is the approved version." : "  The file was changed since, it will be shown for approval.");
                builder.AppendLine(details.Permissions.Count == 0 ? "  Permissions: none" : $"  Permissions: {string.Join(", ", details.Permissions)}");
            }

            if (details.BuiltInDiff != null)
            {
                builder.AppendLine().AppendLine("Changes against the built-in recipe it replaces");
                builder.AppendLine(details.BuiltInDiff);
            }
            return builder.ToString();
        }
    }
}
