using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Measurements
{
    internal sealed class DimensionEdge
    {
        internal int Index;
        internal PointF A, B;
        internal double Length => MeasurementCalculator.LineLength(B.X - A.X, B.Y - A.Y);
        internal string Name => "Edge " + (Index + 1);
    }
    internal sealed class DimensionLabel
    {
        internal DimensionEdge Edge;
        internal string Text;
        internal PointF Center;
        internal int Width, Height;
        internal float Angle;
        internal bool ShaftGap, Crowded;
        internal PointF[] Corners(float extraWidth = 0, float extraHeight = 0)
        {
            double a = Angle * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
            float w = (Width + extraWidth) / 2, h = (Height + extraHeight) / 2;
            return new[] { new PointF(-w,-h), new PointF(w,-h), new PointF(w,h), new PointF(-w,h) }
                .Select(p => new PointF(Center.X + (float)(p.X*c-p.Y*s), Center.Y + (float)(p.X*s+p.Y*c))).ToArray();
        }
        internal NativeRect Bounds
        {
            get
            {
                var p = Corners(4,4); int x = (int)Math.Floor(p.Min(v => v.X)), y = (int)Math.Floor(p.Min(v => v.Y));
                return new NativeRect(x, y, (int)Math.Ceiling(p.Max(v => v.X))-x, (int)Math.Ceiling(p.Max(v => v.Y))-y);
            }
        }
    }
    internal static class DimensionLayout
    {
        internal static DimensionEdge[] Edges(DrawableContainer owner)
        {
            if (owner == null) return Array.Empty<DimensionEdge>();
            PointF[] points; bool closed;
            if (owner is PolygonContainer polygon) { points = polygon.ImageVertices; closed = true; }
            else if (owner.GetType() == typeof(RectangleContainer))
            {
                points = new[] { new PointF(owner.Left,owner.Top), new PointF(owner.Left+owner.Width,owner.Top),
                    new PointF(owner.Left+owner.Width,owner.Top+owner.Height), new PointF(owner.Left,owner.Top+owner.Height) }.Select(owner.ModelToImage).ToArray(); closed = true;
            }
            else if (MeasurementGeometry.IsLine(owner))
            { points = new[] { owner.ModelToImage(new PointF(owner.Left,owner.Top)), owner.ModelToImage(new PointF(owner.Left+owner.Width,owner.Top+owner.Height)) }; closed = false; }
            else return Array.Empty<DimensionEdge>();
            if (points.Length < 2) return Array.Empty<DimensionEdge>();
            return Enumerable.Range(0, closed ? points.Length : 1).Select(i => new DimensionEdge { Index=i,A=points[i],B=points[(i+1)%points.Length] }).ToArray();
        }
        internal static string FormatLength(double pixels, MeasurementState state, DimensionFormat format = null)
        {
            string number = (format ?? DimensionFormat.Automatic).Format(state.Convert(pixels));
            return number == "Out of range" ? number : number + " " + MeasurementState.Symbol(state.Unit);
        }
        private static StringFormat TextFormat() => new StringFormat(StringFormat.GenericTypographic)
        { Alignment=StringAlignment.Center, LineAlignment=StringAlignment.Center, FormatFlags=StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces };

        internal static DimensionLabel[] Labels(DrawableContainer owner)
        {
            if (!owner.HasVisibleDimensions) return Array.Empty<DimensionLabel>();
            var surface = owner.Parent as Surface; var style = surface?.DimensionStyle ?? DimensionStyle.Default;
            var units = surface?.Measurements ?? MeasurementState.Pixels;
            var edges = Edges(owner); bool closed = !MeasurementGeometry.IsLine(owner);
            double signed = edges.Sum(e => (double)e.A.X*e.B.Y-(double)e.B.X*e.A.Y);
            using var bitmap = new Bitmap(1,1); bitmap.SetResolution(96,96);
            using var graphics = Graphics.FromImage(bitmap); using var font = style.CreateFont(); using var format = TextFormat();
            using var outline = new GraphicsPath(); if (closed && edges.Length >= 3) outline.AddPolygon(edges.Select(e=>e.A).ToArray());
            var result = new List<DimensionLabel>();
            foreach (var edge in edges.Where(e => owner.IsDimensionVisible(e.Index) && e.Length > 0))
            {
                string text = FormatLength(edge.Length,units,owner.GetDimensionFormat(edge.Index) ?? style.Format); var size = graphics.MeasureString(text,font,int.MaxValue,format);
                var label = new DimensionLabel { Edge=edge,Text=text,Width=(int)Math.Ceiling(size.Width)+4,Height=(int)Math.Ceiling(size.Height)+2 };
                double ux=(edge.B.X-edge.A.X)/edge.Length, uy=(edge.B.Y-edge.A.Y)/edge.Length;
                float angle=(float)(Math.Atan2(uy,ux)*180/Math.PI); if(angle>90)angle-=180; if(angle < -90)angle+=180; label.Angle=angle;
                var midpoint=new PointF((edge.A.X+edge.B.X)/2,(edge.A.Y+edge.B.Y)/2);
                double nx=uy, ny=-ux;
                if (closed && signed < 0 || (!closed || signed == 0) && (ny>0 || ny==0 && nx>0)) { nx=-nx; ny=-ny; }
                double stroke=owner.GetFieldValueAsInt(FieldType.LINE_THICKNESS)*WorldScale(owner);
                if (owner is ArrowContainer)
                {
                    var heads=(ArrowContainer.ArrowHeadCombination)owner.GetFieldValue(FieldType.ARROWHEADS);
                    double protection=(heads==ArrowContainer.ArrowHeadCombination.NONE ? 0 : 7*stroke)+4;
                    if ((label.Width+8)/2d+protection < edge.Length/2)
                    { label.Center=midpoint; label.ShaftGap=true; result.Add(label); continue; }
                }
                double best=double.MaxValue; PointF bestPoint=midpoint; bool fitted=false;
                for(int lane=0;lane<8 && !fitted;lane++)
                foreach(double shift in new[]{0d,.25,-.25,.5,-.5})
                {
                    double offset=label.Height/2d+stroke/2+5+lane*(label.Height+4);
                    label.Center=new PointF(midpoint.X+(float)(nx*offset+ux*label.Width*shift),midpoint.Y+(float)(ny*offset+uy*label.Width*shift));
                    using var path=new GraphicsPath(); path.AddPolygon(label.Corners(4,4));
                    bool inside=false;
                    if(closed && outline.PointCount>0) { using var intersection=new Region(path); intersection.Intersect(outline); inside=!intersection.IsEmpty(graphics); }
                    int collisions=result.Count(other=>((Rectangle)other.Bounds).IntersectsWith(label.Bounds));
                    double score=(inside?1000000:0)+collisions*10000+lane*10+Math.Abs(shift);
                    if(score<best) { best=score; bestPoint=label.Center; }
                    if(!inside && collisions==0) { fitted=true; break; }
                }
                label.Center=bestPoint; label.Crowded=!fitted; result.Add(label);
            }
            return result.ToArray();
        }
        private static double WorldScale(DrawableContainer owner)
        {
            var o=owner.ModelToImage(PointF.Empty); var x=owner.ModelToImage(new PointF(1,0)); var y=owner.ModelToImage(new PointF(0,1));
            return Math.Max(MeasurementCalculator.LineLength(x.X-o.X,x.Y-o.Y),MeasurementCalculator.LineLength(y.X-o.X,y.Y-o.Y));
        }
        internal static GraphicsPath ArrowGap(DrawableContainer owner)
        {
            var label=Labels(owner).FirstOrDefault(l=>l.ShaftGap); if(label==null)return null;
            var path=new GraphicsPath();
            float extraHeight=(float)(2*(owner.GetFieldValueAsInt(FieldType.LINE_THICKNESS)+10)*WorldScale(owner));
            path.AddPolygon(label.Corners(8,extraHeight).Select(owner.ImageToModel).ToArray()); return path;
        }
        internal static void Draw(Graphics graphics, DrawableContainer owner)
        {
            var labels=Labels(owner); if(labels.Length==0)return;
            var style=(owner.Parent as Surface)?.DimensionStyle ?? DimensionStyle.Default;
            using var font=style.CreateFont(); using var format=TextFormat();
            foreach(var label in labels)
            {
                var saved=graphics.Save();
                try
                {
                    graphics.TranslateTransform(label.Center.X,label.Center.Y); graphics.RotateTransform(label.Angle);
                    graphics.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    if (style.BackgroundColor.A != 0)
                    {
                        using var background = new SolidBrush(style.BackgroundColor);
                        graphics.FillRectangle(background, -label.Width / 2f, -label.Height / 2f, label.Width, label.Height);
                    }
                    TextContainer.DrawText(graphics,new NativeRect(-label.Width/2,-label.Height/2,label.Width,label.Height),0,style.Color,false,format,label.Text,font);
                }
                finally { graphics.Restore(saved); }
            }
        }
    }
}
