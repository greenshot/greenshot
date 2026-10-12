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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Document;
using Greenshot.Document.Editor;
using Greenshot.DocumentModel;
using Greenshot.Editor.Drawing;
using Greenshot.Helpers;
using Newtonsoft.Json;

[assembly: GreenshotModule(typeof(DocumentRecipeSteps))]

namespace Greenshot.DocumentModel
{
    /// <summary>
    /// The recipe steps of the document model. Only in Debug builds: Greenshot.csproj leaves this folder out of Release.
    /// </summary>
    public sealed class DocumentRecipeSteps : IRecipeStepProvider
    {
        public void RegisterSteps(IStepRegistry registry)
        {
            registry.Register<AddElementsStep>(config => new AddElementsStep(config));
            registry.Register<SaveDocumentStep>(config => new SaveDocumentStep(config));
        }
    }

    /// <summary>
    /// Adds elements, written like the elements in document.json, to the capture
    /// </summary>
    [StepInfo("AddElements", "Add Elements", "Adds elements, written like the elements of a .greenshot document, to the capture.", "Annotations")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required, VisualMutation = PayloadEffect.AddsAnnotations)]
    [StepParameter("Elements", ContractDataType.Object, Required = true, Description = "The elements, e.g. [{\"type\": \"rectangle\", \"id\": \"...\", \"area\": {\"x\": 10, \"y\": 10, \"width\": 100, \"height\": 50}}]")]
    public sealed class AddElementsStep : ICaptureStep
    {
        public AddElementsStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? "AddElements";
        }

        public string Name { get; }

        public RecipeNodeConfig NodeConfig { get; }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context.Payload?.EnsureSurface() is not Surface surface || NodeConfig.Parameters == null || !NodeConfig.Parameters.TryGetValue("Elements", out var value) || value == null)
            {
                context.LogStep("AddElements skipped: no surface or no elements.");
                return Task.CompletedTask;
            }

            // An element without id gets a new one
            var elements = GreenshotPackage.ReadElements(JsonConvert.SerializeObject(value))
                .Select(element => element.Id == Guid.Empty ? element with { Id = Uuid7.CreateVersion7() } : element);
            var document = new GreenshotDocument(surface.ID, new Canvas(surface.Image.Width, surface.Image.Height), Array.Empty<Layer>(), new Dictionary<Guid, Resource>());
            var added = DocumentToSurface.AddElements(surface, document, elements, true);
            if (added.Count > 0)
            {
                surface.Invalidate();
                surface.Modified = true;
                context.Payload.InvalidateExportSource();
            }

            context.LogStep($"AddElements added {added.Count} element(s).");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Saves the capture as a .greenshot document (zip)
    /// </summary>
    [StepInfo("SaveDocument", "Save Document", "Saves the capture with its elements as a .greenshot document.", "Destinations")]
    [StepPayload(Surface = PayloadRequirement.Required)]
    [StepParameter("FilePath", ContractDataType.FilePath, Required = true, Description = "The file to write")]
    public sealed class SaveDocumentStep : ICaptureStep
    {
        public SaveDocumentStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? "SaveDocument";
        }

        public string Name { get; }

        public RecipeNodeConfig NodeConfig { get; }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            string filePath = NodeConfig.GetParameter<string>("FilePath");
            var surface = context.Payload?.EnsureSurface();
            if (surface == null || string.IsNullOrWhiteSpace(filePath))
            {
                context.LogStep("SaveDocument skipped: no surface or no file path.");
                return Task.CompletedTask;
            }

            var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>();
            using (var stream = File.Create(filePath))
            {
                GreenshotPackage.Write(SurfaceToDocument.ToDocument(surface), stream, mediaType => registry.GetByMimeType(mediaType)?.PreferredExtension);
            }

            context.LogStep($"SaveDocument wrote {filePath}.");
            return Task.CompletedTask;
        }
    }
}
