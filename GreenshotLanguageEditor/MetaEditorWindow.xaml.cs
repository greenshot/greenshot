/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace GreenshotLanguageEditor
{
	/// <summary>
	/// Interaction logic for LanguageMetaEditor.xaml
	/// </summary>
	public partial class MetaEditorWindow : Window {
		
		private static Regex FILENAME_PATTERN = new Regex(@"[a-z]+\-[a-z]{2}\-[A-Z]{2}.xml");
		private LanguageFile langFile = new LanguageFile();
		
		
		public LanguageFile LangFile {
			get {return langFile;}
			set {langFile = value;}
		}
		public MetaEditorWindow(LanguageFile langFile) {
			InitializeComponent();
			this.LangFile = langFile;
			this.Closing += new System.ComponentModel.CancelEventHandler(OnClosing);            
		}
		
		void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			if(langFile.FileName != null && langFile.FileName.Length != 0 && !FILENAME_PATTERN.IsMatch(langFile.FileName)) {
				MessageBox.Show("The filename is not valid, please use a file name like language-en-US.xml","Filename not valid", MessageBoxButton.OK, MessageBoxImage.Stop);
				e.Cancel = true;
			}
		}
		
		void button1_Click(object sender, RoutedEventArgs e) {
			this.Close();
		}
	}
}