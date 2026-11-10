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
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;
using Greenshot.Plugin.RapidOcr.Files;

namespace Greenshot.Plugin.RapidOcr.ViewModels;

/// <summary>
/// Where the download window is: the question, the download, or how it ended
/// </summary>
internal enum DownloadStage
{
    Asking,
    Downloading,
    Ready,
    Failed,
    Cancelled
}

/// <summary>
/// The download of RapidOCR in one window: the question, the progress, the result. Closing the window while the files
/// are downloaded doesn't stop the download, Greenshot then informs with a notification when it ends.
/// </summary>
internal sealed class RapidOcrDownloadViewModel : INotifyPropertyChanged
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(RapidOcrDownloadViewModel));
    private readonly IRapidOcrLanguage _language;
    private readonly Func<IProgress<DownloadProgress>, CancellationToken, Task> _download;
    private readonly Action<NotificationKind, string> _notify;
    private readonly CancellationTokenSource _cancel;
    private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private DownloadStage _stage = DownloadStage.Asking;
    private double _percentage;
    private string _progressText;
    private string _currentFile;
    private string _resultText;
    private bool _windowClosed;

    /// <param name="language">the texts</param>
    /// <param name="question">what is asked before anything is downloaded</param>
    /// <param name="declineText">the button which downloads nothing (Not now, or Use Windows OCR)</param>
    /// <param name="download">downloads the files, reporting the progress</param>
    /// <param name="notify">shows a notification, used when the window was closed before the download ended</param>
    /// <param name="cancellationToken">cancels the download from outside</param>
    public RapidOcrDownloadViewModel(IRapidOcrLanguage language, string question, string declineText, Func<IProgress<DownloadProgress>, CancellationToken, Task> download,
        Action<NotificationKind, string> notify, CancellationToken cancellationToken)
    {
        _language = language;
        Question = question;
        DeclineText = declineText;
        _download = download;
        _notify = notify;
        _cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// The window asks the window to close (e.g. after Not now or Close)
    /// </summary>
    public event EventHandler CloseRequested;

    /// <summary>
    /// True when the files are there, false when the user declined, cancelled or the download failed
    /// </summary>
    public Task<bool> Completion => _completion.Task;

    public string Question { get; }

    public string DeclineText { get; }

    public DownloadStage Stage
    {
        get => _stage;
        private set
        {
            if (_stage == value) return;
            _stage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAsking));
            OnPropertyChanged(nameof(IsDownloading));
            OnPropertyChanged(nameof(IsFinished));
            OnPropertyChanged(nameof(IsError));
        }
    }

    public bool IsAsking => _stage == DownloadStage.Asking;

    public bool IsDownloading => _stage == DownloadStage.Downloading;

    public bool IsFinished => _stage is DownloadStage.Ready or DownloadStage.Failed or DownloadStage.Cancelled;

    public bool IsError => _stage == DownloadStage.Failed;

    /// <summary>0 to 100</summary>
    public double Percentage
    {
        get => _percentage;
        private set
        {
            _percentage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The file which is downloaded now</summary>
    public string CurrentFile
    {
        get => _currentFile;
        private set
        {
            _currentFile = value;
            OnPropertyChanged();
        }
    }

    /// <summary>MB received of MB</summary>
    public string ProgressText
    {
        get => _progressText;
        private set
        {
            _progressText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>How it ended</summary>
    public string ResultText
    {
        get => _resultText;
        private set
        {
            _resultText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Not now: nothing is downloaded
    /// </summary>
    public void Decline()
    {
        if (!IsAsking) return;
        Log.Info("The RapidOCR download was declined");
        Stage = DownloadStage.Cancelled;
        _completion.TrySetResult(false);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Download: the window shows the progress until it ends. Called on the UI thread.
    /// </summary>
    public async Task DownloadAsync()
    {
        if (!IsAsking) return;
        Stage = DownloadStage.Downloading;
        ProgressText = string.Format(_language.DownloadProgress, 0.0, RapidOcrFiles.DownloadMegabytes);
#pragma warning disable RS0030 // R9: UI code, DownloadAsync starts with the click in the window, so the reports go to the UI thread
        var progress = new Progress<DownloadProgress>(OnProgress);
#pragma warning restore RS0030
        bool ready = false;
        try
        {
            await _download(progress, _cancel.Token).ConfigureAwait(true);
            ready = true;
            Percentage = 100;
            Stage = DownloadStage.Ready;
            ResultText = _language.DownloadReady;
            Log.Info("RapidOCR was downloaded");
        }
        catch (OperationCanceledException) when (_cancel.IsCancellationRequested)
        {
            Log.Info("The RapidOCR download was cancelled");
            Stage = DownloadStage.Cancelled;
            ResultText = _language.DownloadCancelled;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException or TimeoutException or InvalidDataException)
        {
            Log.Warn("RapidOCR couldn't be downloaded", ex);
            Stage = DownloadStage.Failed;
            ResultText = string.Format(_language.DownloadFailed, ex.Message);
        }

        // Nobody sees the window anymore: a notification says how it ended (a cancel was the user's own choice)
        if (_windowClosed && _stage != DownloadStage.Cancelled)
        {
            _notify?.Invoke(ready ? NotificationKind.Info : NotificationKind.Error, ResultText);
        }
        _completion.TrySetResult(ready);
    }

    /// <summary>
    /// Cancel the running download
    /// </summary>
    public void Cancel()
    {
        if (IsDownloading) _cancel.Cancel();
    }

    /// <summary>
    /// Inform me: the window closes, the download goes on
    /// </summary>
    public void InformWhenDone() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Close after the download ended
    /// </summary>
    public void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// The window was closed (by a button, the title bar or with its owner): while asking that is Not now, while downloading
    /// the download goes on and informs when it ends
    /// </summary>
    public void WindowClosed()
    {
        _windowClosed = true;
        if (IsAsking) Decline();
    }

    private void OnProgress(DownloadProgress progress)
    {
        // A report which arrives after the end changes nothing
        if (!IsDownloading || progress == null) return;
        Percentage = progress.Percentage;
        ProgressText = string.Format(_language.DownloadProgress, progress.Received / 1024.0 / 1024.0, progress.Total / 1024.0 / 1024.0);
        if (progress.File != null) CurrentFile = progress.File;
    }

    private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
