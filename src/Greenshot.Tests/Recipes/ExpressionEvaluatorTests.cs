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

using System.Collections.Generic;
using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Expressions;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class ExpressionEvaluatorTests
    {
        public ExpressionEvaluatorTests()
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

            var result = ExpressionEvaluator.Instance.Evaluate("prefix_${context.CustomTag}_${context.Count}", context);
            Assert.Equal("prefix_BugReport_42", result);
        }

        [Fact]
        public void Evaluate_MathAndComparisons_ComputesExpectedResults()
        {
            var recipe = new CaptureRecipe("math_test", "Math Test");
            using var context = new CaptureFlowContext(recipe);
            context.Properties["Width"] = 1920;
            context.Properties["Height"] = 1080;

            bool isFhd = ExpressionEvaluator.Instance.Evaluate<bool>("${Width == 1920 && Height == 1080}", context);
            Assert.True(isFhd);

            bool is4k = ExpressionEvaluator.Instance.Evaluate<bool>("${Width >= 3840}", context);
            Assert.False(is4k);

            double area = ExpressionEvaluator.Instance.Evaluate<double>("${Width * Height}", context);
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

            int w = ExpressionEvaluator.Instance.Evaluate<int>("${payload.width}", context);
            int h = ExpressionEvaluator.Instance.Evaluate<int>("${payload.height}", context);
            string text = ExpressionEvaluator.Instance.Evaluate<string>("${payload.extractedtext}", context);
            string title = ExpressionEvaluator.Instance.Evaluate<string>("${payload.title}", context);

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
                { "Path", "${context.TargetFolder}\\${context.Prefix}_test.png" },
                { "StaticNum", 123 },
                { "ConditionMet", "${1 + 1 == 2}" }
            };

            var resolved = ExpressionEvaluator.Instance.ResolveParameters(rawParams, context);
            Assert.Equal("C:\\Captures\\GS_test.png", resolved["Path"]);
            Assert.Equal(123, resolved["StaticNum"]);
            Assert.Equal(true, resolved["ConditionMet"]);
        }
    }
}
