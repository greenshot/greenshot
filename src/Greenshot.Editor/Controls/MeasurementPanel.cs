using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Measurements;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Controls
{
    internal sealed partial class MeasurementPanel : UserControl
    {
        private readonly Button _toggle = new Button { Text = "‹", Dock = DockStyle.Top, Height = 28, AccessibleName = "Expand measurements" };
        private readonly Panel _body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(6) };
        private readonly TableLayoutPanel _sections = Stack();
        private readonly Panel _resizeGrip = new Panel { Dock = DockStyle.Left, Width = 6, Cursor = Cursors.SizeWE, BackColor = SystemColors.ControlDark, AccessibleName = "Resize measurements panel" };
        private Control _geometryCategory;
        private int _expandedWidth = 340, _resizeStartX, _resizeStartWidth;
        private bool _resetScroll, _resizingPanel;
        private readonly ComboBox _units = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, AccessibleName = "Measurement units" };
        private readonly DataGridView _properties = new DataGridView
        {
            Width = 214, Height = 246, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, RowHeadersVisible = false, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle, AccessibleName = "Object dimensions and coordinates",
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2, SelectionMode = DataGridViewSelectionMode.CellSelect
        };
        private readonly Label _status = new Label { AutoSize = true };
        private readonly Button _clear = new Button { Text = "Clear scale", AutoSize = true };
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
        internal bool IsEditingValue => _properties.IsCurrentCellInEditMode || _documentDigits.ContainsFocus || _edgeDigits.ContainsFocus || _documentFormat.ContainsFocus || _edgeFormat.ContainsFocus;
        internal bool IsExpanded => _expanded;
        internal void FocusProperties() { if (_properties.Visible) _properties.Focus(); else _units.Focus(); }

        public MeasurementPanel()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            _expandedWidth = (int)(340 * DeviceDpi / 96.0);
            Dock = DockStyle.Fill;
            Width = 26;
            Margin = Padding.Empty;
            _units.Items.AddRange(new object[] { "Pixels", "mm", "cm", "m", "inches", "feet" });
            _units.SelectedIndex = 0;
            var calibrate = new Button { Text = "Calibrate…", AutoSize = true };
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
            ConfigureGrid(_properties);
            _properties.Columns[0].MinimumWidth = 100; _properties.Columns[0].FillWeight = 55;
            _properties.Columns[1].MinimumWidth = 80; _properties.Columns[1].FillWeight = 45;
            var origin = new Label { Text = "Origin: image top-left (0, 0)", AutoSize = true };
            _geometryCategory = AddCategory("Object geometry", _properties, origin);
            InitializeDimensionControls();
            AddCategory("Units and calibration", _units, _status, ButtonRow(calibrate, _clear));
            _body.Controls.Add(_sections);
            Controls.Add(_body);
            Controls.Add(_toggle);
            Controls.Add(_resizeGrip);
            _body.Visible = false;
            _resizeGrip.Visible = false;
            _resizeGrip.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                _resizeStartX = _resizeGrip.PointToScreen(e.Location).X; _resizeStartWidth = Width; _resizeGrip.Capture = true; _resizingPanel = true;
            };
            _resizeGrip.MouseMove += (s, e) =>
            {
                if (_resizingPanel && _resizeGrip.Capture) SetPanelWidth(_resizeStartWidth + _resizeStartX - _resizeGrip.PointToScreen(e.Location).X);
            };
            _resizeGrip.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) { _resizingPanel = false; _resizeGrip.Capture = false; } };
            _resizeGrip.MouseCaptureChanged += (s, e) => { if (!_resizeGrip.Capture) _resizingPanel = false; };
            SizeChanged += (s, e) => UpdateGridHeights();
            _toggle.Click += (s, e) => SetExpanded(!_expanded);
            _units.SelectedIndexChanged += (s, e) => { if (!_updating) UnitChanged?.Invoke(this, e); };
            calibrate.Click += (s, e) => CalibrateRequested?.Invoke(this, e);
            _clear.Click += (s, e) => ClearRequested?.Invoke(this, e);
        }

        private static TableLayoutPanel Stack()
        {
            var stack = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1, Margin = Padding.Empty, GrowStyle = TableLayoutPanelGrowStyle.AddRows };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); return stack;
        }

        private static void AddRow(TableLayoutPanel stack, Control control)
        {
            int row = stack.RowCount++; stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Top; control.Margin = new Padding(0, 3, 0, 3);
            if (control is Label label) label.MaximumSize = Size.Empty;
            stack.Controls.Add(control, 0, row);
        }

        private static TableLayoutPanel ButtonRow(params Control[] buttons)
        {
            var row = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = buttons.Length, RowCount = 1, Margin = Padding.Empty };
            row.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            for (int i = 0; i < buttons.Length; i++)
            {
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / buttons.Length));
                if (buttons[i] is Button button) button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                buttons[i].Dock = DockStyle.Fill; buttons[i].Margin = new Padding(i == 0 ? 0 : 3, 0, i == buttons.Length - 1 ? 0 : 3, 0);
                row.Controls.Add(buttons[i], i, 0);
            }
            return row;
        }

        private Control AddCategory(string title, params Control[] controls)
        {
            var section = Stack(); section.Margin = new Padding(0, 0, 0, 7);
            var header = new Button { Text = "▾ " + title, AccessibleName = title, AccessibleDescription = "Collapse " + title,
                AutoSize = true, MinimumSize = new Size(0, 28), TextAlign = ContentAlignment.MiddleLeft };
            var content = Stack(); content.Padding = new Padding(5, 0, 5, 3);
            foreach (var control in controls) AddRow(content, control);
            bool expanded = true;
            header.Click += (s, e) =>
            {
                expanded = !expanded; content.Visible = expanded;
                header.Text = (expanded ? "▾ " : "▸ ") + title;
                header.AccessibleDescription = (expanded ? "Collapse " : "Expand ") + title;
            };
            AddRow(section, header); AddRow(section, content); AddRow(_sections, section); return section;
        }

        private void ConfigureGrid(DataGridView grid)
        {
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.DefaultCellStyle.Padding = new Padding(3, 4, 3, 4);
            grid.RowTemplate.MinimumHeight = 25;
            grid.RowTemplate.Height = 28;
            grid.RowHeadersVisible = true; grid.RowHeadersWidth = 24;
            grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            grid.AllowUserToResizeColumns = grid.AllowUserToResizeRows = true;
            grid.RowHeightChanged += (s, e) => UpdateGridHeights();
        }

        private void UpdateGridHeights()
        {
            int height = Math.Max(170, Math.Min(330, _body.ClientSize.Height / 3));
            int GridHeight(DataGridView grid) => grid.ColumnHeadersHeight + grid.Rows.GetRowsHeight(DataGridViewElementStates.Visible) + 3;
            _properties.Height = Math.Min(height, GridHeight(_properties));
            _dimensionEdges.Height = Math.Min(height, GridHeight(_dimensionEdges));
        }

        internal void SetPanelWidth(int width)
        {
            int scale(int value) => (int)(value * DeviceDpi / 96.0);
            int maximum = Math.Max(scale(280), Math.Min(scale(640), (Parent?.ClientSize.Width ?? scale(900)) - scale(220)));
            _expandedWidth = Math.Max(scale(280), Math.Min(maximum, width));
            if (_expanded) Width = _expandedWidth;
        }

        internal void ConstrainPanelWidth()
        {
            if (!_expanded) return;
            if (Parent != null && Parent.ClientSize.Width < (int)(500 * DeviceDpi / 96.0)) SetExpanded(false);
            else SetPanelWidth(Width);
        }

        public void SetExpanded(bool expanded)
        {
            if (expanded && Parent != null && Parent.ClientSize.Width > 0 && Parent.ClientSize.Width < (int)(500 * DeviceDpi / 96.0)) expanded = false;
            _expanded = expanded;
            if (expanded) SetPanelWidth(_expandedWidth);
            else Width = (int)(26 * DeviceDpi / 96.0);
            _body.Visible = expanded;
            _resizeGrip.Visible = expanded;
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
                if (!ReferenceEquals(selected, _selected)) _resetScroll = true;
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
                    var property = values[i];
                    _properties.Rows[i].Cells[0].Value = property.Label;
                    _properties.Rows[i].Cells[0].ToolTipText = property.Label;
                    _properties.Rows[i].Cells[1].ReadOnly = property.ReadOnly;
                    double converted = property.Power == 0 ? property.Pixels : property.Power == 2 ? property.Pixels * Math.Pow(state.Convert(1), 2) : state.Convert(property.Pixels);
                    _properties.Rows[i].Cells[1].Value = converted.ToString("0.####", CultureInfo.CurrentCulture);
                    _properties.Rows[i].Cells[1].ToolTipText = Convert.ToString(_properties.Rows[i].Cells[1].Value, CultureInfo.CurrentCulture);
                    _properties.Rows[i].Cells[1].ErrorText = string.Empty;
                }
                _properties.Visible = values.Count > 0;
                _geometryCategory.Visible = values.Count > 0;
                UpdateGridHeights();
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
