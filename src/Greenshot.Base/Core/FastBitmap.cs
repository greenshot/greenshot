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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The interface for the FastBitmap
    /// </summary>
    public interface IFastBitmap : IDisposable
    {
        /// <summary>
        /// Get the color at x,y
        /// The returned Color object depends on the underlying pixel format
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <returns>Color</returns>
        Color GetColorAt(int x, int y);

        /// <summary>
        /// Set the color at the specified location
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">Color</param>
        void SetColorAt(int x, int y, Color color);

        /// <summary>
        /// Get the color at x,y
        /// The returned byte[] color depends on the underlying pixel format
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">byte array</param>
        void GetColorAt(int x, int y, byte[] color);

        /// <summary>
        /// Set the color at the specified location
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">byte[] color</param>
        void SetColorAt(int x, int y, byte[] color);

        /// <summary>
        /// Lock the bitmap
        /// </summary>
        void Lock();

        /// <summary>
        /// Unlock the bitmap
        /// </summary>
        void Unlock();

        /// <summary>
        /// Unlock the bitmap and get the underlying bitmap in one call
        /// </summary>
        /// <returns></returns>
        Bitmap UnlockAndReturnBitmap();

        /// <summary>
        /// Size of the underlying image
        /// </summary>
        NativeSize Size { get; }

        /// <summary>
        /// Height of the image area that this fastbitmap covers
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Width of the image area that this fastbitmap covers
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Top of the image area that this fastbitmap covers
        /// </summary>
        int Top { get; }

        /// <summary>
        /// Left of the image area that this fastbitmap covers
        /// </summary>
        int Left { get; }

        /// <summary>
        /// Right of the image area that this fastbitmap covers
        /// </summary>
        int Right { get; }

        /// <summary>
        /// Bottom of the image area that this fastbitmap covers
        /// </summary>
        int Bottom { get; }

        /// <summary>
        /// Does the underlying image need to be disposed
        /// </summary>
        bool NeedsDispose { get; set; }

        /// <summary>
        /// Returns if this FastBitmap has an alpha channel
        /// </summary>
        bool HasAlphaChannel { get; }

        /// <summary>
        /// Draw the stored bitmap to the destination bitmap at the supplied point
        /// </summary>
        /// <param name="graphics">Graphics</param>
        /// <param name="destination">NativePoint with location</param>
        void DrawTo(Graphics graphics, NativePoint destination);

        /// <summary>
        /// Draw the stored Bitmap on the Destination bitmap with the specified rectangle
        /// Be aware that the stored bitmap will be resized to the specified rectangle!!
        /// </summary>
        /// <param name="graphics">Graphics</param>
        /// <param name="destinationRect">NativeRect with destination</param>
        void DrawTo(Graphics graphics, NativeRect destinationRect);

        /// <summary>
        /// Return true if the coordinates are inside the FastBitmap
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        bool Contains(int x, int y);

        /// <summary>
        /// Set the bitmap resolution
        /// </summary>
        /// <param name="horizontal"></param>
        /// <param name="vertical"></param>
        void SetResolution(float horizontal, float vertical);
    }

    /// <summary>
    /// This interface can be used for when offsetting is needed
    /// </summary>
    public interface IFastBitmapWithOffset : IFastBitmap
    {
        /// <summary>
        /// Return true if the coordinates are inside the FastBitmap
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        new bool Contains(int x, int y);

        /// <summary>
        /// Set the color at the specified location, using offsetting so the original coordinates can be used
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">Color color</param>
        new void SetColorAt(int x, int y, Color color);

        /// <summary>
        /// Set the color at the specified location, using offsetting so the original coordinates can be used
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">byte[] color</param>
        new void SetColorAt(int x, int y, byte[] color);

        /// <summary>
        /// Get the color at x,y
        /// The returned Color object depends on the underlying pixel format
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <returns>Color</returns>
        new Color GetColorAt(int x, int y);

        /// <summary>
        /// Get the color at x,y, using offsetting so the original coordinates can be used
        /// The returned byte[] color depends on the underlying pixel format
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">byte array</param>
        new void GetColorAt(int x, int y, byte[] color);

        new int Left { get; set; }

        new int Top { get; set; }
    }

    /// <summary>
    /// This interface can be used for when clipping is needed
    /// </summary>
    public interface IFastBitmapWithClip : IFastBitmap
    {
        NativeRect Clip { get; set; }

        bool InvertClip { get; set; }

        /// <summary>
        /// Set the color at the specified location, this doesn't do anything if the location is excluded due to clipping
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">Color color</param>
        new void SetColorAt(int x, int y, Color color);

        /// <summary>
        /// Set the color at the specified location, this doesn't do anything if the location is excluded due to clipping
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">byte[] color</param>
        new void SetColorAt(int x, int y, byte[] color);

        /// <summary>
        /// Return true if the coordinates are inside the FastBitmap and not clipped
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        new bool Contains(int x, int y);
    }

    /// <summary>
    /// This interface is implemented when there is a alpha-blending possibility
    /// </summary>
    public interface IFastBitmapWithBlend : IFastBitmap
    {
        Color BackgroundBlendColor { get; set; }
        Color GetBlendedColorAt(int x, int y);
    }

    /// <summary>
    /// The base class for the fast bitmap implementation
    /// </summary>
    public abstract unsafe class FastBitmap : IFastBitmapWithClip, IFastBitmapWithOffset
    {
        protected const int PixelformatIndexA = 3;
        protected const int PixelformatIndexR = 2;
        protected const int PixelformatIndexG = 1;
        protected const int PixelformatIndexB = 0;

        public const int ColorIndexR = 0;
        public const int ColorIndexG = 1;
        public const int ColorIndexB = 2;
        public const int ColorIndexA = 3;

        protected NativeRect Area;

        /// <summary>
        /// If this is set to true, the bitmap will be disposed when disposing the IFastBitmap
        /// </summary>
        public bool NeedsDispose { get; set; }

        public NativeRect Clip { get; set; }

        public bool InvertClip { get; set; }

        /// <summary>
        /// The bitmap for which the FastBitmap is creating access
        /// </summary>
        protected Bitmap Bitmap;

        protected BitmapData BmData;
        protected int Stride; /* bytes per pixel row */
        protected bool BitsLocked;
        protected byte* Pointer;

        public static IFastBitmap Create(Bitmap source)
        {
            return Create(source, NativeRect.Empty);
        }

        public void SetResolution(float horizontal, float vertical)
        {
            Bitmap.SetResolution(horizontal, vertical);
        }

        /// <summary>
        /// Factory for creating a FastBitmap depending on the pixelformat of the source
        /// The supplied rectangle specifies the area for which the FastBitmap does its thing
        /// </summary>
        /// <param name="source">Bitmap to access</param>
        /// <param name="area">NativeRect which specifies the area to have access to, can be NativeRect.Empty for the whole image</param>
        /// <returns>IFastBitmap</returns>
        public static IFastBitmap Create(Bitmap source, NativeRect area) =>
            source.PixelFormat switch
            {
                PixelFormat.Format8bppIndexed => new FastChunkyBitmap(source, area),
                PixelFormat.Format24bppRgb => new Fast24RgbBitmap(source, area),
                PixelFormat.Format32bppRgb => new Fast32RgbBitmap(source, area),
                PixelFormat.Format32bppArgb => new Fast32ArgbBitmap(source, area),
                PixelFormat.Format32bppPArgb => new Fast32ArgbBitmap(source, area),
                _ => throw new NotSupportedException($"Not supported PixelFormat {source.PixelFormat}")
            };

        /// <summary>
        /// Factory for creating a FastBitmap as a destination for the source
        /// </summary>
        /// <param name="source">Bitmap to clone</param>
        /// <param name="pixelFormat">new PixelFormat</param>
        /// <returns>IFastBitmap</returns>
        public static IFastBitmap CreateCloneOf(Image source, PixelFormat pixelFormat)
        {
            return CreateCloneOf(source, pixelFormat, NativeRect.Empty);
        }

        /// <summary>
        /// Factory for creating a FastBitmap as a destination for the source
        /// </summary>
        /// <param name="source">Bitmap to clone</param>
        /// <param name="area">Area of the bitmap to access, can be NativeRect.Empty for the whole</param>
        /// <returns>IFastBitmap</returns>
        public static IFastBitmap CreateCloneOf(Image source, NativeRect area)
        {
            return CreateCloneOf(source, PixelFormat.DontCare, area);
        }

        /// <summary>
        /// Factory for creating a FastBitmap as a destination for the source
        /// </summary>
        /// <param name="source">Bitmap to clone</param>
        /// <param name="pixelFormat">Pixelformat of the cloned bitmap</param>
        /// <param name="area">Area of the bitmap to access, can be NativeRect.Empty for the whole</param>
        /// <returns>IFastBitmap</returns>
        public static IFastBitmap CreateCloneOf(Image source, PixelFormat pixelFormat, NativeRect area)
        {
            Bitmap destination = ImageHelper.CloneArea(source, area, pixelFormat);
            FastBitmap fastBitmap = Create(destination) as FastBitmap;
            if (fastBitmap != null)
            {
                fastBitmap.NeedsDispose = true;
                fastBitmap.Left = area.Left;
                fastBitmap.Top = area.Top;
            }

            return fastBitmap;
        }

        /// <summary>
        /// Factory for creating a FastBitmap as a destination
        /// </summary>
        /// <param name="newSize">NativeSize</param>
        /// <param name="pixelFormat">PixelFormat</param>
        /// <param name="backgroundColor">Color</param>
        /// <returns>IFastBitmap</returns>
        public static IFastBitmap CreateEmpty(NativeSize newSize, PixelFormat pixelFormat, Color backgroundColor)
        {
            Bitmap destination = ImageHelper.CreateEmpty(newSize.Width, newSize.Height, pixelFormat, backgroundColor, 96f, 96f);
            IFastBitmap fastBitmap = Create(destination);
            fastBitmap.NeedsDispose = true;
            return fastBitmap;
        }

        /// <summary>
        /// Constructor which stores the image and locks it when called
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <param name="area">NativeRect</param>
        protected FastBitmap(Bitmap bitmap, NativeRect area)
        {
            Bitmap = bitmap;
            var bitmapArea = new NativeRect(NativePoint.Empty, bitmap.Size);
            if (area != NativeRect.Empty)
            {
                area = area.Intersect(bitmapArea);
                Area = area;
            }
            else
            {
                Area = bitmapArea;
            }

            // As the lock takes care that only the specified area is made available we need to calculate the offset
            Left = area.Left;
            Top = area.Top;
            // Default cliping is done to the area without invert
            Clip = Area;
            InvertClip = false;
            // Always lock, so we don't need to do this ourselves
            Lock();
        }

        /// <summary>
        /// Return the size of the image
        /// </summary>
        public NativeSize Size
        {
            get
            {
                if (Area == NativeRect.Empty)
                {
                    return Bitmap.Size;
                }

                return Area.Size;
            }
        }

        /// <summary>
        /// Return the width of the image
        /// </summary>
        public int Width
        {
            get
            {
                if (Area == NativeRect.Empty)
                {
                    return Bitmap.Width;
                }

                return Area.Width;
            }
        }

        /// <summary>
        /// Return the height of the image
        /// </summary>
        public int Height
        {
            get
            {
                if (Area == NativeRect.Empty)
                {
                    return Bitmap.Height;
                }

                return Area.Height;
            }
        }

        private int _left;

        /// <summary>
        /// Return the left of the fastbitmap, this is also used as an offset
        /// </summary>
        public int Left
        {
            get { return 0; }
            set { _left = value; }
        }

        /// <summary>
        /// Return the left of the fastbitmap, this is also used as an offset
        /// </summary>
        int IFastBitmapWithOffset.Left
        {
            get { return _left; }
            set { _left = value; }
        }

        private int _top;

        /// <summary>
        /// Return the top of the fastbitmap, this is also used as an offset
        /// </summary>
        public int Top
        {
            get { return 0; }
            set { _top = value; }
        }

        /// <summary>
        /// Return the top of the fastbitmap, this is also used as an offset
        /// </summary>
        int IFastBitmapWithOffset.Top
        {
            get { return _top; }
            set { _top = value; }
        }

        /// <summary>
        /// Return the right of the fastbitmap
        /// </summary>
        public int Right => Left + Width;

        /// <summary>
        /// Return the bottom of the fastbitmap
        /// </summary>
        public int Bottom => Top + Height;

        /// <summary>
        /// Returns the underlying bitmap, unlocks it and prevents that it will be disposed
        /// </summary>
        public Bitmap UnlockAndReturnBitmap()
        {
            if (BitsLocked)
            {
                Unlock();
            }

            NeedsDispose = false;
            return Bitmap;
        }

        public virtual bool HasAlphaChannel => false;

        /// <summary>
        /// Destructor
        /// </summary>
        ~FastBitmap()
        {
            Dispose(false);
        }

        /// <summary>
        /// The public accessible Dispose
        /// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)

        /// <summary>
        /// This Dispose is called from the Dispose and the Destructor.
        /// When disposing==true all non-managed resources should be freed too!
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            Unlock();
            if (disposing)
            {
                if (Bitmap != null && NeedsDispose)
                {
                    Bitmap.Dispose();
                }
            }

            Bitmap = null;
            BmData = null;
            Pointer = null;
        }

        /// <summary>
        /// Lock the bitmap so we have direct access to the memory
        /// </summary>
        public void Lock()
        {
            if (Width <= 0 || Height <= 0 || BitsLocked)
            {
                return;
            }

            BmData = Bitmap.LockBits(Area, ImageLockMode.ReadWrite, Bitmap.PixelFormat);
            BitsLocked = true;

            IntPtr scan0 = BmData.Scan0;
            Pointer = (byte*) (void*) scan0;
            Stride = BmData.Stride;
        }

        /// <summary>
        /// Unlock the System Memory
        /// </summary>
        public void Unlock()
        {
            if (BitsLocked)
            {
                Bitmap.UnlockBits(BmData);
                BitsLocked = false;
            }
        }

        /// <summary>
        /// Draw the stored bitmap to the destination bitmap at the supplied point
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="destination"></param>
        public void DrawTo(Graphics graphics, NativePoint destination)
        {
            DrawTo(graphics, new NativeRect(destination, Area.Size));
        }

        /// <summary>
        /// Draw the stored Bitmap on the Destination bitmap with the specified rectangle
        /// Be aware that the stored bitmap will be resized to the specified rectangle!!
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="destinationRect">NativeRect</param>
        public void DrawTo(Graphics graphics, NativeRect destinationRect)
        {
            // Make sure this.bitmap is unlocked, if it was locked
            bool isLocked = BitsLocked;
            if (isLocked)
            {
                Unlock();
            }

            graphics.DrawImage(Bitmap, destinationRect, Area, GraphicsUnit.Pixel);
        }

        /// <summary>
        /// returns true if x & y are inside the FastBitmap
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>true if x & y are inside the FastBitmap</returns>
        public bool Contains(int x, int y)
        {
            return Area.Contains(x - Left, y - Top);
        }

        public abstract Color GetColorAt(int x, int y);
        public abstract void SetColorAt(int x, int y, Color color);
        public abstract void GetColorAt(int x, int y, byte[] color);
        public abstract void SetColorAt(int x, int y, byte[] color);

        bool IFastBitmapWithClip.Contains(int x, int y)
        {
            bool contains = Clip.Contains(x, y);
            if (InvertClip)
            {
                return !contains;
            }
            else
            {
                return contains;
            }
        }

        void IFastBitmapWithClip.SetColorAt(int x, int y, byte[] color)
        {
            bool contains = Clip.Contains(x, y);
            if ((InvertClip && contains) || (!InvertClip && !contains))
            {
                return;
            }

            SetColorAt(x, y, color);
        }

        void IFastBitmapWithClip.SetColorAt(int x, int y, Color color)
        {
            bool contains = Clip.Contains(x, y);
            if ((InvertClip && contains) || (!InvertClip && !contains))
            {
                return;
            }

            SetColorAt(x, y, color);
        }

        /// <summary>
        /// returns true if x & y are inside the FastBitmap
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>true if x & y are inside the FastBitmap</returns>
        bool IFastBitmapWithOffset.Contains(int x, int y)
        {
            return Area.Contains(x - Left, y - Top);
        }

        Color IFastBitmapWithOffset.GetColorAt(int x, int y)
        {
            x -= _left;
            y -= _top;
            return GetColorAt(x, y);
        }

        void IFastBitmapWithOffset.GetColorAt(int x, int y, byte[] color)
        {
            x -= _left;
            y -= _top;
            GetColorAt(x, y, color);
        }

        void IFastBitmapWithOffset.SetColorAt(int x, int y, byte[] color)
        {
            x -= _left;
            y -= _top;
            SetColorAt(x, y, color);
        }

        void IFastBitmapWithOffset.SetColorAt(int x, int y, Color color)
        {
            x -= _left;
            y -= _top;
            SetColorAt(x, y, color);
        }
    }

    /// <summary>
    /// This is the implementation of the FastBitmap for the 8BPP pixelformat
    /// </summary>
    public unsafe class FastChunkyBitmap : FastBitmap
    {
        // Used for indexed images
        private readonly Color[] _colorEntries;
        private readonly Dictionary<Color, byte> _colorCache = new Dictionary<Color, byte>();

        public FastChunkyBitmap(Bitmap source, NativeRect area) : base(source, area)
        {
            _colorEntries = Bitmap.Palette.Entries;
        }

        /// <summary>
        /// Get the color from the specified location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>Color</returns>
        public override Color GetColorAt(int x, int y)
        {
            int offset = x + (y * Stride);
            byte colorIndex = Pointer[offset];
            return _colorEntries[colorIndex];
        }

        /// <summary>
        /// Get the color from the specified location into the specified array
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color">byte[4] as reference</param>
        public override void GetColorAt(int x, int y, byte[] color)
        {
            throw new NotImplementedException("No performance gain!");
        }

        /// <summary>
        /// Set the color at the specified location from the specified array
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color">byte[4] as reference</param>
        public override void SetColorAt(int x, int y, byte[] color)
        {
            throw new NotImplementedException("No performance gain!");
        }

        /// <summary>
        /// Get the color-index from the specified location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>byte with index</returns>
        public byte GetColorIndexAt(int x, int y)
        {
            int offset = x + (y * Stride);
            return Pointer[offset];
        }

        /// <summary>
        /// Set the color-index at the specified location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="colorIndex"></param>
        public void SetColorIndexAt(int x, int y, byte colorIndex)
        {
            int offset = x + (y * Stride);
            Pointer[offset] = colorIndex;
        }

        /// <summary>
        /// Set the supplied color at the specified location.
        /// Throws an ArgumentException if the color is not in the palette
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color">Color to set</param>
        public override void SetColorAt(int x, int y, Color color)
        {
            int offset = x + (y * Stride);
            if (!_colorCache.TryGetValue(color, out var colorIndex))
            {
                bool foundColor = false;
                for (colorIndex = 0; colorIndex < _colorEntries.Length; colorIndex++)
                {
                    if (color == _colorEntries[colorIndex])
                    {
                        _colorCache.Add(color, colorIndex);
                        foundColor = true;
                        break;
                    }
                }

                if (!foundColor)
                {
                    throw new ArgumentException("No such color!");
                }
            }

            Pointer[offset] = colorIndex;
        }
    }

    /// <summary>
    /// This is the implementation of the IFastBitmap for 24 bit images (no Alpha)
    /// </summary>
    public unsafe class Fast24RgbBitmap : FastBitmap
    {
        public Fast24RgbBitmap(Bitmap source, NativeRect area) : base(source, area)
        {
        }

        /// <summary>
        /// Retrieve the color at location x,y
        /// Before the first time this is called the Lock() should be called once!
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y Coordinate</param>
        /// <returns>Color</returns>
        public override Color GetColorAt(int x, int y)
        {
            int offset = (x * 3) + (y * Stride);
            return Color.FromArgb(255, Pointer[PixelformatIndexR + offset], Pointer[PixelformatIndexG + offset], Pointer[PixelformatIndexB + offset]);
        }

        /// <summary>
        /// Set the color at location x,y
        /// Before the first time this is called the Lock() should be called once!
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public override void SetColorAt(int x, int y, Color color)
        {
            int offset = (x * 3) + (y * Stride);
            Pointer[PixelformatIndexR + offset] = color.R;
            Pointer[PixelformatIndexG + offset] = color.G;
            Pointer[PixelformatIndexB + offset] = color.B;
        }

        /// <summary>
        /// Get the color from the specified location into the specified array
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color">byte[4] as reference (r,g,b)</param>
        public override void GetColorAt(int x, int y, byte[] color)
        {
            int offset = (x * 3) + (y * Stride);
            color[PixelformatIndexR] = Pointer[PixelformatIndexR + offset];
            color[PixelformatIndexG] = Pointer[PixelformatIndexG + offset];
            color[PixelformatIndexB] = Pointer[PixelformatIndexB + offset];
        }

        /// <summary>
        /// Set the color at the specified location from the specified array
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color">byte[4] as reference (r,g,b)</param>
        public override void SetColorAt(int x, int y, byte[] color)
        {
            int offset = (x * 3) + (y * Stride);
            Pointer[PixelformatIndexR + offset] = color[PixelformatIndexR];
            Pointer[PixelformatIndexG + offset] = color[PixelformatIndexG];
            Pointer[PixelformatIndexB + offset] = color[PixelformatIndexB];
        }
    }

    /// <summary>
    /// This is the implementation of the IFastBitmap for 32 bit images (no Alpha)
    /// </summary>
    public unsafe class Fast32RgbBitmap : FastBitmap
    {
        public Fast32RgbBitmap(Bitmap source, NativeRect area) : base(source, area)
        {
        }

        /// <summary>
        /// Retrieve the color at location x,y
        /// Before the first time this is called the Lock() should be called once!
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y Coordinate</param>
        /// <returns>Color</returns>
        public override Color GetColorAt(int x, int y)
        {
            int offset = (x * 4) + (y * Stride);
            return Color.FromArgb(255, Pointer[PixelformatIndexR + offset], Pointer[PixelformatIndexG + offset], Pointer[PixelformatIndexB + offset]);
        }

        /// <summary>
        /// Set the color at location x,y
        /// Before the first time this is called the Lock() should be called once!
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public override void SetColorAt(int x, int y, Color color)
        {
            int offset = (x * 4) + (y * Stride);
            Pointer[PixelformatIndexR + offset] = color.R;
            Pointer[PixelformatIndexG + offset] = color.G;
            Pointer[PixelformatIndexB + offset] = color.B;
        }

        /// <summary>
        /// Get the color from the specified location into the specified array
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color">byte[4] as reference (a,r,g,b)</param>
        public override void GetColorAt(int x, int y, byte[] color)
        {
            int offset = (x * 4) + (y * Stride);
            color[ColorIndexR] = Pointer[PixelformatIndexR + offset];
            color[ColorIndexG] = Pointer[PixelformatIndexG + offset];
            color[ColorIndexB] = Pointer[PixelformatIndexB + offset];
        }

        /// <summary>
        /// Set the color at the specified location from the specified array
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color">byte[4] as reference (r,g,b)</param>
        public override void SetColorAt(int x, int y, byte[] color)
        {
            int offset = (x * 4) + (y * Stride);
            Pointer[PixelformatIndexR + offset] = color[ColorIndexR]; // R
            Pointer[PixelformatIndexG + offset] = color[ColorIndexG];
            Pointer[PixelformatIndexB + offset] = color[ColorIndexB];
        }
    }

    /// <summary>
    /// This is the implementation of the IFastBitmap for 32 bit images with Alpha
    /// </summary>
    public unsafe class Fast32ArgbBitmap : FastBitmap, IFastBitmapWithBlend
    {
        public override bool HasAlphaChannel => true;

        public Color BackgroundBlendColor { get; set; }

        public Fast32ArgbBitmap(Bitmap source, NativeRect area) : base(source, area)
        {
            BackgroundBlendColor = Color.White;
        }

        /// <summary>
        /// Retrieve the color at location x,y
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y Coordinate</param>
        /// <returns>Color</returns>
        public override Color GetColorAt(int x, int y)
        {
            int offset = (x * 4) + (y * Stride);
            return Color.FromArgb(Pointer[PixelformatIndexA + offset], Pointer[PixelformatIndexR + offset], Pointer[PixelformatIndexG + offset],
                Pointer[PixelformatIndexB + offset]);
        }

        /// <summary>
        /// Set the color at location x,y
        /// Before the first time this is called the Lock() should be called once!
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public override void SetColorAt(int x, int y, Color color)
        {
            int offset = (x * 4) + (y * Stride);
            Pointer[PixelformatIndexA + offset] = color.A;
            Pointer[PixelformatIndexR + offset] = color.R;
            Pointer[PixelformatIndexG + offset] = color.G;
            Pointer[PixelformatIndexB + offset] = color.B;
        }

        /// <summary>
        /// Get the color from the specified location into the specified array
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color">byte[4] as reference (r,g,b,a)</param>
        public override void GetColorAt(int x, int y, byte[] color)
        {
            int offset = (x * 4) + (y * Stride);
            color[ColorIndexR] = Pointer[PixelformatIndexR + offset];
            color[ColorIndexG] = Pointer[PixelformatIndexG + offset];
            color[ColorIndexB] = Pointer[PixelformatIndexB + offset];
            color[ColorIndexA] = Pointer[PixelformatIndexA + offset];
        }

        /// <summary>
        /// Set the color at the specified location from the specified array
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color">byte[4] as reference (r,g,b,a)</param>
        public override void SetColorAt(int x, int y, byte[] color)
        {
            int offset = (x * 4) + (y * Stride);
            Pointer[PixelformatIndexR + offset] = color[ColorIndexR]; // R
            Pointer[PixelformatIndexG + offset] = color[ColorIndexG];
            Pointer[PixelformatIndexB + offset] = color[ColorIndexB];
            Pointer[PixelformatIndexA + offset] = color[ColorIndexA];
        }

        /// <summary>
        /// Retrieve the color, without alpha (is blended), at location x,y
        /// Before the first time this is called the Lock() should be called once!
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y Coordinate</param>
        /// <returns>Color</returns>
        public Color GetBlendedColorAt(int x, int y)
        {
            int offset = (x * 4) + (y * Stride);
            int a = Pointer[PixelformatIndexA + offset];
            int red = Pointer[PixelformatIndexR + offset];
            int green = Pointer[PixelformatIndexG + offset];
            int blue = Pointer[PixelformatIndexB + offset];

            if (a < 255)
            {
                // As the request is to get without alpha, we blend.
                int rem = 255 - a;
                red = (red * a + BackgroundBlendColor.R * rem) / 255;
                green = (green * a + BackgroundBlendColor.G * rem) / 255;
                blue = (blue * a + BackgroundBlendColor.B * rem) / 255;
            }

            return Color.FromArgb(255, red, green, blue);
        }
    }
}