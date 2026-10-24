using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Adorners;
using Greenshot.Editor.Forms;
using Greenshot.Editor.Helpers;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class ObjectSnappingTests
    {
        public ObjectSnappingTests() => TestEnvironment.EnsureInitialized();
        private static Point Snap(Point p, Point? anchor, PointF[] targets, bool points = true, bool through = false, double zoom = 1, Func<Point, bool> compatible = null)
            => ObjectSnapHelper.Snap(p, anchor, targets, points, through, zoom, compatible, out _, out _);

        [Fact]
        public void Targets_IncludeSignedLineAndNormalizedShapes()
        {
            using var surface = new Surface();
            var line = new LineContainer(surface) { Left = 30, Top = 40, Width = -20, Height = -30 };
            Assert.Equal(new[] { new PointF(30,40), new PointF(10,10), new PointF(20,25) }, ObjectSnapHelper.Targets(line));
            var arrow = new ArrowContainer(surface) { Left = 30, Top = 40, Width = -20, Height = -30 };
            Assert.Equal(ObjectSnapHelper.Targets(line), ObjectSnapHelper.Targets(arrow));
            var rect = new RectangleContainer(surface) { Left = 30, Top = 40, Width = -20, Height = -30 };
            Assert.Equal(9, ObjectSnapHelper.Targets(rect).Count());
            Assert.Contains(new PointF(10, 10), ObjectSnapHelper.Targets(rect));
            Assert.Contains(new PointF(20, 25), ObjectSnapHelper.Targets(rect));
            var ellipse = new EllipseContainer(surface) { Left = 30, Top = 40, Width = -20, Height = -30 };
            Assert.Equal(new[] { new PointF(20,10), new PointF(30,25), new PointF(20,40), new PointF(10,25), new PointF(20,25) }, ObjectSnapHelper.Targets(ellipse));
        }
        [Fact]
        public void PointSnap_IsOptionalNearestStableAndZoomAware()
        {
            var targets = new[] { new PointF(10, 10), new PointF(14, 10) };
            Assert.Equal(new Point(13,12), Snap(new Point(13,12), null, targets, false));
            Assert.Equal(new Point(14,10), Snap(new Point(13,12), null, targets));
            Assert.Equal(new Point(10,10), Snap(new Point(12,10), null, targets));
            Assert.Equal(new Point(20,10), Snap(new Point(20,10), null, targets, zoom: 2));
            Assert.Equal(new Point(14,10), Snap(new Point(20,10), null, targets, zoom: .5));
        }
        [Fact]
        public void Through_ProjectsBeyondTargetAndRejectsBehindOrTooShort()
        {
            var targets = new[] { new PointF(10, 10) };
            Assert.Equal(new Point(30,30), Snap(new Point(32,28), Point.Empty, targets, false, true));
            Assert.Equal(new Point(5,4), Snap(new Point(5,4), Point.Empty, targets, false, true));
            Assert.Equal(new Point(-30,-29), Snap(new Point(-30,-29), Point.Empty, targets, false, true));
        }
        [Fact]
        public void PointWinsAndIncompatibleConstraintsAreSkipped()
        {
            var targets = new[] { new PointF(30,29), new PointF(10,10) };
            Assert.Equal(new Point(30,29), Snap(new Point(32,28), Point.Empty, targets, true, true));
            Assert.Equal(new Point(32,28), Snap(new Point(32,28), Point.Empty, targets, true, true, compatible: p => p.Y == 0));
            Assert.Equal(new Point(30,0), Snap(new Point(32,0), Point.Empty, new[] { new PointF(30,0), new PointF(32,2) }, compatible: p => p.Y == 0));
        }
        [Fact]
        public void DrawingHandlesAndExclusion_UseSameTargetsWithoutNumericSnapping()
        {
            RunOnSta(() =>
            {
                using var surface = new Surface(new Bitmap(300,300));
                var target = new RectangleContainer(surface) { Left=100, Top=100, Width=40, Height=40 };
                surface.AddElement(target);
                var line = new LineContainer(surface);
                surface.AddElement(line);
                surface.SetObjectSnapping(true,true);
                line.HandleMouseDown(102,102);
                Assert.Equal(100, line.Left); Assert.Equal(100, line.Top);
                line.HandleMouseMove(137,137);
                Assert.Equal(40,line.Width); Assert.Equal(40,line.Height);
                line.Width = 37; Assert.Equal(37,line.Width);
                var handle = new MoveAdorner(line, Positions.BottomRight);
                handle.MouseDown(surface,new MouseEventArgs(MouseButtons.Left,1,137,137,0));
                handle.MouseMove(surface,new MouseEventArgs(MouseButtons.Left,1,141,139,0));
                Assert.Equal(40,line.Width); Assert.Equal(40,line.Height);
                handle.MouseUp(surface,new MouseEventArgs(MouseButtons.Left,1,141,139,0));
                Assert.Equal(new Point(177,177),surface.SnapGesture(new Point(177,177),null,line,false));
                line.Left=200; line.Top=200;
                Assert.Equal(new Point(201,201),surface.SnapGesture(new Point(201,201),null,line,false));
                surface.SetObjectSnapping(false,false);
                Assert.Equal(new Point(102,102),surface.SnapGesture(new Point(102,102),null,line,false));
            });
        }
        [Fact]
        public void Toolbar_HostsIndependentUncheckedControlsOnFirstRow()
        {
            RunOnSta(() =>
            {
                using var surface = new Surface(new Bitmap(300,300));
                using var editor = new ImageEditorForm(surface,true,false);
                var toolbar = (ToolStrip)typeof(ImageEditorForm).GetField("destinationsToolStrip", BindingFlags.Instance|BindingFlags.NonPublic).GetValue(editor);
                var checks=toolbar.Items.OfType<ToolStripControlHost>().Select(h=>h.Control).OfType<CheckBox>().ToArray();
                Assert.Equal(new[] { "Snap to points", "Snap through" }, checks.Select(c=>c.Text));
                Assert.All(checks,c=>Assert.False(c.Checked));
                checks[0].Checked=true; Assert.False(checks[1].Checked);
                surface.DeselectAllElements();
                Assert.All(toolbar.Items.OfType<ToolStripControlHost>(), host => Assert.True(host.Available));
                editor.Size = new Size(1200,700); editor.Show(); Application.DoEvents();
                Assert.All(checks,c=>Assert.True(c.Visible));
                var target=new RectangleContainer(surface){Left=100,Top=100,Width=40,Height=40};surface.AddElement(target);
                Assert.Equal(new Point(100,100),surface.SnapGesture(new Point(102,102),null,null,false));
                using var secondSurface = new Surface(new Bitmap(300,300));
                secondSurface.AddElement(new EllipseContainer(secondSurface){Left=100,Top=100,Width=40,Height=40});
                editor.AddTab(secondSurface,true,false);
                Assert.Equal(new Point(100,120),secondSurface.SnapGesture(new Point(102,120),null,null,false));
                Assert.Null(typeof(Surface).GetField("_snapTarget",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(surface));
                surface.SnapGesture(new Point(102,102),null,null,false);
                using var baseline=surface.GetImageForExport();
                surface.ClearSnapGuide();
                using var cleared=surface.GetImageForExport();
                Assert.Equal(((Bitmap)baseline).GetPixel(100,100),((Bitmap)cleared).GetPixel(100,100));
            });
        }
        [Fact]
        public void Calibration_ThroughPreviewAndCompletionUseSnappedGeometry()
        {
            RunOnSta(() =>
            {
                using var surface = new Surface(new Bitmap(300,300));
                surface.AddElement(new LineContainer(surface){Left=50,Top=50,Width=20,Height=0});
                surface.SetObjectSnapping(false,true); surface.DrawingMode=DrawingModes.Scale;
                double length=0; surface.ScaleLineCompleted+=(s,e)=>length=e.PixelLength;
                Mouse(surface,"SurfaceMouseDown",0,0);Mouse(surface,"SurfaceMouseUp",0,0);
                Mouse(surface,"SurfaceMouseMove",102,98);
                var preview=(LineContainer)typeof(Surface).GetField("_scalePreview",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(surface);
                Assert.Equal(100,preview.Width);Assert.Equal(100,preview.Height);
                Mouse(surface,"SurfaceMouseDown",102,98);Mouse(surface,"SurfaceMouseUp",102,98);
                Assert.Equal(Math.Sqrt(20000),length,5);
                Assert.Null(typeof(Surface).GetField("_snapTarget",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(surface));
                Assert.Single(surface.Elements);
            });
        }
        [Fact]
        public void MovingSelection_ExcludesAllMembersAndPreservesOffsets()
        {
            RunOnSta(() =>
            {
                using var surface = new Surface(new Bitmap(700,700));
                var first=new RectangleContainer(surface){Left=10,Top=10,Width=100,Height=100};
                var second=new EllipseContainer(surface){Left=200,Top=10,Width=100,Height=100};
                surface.AddElement(first);surface.AddElement(second);
                surface.AddElement(new LineContainer(surface){Left=400,Top=400,Width=10,Height=0});
                surface.SelectElement(first);surface.SelectElement(second);
                surface.SetObjectSnapping(true,true);
                typeof(Surface).GetField("_mouseDownElement",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(surface,first);
                typeof(Surface).GetField("_mouseDown",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(surface,true);
                typeof(Surface).GetField("_mouseStart",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(surface,new Point(40,40));
                Mouse(surface,"SurfaceMouseMove",41,41);
                Assert.Equal(11,first.Left);Assert.Equal(201,second.Left);Assert.Equal(11,second.Top);
                Mouse(surface,"SurfaceMouseMove",329,329);
                Assert.Equal(300,first.Left);Assert.Equal(490,second.Left);Assert.Equal(300,first.Top);
                Assert.Equal(100,first.Width);Assert.Equal(100,second.Height);
                surface.Undo();Assert.Equal(10,first.Left);Assert.Equal(200,second.Left);
            });
        }
        [Fact]
        public void ResizeHandlesAndShiftDrawing_PreserveExplicitConstraints()
        {
            RunOnSta(() =>
            {
                using var surface=new Surface(new Bitmap(300,300));
                var target=new LineContainer(surface){Left=100,Top=100,Width=20,Height=0};surface.AddElement(target);
                var rectangle=new RectangleContainer(surface){Left=10,Top=10,Width=40,Height=40};surface.AddElement(rectangle);
                surface.SetObjectSnapping(true,true);
                var handle=new ResizeAdorner(rectangle,Positions.BottomRight);
                handle.MouseDown(surface,new MouseEventArgs(MouseButtons.Left,1,50,50,0));
                handle.MouseMove(surface,new MouseEventArgs(MouseButtons.Left,1,102,102,0));
                Assert.Equal(90,rectangle.Width);Assert.Equal(90,rectangle.Height);
                handle.MouseUp(surface,new MouseEventArgs(MouseButtons.Left,1,102,102,0));
                var original=new Dapplo.Windows.Common.Structs.NativeRect(0,0,102,3);
                var line=new LineContainer(surface);surface.AddElement(line);
                var baseline=ScaleHelper.Scale(original,102,3,LineAngleRoundBehavior.INSTANCE,ScaleOptions.Rational);
                var result=surface.SnapDrawingBounds(line,original,new Point(102,3),baseline,LineAngleRoundBehavior.INSTANCE,ScaleOptions.Rational);
                Assert.Equal(baseline.Width,result.Width);Assert.Equal(0,result.Height);
                target.Top=0;
                result=surface.SnapDrawingBounds(line,original,new Point(102,3),baseline,LineAngleRoundBehavior.INSTANCE,ScaleOptions.Rational);
                Assert.Equal(100,result.Width);Assert.Equal(0,result.Height);
            });
        }
        private static void Mouse(Surface surface,string method,int x,int y) => typeof(Surface).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)
            .Invoke(surface,new object[]{surface,new MouseEventArgs(MouseButtons.Left,1,x,y,0)});
        private static void RunOnSta(Action action)
        {
            Exception error=null;var thread=new Thread(()=>{try{action();}catch(Exception e){error=e;}});
            thread.SetApartmentState(ApartmentState.STA);thread.Start();Assert.True(thread.Join(TimeSpan.FromSeconds(30)),"UI test timed out");if(error!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
