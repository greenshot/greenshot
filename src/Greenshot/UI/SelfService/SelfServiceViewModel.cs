/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;

namespace Greenshot.UI.SelfService
{
    public class SelfServiceViewModel : INotifyPropertyChanged
    {
        private SelfServiceSectionViewModel _selectedSection;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SelfServiceSectionViewModel> Sections { get; } = new ObservableCollection<SelfServiceSectionViewModel>();

        public SystemInfoSectionViewModel SystemInfoSection { get; }
        public FileInfoSectionViewModel FileInfoSection { get; }
        public ClipboardSectionViewModel ClipboardSection { get; }
        public HotkeySectionViewModel HotkeySection { get; }
        public ChecksumSectionViewModel ChecksumSection { get; }
#if DEBUG
        public IntegrationDebugSectionViewModel IntegrationDebugSection { get; }
#endif

        public string WindowTitle
        {
            get
            {
                return Language.GetString("selfservice_window_title");
            }
        }
        public string AppVersionTitle => $"Greenshot {EnvironmentInfo.GetGreenshotVersion()} ({OsInfo.Bits}-bit)";

        public SelfServiceSectionViewModel SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (_selectedSection != value)
                {
                    _selectedSection?.OnNavigatedFrom();
                    if (_selectedSection != null) _selectedSection.IsSelected = false;

                    _selectedSection = value;

                    if (_selectedSection != null) _selectedSection.IsSelected = true;
                    _selectedSection?.OnNavigatedTo();

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentSectionId));
                }
            }
        }

        public string CurrentSectionId => SelectedSection?.Id;

        // Theme brushes forwarding for seamless WPF binding
        public SolidColorBrush WindowBackgroundBrush => WpfThemeHelper.WindowBackground;
        public SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public SolidColorBrush AccentBrush => WpfThemeHelper.Accent;
        public SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;
        public string ThemeToggleIcon => WpfThemeHelper.IsDarkMode ? "☀️" : "🌙";
        public string ThemeToggleToolTip => WpfThemeHelper.IsDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode";

        public SelfServiceViewModel(string initialSectionId = null)
        {
            SystemInfoSection = new SystemInfoSectionViewModel();
            FileInfoSection = new FileInfoSectionViewModel();
            ClipboardSection = new ClipboardSectionViewModel();
            HotkeySection = new HotkeySectionViewModel();
            ChecksumSection = new ChecksumSectionViewModel();

            Sections.Add(SystemInfoSection);
            Sections.Add(FileInfoSection);
            Sections.Add(ClipboardSection);
            Sections.Add(HotkeySection);
            Sections.Add(ChecksumSection);
#if DEBUG
            IntegrationDebugSection = new IntegrationDebugSectionViewModel();
            Sections.Add(IntegrationDebugSection);
#endif

            SelectSection(initialSectionId ?? "system");

            WpfThemeHelper.ThemeChanged += OnThemeChanged;
            Language.LanguageChanged += OnLanguageChanged;
        }

        public void SelectSection(string sectionId)
        {
            if (string.IsNullOrEmpty(sectionId))
            {
                SelectedSection = Sections.FirstOrDefault();
                return;
            }

            var target = Sections.FirstOrDefault(s => string.Equals(s.Id, sectionId, StringComparison.OrdinalIgnoreCase));
            SelectedSection = target ?? Sections.FirstOrDefault();
        }

        public void RefreshAll()
        {
            foreach (var section in Sections)
            {
                section.Refresh();
            }
        }

        public void ToggleTheme()
        {
            WpfThemeHelper.ToggleTheme();
        }

        private void OnThemeChanged()
        {
            OnPropertyChanged(nameof(WindowBackgroundBrush));
            OnPropertyChanged(nameof(CardBackgroundBrush));
            OnPropertyChanged(nameof(CardBorderBrush));
            OnPropertyChanged(nameof(TextPrimaryBrush));
            OnPropertyChanged(nameof(TextSecondaryBrush));
            OnPropertyChanged(nameof(AccentBrush));
            OnPropertyChanged(nameof(BadgeBackgroundBrush));
            OnPropertyChanged(nameof(ThemeToggleIcon));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitle));
            foreach (var section in Sections)
            {
                section.OnLanguageChanged();
            }
        }

        public void Cleanup()
        {
            WpfThemeHelper.ThemeChanged -= OnThemeChanged;
            Language.LanguageChanged -= OnLanguageChanged;
            ClipboardSection?.StopMonitoring();
            ClipboardSection?.Dispose();
            ChecksumSection?.Cleanup();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
