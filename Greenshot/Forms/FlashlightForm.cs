/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Forms;

namespace Greenshot {
	/// <summary>
	/// Description of FlashlightForm.
	/// </summary>
	public partial class FlashlightForm : Form {
		private int framesPerSecond = 25;
		public FlashlightForm() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			this.TransparencyKey = Color.Magenta;
		}
		
		public void FadeIn() {
			Opacity = 0;
			Show();
			Fade(2, 0, 1, 2);
		}
		public void FadeOut() {
			Fade(6, 1, 0, 2);
			Hide();
		}
		private void Fade(int frames, double startOpacity,  double targetOpacity, double exponent) {
			try {
				this.Opacity = startOpacity;
				double baseOpacity = Math.Min(startOpacity, targetOpacity);
				double diff = Math.Abs(targetOpacity - startOpacity);
				double stepWidth= (double)(10) / (double)frames;
				double maxValue = Math.Pow(10, exponent);
				for(int i=0; i<=frames; i++) {
					double x = ((startOpacity < targetOpacity) ? i : frames -i) * stepWidth;
					double factor = Math.Pow(x,exponent) / 100;
					this.Opacity = baseOpacity + factor * diff;
					Thread.Sleep(1000 / framesPerSecond);
				}
			} catch (Exception e) {
				Hide(); // ignore - after all, it's just a visual effect.
				log4net.LogManager.GetLogger(typeof(FlashlightForm)).Warn("An exception occured while trying to face FlashlightForm", e);
			}
		}
	}
}
