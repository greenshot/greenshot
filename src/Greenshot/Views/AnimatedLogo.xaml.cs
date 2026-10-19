/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Greenshot.Views
{
    /// <summary>
    /// The animated Greenshot "G" (g.svg): the bubbles pop in along the G when it's shown, then a wave or a few sparkles run through them now and then.
    /// </summary>
    public partial class AnimatedLogo : UserControl
    {
        private readonly Random _rand = new();
        private readonly DispatcherTimer _highlightTimer;
        private Storyboard _startupStoryboard;
        private Storyboard _waveStoryboard;
        private readonly SolidColorBrush[] _allDotBrushes;
        private readonly ScaleTransform[] _allDotScales;

        public AnimatedLogo()
        {
            InitializeComponent();

            _allDotBrushes = new[]
            {
                Dot0Brush, Dot1Brush, Dot2Brush, Dot3Brush, Dot4Brush, Dot5Brush, Dot6Brush,
                Dot7Brush, Dot8Brush, Dot9Brush, Dot10Brush, Dot11Brush, Dot12Brush, Dot13Brush,
                Dot14Brush, Dot15Brush, Dot16Brush, Dot17Brush, Dot18Brush, Dot19Brush, Dot20Brush
            };

            _allDotScales = new[]
            {
                Dot0Scale, Dot1Scale, Dot2Scale, Dot3Scale, Dot4Scale, Dot5Scale, Dot6Scale,
                Dot7Scale, Dot8Scale, Dot9Scale, Dot10Scale, Dot11Scale, Dot12Scale, Dot13Scale,
                Dot14Scale, Dot15Scale, Dot16Scale, Dot17Scale, Dot18Scale, Dot19Scale, Dot20Scale
            };

            // Setup highlight timer for periodic animations
            _highlightTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4.0)
            };
            _highlightTimer.Tick += OnHighlightTimerTick;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _startupStoryboard = TryFindResource("StartupStoryboard") as Storyboard;
            _waveStoryboard = TryFindResource("WaveStoryboard") as Storyboard;

            if (_startupStoryboard != null)
            {
                _startupStoryboard.Completed += (s, args) =>
                {
                    _highlightTimer.Interval = TimeSpan.FromMilliseconds(2500 + _rand.Next(3000));
                    _highlightTimer.Start();
                };
                _startupStoryboard.Begin(this);
            }
            else
            {
                _highlightTimer.Start();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _highlightTimer.Stop();
        }

        private void OnHighlightTimerTick(object sender, EventArgs e)
        {
            // Reset timer with next randomized interval (between 3.5s and 7s)
            _highlightTimer.Interval = TimeSpan.FromMilliseconds(3500 + _rand.Next(3500));

            // Randomly choose between full flow wave or sparkle pulses on a random subset of bubbles
            int animationMode = _rand.Next(3);
            if (animationMode == 0 && _waveStoryboard != null)
            {
                _waveStoryboard.Begin(this);
            }
            else
            {
                HighlightRandomBubbles();
            }
        }

        private void HighlightRandomBubbles()
        {
            int count = _rand.Next(2, 6);
            for (int i = 0; i < count; i++)
            {
                int index = _rand.Next(_allDotBrushes.Length);
                var brush = _allDotBrushes[index];
                var scale = _allDotScales[index];
                if (brush == null || scale == null) continue;

                var colorAnim = new ColorAnimationUsingKeyFrames
                {
                    BeginTime = TimeSpan.FromMilliseconds(i * 120)
                };
                colorAnim.KeyFrames.Add(new SplineColorKeyFrame(System.Windows.Media.Color.FromRgb(138, 255, 0), KeyTime.FromTimeSpan(TimeSpan.Zero)));
                colorAnim.KeyFrames.Add(new SplineColorKeyFrame(Colors.White, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2))));
                colorAnim.KeyFrames.Add(new SplineColorKeyFrame(System.Windows.Media.Color.FromRgb(138, 255, 0), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.45))));

                var scaleAnim = new DoubleAnimationUsingKeyFrames
                {
                    BeginTime = TimeSpan.FromMilliseconds(i * 120)
                };
                scaleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                scaleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.35, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2))));
                scaleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.45))));

                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }
        }
    }
}
