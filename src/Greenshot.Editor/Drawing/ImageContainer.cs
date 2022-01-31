﻿/*
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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.Serialization;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using log4net;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Description of BitmapContainer.
    /// </summary>
    [Serializable]
    public class ImageContainer : DrawableContainer, IImageContainer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImageContainer));

        private Image image;

        /// <summary>
        /// This is the shadow version of the bitmap, rendered once to save performance
        /// Do not serialize, as the shadow is recreated from the original bitmap if it's not available
        /// </summary>
        [NonSerialized] private Image _shadowBitmap;

        /// <summary>
        /// This is the offset for the shadow version of the bitmap
        /// Do not serialize, as the offset is recreated
        /// </summary>
        [NonSerialized] private Point _shadowOffset = new Point(-1, -1);

        public ImageContainer(ISurface parent, string filename) : this(parent)
        {
            Load(filename);
        }

        public ImageContainer(ISurface parent) : base(parent)
        {
            FieldChanged += BitmapContainer_OnFieldChanged;
            Init();
        }

        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
            Init();
        }

        private void Init()
        {
            CreateDefaultAdorners();
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.SHADOW, false);
        }

        protected void BitmapContainer_OnFieldChanged(object sender, FieldChangedEventArgs e)
        {
            if (!sender.Equals(this))
            {
                return;
            }

            if (FieldType.SHADOW.Equals(e.Field.FieldType))
            {
                ChangeShadowField();
            }
        }

        public void ChangeShadowField()
        {
            bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
            if (shadow)
            {
                CheckShadow(true);
                Width = _shadowBitmap.Width;
                Height = _shadowBitmap.Height;
                Left -= _shadowOffset.X;
                Top -= _shadowOffset.Y;
            }
            else
            {
                Width = image.Width;
                Height = image.Height;
                if (_shadowBitmap != null)
                {
                    Left += _shadowOffset.X;
                    Top += _shadowOffset.Y;
                }
            }
        }

        public Image Image
        {
            set
            {
                // Remove all current bitmaps
                DisposeImage();
                DisposeShadow();
                image = ImageHelper.Clone(value);
                bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
                CheckShadow(shadow);
                if (!shadow)
                {
                    Width = image.Width;
                    Height = image.Height;
                }
                else
                {
                    Width = _shadowBitmap.Width;
                    Height = _shadowBitmap.Height;
                    Left -= _shadowOffset.X;
                    Top -= _shadowOffset.Y;
                }
            }
            get { return image; }
        }

        /// <summary>
        /// The bulk of the clean-up code is implemented in Dispose(bool)
        /// This Dispose is called from the Dispose and the Destructor.
        /// When disposing==true all non-managed resources should be freed too!
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeImage();
                DisposeShadow();
            }

            base.Dispose(disposing);
        }

        private void DisposeImage()
        {
            image?.Dispose();
            image = null;
        }

        private void DisposeShadow()
        {
            _shadowBitmap?.Dispose();
            _shadowBitmap = null;
        }


        /// <summary>
        /// Make sure the content is also transformed.
        /// </summary>
        /// <param name="matrix"></param>
        public override void Transform(Matrix matrix)
        {
            int rotateAngle = CalculateAngle(matrix);
            // we currently assume only one transformation has been made.
            if (rotateAngle != 0)
            {
                Log.DebugFormat("Rotating element with {0} degrees.", rotateAngle);
                DisposeShadow();
                using var tmpMatrix = new Matrix();
                using (image)
                {
                    image = ImageHelper.ApplyEffect(image, new RotateEffect(rotateAngle), tmpMatrix);
                }
            }

            base.Transform(matrix);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        public void Load(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }

            // Always make sure ImageHelper.LoadBitmap results are disposed some time,
            // as we close the bitmap internally, we need to do it afterwards
            using (var tmpImage = ImageHelper.LoadImage(filename))
            {
                Image = tmpImage;
            }

            Log.Debug("Loaded file: " + filename + " with resolution: " + Height + "," + Width);
        }

        /// <summary>
        /// This checks if a shadow is already generated
        /// </summary>
        /// <param name="shadow"></param>
        private void CheckShadow(bool shadow)
        {
            if (!shadow || _shadowBitmap != null)
            {
                return;
            }

            using var matrix = new Matrix();
            _shadowBitmap = ImageHelper.ApplyEffect(image, new DropShadowEffect(), matrix);
        }

        /// <summary>
        /// Draw the actual container to the graphics object
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="rm"></param>
        public override void Draw(Graphics graphics, RenderMode rm)
        {
            if (image == null)
            {
                return;
            }

            bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            if (shadow)
            {
                CheckShadow(true);
                graphics.DrawImage(_shadowBitmap, Bounds);
            }
            else
            {
                graphics.DrawImage(image, Bounds);
            }
        }

        public override bool HasDefaultSize => true;

        public override Size DefaultSize => image?.Size ?? new Size(32, 32);
    }
}