//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.ComponentModel;
using Dapplo.InterfaceImpl.Extensions;
using Greenshot.Core.Enumerations;

#endregion

namespace Greenshot.Core.Configuration
{
	/// <summary>
	///     This interface represents all the printer settings
	/// </summary>
	public interface IPrinterConfiguration
	{
		[Description("Allow growing the picture for fitting on paper?")]
		[DefaultValue(false)]
		[Tag(ConfigTags.LanguageKey, "printoptions_allowenlarge")]
		bool OutputPrintAllowEnlarge { get; set; }

		[Description("Allow rotating the picture for fitting on paper?")]
		[DefaultValue(false)]
		[Tag(ConfigTags.LanguageKey, "printoptions_allowrotate")]
		bool OutputPrintAllowRotate { get; set; }

		[Description("Allow shrinking the picture for fitting on paper?")]
		[DefaultValue(true)]
		[Tag(ConfigTags.LanguageKey, "printoptions_allowshrink")]
		bool OutputPrintAllowShrink { get; set; }

		[Description("Center image when printing?")]
		[DefaultValue(true)]
		[Tag(ConfigTags.LanguageKey, "printoptions_allowcenter")]
		bool OutputPrintCenter { get; set; }

		[Description("Print footer on print?")]
		[DefaultValue(true)]
		[Tag(ConfigTags.LanguageKey, "printoptions_timestamp")]
		bool OutputPrintFooter { get; set; }

		[Description("Footer pattern")]
		[DefaultValue("${capturetime:d\"D\"} ${capturetime:d\"T\"} - ${title}")]
		string OutputPrintFooterPattern { get; set; }

		[Description("Force grayscale printing")]
		[DefaultValue(false)]
		[Tag(ConfigTags.LanguageKey, "printoptions_printgrayscale")]
		bool OutputPrintGrayscale { get; set; }

		[Description("Print image inverted (use e.g. for console captures)")]
		[DefaultValue(false)]
		[Tag(ConfigTags.LanguageKey, "printoptions_inverted")]
		bool OutputPrintInverted { get; set; }

		[Description("Force monorchrome printing")]
		[DefaultValue(false)]
		[Tag(ConfigTags.LanguageKey, "printoptions_printmonochrome")]
		bool OutputPrintMonochrome { get; set; }

		[Description("Threshold for monochrome filter (0 - 255), lower value means less black")]
		[DefaultValue(127)]
		byte OutputPrintMonochromeThreshold { get; set; }

		[Description("Ask for print options when printing?")]
		[DefaultValue(true)]
		[Tag(ConfigTags.LanguageKey, "settings_alwaysshowprintoptionsdialog")]
		bool OutputPrintPromptOptions { get; set; }
	}
}