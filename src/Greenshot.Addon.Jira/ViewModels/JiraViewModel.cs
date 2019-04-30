// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.Jira.Entities;
using Greenshot.Addon.Jira.Configuration;

namespace Greenshot.Addon.Jira.ViewModels
{
    /// <summary>
    /// The view model for a Jira
    /// </summary>
    public sealed class JiraViewModel : Screen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Provide IJiraConfiguration to the view
        /// </summary>
        public IJiraConfiguration JiraConfiguration { get; }


        /// <summary>
        /// Provide IJiraLanguage to the view
        /// </summary>
        public IJiraLanguage JiraLanguage { get; }

        /// <summary>
        /// Provide JiraConnector to the view
        /// </summary>
        public JiraConnector JiraConnector { get; }


        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="jiraConfiguration">IJiraConfiguration</param>
        /// <param name="jiraLanguage">IJiraLanguage</param>
        /// <param name="jiraConnector">JiraConnector</param>
        public JiraViewModel(
            IJiraConfiguration jiraConfiguration,
            IJiraLanguage jiraLanguage,
            JiraConnector jiraConnector
            )
        {
            JiraConfiguration = jiraConfiguration;
            JiraLanguage = jiraLanguage;
            JiraConnector = jiraConnector;
        }

        public IList<Filter> Filters { get; private set; }

        public Filter Filter { get; set; }

        /// <summary>
        /// The issue selected
        /// </summary>
        public Issue JiraIssue { get; private set; }

        /// <summary>
        /// The comment for the upload
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// The filename for the attachment
        /// </summary>
        public string Filename { get; set; }

        /// <inheritdoc />
        protected override void OnActivate()
        {
            // Prepare disposables
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            // automatically update the DisplayName
            var greenshotLanguageBinding = JiraLanguage.CreateDisplayNameBinding(this, nameof(IJiraLanguage.LabelJira));

            // Make sure the greenshotLanguageBinding is disposed when this is no longer active
            _disposables.Add(greenshotLanguageBinding);
            OnUIThread(async () =>
            {
                try
                {
                    if (!JiraConnector.IsLoggedIn)
                    {
                        await JiraConnector.LoginAsync();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(JiraLanguage.LoginError, e.Message);
                }

                if (!JiraConnector.IsLoggedIn)
                {
                    return;
                }
                Filters = await JiraConnector.GetFavoriteFiltersAsync();
                NotifyOfPropertyChange(nameof(Filters));

                if (!JiraConnector.RecentJiras.Any())
                {
                    return;
                }

                JiraIssue = JiraConnector.RecentJiras.First().JiraIssue;
                NotifyOfPropertyChange(nameof(JiraIssue));
                CanUpload = true;
                NotifyOfPropertyChange(nameof(CanUpload));

            });
            base.OnActivate();
        }

        /// <summary>
        /// Can this be uploaded?
        /// </summary>
        public bool CanUpload { get; private set; }

        /// <inheritdoc />
        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }
    }
}
