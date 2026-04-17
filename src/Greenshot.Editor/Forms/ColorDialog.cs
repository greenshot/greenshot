/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Dpi;
using Greenshot.Base.Controls;
using Greenshot.Base.IniFile;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Controls;
using log4net;

namespace Greenshot.Editor.Forms
{
    /// <summary>
    /// Description of ColorDialog.
    /// </summary>
    public partial class ColorDialog : EditorForm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ColorDialog));
        private static readonly EditorConfiguration EditorConfig = IniConfig.GetIniSection<EditorConfiguration>();
        private static readonly Color[] BasePaletteColumnColors =
        {
            Color.FromArgb(255, 0, 0),
            Color.FromArgb(255, 127, 0),
            Color.FromArgb(255, 255, 0),
            Color.FromArgb(127, 255, 0),
            Color.FromArgb(0, 255, 0),
            Color.FromArgb(0, 255, 127),
            Color.FromArgb(0, 255, 255),
            Color.FromArgb(0, 127, 255),
            Color.FromArgb(0, 0, 255),
            Color.FromArgb(127, 0, 255),
            Color.FromArgb(255, 0, 255),
            Color.FromArgb(255, 0, 127),
            Color.FromArgb(127, 127, 127)
        };

        private const int PaletteShades = 11;
        private const int RecentColorsMax = 12;
        private const int GrayColumnGapBase = 5;
        private static readonly Size BaseClientSize = new Size(292, 218);
        private readonly Dictionary<int, Font> _labelFontsByDpi = new Dictionary<int, Font>();
        private readonly Dictionary<int, Font> _inputFontsByDpi = new Dictionary<int, Font>();

        public ColorDialog()
        {
            SuspendLayout();
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            DpiChanged += (_, args) => EnsurePaletteForDpi(args.DeviceDpiNew);
            ResumeLayout(true);
            EnsurePaletteForDpi(DeviceDpi);
            Disposed += (_, _) =>
            {
                _paletteBitmap?.Dispose();
                _paletteBitmap = null;
                foreach (var font in _labelFontsByDpi.Values)
                {
                    font.Dispose();
                }
                _labelFontsByDpi.Clear();
                foreach (var font in _inputFontsByDpi.Values)
                {
                    font.Dispose();
                }
                _inputFontsByDpi.Clear();
            };
        }

        private readonly List<Button> _recentColorButtons = new List<Button>();
        private readonly ToolTip _toolTip = new ToolTip();
        private bool _updateInProgress;
        private readonly Stopwatch _openStopwatch = new Stopwatch();
        private bool _awaitingFirstShownLog;
        private bool _awaitingFirstPaintLog;
        private int _lastPaletteDpi = -1;
        private Bitmap _paletteBitmap;
        private Rectangle _paletteBounds = Rectangle.Empty;
        private int _paletteCellSize;

        private void EnsurePaletteForDpi(int dpi)
        {
            if (dpi <= 0)
            {
                dpi = 96;
            }
            if (_lastPaletteDpi == dpi)
            {
                return;
            }
            RebuildPaletteForDpi(dpi);
            _lastPaletteDpi = dpi;
        }

        private void ApplyTypographyForDpi(int dpi)
        {
            var labelFont = GetFontForDpi(_labelFontsByDpi, dpi, 10f);
            var inputFont = GetFontForDpi(_inputFontsByDpi, dpi, 11f);

            labelHtmlColor.Font = labelFont;
            labelRed.Font = labelFont;
            labelGreen.Font = labelFont;
            labelBlue.Font = labelFont;
            labelAlpha.Font = labelFont;
            labelRecentColors.Font = inputFont;

            textBoxHtmlColor.Font = inputFont;
            textBoxRed.Font = inputFont;
            textBoxGreen.Font = inputFont;
            textBoxBlue.Font = inputFont;
            textBoxAlpha.Font = inputFont;

            btnTransparent.Font = inputFont;
            btnApply.Font = inputFont;
        }

        private Font GetFontForDpi(Dictionary<int, Font> cache, int dpi, float baseSize)
        {
            if (!cache.TryGetValue(dpi, out var font))
            {
                font = new Font(Font.FontFamily, DpiCalculator.ScaleWithDpi(baseSize, dpi), FontStyle.Regular, GraphicsUnit.Pixel);
                cache[dpi] = font;
            }

            return font;
        }

        private void RebuildPaletteForDpi(int dpi)
        {
            var rebuildStopwatch = Stopwatch.StartNew();
            if (dpi <= 0)
            {
                dpi = 96;
            }

            ApplyTypographyForDpi(dpi);

            var margin = DpiCalculator.ScaleWithDpi(5, dpi);
            var targetButtonSize = DpiCalculator.ScaleWithDpi(15, dpi);
            var maxButtonSizeByWidth = (btnTransparent.Left - margin - margin) / 13;
            var maxButtonSizeByHeight = (labelRecentColors.Top - margin - margin) / 11;
            var buttonSize = Math.Max(10, Math.Min(targetButtonSize, Math.Min(maxButtonSizeByWidth, maxButtonSizeByHeight)));
            var paletteBottom = margin + (11 * buttonSize);
            var grayColumnGap = DpiCalculator.ScaleWithDpi(GrayColumnGapBase, dpi);
            var labelRecentHeight = TextRenderer.MeasureText(labelRecentColors.Text ?? string.Empty, labelRecentColors.Font).Height;
            var recentRowTop = paletteBottom + margin + labelRecentHeight + DpiCalculator.ScaleWithDpi(3, dpi);
            _paletteCellSize = buttonSize;
            _paletteBounds = new Rectangle(margin, margin, (BasePaletteColumnColors.Length * buttonSize) + grayColumnGap, PaletteShades * buttonSize);
            RenderPaletteBitmap(_paletteBounds.Size, buttonSize, grayColumnGap);

            if (_recentColorButtons.Count == 0)
            {
                CreateLastUsedColorButtonRow(margin, recentRowTop, buttonSize, buttonSize);
            }
            else
            {
                LayoutRecentColorButtonRow(margin, recentRowTop, buttonSize, buttonSize);
            }

            labelRecentColors.Location = new Point(margin - 2, paletteBottom + margin);
            UpdateRecentColorsButtonRow();
            rebuildStopwatch.Stop();
            Log.Debug(
                $"ColorDialog.RebuildPaletteForDpi dpi={dpi}, paletteCells={BasePaletteColumnColors.Length * PaletteShades}, recentButtons={_recentColorButtons.Count}, elapsedMs={rebuildStopwatch.ElapsedMilliseconds}");
        }

        private void LayoutRecentColorButtonRow(int x, int y, int w, int h)
        {
            for (var i = 0; i < _recentColorButtons.Count; i++)
            {
                LayoutButton(_recentColorButtons[i], x + (i * w), y, w, h);
            }
        }

        private static void LayoutButton(Control control, int x, int y, int w, int h)
        {
            control.Location = new Point(x, y);
            if (control.Width != w || control.Height != h)
            {
                control.Size = new Size(w, h);
            }
        }

        public Color Color
        {
            get { return colorPanel.BackColor; }
            set { PreviewColor(value, this); }
        }

        private void RenderPaletteBitmap(Size paletteSize, int buttonSize, int grayColumnGap)
        {
            _paletteBitmap?.Dispose();
            _paletteBitmap = new Bitmap(Math.Max(1, paletteSize.Width), Math.Max(1, paletteSize.Height));
            using (var graphics = Graphics.FromImage(_paletteBitmap))
            {
                graphics.Clear(BackColor);
                for (var column = 0; column < BasePaletteColumnColors.Length; column++)
                {
                    var x = PaletteColumnToX(column, buttonSize, grayColumnGap);
                    for (var row = 0; row < PaletteShades; row++)
                    {
                        using (var brush = new SolidBrush(GetPaletteColor(column, row)))
                        {
                            graphics.FillRectangle(brush, x, row * buttonSize, buttonSize, buttonSize);
                        }
                    }
                }
            }
            Invalidate(_paletteBounds);
        }

        private static int PaletteColumnToX(int column, int buttonSize, int grayColumnGap)
        {
            var x = column * buttonSize;
            if (column == BasePaletteColumnColors.Length - 1)
            {
                x += grayColumnGap;
            }
            return x;
        }

        private Color GetPaletteColor(int column, int row)
        {
            var shadedColorsNum = (PaletteShades - 1) / 2;
            var baseColor = BasePaletteColumnColors[Math.Max(0, Math.Min(BasePaletteColumnColors.Length - 1, column))];
            if (row <= shadedColorsNum)
            {
                return ScaleColor(baseColor, row / (float)shadedColorsNum);
            }
            var lightenFactor = (row - shadedColorsNum) / (float)shadedColorsNum;
            return LightenColor(baseColor, lightenFactor);
        }

        private static Color ScaleColor(Color color, float factor) =>
            Color.FromArgb(255, (int)(color.R * factor), (int)(color.G * factor), (int)(color.B * factor));

        private static Color LightenColor(Color color, float factor) =>
            Color.FromArgb(
                255,
                color.R + (int)((255 - color.R) * factor),
                color.G + (int)((255 - color.G) * factor),
                color.B + (int)((255 - color.B) * factor));

        private bool TryGetPaletteColorFromPoint(Point point, out Color color)
        {
            color = Color.Empty;
            if (_paletteBounds == Rectangle.Empty || !_paletteBounds.Contains(point) || _paletteCellSize <= 0)
            {
                return false;
            }

            var localX = point.X - _paletteBounds.Left;
            var localY = point.Y - _paletteBounds.Top;
            var grayGap = _paletteBounds.Width - (BasePaletteColumnColors.Length * _paletteCellSize);
            var grayStartX = (BasePaletteColumnColors.Length - 1) * _paletteCellSize + grayGap;

            int column;
            if (localX >= grayStartX)
            {
                column = BasePaletteColumnColors.Length - 1;
                localX -= grayGap;
            }
            else
            {
                column = localX / _paletteCellSize;
            }

            var row = localY / _paletteCellSize;
            if (column < 0 || column >= BasePaletteColumnColors.Length || row < 0 || row >= PaletteShades)
            {
                return false;
            }

            color = GetPaletteColor(column, row);
            return true;
        }

        private void CreateLastUsedColorButtonRow(int x, int y, int w, int h)
        {
            for (int i = 0; i < 12; i++)
            {
                Button b = new GreenshotDoubleClickButton
                {
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(x, y),
                    Size = new Size(w, h),
                    TabStop = false,
                    Enabled = false
                };
                b.FlatAppearance.BorderSize = 0;
                b.Click += ColorButtonClick;
                b.DoubleClick += ColorButtonDoubleClick;
                b.Enabled = false;
                _recentColorButtons.Add(b);
                x += w;
            }

            Controls.AddRange(_recentColorButtons.ToArray());
        }

        private void UpdateRecentColorsButtonRow()
        {
            if (_recentColorButtons.Count == 0)
            {
                return;
            }

            for (int i = 0; i < EditorConfig.RecentColors.Count && i < RecentColorsMax; i++)
            {
                _recentColorButtons[i].BackColor = EditorConfig.RecentColors[i];
                _recentColorButtons[i].Enabled = true;
                SetButtonTooltip(_recentColorButtons[i], EditorConfig.RecentColors[i]);
            }

            for (int i = EditorConfig.RecentColors.Count; i < _recentColorButtons.Count; i++)
            {
                _recentColorButtons[i].BackColor = Color.Transparent;
                _recentColorButtons[i].Enabled = false;
            }
        }

        private void PreviewColor(Color colorToPreview, Control trigger)
        {
            _updateInProgress = true;
            colorPanel.BackColor = colorToPreview;
            if (trigger != textBoxHtmlColor)
            {
                textBoxHtmlColor.Text = ColorTranslator.ToHtml(colorToPreview);
            }

            if (trigger != textBoxRed && trigger != textBoxGreen && trigger != textBoxBlue && trigger != textBoxAlpha)
            {
                textBoxRed.Text = colorToPreview.R.ToString();
                textBoxGreen.Text = colorToPreview.G.ToString();
                textBoxBlue.Text = colorToPreview.B.ToString();
                textBoxAlpha.Text = colorToPreview.A.ToString();
            }

            _updateInProgress = false;
        }

        private void AddToRecentColors(Color c)
        {
            EditorConfig.RecentColors.Remove(c);
            EditorConfig.RecentColors.Insert(0, c);
            if (EditorConfig.RecentColors.Count > 12)
            {
                EditorConfig.RecentColors.RemoveRange(12, EditorConfig.RecentColors.Count - 12);
            }

            UpdateRecentColorsButtonRow();
        }

        private void TextBoxHexadecimalTextChanged(object sender, EventArgs e)
        {
            if (_updateInProgress)
            {
                return;
            }

            TextBox textBox = (TextBox)sender;
            string text = textBox.Text.Replace("#", string.Empty);
            Color c;
            if (int.TryParse(text, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture, out var i))
            {
                c = Color.FromArgb(i);
            }
            else
            {
                try
                {
                    var knownColor = (KnownColor)Enum.Parse(typeof(KnownColor), text, true);
                    c = Color.FromKnownColor(knownColor);
                }
                catch (Exception)
                {
                    return;
                }
            }

            Color opaqueColor = Color.FromArgb(255, c.R, c.G, c.B);
            PreviewColor(opaqueColor, textBox);
        }

        private void TextBoxRgbTextChanged(object sender, EventArgs e)
        {
            if (_updateInProgress)
            {
                return;
            }

            TextBox textBox = (TextBox)sender;
            PreviewColor(
                Color.FromArgb(GetColorPartIntFromString(textBoxAlpha.Text), GetColorPartIntFromString(textBoxRed.Text), GetColorPartIntFromString(textBoxGreen.Text),
                    GetColorPartIntFromString(textBoxBlue.Text)), textBox);
        }

        private void TextBoxGotFocus(object sender, EventArgs e)
        {
            textBoxHtmlColor.SelectAll();
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter)
            {
                AddToRecentColors(colorPanel.BackColor);
            }
        }

        private void ColorButtonClick(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            PreviewColor(b.BackColor, b);
        }

        private void ColorButtonDoubleClick(object sender, EventArgs e)
        {
            ColorButtonClick(sender, e);
            BtnApplyClick(sender, e);
        }

        private void SetButtonTooltip(Button colorButton, Color color)
        {
            _toolTip.SetToolTip(colorButton, ColorTranslator.ToHtml(color) + " | R:" + color.R + ", G:" + color.G + ", B:" + color.B);
        }

        private void BtnTransparentClick(object sender, EventArgs e)
        {
            ColorButtonClick(sender, e);
        }

        private void BtnApplyClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Hide();
            AddToRecentColors(colorPanel.BackColor);
        }

        private int GetColorPartIntFromString(string s)
        {
            int.TryParse(s, out var ret);
            if (ret < 0)
            {
                ret = 0;
            }
            else if (ret > 255)
            {
                ret = 255;
            }

            return ret;
        }

        private void PipetteUsed(object sender, PipetteUsedArgs e)
        {
            Color = e.Color;
        }

        public new DialogResult ShowDialog(IWin32Window owner)
        {
            var showStopwatch = Stopwatch.StartNew();
            _openStopwatch.Restart();
            _awaitingFirstShownLog = true;
            _awaitingFirstPaintLog = true;
            var mouse = Cursor.Position;
            var targetDpi = NativeDpiMethods.GetDpi(mouse);
            if (targetDpi <= 0)
            {
                targetDpi = 96;
            }

            // Keep dialog sizing on the framework autoscale path.
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = GetFontForDpi(_inputFontsByDpi, 96, 8.25f);
            ClientSize = BaseClientSize;
            var screen = Screen.FromPoint(mouse);
            var workingArea = screen.WorkingArea;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(
                Math.Max(workingArea.Left, Math.Min(mouse.X - Width / 2, workingArea.Right - Width)),
                Math.Max(workingArea.Top, Math.Min(mouse.Y - Height / 2, workingArea.Bottom - Height)));
            _ = Handle;
            if (DeviceDpi > 0 && DeviceDpi != targetDpi)
            {
                var scaleFactor = targetDpi / (float)DeviceDpi;
                Scale(new SizeF(scaleFactor, scaleFactor));
            }
            var dialogDpi = targetDpi;
            var baseClientSize = DpiCalculator.ScaleWithDpi(BaseClientSize, dialogDpi);
            ClientSize = baseClientSize;
            SuspendLayout();
            EnsurePaletteForDpi(dialogDpi);

            // Two-pass sizing:
            // 1) layout once, measure required bounds
            // 2) apply size, layout again, then expand/shrink if still off by a small amount.
            var contentPadding = DpiCalculator.ScaleWithDpi(12, dialogDpi);
            var scaleUpSafety = (DeviceDpi > 0 && dialogDpi > DeviceDpi)
                ? DpiCalculator.ScaleWithDpi(10, dialogDpi)
                : 0;

            Size MeasureRequiredClientSize()
            {
                var requiredWidth = 0;
                var requiredHeight = 0;
                foreach (Control control in Controls)
                {
                    if (!control.Visible)
                    {
                        continue;
                    }

                    requiredWidth = Math.Max(requiredWidth, control.Right);
                    requiredHeight = Math.Max(requiredHeight, control.Bottom);
                }
                requiredWidth = Math.Max(requiredWidth, _paletteBounds.Right);
                requiredHeight = Math.Max(requiredHeight, _paletteBounds.Bottom);
                requiredHeight += scaleUpSafety;

                return new Size(
                    Math.Max(baseClientSize.Width, requiredWidth + contentPadding),
                    Math.Max(baseClientSize.Height, requiredHeight + contentPadding));
            }

            ResumeLayout(false);
            PerformLayout();
            var targetClientSize = MeasureRequiredClientSize();

            SuspendLayout();
            ClientSize = targetClientSize;
            ResumeLayout(false);
            PerformLayout();

            // Re-measure after layout: expand if still clipped; shrink slightly on downscale if we overshot.
            var measuredAfter = MeasureRequiredClientSize();
            var finalSize = ClientSize;
            if (measuredAfter.Width > ClientSize.Width || measuredAfter.Height > ClientSize.Height)
            {
                SuspendLayout();
                ClientSize = new Size(Math.Max(ClientSize.Width, measuredAfter.Width), Math.Max(ClientSize.Height, measuredAfter.Height));
                ResumeLayout(false);
                PerformLayout();
                finalSize = ClientSize;
            }
            else
            {
                var allowShrink = DeviceDpi > 0 && dialogDpi < DeviceDpi;
                if (allowShrink)
                {
                    SuspendLayout();
                    ClientSize = new Size(
                        Math.Max(baseClientSize.Width, measuredAfter.Width),
                        Math.Max(baseClientSize.Height, measuredAfter.Height));
                    ResumeLayout(false);
                    PerformLayout();
                    finalSize = ClientSize;
                }
            }

            Log.Debug(
                $"ColorDialog.SizePass baseClient={baseClientSize}, pass1Target={targetClientSize}, pass2Measured={measuredAfter}, finalClient={finalSize}, " +
                $"deltaW={finalSize.Width - measuredAfter.Width}, deltaH={finalSize.Height - measuredAfter.Height}, " +
                $"targetDpi={targetDpi}, deviceDpi={DeviceDpi}, layoutDpi={dialogDpi}");

            // Empirical slack: in mixed-DPI transitions WinForms can still adjust the client area
            // after our measurement pass (seen as "slightly too small"). Add a small extra height
            // only when scaling up to avoid clipping, without worsening the downscale case.
            if (DeviceDpi > 0 && dialogDpi > DeviceDpi)
            {
                var visualSlackH = DpiCalculator.ScaleWithDpi(16, dialogDpi);
                var visualSlackW = DpiCalculator.ScaleWithDpi(10, dialogDpi);
                ClientSize = new Size(ClientSize.Width + visualSlackW, ClientSize.Height + visualSlackH);
            }
            Location = new Point(
                Math.Max(workingArea.Left, Math.Min(mouse.X - Width / 2, workingArea.Right - Width)),
                Math.Max(workingArea.Top, Math.Min(mouse.Y - Height / 2, workingArea.Bottom - Height)));

            Log.Debug($"ColorDialog.FinalClientSize client={ClientSize}, targetDpi={targetDpi}, deviceDpi={DeviceDpi}, layoutDpi={dialogDpi}");

            Log.Debug($"ColorDialog.ShowDialog targetDpi={targetDpi}, deviceDpi={DeviceDpi}, layoutDpi={dialogDpi}, size={Size}, client={ClientSize}, location={Location}");
            showStopwatch.Stop();
            Log.Debug($"ColorDialog.ShowDialog setup elapsedMs={showStopwatch.ElapsedMilliseconds}");

            return base.ShowDialog(owner);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_awaitingFirstShownLog)
            {
                _awaitingFirstShownLog = false;
                Log.Debug($"ColorDialog.OnShown elapsedMs={_openStopwatch.ElapsedMilliseconds}");
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_paletteBitmap != null && _paletteBounds != Rectangle.Empty)
            {
                e.Graphics.DrawImageUnscaled(_paletteBitmap, _paletteBounds.Location);
            }
            if (_awaitingFirstPaintLog)
            {
                _awaitingFirstPaintLog = false;
                _openStopwatch.Stop();
                Log.Debug($"ColorDialog.FirstPaint elapsedMs={_openStopwatch.ElapsedMilliseconds}");
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (TryGetPaletteColorFromPoint(e.Location, out var color))
            {
                PreviewColor(color, this);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (TryGetPaletteColorFromPoint(e.Location, out var color))
            {
                PreviewColor(color, this);
                BtnApplyClick(this, EventArgs.Empty);
            }
        }

    }
}