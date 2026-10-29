using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Measurements;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Controls
{
    internal sealed class MeasurementPanel : UserControl
    {
        private readonly Button _toggle = new Button { Text = "‹", Dock = DockStyle.Top, Height = 28, AccessibleName = "Expand measurements" };
        private readonly FlowLayoutPanel _body = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(5) };
        private readonly ComboBox _units = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, AccessibleName = "Measurement units" };
        private readonly DataGridView _properties = new DataGridView
        {
            Width = 214, Height = 246, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, RowHeadersVisible = false, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle, AccessibleName = "Object dimensions and coordinates",
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2, SelectionMode = DataGridViewSelectionMode.CellSelect
        };
        private readonly Label _status = new Label { AutoSize = true, MaximumSize = new Size(155, 0) };
        private readonly Button _clear = new Button { Text = "Clear calibration", AutoSize = true };
        private bool _updating;
        private bool _expanded;
        private bool _committing;
        private string _editOriginalText;
        private DrawableContainer _selected;
        private MeasurementState _state = MeasurementState.Pixels;
        public event EventHandler UnitChanged;
        public event EventHandler CalibrateRequested;
        public event EventHandler ClearRequested;
        public event EventHandler GeometryEdited;
        public MeasurementUnit SelectedUnit => (MeasurementUnit)Math.Max(0, _units.SelectedIndex);
        internal bool IsEditingValue => _properties.IsCurrentCellInEditMode;
        internal bool IsExpanded => _expanded;
        internal void FocusProperties() { if (_properties.Visible) _properties.Focus(); else _units.Focus(); }

        public MeasurementPanel()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            Dock = DockStyle.Fill;
            Width = 26;
            Margin = Padding.Empty;
            _units.Items.AddRange(new object[] { "Pixels", "mm", "cm", "m", "inches", "feet" });
            _units.SelectedIndex = 0;
            var calibrate = new Button { Text = "Calibrate / recalibrate", AutoSize = true };
            _properties.Columns.Add(new DataGridViewTextBoxColumn { Name = "Property", HeaderText = "Property", ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
            _properties.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value (px)", SortMode = DataGridViewColumnSortMode.NotSortable });
            _properties.CellValidating += ValidateProperty;
            _properties.CellBeginEdit += (s, e) => _editOriginalText = Convert.ToString(_properties.Rows[e.RowIndex].Cells[e.ColumnIndex].Value, CultureInfo.CurrentCulture);
            _properties.CellEndEdit += (s, e) =>
            {
                if (_updating) return;
                string error = null;
                string input = Convert.ToString(_properties.Rows[e.RowIndex].Cells[1].Value, CultureInfo.CurrentCulture);
                // EndEdit can commit without CellValidating (for example, an explicit programmatic commit).
                if (_editOriginalText != null && input != _editOriginalText) ApplyProperty(e.RowIndex, input, out error);
                _editOriginalText = null;
                RefreshDimensions(_selected, _state);
                if (error != null) _status.Text = error;
            };
            _properties.DataError += (s, e) => { e.ThrowException = false; };
            var origin = new Label { Text = "Coordinates: image top-left (0,0).", AutoSize = true, MaximumSize = new Size(215, 0) };
            _body.Controls.AddRange(new Control[] { _units, _properties, origin, _status, calibrate, _clear });
            Controls.Add(_body);
            Controls.Add(_toggle);
            _body.Visible = false;
            _toggle.Click += (s, e) => SetExpanded(!_expanded);
            _units.SelectedIndexChanged += (s, e) => { if (!_updating) UnitChanged?.Invoke(this, e); };
            calibrate.Click += (s, e) => CalibrateRequested?.Invoke(this, e);
            _clear.Click += (s, e) => ClearRequested?.Invoke(this, e);
        }

        public void SetExpanded(bool expanded)
        {
            _expanded = expanded;
            Width = (int)((expanded ? 240 : 26) * DeviceDpi / 96.0);
            _body.Visible = expanded;
            _toggle.Text = expanded ? "Measurements  ›" : "‹";
            _toggle.AccessibleName = expanded ? "Collapse measurements" : "Expand measurements";
        }

        public void RefreshDimensions(IDrawableContainer selected, MeasurementState state)
        {
            if (_committing) return;
            bool sameContext = ReferenceEquals(selected, _selected) && ReferenceEquals(state, _state);
            if (sameContext && _properties.IsCurrentCellInEditMode) return;
            _updating = true;
            try
            {
                if (!sameContext) _properties.CancelEdit();
                _selected = selected as DrawableContainer;
                _state = state;
                _units.SelectedIndex = (int)state.Unit;
                _clear.Enabled = state.IsCalibrated;
                _status.Text = state.IsCalibrated ? "Image calibrated" : "Calibrate for real-world units.";
                _properties.Columns[1].HeaderText = "Value (" + MeasurementState.Symbol(state.Unit) + ")";
                var values = MeasurementGeometry.GetProperties(selected);
                bool rebuild = _properties.Rows.Count != values.Count;
                if (!rebuild)
                    for (int i = 0; i < values.Count; i++) if (!Equals(_properties.Rows[i].Tag, values[i].Key)) { rebuild = true; break; }
                if (rebuild)
                {
                    _properties.Rows.Clear();
                    foreach (var property in values)
                    {
                        int row = _properties.Rows.Add(property.Label, null);
                        _properties.Rows[row].Tag = property.Key;
                    }
                }
                for (int i = 0; i < values.Count; i++)
                {
                    _properties.Rows[i].Cells[1].Value = state.Convert(values[i].Pixels).ToString("0.####", CultureInfo.CurrentCulture);
                    _properties.Rows[i].Cells[1].ErrorText = string.Empty;
                }
                _properties.Height = Math.Min(290, _properties.ColumnHeadersHeight + values.Count * _properties.RowTemplate.Height + 3);
                _properties.Visible = values.Count > 0;
            }
            finally { _updating = false; }
        }

        private void ValidateProperty(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_updating || e.ColumnIndex != 1 || _selected == null || _editOriginalText == null) return;
            string input = Convert.ToString(e.FormattedValue, CultureInfo.CurrentCulture);
            if (input == _editOriginalText) return;
            bool applied = ApplyProperty(e.RowIndex, input, out string error);
            e.Cancel = !applied;
            _properties.Rows[e.RowIndex].Cells[1].ErrorText = applied ? string.Empty : error;
            if (!applied) _status.Text = error;
        }

        private bool ApplyProperty(int row, string input, out string error)
        {
            error = "Enter a finite number.";
            bool applied = false;
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out double value))
            {
                _committing = true;
                try { applied = MeasurementGeometry.TryEdit(_selected, _state, (string)_properties.Rows[row].Tag, value, out error); }
                finally { _committing = false; }
            }
            if (applied) GeometryEdited?.Invoke(this, EventArgs.Empty);
            return applied;
        }
    }
}
