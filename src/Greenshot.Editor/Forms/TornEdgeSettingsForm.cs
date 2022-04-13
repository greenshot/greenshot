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
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Effects;

namespace Greenshot.Editor.Forms
{
    public partial class TornEdgeSettingsForm : EditorForm
    {
        private readonly TornEdgeEffect _effect;

        public TornEdgeSettingsForm(TornEdgeEffect effect)
        {
            _effect = effect;
            InitializeComponent();
            ShowSettings();
        }

        private void ShowSettings()
        {
            shadowCheckbox.Checked = _effect.GenerateShadow;
            // Fix to prevent BUG-1753
            shadowDarkness.Value = Math.Max(shadowDarkness.Minimum, Math.Min(shadowDarkness.Maximum, (int) (_effect.Darkness * shadowDarkness.Maximum)));
            offsetX.Value = _effect.ShadowOffset.X;
            offsetY.Value = _effect.ShadowOffset.Y;
            toothsize.Value = _effect.ToothHeight;
            verticaltoothrange.Value = _effect.VerticalToothRange;
            horizontaltoothrange.Value = _effect.HorizontalToothRange;
            top.Checked = _effect.Edges[0];
            right.Checked = _effect.Edges[1];
            bottom.Checked = _effect.Edges[2];
            left.Checked = _effect.Edges[3];
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            _effect.Darkness = shadowDarkness.Value / (float) 40;
            _effect.ShadowOffset = new NativePoint((int) offsetX.Value, (int) offsetY.Value);
            _effect.ShadowSize = (int) thickness.Value;
            _effect.ToothHeight = (int) toothsize.Value;
            _effect.VerticalToothRange = (int) verticaltoothrange.Value;
            _effect.HorizontalToothRange = (int) horizontaltoothrange.Value;
            _effect.Edges = new[]
            {
                top.Checked, right.Checked, bottom.Checked, left.Checked
            };
            _effect.GenerateShadow = shadowCheckbox.Checked;
            DialogResult = DialogResult.OK;
        }

        private void ShadowCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            thickness.Enabled = shadowCheckbox.Checked;
            offsetX.Enabled = shadowCheckbox.Checked;
            offsetY.Enabled = shadowCheckbox.Checked;
            shadowDarkness.Enabled = shadowCheckbox.Checked;
        }


        private void all_CheckedChanged(object sender, EventArgs e)
        {
            AnySideChangeChecked(top, all.Checked);
            AnySideChangeChecked(right, all.Checked);
            AnySideChangeChecked(bottom, all.Checked);
            AnySideChangeChecked(left, all.Checked);
        }

        private void AnySideCheckedChanged(object sender, EventArgs e)
        {
            all.CheckedChanged -= all_CheckedChanged;
            all.Checked = top.Checked && right.Checked && bottom.Checked && left.Checked;
            all.CheckedChanged += all_CheckedChanged;
        }

        /// <summary>
        /// changes the Checked property of top/right/bottom/left checkboxes without triggering AnySideCheckedChange
        /// </summary>
        /// <param name="cb">Checkbox to change Checked</param>
        /// <param name="status">true to check</param>
        private void AnySideChangeChecked(CheckBox cb, bool status)
        {
            if (status != cb.Checked)
            {
                cb.CheckedChanged -= AnySideCheckedChanged;
                cb.Checked = status;
                cb.CheckedChanged += AnySideCheckedChanged;
            }
        }

        private void TornEdgeSettingsForm_Load(object sender, EventArgs e)
        {
        }
    }
}