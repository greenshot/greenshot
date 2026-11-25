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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Plugin.ExternalCommand.Running
{
    /// <summary>
    /// What the standard output of a command is
    /// </summary>
    public enum CommandOutput
    {
        /// <summary>Text: kept as the output, the first URL in it is the link</summary>
        Text,
        /// <summary>The bytes of an image, the new image of the flow</summary>
        Image,
        /// <summary>Not read; the command changed the file it got, the image is read again from it</summary>
        File,
        /// <summary>Not read</summary>
        None
    }

    /// <summary>
    /// What a command gets and how its output is read
    /// </summary>
    public sealed class CommandRequest
    {
        /// <summary>The program, rendered (Command context)</summary>
        public string Program { get; set; }

        /// <summary>The file of the capture, null when the command gets none; a Windows App gets only this</summary>
        public string File { get; set; }

        /// <summary>The arguments, rendered (Argument context)</summary>
        public string Arguments { get; set; }

        /// <summary>The working folder, rendered (Folder context); null: the folder of Greenshot</summary>
        public string WorkingDirectory { get; set; }

        /// <summary>The bytes written to the standard input, which is closed afterwards; null: the command gets no standard input</summary>
        public byte[] StandardInput { get; set; }

        /// <summary>What the standard output is</summary>
        public CommandOutput Output { get; set; } = CommandOutput.Text;

        /// <summary>Read the standard error (logged as a warning, the message of a failure)</summary>
        public bool ReadError { get; set; } = true;

        /// <summary>Look for a URL in the output (<see cref="CommandOutput.Text"/>)</summary>
        public bool FindUrl { get; set; } = true;

        /// <summary>The encoding of standard input, output and error text</summary>
        public Encoding Encoding { get; set; } = CommandRunner.Utf8;

        /// <summary>The command is stopped after this time; null: no limit</summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>The exit codes which mean success</summary>
        public IReadOnlyCollection<int> SuccessExitCodes { get; set; } = new[] { 0 };
    }

    /// <summary>
    /// What a command gave back
    /// </summary>
    public sealed class CommandResult
    {
        public int ExitCode { get; set; }

        /// <summary>The standard output as text (<see cref="CommandOutput.Text"/>), else null</summary>
        public string Output { get; set; }

        /// <summary>The standard output as bytes (<see cref="CommandOutput.Image"/>), else null</summary>
        public byte[] OutputBytes { get; set; }

        /// <summary>The standard error, null when not read</summary>
        public string Error { get; set; }

        /// <summary>The first URL in the output, null when there is none</summary>
        public string Url { get; set; }

        /// <summary>The command didn't finish in time and was stopped</summary>
        public bool TimedOut { get; set; }

        /// <summary>The exit code is one of the success codes and the command didn't time out</summary>
        public bool Succeeded { get; set; }

        /// <summary>Why the command failed, for the notification: the timeout, the standard error or the exit code</summary>
        public string FailureMessage { get; set; }
    }

    /// <summary>
    /// Runs a command: the program is started directly (no shell), standard input is written and output and error are read while it runs,
    /// cancelling or the timeout stops it.
    /// </summary>
    public static class CommandRunner
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CommandRunner));

        /// <summary>UTF-8 without a byte order mark</summary>
        public static readonly Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>
        /// .NET Framework writes standard input through a StreamWriter with Console.InputEncoding, which writes the preamble at once.
        /// With the UTF-8 code page (65001, e.g. "Use Unicode UTF-8 for worldwide language support") that encoding has a byte order
        /// mark, so the command would read U+FEFF before the bytes Greenshot gives it. The same code page without the mark.
        /// </summary>
        private static void InputWithoutByteOrderMark()
        {
            try
            {
                var encoding = Console.InputEncoding;
                if (encoding.CodePage == Utf8.CodePage && encoding.GetPreamble().Length > 0)
                {
                    Console.InputEncoding = Utf8;
                }
            }
            catch (IOException ex)
            {
                // No console code page to set: the encoding of the writer is then the ANSI code page
                Log.Debug("Can't set the input encoding of the console", ex);
            }
        }

        /// <summary>Text output and error are kept up to this size, the rest is cut</summary>
        public const int TextLimit = 1024 * 1024;

        /// <summary>An image on the standard output may be this large</summary>
        public const int ImageLimit = 256 * 1024 * 1024;

        private static readonly Regex UriRegex = new Regex(
            @"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)",
            RegexOptions.Compiled);

        /// <summary>
        /// The first URL in a text, null when there is none
        /// </summary>
        public static string FindUrl(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var match = UriRegex.Match(text);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// The text to copy to the clipboard: the URL when it is wanted and one was found, else the output when it is wanted; null: nothing
        /// </summary>
        public static string ClipboardTextOf(CommandResult result, bool copyOutput, bool copyUrl)
        {
            if (result == null) return null;
            if (copyUrl && !string.IsNullOrEmpty(result.Url)) return result.Url;
            return copyOutput && !string.IsNullOrEmpty(result.Output) ? result.Output : null;
        }

        /// <summary>
        /// Parse exit codes like "0, 98,99"; empty: 0
        /// </summary>
        /// <exception cref="FormatException">Not a list of numbers</exception>
        public static IReadOnlyCollection<int> ParseExitCodes(string codes)
        {
            if (string.IsNullOrWhiteSpace(codes)) return new[] { 0 };
            var result = new List<int>();
            foreach (string part in codes.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(part, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out int code))
                {
                    throw new FormatException($"'{part}' is not an exit code");
                }
                result.Add(code);
            }
            return result;
        }

        /// <summary>
        /// Run the command and wait for it.
        /// </summary>
        /// <exception cref="OperationCanceledException">Cancelled, the command was stopped</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">The program couldn't be started (not found, needs elevation)</exception>
        public static async Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Program)) throw new ArgumentException("No program to run", nameof(request));
            if (WindowsAppLauncher.AppIdOf(request.Program) is { } appId)
            {
                // Like "Open with": the app gets the file, nothing is read back and Greenshot doesn't wait for it
                if (string.IsNullOrEmpty(request.File)) throw new ArgumentException("A Windows App needs the file", nameof(request));
                WindowsAppLauncher.OpenFile(appId, request.File);
                return new CommandResult { ExitCode = 0, Succeeded = true };
            }

            bool readOutput = request.Output == CommandOutput.Text || request.Output == CommandOutput.Image;
            using var process = new Process();
            var startInfo = process.StartInfo;
            startInfo.FileName = request.Program;
            startInfo.Arguments = request.Arguments ?? string.Empty;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            if (!string.IsNullOrWhiteSpace(request.WorkingDirectory) && Directory.Exists(request.WorkingDirectory))
            {
                startInfo.WorkingDirectory = request.WorkingDirectory;
            }
            startInfo.RedirectStandardInput = request.StandardInput != null;
            if (startInfo.RedirectStandardInput)
            {
                InputWithoutByteOrderMark();
            }
            startInfo.RedirectStandardOutput = readOutput;
            startInfo.RedirectStandardError = request.ReadError;
            if (readOutput && request.Output == CommandOutput.Text)
            {
                startInfo.StandardOutputEncoding = request.Encoding ?? Utf8;
            }
            if (request.ReadError)
            {
                startInfo.StandardErrorEncoding = request.Encoding ?? Utf8;
            }

            Log.InfoFormat("Starting : {0} {1}", startInfo.FileName, startInfo.Arguments);
            process.EnableRaisingEvents = true;
            process.Start();

            using var timeout = request.Timeout.HasValue ? new CancellationTokenSource(request.Timeout.Value) : new CancellationTokenSource();
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            // Read and write while it runs: a full pipe would block the command (or us)
            var outputTask = readOutput
                ? ReadLimitedAsync(process.StandardOutput.BaseStream, request.Output == CommandOutput.Image ? ImageLimit : TextLimit, stop.Token)
                : Task.FromResult<(byte[], bool)>((null, false));
            var errorTask = request.ReadError
                ? ReadLimitedAsync(process.StandardError.BaseStream, TextLimit, stop.Token)
                : Task.FromResult<(byte[], bool)>((null, false));
            var inputTask = request.StandardInput != null
                ? WriteInputAsync(process.StandardInput.BaseStream, request.StandardInput, stop.Token)
                : Task.CompletedTask;

            try
            {
                await process.WaitForExitAsync(stop.Token).ConfigureAwait(false);
                await inputTask.ConfigureAwait(false);
                var (outputBytes, outputCut) = await outputTask.ConfigureAwait(false);
                var (errorBytes, errorCut) = await errorTask.ConfigureAwait(false);
                return CreateResult(request, process.ExitCode, outputBytes, outputCut, errorBytes, errorCut);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
            {
                Stop(process);
                Log.WarnFormat("{0} didn't finish within {1} and was stopped", startInfo.FileName, request.Timeout);
                return new CommandResult
                {
                    ExitCode = -1,
                    TimedOut = true,
                    FailureMessage = $"The command didn't finish within {request.Timeout.Value.TotalSeconds:0} seconds and was stopped."
                };
            }
            catch (OperationCanceledException)
            {
                Stop(process);
                throw;
            }
        }

        private static CommandResult CreateResult(CommandRequest request, int exitCode, byte[] outputBytes, bool outputCut, byte[] errorBytes, bool errorCut)
        {
            var encoding = request.Encoding ?? Utf8;
            var result = new CommandResult
            {
                ExitCode = exitCode,
                Error = errorBytes == null ? null : encoding.GetString(errorBytes)
            };
            if (request.Output == CommandOutput.Text && outputBytes != null)
            {
                result.Output = encoding.GetString(outputBytes);
                if (request.FindUrl)
                {
                    result.Url = FindUrl(result.Output);
                }
            }
            else if (request.Output == CommandOutput.Image)
            {
                result.OutputBytes = outputBytes;
            }

            if (outputCut)
            {
                Log.WarnFormat("The output of {0} was longer than {1} bytes, the rest was cut", request.Program, request.Output == CommandOutput.Image ? ImageLimit : TextLimit);
            }
            if (errorCut)
            {
                Log.WarnFormat("The error output of {0} was longer than {1} bytes, the rest was cut", request.Program, TextLimit);
            }
            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                Log.WarnFormat("Error:\n{0}", result.Error);
            }

            var successCodes = request.SuccessExitCodes ?? new[] { 0 };
            // An image which was cut isn't an image
            result.Succeeded = successCodes.Contains(exitCode) && !(request.Output == CommandOutput.Image && outputCut);
            if (!result.Succeeded)
            {
                result.FailureMessage = request.Output == CommandOutput.Image && outputCut
                    ? $"The image of the command is larger than {ImageLimit / (1024 * 1024)} MB."
                    : string.IsNullOrWhiteSpace(result.Error) ? $"Exit code {exitCode}" : result.Error.Trim();
            }
            Log.InfoFormat("Finished : {0} with exit code {1}", request.Program, exitCode);
            return result;
        }

        /// <summary>
        /// Read a stream to its end, keep at most limit bytes; reading goes on after the limit, so the command isn't blocked
        /// </summary>
        private static async Task<(byte[] Bytes, bool Cut)> ReadLimitedAsync(Stream stream, int limit, CancellationToken cancellationToken)
        {
            using var kept = new MemoryStream();
            var buffer = new byte[81920];
            bool cut = false;
            int read;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                int room = limit - (int)kept.Length;
                if (room > 0)
                {
                    await kept.WriteAsync(buffer, 0, Math.Min(room, read), cancellationToken).ConfigureAwait(false);
                }
                if (read > room)
                {
                    cut = true;
                }
            }
            return (kept.ToArray(), cut);
        }

        private static async Task WriteInputAsync(Stream stream, byte[] input, CancellationToken cancellationToken)
        {
            try
            {
                await stream.WriteAsync(input, 0, input.Length, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                // The command ended or closed its input before it read everything: its exit code tells whether that is a problem
                Log.DebugFormat("The command didn't read all of its standard input: {0}", ex.Message);
            }
            finally
            {
                try
                {
                    stream.Dispose();
                }
                catch (IOException)
                {
                    // Closed by the command already
                }
            }
        }

        private static void Stop(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    // .NET Framework can't stop the processes the command started (Kill(entireProcessTree) came with .NET Core 3.0)
                    process.Kill();
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
            {
                Log.DebugFormat("Couldn't stop the command: {0}", ex.Message);
            }
        }
    }
}
