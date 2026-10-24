using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Measurements;

namespace Greenshot.Editor.Memento
{
    internal sealed class MeasurementChangeMemento : IMemento
    {
        private readonly Surface _surface;
        private readonly MeasurementState _state;
        public MeasurementChangeMemento(Surface surface) { _surface = surface; _state = surface.Measurements; }
        public IMemento Restore()
        {
            var inverse = new MeasurementChangeMemento(_surface);
            _surface.RestoreMeasurements(_state);
            return inverse;
        }
        public bool Merge(IMemento other) => false;
        public void Dispose() { }
    }
}
