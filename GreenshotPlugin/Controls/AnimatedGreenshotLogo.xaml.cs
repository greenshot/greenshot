/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace GreenshotPlugin.Controls
{
	/// <summary>
	/// Interaction logic for AnimatedGreenshotLogo.xaml
	/// </summary>
	public partial class AnimatedGreenshotLogo : UserControl
	{
		public AnimatedGreenshotLogo()
		{
			InitializeComponent();
			CreateAnimation();
			Loaded += AnimatedGreenshotLogo_Loaded;
		}

		private void AnimatedGreenshotLogo_Loaded(object sender, RoutedEventArgs e)
		{
			CreateAnimation();
		}

		private void CreateAnimation()
		{
			// Create glimmer "Color-Cycle" animation

			var dots = new Ellipse[]
			{
				L1C1, L1C2, L1C3, L1C4, L1C5, L2C1, L2C2, L3C1, L3C2, L4C1, L4C2, L5C1, L6C1, L6C2, L6C3, L6C4, L7C1, L7C2, L8C1, L8C2, L8C3
			};

			int delayOffset = 15;
			int initialDelay = 5000;
			foreach (var ellipse in dots)
			{
				var storyBoard = new Storyboard();
				storyBoard.BeginTime = TimeSpan.FromMilliseconds(initialDelay);
				storyBoard.RepeatBehavior = RepeatBehavior.Forever;
				storyBoard.Duration = TimeSpan.FromSeconds(10);

				var goBright = new ColorAnimation();
				goBright.From = Color.FromArgb(0xff, 0x8a, 0xff, 0x00);
				goBright.To = Colors.White;
				goBright.Duration = TimeSpan.FromMilliseconds(400);
				storyBoard.Children.Add(goBright);
				var goNormal = new ColorAnimation();
				goNormal.To = Color.FromArgb(0xff, 0x8a, 0xff, 0x00);
				goNormal.From = Colors.White;
				goNormal.Duration = TimeSpan.FromMilliseconds(400);
				storyBoard.Children.Add(goNormal);
				Storyboard.SetTarget(goBright, ellipse);
				Storyboard.SetTargetProperty(goBright, new PropertyPath("(Shape.Fill).(SolidColorBrush.Color)"));
				Storyboard.SetTarget(goNormal, ellipse);
				Storyboard.SetTargetProperty(goNormal, new PropertyPath("(Shape.Fill).(SolidColorBrush.Color)"));
				ellipse.BeginStoryboard(storyBoard);
				initialDelay += delayOffset;
			}
		}
	}
}