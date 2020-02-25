using System.Collections.Generic;
using System.Drawing;
using GreenshotPlugin.Interfaces;

namespace GreenshotPlugin.Core
{
    /// <summary>
    /// A class representing an element in the capture
    /// </summary>
    public class CaptureElement : ICaptureElement {
        public CaptureElement(Rectangle bounds) {
            Bounds = bounds;
        }
        public CaptureElement(string name) {
            Name = name;
        }
        public CaptureElement(string name, Rectangle bounds) {
            Name = name;
            Bounds = bounds;
        }

        public List<ICaptureElement> Children { get; set; } = new List<ICaptureElement>();

        public string Name {
            get;
            set;
        }
        public Rectangle Bounds {
            get;
            set;
        }

        // CaptureElements are regarded equal if their bounds are equal. this should be sufficient.
        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return obj is CaptureElement other && Bounds.Equals(other.Bounds);
        }

        public override int GetHashCode() {
            // TODO: Fix this, this is not right...
            return Bounds.GetHashCode();
        }
    }
}