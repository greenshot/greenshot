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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Base.Threading;

namespace Greenshot.Tests.Core;

/// <summary>
/// Runs a form on its own STA thread with a message loop, so it paints and delivers frames to Windows Graphics Capture
/// while the test awaits on the thread pool.
/// </summary>
internal sealed class TestFormHost : IDisposable
{
    private readonly Thread _thread;
    private Form _form;

    private TestFormHost(Func<Form> createForm, Action<Form> afterShown)
    {
        var ready = Tcs.Create<bool>();
        _thread = new Thread(() =>
        {
            try
            {
                _form = createForm();
                _form.Shown += (_, _) =>
                {
                    afterShown?.Invoke(_form);
                    ready.TrySetResult(true);
                };
                Application.Run(_form);
            }
            catch (Exception ex)
            {
                ready.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "Test form"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        Ready = ready.Task;
    }

    /// <summary>
    /// Completes when the form is shown
    /// </summary>
    public Task Ready { get; }

    public Form Form => _form;

    /// <summary>
    /// Show the form, completes when it is shown (and afterShown ran on the form's thread)
    /// </summary>
    public static async Task<TestFormHost> ShowAsync(Func<Form> createForm, Action<Form> afterShown = null)
    {
        var host = new TestFormHost(createForm, afterShown);
        await host.Ready.WaitAsync(TimeSpan.FromSeconds(10));
        // Give the window time to paint
        await Task.Delay(300);
        return host;
    }

    /// <summary>
    /// Run the action on the form's thread
    /// </summary>
    public Task InvokeAsync(Action<Form> action)
    {
        var done = Tcs.Create<bool>();
        _form.BeginInvoke(new Action(() =>
        {
            try
            {
                action(_form);
                done.TrySetResult(true);
            }
            catch (Exception ex)
            {
                done.TrySetException(ex);
            }
        }));
        return done.Task;
    }

    public void Dispose()
    {
        if (_form is { IsDisposed: false })
        {
            _form.BeginInvoke(new Action(() => _form.Close()));
        }

        _thread.Join(TimeSpan.FromSeconds(5));
    }
}
