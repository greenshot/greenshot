/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Xml;

namespace GreenshotLanguageEditor {
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window {
		public ICollectionView View {
			get;
			set;
		}

		public Window1() {
			var dialog = new System.Windows.Forms.FolderBrowserDialog();
			dialog.ShowNewFolderButton = false;
			System.Windows.Forms.DialogResult result = dialog.ShowDialog();
			string languagePath;
			if (result == System.Windows.Forms.DialogResult.OK) {
				languagePath = dialog.SelectedPath;
			} else {
				this.Close();
				return;
			}

			InitializeComponent();
			View = CollectionViewSource.GetDefaultView(LoadResources(languagePath));
			DataContext = this;
			this.Activate();
		}
		
		private IList<LanguageEntry> LoadResources(string languagePath) {
			IDictionary<string, LanguageEntry> languageResources = new SortedDictionary<string, LanguageEntry>();

			foreach (LanguageFile languageFile in GreenshotLanguage.GetLanguageFiles(languagePath, "language*.xml")) {
				if ("en-US".Equals(languageFile.IETF)) {
					IDictionary<string, string> enResources = GreenshotLanguage.ReadLanguageFile(languageFile);
					foreach(string key in enResources.Keys) {
						LanguageEntry entry;
						if (languageResources.ContainsKey(key)) {
							entry = languageResources[key];
						} else {
							entry = new LanguageEntry();
							entry.Key = key;
							languageResources.Add(key, entry);
						}
						entry.Entry1 = enResources[key];
					}
				}
				if ("de-DE".Equals(languageFile.IETF)) {
					IDictionary<string, string> deResources = GreenshotLanguage.ReadLanguageFile(languageFile);
					foreach(string key in deResources.Keys) {
						LanguageEntry entry;
						if (languageResources.ContainsKey(key)) {
							entry = languageResources[key];
						} else {
							entry = new LanguageEntry();
							entry.Key = key;
							languageResources.Add(key, entry);
						}
						entry.Entry2 = deResources[key];
					}
				}
			}
			return new List<LanguageEntry>(languageResources.Values);
		}
		
		public void CreateXML(string savePath) {

			ICollectionView view = (ICollectionView)LanguageGrid.ItemsSource;
			IList<LanguageEntry> entries = (IList<LanguageEntry>)view.SourceCollection;

			using (XmlTextWriter xmlWriter = new XmlTextWriter(savePath, Encoding.UTF8)) {
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.Indentation = 1;
                xmlWriter.IndentChar = '\t';
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("language");
                xmlWriter.WriteAttributeString("description", "testdescription");
                xmlWriter.WriteAttributeString("ietf", "testietf");
                xmlWriter.WriteAttributeString("version", "testversion");
                xmlWriter.WriteAttributeString("languagegroup", "testlanguagegroup");
                xmlWriter.WriteStartElement("resources");
                foreach(LanguageEntry entry in entries) {
                    xmlWriter.WriteStartElement("resource");
                    xmlWriter.WriteAttributeString("name", entry.Key);
                    xmlWriter.WriteString(entry.Entry1);
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
			}
		}
		
		private void Button_Click(object sender, RoutedEventArgs e) {
			var dialog = new System.Windows.Forms.SaveFileDialog();
			dialog.AutoUpgradeEnabled = true;
			dialog.DefaultExt = ".xml";
			System.Windows.Forms.DialogResult result = dialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK) {
				CreateXML(dialog.FileName);
			} else {
				this.Close();
				return;
			}
			
		}
	}
	
	public class LanguageEntry : IEditableObject, INotifyPropertyChanged {
		private string key;
		private string entry1;
		private string entry2;
		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String info) {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		public Brush Background {
			get {
				if (Entry1 == null) {
					return Brushes.Red;
				}
				if (Entry2 == null) {
					return Brushes.Red;
				}
				return Brushes.Green;
			}
		}
		public string Key {
			get {
				return key;
			}
			set {
				key = value;
				NotifyPropertyChanged("Key");
			}
		}

		public string Entry1 {
			get {
				return entry1;
			}
			set {
				entry1 = value;
				NotifyPropertyChanged("Entry1");
				NotifyPropertyChanged("Background");

			}
		}
		
		public string Entry2 {
			get {
				return entry2;
			}
			set {
				entry2 = value;
				NotifyPropertyChanged("Entry2");
				NotifyPropertyChanged("Background");
			}
		}
		
		public void BeginEdit() {
			
		}
		
		public void EndEdit() {
			
		}
		
		public void CancelEdit() {
			
		}
	}

}