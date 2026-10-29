using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Controls
{
    internal sealed class QuickColorPalette : IDisposable
    {
        private readonly IEditorConfiguration _configuration;
        private readonly ToolStripDropDownButton _target = new ToolStripDropDownButton("Line");
        private readonly ToolStripMenuItem _line = new ToolStripMenuItem("Line"), _fill = new ToolStripMenuItem("Fill");
        private readonly List<ToolStripItem> _items = new List<ToolStripItem>();
        private readonly List<ToolStripButton> _recent = new List<ToolStripButton>();
        public IFieldType Target { get; private set; } = FieldType.LINE_COLOR;
        public event Action<Color> ColorSelected;

        public QuickColorPalette(ToolStrip toolbar, IEditorConfiguration configuration)
        {
            _configuration = configuration;
            _target.ToolTipText = "Quick colour target";
            _target.DropDownItems.AddRange(new ToolStripItem[] { _line, _fill });
            _line.Click += (s, e) => SelectTarget(FieldType.LINE_COLOR);
            _fill.Click += (s, e) => SelectTarget(FieldType.FILL_COLOR);
            _items.Add(new ToolStripSeparator());
            _items.Add(_target);
            foreach (var color in new[] { Color.Black, Color.White, Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Magenta })
            {
                var swatch = CreateSwatch();
                SetColor(swatch, color);
                _items.Add(swatch);
            }
            _items.Add(new ToolStripSeparator());
            for (int i = 0; i < 6; i++)
            {
                var swatch = CreateSwatch();
                _recent.Add(swatch);
                _items.Add(swatch);
            }
            toolbar.Items.AddRange(_items.ToArray());
            RefreshRecents();
        }

        private void SelectTarget(IFieldType field)
        {
            Target = field;
            _target.Text = field == FieldType.FILL_COLOR ? "Fill" : "Line";
            _line.Checked = field == FieldType.LINE_COLOR;
            _fill.Checked = field == FieldType.FILL_COLOR;
        }

        private ToolStripButton CreateSwatch()
        {
            var button = new ToolStripButton { DisplayStyle = ToolStripItemDisplayStyle.Image, AutoSize = true, Padding = Padding.Empty, Margin = new Padding(0, 1, 0, 1) };
            button.Click += (s, e) =>
            {
                if (button.Tag is Color color) ColorSelected?.Invoke(color);
            };
            return button;
        }

        private static void SetColor(ToolStripButton button, Color color)
        {
            var image = new Bitmap(14, 14);
            using (var graphics = Graphics.FromImage(image))
            {
                graphics.Clear(Color.White);
                using var brush = new SolidBrush(color);
                graphics.FillRectangle(brush, 1, 1, 12, 12);
                graphics.DrawRectangle(Pens.Gray, 0, 0, 13, 13);
                if (color.A == 0) graphics.DrawLine(Pens.Red, 1, 12, 12, 1);
            }
            var old = button.Image;
            button.Image = image;
            old?.Dispose();
            button.Tag = color;
            button.ToolTipText = button.AccessibleName = ColorDescription.Describe(color);
        }

        public void Remember(Color color)
        {
            var colors = _configuration.RecentColors ?? new List<Color>();
            colors.RemoveAll(c => c.ToArgb() == color.ToArgb());
            colors.Insert(0, color);
            _configuration.RecentColors = colors.Take(12).ToList();
            RefreshRecents();
        }

        public void Refresh(bool hasLine, bool hasFill)
        {
            _line.Enabled = hasLine;
            _fill.Enabled = hasFill;
            if (Target == FieldType.FILL_COLOR && !hasFill) SelectTarget(FieldType.LINE_COLOR);
            if (Target == FieldType.LINE_COLOR && !hasLine && hasFill) SelectTarget(FieldType.FILL_COLOR);
            foreach (var item in _items) item.Visible = hasLine || hasFill;
            RefreshRecents();
        }

        private void RefreshRecents()
        {
            var colors = (_configuration.RecentColors ?? new List<Color>()).GroupBy(c => c.ToArgb()).Select(g => g.First()).Take(6).ToList();
            for (int i = 0; i < _recent.Count; i++)
            {
                _recent[i].Available = _target.Available && i < colors.Count;
                if (i < colors.Count) SetColor(_recent[i], colors[i]);
            }
        }

        public void Dispose()
        {
            foreach (var item in _items) { item.Image?.Dispose(); item.Dispose(); }
        }
    }
}
