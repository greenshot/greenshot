using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormatHandlers;
using Greenshot.Editor.Measurements;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class DimensionTests
    {
        public DimensionTests() => TestEnvironment.EnsureInitialized();
        private static T Add<T>(Surface s, T shape) where T : DrawableContainer
        {
            if (!(shape is PolygonContainer)) shape.SetFieldValue(FieldType.SHADOW, false);
            shape.SetFieldValue(FieldType.LINE_THICKNESS, 2);
            shape.SetFieldValue(FieldType.LINE_COLOR, Color.Navy);
            s.AddElement(shape, false); return shape;
        }
        private static void Show(DrawableContainer shape)
        { foreach (var edge in DimensionLayout.Edges(shape)) shape.SetDimensionVisible(edge.Index, true); }

        [Fact]
        public void Visibility_DefaultHiddenIndependentAndUndoableWithNoOp()
        {
            using var s = new Surface(new Bitmap(400, 300));
            var box = Add(s, new RectangleContainer(s) { Left=100,Top=100,Width=160,Height=80 });
            Assert.Empty(DimensionLayout.Labels(box)); Assert.Equal(4, DimensionLayout.Edges(box).Length);
            Assert.True(box.SetDimensionVisible(2,true)); Assert.False(box.SetDimensionVisible(2,true));
            Assert.Equal(2, Assert.Single(DimensionLayout.Labels(box)).Edge.Index);
            var flags=box.DimensionVisibility; flags[2]=false; Assert.True(box.IsDimensionVisible(2));
            s.Undo(); Assert.Empty(DimensionLayout.Labels(box)); s.Redo(); Assert.Single(DimensionLayout.Labels(box));
            Assert.Empty(DimensionLayout.Edges(new EllipseContainer(s)));
        }

        [Fact]
        public void ActualTransformedLengthsAndReadableExteriorLabels()
        {
            using var s=new Surface(new Bitmap(800,600));
            var box=Add(s,new RectangleContainer(s){Left=200,Top=200,Width=120,Height=60}); Show(box);
            s.SelectElement(box); s.RotateSelectedObjects(37);
            Assert.Equal(120,DimensionLayout.Edges(box)[0].Length,3);
            using var stretch=new Matrix(2,0,0,-1,0,600); box.Transform(stretch);
            var edges=DimensionLayout.Edges(box); var labels=DimensionLayout.Labels(box);
            Assert.Equal(4,labels.Length); Assert.All(labels,l=>Assert.InRange(l.Angle,-90,90));
            foreach(var edge in edges)
                Assert.Equal(Math.Sqrt(Math.Pow(edge.B.X-edge.A.X,2)+Math.Pow(edge.B.Y-edge.A.Y,2)),edge.Length,5);
            Assert.All(labels,l=>Assert.False(box.ClickableAt((int)l.Center.X,(int)l.Center.Y)));
            Assert.Equal(120,box.Width);
        }

        [Fact]
        public void ConcaveWindingReflectionAndZeroEdgesRemainStable()
        {
            using var s=new Surface(new Bitmap(700,600));
            var polygon=Add(s,new PolygonContainer(s));
            polygon.SetVertices(new[]{new Point(100,100),new Point(400,100),new Point(400,350),new Point(250,200),new Point(100,350)}); Show(polygon);
            Assert.Equal(5,DimensionLayout.Labels(polygon).Length);
            Assert.All(DimensionLayout.Labels(polygon),l=>Assert.False(polygon.ClickableAt((int)l.Center.X,(int)l.Center.Y)));
            polygon.SetVertices(polygon.Vertices.AsEnumerable().Reverse().ToArray());
            using var reflection=new Matrix(-1,0,0,1,600,0); polygon.Transform(reflection);
            Assert.All(DimensionLayout.Labels(polygon),l=>Assert.InRange(l.Angle,-90,90));
            polygon.SetVertices(new[]{new Point(100,100),new Point(100,100),new Point(200,200)});
            Assert.True(polygon.IsDimensionVisible(0)); Assert.Equal(0,DimensionLayout.Edges(polygon)[0].Length);
            Assert.DoesNotContain(DimensionLayout.Labels(polygon),l=>l.Edge.Index==0);
        }

        [Fact]
        public void CalibrationStyleAndMovementRefreshDerivedLabelsWithUndo()
        {
            using var s=new Surface(new Bitmap(500,300)); using var other=new Surface();
            var line=Add(s,new LineContainer(s){Left=100,Top=100,Width=100,Height=0}); Show(line);
            Assert.Equal("100 px",Assert.Single(DimensionLayout.Labels(line)).Text);
            s.SetMeasurements(MeasurementState.Calibrate(100,2,MeasurementUnit.Metres));
            Assert.Equal("2 m",Assert.Single(DimensionLayout.Labels(line)).Text);
            s.Undo(); Assert.Equal("100 px",Assert.Single(DimensionLayout.Labels(line)).Text); s.Redo();
            var style=new DimensionStyle("Arial",18,FontStyle.Bold,Color.Red); s.SetDimensionStyle(style);
            Assert.Equal(DimensionStyle.Default,other.DimensionStyle); s.Undo(); Assert.Equal(DimensionStyle.Default,s.DimensionStyle); s.Redo();
            Assert.Equal(style,s.DimensionStyle); Assert.False(s.SetDimensionStyle(style));
            var center=Assert.Single(DimensionLayout.Labels(line)).Center;
            line.MakeBoundsChangeUndoable(false); line.MoveBy(17,23);
            Assert.Equal(center.X+17,Assert.Single(DimensionLayout.Labels(line)).Center.X); s.Undo(); Assert.Equal(center,Assert.Single(DimensionLayout.Labels(line)).Center);
            Assert.DoesNotContain("0 px",DimensionLayout.FormatLength(.0000001,MeasurementState.Pixels));
        }

        [Fact]
        public void ProjectStyleAndFlagsRoundTripAndLegacyCalibrationTrailerStaysLast()
        {
            using var s=new Surface(new Bitmap(500,300)); var box=Add(s,new RectangleContainer(s){Left=100,Top=100,Width=160,Height=80}); Show(box);
            s.SetDimensionStyle(new DimensionStyle("Arial",14,FontStyle.Italic,Color.DarkGreen));
            s.SetMeasurements(MeasurementState.Calibrate(100,2,MeasurementUnit.Centimetres));
            var handler=new GreenshotFileFormatHandler(); using var bytes=new MemoryStream();
            Assert.True(handler.TrySaveToStream((Bitmap)s.Image,bytes,".greenshot",s));
            var saved=bytes.ToArray(); using var input=new MemoryStream(saved); using var loaded=(Surface)handler.LoadSurface(input);
            Assert.Equal(s.DimensionStyle,loaded.DimensionStyle); Assert.Equal(box.DimensionVisibility,Assert.IsType<RectangleContainer>(Assert.Single(loaded.Elements)).DimensionVisibility);
            using var trailer=new MemoryStream(saved); trailer.Position=trailer.Length-22;
            using var reader=new BinaryReader(trailer,Encoding.UTF8,true); long size=reader.ReadInt64(); long start=trailer.Length-22-size;
            Assert.Equal(s.Measurements.MetresPerPixel,MeasurementMetadata.Read(trailer,start,size).MetresPerPixel);
        }

        [Theory]
        [InlineData("legacy")]
        [InlineData("version")]
        [InlineData("length")]
        [InlineData("font")]
        public void OptionalMetadataMalformedOrUnknownDefaultsWithoutLosingObjects(string kind)
        {
            using var s=new Surface(new Bitmap(100,100)); Add(s,new LineContainer(s){Width=60});
            using var payload=new MemoryStream(); s.SaveElementsToStream(payload); long modelEnd=payload.Position;
            if(kind!="legacy") DimensionMetadata.Write(payload,new DimensionStyle("Arial",12,FontStyle.Regular,Color.Red));
            if(kind=="version") { payload.Position=modelEnd; new BinaryWriter(payload,Encoding.UTF8,true).Write(99); }
            if(kind=="font") { payload.Position=modelEnd+6+5; new BinaryWriter(payload,Encoding.UTF8,true).Write(float.NaN); }
            if(kind=="length") { payload.Position=payload.Length-12; new BinaryWriter(payload,Encoding.UTF8,true).Write(int.MaxValue); }
            payload.Position=payload.Length; MeasurementMetadata.Write(payload,MeasurementState.Pixels,true);
            payload.Position=3; Assert.Equal(DimensionStyle.Default,DimensionMetadata.Read(payload,0,payload.Length)); Assert.Equal(3,payload.Position);
            using var project=new MemoryStream(); s.Image.Save(project,ImageFormat.Png); payload.Position=0; payload.CopyTo(project);
            using(var writer=new BinaryWriter(project,Encoding.UTF8,true)){writer.Write(payload.Length); writer.Write(Encoding.ASCII.GetBytes("Greenshot01.00"));}
            project.Position=0; using var loaded=(Surface)new GreenshotFileFormatHandler().LoadSurface(project);
            Assert.Single(loaded.Elements); Assert.Equal(DimensionStyle.Default,loaded.DimensionStyle);
        }

        [Fact]
        public void ClipboardFlagsAreIndependentAndUseDestinationUnitsAndStyle()
        {
            using var source=new Surface(new Bitmap(400,300)); using var target=new Surface(new Bitmap(400,300));
            var line=Add(source,new LineContainer(source){Left=100,Top=100,Width=100,Height=0}); Show(line);
            var list=DrawableContainerClipboard.Deserialize(DrawableContainerClipboard.Serialize(new DrawableContainerList{line}));
            var pasted=Assert.IsType<LineContainer>(Assert.Single(list)); target.AddElement(pasted,false);
            target.SetMeasurements(MeasurementState.Calibrate(100,5,MeasurementUnit.Feet));
            target.SetDimensionStyle(new DimensionStyle("Arial",24,FontStyle.Bold,Color.Red));
            Assert.Equal("5 ft",Assert.Single(DimensionLayout.Labels(pasted)).Text);
            Assert.True(Assert.Single(DimensionLayout.Labels(pasted)).Height>Assert.Single(DimensionLayout.Labels(line)).Height);
            pasted.SetDimensionVisible(0,false); Assert.True(line.IsDimensionVisible(0));
        }

        [Fact]
        public void ArrowGapPreservesScreenshotHeadsAndShadowAndShortArrowFallsBack()
        {
            using var s=new Surface(new Bitmap(500,260)); using(var g=Graphics.FromImage(s.Image))g.Clear(Color.Gold);
            var arrow=Add(s,new ArrowContainer(s){Left=50,Top=100,Width=350,Height=0});
            arrow.SetFieldValue(FieldType.SHADOW,true); arrow.SetFieldValue(FieldType.LINE_THICKNESS,4);
            arrow.SetFieldValue(FieldType.ARROWHEADS,ArrowContainer.ArrowHeadCombination.BOTH);
            using var before=(Bitmap)s.GetImageForExport(); Show(arrow); var label=Assert.Single(DimensionLayout.Labels(arrow)); Assert.True(label.ShaftGap);
            using var after=(Bitmap)s.GetImageForExport();
            int gapEdge=(int)(label.Center.X-label.Width/2-2);
            Assert.Equal(Color.Gold.ToArgb(),after.GetPixel(gapEdge,100).ToArgb());
            Assert.NotEqual(before.GetPixel(gapEdge,100).ToArgb(),after.GetPixel(gapEdge,100).ToArgb());
            for(int y=80;y<125;y++)for(int x=40;x<90;x++)Assert.Equal(before.GetPixel(x,y),after.GetPixel(x,y));
            arrow.SetDimensionVisible(0,false);
            using(var hidden=(Bitmap)s.GetImageForExport())
                for(int y=0;y<hidden.Height;y++)for(int x=0;x<hidden.Width;x++)Assert.Equal(before.GetPixel(x,y),hidden.GetPixel(x,y));
            var shortArrow=Add(s,new ArrowContainer(s){Left=100,Top=200,Width=35,Height=0}); Show(shortArrow);
            Assert.False(Assert.Single(DimensionLayout.Labels(shortArrow)).ShaftGap); Assert.Null(DimensionLayout.ArrowGap(shortArrow));
        }

        [Fact]
        public void WholeCanvasTransformAndUndoPreserveFlagsAndFixedTextSize()
        {
            using var s=new Surface(new Bitmap(400,300));
            var box=Add(s,new RectangleContainer(s){Left=100,Top=100,Width=120,Height=60}); Show(box);
            var before=DimensionLayout.Labels(box); var flags=box.DimensionVisibility;
            var apply=typeof(Surface).GetMethod("ApplyEffectResult",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(Image),typeof(Matrix)},null);
            using var stretch=new Matrix(2,0,0,1,0,0); apply.Invoke(s,new object[]{new Bitmap(800,300),stretch});
            Assert.Equal(240,DimensionLayout.Edges(box)[0].Length,3);
            Assert.Equal(before[0].Height,DimensionLayout.Labels(box)[0].Height); Assert.Equal(flags,box.DimensionVisibility);
            s.Undo(); Assert.Equal(120,DimensionLayout.Edges(box)[0].Length,3); Assert.Equal(flags,box.DimensionVisibility);
            s.Redo(); Assert.Equal(240,DimensionLayout.Edges(box)[0].Length,3);
        }

        [Fact]
        public void ExportBoundsIncludeLabelsButGeometryAndTargetsDoNotAndWritePreview()
        {
            using var s=new Surface(new Bitmap(1000,700)); using(var g=Graphics.FromImage(s.Image))g.Clear(Color.WhiteSmoke);
            var box=Add(s,new RectangleContainer(s){Left=100,Top=100,Width=220,Height=130}); Show(box);
            Assert.True(box.DrawingBounds.Top<box.Bounds.Top); var bounds=box.Bounds;
            var polygon=Add(s,new PolygonContainer(s)); polygon.SetVertices(new[]{new Point(520,120),new Point(850,120),new Point(850,330),new Point(680,230),new Point(520,330)}); Show(polygon);
            Show(Add(s,new LineContainer(s){Left=100,Top=350,Width=260,Height=60}));
            Show(Add(s,new ArrowContainer(s){Left=100,Top=530,Width=340,Height=0}));
            Show(Add(s,new ArrowContainer(s){Left=550,Top=520,Width=40,Height=0}));
            var rotated=Add(s,new RectangleContainer(s){Left=740,Top=480,Width=160,Height=90}); Show(rotated); s.SelectElement(rotated); s.RotateSelectedObjects(31);
            using var export=(Bitmap)s.GetImageForExport(); Assert.Equal(1000,export.Width); Assert.Equal(bounds,box.Bounds);
            string path=Path.Combine(Path.GetTempPath(),"GreenshotPlus-dimensions-preview.png"); export.Save(path,ImageFormat.Png);
            Assert.True(File.Exists(path));
        }
    }
}
