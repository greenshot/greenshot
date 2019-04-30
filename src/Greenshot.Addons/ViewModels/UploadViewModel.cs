// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Reactive.Disposables;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addons.Components;

namespace Greenshot.Addons.ViewModels
{
    /// <summary>
    /// A view model for showing uploads
    /// </summary>
    public sealed class UploadViewModel : Conductor<IDestination>.Collection.AllActive
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Provide the IGreenshotLanguage to the view
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="greenshotLanguage"></param>
        public UploadViewModel(IGreenshotLanguage greenshotLanguage)
        {
            GreenshotLanguage = greenshotLanguage;
        }

        /// <inheritdoc />
        protected override void OnActivate()
        {
            // Prepare disposables
            _disposables?.Dispose();
            _disposables = new CompositeDisposable
            {
                GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.ApplicationTitle))
            };
            base.OnActivate();
        }

        /// <inheritdoc />
        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }
    }
}
