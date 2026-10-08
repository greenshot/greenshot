using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Plugin.Office.Destinations;
using Microsoft.Office.Interop.PowerPoint;
using System.Threading.Tasks;
using Greenshot.Base.Threading;
using Greenshot.Base.Languages;

namespace Greenshot.Plugin.Office.Views
{
    public class OfficeAppItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Title { get; set; }
        private ImageSource _icon;

        public ImageSource Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Load the icon of the destination, the continuation sets it on the UI thread
        /// </summary>
        public async Task LoadIconAsync(IDestination destination)
        {
            Icon = await DestinationIcons.GetImageSourceAsync(destination.Descriptor?.IconKey);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class OfficeConfigurationView : UserControl, INotifyPropertyChanged
    {
        private OfficeAppItem _selectedApp;

        public IOfficeConfiguration Config { get; }

        public ObservableCollection<OfficeAppItem> OfficeApps { get; } = new();

        public OfficeAppItem SelectedApp
        {
            get => _selectedApp;
            set
            {
                if (_selectedApp != value)
                {
                    _selectedApp = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsWordSelected));
                    OnPropertyChanged(nameof(IsPowerPointSelected));
                    OnPropertyChanged(nameof(IsOutlookSelected));
                    OnPropertyChanged(nameof(IsExcelSelected));
                    OnPropertyChanged(nameof(IsOneNoteSelected));
                }
            }
        }

        public bool IsWordSelected => SelectedApp?.Name == "Word";
        public bool IsPowerPointSelected => SelectedApp?.Name == "PowerPoint";
        public bool IsOutlookSelected => SelectedApp?.Name == "Outlook";
        public bool IsExcelSelected => SelectedApp?.Name == "Excel";
        public bool IsOneNoteSelected => SelectedApp?.Name == "OneNote";

        public Array SlideLayouts => Enum.GetValues(typeof(PpSlideLayout));

        public OfficeConfigurationView()
        {
            Config = IniConfigRegistry.GetSection<IOfficeConfiguration>();
            DataContext = this;
            InitializeComponent();
            InitializeApps();
        }

        private void InitializeApps()
        {
            OfficeApps.Add(new OfficeAppItem
            {
                Name = "Word",
                Title = Texts.Get<IOfficeLanguage>().AppWord
            });
            AsyncCommand.Run(() => OfficeApps[OfficeApps.Count - 1].LoadIconAsync(new WordDestination()), "Load the icon of WordDestination");

            OfficeApps.Add(new OfficeAppItem
            {
                Name = "Excel",
                Title = Texts.Get<IOfficeLanguage>().AppExcel
            });
            AsyncCommand.Run(() => OfficeApps[OfficeApps.Count - 1].LoadIconAsync(new ExcelDestination()), "Load the icon of ExcelDestination");

            OfficeApps.Add(new OfficeAppItem
            {
                Name = "PowerPoint",
                Title = Texts.Get<IOfficeLanguage>().AppPowerpoint
            });
            AsyncCommand.Run(() => OfficeApps[OfficeApps.Count - 1].LoadIconAsync(new PowerpointDestination()), "Load the icon of PowerpointDestination");

            OfficeApps.Add(new OfficeAppItem
            {
                Name = "Outlook",
                Title = Texts.Get<IOfficeLanguage>().AppOutlook
            });
            AsyncCommand.Run(() => OfficeApps[OfficeApps.Count - 1].LoadIconAsync(new OutlookDestination()), "Load the icon of OutlookDestination");

            OfficeApps.Add(new OfficeAppItem
            {
                Name = "OneNote",
                Title = Texts.Get<IOfficeLanguage>().AppOnenote
            });
            AsyncCommand.Run(() => OfficeApps[OfficeApps.Count - 1].LoadIconAsync(new OneNoteDestination()), "Load the icon of OneNoteDestination");

            if (OfficeApps.Count > 0)
            {
                SelectedApp = OfficeApps[0];
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
