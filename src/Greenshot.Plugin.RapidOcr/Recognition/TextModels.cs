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

/*
 * Parts of this file are ported from RapidOCR (https://github.com/RapidAI/RapidOCR),
 * Copyright (c) 2021 RapidOCR Authors, licensed under the Apache License, Version 2.0
 * (see Licenses/RapidOCR-LICENSE.txt and Licenses/NOTICE.txt of this plug-in).
 * Changed by the Greenshot developers: the TextDetector and TextRecognizer of RapidOCR 3.10: ported to C#, word boxes from the CTC decoding.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Greenshot.Plugin.RapidOcr.Recognition;

/// <summary>
/// The text of a cut out line and where its words are, in pixels of the cut out line
/// </summary>
internal sealed class RecognizedLine
{
    public RecognizedLine(string text, double score, IReadOnlyList<(string Text, double Start, double End, double Score)> words)
    {
        Text = text;
        Score = score;
        Words = words;
    }

    public string Text { get; }

    /// <summary>The mean probability of the characters</summary>
    public double Score { get; }

    /// <summary>The words, where they start and end along the line and the mean probability of their characters</summary>
    public IReadOnlyList<(string Text, double Start, double End, double Score)> Words { get; }
}

/// <summary>
/// The text detection model: a probability map of text pixels, as the TextDetector of RapidOCR (Apache 2.0)
/// </summary>
internal sealed class TextDetector : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;

    public TextDetector(InferenceSession session)
    {
        _session = session;
        _inputName = session.InputMetadata.Keys.First();
    }

    public List<TextBox> Detect(BgrImage image)
    {
        // Both sides multiples of 32. RapidOCR enlarges the shorter side to 736 pixels (limit_type min), made for photos: a capture
        // has the text in its real size, enlarging found the same lines on the samples but took up to 40 times longer (a line of
        // text 831 x 121 became 5056 x 736: 2 s instead of 50 ms)
        int width = Math.Max(32, (int)Math.Round(image.Width / 32.0) * 32);
        int height = Math.Max(32, (int)Math.Round(image.Height / 32.0) * 32);
        // The input and the output stay outside the managed heap (OrtValue in native memory, read as spans): a capture of the screen
        // is millions of floats
        using var input = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, new long[] { 1, 3, height, width });
        image.WriteNormalized(input.GetTensorMutableDataAsSpan<float>(), 0, width * height, width, width, height);
        using var runOptions = new RunOptions();
        using var outputs = _session.Run(runOptions, new[] { _inputName }, new[] { input }, _session.OutputNames);
        var output = outputs.First();
        var shape = output.GetTensorTypeAndShape().Shape;
        int mapHeight = (int)shape[2], mapWidth = (int)shape[3];
        return TextBoxFinder.Find(output.GetTensorDataAsSpan<float>(), mapWidth, mapHeight, image.Width, image.Height);
    }

    public void Dispose() => _session.Dispose();
}

/// <summary>
/// The text recognition model: the characters of cut out lines (CTC), as the TextRecognizer of RapidOCR (Apache 2.0)
/// </summary>
internal sealed class TextRecognizer : IDisposable
{
    private const int ImageHeight = 48;
    private const int DefaultImageWidth = 320;
    private const int BatchSize = 6;
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly string[] _characters;

    public TextRecognizer(InferenceSession session)
    {
        _session = session;
        _inputName = session.InputMetadata.Keys.First();
        if (!session.ModelMetadata.CustomMetadataMap.TryGetValue("character", out string characters))
        {
            throw new InvalidOperationException("The recognition model has no character list");
        }

        // CTC: 0 is the blank, the space comes last
        var list = new List<string> { string.Empty };
        list.AddRange(characters.Replace("\r\n", "\n").Split('\n'));
        list.Add(" ");
        _characters = list.ToArray();
    }

    public RecognizedLine[] Recognize(IReadOnlyList<BgrImage> lines)
    {
        var results = new RecognizedLine[lines.Count];
        // Similar widths together, the batch is padded to the widest
        var order = Enumerable.Range(0, lines.Count).OrderBy(index => (double)lines[index].Width / lines[index].Height).ToArray();
        for (int start = 0; start < order.Length; start += BatchSize)
        {
            var batch = order.Skip(start).Take(BatchSize).ToArray();
            double maxRatio = Math.Max((double)DefaultImageWidth / ImageHeight, batch.Max(index => (double)lines[index].Width / lines[index].Height));
            int imageWidth = (int)(ImageHeight * maxRatio);

            using var input = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, TensorElementType.Float, new long[] { batch.Length, 3, ImageHeight, imageWidth });
            var buffer = input.GetTensorMutableDataAsSpan<float>();
            // The padding right of the narrower lines of the batch
            buffer.Clear();
            var resizedWidths = new int[batch.Length];
            int plane = ImageHeight * imageWidth;
            for (int b = 0; b < batch.Length; b++)
            {
                var line = lines[batch[b]];
                double ratio = (double)line.Width / line.Height;
                int resizedWidth = (int)Math.Ceiling(ImageHeight * ratio) > imageWidth ? imageWidth : (int)Math.Ceiling(ImageHeight * ratio);
                resizedWidths[b] = resizedWidth;
                line.WriteNormalized(buffer, b * 3 * plane, plane, imageWidth, resizedWidth, ImageHeight);
            }

            using var runOptions = new RunOptions();
            using var outputs = _session.Run(runOptions, new[] { _inputName }, new[] { input }, _session.OutputNames);
            var output = outputs.First();
            var shape = output.GetTensorTypeAndShape().Shape;
            int steps = (int)shape[1], classes = (int)shape[2];
            // The output as it is, in native memory (steps x classes values per line, about 18000 classes)
            var values = output.GetTensorDataAsSpan<float>();
            for (int b = 0; b < batch.Length; b++)
            {
                var line = lines[batch[b]];
                // One step of the output covers this many pixels of the cut out line
                double stepWidth = (double)imageWidth / steps * line.Width / resizedWidths[b];
                // The steps right of the line only see the padding of the batch
                int usedSteps = Math.Min(steps, (int)Math.Ceiling((double)resizedWidths[b] * steps / imageWidth) + 1);
                results[batch[b]] = Decode(values.Slice(b * steps * classes, usedSteps * classes), usedSteps, classes, stepWidth);
            }
        }

        return results;
    }

    /// <summary>
    /// Greedy CTC: the most likely class per step, repeats and blanks removed; words split at spaces
    /// </summary>
    private RecognizedLine Decode(ReadOnlySpan<float> values, int steps, int classes, double stepWidth)
    {
        var text = new System.Text.StringBuilder();
        var words = new List<(string, double, double, double)>();
        var word = new System.Text.StringBuilder();
        int wordStart = -1, wordEnd = -1;
        double sum = 0, wordSum = 0;
        int count = 0, wordCount = 0;
        int previous = -1;
        for (int step = 0; step < steps; step++)
        {
            int best = 0;
            float bestValue = float.MinValue;
            var row = values.Slice(step * classes, classes);
            for (int c = 0; c < row.Length; c++)
            {
                if (row[c] > bestValue)
                {
                    bestValue = row[c];
                    best = c;
                }
            }

            bool selected = best != previous && best != 0;
            previous = best;
            if (!selected || best >= _characters.Length) continue;

            string character = _characters[best];
            text.Append(character);
            sum += bestValue;
            count++;
            if (string.IsNullOrWhiteSpace(character))
            {
                AddWord();
                continue;
            }

            if (wordStart < 0) wordStart = step;
            wordEnd = step;
            word.Append(character);
            wordSum += bestValue;
            wordCount++;
        }

        AddWord();
        return new RecognizedLine(text.ToString(), count == 0 ? 0 : sum / count, words);

        void AddWord()
        {
            if (word.Length == 0) return;
            // A character is seen around its step: from half a step before the first to one and a half after the last
            words.Add((word.ToString(), Math.Max(0, (wordStart - 0.5) * stepWidth), (wordEnd + 1.5) * stepWidth, wordSum / wordCount));
            word.Clear();
            wordSum = 0;
            wordCount = 0;
            wordStart = -1;
        }
    }

    public void Dispose() => _session.Dispose();
}
