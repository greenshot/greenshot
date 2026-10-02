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
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Recipes;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The approval of recipes: per recipe, trigger and kind of gated action, the content that is pinned, the diff and the
    /// plain-language description shown in the approval window.
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class RecipeApprovalTests
    {
        public RecipeApprovalTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Diff_OfEqualTexts_HasNoChanges()
        {
            Assert.Equal("// No changes.", RecipeTextDiff.ToUnifiedText("a\nb\n", "a\r\nb"));
            Assert.All(RecipeTextDiff.Compare("a\nb", "a\nb"), l => Assert.Equal(' ', l.Kind));
        }

        [Fact]
        public void Diff_ShowsRemovedAndAddedLines_WithContext()
        {
            var oldLines = Enumerable.Range(1, 20).Select(i => $"line {i}").ToList();
            var newLines = oldLines.ToList();
            newLines[1] = "line two";
            newLines.Insert(15, "inserted");

            string diff = RecipeTextDiff.ToUnifiedText(string.Join("\n", oldLines), string.Join("\n", newLines), context: 1);
            var lines = diff.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

            Assert.Contains("- line 2", lines);
            Assert.Contains("+ line two", lines);
            Assert.Contains("+ inserted", lines);
            // Unchanged lines far from the changes are left out, a "@@" separates the parts
            Assert.DoesNotContain("  line 8", lines);
            Assert.Contains("@@", lines);
        }

        [Fact]
        public void Diff_OfHugeTexts_IsNotMade()
        {
            string huge = string.Join("\n", Enumerable.Range(0, RecipeTextDiff.MaxLines + 1).Select(i => i.ToString()));
            Assert.Null(RecipeTextDiff.Compare(huge, "x"));
            Assert.Contains("too large", RecipeTextDiff.ToUnifiedText(huge, "x"));
        }

        [Fact]
        public void TriggerApprovalFlags_AreNotSaved_AndAreCloned()
        {
            var trigger = TriggerConfig.CreateHotkey("Ctrl + Shift + F9");
            trigger.IsApproved = false;
            trigger.IsBrowserInvocationApproved = false;

            Assert.True(trigger.Enabled);
            Assert.False(trigger.IsActive);

            var clone = trigger.Clone();
            Assert.False(clone.IsApproved);
            Assert.False(clone.IsBrowserInvocationApproved);

            var recipe = CreateRecipe("flags_recipe").AddTrigger(trigger);
            string json = RecipeSerializer.Serialize(recipe);
            Assert.DoesNotContain("isApproved", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("isActive", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BrowserInvocationApproved", json, StringComparison.OrdinalIgnoreCase);

            // A loaded trigger is approved until an approval says otherwise
            var loaded = RecipeSerializer.Deserialize(json, validate: false);
            Assert.True(loaded.Triggers[0].IsApproved);
            Assert.True(loaded.Triggers[0].IsActive);
        }

        [Fact]
        public void Apply_SwitchesOnOnlyTheApprovedTriggers()
        {
            var recipe = CreateRecipe("apply_recipe")
                .AddTrigger(TriggerConfig.CreateHotkey("Ctrl + Shift + F9"))
                .AddTrigger(TriggerConfig.CreateClipboard())
                .AddTrigger(CreateBrowserCommandline("apply"));

            var approval = new RecipeApproval
            {
                RecipeId = recipe.Id,
                ApprovedTriggers = new List<string>
                {
                    RecipeApprovalPolicy.GetTriggerKey(0, recipe.Triggers[0]),
                    RecipeApprovalPolicy.GetTriggerKey(2, recipe.Triggers[2])
                }
            };
            RecipeApprovalPolicy.Apply(recipe, approval);

            Assert.Equal("0:Hotkey", RecipeApprovalPolicy.GetTriggerKey(0, recipe.Triggers[0]));
            Assert.True(recipe.Triggers[0].IsActive);
            Assert.False(recipe.Triggers[1].IsActive);
            Assert.True(recipe.Triggers[2].IsActive);
            // Starting it from web pages is approved separately
            Assert.False(recipe.Triggers[2].IsBrowserInvocationApproved);

            approval.ApprovedTriggers.Add(RecipeApprovalPolicy.GetBrowserInvocationKey(2, recipe.Triggers[2]));
            RecipeApprovalPolicy.Apply(recipe, approval);
            Assert.True(recipe.Triggers[2].IsBrowserInvocationApproved);

            RecipeApprovalPolicy.Apply(recipe, null);
            Assert.All(recipe.Triggers, t => Assert.False(t.IsActive));
        }

        [Fact]
        public void MissingGates_AreTheKindsOfGatedActionsNotAllowed()
        {
            var validation = new RecipeValidationResult();
            validation.GatedActions.Add(new RecipeGatedAction(RecipeGateType.NetworkAccess, "Imgur (imgur.com)"));
            validation.GatedActions.Add(new RecipeGatedAction(RecipeGateType.ExternalCommand, @"C:\Tools\tool.exe"));
            validation.GatedActions.Add(new RecipeGatedAction(RecipeGateType.NetworkAccess, "Box (box.com)"));

            Assert.Equal(new[] { RecipeGateType.ExternalCommand, RecipeGateType.NetworkAccess }.OrderBy(g => g), RecipeApprovalPolicy.GetGateTypes(validation));

            var approval = new RecipeApproval { AllowedGates = new List<RecipeGateType> { RecipeGateType.NetworkAccess } };
            Assert.Equal(new[] { RecipeGateType.ExternalCommand }, RecipeApprovalPolicy.GetMissingGates(validation, approval));

            approval.AllowedGates.Add(RecipeGateType.ExternalCommand);
            Assert.Empty(RecipeApprovalPolicy.GetMissingGates(validation, approval));
            Assert.Equal(2, RecipeApprovalPolicy.GetMissingGates(validation, null).Count);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LegacyTrustRecord_ApprovesAllTriggers_AndExternalCommandsAsBefore(bool allowExternalCommands)
        {
            var record = new RecipeTrustRecord { Sha256Hash = "abc", AllowExternalCommands = allowExternalCommands };
            var approval = record.GetApproval("any_recipe");

            Assert.True(approval.IsTriggerApproved("5:Clipboard"));
            Assert.True(approval.IsGateAllowed(RecipeGateType.NetworkAccess));
            Assert.True(approval.IsGateAllowed(RecipeGateType.FileSystemAccess));
            Assert.Equal(allowExternalCommands, approval.IsGateAllowed(RecipeGateType.ExternalCommand));
            Assert.False(record.IsAiCreated);
        }

        [Fact]
        public void TrustStore_PinsTheApprovalPerRecipe_AndPerContent()
        {
            WithTemporaryTrustStore(directory =>
            {
                string file = Path.Combine(directory, "two.gsrecipe.json");
                byte[] bytes = Encoding.UTF8.GetBytes("[{\"id\":\"one\"},{\"id\":\"two\"}]");
                File.WriteAllBytes(file, bytes);
                string hash = RecipeTrustStore.ComputeSha256(bytes);
                Assert.Equal(hash, RecipeTrustStore.ComputeSha256(file));

                RecipeTrustStore.RecordApproval(file, hash, new RecipeApproval
                {
                    RecipeId = "one",
                    ApprovedTriggers = new List<string> { "0:Hotkey" },
                    AllowedGates = new List<RecipeGateType> { RecipeGateType.NetworkAccess }
                }, "content", RecipeTrustRecord.AiOriginPrefix + "Test AI");

                var one = RecipeTrustStore.GetApproval(file, hash, "one");
                Assert.NotNull(one);
                Assert.True(one.IsTriggerApproved("0:Hotkey"));
                Assert.False(one.IsTriggerApproved("1:Clipboard"));
                Assert.True(one.IsGateAllowed(RecipeGateType.NetworkAccess));
                Assert.False(one.IsGateAllowed(RecipeGateType.ExternalCommand));

                // The other recipe of the file isn't approved by that
                Assert.Null(RecipeTrustStore.GetApproval(file, hash, "two"));
                // Another content isn't approved
                Assert.Null(RecipeTrustStore.GetApproval(file, "0000", "one"));

                var record = RecipeTrustStore.GetTrustRecord(file);
                Assert.True(record.IsAiCreated);
                Assert.Equal("content", record.ApprovedContent);
                Assert.True(RecipeTrustStore.IsRecipeApproved(file, out _, out bool allowExternalCommands));
                Assert.False(allowExternalCommands);

                // The second recipe of the same content is added to the record
                RecipeTrustStore.RecordApproval(file, hash, new RecipeApproval { RecipeId = "two", AllTriggers = true });
                Assert.NotNull(RecipeTrustStore.GetApproval(file, hash, "one"));
                Assert.NotNull(RecipeTrustStore.GetApproval(file, hash, "two"));
            });
        }

        [Fact]
        public void DecodeRecipeFile_ReadsUtf8WithAndWithoutBom()
        {
            const string json = "{\"name\":\"Grüße\"}";
            var withBom = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(json)).ToArray();
            Assert.Equal(json, RecipeManager.DecodeRecipeFile(withBom));
            Assert.Equal(json, RecipeManager.DecodeRecipeFile(Encoding.UTF8.GetBytes(json)));
        }

        [Fact]
        public void Describer_ExplainsTheRiskOfTriggers()
        {
            var recipe = CreateRecipe("describe_recipe")
                .AddTrigger(TriggerConfig.CreateHotkey("Ctrl + Shift + F9"))
                .AddTrigger(TriggerConfig.CreateClipboard())
                .AddTrigger(CreateBrowserCommandline("describe"));

            var triggers = RecipeDescriber.DescribeTriggers(recipe);

            Assert.Equal(4, triggers.Count);
            Assert.Contains("Ctrl + Shift + F9", triggers[0].Label);
            Assert.True(string.IsNullOrEmpty(triggers[0].Risk));
            Assert.False(string.IsNullOrEmpty(triggers[1].Risk));
            Assert.Equal("2:Commandline", triggers[2].Key);
            Assert.Equal("2:Commandline" + RecipeApprovalPolicy.BrowserInvocationSuffix, triggers[3].Key);
            Assert.Contains("web page", triggers[3].Risk, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Describer_ShowsTheProgramAnExternalCommandRuns()
        {
            var recipe = CreateRecipe("describe_steps")
                .AddNode(new RecipeNodeConfig
                {
                    Id = "cmd_step",
                    StepType = "ExternalCommand",
                    Parameters = new Dictionary<string, object>
                    {
                        { "CommandLine", @"C:\Tools\optimize.exe" },
                        { "Arguments", "\"{0}\"" }
                    }
                });
            recipe.Flow.AddTransition("source", "cmd_step");

            var lines = RecipeDescriber.DescribeSteps(recipe);

            Assert.Contains(lines, l => !l.IsDetail && l.Risk == null);
            var detail = Assert.Single(lines, l => l.IsDetail);
            Assert.Equal(RecipeGateType.ExternalCommand, detail.Risk);
            Assert.Contains(@"C:\Tools\optimize.exe", detail.Text);
        }

        [Fact]
        public void Describer_NamesAddedAndRemovedSteps()
        {
            var oldRecipe = CreateRecipe("changes");
            var newRecipe = CreateRecipe("changes")
                .AddNode(new RecipeNodeConfig { Id = "clip", StepType = "Clipboard" });
            newRecipe.Flow.AddTransition("source", "clip");

            var changes = RecipeDescriber.DescribeChanges(oldRecipe, newRecipe);
            Assert.Contains(changes, c => c.StartsWith("Adds", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(changes, c => c.StartsWith("Removes", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void BuiltInRecipes_HavePlainNames()
        {
            var builtIns = RecipeManager.Instance.GetAllRecipes().Where(r => r.IsBuiltIn && !r.IsOverridden).ToList();
            Assert.NotEmpty(builtIns);
            Assert.All(builtIns, r =>
            {
                Assert.False(string.IsNullOrWhiteSpace(r.Name));
                Assert.DoesNotContain("###", r.Name);
            });
        }

        [Fact]
        public void ProposedBy_IsNotSaved_AndIsCloned()
        {
            var recipe = CreateRecipe("proposed_by");
            recipe.ProposedBy = "Test AI";
            Assert.Equal("Test AI", recipe.Clone().ProposedBy);
            Assert.DoesNotContain("Test AI", RecipeSerializer.Serialize(recipe));
        }

        [Fact]
        public void EditApproval_KeepsUnchangedSwitches_AndApprovesHarmlessNewTriggers()
        {
            var approved = CreateRecipe("edit_recipe")
                .AddTrigger(TriggerConfig.CreateHotkey("Ctrl + Shift + F9"))
                .AddTrigger(TriggerConfig.CreateClipboard());
            // The user switched the clipboard trigger off when approving
            var previous = new RecipeApproval { RecipeId = "edit_recipe", ApprovedTriggers = new List<string> { "0:Hotkey" } };

            // The edit puts a new hotkey first: positions change, the switches stay with their triggers
            var edited = RecipeSerializer.Deserialize(RecipeSerializer.Serialize(approved), validate: false);
            edited.Triggers.Insert(0, TriggerConfig.CreateHotkey("Ctrl + Shift + F10"));

            var approval = RecipeEditApproval.Create(edited, RecipeValidator.Validate(edited), approved, previous, false, out var decision);

            Assert.False(decision.IsNeeded, string.Join(" ", decision.Reasons));
            Assert.True(approval.IsTriggerApproved("0:Hotkey"));
            Assert.True(approval.IsTriggerApproved("1:Hotkey"));
            Assert.False(approval.IsTriggerApproved("2:Clipboard"));
        }

        [Fact]
        public void EditApproval_NewTriggerWhichStartsOnItsOwn_NeedsADecision()
        {
            var approved = CreateRecipe("edit_risky");
            var previous = new RecipeApproval { RecipeId = "edit_risky" };
            var edited = CreateRecipe("edit_risky").AddTrigger(TriggerConfig.CreateClipboard());

            RecipeEditApproval.Create(edited, RecipeValidator.Validate(edited), approved, previous, false, out var decision);

            Assert.True(decision.IsNeeded);
            Assert.Equal(new[] { "0:Clipboard" }, decision.TriggerKeys);
        }

        [Fact]
        public void EditApproval_NewKindOfGatedAction_NeedsADecision_AllowedOnesStay()
        {
            var edited = CreateRecipe("edit_gates")
                .AddNode(new RecipeNodeConfig
                {
                    Id = "cmd_step",
                    StepType = "ExternalCommand",
                    Parameters = new Dictionary<string, object> { { "CommandLine", @"C:\Tools\optimize.exe" }, { "Arguments", "\"{0}\"" } }
                });
            edited.Flow.AddTransition("source", "cmd_step");
            var validation = RecipeValidator.Validate(edited);

            RecipeEditApproval.Create(edited, validation, CreateRecipe("edit_gates"), new RecipeApproval(), false, out var decision);
            Assert.True(decision.IsNeeded);

            var allowed = new RecipeApproval { AllowedGates = new List<RecipeGateType> { RecipeGateType.ExternalCommand } };
            var approval = RecipeEditApproval.Create(edited, validation, CreateRecipe("edit_gates"), allowed, false, out decision);
            Assert.False(decision.IsNeeded);
            Assert.True(approval.IsGateAllowed(RecipeGateType.ExternalCommand));
        }

        [Fact]
        public void EditApproval_FirstReplacementOfABuiltIn_NeedsADecision()
        {
            var edited = CreateRecipe("edit_builtin");
            RecipeEditApproval.Create(edited, RecipeValidator.Validate(edited), null, null, true, out var decision);
            Assert.True(decision.IsNeeded);

            RecipeEditApproval.Create(edited, RecipeValidator.Validate(edited), edited, new RecipeApproval { ReplacesBuiltIn = true }, true, out decision);
            Assert.False(decision.IsNeeded);
        }

        [Fact]
        public void SaveRecipeToFile_RenewsTheApproval_ForExactlyTheSavedContent()
        {
            WithTemporaryTrustStore(directory =>
            {
                const string id = "saved_in_editor";
                string file = Path.Combine(directory, id + RecipeSerializer.RecipeFileExtension);
                var recipe = CreateRecipe(id).AddTrigger(new TriggerConfig(TriggerConfig.TypeManual));
                try
                {
                    var result = RecipeManager.Instance.SaveRecipeToFile(recipe, file);
                    Assert.True(result.IsValid, string.Join("; ", result.Errors));
                    Assert.True(File.Exists(file));
                    Assert.True(RecipeTrustStore.IsRecipeApproved(file, out _, out _));
                    Assert.NotNull(RecipeTrustStore.GetApproval(file, RecipeTrustStore.ComputeSha256(file), id));
                    Assert.Same(recipe, RecipeManager.Instance.GetRecipeById(id));
                    Assert.True(recipe.Triggers[0].IsActive);

                    // A second save after a harmless change: approved again, without asking
                    recipe.Description = "changed";
                    result = RecipeManager.Instance.SaveRecipeToFile(recipe, file);
                    Assert.True(result.IsValid, string.Join("; ", result.Errors));
                    Assert.True(RecipeTrustStore.IsRecipeApproved(file, out _, out _));

                    var details = RecipeManager.Instance.GetRecipeDetails(id);
                    Assert.NotNull(details.ApprovedAt);
                    Assert.True(details.IsApprovalCurrent);
                    Assert.NotEmpty(details.WhatItDoes);
                }
                finally
                {
                    RecipeManager.Instance.UnregisterRecipe(id);
                }
            });
        }

        internal static CaptureRecipe CreateRecipe(string id)
        {
            var recipe = new CaptureRecipe(id, id)
                .AddNode(new RecipeNodeConfig
                {
                    Id = "source",
                    StepType = "Source",
                    Parameters = new Dictionary<string, object> { { "SourceType", "FullScreen" } }
                });
            recipe.Flow = new RecipeFlowConfig("source");
            return recipe;
        }

        private static TriggerConfig CreateBrowserCommandline(string command)
        {
            var trigger = new TriggerConfig(TriggerConfig.TypeCommandline);
            trigger.Parameters["Command"] = command;
            trigger.Parameters["AllowBrowserInvocation"] = true;
            return trigger;
        }

        /// <summary>
        /// Runs the test with a trust store in a temporary directory, never the user's
        /// </summary>
        internal static void WithTemporaryTrustStore(Action<string> test)
        {
            string directory = Path.Combine(Path.GetTempPath(), "GreenshotTrustTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                RecipeTrustStore.UseStoreFile(Path.Combine(directory, "recipe_trust.dat"));
                test(directory);
            }
            finally
            {
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
    }
}
