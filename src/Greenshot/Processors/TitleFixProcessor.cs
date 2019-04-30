// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Text.RegularExpressions;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;

namespace Greenshot.Processors
{
	/// <summary>
	///     Description of TitleFixProcessor.
	/// </summary>
	public class TitleFixProcessor : AbstractProcessor
	{
        private readonly ICoreConfiguration _coreConfiguration;

        public TitleFixProcessor(ICoreConfiguration coreConfiguration)
		{
            _coreConfiguration = coreConfiguration;
        }

        /// <inheritdoc />
        public override string Designation => "TitleFix";

        /// <inheritdoc />
        public override string Description => Designation;

        /// <inheritdoc />
        public override bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails)
		{
			var changed = false;
			var title = captureDetails.Title;
			if (!string.IsNullOrEmpty(title))
			{
				title = title.Trim();
				foreach (var titleIdentifier in _coreConfiguration.ActiveTitleFixes)
				{
					var regexpString = _coreConfiguration.TitleFixMatcher[titleIdentifier];
					var replaceString = _coreConfiguration.TitleFixReplacer[titleIdentifier];
					if (replaceString == null)
					{
						replaceString = "";
					}
					if (!string.IsNullOrEmpty(regexpString))
					{
						var regex = new Regex(regexpString);
						title = regex.Replace(title, replaceString);
						changed = true;
					}
				}
			}
			captureDetails.Title = title;
			return changed;
		}
	}
}