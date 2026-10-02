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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Helpers.Ipc;
using Greenshot.Recipes;
using Greenshot.Tests.Recipes;
using Greenshot.UI;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    /// <summary>
    /// Recipes written by AI tools: the commands are only for greenshot-mcp.exe with consent, a proposal is only saved when the user
    /// approves it, and then only with the triggers and permissions the user switched on.
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class AiRecipeIpcHandlerTests
    {
        private static readonly AiToolClient TestClient = new AiToolClient
        {
            ExePath = @"C:\Test\AiRecipeTool.exe",
            DisplayName = "Test AI tool"
        };

        public AiRecipeIpcHandlerTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        [InlineData("RECIPE_CATALOG")]
        [InlineData("VALIDATE_RECIPE")]
        [InlineData("PROPOSE_RECIPE")]
        public void RecipeCommands_AreOnlyForMcp_AndNeedConsent(string command)
        {
            Assert.True(IpcSecurityDispatcher.IsCommandAllowedForSource(command, IpcSources.Mcp));
            Assert.True(IpcSecurityDispatcher.RequiresAiToolConsent(command, IpcSources.Mcp));
            foreach (var source in new[] { "cli", "url_scheme", "native_messaging", "open_with", null })
            {
                Assert.False(IpcSecurityDispatcher.IsCommandAllowedForSource(command, source));
            }
        }

        [Fact]
        public void Parse_NeedsExactlyOneValidRecipe()
        {
            Assert.NotNull(AiRecipeIpcHandler.Parse(null).Error);
            Assert.NotNull(AiRecipeIpcHandler.Parse("not json").Error);

            string one = RecipeSerializer.Serialize(CreateProposal("parse_one"));
            string two = RecipeSerializer.SerializeList(new[] { CreateProposal("parse_a"), CreateProposal("parse_b") });
            Assert.NotNull(AiRecipeIpcHandler.Parse(two).Error);

            var parsed = AiRecipeIpcHandler.Parse(one);
            Assert.Null(parsed.Error);
            Assert.True(parsed.Validation.IsValid, string.Join("; ", parsed.Validation.Errors));
            Assert.Equal("parse_one", parsed.Recipe.Id);
        }

        [Fact]
        public void Parse_IgnoresWhereTheJsonSaysTheRecipeComesFrom()
        {
            var recipe = CreateProposal("parse_origin");
            recipe.IsBuiltIn = true;
            recipe.IsOverridden = true;
            recipe.FilePath = @"C:\Windows\evil.gsrecipe.json";

            var parsed = AiRecipeIpcHandler.Parse(RecipeSerializer.Serialize(recipe));

            Assert.Null(parsed.Recipe.FilePath);
            Assert.False(parsed.Recipe.IsBuiltIn);
            Assert.False(parsed.Recipe.IsOverridden);
            Assert.DoesNotContain("evil", parsed.Content);
        }

        [Fact]
        public void ResolveTarget_NewRecipe_GoesToTheAiDirectory_AndNeedsAnUnusedId()
        {
            WithAiDirectory(directory =>
            {
                var recipeManager = RecipeManager.Instance;
                var existing = recipeManager.GetAllRecipes().First();

                var taken = AiRecipeIpcHandler.ResolveTarget(recipeManager, CreateProposal(existing.Id), null);
                Assert.NotNull(taken.Error);

                var target = AiRecipeIpcHandler.ResolveTarget(recipeManager, CreateProposal("brand_new_ai_recipe"), null);
                Assert.Null(target.Error);
                Assert.Equal(directory, Path.GetDirectoryName(target.FilePath));
                Assert.Null(target.Replaced);

                var unknown = AiRecipeIpcHandler.ResolveTarget(recipeManager, CreateProposal("x"), "no_such_recipe");
                Assert.NotNull(unknown.Error);
            });
        }

        [Fact]
        public void ResolveTarget_BuiltInRecipe_IsReplacedWithAFileInTheAiDirectory()
        {
            WithAiDirectory(directory =>
            {
                var recipeManager = RecipeManager.Instance;
                var builtIn = recipeManager.GetAllRecipes().First(r => r.IsBuiltIn && !r.IsOverridden && string.IsNullOrEmpty(r.FilePath));

                var proposal = CreateProposal("some_other_id");
                var target = AiRecipeIpcHandler.ResolveTarget(recipeManager, proposal, builtIn.Id);

                Assert.Null(target.Error);
                Assert.True(target.ReplacesBuiltIn);
                Assert.Equal(builtIn.Id, proposal.Id);
                Assert.Same(builtIn, target.Replaced);
                Assert.Equal(directory, Path.GetDirectoryName(target.FilePath));
            });
        }

        [Fact]
        public async Task ValidateRecipe_ListsWhatTheUserWillBeAskedToAllow()
        {
            var recipe = CreateProposal("validate_ai_recipe")
                .AddNode(new RecipeNodeConfig
                {
                    Id = "cmd_step",
                    StepType = "ExternalCommand",
                    Parameters = new Dictionary<string, object> { { "CommandLine", @"C:\Tools\optimize.exe" }, { "Arguments", "\"{0}\"" } }
                });
            recipe.Flow.AddTransition("source", "cmd_step");

            await WithConsentAsync(async () =>
            {
                var reply = await DispatchAsync("VALIDATE_RECIPE", new Dictionary<string, string> { ["recipe"] = RecipeSerializer.Serialize(recipe) });
                Assert.Equal("ok", reply.Value<string>("status"));
                Assert.True(reply.Value<bool>("valid"));
                var needs = Assert.IsType<JArray>(reply["needs_permission"]);
                Assert.Contains(needs, n => n.Value<string>("kind") == nameof(RecipeGateType.ExternalCommand));
            });
        }

        [Fact]
        public async Task ProposeRecipe_WithoutConsent_IsRejected_WithoutShowingIt()
        {
            bool shown = false;
            await WithProposalPromptAsync((request, cancellationToken) =>
            {
                shown = true;
                return Task.FromResult<RecipeApprovalWindow.ApprovalResult>(null);
            }, async () =>
            {
                var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
                var previousAllowed = config.AiToolsAllowedClients;
                var previousPrompt = AiToolAccess.ConsentPrompt;
                try
                {
                    config.AiToolsAllowedClients = new List<string>();
                    AiToolAccess.ResetDeniedClients();
                    AiToolAccess.ConsentPrompt = (client, cancellationToken) => Task.FromResult(false);

                    var reply = await DispatchAsync("PROPOSE_RECIPE", ProposalParameters(CreateProposal("no_consent_recipe")));
                    Assert.Equal("error", reply.Value<string>("status"));
                    Assert.False(shown);
                }
                finally
                {
                    AiToolAccess.ConsentPrompt = previousPrompt;
                    AiToolAccess.ResetDeniedClients();
                    config.AiToolsAllowedClients = previousAllowed;
                }
            });
        }

        [Fact]
        public async Task ProposeRecipe_Rejected_IsNotSaved_AndNotShownAgain()
        {
            var shown = new List<RecipeApprovalRequest>();
            await WithProposalPromptAsync((request, cancellationToken) =>
            {
                shown.Add(request);
                return Task.FromResult<RecipeApprovalWindow.ApprovalResult>(null);
            }, async () =>
            {
                var parameters = ProposalParameters(CreateProposal("rejected_ai_recipe"));
                var reply = await DispatchAsync("PROPOSE_RECIPE", parameters);
                Assert.Equal("ok", reply.Value<string>("status"));
                Assert.Equal("rejected", reply.Value<string>("decision"));

                var request = Assert.Single(shown);
                Assert.True(request.IsAiProposal);
                Assert.True(request.StartSwitchedOff);
                Assert.Equal(TestClient.DisplayName, request.ProposedByName);
                Assert.Equal("Capture the screen to the clipboard", request.AiRequest);
                // What is shown is what would be hashed and saved
                Assert.Equal(RecipeTrustStore.ComputeSha256(new UTF8Encoding(false).GetBytes(request.Content)), request.ContentHash);
                Assert.False(File.Exists(request.FilePath));
                Assert.Null(RecipeManager.Instance.GetRecipeById("rejected_ai_recipe"));

                var again = await DispatchAsync("PROPOSE_RECIPE", parameters);
                Assert.Equal("error", again.Value<string>("status"));
                Assert.Single(shown);
            });
        }

        [Fact]
        public async Task ProposeRecipe_Approved_IsSaved_WithOnlyTheApprovedTriggers()
        {
            const string id = "approved_ai_recipe";
            var recipe = CreateProposal(id)
                .AddTrigger(new TriggerConfig(TriggerConfig.TypeManual))
                .AddTrigger(TriggerConfig.CreateClipboard());

            await WithProposalPromptAsync((request, cancellationToken) =>
            {
                var approval = new RecipeApproval { ApprovedTriggers = new List<string> { "0:" + TriggerConfig.TypeManual } };
                return Task.FromResult(new RecipeApprovalWindow.ApprovalResult(approval, false));
            }, async () =>
            {
                try
                {
                    var reply = await DispatchAsync("PROPOSE_RECIPE", ProposalParameters(recipe));
                    Assert.Equal("ok", reply.Value<string>("status"));
                    Assert.Equal("approved", reply.Value<string>("decision"));
                    Assert.Single(reply.Value<JArray>("triggers_on"));
                    Assert.Single(reply.Value<JArray>("triggers_off"));

                    string file = reply.Value<string>("file");
                    Assert.True(File.Exists(file));

                    var loaded = RecipeManager.Instance.GetRecipeById(id);
                    Assert.NotNull(loaded);
                    Assert.True(loaded.Triggers[0].IsActive);
                    Assert.False(loaded.Triggers[1].IsActive);
                    // The approval isn't in the file, the file says the trigger is enabled
                    Assert.True(loaded.Triggers[1].Enabled);

                    var record = RecipeTrustStore.GetTrustRecord(file);
                    Assert.True(record.IsAiCreated);
                    Assert.Equal(RecipeTrustStore.ComputeSha256(file), record.Sha256Hash);
                }
                finally
                {
                    RecipeManager.Instance.UnregisterRecipe(id);
                }
            });
        }

        [Fact]
        public async Task RecipeCatalog_ListsStepsAndRecipes_OrTheJsonOfOneRecipe()
        {
            await WithConsentAsync(async () =>
            {
                var catalog = await DispatchAsync("RECIPE_CATALOG", null);
                Assert.Equal("ok", catalog.Value<string>("status"));
                Assert.Contains(catalog.Value<JArray>("steps"), s => s.Value<string>("step_type") == "Source");
                var recipes = catalog.Value<JArray>("recipes");
                Assert.NotEmpty(recipes);

                string id = recipes[0].Value<string>("id");
                var one = await DispatchAsync("RECIPE_CATALOG", new Dictionary<string, string> { ["recipe"] = id });
                Assert.Equal("ok", one.Value<string>("status"));
                Assert.Equal(id, RecipeSerializer.Deserialize(one.Value<string>("recipe_json"), validate: false).Id);
            });
        }

        private static CaptureRecipe CreateProposal(string id)
        {
            var recipe = RecipeApprovalTests.CreateRecipe(id)
                .AddNode(new RecipeNodeConfig { Id = "clip", StepType = "Clipboard" });
            recipe.Flow.AddTransition("source", "clip");
            return recipe;
        }

        private static Dictionary<string, string> ProposalParameters(CaptureRecipe recipe)
        {
            return new Dictionary<string, string>
            {
                ["recipe"] = RecipeSerializer.Serialize(recipe),
                ["request"] = "Capture the screen to the clipboard",
                ["explanation"] = "Captures the full screen and copies it."
            };
        }

        private static void WithAiDirectory(Action<string> test)
        {
            string directory = Path.Combine(Path.GetTempPath(), "GreenshotAiRecipes_" + Guid.NewGuid().ToString("N"));
            var previous = AiRecipeIpcHandler.AiRecipeDirectoryProvider;
            try
            {
                AiRecipeIpcHandler.AiRecipeDirectoryProvider = () => directory;
                test(directory);
            }
            finally
            {
                AiRecipeIpcHandler.AiRecipeDirectoryProvider = previous;
            }
        }

        /// <summary>
        /// The test client is allowed, proposals go to a temporary directory and trust store, and the approval window is replaced
        /// </summary>
        private static async Task WithProposalPromptAsync(Func<RecipeApprovalRequest, CancellationToken, Task<RecipeApprovalWindow.ApprovalResult>> prompt, Func<Task> test)
        {
            string directory = Path.Combine(Path.GetTempPath(), "GreenshotAiRecipes_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var previousPrompt = AiRecipeIpcHandler.ApprovalPrompt;
            var previousDirectory = AiRecipeIpcHandler.AiRecipeDirectoryProvider;
            try
            {
                RecipeTrustStore.UseStoreFile(Path.Combine(directory, "recipe_trust.dat"));
                AiRecipeIpcHandler.ApprovalPrompt = prompt;
                AiRecipeIpcHandler.AiRecipeDirectoryProvider = () => Path.Combine(directory, "AI");
                AiRecipeIpcHandler.ResetRejectedProposals();
                await WithConsentAsync(test);
            }
            finally
            {
                AiRecipeIpcHandler.ApprovalPrompt = previousPrompt;
                AiRecipeIpcHandler.AiRecipeDirectoryProvider = previousDirectory;
                AiRecipeIpcHandler.ResetRejectedProposals();
                RecipeTrustStore.UseStoreFile(null);
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // A file watcher may still hold it
                }
            }
        }

        private static async Task WithConsentAsync(Func<Task> test)
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            var previousAllowed = config.AiToolsAllowedClients;
            try
            {
                config.AiToolsAllowedClients = new List<string> { TestClient.ExePath };
                AiToolAccess.ResetDeniedClients();
                await test();
            }
            finally
            {
                AiToolAccess.ResetDeniedClients();
                config.AiToolsAllowedClients = previousAllowed;
            }
        }

        private static async Task<JObject> DispatchAsync(string command, Dictionary<string, string> parameters)
        {
            var envelope = new IpcEnvelope
            {
                Command = command,
                Source = IpcSources.Mcp
            };
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    envelope.Parameters[parameter.Key] = parameter.Value;
                }
            }

            using var stream = new MemoryStream();
            var context = new IpcRequestContext(envelope, stream)
            {
                ConnectionOrigin = "test-client",
                AiClient = TestClient
            };
            await IpcSecurityDispatcher.DispatchAsync(context, null, null, null, null, null);

            stream.Position = 0;
            byte[] lengthBytes = new byte[4];
            Assert.Equal(4, stream.Read(lengthBytes, 0, 4));
            int length = BitConverter.ToInt32(lengthBytes, 0);
            byte[] payload = new byte[length];
            Assert.Equal(length, stream.Read(payload, 0, length));
            return JObject.Parse(Encoding.UTF8.GetString(payload));
        }
    }
}
