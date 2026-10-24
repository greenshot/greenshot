using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Measurements;

namespace Greenshot.Editor.Controls
{
    internal sealed class MeasurementPanel : UserControl
    {
        private readonly Button _toggle = new Button { Text = "‹", Dock = DockStyle.Top, Height = 28, AccessibleName = "Expand measurements" };
        private readonly FlowLayoutPanel _body = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(5) };
        private readonly ComboBox _units = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, AccessibleName = "Measurement units" };
        private readonly Label _dimensions = new Label { AutoSize = true, Margin = new Padding(3, 12, 3, 12) };
        private readonly Label _status = new Label { AutoSize = true, MaximumSize = new Size(155, 0) };
        private readonly Button _clear = new Button { Text = "Clear calibration", AutoSize = true };
        private bool _updating;
        private bool _expanded;
        public event EventHandler UnitChanged;
        public event EventHandler CalibrateRequested;
        public event EventHandler ClearRequested;
        public MeasurementUnit SelectedUnit => (MeasurementUnit)Math.Max(0, _units.SelectedIndex);

        public MeasurementPanel()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            Dock = DockStyle.Fill;
            Width = 26;
            Margin = Padding.Empty;
            _units.Items.AddRange(new object[] { "Pixels", "mm", "cm", "m", "inches", "feet" });
            _units.SelectedIndex = 0;
            var calibrate = new Button { Text = "Calibrate / recalibrate", AutoSize = true };
            _body.Controls.AddRange(new Control[] { _units, _dimensions, _status, calibrate, _clear });
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
            Width = (int)((expanded ? 185 : 26) * DeviceDpi / 96.0);
            _body.Visible = expanded;
            _toggle.Text = expanded ? "Measurements  ›" : "‹";
            _toggle.AccessibleName = expanded ? "Collapse measurements" : "Expand measurements";
        }

        public void RefreshDimensions(IDrawableContainer selected, MeasurementState state)
        {
            _updating = true;
            _units.SelectedIndex = (int)state.Unit;
            _updating = false;
            _clear.Enabled = state.IsCalibrated;
            _status.Text = state.IsCalibrated ? "Image calibrated" : "Calibrate for real-world units.";
            _dimensions.Text = string.Join(Environment.NewLine, MeasurementCalculator.GetDimensions(selected)
                .Select(d => $"{d.Label} = {state.Convert(d.Pixels).ToString("0.##", CultureInfo.CurrentCulture)} {MeasurementState.Symbol(state.Unit)}"));
        }
    }
}
