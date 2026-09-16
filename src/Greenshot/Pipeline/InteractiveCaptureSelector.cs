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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Forms;
using log4net;

namespace Greenshot.Pipeline
{
    /// <summary>
    /// Presents the CaptureForm to the user on the UI thread and returns the selection.
    /// </summary>
    public class InteractiveCaptureSelector : IInteractiveCaptureSelector
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(InteractiveCaptureSelector));
        private static int _activeSelectionCount;

        public Task<SelectionResult> SelectAsync(
            ICapture fullscreenCapture,
            List<WindowDetails> visibleWindows,
            CaptureMode initialMode,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _activeSelectionCount, 1, 0) != 0)
            {
                Log.Warn("Interactive selection is already in progress. Ignoring concurrent selection request.");
                return Task.FromResult(SelectionResult.Cancelled());
            }

            var tcs = new TaskCompletionSource<SelectionResult>();
            var uiContext = SimpleServiceProvider.Current.GetInstance<SynchronizationContext>(isOptional: true) ?? SynchronizationContext.Current;

            void ShowForm()
            {
                try
                {
                    if (fullscreenCapture?.CaptureDetails != null)
                    {
                        fullscreenCapture.CaptureDetails.CaptureMode = initialMode;
                    }
                    using CaptureForm captureForm = new CaptureForm(fullscreenCapture, visibleWindows);
                    var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>();
                    DialogResult result;
                    try
                    {
                        result = captureForm.ShowDialog(mainForm as IWin32Window);
                    }
                    finally
                    {
                        captureForm.Hide();
                    }

                    if (result != DialogResult.OK)
                    {
                        tcs.SetResult(SelectionResult.Cancelled());
                        return;
                    }

                    var selResult = new SelectionResult
                    {
                        IsCancelled = false,
                        SelectedRegion = captureForm.CaptureRectangle,
                        SelectedWindow = captureForm.SelectedCaptureWindow,
                        FinalMode = captureForm.UsedCaptureMode
                    };

                    tcs.SetResult(selResult);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    Interlocked.Exchange(ref _activeSelectionCount, 0);
                }
            }

            if (uiContext != null && SynchronizationContext.Current != uiContext)
            {
                uiContext.Post(_ => ShowForm(), null);
            }
            else
            {
                ShowForm();
            }

            return tcs.Task;
        }
    }
}
