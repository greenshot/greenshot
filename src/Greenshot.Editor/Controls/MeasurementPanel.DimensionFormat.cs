using System;
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Editor.Measurements;

namespace Greenshot.Editor.Controls
{
    internal sealed partial class MeasurementPanel
    {
        private readonly ComboBox _documentFormat = new ComboBox { Width = 174, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Document dimension format" };
        private readonly NumericUpDown _documentDigits = new NumericUpDown { Width = 36, Minimum = 0, Maximum = 10, AccessibleName = "Document dimension digits" };
        private readonly Button _applyDimensionFormat = new Button { Text = "Apply to all dimensions", Width = 214, AccessibleName = "Apply to all dimensions" };
        private readonly ComboBox _edgeFormat = new ComboBox { Width = 174, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Selected edge dimension format" };
        private readonly NumericUpDown _edgeDigits = new NumericUpDown { Width = 36, Minimum = 0, Maximum = 10, AccessibleName = "Selected edge dimension digits" };
        private readonly CheckBox _dimensionTransparent = new CheckBox { Text = "Transparent background", AutoSize = true, AccessibleName = "Transparent dimension background" };
        private readonly Button _dimensionBackground = new Button { Text = "Background colour…", Width = 214, AccessibleName = "Dimension background colour" };
        private readonly CheckBox _inheritDimensionBackground = new CheckBox { Text = "Use document background", AutoSize = true };
        private readonly Label _backgroundScope = new Label { AutoSize = true };
        private readonly Label _edgePrecisionLabel = new Label { Text = "Selected edge", AutoSize = true };
        private Control _edgePrecisionRow;
        private int SelectedDimensionEdge => _dimensionEdges.CurrentRow?.Tag is int index ? index : -1;
        private bool HasSelectedDimension => _selected != null && SelectedDimensionEdge >= 0;
        private Color CurrentDimensionBackground => (HasSelectedDimension ? _selected.GetDimensionBackground(SelectedDimensionEdge) : null)
            ?? _dimensionDocument?.DimensionStyle.BackgroundColor ?? Color.Transparent;
        private void SetCurrentDimensionBackground(Color color)
        {
            if (HasSelectedDimension) _selected.SetDimensionBackground(SelectedDimensionEdge, color);
            else _dimensionDocument.SetDimensionStyle(_dimensionDocument.DimensionStyle.WithBackground(color));
        }

        private void InitializeDimensionFormatControls()
        {
            _documentFormat.Items.AddRange(new object[] { "Automatic", "Decimal places", "Significant figures" });
            _edgeFormat.Items.AddRange(new object[] { "Use document default", "Automatic", "Decimal places", "Significant figures" });
            TableLayoutPanel Row(Control first, Control second)
            {
                var row = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
                row.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64 * DeviceDpi / 96f));
                first.Dock = second.Dock = DockStyle.Fill;
                first.Margin = new Padding(0, 0, 6, 0); second.Margin = Padding.Empty;
                row.Controls.Add(first, 0, 0); row.Controls.Add(second, 1, 0); return row;
            }
            _edgePrecisionRow = Row(_edgeFormat, _edgeDigits);
            _applyDimensionFormat.Text = "Apply to all edges";
            AddCategory("Dimension precision",
                new Label { Text = "Document default", AutoSize = true }, Row(_documentFormat, _documentDigits), _applyDimensionFormat,
                _edgePrecisionLabel, _edgePrecisionRow);
            AddCategory("Dimension background", _backgroundScope, _inheritDimensionBackground, _dimensionTransparent, _dimensionBackground);
            _inheritDimensionBackground.CheckedChanged += (s, e) =>
            {
                if (_dimensionsUpdating || !HasSelectedDimension) return;
                _selected.SetDimensionBackground(SelectedDimensionEdge, _inheritDimensionBackground.Checked ? (Color?)null : CurrentDimensionBackground);
                RefreshDimensionLabels(_dimensionDocument); GeometryEdited?.Invoke(this, EventArgs.Empty);
            };
            _documentFormat.SelectedIndexChanged += (s,e) => ChangeDocumentFormat();
            _documentDigits.ValueChanged += (s,e) => ChangeDocumentFormat();
            _edgeFormat.SelectedIndexChanged += (s,e) => ChangeEdgeFormat();
            _edgeDigits.ValueChanged += (s,e) => ChangeEdgeFormat();
            _dimensionEdges.SelectionChanged += (s,e) => { if (!_dimensionsUpdating) RefreshDimensionFormatControls(); };
            _applyDimensionFormat.Click += (s,e) =>
            {
                if (_dimensionDocument == null) return;
                _dimensionDocument.ApplyDimensionFormatToAll(ReadFormat(_documentFormat, _documentDigits, false));
                RefreshDimensionLabels(_dimensionDocument); GeometryEdited?.Invoke(this, EventArgs.Empty);
            };
            _dimensionTransparent.CheckedChanged += (s,e) =>
            {
                if (_dimensionsUpdating || _dimensionDocument == null) return;
                SetCurrentDimensionBackground(Color.FromArgb(_dimensionTransparent.Checked ? 0 : 255, CurrentDimensionBackground));
                RefreshDimensionLabels(_dimensionDocument); GeometryEdited?.Invoke(this, EventArgs.Empty);
            };
            _dimensionBackground.Click += (s,e) =>
            {
                if (_dimensionDocument == null) return;
                using var picker = new ColorDialog { Color = Color.FromArgb(255, CurrentDimensionBackground), FullOpen = true };
                if (picker.ShowDialog(FindForm()) != DialogResult.OK) return;
                SetCurrentDimensionBackground(picker.Color);
                RefreshDimensionLabels(_dimensionDocument); GeometryEdited?.Invoke(this, EventArgs.Empty);
            };
        }
        private static DimensionFormat ReadFormat(ComboBox mode, NumericUpDown digits, bool edge)
        {
            int selected = mode.SelectedIndex - (edge ? 1 : 0);
            if (selected < 0) return null;
            var format = (DimensionFormatMode)selected;
            return format == DimensionFormatMode.Automatic ? DimensionFormat.Automatic :
                new DimensionFormat(format, Math.Max(format == DimensionFormatMode.SignificantFigures ? 1 : 0, (int)digits.Value));
        }
        private void ChangeDocumentFormat()
        {
            if (_dimensionsUpdating || _dimensionDocument == null) return;
            _dimensionDocument.SetDimensionStyle(_dimensionDocument.DimensionStyle.WithFormat(ReadFormat(_documentFormat, _documentDigits, false)));
            RefreshDimensionLabels(_dimensionDocument); GeometryEdited?.Invoke(this, EventArgs.Empty);
        }
        private void ChangeEdgeFormat()
        {
            if (_dimensionsUpdating || _selected == null || SelectedDimensionEdge < 0) return;
            _selected.SetDimensionFormat(SelectedDimensionEdge, ReadFormat(_edgeFormat, _edgeDigits, true));
            RefreshDimensionLabels(_dimensionDocument); GeometryEdited?.Invoke(this, EventArgs.Empty);
        }
        private static void ShowFormat(ComboBox mode, NumericUpDown digits, DimensionFormat format, bool edge, bool enabled)
        {
            mode.SelectedIndex = format == null ? 0 : (int)format.Mode + (edge ? 1 : 0);
            digits.Minimum = format?.Mode == DimensionFormatMode.SignificantFigures ? 1 : 0;
            digits.Value = Math.Max(digits.Minimum, format?.Digits ?? 0);
            mode.Enabled = enabled; digits.Enabled = enabled && format != null && format.Mode != DimensionFormatMode.Automatic;
        }
        private void RefreshDimensionFormatControls()
        {
            bool updating = _dimensionsUpdating; _dimensionsUpdating = true;
            try
            {
                var style = _dimensionDocument?.DimensionStyle ?? DimensionStyle.Default;
                ShowFormat(_documentFormat, _documentDigits, style.Format, false, _dimensionDocument != null);
                ShowFormat(_edgeFormat, _edgeDigits, _selected?.GetDimensionFormat(SelectedDimensionEdge), true, _selected != null && SelectedDimensionEdge >= 0);
                _edgePrecisionLabel.Visible = _edgePrecisionRow.Visible = HasSelectedDimension;
                _applyDimensionFormat.Enabled = _dimensionDocument != null;
                _dimensionTransparent.Enabled = _dimensionBackground.Enabled = _dimensionDocument != null;
                _backgroundScope.Text = HasSelectedDimension ? "Selected edge" : "Document default";
                _inheritDimensionBackground.Visible = HasSelectedDimension;
                _inheritDimensionBackground.Checked = HasSelectedDimension && !_selected.GetDimensionBackground(SelectedDimensionEdge).HasValue;
                var background = CurrentDimensionBackground;
                _dimensionTransparent.Checked = background.A == 0;
                _dimensionBackground.BackColor = Color.FromArgb(255, background);
            }
            finally { _dimensionsUpdating = updating; }
        }
    }
}
