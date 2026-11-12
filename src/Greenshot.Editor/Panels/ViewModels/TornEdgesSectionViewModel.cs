/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Helpers;

namespace Greenshot.Editor.Panels.ViewModels
{
    /// <summary>
    /// The Torn edges section of the Selection panel: the torn sides and the shadow of the selected torn edges. A change shows at once
    /// and is an undo step (dragging a slider is one step); the settings are kept for the next torn edges. No UI toolkit here.
    /// </summary>
    internal sealed class TornEdgesSectionViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IEditorPanelContext _context;
        private readonly IEditorConfiguration _configuration;
        private TornEdgeContainer _tornEdges;

        public TornEdgesSectionViewModel(IEditorPanelContext context, IEditorConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _context.SelectionChanged += OnSelectionChanged;
            _context.CurrentSurfaceChanged += OnSelectionChanged;
            _context.ElementsChanged += OnElementsChanged;
            Attach(FindTornEdges());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool TornTop
        {
            get => GetEdge(0);
            set => SetEdge(0, value);
        }

        public bool TornRight
        {
            get => GetEdge(1);
            set => SetEdge(1, value);
        }

        public bool TornBottom
        {
            get => GetEdge(2);
            set => SetEdge(2, value);
        }

        public bool TornLeft
        {
            get => GetEdge(3);
            set => SetEdge(3, value);
        }

        /// <summary>
        /// The torn edges have a shadow, switched in the properties of the element
        /// </summary>
        public bool HasShadow => _tornEdges?.GetFieldValueAsBool(FieldType.SHADOW) == true;

        /// <summary>
        /// 1 to 40, like the slider: 40 is black
        /// </summary>
        public int ShadowDarkness
        {
            get => Math.Max(1, Math.Min(40, (int)Math.Round((Settings?.Darkness ?? 0.6f) * 40)));
            set => Change(settings => settings.Darkness = Math.Max(1, Math.Min(40, value)) / 40f);
        }

        public int ShadowSize
        {
            get => Math.Max(1, Math.Min(20, Settings?.ShadowSize ?? 7));
            set => Change(settings => settings.ShadowSize = Math.Max(1, Math.Min(20, value)));
        }

        public int ShadowOffsetX
        {
            get => Math.Max(-20, Math.Min(20, Settings?.ShadowOffset.X ?? 0));
            set => Change(settings => settings.ShadowOffset = new NativePoint(Math.Max(-20, Math.Min(20, value)), settings.ShadowOffset.Y));
        }

        public int ShadowOffsetY
        {
            get => Math.Max(-20, Math.Min(20, Settings?.ShadowOffset.Y ?? 0));
            set => Change(settings => settings.ShadowOffset = new NativePoint(settings.ShadowOffset.X, Math.Max(-20, Math.Min(20, value))));
        }

        /// <summary>
        /// Other random edges with the same settings, an undo step
        /// </summary>
        public void NewEdges() => Change(settings => settings.Seed = CutOutHelper.NewSeed(settings.Seed), false);

        private TornEdgeEffect Settings => _tornEdges?.GetSettings();

        private bool GetEdge(int index) => Settings?.Edges is { Length: 4 } edges && edges[index];

        private void SetEdge(int index, bool value) => Change(settings =>
        {
            var edges = settings.Edges is { Length: 4 } current ? (bool[])current.Clone() : new[] { true, true, true, true };
            edges[index] = value;
            settings.Edges = edges;
        }, false);

        /// <summary>
        /// Change the settings of the torn edges, as an undo step
        /// </summary>
        /// <param name="change">changes the settings</param>
        /// <param name="merge">true: the next change of the same setting is part of the same undo step (a slider)</param>
        /// <param name="setting">the property which changes</param>
        private void Change(Action<TornEdgeEffect> change, bool merge = true, [CallerMemberName] string setting = null)
        {
            var tornEdges = _tornEdges;
            if (tornEdges == null || _context.CurrentSurface is not Surface surface)
            {
                return;
            }

            var before = tornEdges.GetSettings();
            var settings = tornEdges.GetSettings();
            change(settings);
            if (Same(before, settings))
            {
                return;
            }

            surface.ChangeTornEdges(tornEdges, settings, merge ? setting : null);
            Remember(settings);
            OnAllChanged();
        }

        private static bool Same(TornEdgeEffect a, TornEdgeEffect b) =>
            a.Darkness.Equals(b.Darkness) && a.ShadowSize == b.ShadowSize && a.ShadowOffset == b.ShadowOffset && a.Seed == b.Seed
            && (a.Edges ?? Array.Empty<bool>()).SequenceEqual(b.Edges ?? Array.Empty<bool>());

        /// <summary>
        /// The next torn edges get the same sides and shadow
        /// </summary>
        private void Remember(TornEdgeEffect settings)
        {
            if (_configuration?.TornEdgeEffectSettings is not { } remembered)
            {
                return;
            }

            remembered.Edges = (bool[])settings.Edges?.Clone();
            remembered.Darkness = settings.Darkness;
            remembered.ShadowSize = settings.ShadowSize;
            remembered.ShadowOffset = settings.ShadowOffset;
        }

        private TornEdgeContainer FindTornEdges() =>
            _context.CurrentSurface?.SelectedElements is { Count: 1 } selected ? selected[0] as TornEdgeContainer : null;

        private void Attach(TornEdgeContainer tornEdges)
        {
            if (ReferenceEquals(tornEdges, _tornEdges))
            {
                return;
            }

            if (_tornEdges != null)
            {
                _tornEdges.FieldChanged -= OnFieldChanged;
            }

            _tornEdges = tornEdges;
            if (_tornEdges != null)
            {
                _tornEdges.FieldChanged += OnFieldChanged;
            }

            OnAllChanged();
        }

        private void OnSelectionChanged(object sender, EventArgs e) => Attach(FindTornEdges());

        /// <summary>
        /// E.g. an undo: the settings are shown as they are now
        /// </summary>
        private void OnElementsChanged(object sender, EventArgs e)
        {
            Attach(FindTornEdges());
            OnAllChanged();
        }

        private void OnFieldChanged(object sender, FieldChangedEventArgs e)
        {
            if (e.Field != null && Equals(e.Field.FieldType, FieldType.SHADOW))
            {
                OnPropertyChanged(nameof(HasShadow));
            }
        }

        private void OnAllChanged()
        {
            OnPropertyChanged(nameof(TornTop));
            OnPropertyChanged(nameof(TornRight));
            OnPropertyChanged(nameof(TornBottom));
            OnPropertyChanged(nameof(TornLeft));
            OnPropertyChanged(nameof(HasShadow));
            OnPropertyChanged(nameof(ShadowDarkness));
            OnPropertyChanged(nameof(ShadowSize));
            OnPropertyChanged(nameof(ShadowOffsetX));
            OnPropertyChanged(nameof(ShadowOffsetY));
        }

        public void Dispose()
        {
            _context.SelectionChanged -= OnSelectionChanged;
            _context.CurrentSurfaceChanged -= OnSelectionChanged;
            _context.ElementsChanged -= OnElementsChanged;
            Attach(null);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
