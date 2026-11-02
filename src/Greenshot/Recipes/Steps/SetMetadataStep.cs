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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using log4net;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Pipeline step deciding the details (EXIF/XMP) the following steps write into the image file: which fields, and a title,
    /// description, author or copyright of the recipe. The title and description are private details like the window title
    /// and the text: they are only written into files on this computer, before they leave it the user is asked.
    /// See docs/vNext/image-formats.md.
    /// </summary>
    [StepInfo(WellKnownStepTypes.SetMetadata, "Set Metadata", "Decides the details written into the saved image: fields, title, description, author and copyright.", "Output")]
    [StepPayload(RawCapture = PayloadRequirement.Required)]
    [StepParameter(FieldsParameter, ContractDataType.Object, ValueType = typeof(List<string>), AllowedValues = new[] { "CaptureTime", "Author", "Source", "Text", "Barcodes" },
        SupportsExpressions = false, Description = "The details to write, a list of CaptureTime, Author, Source, Text, Barcodes; without it those of the settings, an empty list none")]
    [StepParameter(TitleParameter, ContractDataType.String, Description = "The title of the image, instead of the window title (private, e.g. ${payload.title})")]
    [StepParameter(DescriptionParameter, ContractDataType.String, Description = "The description of the image, instead of the text (private)")]
    [StepParameter(AuthorParameter, ContractDataType.String, Description = "The author, instead of the one of the settings")]
    [StepParameter(CopyrightParameter, ContractDataType.String, Description = "The copyright notice, instead of the one of the settings")]
    public class SetMetadataStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SetMetadataStep));

        internal const string FieldsParameter = "Fields";
        internal const string TitleParameter = "Title";
        internal const string DescriptionParameter = "Description";
        internal const string AuthorParameter = "Author";
        internal const string CopyrightParameter = "Copyright";

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public SetMetadataStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? WellKnownStepTypes.SetMetadata;
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var payload = context.Payload;
            var details = new[] { payload?.RawCapture?.CaptureDetails, payload?.Surface?.CaptureDetails }.Where(d => d != null).Distinct().ToList();
            if (details.Count == 0)
            {
                context.LogStep("Set metadata skipped: there is no capture.");
                return Task.CompletedTask;
            }

            var values = new Dictionary<string, string>();
            if (NodeConfig.Parameters?.ContainsKey(FieldsParameter) == true)
            {
                var names = NodeConfig.GetParameter<List<string>>(FieldsParameter) ?? new List<string>();
                List<ExportMetadataField> fields;
                try
                {
                    fields = ExportImageMetadata.ParseFields(string.Join(",", names));
                }
                catch (ArgumentException ex)
                {
                    context.Abort($"Set metadata: {ex.Message}");
                    return Task.CompletedTask;
                }
                values[ExportImageMetadata.RecipeFieldsKey] = string.Join(",", fields);
            }
            AddValue(values, ExportImageMetadata.RecipeTitleKey, TitleParameter);
            AddValue(values, ExportImageMetadata.RecipeDescriptionKey, DescriptionParameter);
            AddValue(values, ExportImageMetadata.RecipeAuthorKey, AuthorParameter);
            AddValue(values, ExportImageMetadata.RecipeCopyrightKey, CopyrightParameter);

            foreach (var captureDetails in details)
            {
                Apply(captureDetails, values);
            }
            // An image encoded before has the old details
            payload.InvalidateExportSource();

            context.LogStep(values.Count == 0 ? "Set metadata: nothing to change." : $"Set metadata: {string.Join(", ", values.Keys)}");
            Log.DebugFormat("SetMetadataStep '{0}' set {1}", Name, string.Join(", ", values.Keys));
            return Task.CompletedTask;
        }

        private void AddValue(IDictionary<string, string> values, string key, string parameter)
        {
            if (NodeConfig.Parameters?.ContainsKey(parameter) == true)
            {
                // Already evaluated by the engine; an empty value removes the detail of the settings
                values[key] = NodeConfig.GetParameter<string>(parameter) ?? string.Empty;
            }
        }

        private static void Apply(ICaptureDetails captureDetails, IDictionary<string, string> values)
        {
            foreach (var value in values)
            {
                captureDetails.MetaData[value.Key] = value.Value;
            }
        }
    }
}
