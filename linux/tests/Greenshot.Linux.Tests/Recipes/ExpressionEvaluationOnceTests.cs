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

#nullable disable

using Greenshot.Linux.Core.Capturing;

using SkiaSharp;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Pipeline;
using Greenshot.Linux.Recipes.Steps;

namespace Greenshot.Linux.Tests.Recipes
{
    /// <summary>
    /// Expressions are evaluated exactly once. Values that come from data (OCR text, barcodes, page titles, arguments) may
    /// contain "${...}"; they must never be evaluated, otherwise that data could read environment or configuration values.
    /// </summary>
    public class ExpressionEvaluationOnceTests
    {
        /// <summary>Looks like an expression, e.g. text decoded from a malicious QR code</summary>
        private const string InjectedText = "${user.username}";

        public ExpressionEvaluationOnceTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static CaptureFlowContext CreateContextWithDataVariable()
        {
            var context = new CaptureFlowContext(new CaptureRecipe("expression_once", "Expression Once"));
            context.Properties["Barcode.Text"] = InjectedText;
            return context;
        }

        [Fact]
        public async Task StdoutStep_DoesNotEvaluateExpressionsInsideVariableValues()
        {
            using var context = CreateContextWithDataVariable();
            var output = new List<string>();
            context.StdoutWriter = text =>
            {
                output.Add(text);
                return Task.CompletedTask;
            };

            var node = new RecipeNodeConfig("print", WellKnownStepTypes.Stdout)
            {
                Parameters = new Dictionary<string, object> { ["text"] = "Decoded: ${Barcode.Text}" }
            };
            await SingleNodeRunner.RunAsync(node, cfg => new StdoutStep(cfg), context);

            Assert.Equal(new[] { "Decoded: " + InjectedText }, output);
        }

        [Fact]
        public async Task StderrStep_DoesNotEvaluateExpressionsInsideVariableValues()
        {
            using var context = CreateContextWithDataVariable();
            string stderr = null;
            context.StderrWriter = text =>
            {
                stderr = text;
                return Task.CompletedTask;
            };

            var node = new RecipeNodeConfig("fail", WellKnownStepTypes.Stderr)
            {
                Parameters = new Dictionary<string, object> { ["text"] = "${Barcode.Text}", ["abort"] = false }
            };
            await SingleNodeRunner.RunAsync(node, cfg => new StderrStep(cfg), context);

            Assert.Equal(InjectedText, stderr);
        }

        [Fact]
        public async Task SetVariableStep_DoesNotEvaluateExpressionsInsideVariableValues()
        {
            using var context = CreateContextWithDataVariable();

            var node = new RecipeNodeConfig("copy", WellKnownStepTypes.SetVariable)
            {
                Parameters = new Dictionary<string, object>
                {
                    ["Variables"] = new Dictionary<string, object> { ["Copy"] = "${Barcode.Text}", ["Other"] = "prefix ${Barcode.Text}" }
                }
            };
            await SingleNodeRunner.RunAsync(node, cfg => new SetVariableStep(cfg), context);

            Assert.Equal(InjectedText, context.Properties["Copy"]);
            Assert.Equal("prefix " + InjectedText, context.Properties["Other"]);
        }

        [Fact]
        public async Task FileCaptureSource_DoesNotEvaluateExpressionsInsideVariableValues()
        {
            using var context = CreateContextWithDataVariable();
            context.Properties["InputFile"] = InjectedText + ".png";

            var node = new RecipeNodeConfig("load", WellKnownStepTypes.Source)
            {
                Parameters = new Dictionary<string, object> { ["sourceType"] = "File", ["filename"] = "${InputFile}" }
            };
            using var scope = new HostScope();
            await SingleNodeRunner.RunAsync(node, cfg => new SourceAcquisitionStep(cfg), context);

            // The file name is used literally: the load fails for "${user.username}.png" instead of expanding it
            Assert.True(context.IsAborted);
            Assert.Contains(InjectedText + ".png", context.AbortReason);
        }

        [Fact]
        public async Task AnnotationStep_EvaluatesItsOwnParametersWithSurfaceVariables()
        {
            using var context = new CaptureFlowContext(new CaptureRecipe("annotation_once", "Annotation Once"))
            {
                Payload = new CapturePayload(new Capture(new SKBitmap(1000, 800), new CaptureDetails()))
            };

            // "w" and "h" only exist while the annotation step runs; the engine must leave the expressions to the step
            var node = new RecipeNodeConfig("qr", WellKnownStepTypes.Annotation)
            {
                Parameters = new Dictionary<string, object>
                {
                    ["Annotations"] = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            ["Type"] = "QRCode",
                            ["Text"] = "${Barcode.Text}",
                            ["HorizontalAnchor"] = "Right",
                            ["VerticalAnchor"] = "Bottom",
                            ["Width"] = "${w - 850}",
                            ["Height"] = "${h - 650}",
                            ["Margin"] = 25
                        }
                    }
                }
            };
            context.Properties["Barcode.Text"] = InjectedText;

            await SingleNodeRunner.RunAsync(node, cfg => new AnnotationStep(cfg), context);

            var element = context.Payload.EnsureSurface().Elements.Single();
            Assert.Equal(150, element.Bounds.Width);
            Assert.Equal(150, element.Bounds.Height);
            Assert.Equal(825, element.Bounds.Left);
            Assert.Equal(625, element.Bounds.Top);
        }
    }
}
