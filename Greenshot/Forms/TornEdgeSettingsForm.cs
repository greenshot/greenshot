using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Greenshot.Core;
using GreenshotPlugin.Core;

namespace Greenshot.Forms {
	public partial class TornEdgeSettingsForm : BaseForm {
		private TornEdgeEffect effect;
		public TornEdgeSettingsForm(TornEdgeEffect effect) {
			this.effect = effect;
			InitializeComponent();
			this.Icon = GreenshotResources.getGreenshotIcon();
			trackBar1.Value = (int)(effect.Darkness * 40);
			offsetX.Value = effect.ShadowOffset.X;
			offsetY.Value = effect.ShadowOffset.Y;
			toothsize.Value = effect.ToothHeight;
			verticaltoothrange.Value = effect.VerticalToothRange;
			horizontaltoothrange.Value = effect.HorizontalToothRange;
		}

		private void buttonOK_Click(object sender, EventArgs e) {
			effect.Darkness = (float)trackBar1.Value / (float)40;
			effect.ShadowOffset = new Point((int)offsetX.Value, (int)offsetY.Value);
			effect.ShadowSize = (int)thickness.Value;
			effect.ToothHeight = (int)toothsize.Value;
			effect.VerticalToothRange = (int)verticaltoothrange.Value;
			effect.HorizontalToothRange = (int)horizontaltoothrange.Value;
			DialogResult = DialogResult.OK;
		}
	}
}
