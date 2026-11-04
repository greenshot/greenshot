using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Measurements;

namespace Greenshot.Editor.Controls
{
    internal sealed partial class MeasurementPanel
    {
        private Surface _dimensionDocument;
        private bool _dimensionsUpdating, _dimensionCommit;
        private readonly DataGridView _dimensionEdges = new DataGridView
        {
            Width=214, Height=120, AllowUserToAddRows=false, AllowUserToDeleteRows=false, RowHeadersVisible=false,
            AllowUserToResizeRows=false, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
            AccessibleName="Edge dimension visibility and length", MultiSelect=false, BackgroundColor=SystemColors.Window
        };
        private readonly Button _dimensionFont = new Button { Text="Dimension font…", AutoSize=true };
        private readonly Button _dimensionColor = new Button { Text="Dimension colour…", AutoSize=true };
        private readonly Label _dimensionStyleLabel = new Label { AutoSize=true, MaximumSize=new Size(210,0) };
        private readonly Label _dimensionWarning = new Label { AutoSize=true, MaximumSize=new Size(210,0), ForeColor=Color.DarkRed };

        private void InitializeDimensionControls()
        {
            _dimensionEdges.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText="Show", FillWeight=38, SortMode=DataGridViewColumnSortMode.NotSortable });
            _dimensionEdges.Columns.Add(new DataGridViewTextBoxColumn { HeaderText="Edge", ReadOnly=true, FillWeight=52, SortMode=DataGridViewColumnSortMode.NotSortable });
            _dimensionEdges.Columns.Add(new DataGridViewTextBoxColumn { HeaderText="Length", ReadOnly=true, FillWeight=100, SortMode=DataGridViewColumnSortMode.NotSortable });
            _dimensionEdges.CurrentCellDirtyStateChanged += (s,e) =>
            { if (!_dimensionsUpdating && _dimensionEdges.IsCurrentCellDirty) _dimensionEdges.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            _dimensionEdges.CellValueChanged += (s,e) =>
            {
                if (_dimensionsUpdating || _dimensionCommit || e.RowIndex<0 || e.ColumnIndex!=0 || _selected==null) return;
                _dimensionCommit=true;
                try { _selected.SetDimensionVisible((int)_dimensionEdges.Rows[e.RowIndex].Tag, Convert.ToBoolean(_dimensionEdges.Rows[e.RowIndex].Cells[0].Value)); }
                finally { _dimensionCommit=false; }
                RefreshDimensionLabels(_dimensionDocument); GeometryEdited?.Invoke(this,EventArgs.Empty);
            };
            _dimensionFont.Click += (s,e) =>
            {
                if(_dimensionDocument==null)return;
                var current=_dimensionDocument.DimensionStyle;
                using var font=current.CreateFont(); using var dialog=new FontDialog { Font=font,MinSize=1,MaxSize=144,FontMustExist=true,ShowEffects=true };
                if(dialog.ShowDialog(FindForm())!=DialogResult.OK)return;
                using var chosen=dialog.Font;
                _dimensionDocument.SetDimensionStyle(new DimensionStyle(chosen.Name,chosen.SizeInPoints,chosen.Style,current.Color,current.Format,current.BackgroundColor));
                RefreshDimensionLabels(_dimensionDocument); GeometryEdited?.Invoke(this,EventArgs.Empty);
            };
            _dimensionColor.Click += (s,e) =>
            {
                if(_dimensionDocument==null)return;
                var current=_dimensionDocument.DimensionStyle;
                using var dialog=new ColorDialog { Color=current.Color,FullOpen=true };
                if(dialog.ShowDialog(FindForm())!=DialogResult.OK)return;
                _dimensionDocument.SetDimensionStyle(new DimensionStyle(current.Family,current.Size,current.FontStyle,dialog.Color,current.Format,current.BackgroundColor));
                RefreshDimensionLabels(_dimensionDocument); GeometryEdited?.Invoke(this,EventArgs.Empty);
            };
            _body.Controls.AddRange(new Control[] { new Label { Text="Edge dimensions",AutoSize=true },_dimensionEdges });
            InitializeDimensionFormatControls();
            _body.Controls.AddRange(new Control[] { _dimensionStyleLabel,_dimensionFont,_dimensionColor,_dimensionWarning });
        }

        internal void RefreshDimensionLabels(Surface document)
        {
            if(_dimensionCommit)return;
            _dimensionDocument=document; _dimensionsUpdating=true;
            try
            {
                int selectedEdge = SelectedDimensionEdge;
                _dimensionEdges.Rows.Clear();
                var edges=DimensionLayout.Edges(_selected);
                foreach(var edge in edges)
                {
                    string length=DimensionLayout.FormatLength(edge.Length,document?.Measurements ?? MeasurementState.Pixels,_selected.GetDimensionFormat(edge.Index) ?? document?.DimensionStyle.Format);
                    if(edge.Length==0)length+=" (zero; hidden)";
                    int row=_dimensionEdges.Rows.Add(_selected.IsDimensionVisible(edge.Index),edge.Name,length);
                    _dimensionEdges.Rows[row].Tag=edge.Index;
                    _dimensionEdges.Rows[row].Cells[2].ToolTipText=length;
                }
                if (selectedEdge >= 0 && selectedEdge < _dimensionEdges.Rows.Count) _dimensionEdges.CurrentCell = _dimensionEdges.Rows[selectedEdge].Cells[1];
                RefreshDimensionFormatControls();
                _dimensionEdges.Visible=edges.Length>0;
                _dimensionEdges.Height=Math.Min(190,_dimensionEdges.ColumnHeadersHeight+edges.Length*_dimensionEdges.RowTemplate.Height+3);
                _dimensionFont.Enabled=_dimensionColor.Enabled=document!=null;
                var style=document?.DimensionStyle ?? DimensionStyle.Default;
                _dimensionStyleLabel.Text="All dimension text: "+style.Family+", "+style.Size.ToString("0.##")+" pt";
                _dimensionColor.ForeColor=style.Color;
                _dimensionWarning.Text=string.Empty;
                if(document?.Image!=null)
                {
                    var labels=document.Elements.OfType<DrawableContainer>().Where(o=>o.HasVisibleDimensions).SelectMany(DimensionLayout.Labels).ToArray();
                    if(labels.Any(l=>l.Bounds.Left<0 || l.Bounds.Top<0 || l.Bounds.Right>document.Image.Width || l.Bounds.Bottom>document.Image.Height))
                        _dimensionWarning.Text="Labels clip at the image boundary. Add canvas room manually.";
                    else if(labels.Any(l=>l.Crowded))_dimensionWarning.Text="Crowded edges: some dimension labels may overlap.";
                }
            }
            finally { _dimensionsUpdating=false; }
        }
    }
}
