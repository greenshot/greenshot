using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Controls;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Measurements;

namespace Greenshot.Editor.Forms
{
    public partial class ImageEditorForm
    {
        private MeasurementPanel _measurementPanel;
        private QuickColorPalette _quickColors;
        private ToolStripButton _scaleButton, _polygonButton, _groupButton, _ungroupButton, _rotateObjectButton;
        private DrawableContainer _measuredObject;
        private CheckBox _snapPoints, _snapThrough;
        private NumericUpDown _snappingRadius;

        private void InitializeMeasurements()
        {
            _snapPoints = AddSnapCheckBox("Snap to points", "Snap handles and objects to nearby object points");
            _snapThrough = AddSnapCheckBox("Snap through", "Align lines through nearby object points");
            _snappingRadius = new NumericUpDown { Minimum = 1, Maximum = 100, Width = 48,
                Value = Math.Max(1, Math.Min(100, EditorConfiguration.SnappingRadius)), AccessibleName = "Snapping radius" };
            destinationsToolStrip.Items.Add(new ToolStripLabel("Snapping radius"));
            destinationsToolStrip.Items.Add(new ToolStripControlHost(_snappingRadius) { Overflow = ToolStripItemOverflow.Never });
            var savedPoints = EditorConfiguration.SnapToPoints;
            var savedThrough = EditorConfiguration.SnapThrough;
            _snapPoints.Checked = savedPoints;
            _snapThrough.Checked = savedThrough;
            _snappingRadius.ValueChanged += (s, e) => SaveSnappingOptions();
            _measurementPanel = new MeasurementPanel();
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayoutPanel1.Controls.Add(_measurementPanel, 1, 0);
            _measurementPanel.CalibrateRequested += (s, e) => BeginCalibration();
            _measurementPanel.ClearRequested += (s, e) => _surface?.SetMeasurements(MeasurementState.Pixels);
            _measurementPanel.GeometryEdited += (s, e) => UpdateUndoRedoSurfaceDependencies();
            _measurementPanel.UnitChanged += (s, e) =>
            {
                if (_surface == null) return;
                if (!_surface.Measurements.IsCalibrated && _measurementPanel.SelectedUnit != MeasurementUnit.Pixels)
                {
                    UpdateMeasurements();
                    BeginCalibration();
                }
                else _surface.SetMeasurements(_surface.Measurements.WithUnit(_measurementPanel.SelectedUnit));
            };
            _scaleButton = new ToolStripButton("Scale")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Scale: click two points; hold Shift to snap the angle", AccessibleName = "Scale", Image = CreateScaleIcon()
            };
            _polygonButton = new ToolStripButton("Polygon") { DisplayStyle = ToolStripItemDisplayStyle.Text, ToolTipText = "Polygon: click vertices, then the first vertex to close; Esc cancels" };
            _polygonButton.Click += (s, e) => { if (_surface == null) return; _surface.DrawingMode = DrawingModes.Polygon; FocusCanvas(fromKeyboard: false); };
            toolsToolStrip.Items.Insert(toolsToolStrip.Items.IndexOf(btnFreehand) + 1, _polygonButton);
            _groupButton = new ToolStripButton("Group") { ToolTipText = "Group selected objects (Ctrl+Alt+G)", Enabled = false };
            _ungroupButton = new ToolStripButton("Ungroup") { ToolTipText = "Ungroup selected objects (Ctrl+Shift+G)", Enabled = false };
            _groupButton.Click += (s, e) => { _surface?.GroupSelectedElements(); FocusCanvas(fromKeyboard: false); };
            _ungroupButton.Click += (s, e) => { _surface?.UngroupSelectedElements(); FocusCanvas(fromKeyboard: false); };
            toolsToolStrip.Items.Add(_groupButton); toolsToolStrip.Items.Add(_ungroupButton);
            _rotateObjectButton = new ToolStripButton("Rotate…") { Enabled = false, ToolTipText = "Rotate selected objects by an angle" };
            _rotateObjectButton.Click += (s, e) => RotateObjects();
            toolsToolStrip.Items.Add(_rotateObjectButton);
            _scaleButton.Click += (s, e) => BeginCalibration();
            toolsToolStrip.Items.Insert(toolsToolStrip.Items.IndexOf(btnLine) + 1, _scaleButton);
            _quickColors = new QuickColorPalette(propertiesToolStrip, EditorConfiguration);
            _quickColors.ColorSelected += color =>
            {
                if (_surface == null) return;
                var fields = (FieldAggregator)_surface.FieldAggregator;
                if (!fields.HasFieldValue(_quickColors.Target)) return;
                fields.GetField(_quickColors.Target).Value = color;
                _quickColors.Remember(color);
                FocusCanvas(fromKeyboard: false);
            };
            btnLineColor.ColorSelected += (s, e) => _quickColors.Remember(btnLineColor.SelectedColor);
            btnFillColor.ColorSelected += (s, e) => _quickColors.Remember(btnFillColor.SelectedColor);
            Activated += (s, e) => RefreshQuickColors();
            Disposed += (s, e) =>
            {
                if (_measuredObject != null) _measuredObject.GeometryChanged -= SelectedGeometryChanged;
                _quickColors.Dispose();
                _scaleButton.Image?.Dispose();
            };
        }

        private CheckBox AddSnapCheckBox(string label, string tip)
        {
            var checkBox = new CheckBox { Text = label, AutoSize = true, AccessibleName = label, AccessibleDescription = tip, BackColor = Color.Transparent };
            destinationsToolStrip.Items.Add(new ToolStripControlHost(checkBox) { ToolTipText = tip, Overflow = ToolStripItemOverflow.Never });
            checkBox.CheckedChanged += (s, e) => SaveSnappingOptions();
            return checkBox;
        }

        private void SaveSnappingOptions()
        {
            if (_snapPoints == null || _snapThrough == null || _snappingRadius == null) return;
            EditorConfiguration.SnapToPoints = _snapPoints.Checked;
            EditorConfiguration.SnapThrough = _snapThrough.Checked;
            EditorConfiguration.SnappingRadius = (int)_snappingRadius.Value;
            _surface?.SetObjectSnapping(_snapPoints.Checked, _snapThrough.Checked, (int)_snappingRadius.Value);
        }

        private static Bitmap CreateScaleIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bitmap);
            g.FillRectangle(Brushes.LightGoldenrodYellow, 1, 4, 14, 8);
            g.DrawRectangle(Pens.DimGray, 1, 4, 14, 8);
            for (int x = 3; x < 15; x += 3) g.DrawLine(Pens.DimGray, x, 4, x, x % 2 == 0 ? 9 : 7);
            return bitmap;
        }

        private void RotateObjects()
        {
            if (_surface?.CanRotateObjects != true) return;
            using var dialog = new Form { Text = "Rotate selected objects", Width = 420, Height = 210, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var angle = new NumericUpDown { DecimalPlaces = 4, Minimum = -36000, Maximum = 36000, Width = 140, Left = 16, Top = 75, AccessibleName = "Clockwise rotation delta in degrees" };
            string current = _surface.SelectedElements.Count == 1 ? "Current angle: " + ((DrawableContainer)_surface.SelectedElements[0]).ObjectAngle.ToString("0.####") + "°. " : _surface.SelectedElements.Count + " objects. ";
            var label = new Label { Text = current + "Enter clockwise change in degrees. Pivot: mean of selected object centres.", Left = 16, Top = 16, Width = 375, Height = 50 };
            var ok = new Button { Text = "Rotate", DialogResult = DialogResult.OK, Left = 210, Top = 120 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 295, Top = 120 };
            dialog.Controls.AddRange(new Control[] { label, angle, ok, cancel }); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
            if (dialog.ShowDialog(this) == DialogResult.OK) _surface.RotateSelectedObjects((double)angle.Value);
            FocusCanvas(fromKeyboard: false);
        }

        private void BeginCalibration()
        {
            if (_surface == null || _controlsDisabledDueToConfirmable) return;
            _measurementPanel.SetExpanded(true);
            _surface.DrawingMode = DrawingModes.Scale;
            UpdateStatusLabel("Click the first point, then the second point. Hold Shift to snap in 15° steps; Esc cancels.");
            FocusCanvas(fromKeyboard: false);
        }

        private void BindMeasurements()
        {
            _surface.SetObjectSnapping(_snapPoints.Checked, _snapThrough.Checked, (int)_snappingRadius.Value);
            _surface.MeasurementsChanged += SelectedGeometryChanged;
            _surface.ScaleLineCompleted += CompleteCalibration;
            UpdateMeasurements();
        }

        private void UnbindMeasurements()
        {
            _surface.MeasurementsChanged -= SelectedGeometryChanged;
            _surface.ScaleLineCompleted -= CompleteCalibration;
            _surface.CancelScalePreview();
            _surface.CancelPolygonInteraction();
            if (_measuredObject != null) _measuredObject.GeometryChanged -= SelectedGeometryChanged;
            _measuredObject = null;
            _measurementPanel.RefreshDimensions(null, MeasurementState.Pixels);
        }

        private void SelectedGeometryChanged(object sender, EventArgs e) => UpdateMeasurements();

        private void UpdateMeasurements()
        {
            if (_measurementPanel == null) return;
            _rotateObjectButton.Enabled = _surface?.CanRotateObjects == true;
            _rotateObjectButton.ToolTipText = _rotateObjectButton.Enabled ? "Rotate selected objects by an angle" : Surface.RotationLimit;
            _groupButton.Enabled = _surface?.CanGroup == true;
            _ungroupButton.Enabled = _surface?.CanUngroup == true;
            var selected = _surface?.SelectedElements;
            var next = selected?.Count == 1 ? selected.First() as DrawableContainer : null;
            if (!ReferenceEquals(next, _measuredObject))
            {
                if (_measuredObject != null) _measuredObject.GeometryChanged -= SelectedGeometryChanged;
                _measuredObject = next;
                if (_measuredObject != null) _measuredObject.GeometryChanged += SelectedGeometryChanged;
            }
            _measurementPanel.RefreshDimensions(_measuredObject, _surface?.Measurements ?? MeasurementState.Pixels);
        }

        private void RefreshQuickColors()
        {
            var fields = _surface?.FieldAggregator as FieldAggregator;
            _quickColors?.Refresh(fields?.HasFieldValue(FieldType.LINE_COLOR) == true, fields?.HasFieldValue(FieldType.FILL_COLOR) == true);
        }

        private void CompleteCalibration(object sender, ScaleLineEventArgs e)
        {
            var surface = (Surface)sender;
            if (e.PixelLength <= 0)
            {
                UpdateStatusLabel("Draw a nonzero line to calibrate the image.");
                return;
            }
            using var dialog = new ScaleCalibrationDialog(e.PixelLength, surface.Measurements.Unit);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                surface.SetMeasurements(dialog.Calibration);
                UpdateStatusLabel("Image scale calibrated.");
            }
            FocusCanvas(fromKeyboard: false);
        }
    }
}
