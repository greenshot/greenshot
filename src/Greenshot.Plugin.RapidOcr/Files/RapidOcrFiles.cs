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
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;

namespace Greenshot.Plugin.RapidOcr.Files;

/// <summary>
/// A file RapidOCR needs: where it comes from and the SHA-256 it must have. A file with another hash is never used.
/// </summary>
internal sealed class RapidOcrFile
{
    public RapidOcrFile(string name, string url, string zipEntry, long size, long transferSize, string sha256)
    {
        Name = name;
        Url = url;
        ZipEntry = zipEntry;
        Size = size;
        TransferSize = transferSize;
        Sha256 = sha256;
    }

    /// <summary>The name in the folder</summary>
    public string Name { get; }

    public string Url { get; }

    /// <summary>The entry in the zip at the url, null when the url is the file itself</summary>
    public string ZipEntry { get; }

    public long Size { get; }

    /// <summary>What is transferred: the compressed size of a zip entry, else the size</summary>
    public long TransferSize { get; }

    public string Sha256 { get; }
}

/// <summary>
/// How far a download is: the file which is downloaded now (null at the end) and the bytes of all files
/// </summary>
internal sealed class DownloadProgress
{
    public DownloadProgress(string file, long received, long total)
    {
        File = file;
        Received = received;
        Total = total;
    }

    public string File { get; }

    public long Received { get; }

    public long Total { get; }

    /// <summary>0 to 100</summary>
    public double Percentage => Total <= 0 ? 100 : 100.0 * Received / Total;
}

/// <summary>
/// The files of RapidOCR in their folder: which are there, downloading the missing ones, checking them before they are used
/// </summary>
internal sealed class RapidOcrFiles
{
    internal const string OnnxRuntimeVersion = "1.31.1";
    private const string OnnxRuntimeZip = "https://github.com/microsoft/onnxruntime/releases/download/v" + OnnxRuntimeVersion + "/onnxruntime-win-x64-" + OnnxRuntimeVersion + ".zip";
    // The models as RapidOCR 3.10 publishes them (PP-OCRv6 small, Apache 2.0)
    private const string ModelBase = "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.10.0/onnx/PP-OCRv6/";

    internal const string OnnxRuntimeFile = "onnxruntime.dll";
    internal const string DetectionFile = "PP-OCRv6_det_small.onnx";
    internal const string RecognitionFile = "PP-OCRv6_rec_small.onnx";

    /// <summary>
    /// Everything which is downloaded, with the expected size and SHA-256
    /// </summary>
    internal static readonly IReadOnlyList<RapidOcrFile> All = new[]
    {
        new RapidOcrFile(OnnxRuntimeFile, OnnxRuntimeZip, $"onnxruntime-win-x64-{OnnxRuntimeVersion}/lib/onnxruntime.dll", 17253728, 6178062,
            "cb2fa518b18356617f6f9f460c012501da2819ec7e5c1d80bd8f5de20492db0c"),
        new RapidOcrFile("onnxruntime-LICENSE.txt", OnnxRuntimeZip, $"onnxruntime-win-x64-{OnnxRuntimeVersion}/LICENSE", 1094, 637,
            "c250d6278f0b47a6439fb7592b08b58a55eb9f535aa49a1db63211c3f982b674"),
        new RapidOcrFile(DetectionFile, ModelBase + "det/" + DetectionFile, null, 9929594, 9929594,
            "090f04abcd9d9a7498bc4ebf677e4cb9bdce1fe4197ddb7e529f1ef44e1ff94f"),
        new RapidOcrFile(RecognitionFile, ModelBase + "rec/" + RecognitionFile, null, 21234383, 21234383,
            "6f327246b50388f3c176ae304bd95767ea6dc0c9ae92153ef8cbe210b3c14884"),
    };

    public RapidOcrFiles(string directory)
    {
        Directory = directory;
    }

    /// <summary>The folder of the files</summary>
    public string Directory { get; }

    /// <summary>What is downloaded when nothing is there yet, in MB</summary>
    public static double DownloadMegabytes => All.Sum(file => file.TransferSize) / 1024.0 / 1024.0;

    public string PathOf(string name) => Path.Combine(Directory, name);

    /// <summary>
    /// All files are there with the right size (the hash is checked when they are loaded)
    /// </summary>
    public bool AreComplete => All.All(file =>
    {
        var info = new FileInfo(PathOf(file.Name));
        return info.Exists && info.Length == file.Size;
    });

    /// <summary>
    /// The file is the expected one: its SHA-256
    /// </summary>
    public bool IsValid(RapidOcrFile file)
    {
        string path = PathOf(file.Name);
        if (!File.Exists(path)) return false;
        using var stream = File.OpenRead(path);
        return stream.Length == file.Size && string.Equals(Hash(stream), file.Sha256, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Download every file which is missing or not as expected; each is checked before it is saved
    /// </summary>
    /// <exception cref="InvalidDataException">a downloaded file isn't the expected one</exception>
    public Task DownloadAsync(HttpService http, CancellationToken cancellationToken) => DownloadAsync(http, null, cancellationToken);

    /// <summary>
    /// Download every file which is missing or not as expected, reporting the bytes of all of them; each is checked before it is saved
    /// </summary>
    /// <exception cref="InvalidDataException">a downloaded file isn't the expected one</exception>
    public async Task DownloadAsync(HttpService http, IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var missing = All.Where(file => !IsValid(file)).ToList();
        long total = missing.Sum(file => file.TransferSize);
        long done = 0;
        foreach (var file in missing)
        {
            long before = done;
            var received = progress == null ? null : new SynchronousProgress<long>(bytes =>
                progress.Report(new DownloadProgress(file.Name, Math.Min(total, before + bytes), total)));
            received?.Report(0);

            var content = file.ZipEntry == null
                ? await http.GetBytesAsync(new HttpRequestMessage(HttpMethod.Get, file.Url), received, cancellationToken).ConfigureAwait(false)
                : await RemoteZip.ReadEntryAsync(http, file.Url, file.ZipEntry, received, cancellationToken).ConfigureAwait(false);
            done += file.TransferSize;
            string hash;
            using (var stream = new MemoryStream(content, false))
            {
                hash = Hash(stream);
            }
            if (content.Length != file.Size || !string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"{file.Name} isn't the expected file");
            }

            // Written next to it first, so an interrupted download leaves no half file under the name
            string path = PathOf(file.Name);
            string temporary = path + ".download";
            File.WriteAllBytes(temporary, content);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }

        progress?.Report(new DownloadProgress(null, total, total));
    }

    /// <summary>
    /// Calls the action right away, on the thread which reports: the bytes are turned into one report for the caller,
    /// whose own IProgress then goes to its thread
    /// </summary>
    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _action;

        public SynchronousProgress(Action<T> action) => _action = action;

        public void Report(T value) => _action(value);
    }

    private static string Hash(Stream stream)
    {
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(stream).Select(b => b.ToString("x2")));
    }
}
