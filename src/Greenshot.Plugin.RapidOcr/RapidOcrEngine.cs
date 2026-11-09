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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Dapplo.Windows.Kernel32;
using Greenshot.Base.Core;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.RapidOcr.Files;
using Greenshot.Plugin.RapidOcr.Recognition;
using Greenshot.Plugin.RapidOcr.ViewModels;
using Greenshot.Plugin.RapidOcr.Views;

namespace Greenshot.Plugin.RapidOcr;

/// <summary>
/// The OCR engine RapidOcr: the PaddleOCR models on ONNX Runtime, both downloaded when the user agrees (see docs/vNext/rapid-ocr.md)
/// </summary>
internal sealed class RapidOcrEngine : IOcrEngine, IDisposable
{
    internal const string EngineId = "RapidOcr";
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(RapidOcrEngine));
    private readonly RapidOcrFiles _files;
    private readonly object _lock = new();
    private RapidOcrPipeline _pipeline;
    private bool _invalid;
    private Task<bool> _preparing;
    private Window _window;

    public RapidOcrEngine(RapidOcrFiles files)
    {
        _files = files;
    }

    private static IRapidOcrLanguage Language => Texts.Get<IRapidOcrLanguage>();

    public string Id => EngineId;

    public string DisplayName => Language.EngineName;

    /// <summary>
    /// The files are downloaded (and weren't found damaged)
    /// </summary>
    public bool IsReady => !_invalid && _files.AreComplete;

    /// <summary>
    /// Asks in the download window and downloads; must be called on the UI thread. While a download runs, its window comes
    /// to the front (if still open) and the same download is awaited.
    /// </summary>
    /// <returns>true when the files are there, false when the user declined or cancelled, or the download failed</returns>
    public Task<bool> PrepareAsync(IntPtr owner, CancellationToken cancellationToken)
    {
        if (IsReady) return Task.FromResult(true);
        if (_preparing is { IsCompleted: false })
        {
            _window?.Activate();
            return _preparing;
        }

        string question = string.Format(Language.DownloadQuestion, RapidOcrFiles.DownloadMegabytes, Environment.NewLine, "modelscope.cn", _files.Directory);
        var viewModel = new RapidOcrDownloadViewModel(Language, question,
            (progress, token) => _files.DownloadAsync(HttpService.Current, progress, token), Notify, cancellationToken);
        var window = new RapidOcrDownloadWindow(viewModel);
        if (owner != IntPtr.Zero)
        {
            new WindowInteropHelper(window).Owner = owner;
        }
        else
        {
            window.Owner = ActiveWindow();
            if (window.Owner == null) window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        window.Closed += (_, _) => _window = null;
        _window = window;
        window.Show();
        _preparing = FinishAsync(viewModel);
        return _preparing;
    }

    private async Task<bool> FinishAsync(RapidOcrDownloadViewModel viewModel)
    {
        bool ready = await viewModel.Completion.ConfigureAwait(false);
        if (ready)
        {
            _invalid = false;
            Log.InfoFormat("RapidOCR is in {0}", _files.Directory);
        }
        return ready;
    }

    public Task<List<IOcrLineFeature>> DoOcrAsync(ISurface surface, string languageTag = null) => DoOcrAsync(surface?.Image, languageTag);

    /// <summary>
    /// The pixels are copied before this returns; the models read every language they know, the language tag isn't used
    /// </summary>
    public Task<List<IOcrLineFeature>> DoOcrAsync(Image image, string languageTag = null)
    {
        if (image is not Bitmap bitmap) return Task.FromResult(new List<IOcrLineFeature>());

        var pixels = RapidOcrPipeline.FromBitmap(bitmap);
        // PARALLEL: the models take about a second on a full screen capture, the caller (often the UI) must not wait for it
#pragma warning disable RS0030 // R10: documented parallel branch
        return Task.Run(() =>
        {
            var pipeline = Pipeline();
            return pipeline == null ? new List<IOcrLineFeature>() : pipeline.Read(pixels);
        });
#pragma warning restore RS0030
    }

    /// <summary>
    /// One set of languages for all: the models aren't chosen by language
    /// </summary>
    public IList<OcrLanguage> GetAvailableLanguages() => new List<OcrLanguage>();

    /// <summary>
    /// The models, loaded the first time: every file checked against its SHA-256 before ONNX Runtime and the models are loaded
    /// </summary>
    private RapidOcrPipeline Pipeline()
    {
        lock (_lock)
        {
            if (_pipeline != null || _invalid) return _pipeline;

            var damaged = RapidOcrFiles.All.Where(file => !_files.IsValid(file)).Select(file => file.Name).ToList();
            if (damaged.Count > 0)
            {
                _invalid = true;
                Log.WarnFormat("RapidOCR files not as expected, not used: {0}", string.Join(", ", damaged));
                Notify(NotificationKind.Warning, string.Format(Language.FilesInvalid, _files.Directory));
                return null;
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Loaded with its full path; ONNX Runtime's own lookup of "onnxruntime" then finds this module
                string native = _files.PathOf(RapidOcrFiles.OnnxRuntimeFile);
                if (Kernel32Api.LoadLibrary(native) == IntPtr.Zero)
                {
                    _invalid = true;
                    Log.WarnFormat("Couldn't load {0}", native);
                    return null;
                }
            }

            _pipeline = new RapidOcrPipeline(_files.PathOf(RapidOcrFiles.DetectionFile), _files.PathOf(RapidOcrFiles.RecognitionFile));
            return _pipeline;
        }
    }

    private static Window ActiveWindow() =>
        Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);

    private static void Notify(NotificationKind kind, string message)
    {
        try
        {
            SimpleServiceProvider.Current.GetInstance<INotificationService>(isOptional: true)?.ShowNotification(new Notification(kind, message));
        }
        catch (Exception ex)
        {
            Log.Warn("Couldn't show the notification", ex);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _pipeline?.Dispose();
            _pipeline = null;
        }
    }
}
