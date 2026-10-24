using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Measurements;
using Greenshot.Editor.Memento;

namespace Greenshot.Editor.Drawing
{
    public sealed partial class Surface : IMeasurementSurface
    {
        public MeasurementState Measurements { get; private set; } = MeasurementState.Pixels;
        public event EventHandler MeasurementsChanged;
        public event EventHandler<ScaleLineEventArgs> ScaleLineCompleted;
        private LineContainer _scalePreview;

        public void SetMeasurements(MeasurementState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (Measurements.MetresPerPixel == state.MetresPerPixel && Measurements.Unit == state.Unit) return;
            MakeUndoable(new MeasurementChangeMemento(this), false);
            RestoreMeasurements(state);
            Modified = true;
        }

        public void RestoreMeasurements(MeasurementState state)
        {
            Measurements = state ?? MeasurementState.Pixels;
            MeasurementsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TransformMeasurements(Matrix matrix)
        {
            bool calibrated = Measurements.IsCalibrated;
            RestoreMeasurements(Measurements.Transform(matrix));
            if (calibrated && !Measurements.IsCalibrated)
                SendMessageEvent(this, SurfaceMessageTyp.Info, "Scale cleared after nonuniform image resizing. Recalibrate to measure real lengths.");
        }

        internal void CancelScalePreview()
        {
            if (_scalePreview == null) return;
            _scalePreview.Dispose();
            _scalePreview = null;
            Capture = false;
            Invalidate();
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture) CancelScalePreview();
        }

        private void StartScalePreview(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            CancelScalePreview();
            _scalePreview = new LineContainer(this)
            {
                Left = e.X, Top = e.Y, Status = EditStatus.DRAWING
            };
            _scalePreview.HandleMouseDown(e.X, e.Y);
            Capture = true;
            Focus();
        }

        private void MoveScalePreview(MouseEventArgs e)
        {
            Cursor = Cursors.Cross;
            if (_scalePreview == null) return;
            // Use exact endpoints, independent of normal tool modifier/rounding behaviours.
            _scalePreview.Width = e.X - _scalePreview.Left;
            _scalePreview.Height = e.Y - _scalePreview.Top;
            Invalidate();
        }

        private void FinishScalePreview(MouseEventArgs e)
        {
            if (_scalePreview == null || e.Button != MouseButtons.Left) return;
            MoveScalePreview(e);
            double length = MeasurementCalculator.LineLength(_scalePreview.Width, _scalePreview.Height);
            CancelScalePreview();
            DrawingMode = DrawingModes.None;
            ScaleLineCompleted?.Invoke(this, new ScaleLineEventArgs(length));
        }

        private void PaintScalePreview(Graphics graphics)
        {
            if (_scalePreview == null) return;
            var saved = graphics.Save();
            graphics.ScaleTransform(_zoomFactor, _zoomFactor);
            using var outline = new Pen(Color.White, 3);
            using var line = new Pen(Color.Black, 1) { DashStyle = DashStyle.Dash };
            var start = new Point(_scalePreview.Left, _scalePreview.Top);
            var end = new Point(start.X + _scalePreview.Width, start.Y + _scalePreview.Height);
            graphics.DrawLine(outline, start, end);
            graphics.DrawLine(line, start, end);
            graphics.Restore(saved);
        }
    }

    public sealed class ScaleLineEventArgs : EventArgs
    {
        public double PixelLength { get; }
        public ScaleLineEventArgs(double pixelLength) { PixelLength = pixelLength; }
    }
}
