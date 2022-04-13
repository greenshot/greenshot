/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Effects;

namespace Greenshot.Editor.Forms
{
    public partial class DropShadowSettingsForm : EditorForm
    {
        private readonly DropShadowEffect _effect;

        public DropShadowSettingsForm(DropShadowEffect effect)
        {
            _effect = effect;
            InitializeComponent();
            ShowSettings();
        }

        /// <summary>
        /// Apply the settings from the effect to the view
        /// </summary>
        private void ShowSettings()
        {
            trackBar1.Value = (int) (_effect.Darkness * 40);
            offsetX.Value = _effect.ShadowOffset.X;
            offsetY.Value = _effect.ShadowOffset.Y;
            thickness.Value = _effect.ShadowSize;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            _effect.Darkness = trackBar1.Value / (float) 40;
            _effect.ShadowOffset = new NativePoint((int) offsetX.Value, (int) offsetY.Value);
            _effect.ShadowSize = (int) thickness.Value;
            DialogResult = DialogResult.OK;
        }
    }
}