/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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


using System.Drawing;
using System.Runtime.Serialization;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Adorners;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Helpers;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Description of CropContainer.
    /// </summary>
    public class CropContainer : DrawableContainer
    {
        //awailable Styles
        public static readonly string DefaultCropStyle = nameof(DefaultCropStyle);
        public static readonly string VerticalCropOutStyle = nameof(VerticalCropOutStyle);
        public static readonly string HorizontalCropOutStyle = nameof(HorizontalCropOutStyle);

        public CropContainer(ISurface parent) : base(parent)
        {
            Init();
        }

        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
            Init();
        }

        private void Init()
        {
            switch (GetFieldValueAsString(FieldType.CROPSTYLE))
            {
                case string s when s.Equals(HorizontalCropOutStyle):
                    {
                        InitHorizontalCropOutStyle();
                        break;
                    }
                case string s when s.Equals(VerticalCropOutStyle):
                    {
                        InitVerticalCropOutStyle();
                        break;
                    }
                default:
                    {
                        InitCropStyle();
                        break;
                    }
            }
        }

        /// <summary>
        /// rotate through all awailable Styles
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public static string GetNextStyle(string style)
        {
            return style switch
            {
                var s when s.Equals(HorizontalCropOutStyle) => VerticalCropOutStyle,
                var s when s.Equals(VerticalCropOutStyle) => DefaultCropStyle,
                _ => HorizontalCropOutStyle,
            };
        }

        private void InitCropStyle()
        {
            CreateDefaultAdorners();
        }

        private void InitHorizontalCropOutStyle()
        {
            var defaultHeight = 25;

            if (_parent?.Image is { } image)
            {
                Size = new Size(image.Width, defaultHeight);
            }
            CreateTopBottomAdorners();
        }

        private void InitVerticalCropOutStyle()
        {
            var defaultWidth = 25;

            if (_parent?.Image is { } image)
            {
                Size = new Size(defaultWidth, image.Height);
            }

            CreateLeftRightAdorners();
        }

        private void CreateTopBottomAdorners()
        {
            Adorners.Add(new ResizeAdorner(this, Positions.TopCenter));
            Adorners.Add(new ResizeAdorner(this, Positions.BottomCenter));
        }
        private void CreateLeftRightAdorners()
        {
            Adorners.Add(new ResizeAdorner(this, Positions.MiddleLeft));
            Adorners.Add(new ResizeAdorner(this, Positions.MiddleRight));
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.FLAGS, FieldFlag.CONFIRMABLE);
            AddField(GetType(), FieldType.CROPSTYLE, DefaultCropStyle);
        }

        public override void Invalidate()
        {
            _parent?.Invalidate();
        }

        /// <summary>
        /// We need to override the DrawingBound, return a rectangle in the size of the image, to make sure this element is always draw
        /// (we create a transparent brown over the complete picture)
        /// </summary>
        public override Rectangle DrawingBounds
        {
            get
            {
                if (_parent?.Image is { } image)
                {
                    return new Rectangle(0, 0, image.Width, image.Height);
                }

                return Rectangle.Empty;
            }
        }

        public override void Draw(Graphics g, RenderMode rm)
        {
            if (_parent == null)
            {
                return;
            }


            using Brush cropBrush = new SolidBrush(Color.FromArgb(100, 150, 150, 100));
            Rectangle cropRectangle = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);
            Rectangle selectionRect = new Rectangle(cropRectangle.Left - 1, cropRectangle.Top - 1, cropRectangle.Width + 1, cropRectangle.Height + 1);
            Size imageSize = _parent.Image.Size;

            DrawSelectionBorder(g, selectionRect);

            switch (GetFieldValueAsString(FieldType.CROPSTYLE))
            {
                case var s when s.Equals(HorizontalCropOutStyle):
                case var t when t.Equals(VerticalCropOutStyle):
                    {
                        //draw inside
                        g.FillRectangle(cropBrush, cropRectangle);
                        break;
                    }
                default:
                    {
                        //draw outside
                        // top
                        g.FillRectangle(cropBrush, new Rectangle(0, 0, imageSize.Width, cropRectangle.Top));
                        // left
                        g.FillRectangle(cropBrush, new Rectangle(0, cropRectangle.Top, cropRectangle.Left, cropRectangle.Height));
                        // right
                        g.FillRectangle(cropBrush, new Rectangle(cropRectangle.Left + cropRectangle.Width, cropRectangle.Top, imageSize.Width - (cropRectangle.Left + cropRectangle.Width), cropRectangle.Height));
                        // bottom
                        g.FillRectangle(cropBrush, new Rectangle(0, cropRectangle.Top + cropRectangle.Height, imageSize.Width, imageSize.Height - (cropRectangle.Top + cropRectangle.Height)));
                        break;
                    }
            }


        }

        /// <summary>
        /// No context menu for the CropContainer
        /// </summary>
        public override bool HasContextMenu => false;

        public override bool HandleMouseDown(int x, int y)
        {
            return GetFieldValueAsString(FieldType.CROPSTYLE) switch
            {
                //force horizontal crop to left edge
                var s when s.Equals(HorizontalCropOutStyle) => base.HandleMouseDown(0, y),
                //force vertical crop to top edge
                var s when s.Equals(VerticalCropOutStyle) => base.HandleMouseDown(x, 0),
                _ => base.HandleMouseDown(x, y),
            };
        }
  
        public override bool HandleMouseMove(int x, int y)
        {
            Invalidate();

            switch (GetFieldValueAsString(FieldType.CROPSTYLE))
            {
                case var s when s.Equals(HorizontalCropOutStyle):
                    {
                        //stick on left and right
                        //allow only horizontal changes
                        if (_parent?.Image is { } image)
                        {
                            _boundsAfterResize.X = 0;
                            _boundsAfterResize.Y = _boundsBeforeResize.Top;
                            _boundsAfterResize.Width = image.Width;
                            _boundsAfterResize.Height = y - _boundsAfterResize.Top;
                        }
                        break;
                    }
                case var s when s.Equals(VerticalCropOutStyle):
                    {
                        //stick on top and bottom
                        //allow only vertical changes
                        if (_parent?.Image is { } image)
                        {
                            _boundsAfterResize.X = _boundsBeforeResize.Left;
                            _boundsAfterResize.Y = 0;
                            _boundsAfterResize.Width = x - _boundsAfterResize.Left;
                            _boundsAfterResize.Height = image.Height;
                        }
                        break;
                    }
                default:
                    {
                        // reset "workbench" rectangle to current bounds
                        _boundsAfterResize.X = _boundsBeforeResize.Left;
                        _boundsAfterResize.Y = _boundsBeforeResize.Top;
                        _boundsAfterResize.Width = x - _boundsAfterResize.Left;
                        _boundsAfterResize.Height = y - _boundsAfterResize.Top;
                        break;
                    }

            }
            ScaleHelper.Scale(_boundsBeforeResize, x, y, ref _boundsAfterResize, GetAngleRoundProcessor());

            // apply scaled bounds to this DrawableContainer
            ApplyBounds(_boundsAfterResize);

            Invalidate();
            return true;
        }
    }
}
