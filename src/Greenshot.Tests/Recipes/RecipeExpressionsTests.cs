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

using System.Collections.Generic;
using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Hdr;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Expressions;
using Greenshot.Base.Recipes.Pipeline;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class RecipeExpressionsTests
    {
        public RecipeExpressionsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Evaluate_StringInterpolationAndContextProperties_ResolvesValues()
        {
            var recipe = new CaptureRecipe("expr_test", "Expression Test");
            using var context = new CaptureFlowContext(recipe);
            context.Properties["CustomTag"] = "BugReport";
            context.Properties["Count"] = 42;

            var result = RecipeExpressions.Evaluate("prefix_${var.CustomTag}_${var.Count}", context);
            Assert.Equal("prefix_BugReport_42", result);
        }

        [Fact]
        public void Evaluate_MathAndComparisons_ComputesExpectedResults()
        {
            var recipe = new CaptureRecipe("math_test", "Math Test");
            using var context = new CaptureFlowContext(recipe);
            context.Properties["Width"] = 1920;
            context.Properties["Height"] = 1080;

            bool isFhd = RecipeExpressions.Evaluate<bool>("${var.Width == 1920 && var.Height == 1080}", context);
            Assert.True(isFhd);

            bool is4k = RecipeExpressions.Evaluate<bool>("${var.Width >= 3840}", context);
            Assert.False(is4k);

            double area = RecipeExpressions.Evaluate<double>("${var.Width * var.Height}", context);
            Assert.Equal(1920 * 1080, area);
        }

        [Fact]
        public void Evaluate_PayloadDimensionsAndExtractedText_ResolvesCorrectly()
        {
            var recipe = new CaptureRecipe("payload_expr_test", "Payload Expr Test");
            using var bmp = new Bitmap(800, 600);
            var capture = new Capture((Image)bmp.Clone());
            capture.CaptureDetails.Title = "Application Window";

            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
                {
                    ExtractedText = "Error Code: 404"
                }
            };

            int w = RecipeExpressions.Evaluate<int>("${payload.width}", context);
            int h = RecipeExpressions.Evaluate<int>("${payload.height}", context);
            string text = RecipeExpressions.Evaluate<string>("${payload.ocr.text}", context);
            string title = RecipeExpressions.Evaluate<string>("${payload.title}", context);

            Assert.Equal(800, w);
            Assert.Equal(600, h);
            Assert.Equal("Error Code: 404", text);
            Assert.Equal("Application Window", title);
        }

        [Fact]
        public void ResolveParameters_DictionaryInterpolation_ResolvesAllExpressions()
        {
            var recipe = new CaptureRecipe("param_res_test", "Param Resolve Test");
            using var context = new CaptureFlowContext(recipe);
            context.Properties["TargetFolder"] = "C:\\Captures";
            context.Properties["Prefix"] = "GS";

            var rawParams = new Dictionary<string, object>
            {
                { "Path", "${var.TargetFolder}\\${var.Prefix}_test.png" },
                { "StaticNum", 123 },
                { "ConditionMet", "${1 + 1 == 2}" }
            };

            var resolved = RecipeExpressions.ResolveParameters(rawParams, context);
            Assert.Equal("C:\\Captures\\GS_test.png", resolved["Path"]);
            Assert.Equal(123, resolved["StaticNum"]);
            Assert.Equal(true, resolved["ConditionMet"]);
        }

        [Theory]
        [InlineData(true, "Available")]
        [InlineData(false, "None")]
        public void Evaluate_PayloadHdr_TellsWhetherTheCaptureHasHdrContent(bool withHdr, string expected)
        {
            var capture = new Capture(new Bitmap(4, 2));
            if (withHdr)
            {
                var hdrImage = new HdrImage(4, 2, 240, 400);
                hdrImage.Pixels[0, 0] = new SixLabors.ImageSharp.PixelFormats.HalfVector4(5, 5, 5, 1);
                hdrImage.BelongTo(capture.Image);
                capture.HdrImage = hdrImage;
            }
            using var context = new CaptureFlowContext(new CaptureRecipe("hdr_test", "HDR Test"))
            {
                Payload = new CapturePayload(capture)
            };

            Assert.Equal(expected, RecipeExpressions.Evaluate("${payload.hdr}", context));
            Assert.Equal(withHdr, RecipeExpressions.IsMet("${payload.hdr == 'Available'}", context));
        }

        [Fact]
        public void HdrState_OfAnHdrImageWhichNoLongerBelongsToTheImage_IsLost()
        {
            using var image = new Bitmap(4, 2);
            using var changed = new Bitmap(4, 2);
            using var hdrImage = new HdrImage(4, 2, 240, 400);
            hdrImage.Pixels[0, 0] = new SixLabors.ImageSharp.PixelFormats.HalfVector4(5, 5, 5, 1);
            hdrImage.BelongTo(image);

            Assert.Equal(HdrOutput.StateAvailable, HdrOutput.StateOf(hdrImage, image));
            Assert.Equal(HdrOutput.StateLost, HdrOutput.StateOf(hdrImage, changed));
            Assert.Equal(HdrOutput.StateNone, HdrOutput.StateOf(null, image));
        }

        [Fact]
        public void Evaluate_ChoiceOfAPromptWhichDidntRun_IsTheDefault()
        {
            using var context = new CaptureFlowContext(new CaptureRecipe("choice_test", "Choice Test"));
            const string hdrFormat = "${(var.UserChoice.ask == 'Png' ? 'None' : 'Auto')}";

            Assert.Equal("Auto", RecipeExpressions.Evaluate(hdrFormat, context));
            context.Properties["UserChoice.ask"] = "Png";
            Assert.Equal("None", RecipeExpressions.Evaluate(hdrFormat, context));
        }

        [Fact]
        public void Validate_ANameOfTheOldEvaluator_IsAnError()
        {
            const string json = @"{
  ""kind"": ""recipe"", ""version"": ""2.0"", ""id"": ""old_names"", ""name"": ""Old names"",
  ""nodes"": [ { ""id"": ""a"", ""stepType"": ""SetVariable"", ""when"": ""${Barcode.Text == 'x'}"" } ],
  ""flow"": { ""startNodes"": [ ""a"" ] }
}";
            var recipe = RecipeSerializer.Deserialize(json, false);

            var result = RecipeValidator.Validate(recipe);

            Assert.Contains(result.Errors, e => e.Contains("Barcode.Text"));
        }
    }
}
