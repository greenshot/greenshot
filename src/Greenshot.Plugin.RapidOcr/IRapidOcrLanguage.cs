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

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Plugin.RapidOcr
{
    /// <summary>
    /// The texts of the [RapidOcr] section of greenshot.rapidocr.{ietf}.ini, the English text is shown per property.
    /// Add a key to every language file and a property here.
    /// </summary>
    [IniLanguageSection("RapidOcr", ModuleName = "rapidocr")]
    public interface IRapidOcrLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Download
        /// </summary>
        string DownloadButton { get; }

        /// <summary>
        /// The download was cancelled, Windows OCR keeps recognizing text. Choose RapidOCR again in the settings to download it later.
        /// </summary>
        string DownloadCancelled { get; }

        /// <summary>
        /// Close
        /// </summary>
        string DownloadClose { get; }

        /// <summary>
        /// RapidOCR couldn't be downloaded ({0}). Windows OCR keeps recognizing text, choose RapidOCR again in the settings to retry.
        /// </summary>
        string DownloadFailed { get; }

        /// <summary>
        /// Inform me when done
        /// </summary>
        string DownloadInform { get; }

        /// <summary>
        /// Not now
        /// </summary>
        string DownloadLater { get; }

        /// <summary>
        /// {0:0.0} MB of {1:0.0} MB
        /// </summary>
        string DownloadProgress { get; }

        /// <summary>
        /// RapidOCR recognizes text with the PaddleOCR models, it reads small and underlined text (links) better than Windows OCR, in many languages at once. It needs {0:0} MB of files, downloaded once:{1}- onnxruntime.dll from Microsoft (github.com/microsoft/onnxruntime){1}- the PaddleOCR models from RapidAI ({2}){1}{1}They are saved in {3}. Each file is checked before it is used. Download now?
        /// </summary>
        string DownloadQuestion { get; }

        /// <summary>
        /// RapidOCR is ready, it recognizes text from now on.
        /// </summary>
        string DownloadReady { get; }

        /// <summary>
        /// RapidOCR is being downloaded. You can close this window with "Inform me when done": the download goes on and Greenshot tells you when it is ready. Until then Windows OCR recognizes text.
        /// </summary>
        string DownloadRunning { get; }

        /// <summary>
        /// Download RapidOCR
        /// </summary>
        string DownloadTitle { get; }

        /// <summary>
        /// RapidOCR (PaddleOCR models)
        /// </summary>
        string EngineName { get; }

        /// <summary>
        /// Keep the models loaded between recognitions (faster OCR, about 80 MB more memory)
        /// </summary>
        string KeepModelsLoaded { get; }

        /// <summary>
        /// RapidOCR is chosen for text recognition, but its files in {3} are missing or not the expected ones. Until they are there, Windows OCR recognizes the text.{1}{1}Download them again ({0:0} MB from github.com/microsoft/onnxruntime and {2}), or use Windows OCR from now on?
        /// </summary>
        string RepairQuestion { get; }

        /// <summary>
        /// Use Windows OCR
        /// </summary>
        string UseWindowsOcr { get; }
    }
}
