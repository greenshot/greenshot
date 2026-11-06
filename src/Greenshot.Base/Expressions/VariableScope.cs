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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// What is known where a text is used. Everything is optional: a value that isn't known is missing (empty).
    /// </summary>
    public sealed class VariableScope
    {
        /// <summary>No capture, no recipe: settings previews, folders</summary>
        public static VariableScope Empty => new VariableScope();

        /// <summary>The details of the capture or opened file, used when there is no recipe payload</summary>
        public ICaptureDetails Capture { get; set; }

        /// <summary>The running recipe, for var.*, option.*, recipe.* and the payload</summary>
        public CaptureFlowContext Flow { get; set; }

        /// <summary>The current image size, when it isn't the payload's (an annotation step on a surface, a preview)</summary>
        public Size? ImageSize { get; set; }

        /// <summary>The folder a file name goes to, needed by counter.folder and counter.name</summary>
        public string TargetFolder { get; set; }

        /// <summary>The file written by the running export, for output.*</summary>
        public string OutputFile { get; set; }

        /// <summary>The link of the running upload, for output.url</summary>
        public string OutputUrl { get; set; }

        /// <summary>A preview never changes state: counters show their next value and stay</summary>
        public bool Preview { get; set; }

        /// <summary>Extra values of a plugin or step, by full name (e.g. "jira.issue"); they come before the providers</summary>
        public IDictionary<string, object> Values { get; set; }

        /// <summary>The time used for payload.time when there's no capture (fixed for a whole render)</summary>
        internal DateTime? FixedNow { get; set; }

        /// <summary>A copy with another target folder or output file</summary>
        public VariableScope With(string targetFolder = null, string outputFile = null)
        {
            var copy = (VariableScope)MemberwiseClone();
            if (targetFolder != null) copy.TargetFolder = targetFolder;
            if (outputFile != null) copy.OutputFile = outputFile;
            return copy;
        }

        /// <summary>The scope of a running recipe</summary>
        public static VariableScope ForFlow(CaptureFlowContext flow) => new VariableScope
        {
            Flow = flow,
            Capture = flow?.Payload?.RawCapture?.CaptureDetails ?? flow?.Payload?.Surface?.CaptureDetails
        };

        /// <summary>The scope of a capture outside recipes</summary>
        public static VariableScope ForCapture(ICaptureDetails captureDetails) => new VariableScope { Capture = captureDetails };
    }
}
