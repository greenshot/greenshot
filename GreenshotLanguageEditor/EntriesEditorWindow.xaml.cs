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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace GreenshotLanguageEditor {
	/// <summary>
	/// Interaction logic for EntriesEditorWindow.xaml
	/// </summary>
	public partial class EntriesEditorWindow : Window, INotifyPropertyChanged {
		
		private string languagePath;
		ObservableDictionary<string, LanguageEntry> languageResources = new ObservableDictionary<string, LanguageEntry>();
		
		IList<LanguageFile> languageFiles;
		public IList<LanguageFile> LanguageFiles {
			get {return languageFiles;}
			set {languageFiles = value; NotifyPropertyChanged("languageFiles");}
		}
		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String info) {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
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
		
		private ICollectionView view;
		public ICollectionView View {
			get {return view;}
			set {view=value; NotifyPropertyChanged("View");}
		}
		
		public EntriesEditorWindow() {
			
			
			var dialog = new System.Windows.Forms.FolderBrowserDialog();
			dialog.Description = "Select the directory containing the translation files for Greenshot. " +
				"Please get the latest files first: " +
				"https://greenshot.svn.sourceforge.net/svnroot/greenshot/trunk/Greenshot/Languages/";
			dialog.ShowNewFolderButton = false;
			System.Windows.Forms.DialogResult result = dialog.ShowDialog();
			
			
			if (result == System.Windows.Forms.DialogResult.OK) {
				languagePath = dialog.SelectedPath;
			} else {
				this.Close();
				return;
			}

			InitializeComponent();
			DataContext = this;
			this.Activate();

			View = CollectionViewSource.GetDefaultView(LoadResources(languagePath));
			languageResources.CollectionChanged += delegate {
				
				View = CollectionViewSource.GetDefaultView(new List<LanguageEntry>(languageResources.Values));
				View.Refresh();
			};
		}
		
		private IList<LanguageEntry> LoadResources(string languagePath) {
			LanguageFiles = new BindingList<LanguageFile>();
			foreach (LanguageFile languageFile in GreenshotLanguage.GetLanguageFiles(languagePath, "language*.xml")) {
				LanguageFiles.Add(languageFile);
				
				// default: first non-english file is for right column, english file for left column
				if(LanguageFile2 == null && !"en-US".Equals(languageFile.IETF)) {
					LanguageFile2 = languageFile;
				}else if (LanguageFile1 == null || "en-US".Equals(languageFile.IETF)) {
					LanguageFile1 = languageFile;
				}
			}
			if(LanguageFile1 != null) PopulateColumn(LanguageFile1, 1);
			if(LanguageFile2 != null) PopulateColumn(LanguageFile2, 2);

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
			IList<string> resKeys = new List<string>(languageResources.Keys);
			//foreach(string key in  resKeys) {
			foreach(LanguageEntry e in languageResources.Values) {
				if (columnIndex == 1)  e.Entry1 = null;
				else if (columnIndex == 2) e.Entry2 = null;
				else throw new ArgumentOutOfRangeException("Argument columnIndex must be either 1 or 2");
			}
			// remove entries with two null values
			foreach(string key in  resKeys) {
				LanguageEntry e = languageResources[key];
				if(string.IsNullOrWhiteSpace(e.Entry1) && string.IsNullOrWhiteSpace(e.Entry2)) {
					languageResources.Remove(e.Key);
				}
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

			CreateXML(editedFile.FilePath, targetColumn);
			
			if(editedFile.FileName.Contains("website")) {
				CreateWebsitePart(editedFile.FilePath, targetColumn);
			} else if(editedFile.FileName.Contains("installer")) {
				CreateInstallerPart(editedFile.FilePath, targetColumn);
			}
			
			if(targetColumn == 1) unsavedChangesInLanguage1 = false;
			else if(targetColumn == 2) unsavedChangesInLanguage2 = false;
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
			// TODO Language resources does not implement notifycollectionwhatever interface. does not work when keys are removed
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
		
		private void newButtonClicked(object sender, RoutedEventArgs e) {
			int targetColumn = GetTargetColumn((Control)sender);
			if((targetColumn == 1 && unsavedChangesInLanguage1) || (targetColumn == 2 && unsavedChangesInLanguage2)) {
				MessageBoxResult res = MessageBox.Show("Do you really want to discard this column? Unsaved changes will be lost.", "Confirm new language file creation", MessageBoxButton.OKCancel, MessageBoxImage.Warning,MessageBoxResult.Cancel,MessageBoxOptions.None);
				if(res != MessageBoxResult.OK) {
					return;
				}
			}
			LanguageFile newLang = new LanguageFile();
			newLang.FileDir = languagePath;
			new MetaEditorWindow(newLang).ShowDialog();
			if(newLang.FileName != null && newLang.FileName.Length > 0) {
				ClearColumn(targetColumn);
				CreateXML(newLang.FilePath,targetColumn);
				LanguageFiles.Add(newLang);
				
				LanguageFiles = LanguageFiles.OrderBy(f => f.FileName).ToList();
				if(targetColumn == 1) {
					LanguageFile1 = newLang;
					language1ComboBox.SelectedItem = newLang;
				}
				else {
					LanguageFile2 = newLang;
					language2ComboBox.SelectedItem = newLang;
				}
				PopulateColumn(newLang, targetColumn);
			}
		}
		
		private void metaButtonClicked(object sender, RoutedEventArgs e)  {
			int targetColumn = GetTargetColumn((Control)sender);
			new MetaEditorWindow(targetColumn == 1 ? LanguageFile1 : LanguageFile2).ShowDialog();
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
			LanguageFile langfile = targetColumn == 1 ? LanguageFile1 : LanguageFile2;
			ICollectionView view = (ICollectionView)LanguageGrid.ItemsSource;
			IList<LanguageEntry> entries = (IList<LanguageEntry>)view.SourceCollection;
			List<LanguageEntry> sortList = new List<LanguageEntry>(entries);
			sortList.Sort(compareEntryKeys);
			
			using (XmlTextWriter xmlWriter = new XmlTextWriter(savePath, Encoding.UTF8)) {
				xmlWriter.Formatting = Formatting.Indented;
				xmlWriter.Indentation = 1;
				xmlWriter.IndentChar = '\t';
				xmlWriter.WriteStartDocument();
				xmlWriter.WriteStartElement("language");
				xmlWriter.WriteAttributeString("description", langfile.Description);
				xmlWriter.WriteAttributeString("ietf", langfile.IETF);
				xmlWriter.WriteAttributeString("version", langfile.Version);
				xmlWriter.WriteAttributeString("languagegroup", langfile.Languagegroup);
				xmlWriter.WriteStartElement("resources");
				foreach(LanguageEntry entry in sortList) {
					string entryValue = (targetColumn == 1) ? entry.Entry1 : entry.Entry2;
					if(!String.IsNullOrWhiteSpace(entryValue) && !String.IsNullOrWhiteSpace(entry.Key)) {
						xmlWriter.WriteStartElement("resource");
						xmlWriter.WriteAttributeString("name", entry.Key);
						xmlWriter.WriteString(entryValue);
						xmlWriter.WriteEndElement();
					}
				}
				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndElement();
				xmlWriter.WriteEndDocument();
			}
		}
		
		public void CreateWebsitePart(string savePath, int targetColumn) {
			PopulateTemplate("template-homepage.html.part", savePath, targetColumn);
		}
		
		public void CreateInstallerPart(string savePath, int targetColumn) {
			PopulateTemplate("template-installer.iss.part", savePath, targetColumn);
		}


		void PopulateTemplate(string fileName, string savePath, int targetColumn) {
			LanguageFile langfile = targetColumn == 1 ? LanguageFile1 : LanguageFile2;
			ICollectionView view = (ICollectionView)LanguageGrid.ItemsSource;
			IList<LanguageEntry> entries = (IList<LanguageEntry>)view.SourceCollection;
			string tmp;
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GreenshotLanguageEditor."+fileName)) {
				using (StreamReader reader = new StreamReader(stream)) {
					tmp = reader.ReadToEnd();
					foreach (LanguageEntry e in entries) {
						string entryString = targetColumn == 1 ? e.Entry1 : e.Entry2;
						tmp = tmp.Replace("${" + e.Key + "}", entryString);
					}
					tmp = tmp.Replace("${lang}",extractIetfLanguageCode(savePath));
				}
			}
			FileInfo fi = new FileInfo(savePath.Replace(".xml", fileName.Substring(fileName.IndexOf("."))));
			FileStream fs = fi.Open(FileMode.OpenOrCreate);
			byte[] barr = Encoding.GetEncoding("UTF-8").GetBytes(tmp);
			fs.Write(barr, 0, barr.Length);
			fs.Close();
		}

		private  int compareEntryKeys(LanguageEntry a, LanguageEntry b) {
			return a.Key.CompareTo(b.Key);
		}
		
		// assuming that filename always ends with -LANG-REGION.EXT, extracting LANG
		private string extractIetfLanguageCode(string filename) {
			string[] s = filename.Split('-');
			if(s.Length > 2) return s[s.Length - 2];
			else throw new ArgumentException("Filename does not match expected pattern: "+filename);
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