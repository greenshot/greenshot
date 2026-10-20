/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Wpf.ViewModels;
using Greenshot.Plugin.Jira.Api;

namespace Greenshot.Plugin.Jira.Views;

public partial class JiraConfigurationView : UserControl
{
    public JiraConfigurationView(IJiraConfiguration config)
    {
        DataContext = config;
        InitializeComponent();
        var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
        if (FindName("UploadFormatCombo") is ComboBox uploadFormatCombo)
        {
            uploadFormatCombo.ItemsSource = registry?.GetSaveableFileFormatOptions()
                .WithCurrentFormat(config.UploadFormat);
        }

        var connector = SimpleServiceProvider.Current.GetInstance<JiraConnector>(true);
        if (connector == null)
        {
            AccountView.Visibility = Visibility.Collapsed;
            return;
        }

        var account = new ServerAccountViewModel(connector.SignIn);
        AccountView.DataContext = account;
        // Another url is another sign-in
        PropertyChangedEventHandler onUrlChanged = (_, args) =>
        {
            if (args.PropertyName == nameof(IJiraConfiguration.Url))
            {
                account.Refresh();
            }
        };
        if (config is INotifyPropertyChanged notifying)
        {
            Loaded += (_, _) => notifying.PropertyChanged += onUrlChanged;
            Unloaded += (_, _) => notifying.PropertyChanged -= onUrlChanged;
        }
    }
}
