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
using System.IO;
using System.Linq;
using Greenshot.Base.Recipes;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The example recipes in docs\vNext\examples are valid: what the documentation shows (and what goes into the recipe catalog) loads
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class ExampleRecipeTests
    {
        public static IEnumerable<object[]> ExampleFiles()
        {
            string directory = FindExamplesDirectory();
            if (directory == null)
            {
                yield break;
            }
            foreach (var file in Directory.GetFiles(directory, "*.gsrecipe.json").OrderBy(file => file))
            {
                yield return new object[] { Path.GetFileName(file) };
            }
        }

        [Theory]
        [MemberData(nameof(ExampleFiles))]
        public void ExampleRecipe_IsValid(string fileName)
        {
            string file = Path.Combine(FindExamplesDirectory(), fileName);

            var recipe = RecipeSerializer.LoadFromFile(file, false);
            var result = RecipeValidator.Validate(recipe);

            Assert.True(result.IsValid, $"{fileName}: {string.Join("; ", result.Errors)}");
        }

        [Fact]
        public void HdrExamples_SaveAnHdrFile()
        {
            string directory = FindExamplesDirectory();
            Assert.NotNull(directory);
            foreach (var name in new[] { "hdr_region_to_file.gsrecipe.json", "hdr_window_to_file.gsrecipe.json" })
            {
                var recipe = RecipeSerializer.LoadFromFile(Path.Combine(directory, name), false);
                Assert.True(HdrOutput.SavesHdrFile(recipe), name);
                Assert.True(HdrOutput.CapturesHdr(recipe), name);
                Assert.DoesNotContain(RecipeValidator.Validate(recipe).Warnings, warning => warning.Contains("Hdr is false"));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void HdrRegion_OpenInEditor_OpensTheEditorInsteadOfSaving(bool openInEditor)
        {
            var recipe = RecipeSerializer.LoadFromFile(Path.Combine(FindExamplesDirectory(), "hdr_region_to_file.gsrecipe.json"), false);
            try
            {
                RecipeOptionStore.SetValue(recipe.Id, recipe.FindOption("openInEditor"), openInEditor);
                var context = new Greenshot.Base.Recipes.Pipeline.CaptureFlowContext(recipe);

                Assert.Equal(!openInEditor, recipe.Nodes.Single(node => node.StepType == WellKnownStepTypes.SaveFile).ShouldRun(context));
                Assert.Equal(!openInEditor, recipe.Nodes.Single(node => node.StepType == WellKnownStepTypes.Notification).ShouldRun(context));
                Assert.Equal(openInEditor, recipe.Nodes.Single(node => node.StepType == WellKnownStepTypes.Editor).ShouldRun(context));
            }
            finally
            {
                RecipeOptionStore.Reset(recipe.Id);
            }
        }

        [Theory]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void HdrRegion_AskHowToSave_OnlyWithTheOption_AndHdrContent(bool askHowToSave, bool withHdr, bool asks)
        {
            var recipe = RecipeSerializer.LoadFromFile(Path.Combine(FindExamplesDirectory(), "hdr_region_to_file.gsrecipe.json"), false);
            try
            {
                RecipeOptionStore.SetValue(recipe.Id, recipe.FindOption("askHowToSave"), askHowToSave);
                var capture = new Greenshot.Base.Core.Capture(new System.Drawing.Bitmap(4, 2));
                if (withHdr)
                {
                    var hdrImage = new Greenshot.Base.Core.HdrImage(4, 2, 240, 400);
                    hdrImage.Pixels[0, 0] = new SixLabors.ImageSharp.PixelFormats.HalfVector4(5, 5, 5, 1);
                    hdrImage.BelongTo(capture.Image);
                    capture.HdrImage = hdrImage;
                }
                using var context = new Greenshot.Base.Recipes.Pipeline.CaptureFlowContext(recipe)
                {
                    Payload = new Greenshot.Base.Recipes.Pipeline.CapturePayload(capture)
                };

                Assert.Equal(asks, recipe.Nodes.Single(node => node.StepType == WellKnownStepTypes.UserPrompt).ShouldRun(context));
            }
            finally
            {
                RecipeOptionStore.Reset(recipe.Id);
            }
        }

        [Theory]
        [InlineData("hdr_region_to_file.gsrecipe.json", false)]
        [InlineData("hdr_region_to_file.gsrecipe.json", true)]
        [InlineData("hdr_window_to_file.gsrecipe.json", true)]
        public void HdrExamples_AskForFilename_FollowsTheOption(string name, bool ask)
        {
            var recipe = RecipeSerializer.LoadFromFile(Path.Combine(FindExamplesDirectory(), name), false);
            try
            {
                RecipeOptionStore.SetValue(recipe.Id, recipe.FindOption("askForFilename"), ask);
                var save = recipe.Nodes.Single(node => node.StepType == WellKnownStepTypes.SaveFile);
                var resolved = new RecipeNodeConfig(save.Id, save.StepType)
                {
                    Parameters = Greenshot.Base.Recipes.Expressions.ExpressionEvaluator.Instance.ResolveParameters(save.Parameters, new Greenshot.Base.Recipes.Pipeline.CaptureFlowContext(recipe))
                };

                Assert.Equal(ask, resolved.GetParameter<bool?>("AskForFilename"));
                // The notification of the save comes after it
                Assert.Contains(recipe.Nodes, node => node.StepType == WellKnownStepTypes.Notification);
            }
            finally
            {
                RecipeOptionStore.Reset(recipe.Id);
            }
        }

        /// <summary>
        /// docs\vNext\examples of the repository, found from the test's directory
        /// </summary>
        private static string FindExamplesDirectory()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                string examples = Path.Combine(directory.FullName, "docs", "vNext", "examples");
                if (Directory.Exists(examples))
                {
                    return examples;
                }
                directory = directory.Parent;
            }
            return null;
        }
    }
}
