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

using System;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Languages;
using Greenshot.Editor.Configuration;

namespace Greenshot.Editor.Panels.Effects
{
    /// <summary>
    /// The Effects panel of the editor: border, drop shadow, torn edges, grayscale, invert, remove transparency and resize with
    /// presets, settings and a preview. Shown from the Effects menu and the Resize button of the editor, or the panel list.
    /// One instance per editor window.
    /// </summary>
    internal sealed class EffectsPanel : IEditorPanel
    {
        public const string PanelId = "Greenshot.Effects";

        private readonly IEditorConfiguration _configuration;
        private readonly Action _applied;
        private EffectKind _requestedEffect = EffectKind.Border;

        /// <param name="configuration">IEditorConfiguration with the settings of drop shadow and torn edges</param>
        /// <param name="applied">called after an effect was applied</param>
        public EffectsPanel(IEditorConfiguration configuration, Action applied)
        {
            _configuration = configuration;
            _applied = applied;
        }

        public string Id => PanelId;

        public string Title => Texts.Editor.Effects;

        // After Selection (0), before the panels of the plugins (100 and more)
        public int Order => 10;

        /// <summary>
        /// The Effects menu of the editor shows it
        /// </summary>
        public string MenuText => null;

        public string Shortcut => null;

        /// <summary>
        /// The view model of the window, once the panel was shown
        /// </summary>
        internal EffectsPanelViewModel ViewModel { get; private set; }

        public bool IsAvailable(IEditorPanelContext context) => true;

        /// <summary>
        /// The effect to show: the view model shows it now, or when it's made
        /// </summary>
        internal void Request(EffectKind effect)
        {
            _requestedEffect = effect;
            ViewModel?.Show(effect);
        }

        public object CreateViewModel(IEditorPanelContext context)
        {
            ViewModel = new EffectsPanelViewModel(context, _configuration, _applied);
            ViewModel.Show(_requestedEffect);
            return ViewModel;
        }

        public object CreateView(object viewModel) => new EffectsPanelView { DataContext = viewModel };
    }
}
