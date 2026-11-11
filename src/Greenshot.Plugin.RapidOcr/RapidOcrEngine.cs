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
    private readonly IRapidOcrConfiguration _configuration;
    private readonly object _lock = new();
    private RapidOcrPipeline _pipeline;
    // The OCR runs using the pipeline right now: it is only unloaded when none uses it
    private int _users;
    private bool _filesChecked;
    private bool _invalid;
    private Task<bool> _preparing;
    private Window _window;

    public RapidOcrEngine(RapidOcrFiles files, IRapidOcrConfiguration configuration)
    {
        _files = files;
        _configuration = configuration;
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
    public Task<bool> PrepareAsync(IntPtr owner, bool isChosen, CancellationToken cancellationToken)
    {
        if (IsReady) return Task.FromResult(true);
        if (_preparing is { IsCompleted: false })
        {
            _window?.Activate();
            return _preparing;
        }

        // Chosen but not usable: the files were deleted or changed, the other answer is Windows OCR
        string question = string.Format(isChosen ? Language.RepairQuestion : Language.DownloadQuestion, RapidOcrFiles.DownloadMegabytes, Environment.NewLine, RapidOcrFiles.ModelHost, _files.Directory);
        var viewModel = new RapidOcrDownloadViewModel(Language, question, isChosen ? Language.UseWindowsOcr : Language.DownloadLater,
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
            _filesChecked = false;
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
            using (pixels)
            {
                var pipeline = Acquire();
                if (pipeline == null) return new List<IOcrLineFeature>();
                try
                {
                    return pipeline.Read(pixels);
                }
                finally
                {
                    Release();
                }
            }
        });
#pragma warning restore RS0030
    }

    /// <summary>
    /// One set of languages for all: the models aren't chosen by language
    /// </summary>
    public IList<OcrLanguage> GetAvailableLanguages() => new List<OcrLanguage>();

    /// <summary>
    /// The pipeline for one OCR run, given back with Release
    /// </summary>
    private RapidOcrPipeline Acquire()
    {
        lock (_lock)
        {
            var pipeline = Pipeline();
            if (pipeline != null) _users++;
            return pipeline;
        }
    }

    /// <summary>
    /// Unless KeepModelsLoaded is set, the models (about 80 MB) are unloaded after each use, once no other run uses them; the next OCR
    /// loads them again (about 0.1-0.2 s)
    /// </summary>
    private void Release()
    {
        lock (_lock)
        {
            _users--;
            if (_users > 0 || _pipeline == null || _configuration.KeepModelsLoaded) return;
            _pipeline.Dispose();
            _pipeline = null;
        }
    }

    /// <summary>
    /// The models, loaded when needed: every file checked against its SHA-256 before ONNX Runtime and the models are loaded. Called
    /// under the lock
    /// </summary>
    private RapidOcrPipeline Pipeline()
    {
        if (_pipeline != null || _invalid) return _pipeline;
        if (!_filesChecked && !CheckFiles()) return null;

        _pipeline = new RapidOcrPipeline(_files.PathOf(RapidOcrFiles.DetectionFile), _files.PathOf(RapidOcrFiles.RecognitionFile));
        return _pipeline;
    }

    /// <summary>
    /// Once per run of Greenshot, before the first load: the SHA-256 of every file, and onnxruntime.dll loaded from its full path
    /// </summary>
    private bool CheckFiles()
    {
        var damaged = RapidOcrFiles.All.Where(file => !_files.IsValid(file)).Select(file => file.Name).ToList();
        if (damaged.Count > 0)
        {
            // Not ready anymore: Greenshot recognizes this capture with Windows OCR and asks to download them again
            _invalid = true;
            Log.WarnFormat("RapidOCR files not as expected, not used: {0}", string.Join(", ", damaged));
            return false;
        }

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            // Loaded with its full path; ONNX Runtime's own lookup of "onnxruntime" then finds this module
            string native = _files.PathOf(RapidOcrFiles.OnnxRuntimeFile);
            if (Kernel32Api.LoadLibrary(native) == IntPtr.Zero)
            {
                _invalid = true;
                Log.WarnFormat("Couldn't load {0}", native);
                return false;
            }
        }

        _filesChecked = true;
        return true;
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
