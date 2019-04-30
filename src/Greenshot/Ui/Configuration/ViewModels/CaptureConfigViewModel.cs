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

using System.Collections.Generic;
using System.Reactive.Disposables;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;
using Greenshot.Configuration;
using Greenshot.Core.Enums;

namespace Greenshot.Ui.Configuration.ViewModels
{
    /// <summary>
    /// Configuration for the capture
    /// </summary>
    public sealed class CaptureConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Provides the ICoreConfiguration to the view
        /// </summary>
        public ICoreConfiguration CoreConfiguration { get; }

        /// <summary>
        /// Provides the IConfigTranslations to the view
        /// </summary>
        public IConfigTranslations ConfigTranslations { get; }

        /// <summary>
        /// Provides the IGreenshotLanguage to the view
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="configTranslations">IConfigTranslations</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        public CaptureConfigViewModel(ICoreConfiguration coreConfiguration,
            IConfigTranslations configTranslations,
            IGreenshotLanguage greenshotLanguage)
        {
            CoreConfiguration = coreConfiguration;
            ConfigTranslations = configTranslations;
            GreenshotLanguage = greenshotLanguage;
        }

        /// <inheritdoc />
        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.Capture);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(CoreConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.SettingsCapture))
            };
            
            base.Initialize(config);
        }

        /// <inheritdoc />
        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }

        /// <summary>
        /// Defines the capture mode which is going to be used
        /// </summary>
        public WindowCaptureModes SelectedWindowCaptureMode
        {
            get => CoreConfiguration.WindowCaptureMode;
            set
            {
                CoreConfiguration.WindowCaptureMode = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Available window capture modes
        /// </summary>
        public IDictionary<WindowCaptureModes, string> WindowCaptureModes => GreenshotLanguage.TranslationValuesForEnum<WindowCaptureModes>();

        /// <summary>
        /// Defines the screen capture mode which is going to be used
        /// </summary>
        public ScreenCaptureMode SelectedScreenCaptureMode
        {
            get => CoreConfiguration.ScreenCaptureMode;
            set
            {
                CoreConfiguration.ScreenCaptureMode = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Available screen capture modes
        /// </summary>
        public IDictionary<ScreenCaptureMode, string> ScreenCaptureModes => GreenshotLanguage.TranslationValuesForEnum<ScreenCaptureMode>();
    }
}
