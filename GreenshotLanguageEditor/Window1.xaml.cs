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
using System.IO;
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
		
		IDictionary<string, LanguageEntry> languageResources = new SortedDictionary<string, LanguageEntry>();
		public IList<LanguageFile> LanguageFiles {
			get;
			set;
		}
		// maybe refactor this encapsulating column related info 
		bool unsavedChangesInLanguage1 = false;
		bool unsavedChangesInLanguage2 = false;
		public LanguageFile LanguageFile1 {
			get;
			set;
		}
		public LanguageFile LanguageFile2 {
			get;
			set;
		}
		
		public ICollectionView View {
			get;
			set;
		}
		
		// TODO user should be able to create a new translation file using this app
		// TODO possibility to edit language file meta data, such as version, languagegroup, description, etc.

		public Window1() {
			
			// TODO remember last selected location
			
			/*var dialog = new System.Windows.Forms.FolderBrowserDialog();
			dialog.ShowNewFolderButton = false;
			System.Windows.Forms.DialogResult result = dialog.ShowDialog();
			string languagePath;
			
			
			if (result == System.Windows.Forms.DialogResult.OK) {
				languagePath = dialog.SelectedPath;
			} else {
				this.Close();
				return;
			}*/
			
			
			string languagePath = @"C:\Users\jens\Documents\Sharpdevelop Projects\Greenshot\trunk\Greenshot\Languages\";

			InitializeComponent();
			DataContext = this;
			this.Activate();
			View = CollectionViewSource.GetDefaultView(LoadResources(languagePath));
		}
		
		private IList<LanguageEntry> LoadResources(string languagePath) {
			LanguageFiles = new List<LanguageFile>();
			foreach (LanguageFile languageFile in GreenshotLanguage.GetLanguageFiles(languagePath, "language*.xml")) {
				LanguageFiles.Add(languageFile);
				
				if ("en-US".Equals(languageFile.IETF)) {
					// we should always start with en-US, so the grid is initialized with the probably most-complete language file as benchmark for translations
					LanguageFile1 = languageFile;
					PopulateColumn(languageFile, 1);
				} else if(LanguageFile2 == null) {
					LanguageFile2 = languageFile;
					PopulateColumn(languageFile, 2);
				}
			}
			if(LanguageFile1 == null) {
				MessageBox.Show("language-en-US.xml does not exist in the location selected. It is needed as reference for the translation.");
				this.Close();
			}
			return new List<LanguageEntry>(languageResources.Values);
		}
		
		private void PopulateColumn(LanguageFile languageFile, int columnIndex) {
			ClearColumn(columnIndex);
			IDictionary<string, string> resources = GreenshotLanguage.ReadLanguageFile(languageFile);
			foreach(string key in resources.Keys) {
				LanguageEntry entry = GetOrCreateLanguageEntry(key);
				if(columnIndex == 1) entry.Entry1 = resources[key];
				else if (columnIndex == 2) entry.Entry2 = resources[key];
				else throw new ArgumentOutOfRangeException("Argument columnIndex must be either 1 or 2");
			}
			if(columnIndex == 1) unsavedChangesInLanguage1 = false;
			if(columnIndex == 2) unsavedChangesInLanguage2 = false;
		}
		
		private void ClearColumn(int columnIndex) {
			// we do not throw out LanguageEntries that do not exist in selected language, 
			// so that en-US (loaded at startup) is always the benchmark, even when other languages are displayed
			foreach(LanguageEntry e in languageResources.Values) {
				if (columnIndex == 1) e.Entry1 = null;
				else if (columnIndex == 2) e.Entry2 = null;
				else throw new ArgumentOutOfRangeException("Argument columnIndex must be either 1 or 2");
			}
		}
		
		private LanguageEntry GetOrCreateLanguageEntry(string key) {
			LanguageEntry entry;
			if (languageResources.ContainsKey(key)) {
				entry = languageResources[key];
			} else {
				entry = new LanguageEntry();
				entry.Key = key;
				languageResources.Add(key, entry);
			}
			return entry;
		}
		
		private void saveButtonClicked(object sender, RoutedEventArgs e) {
			int targetColumn = GetTargetColumn((Control)sender);
			LanguageFile editedFile = (LanguageFile) (targetColumn == 1 ? language1ComboBox.SelectedItem : language2ComboBox.SelectedItem);
			
			var dialog = new System.Windows.Forms.SaveFileDialog();
			dialog.AutoUpgradeEnabled = true;
			dialog.DefaultExt = ".xml";
			dialog.InitialDirectory = Path.GetDirectoryName(editedFile.FilePath);
			dialog.FileName = editedFile.FileName;
			dialog.CheckFileExists = true;
			System.Windows.Forms.DialogResult result = dialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK) {
				CreateXML(dialog.FileName, targetColumn);
			} 
		}
		
		private void languageComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e) {
			int targetColumn = GetTargetColumn((Control)sender);
			LanguageFile file = (LanguageFile)((ComboBox)sender).SelectedItem;
			if((targetColumn == 1 && file.Equals(LanguageFile1)) || (targetColumn == 2 && file.Equals(LanguageFile2))) {
				// nothing changed
				return;
			}
			if((targetColumn == 1 && unsavedChangesInLanguage1) || (targetColumn == 2 && unsavedChangesInLanguage2)) {
				MessageBoxResult res = MessageBox.Show("Do you really want to switch language? Unsaved changes will be lost.", "Confirm language switch", MessageBoxButton.OKCancel, MessageBoxImage.Warning,MessageBoxResult.Cancel,MessageBoxOptions.None);
				if(res != MessageBoxResult.OK) {
					// cancelled by user
					((ComboBox)sender).SelectedItem = (targetColumn == 1) ? LanguageFile1 : LanguageFile2;
					return;
				}
			}
			if(targetColumn == 1) LanguageFile1 = file;
			else if(targetColumn == 2) LanguageFile2 = file;
			PopulateColumn(file, targetColumn);
		}
		
		private void cancelButtonClicked(object sender, RoutedEventArgs e) {
			int targetColumn = GetTargetColumn((Control)sender);
			if((targetColumn == 1 && unsavedChangesInLanguage1) || (targetColumn == 2 && unsavedChangesInLanguage2)) {
				MessageBoxResult res = MessageBox.Show("Do you really want to reset this column? Unsaved changes will be lost.", "Confirm language reset", MessageBoxButton.OKCancel, MessageBoxImage.Warning,MessageBoxResult.Cancel,MessageBoxOptions.None);
				if(res == MessageBoxResult.OK) {
					LanguageFile file = (LanguageFile)(targetColumn == 1 ? language1ComboBox.SelectedItem : language2ComboBox.SelectedItem);
					PopulateColumn(file, targetColumn);
				}
			}
		}
		
		private int GetTargetColumn(Control control) {
			object tag = control.Tag;
			if(tag == null && !tag.Equals("1") && !tag.Equals("2")) {
				throw new ApplicationException("Please use the control's Tag property to indicate the column to interact with (1 or 2).");
			} else {
				return tag.Equals("1") ? 1 : 2;
			}
		}
		
		private void cellEdited(object sender, DataGridCellEditEndingEventArgs e) {
			if(e.Column.DisplayIndex == 1) unsavedChangesInLanguage1 = true;
			else if(e.Column.DisplayIndex == 2) unsavedChangesInLanguage2 = true;
		}
		
		public void CreateXML(string savePath, int targetColumn) {

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
                    if(targetColumn == 1) xmlWriter.WriteString(entry.Entry1);
                    else if(targetColumn == 2 ) xmlWriter.WriteString(entry.Entry2);
                    else throw new ArgumentOutOfRangeException("Argument columnIndex must be either 1 or 2");
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
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
				if (String.IsNullOrEmpty(Entry1)) {
					return Brushes.Red;
				}
				if (String.IsNullOrEmpty(Entry2)) {
					return Brushes.Red;
				}
				return Brushes.White;
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