using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace GreenshotPlugin.UnmanagedHelpers.Structs
{
    [StructLayout(LayoutKind.Sequential), Serializable()]
    public struct POINT {
        public int X;
        public int Y;

        public POINT(int x, int y) {
            X = x;
            Y = y;
        }
        public POINT(Point point) {
            X = point.X;
            Y = point.Y;
        }

        public static implicit operator Point(POINT p) {
            return new Point(p.X, p.Y);
        }

        public static implicit operator POINT(Point p) {
            return new POINT(p.X, p.Y);
        }

        public Point ToPoint() {
            return new Point(X, Y);
        }

        public override string ToString() {
            return X + "," + Y;
        }
    }
}