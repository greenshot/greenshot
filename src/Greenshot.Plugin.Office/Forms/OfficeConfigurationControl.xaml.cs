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

namespace Greenshot.Plugin.Office.Forms
{
    public class OfficeAppItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public ImageSource Icon { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class OfficeConfigurationControl : UserControl, INotifyPropertyChanged
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

        public OfficeConfigurationControl()
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
                Title = Greenshot.Base.Core.Language.GetString("office", "app_word"),
                Icon = new WordDestination().DisplayIcon?.ToBitmapSource()
            });

            OfficeApps.Add(new OfficeAppItem
            {
                Name = "Excel",
                Title = Greenshot.Base.Core.Language.GetString("office", "app_excel"),
                Icon = new ExcelDestination().DisplayIcon?.ToBitmapSource()
            });

            OfficeApps.Add(new OfficeAppItem
            {
                Name = "PowerPoint",
                Title = Greenshot.Base.Core.Language.GetString("office", "app_powerpoint"),
                Icon = new PowerpointDestination().DisplayIcon?.ToBitmapSource()
            });

            OfficeApps.Add(new OfficeAppItem
            {
                Name = "Outlook",
                Title = Greenshot.Base.Core.Language.GetString("office", "app_outlook"),
                Icon = new OutlookDestination().DisplayIcon?.ToBitmapSource()
            });

            OfficeApps.Add(new OfficeAppItem
            {
                Name = "OneNote",
                Title = Greenshot.Base.Core.Language.GetString("office", "app_onenote"),
                Icon = new OneNoteDestination().DisplayIcon?.ToBitmapSource()
            });

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
