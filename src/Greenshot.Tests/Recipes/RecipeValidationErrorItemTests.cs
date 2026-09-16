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

using Greenshot.UI;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class RecipeValidationErrorItemTests
    {
        [Fact]
        public void Create_LegacyDrawableError_ReturnsSpecificAnnotationHint()
        {
            string errorMessage = "Recipe validation failed: Node 'watermark_node' uses stepType 'Drawable' which is not available because the required extension/plugin is not installed or active.";
            var item = RecipeValidationErrorItem.Create(errorMessage);

            Assert.Equal(errorMessage, item.ErrorMessage);
            Assert.True(item.HasHint);
            Assert.Contains("renamed to 'Annotation'", item.DiagnosticHint);
            Assert.Contains("'annotations'", item.DiagnosticHint);
        }

        [Fact]
        public void Create_UnknownStepTypePluginError_ReturnsPluginHint()
        {
            string errorMessage = "Node 'ocr_node' uses stepType 'OcrPlugin' which is not available because the required extension/plugin is not installed or active.";
            var item = RecipeValidationErrorItem.Create(errorMessage);

            Assert.True(item.HasHint);
            Assert.Contains("Settings > Plugins", item.DiagnosticHint);
        }

        [Fact]
        public void Create_UnknownSourceType_ReturnsSourceTypeHint()
        {
            string errorMessage = "Node 'src' [Source]: Unknown SourceType 'Desktop'.";
            var item = RecipeValidationErrorItem.Create(errorMessage);

            Assert.True(item.HasHint);
            Assert.Contains("Screen, Window, Region", item.DiagnosticHint);
        }

        [Fact]
        public void Create_MissingTargetTransition_ReturnsTargetHint()
        {
            string errorMessage = "Transition targets node 'step_2' which does not exist in the recipe.";
            var item = RecipeValidationErrorItem.Create(errorMessage);

            Assert.True(item.HasHint);
            Assert.Contains("target node ID", item.DiagnosticHint);
        }

        [Fact]
        public void Create_JsonSyntaxError_ReturnsJsonHint()
        {
            string errorMessage = "JsonReaderException: Unexpected character encountered while parsing value: { at line 12, position 4.";
            var item = RecipeValidationErrorItem.Create(errorMessage);

            Assert.True(item.HasHint);
            Assert.Contains("Check the recipe JSON syntax", item.DiagnosticHint);
        }
    }
}
