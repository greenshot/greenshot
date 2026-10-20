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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.CustomUploader.Destinations;
using Greenshot.Plugin.CustomUploader.Recipes;
using Greenshot.Plugin.CustomUploader.Sxcu;
using Greenshot.Plugin.CustomUploader.ViewModels;
using Greenshot.Plugin.CustomUploader.Views;

namespace Greenshot.Plugin.CustomUploader;

/// <summary>
/// Uploads captures with the uploader files (.sxcu) of a service, a beta feature: without "beta tester" nothing is registered.
/// See docs/vNext/custom-uploader.md.
/// </summary>
public class CustomUploaderPlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(CustomUploaderPlugin));
    private static ICustomUploaderConfiguration _config;

    public ValueTask DisposeAsync() => default;

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "CustomUploader";

    /// <summary>
    /// The feature is for beta testers only
    /// </summary>
    internal static bool IsEnabled => IniConfigRegistry.GetSection<ICoreConfiguration>()?.IsBetaTester == true;

    private static ICustomUploaderLanguage Language => Texts.Get<ICustomUploaderLanguage>();

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<ICustomUploaderLanguage>(new CustomUploaderLanguageImpl());
        var section = new CustomUploaderConfigurationImpl();
        services.AddConfiguration(section);
        _config = section;

        // Decided with the loaded configuration: nothing is registered for a user who isn't a beta tester
        services.AddServices<IDestination>(() => IsEnabled ? new IDestination[] { new CustomUploaderDestination(this) } : Array.Empty<IDestination>());
        services.AddService<IIconProvider>(new CustomUploaderIconProvider());
        services.AddRecipeStepProvider(this);
        services.AddSettingsView<CustomUploaderSettingsViewModel>(viewModel => new CustomUploaderSettingsView(viewModel));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => IsEnabled ? new CustomUploaderSettingsViewModel(CreateStore()) : null;

    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null || !IsEnabled) return;
        registry.Register<CustomUploaderStep>(config => new CustomUploaderStep(config, this));
    }

    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// The uploaders and uploads of the configuration, empty before the plugin registered it
    /// </summary>
    internal static UploaderStore CreateStore()
    {
        var config = _config;
        if (config == null)
        {
            return new UploaderStore(new Dictionary<string, string>(), new Dictionary<string, string>(), null);
        }

        config.OnAfterLoad();
        return new UploaderStore(config.Uploaders, config.UploadHistory, () =>
        {
            config.MarkChanged();
            // Saved at once: an import is confirmed, and a deletion link must not get lost
            IniConfigRegistry.Get()?.Save();
        }, icons: config.UploaderIcons, reminders: config.UploadReminders);
    }

    /// <summary>
    /// What the user approves for a recipe: the uploader and its server
    /// </summary>
    internal static RecipeGatedAction CreateGatedAction(UploaderDefinition definition) =>
        new RecipeGatedAction(RecipeGateType.NetworkAccess, $"{definition.Name} ({UploaderFile.DescribeServer(definition)})");

    /// <summary>
    /// Upload the capture with the uploader, shows the progress to the user, puts the link on the clipboard and keeps the upload.
    /// </summary>
    internal async Task<ExportResult> UploadAsync(IExportSource source, ICaptureDetails captureDetails, UploaderDefinition definition, IUserInteraction userInteraction, CancellationToken cancellationToken)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));

        var outputSettings = new SurfaceOutputSettings().PreventGreenshotFormat();
        if (!await ConfirmUploadAsync(source, definition, outputSettings, userInteraction, cancellationToken).ConfigureAwait(false))
        {
            return ExportResult.Declined;
        }

        string filename = Path.GetFileName(FilenameHelper.GetFilename(outputSettings.Format, captureDetails));
        var image = await source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);

        UploaderResponse response;
        try
        {
            response = await userInteraction.RunWithProgressAsync(string.Format(Language.CommunicationWait, definition.Name),
                (progress, token) => SendAsync(HttpService.WithoutRedirects, definition, image, filename, progress, token), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException)
        {
            Log.Warn($"Upload with {definition.Name} failed", ex);
            await ShowFailureAsync(definition, ex.Message, image, filename, userInteraction, cancellationToken).ConfigureAwait(false);
            return ExportResult.Failed(ex.Message, ex);
        }

        if (!response.IsSuccess)
        {
            Log.WarnFormat("Upload with {0} failed, the service didn't accept it: {1}", definition.Name, response.Error);
            await ShowFailureAsync(definition, response.Error, image, filename, userInteraction, cancellationToken).ConfigureAwait(false);
            return ExportResult.Failed(response.Error);
        }

        if (response.Url == null)
        {
            Log.WarnFormat("Upload with {0}: the response has no link", definition.Name);
            await ShowFailureAsync(definition, Language.NoLink, image, filename, userInteraction, cancellationToken).ConfigureAwait(false);
            return ExportResult.Failed(Language.NoLink);
        }

        CreateStore().AddUpload(new UploadRecord
        {
            Uploader = definition.Name,
            Url = response.Url.AbsoluteUri,
            DeletionUrl = response.DeletionUrl?.AbsoluteUri,
            Uploaded = DateTimeOffset.Now
        });
        await ClipboardService.Current.SetTextAsync(response.Url.AbsoluteUri, cancellationToken).ConfigureAwait(false);
        return ExportResult.Succeeded(uri: response.Url);
    }

    /// <summary>
    /// Tell the user why the upload failed and that the service and the uploader file aren't Greenshot's, with the details for
    /// whoever can fix it. Only for an interactive export, a recipe run logs and notifies as before.
    /// </summary>
    private static Task ShowFailureAsync(UploaderDefinition definition, string error, EncodedImage image, string filename, IUserInteraction userInteraction,
        CancellationToken cancellationToken)
    {
        if (!userInteraction.IsInteractive)
        {
            return Task.CompletedTask;
        }

        string server = UploaderFile.DescribeServer(definition);
        var request = new ErrorDetailsRequest(string.Format(Language.UploadFailedTitle, definition.Name), string.Format(Language.UploadFailedMessage, server, error),
            string.Format(Language.UploadFailedNote, definition.Name, server), DescribeFailure(definition, error, image, filename, DateTimeOffset.Now));
        return userInteraction.ShowDialogAsync(request, cancellationToken);
    }

    /// <summary>
    /// The details of a failed upload, to pass on to the service or the author of the uploader file: what was sent, by name only
    /// (no values: they hold the keys), and what the service answered
    /// </summary>
    internal static string DescribeFailure(UploaderDefinition definition, string error, EncodedImage image, string filename, DateTimeOffset time)
    {
        static string Names(IDictionary<string, string> values) => values?.Count > 0 ? string.Join(", ", values.Keys) : "-";

        string file = $"\"{filename}\", {image.MimeType}, {image.Length} bytes";
        string body = definition.Body == "Binary"
            ? $"Binary ({file})"
            : $"MultipartFormData, fields {Names(definition.Arguments)}, file {definition.FileFormName} ({file})";
        return string.Join(Environment.NewLine,
            $"Uploader: {definition.Name} (ShareX {definition.Version ?? "-"})",
            $"Server: {UploaderFile.GetServer(definition)}",
            $"Request: {definition.RequestMethod}, {body}",
            $"Address parameters: {Names(definition.Parameters)}",
            $"Headers: {Names(definition.Headers)}",
            $"Answer: {error}",
            $"Time: {time:yyyy-MM-dd HH:mm:ss zzz}",
            $"Greenshot: {EnvironmentInfo.GetGreenshotVersion()}");
    }

    /// <summary>
    /// The reminder before an upload which can't be deleted, when the user asked for it at the import: the capture as it is
    /// uploaded, Upload or Cancel, "don't remind me again". Only for an interactive export, a recipe was approved for the server.
    /// </summary>
    /// <returns>true to upload</returns>
    private static async Task<bool> ConfirmUploadAsync(IExportSource source, UploaderDefinition definition, SurfaceOutputSettings outputSettings, IUserInteraction userInteraction,
        CancellationToken cancellationToken)
    {
        var store = CreateStore();
        var uploader = store.Find(definition.Name);
        if (uploader == null || !store.RemindsBeforeUpload(uploader.Id) || !userInteraction.IsInteractive)
        {
            return true;
        }

        using var preview = await source.RenderAsync(outputSettings, cancellationToken).ConfigureAwait(false);
        string server = UploaderFile.DescribeServer(definition);
        var request = new PreviewPromptRequest(string.Format(Language.ReminderTitle, definition.Name), string.Format(Language.ReminderMessage, server), preview.Image,
            new[]
            {
                new PromptChoice(UploadChoice, Language.ReminderUpload, "Primary"),
                new PromptChoice("Cancel", Texts.Core.Cancel, "Secondary", isDefault: true, isCancel: true)
            }, string.Format(Language.ReminderStop, definition.Name));
        var result = await userInteraction.ShowDialogAsync(request, cancellationToken).ConfigureAwait(false);
        if (result?.ChoiceKey != UploadChoice)
        {
            return false;
        }

        if (result.IsChecked)
        {
            store.StopReminding(uploader.Id);
        }

        return true;
    }

    private const string UploadChoice = "Upload";

    /// <summary>
    /// Send the capture and read the response, without UI (tests take their own HttpService).
    /// </summary>
    internal static async Task<UploaderResponse> SendAsync(HttpService httpService, UploaderDefinition definition, EncodedImage image, string filename, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
    {
        var request = UploaderRequest.Create(definition, image, filename, progress);
        var result = await httpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return UploaderResponse.From(definition, result, filename);
    }

    /// <summary>
    /// The readable uploaders
    /// </summary>
    internal static UploaderDefinition[] GetUploaders() =>
        CreateStore().GetUploaders().Where(uploader => uploader.Definition != null).Select(uploader => uploader.Definition).ToArray();
}
