using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Measurements;
using Greenshot.Editor.Memento;
using Greenshot.Editor.Helpers;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Editor.Drawing
{
    public sealed partial class Surface : IMeasurementSurface
    {
        public MeasurementState Measurements { get; private set; } = MeasurementState.Pixels;
        public event EventHandler MeasurementsChanged;
        public event EventHandler<ScaleLineEventArgs> ScaleLineCompleted;
        private LineContainer _scalePreview;
        private bool _scaleCompleting;
        private Point _scalePointer;

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
            _scaleCompleting = false;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (DrawingMode == DrawingModes.Scale) UpdateScalePreview(ModifierKeys);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (DrawingMode == DrawingModes.Scale) UpdateScalePreview(ModifierKeys);
        }

        private void StartScalePreview(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) { DrawingMode = DrawingModes.None; return; }
            if (e.Button != MouseButtons.Left) return;
            if (_scalePreview != null)
            {
                _scaleCompleting = true;
                MoveScalePreview(e);
                return;
            }
            _scalePreview = new LineContainer(this)
            {
                Left = e.X, Top = e.Y, Status = EditStatus.DRAWING
            };
            _scalePreview.HandleMouseDown(e.X, e.Y);
            _scalePointer = e.Location;
            _scaleCompleting = false;
            Focus();
        }

        private void MoveScalePreview(MouseEventArgs e)
        {
            Cursor = Cursors.Cross;
            if (_scalePreview == null) return;
            _scalePointer = e.Location;
            UpdateScalePreview(ModifierKeys);
        }

        private void UpdateScalePreview(Keys modifiers)
        {
            if (_scalePreview == null) return;
            var end = GetScaleEndpoint(new Point(_scalePreview.Left, _scalePreview.Top), _scalePointer, modifiers);
            _scalePreview.Width = end.X - _scalePreview.Left;
            _scalePreview.Height = end.Y - _scalePreview.Top;
            Invalidate();
        }

        // A shared endpoint calculation leaves a single place to compose future object/grid snaps.
        internal static Point GetScaleEndpoint(Point start, Point pointer, Keys modifiers)
        {
            var bounds = new NativeRect(start.X, start.Y, pointer.X - start.X, pointer.Y - start.Y);
            var options = (modifiers & Keys.Shift) != 0 ? ScaleOptions.Rational : ScaleOptions.Default;
            var snapped = ScaleHelper.Scale(bounds, pointer.X, pointer.Y, LineAngleRoundBehavior.INSTANCE, options);
            return new Point(start.X + (int)snapped.Width, start.Y + (int)snapped.Height);
        }

        private void FinishScalePreview(MouseEventArgs e)
        {
            // Releasing the first click only anchors the first point. The second click completes it.
            if (_scalePreview == null || !_scaleCompleting || e.Button != MouseButtons.Left) return;
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
