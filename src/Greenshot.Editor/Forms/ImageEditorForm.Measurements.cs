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
        private ToolStripButton _scaleButton;
        private DrawableContainer _measuredObject;

        private void InitializeMeasurements()
        {
            _measurementPanel = new MeasurementPanel();
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayoutPanel1.Controls.Add(_measurementPanel, 1, 0);
            _measurementPanel.CalibrateRequested += (s, e) => BeginCalibration();
            _measurementPanel.ClearRequested += (s, e) => _surface?.SetMeasurements(MeasurementState.Pixels);
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
                ToolTipText = "Scale: draw a line of known length", AccessibleName = "Scale", Image = CreateScaleIcon()
            };
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

        private static Bitmap CreateScaleIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bitmap);
            g.FillRectangle(Brushes.LightGoldenrodYellow, 1, 4, 14, 8);
            g.DrawRectangle(Pens.DimGray, 1, 4, 14, 8);
            for (int x = 3; x < 15; x += 3) g.DrawLine(Pens.DimGray, x, 4, x, x % 2 == 0 ? 9 : 7);
            return bitmap;
        }

        private void BeginCalibration()
        {
            if (_surface == null || _controlsDisabledDueToConfirmable) return;
            _measurementPanel.SetExpanded(true);
            _surface.DrawingMode = DrawingModes.Scale;
            UpdateStatusLabel("Draw a line between two points with a known distance.");
            FocusCanvas(fromKeyboard: false);
        }

        private void BindMeasurements()
        {
            _surface.MeasurementsChanged += SelectedGeometryChanged;
            _surface.ScaleLineCompleted += CompleteCalibration;
            UpdateMeasurements();
        }

        private void UnbindMeasurements()
        {
            _surface.MeasurementsChanged -= SelectedGeometryChanged;
            _surface.ScaleLineCompleted -= CompleteCalibration;
            _surface.CancelScalePreview();
            if (_measuredObject != null) _measuredObject.GeometryChanged -= SelectedGeometryChanged;
            _measuredObject = null;
            _measurementPanel.RefreshDimensions(null, MeasurementState.Pixels);
        }

        private void SelectedGeometryChanged(object sender, EventArgs e) => UpdateMeasurements();

        private void UpdateMeasurements()
        {
            if (_measurementPanel == null) return;
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
