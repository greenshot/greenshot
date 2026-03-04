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
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using log4net;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Description of CursorContainer.
    /// </summary>
    [Serializable]
    public class CursorContainer : DrawableContainer, ICursorContainer
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(CursorContainer));

        [NonSerialized]
        protected CapturedCursor cursor;

        /// <summary>
        /// This is used to serialize the <see cref="cursor"/>
        /// </summary>
        private CaptureCursorSerializationWrapper savedCursor;
        [Serializable]
        public class CaptureCursorSerializationWrapper
        {
            public Bitmap ColorLayer { get; set; }
            public Bitmap MaskLayer { get; set; }
            public int SizeWidth { get; set; }
            public int SizeHeight { get; set; }
            public int HotspotX { get; set; }
            public int HotspotY { get; set; }

            public CaptureCursorSerializationWrapper(CapturedCursor cursor)
            {
                ColorLayer = cursor.ColorLayer;
                MaskLayer = cursor.MaskLayer;
                SizeWidth = cursor.Size.Width;
                SizeHeight = cursor.Size.Height;
                HotspotX = cursor.HotSpot.X;
                HotspotY = cursor.HotSpot.Y;
            }

            public CapturedCursor ToCapturedCursor()
            {
                return new CapturedCursor
                {
                    ColorLayer = ColorLayer,
                    MaskLayer = MaskLayer,
                    Size = new NativeSize(SizeWidth, SizeHeight),
                    HotSpot = new NativePoint(HotspotX, HotspotY)
                };
            }
        }

        public CursorContainer(ISurface parent) : base(parent)
        {
            Init();
        }

        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);

            if (savedCursor != null)
            {
                cursor = savedCursor.ToCapturedCursor();
            }

            Init();
        }

        private void Init()
        {
            CreateDefaultAdorners();
        }

        public CursorContainer(ISurface parent, string filename) : this(parent)
        {
            Load(filename);
        }

        public CapturedCursor Cursor
        {
            set
            {
                if (cursor != null)
                {
                    cursor.Dispose();
                }

                // Clone cursor (is this correct??)
                Width = value.Size.Width;
                Height = value.Size.Height;
                cursor = value;
                savedCursor = new CaptureCursorSerializationWrapper(value);
            }
            get { return cursor; }
        }

        /// <summary>
        /// This Dispose is called from the Dispose and the Destructor.
        /// When disposing==true all non-managed resources should be freed too!
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (cursor != null)
                {
                    cursor.Dispose();
                }
            }

            cursor = null;
            base.Dispose(disposing);
        }

        public void Load(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }

            LOG.Debug("Loaded file: " + filename + " with resolution: " + Height + "," + Width);
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {
            if (cursor == null)
            {
                return;
            }

            CursorHelper.DrawCursorOnGraphics(graphics, cursor, Bounds.Location, Bounds.Size);
        }

        public override void DrawContent(Graphics graphics, Bitmap bmp, RenderMode renderMode, NativeRect clipRectangle)
        {
            if (bmp == null)
            {
                base.DrawContent(graphics, bmp, renderMode, clipRectangle);
                return;
            }

            if (cursor == null)
            {
                return;
            }

            CursorHelper.DrawCursorOnBitmap(bmp, cursor, Bounds.Location, Bounds.Size);
        }

        public override NativeSize DefaultSize => cursor?.Size ?? new NativeSize(16, 16);
    }
}