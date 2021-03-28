/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Processors
{
    /// <summary>
    /// Description of TitleFixProcessor.
    /// </summary>
    public class TitleFixProcessor : AbstractProcessor
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(TitleFixProcessor));
        private static readonly CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();

        public TitleFixProcessor()
        {
            List<string> corruptKeys = new List<string>();
            foreach (string key in config.ActiveTitleFixes)
            {
                if (config.TitleFixMatcher.ContainsKey(key)) continue;

                LOG.WarnFormat("Key {0} not found, configuration is broken! Disabling this key!", key);
                corruptKeys.Add(key);
            }

            // Fix configuration if needed
            if (corruptKeys.Count <= 0) return;

            foreach (string corruptKey in corruptKeys)
            {
                // Removing any reference to the key
                config.ActiveTitleFixes.Remove(corruptKey);
                config.TitleFixMatcher.Remove(corruptKey);
                config.TitleFixReplacer.Remove(corruptKey);
            }

            config.IsDirty = true;
        }

        public override string Designation => "TitleFix";

        public override string Description => Designation;

        public override bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails)
        {
            bool changed = false;
            string title = captureDetails.Title;
            if (!string.IsNullOrEmpty(title))
            {
                title = title.Trim();
                foreach (string titleIdentifier in config.ActiveTitleFixes)
                {
                    string regexpString = config.TitleFixMatcher[titleIdentifier];
                    string replaceString = config.TitleFixReplacer[titleIdentifier] ?? string.Empty;

                    if (string.IsNullOrEmpty(regexpString)) continue;

                    var regex = new Regex(regexpString);
                    title = regex.Replace(title, replaceString);
                    changed = true;
                }
            }

            captureDetails.Title = title;
            return changed;
        }
    }
}