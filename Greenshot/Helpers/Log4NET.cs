/*
 * User: Robin
 * Date: 05.04.2010
 */
using System;
using System.IO;
using log4net.Util;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of Log4NET.
	/// </summary>
	public class SpecialFolderPatternConverter : PatternConverter {
		override protected void Convert(TextWriter writer, object state) {
			Environment.SpecialFolder specialFolder = (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), base.Option, true);
			writer.Write(Environment.GetFolderPath(specialFolder));
		}
	}
}
