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
using Greenshot.Base.Recipes;
using Greenshot.Recipes.Approval;
using Greenshot.Recipes.Views;
using Newtonsoft.Json.Linq;

namespace Greenshot.Recipes
{
    /// <summary>
    /// Automatic steps (recipe extensions) from files: loaded like recipe files, with their own approval in the trust store.
    /// An extension changes other recipes, so the approval shows which ones; it is only used while its file is approved,
    /// a file changed on disk drops it from the recipes until it is approved again.
    /// </summary>
    public partial class RecipeManager
    {
        /// <summary>
        /// The built-in extensions, to bring one back when a file which replaced it is dropped
        /// </summary>
        private readonly Dictionary<string, RecipeExtension> _builtInExtensions = new Dictionary<string, RecipeExtension>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Whether the file content is an extension ("kind": "extension")
        /// </summary>
        internal static bool IsExtensionContent(string content)
        {
            try
            {
                return JToken.Parse(content) is JObject definition && RecipeSerializer.ReadKind(definition) == FlowDefinition.KindExtension;
            }
            catch (Exception)
            {
                // Not JSON: the recipe loading reports it
                return false;
            }
        }

        /// <summary>
        /// Loads an extension from the content of its file (read once), after the user approved it
        /// </summary>
        private RecipeValidationResult LoadExtensionFromContent(string filePath, string content, string contentHash, bool interactiveApproval, bool forceApprovalPrompt)
        {
            var result = new RecipeValidationResult();
            string fullPath = Path.GetFullPath(filePath);
            RecipeExtension extension;
            try
            {
                extension = RecipeSerializer.DeserializeExtension(content, validate: false);
            }
            catch (Exception ex)
            {
                result.AddError($"The automatic step in '{filePath}' can't be read: {ex.Message}");
                if (interactiveApproval)
                {
                    RecipeApprovalWindow.ShowValidationError(filePath, rawErrorMessage: ex.Message);
                }
                return result;
            }
            extension.FilePath = fullPath;

            var validation = ValidateExtension(extension);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors) result.AddError($"[{extension.Id ?? "unknown"}]: {error}");
                if (interactiveApproval)
                {
                    RecipeApprovalWindow.ShowValidationError(filePath, validation, extension.AsRecipeView());
                }
                return result;
            }

            // Security check: SHA-256 trust pinning, like a recipe of the file
            var approval = forceApprovalPrompt ? null : RecipeTrustStore.GetApproval(fullPath, contentHash, extension.Id);
            bool newlyApproved = false;
            if (approval == null)
            {
                if (!interactiveApproval)
                {
                    result.AddError($"Automatic step '{extension.Name}' from '{filePath}' is not approved and interactive approval is disabled.");
                    return result;
                }
                var previous = RecipeTrustStore.GetTrustRecord(fullPath)?.GetApproval(extension.Id);
                var decision = RequestInteractiveApprovalWithOptions(CreateExtensionApprovalRequest(extension, fullPath, content, contentHash, validation));
                approval = decision?.Approval;
                if (approval != null)
                {
                    RecipeTrustStore.RecordApproval(fullPath, contentHash, approval, content, recipeName: extension.Name, recipeVersion: extension.Version);
                    newlyApproved = previous == null;
                }
                else if (decision?.IsRevoked == true)
                {
                    RevokeApproval(extension.Id, fullPath);
                    result.AddError($"The approval of automatic step '{extension.Name}' ({extension.Id}) was revoked.");
                    return result;
                }
                else if (forceApprovalPrompt && (approval = RecipeTrustStore.GetApproval(fullPath, contentHash, extension.Id)) != null)
                {
                    Log.InfoFormat("The review of automatic step '{0}' was closed, its approval stays as it was.", extension.Id);
                }
                else
                {
                    result.AddError($"User rejected automatic step '{extension.Name}' ({extension.Id}) from '{filePath}'.");
                    return result;
                }
            }

            var missingGates = RecipeApprovalPolicy.GetMissingGates(validation, approval);
            if (missingGates.Count > 0)
            {
                result.AddError($"Automatic step '{extension.Name}' needs {string.Join(", ", missingGates.Select(RecipeApprovalPolicy.GetGateName))}, but that was not allowed.");
                return result;
            }
            foreach (var warning in validation.Warnings) result.AddWarning($"[{extension.Id}]: {warning}");

            var trustRecord = RecipeTrustStore.GetTrustRecord(fullPath);
            extension.ProposedBy = trustRecord?.IsAiCreated == true ? trustRecord.Origin.Substring(RecipeTrustRecord.AiOriginPrefix.Length) : null;
            if (newlyApproved && extension.ProposedBy != null && extension.EnabledOption != null)
            {
                // Approving is not switching on: what an AI tool wrote starts switched off
                RecipeOptionStore.SetValue(extension.Id, extension.EnabledOption, false);
            }

            lock (_extensions)
            {
                extension.IsOverridden = _builtInExtensions.ContainsKey(extension.Id);
                _extensions[extension.Id] = extension;
            }
            Log.InfoFormat("Registered automatic step '{0}' from '{1}'", extension.Id, fullPath);
            AddRecipeFileToConfig(fullPath);
            NotifyRecipesChanged();
            return result;
        }

        /// <summary>
        /// The checks of an extension: the validator's, and that it doesn't extend another extension
        /// </summary>
        internal RecipeValidationResult ValidateExtension(RecipeExtension extension)
        {
            var result = RecipeValidator.Validate(extension);
            if (extension?.Extends?.Recipes == null) return result;
            lock (_extensions)
            {
                foreach (var target in extension.Extends.Recipes.Where(t => t != null && _extensions.ContainsKey(t.Trim()) && !string.Equals(t.Trim(), extension.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    result.AddError($"Automatic step '{extension.Id}' targets the automatic step '{target}'; an automatic step can't extend another one.");
                }
            }
            return result;
        }

        /// <summary>
        /// The approval window for an extension: its steps, and which recipes it changes
        /// </summary>
        private RecipeApprovalRequest CreateExtensionApprovalRequest(RecipeExtension extension, string filePath, string content, string contentHash, RecipeValidationResult validation)
        {
            var previousRecord = RecipeTrustStore.GetTrustRecord(filePath);
            var request = new RecipeApprovalRequest
            {
                Recipe = extension.AsRecipeView(),
                Extension = extension,
                ExtensionReach = DescribeExtensionReach(extension),
                FilePath = filePath,
                Content = content,
                ContentHash = contentHash,
                Validation = validation,
                PreviousRecord = previousRecord,
                StartSwitchedOff = previousRecord?.IsAiCreated == true
            };
            RecipeExtension builtIn;
            lock (_extensions)
            {
                _builtInExtensions.TryGetValue(extension.Id, out builtIn);
            }
            if (builtIn != null)
            {
                request.ReplacedRecipe = builtIn.AsRecipeView();
                request.ReplacesBuiltIn = true;
                request.PreviousContent = RecipeSerializer.Serialize(builtIn);
            }
            return request;
        }

        /// <summary>
        /// Which recipes an extension changes, where and when, in plain words (for its approval)
        /// </summary>
        public IReadOnlyList<string> DescribeExtensionReach(RecipeExtension extension)
        {
            var lines = new List<string>();
            if (extension == null) return lines;

            var recipes = GetAllRecipes().Where(r => RecipeComposer.CanExtend(extension, r)).Select(r => r.Name ?? r.Id).OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToList();
            lines.Add(recipes.Count == 0
                ? "• Changes no recipe now (none has a slot it fits in)."
                : $"• Changes these recipes now: {string.Join(", ", recipes)}.");

            var targets = extension.Extends?.Recipes ?? new List<string>();
            if (targets.Any(t => t?.Trim() == RecipeExtension.TargetAll))
            {
                lines.Add("• Also every recipe added later which has the slot (\"*\").");
            }
            else if (targets.Any(t => string.Equals(t?.Trim(), RecipeExtension.TargetCaptures, StringComparison.OrdinalIgnoreCase)))
            {
                lines.Add("• Also every capture recipe added later (\"*capture\").");
            }

            lines.Add(extension.SlotName switch
            {
                RecipeSlots.AfterCapture => "• Where: after the capture and the selection, before the processors.",
                RecipeSlots.BeforeExport => "• Where: once before the destinations, when the image is final.",
                RecipeSlots.AfterExport => "• Where: after the destinations.",
                RecipeSlots.BeforeDestination => "• Where: for each destination, on its own copy of the capture.",
                _ => $"• Where: the slot '{extension.Extends?.Slot}'."
            });

            if (!string.IsNullOrWhiteSpace(extension.When))
            {
                lines.Add($"• Only when: {extension.When}");
            }
            lines.Add(extension.EnabledOption != null
                ? "• Switched on and off in Settings > Recipes; there you can also limit it to some recipes and destinations."
                : "• It has no on/off switch: it is always used once approved. You can limit it in Settings > Recipes.");
            return lines;
        }

        /// <summary>
        /// The extension isn't used anymore (its file changed or its approval was revoked); a built-in one it replaced comes back
        /// </summary>
        private void DropExtension(string extensionId)
        {
            bool dropped;
            lock (_extensions)
            {
                dropped = _extensions.TryGetValue(extensionId, out var extension) && !string.IsNullOrEmpty(extension.FilePath);
                if (dropped)
                {
                    _extensions.Remove(extensionId);
                    if (_builtInExtensions.TryGetValue(extensionId, out var builtIn))
                    {
                        _extensions[extensionId] = builtIn;
                    }
                }
            }
            if (dropped)
            {
                Log.InfoFormat("Automatic step '{0}' is not used anymore, until its file is approved (again).", extensionId);
                NotifyRecipesChanged();
            }
        }

        /// <summary>
        /// What the settings and the recipe manager show about an extension: what it does, which recipes it changes, its approval
        /// </summary>
        private RecipeDetails GetExtensionDetails(RecipeExtension extension)
        {
            var details = new RecipeDetails
            {
                WhatItDoes = RecipeDescriber.DescribeSteps(extension.AsRecipeView()).Select(l => l.IsDetail ? "    " + l.Text : l.Text).ToList(),
                Reach = DescribeExtensionReach(extension),
                ProposedBy = extension.ProposedBy
            };
            if (!string.IsNullOrEmpty(extension.FilePath))
            {
                var record = RecipeTrustStore.GetTrustRecord(extension.FilePath);
                if (record != null)
                {
                    details.ApprovedAt = record.ApprovedAt.ToLocalTime();
                    details.ApprovedHash = record.Sha256Hash;
                    details.IsApprovalCurrent = string.Equals(RecipeTrustStore.ComputeSha256(extension.FilePath), record.Sha256Hash, StringComparison.OrdinalIgnoreCase);
                    details.Permissions = record.GetApproval(extension.Id)?.AllowedGates?.Select(RecipeApprovalPolicy.GetGateName).ToList() ?? new List<string>();
                }
            }
            return details;
        }

        /// <summary>
        /// The approval window read-only for an extension: what it does, which recipes it changes, what was approved
        /// </summary>
        private bool ShowExtensionDetails(RecipeExtension extension)
        {
            RecipeApprovalRequest request;
            if (!string.IsNullOrEmpty(extension.FilePath) && File.Exists(extension.FilePath))
            {
                byte[] bytes = File.ReadAllBytes(extension.FilePath);
                request = CreateExtensionApprovalRequest(extension, extension.FilePath, DecodeRecipeFile(bytes), RecipeTrustStore.ComputeSha256(bytes), ValidateExtension(extension));
                request.StartSwitchedOff = false;
            }
            else
            {
                request = new RecipeApprovalRequest
                {
                    Recipe = extension.AsRecipeView(),
                    Extension = extension,
                    ExtensionReach = DescribeExtensionReach(extension),
                    Content = RecipeSerializer.Serialize(extension),
                    Validation = RecipeValidator.Validate(extension)
                };
            }
            request.IsReadOnly = true;

            var window = new RecipeApprovalWindow(request)
            {
                Owner = System.Windows.Application.Current?.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive),
                ShowActivated = true
            };
            window.WindowStartupLocation = window.Owner != null ? System.Windows.WindowStartupLocation.CenterOwner : System.Windows.WindowStartupLocation.CenterScreen;
            window.ShowDialog();
            return true;
        }

        public RecipeValidationResult SaveExtensionToFile(RecipeExtension extension, string filePath)
        {
            var result = new RecipeValidationResult();
            if (!RecipeFilesSupported)
            {
                result.AddError("Greenshot Light doesn't save automatic step files.");
                return result;
            }
            if (extension == null || string.IsNullOrWhiteSpace(filePath))
            {
                result.AddError("No automatic step or file to save to.");
                return result;
            }

            string fullPath = Path.GetFullPath(filePath);
            extension.FilePath = fullPath;
            // The bytes which are approved are the bytes which are written
            string content = RecipeSerializer.Serialize(extension);
            byte[] bytes = new System.Text.UTF8Encoding(false).GetBytes(content);
            string contentHash = RecipeTrustStore.ComputeSha256(bytes);

            var validation = ValidateExtension(extension);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors) result.AddError(error);
                return result;
            }

            var record = RecipeTrustStore.GetTrustRecord(fullPath);
            var previousApproval = record?.GetApproval(extension.Id);
            var approved = FindApprovedExtension(GetApprovedContent(record, fullPath), extension.Id);
            var reasons = GetExtensionSaveReasons(extension, validation, previousApproval, approved);
            RecipeApproval approval;
            if (reasons.Count > 0)
            {
                Log.InfoFormat("The change of automatic step '{0}' needs the user's decision: {1}", extension.Id, string.Join(" ", reasons));
                var request = CreateExtensionApprovalRequest(extension, fullPath, content, contentHash, validation);
                request.StartSwitchedOff = false;
                request.OwnEditReasons = reasons;
                approval = RequestInteractiveApproval(request);
                if (approval == null)
                {
                    result.AddError("The automatic step was not saved, its changes were not approved.");
                    return result;
                }
            }
            else
            {
                approval = new RecipeApproval { RecipeId = extension.Id, AllowedGates = previousApproval?.AllowedGates?.ToList() ?? new List<Greenshot.Base.Recipes.Pipeline.RecipeGateType>() };
            }
            approval.RecipeId = extension.Id;
            // Recorded before writing, so the change on disk is known as Greenshot's own
            RecipeTrustStore.RecordApproval(fullPath, contentHash, approval, content, recipeName: extension.Name, recipeVersion: extension.Version);

            try
            {
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllBytes(fullPath, bytes);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not save the automatic step '{extension.Id}' to '{fullPath}'.", ex);
                result.AddError($"Could not save the automatic step: {ex.Message}");
                return result;
            }
            SetupWatcherForFile(fullPath);

            extension.ProposedBy = null;
            extension.IsBuiltIn = false;
            lock (_extensions)
            {
                extension.IsOverridden = _builtInExtensions.ContainsKey(extension.Id);
                _extensions[extension.Id] = extension;
            }
            AddRecipeFileToConfig(fullPath);
            NotifyRecipesChanged();
            foreach (var warning in validation.Warnings) result.AddWarning(warning);
            return result;
        }

        /// <summary>
        /// Why saving an extension from the editor needs the user's decision; empty when its approval is renewed without asking
        /// </summary>
        private IReadOnlyList<string> GetExtensionSaveReasons(RecipeExtension extension, RecipeValidationResult validation, RecipeApproval previousApproval, RecipeExtension approved)
        {
            var reasons = new List<string>();
            bool replacesBuiltIn;
            lock (_extensions)
            {
                replacesBuiltIn = _builtInExtensions.ContainsKey(extension.Id);
            }
            if (previousApproval == null || approved == null)
            {
                reasons.Add(replacesBuiltIn ? "It replaces a built-in automatic step." : "It is a new automatic step, which changes other recipes.");
            }
            else
            {
                if (!SameReach(approved, extension))
                {
                    reasons.Add("It changes other recipes than before (which recipes or where).");
                }
                var newGates = RecipeApprovalPolicy.GetMissingGates(validation, previousApproval);
                if (newGates.Count > 0)
                {
                    reasons.Add($"It needs a new permission: {string.Join(", ", newGates.Select(RecipeApprovalPolicy.GetGateName))}.");
                }
            }
            return reasons;
        }

        private static bool SameReach(RecipeExtension a, RecipeExtension b)
        {
            var targetsA = new HashSet<string>(a.Extends?.Recipes?.Select(t => t.Trim()) ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var targetsB = new HashSet<string>(b.Extends?.Recipes?.Select(t => t.Trim()) ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            return targetsA.SetEquals(targetsB) && a.SlotName == b.SlotName;
        }

        private static RecipeExtension FindApprovedExtension(string content, string extensionId)
        {
            if (string.IsNullOrEmpty(content) || !IsExtensionContent(content)) return null;
            try
            {
                var extension = RecipeSerializer.DeserializeExtension(content, validate: false);
                return string.Equals(extension.Id, extensionId, StringComparison.OrdinalIgnoreCase) ? extension : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool UnregisterExtension(string extensionId)
        {
            var extension = GetExtensionById(extensionId);
            if (string.IsNullOrEmpty(extension?.FilePath)) return false;
            DropExtension(extensionId);
            bool fileStillUsed;
            lock (_recipes)
            {
                fileStillUsed = _recipes.Values.Any(r => string.Equals(r.FilePath, extension.FilePath, StringComparison.OrdinalIgnoreCase));
            }
            if (!fileStillUsed)
            {
                RemoveRecipeFileFromConfig(extension.FilePath);
            }
            Log.InfoFormat("Unloaded automatic step '{0}' from '{1}'.", extensionId, extension.FilePath);
            return true;
        }

        /// <summary>
        /// The extensions loaded from this file
        /// </summary>
        private IReadOnlyList<RecipeExtension> GetExtensionsOfFile(string fullPath)
        {
            lock (_extensions)
            {
                return _extensions.Values.Where(e => !string.IsNullOrEmpty(e.FilePath) && string.Equals(Path.GetFullPath(e.FilePath), fullPath, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
    }
}
