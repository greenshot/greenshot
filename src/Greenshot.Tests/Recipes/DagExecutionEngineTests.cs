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
using System.Drawing;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Pipeline;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class DagExecutionEngineTests
    {
        public DagExecutionEngineTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public async Task ExecuteAsync_LinearPipeline_ExecutesInOrder()
        {
            var recipe = new CaptureRecipe("linear_recipe", "Linear Pipeline")
                .AddNode(new RecipeNodeConfig { Id = "node_1", StepType = "Step1" })
                .AddNode(new RecipeNodeConfig { Id = "node_2", StepType = "Step2" })
                .AddNode(new RecipeNodeConfig { Id = "node_3", StepType = "Step3" });

            recipe.Flow = new RecipeFlowConfig("node_1")
                .AddTransition("node_1", "node_2")
                .AddTransition("node_2", "node_3");

            using var bmp = new Bitmap(10, 10);
            var capture = new Capture((Image)bmp.Clone());
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };

            var executionLog = new List<string>();

            var engine = new DagExecutionEngine(nodeConfig =>
            {
                return new MockTestStep(nodeConfig.Id, ctx =>
                {
                    executionLog.Add(nodeConfig.Id);
                    return Task.CompletedTask;
                });
            });

            await engine.ExecuteAsync(recipe, context);

            Assert.Equal(new[] { "node_1", "node_2", "node_3" }, executionLog);
            Assert.False(context.IsAborted);
        }

        [Fact]
        public async Task ExecuteAsync_AsymmetricDiamond_ExecutesStrictlyDepthFirst()
        {
            var recipe = new CaptureRecipe("dfs_diamond", "DFS Diamond")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Start" })
                .AddNode(new RecipeNodeConfig { Id = "b1", StepType = "B1" })
                .AddNode(new RecipeNodeConfig { Id = "b2", StepType = "B2" })
                .AddNode(new RecipeNodeConfig { Id = "c1", StepType = "C1" })
                .AddNode(new RecipeNodeConfig { Id = "join", StepType = "Join" });

            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "b1")
                .AddTransition("start", "c1")
                .AddTransition("b1", "b2")
                .AddTransition("b2", "join")
                .AddTransition("c1", "join");

            using var bmp = new Bitmap(10, 10);
            var capture = new Capture((Image)bmp.Clone());
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };

            var executionLog = new List<string>();

            var engine = new DagExecutionEngine(nodeConfig =>
            {
                return new MockTestStep(nodeConfig.Id, ctx =>
                {
                    executionLog.Add(nodeConfig.Id);
                    return Task.CompletedTask;
                });
            });

            await engine.ExecuteAsync(recipe, context);

            // Declaration order: b1 branch comes first, so depth-first is start -> b1 -> b2 -> c1 -> join
            Assert.Equal(new[] { "start", "b1", "b2", "c1", "join" }, executionLog);
        }

        [Fact]
        public async Task ExecuteAsync_NonMergingSplit_ExecutesWithIsolatedContexts()
        {
            var recipe = new CaptureRecipe("split_isolated", "Split Isolated")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Start" })
                .AddNode(new RecipeNodeConfig { Id = "branch_a", StepType = "BranchA" })
                .AddNode(new RecipeNodeConfig { Id = "branch_b", StepType = "BranchB" });

            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "branch_a")
                .AddTransition("start", "branch_b");

            using var bmp = new Bitmap(10, 10);
            var capture = new Capture((Image)bmp.Clone());
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };
            context.Payload.EnsureSurface();

            CaptureFlowContext ctxA = null;
            CaptureFlowContext ctxB = null;

            var engine = new DagExecutionEngine(nodeConfig =>
            {
                return new MockTestStep(nodeConfig.Id, ctx =>
                {
                    if (nodeConfig.Id == "branch_a")
                    {
                        ctxA = ctx;
                        ctx.Properties["branch"] = "A";
                    }
                    else if (nodeConfig.Id == "branch_b")
                    {
                        ctxB = ctx;
                        ctx.Properties["branch"] = "B";
                    }
                    return Task.CompletedTask;
                });
            });

            await engine.ExecuteAsync(recipe, context);

            Assert.NotNull(ctxA);
            Assert.NotNull(ctxB);
            Assert.False(ReferenceEquals(ctxA, ctxB));
            Assert.False(ReferenceEquals(ctxA.Payload, ctxB.Payload));
            Assert.False(ReferenceEquals(ctxA.Payload.Surface, ctxB.Payload.Surface));
            Assert.Equal("A", ctxA.Properties["branch"]);
            Assert.Equal("B", ctxB.Properties["branch"]);
        }

        [Fact]
        public async Task ExecuteAsync_ConditionalBranching_BypassesUnselectedBranch()
        {
            var recipe = new CaptureRecipe("conditional_recipe", "Conditional Recipe")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Start" })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "cond_node",
                    StepType = WellKnownStepTypes.Conditional,
                    Parameters = new Dictionary<string, object>
                    {
                        {
                            "Branches", new List<Dictionary<string, object>>
                            {
                                new Dictionary<string, object> { { "Key", "BranchA" }, { "Expression", "" } },
                                new Dictionary<string, object> { { "Key", "BranchB" }, { "Expression", "default" } }
                            }
                        }
                    }
                })
                .AddNode(new RecipeNodeConfig { Id = "action_a", StepType = "ActionA" })
                .AddNode(new RecipeNodeConfig { Id = "action_b", StepType = "ActionB" })
                .AddNode(new RecipeNodeConfig { Id = "join_node", StepType = "Join" });

            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "cond_node")
                .AddTransition("action_a", "join_node")
                .AddTransition("action_b", "join_node")
                .AddConditionalTransition("cond_node", "BranchA", "action_a")
                .AddConditionalTransition("cond_node", "BranchB", "action_b");

            using var bmp = new Bitmap(10, 10);
            var capture = new Capture((Image)bmp.Clone());
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };
            context.Properties["CustomScore"] = 75; // Matches BranchA

            var executed = new List<string>();

            var engine = new DagExecutionEngine(nodeConfig =>
            {
                return new MockTestStep(nodeConfig.Id, ctx =>
                {
                    executed.Add(nodeConfig.Id);
                    return Task.CompletedTask;
                });
            });

            await engine.ExecuteAsync(recipe, context);

            Assert.Contains("start", executed);
            Assert.Contains("cond_node", executed);
            Assert.Contains("action_a", executed);
            Assert.DoesNotContain("action_b", executed);
            Assert.Contains("join_node", executed);
        }

        [Fact]
        public async Task ExecuteAsync_StepError_AbortsFlowContext()
        {
            var recipe = new CaptureRecipe("error_recipe", "Error Recipe")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Start" })
                .AddNode(new RecipeNodeConfig { Id = "fail_node", StepType = "Fail" })
                .AddNode(new RecipeNodeConfig { Id = "never_reached", StepType = "Never" });

            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "fail_node")
                .AddTransition("fail_node", "never_reached");

            using var bmp = new Bitmap(10, 10);
            var capture = new Capture((Image)bmp.Clone());
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };

            var engine = new DagExecutionEngine(nodeConfig =>
            {
                return new MockTestStep(nodeConfig.Id, ctx =>
                {
                    if (nodeConfig.Id == "fail_node")
                    {
                        throw new InvalidOperationException("Simulated step failure");
                    }
                    return Task.CompletedTask;
                });
            });

            await engine.ExecuteAsync(recipe, context);

            Assert.True(context.IsAborted);
            Assert.Contains("Simulated step failure", context.AbortReason);
        }

        [Fact]
        public async Task ExecuteAsync_StepError_WithMatchingErrorTransition_RoutesToErrorHandlerNode()
        {
            var recipe = new CaptureRecipe("error_routed_recipe", "Error Routed Recipe")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Start" })
                .AddNode(new RecipeNodeConfig { Id = "clipboard_export", StepType = "Export" })
                .AddNode(new RecipeNodeConfig { Id = "success_notify", StepType = "Notify" })
                .AddNode(new RecipeNodeConfig { Id = "error_fallback_dest", StepType = "DynamicDestination" });

            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "clipboard_export")
                .AddTransition("clipboard_export", "success_notify")
                .AddErrorTransition("clipboard_export", "error_fallback_dest", "ClipboardException");

            using var bmp = new Bitmap(10, 10);
            var capture = new Capture((Image)bmp.Clone());
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };

            var executed = new List<string>();

            var engine = new DagExecutionEngine(nodeConfig =>
            {
                return new MockTestStep(nodeConfig.Id, ctx =>
                {
                    executed.Add(nodeConfig.Id);
                    if (nodeConfig.Id == "clipboard_export")
                    {
                        throw new ClipboardException("The clipboard is currently in use by Excel.exe", "Excel.exe");
                    }
                    return Task.CompletedTask;
                });
            });

            await engine.ExecuteAsync(recipe, context);

            Assert.Contains("start", executed);
            Assert.Contains("clipboard_export", executed);
            Assert.DoesNotContain("success_notify", executed); // Bypassed
            Assert.Contains("error_fallback_dest", executed);  // Error handler executed!
            Assert.False(context.IsAborted);                   // Handled gracefully!
            Assert.Equal("The clipboard is currently in use by Excel.exe", context.Properties["LastError"]);
            Assert.Equal("clipboard_export", context.Properties["FailedNodeId"]);
        }

        [Fact]
        public async Task ExecuteAsync_StepError_WithNodeLevelFallbackParameter_RoutesToErrorHandlerNode()
        {
            var failNode = new RecipeNodeConfig { Id = "fail_node", StepType = "Export" };
            failNode.Set("OnErrorNodeId", "recovery_node");

            var recipe = new CaptureRecipe("node_fallback_recipe", "Node Fallback Recipe")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Start" })
                .AddNode(failNode)
                .AddNode(new RecipeNodeConfig { Id = "normal_next", StepType = "Next" })
                .AddNode(new RecipeNodeConfig { Id = "recovery_node", StepType = "Recovery" });

            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "fail_node")
                .AddTransition("fail_node", "normal_next");

            using var bmp = new Bitmap(10, 10);
            var capture = new Capture((Image)bmp.Clone());
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };

            var executed = new List<string>();

            var engine = new DagExecutionEngine(nodeConfig =>
            {
                return new MockTestStep(nodeConfig.Id, ctx =>
                {
                    executed.Add(nodeConfig.Id);
                    if (nodeConfig.Id == "fail_node")
                    {
                        throw new InvalidOperationException("Export failed");
                    }
                    return Task.CompletedTask;
                });
            });

            await engine.ExecuteAsync(recipe, context);

            Assert.Contains("start", executed);
            Assert.Contains("fail_node", executed);
            Assert.DoesNotContain("normal_next", executed);
            Assert.Contains("recovery_node", executed);
            Assert.False(context.IsAborted);
            Assert.Equal("Export failed", context.Properties["LastError"]);
        }
    }
}
