using System;
using System.Collections.Generic;
using System.Text;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using System.ComponentModel.Design;
using System.Reflection;
using System.IO;
using System.Windows.Forms;

namespace GreenshotImgurPlugin.Forms {
	public class ImgurForm : GreenshotForm {
		protected override string LanguagePattern {
			get {
				return Language.LANGUAGE_FILENAME_PATTERN;
			}
		}
	}
}
