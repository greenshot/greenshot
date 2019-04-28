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
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addons.Core.Enums;

namespace Greenshot.Addon.LegacyEditor.ViewModels
{
    public sealed class EditorConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        public IEditorConfiguration EditorConfiguration { get; }

        public IEditorLanguage EditorLanguage { get; }

        public EditorConfigViewModel(
            IEditorConfiguration editorConfiguration,
            IEditorLanguage editorLanguage)
        {
            EditorConfiguration = editorConfiguration;
            EditorLanguage = editorLanguage;
        }

        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.InternalDestinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(EditorConfiguration);

            // automatically update the DisplayName
            var boxLanguageBinding = EditorLanguage.CreateDisplayNameBinding(this, nameof(IEditorLanguage.EditorTitle));

            // Make sure the greenshotLanguageBinding is disposed when this is no longer active
            _disposables.Add(boxLanguageBinding);

            base.Initialize(config);
        }

        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }
    }
}
