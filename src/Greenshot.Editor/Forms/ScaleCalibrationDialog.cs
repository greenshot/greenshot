using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Greenshot.Editor.Measurements;

namespace Greenshot.Editor.Forms
{
    internal sealed class ScaleCalibrationDialog : Form
    {
        public MeasurementState Calibration { get; private set; }

        public ScaleCalibrationDialog(double pixels, MeasurementUnit preferredUnit)
        {
            Text = "Calibrate image scale";
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(330, 175);
            var description = new Label { Text = $"Real distance along the {pixels:0.##} px line:", AutoSize = true, Location = new Point(12, 12) };
            var distance = new TextBox { Location = new Point(12, 42), Width = 185, AccessibleName = "Known distance" };
            var units = new ComboBox { Location = new Point(205, 42), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Distance units" };
            units.Items.AddRange(new object[] { "mm", "cm", "m", "inches", "feet" });
            units.SelectedIndex = preferredUnit == MeasurementUnit.Pixels ? 0 : (int)preferredUnit - 1;
            var error = new Label { Location = new Point(12, 74), Size = new Size(305, 45), ForeColor = SystemColors.HotTrack };
            var ok = new Button { Text = "Calibrate", Location = new Point(155, 130), Width = 80 };
            var cancel = new Button { Text = "Cancel", Location = new Point(240, 130), Width = 75, DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { description, distance, units, error, ok, cancel });
            AcceptButton = ok;
            CancelButton = cancel;
            ok.Click += (s, e) =>
            {
                if (double.TryParse(distance.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value))
                {
                    try
                    {
                        Calibration = MeasurementState.Calibrate(pixels, value, (MeasurementUnit)(units.SelectedIndex + 1));
                        DialogResult = DialogResult.OK;
                        return;
                    }
                    catch (ArgumentOutOfRangeException) { }
                }
                error.Text = "Enter a positive, finite distance in the selected units.";
                distance.Focus();
                distance.SelectAll();
            };
            Shown += (s, e) => distance.Focus();
        }
    }
}
