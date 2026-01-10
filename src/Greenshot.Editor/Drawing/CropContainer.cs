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
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
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
        /// <summary>
        /// Available Crop modes
        /// </summary>
        public enum CropModes
        {
            /// <summary>
            ///  crop all outside the selection rectangle
            /// </summary>
            Default,
            /// <summary>
            /// like default, but initially creates the selection rectangle
            /// </summary>
            AutoCrop,
            /// <summary>
            /// crop all inside the selection, anchors the selection to the top and bottom edges
            /// </summary>
            Vertical,
            /// <summary>
            /// crop all inside the selection, anchors the selection to the left and right edges
            /// </summary>
            Horizontal
        }

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
            switch (GetFieldValue(FieldType.CROPMODE))
            {
                case CropModes.Horizontal:
                    {
                        InitHorizontalCropOutStyle();
                        break;
                    }
                case CropModes.Vertical:
                    {
                        InitVerticalCropOutStyle();
                        break;
                    }
                default:
                    {
                        CreateDefaultAdorners();
                        break;
                    }
            }
        }

        private void InitHorizontalCropOutStyle()
        {
            const int defaultHeight = 25;

            if (_parent?.Image is { } image)
            {
                Size = new Size(image.Width, defaultHeight);
            }
            CreateTopBottomAdorners();
        }

        private void InitVerticalCropOutStyle()
        {
            const int defaultWidth = 25;

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
            AddField(GetType(), FieldType.CROPMODE, CropModes.Default);
        }

        public override void Invalidate()
        {
            _parent?.Invalidate();
        }

        /// <summary>
        /// We need to override the DrawingBound, return a rectangle in the size of the image, to make sure this element is always draw
        /// (we create a transparent brown over the complete picture)
        /// </summary>
        public override NativeRect DrawingBounds
        {
            get
            {
                if (_parent?.Image is { } image)
                {
                    return new NativeRect(0, 0, image.Width, image.Height);
                }

                return NativeRect.Empty;
            }
        }

        public override void Draw(Graphics g, RenderMode rm)
        {
            if (_parent == null)
            {
                return;
            }


            using Brush cropBrush = new SolidBrush(Color.FromArgb(100, 150, 150, 100));
            var cropRectangle = new NativeRect(Left, Top, Width, Height).Normalize();
            var selectionRect = new NativeRect(cropRectangle.Left - 1, cropRectangle.Top - 1, cropRectangle.Width + 1, cropRectangle.Height + 1);
            Size imageSize = _parent.Image.Size;

            DrawSelectionBorder(g, selectionRect);

            switch (GetFieldValue(FieldType.CROPMODE))
            {
                case CropModes.Horizontal:
                case CropModes.Vertical:
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
            return GetFieldValue(FieldType.CROPMODE) switch
            {
                //force horizontal crop to left edge
                CropModes.Horizontal => base.HandleMouseDown(0, y),
                //force vertical crop to top edge
                CropModes.Vertical => base.HandleMouseDown(x, 0),
                _ => base.HandleMouseDown(x, y),
            };
        }
  
        public override bool HandleMouseMove(int x, int y)
        {
            Invalidate();

            switch (GetFieldValue(FieldType.CROPMODE))
            {
                case CropModes.Horizontal:
                    {
                        //stick on left and right
                        //allow only horizontal changes
                        if (_parent?.Image is { } image)
                        {
                            _boundsAfterResize = new NativeRectFloat(0, _boundsBeforeResize.Top, image.Width, y - _boundsAfterResize.Top);
                        }
                        break;
                    }
                case CropModes.Vertical:
                    {
                        //stick on top and bottom
                        //allow only vertical changes
                        if (_parent?.Image is { } image)
                        {
                            _boundsAfterResize = new NativeRectFloat(_boundsBeforeResize.Left, 0, x - _boundsAfterResize.Left, image.Height);
                        }
                        break;
                    }
                default:
                    {
                        // reset "workbench" rectangle to current bounds
                        _boundsAfterResize = new NativeRectFloat(
                            _boundsBeforeResize.Left, _boundsBeforeResize.Top,
                            x - _boundsAfterResize.Left, y - _boundsAfterResize.Top);

                        _boundsAfterResize = ScaleHelper.Scale(_boundsAfterResize, x, y, GetAngleRoundProcessor());
                        break;
                    }
            }

            // apply scaled bounds to this DrawableContainer
            ApplyBounds(_boundsAfterResize);

            Invalidate();
            return true;
        }
        /// <summary>
        /// <inheritdoc />
        /// <para/>
        /// Make sure this container is not undoable
        /// </summary>
        public override bool IsUndoable => false;

        /// <summary>
        /// <inheritdoc />
        /// <para/>
        /// See dedicated confirm method <see cref="Surface.ConfirmCrop(bool)"/>
        /// </summary>
        public override bool IsConfirmable => true;
    }
}
