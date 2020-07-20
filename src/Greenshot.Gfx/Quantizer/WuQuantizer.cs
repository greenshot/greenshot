// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
//
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Greenshot.Gfx.Structs;

namespace Greenshot.Gfx.Quantizer
{
	/// <summary>
	///     Implementation of the WuQuantizer algorithm
	/// </summary>
	public class WuQuantizer<TPixel> : IDisposable where TPixel : unmanaged
	{
		private const int Maxcolor = 512;
		private const int Red = 2;
		private const int Green = 1;
		private const int Blue = 0;
		private const int Sidesize = 33;
		private const int Maxsideindex = 32;
		private const int Maxvolume = Sidesize * Sidesize * Sidesize;

		// To count the colors
		private readonly int _colorCount;

		private readonly WuColorCube[] _cubes;
		private readonly float[,,] _moments;
		private readonly long[,,] _momentsBlue;
		private readonly long[,,] _momentsGreen;
		private readonly long[,,] _momentsRed;
		private readonly UnmanagedBitmap<TPixel> _sourceBitmap;

		private readonly long[,,] _weights;
		private ChunkyBitmap _resultBitmap;

        /// <summary>
        /// The constructor for the WuQauntizer
        /// </summary>
        /// <param name="sourceBitmap">IBitmapWithNativeSupport</param>
        public WuQuantizer(IBitmapWithNativeSupport sourceBitmap) : this(sourceBitmap as UnmanagedBitmap<TPixel>)
        {

        }

        /// <summary>
        /// The constructor for the WuQauntizer
        /// </summary>
        /// <param name="sourceBitmap">UnmanagedBitmap of TPixel</param>
        public WuQuantizer(UnmanagedBitmap<TPixel> sourceBitmap)
		{
			_sourceBitmap = sourceBitmap;
			// Make sure the color count variables are reset
			var bitArray = new BitArray((int) Math.Pow(2, 24));
			_colorCount = 0;

			// creates all the cubes
			_cubes = new WuColorCube[Maxcolor];

			// resets the reference minimums
			_cubes[0].RedMinimum = 0;
			_cubes[0].GreenMinimum = 0;
			_cubes[0].BlueMinimum = 0;

			// resets the reference maximums
			_cubes[0].RedMaximum = Maxsideindex;
			_cubes[0].GreenMaximum = Maxsideindex;
			_cubes[0].BlueMaximum = Maxsideindex;

			_weights = new long[Sidesize, Sidesize, Sidesize];
			_momentsRed = new long[Sidesize, Sidesize, Sidesize];
			_momentsGreen = new long[Sidesize, Sidesize, Sidesize];
			_momentsBlue = new long[Sidesize, Sidesize, Sidesize];
			_moments = new float[Sidesize, Sidesize, Sidesize];

            Span<int> table = stackalloc int[256];
            for (var tableIndex = 0; tableIndex < 256; ++tableIndex)
			{
				table[tableIndex] = tableIndex * tableIndex;
			}

            _resultBitmap = new ChunkyBitmap(sourceBitmap.Width, sourceBitmap.Height);
            for (var y = 0; y < sourceBitmap.Height; y++)
            {
                var srcPixelSpan = MemoryMarshal.Cast<TPixel, Bgra32>(_sourceBitmap[y]);
                var destPixelSpan = _resultBitmap[y];
                for (var x = 0; x < sourceBitmap.Width; x++)
                {
                    var color = srcPixelSpan[x];

                    // To count the colors
                    var index = (color.B << 16) | (color.G << 8) | color.R;
                    // Check if we already have this color
                    if (!bitArray.Get(index))
                    {
                        // If not, add 1 to the single colors
                        _colorCount++;
                        bitArray.Set(index, true);
                    }

                    var indexRed = (color.R >> 3) + 1;
                    var indexGreen = (color.G >> 3) + 1;
                    var indexBlue = (color.B >> 3) + 1;

                    _weights[indexRed, indexGreen, indexBlue]++;
                    _momentsRed[indexRed, indexGreen, indexBlue] += color.R;
                    _momentsGreen[indexRed, indexGreen, indexBlue] += color.G;
                    _momentsBlue[indexRed, indexGreen, indexBlue] += color.B;
                    _moments[indexRed, indexGreen, indexBlue] += table[color.R] + table[color.G] + table[color.B];

                    // Store the initial "match"
                    var paletteIndex = (indexRed << 10) + (indexRed << 6) + indexRed + (indexGreen << 5) + indexGreen + indexBlue;
                    destPixelSpan[x] = (byte)(paletteIndex & 0xff);
                }
            }
        }

        /// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
		}


		/// <summary>
		/// Dispose implementation
		/// </summary>
		/// <param name="disposing">bool</param>
		protected virtual void Dispose(bool disposing)
		{
		    if (!disposing)
		    {
		        return;
		    }

		    if (_resultBitmap == null)
		    {
		        return;
		    }

		    _resultBitmap.Dispose();
		    _resultBitmap = null;
		}

        /// <summary>
        /// Returns the number of colors
        /// </summary>
        /// <returns>int</returns>
		public int GetColorCount()
		{
			return _colorCount;
		}

		/// <summary>
		///     Reindex the 24/32 BPP (A)RGB image to a 8BPP
		/// </summary>
		/// <returns>Bitmap</returns>
		public IBitmapWithNativeSupport SimpleReindex()
		{
            Span<uint> colors = stackalloc uint[256];
			var lookup = new Dictionary<uint, byte>();

            byte colorCount = 0;
            for (var y = 0; y < _sourceBitmap.Height; y++)
            {
                var srcSpan = MemoryMarshal.Cast<TPixel, uint>(_sourceBitmap[y]);
                var destSpan = _resultBitmap[y];
                for (var x = 0; x < _sourceBitmap.Width; x++)
                {
                    var color = srcSpan[x];
                    byte index;
                    if (lookup.ContainsKey(color))
                    {
                        index = lookup[color];
                    }
                    else
                    {
                        colors[colorCount] = color;
                        index = colorCount;
                        colorCount++;
                        lookup.Add(color, index);
                    }
                    destSpan[x] = index;
                }
            }

			// generates palette
			var imagePalette = _resultBitmap.NativeBitmap.Palette;
			var entries = imagePalette.Entries;
			for (var paletteIndex = 0; paletteIndex < 256; paletteIndex++)
			{
				if (paletteIndex < colorCount)
				{
					entries[paletteIndex] = colors[paletteIndex].ToColor();
				}
				else
				{
					entries[paletteIndex] = Color.Black;
				}
			}
			_resultBitmap.NativeBitmap.Palette = imagePalette;

			// Make sure the bitmap is not disposed, as we return it.
			var tmpBitmap = _resultBitmap;
			_resultBitmap = null;
			return tmpBitmap;
		}

		/// <summary>
		///     Get the image
		/// </summary>
		public IBitmapWithNativeSupport GetQuantizedImage(int allowedColorCount = 256)
		{
			if (allowedColorCount > 256)
			{
				throw new ArgumentOutOfRangeException(nameof(allowedColorCount), "Quantizing muss be done to get less than 256 colors");
			}
			if (_colorCount < allowedColorCount)
			{
				// Simple logic to reduce to 8 bit
				return SimpleReindex();
			}
			// preprocess the colors
			CalculateMoments();
			var next = 0;
			var volumeVariance = new float[Maxcolor];

			// processes the cubes
			for (var cubeIndex = 1; cubeIndex < allowedColorCount; ++cubeIndex)
			{
				// if cut is possible; make it
				if (Cut(ref _cubes[next], ref _cubes[cubeIndex]))
				{
					volumeVariance[next] = _cubes[next].Volume > 1 ? CalculateVariance(_cubes[next]) : 0.0f;
					volumeVariance[cubeIndex] = _cubes[cubeIndex].Volume > 1 ? CalculateVariance(_cubes[cubeIndex]) : 0.0f;
				}
				else
				{
					// the cut was not possible, revert the index
					volumeVariance[next] = 0.0f;
					cubeIndex--;
				}

				next = 0;
				var temp = volumeVariance[0];

				for (var index = 1; index <= cubeIndex; ++index)
				{
					if (volumeVariance[index] <= temp)
					{
						continue;
					}
					temp = volumeVariance[index];
					next = index;
				}

				if (temp > 0.0)
				{
					continue;
				}
				allowedColorCount = cubeIndex + 1;
				break;
			}

			Span<int> lookupRed = stackalloc int[Maxcolor];
            Span<int> lookupGreen = stackalloc int[Maxcolor];
            Span<int> lookupBlue = stackalloc int[Maxcolor];

			Span<byte> tag = stackalloc byte[Maxvolume];

			// pre-calculates lookup tables
			for (var k = 0; k < allowedColorCount; ++k)
			{
				Mark(_cubes[k], k, tag);

				var weight = Volume(_cubes[k], _weights);

				if (weight > 0)
				{
					lookupRed[k] = (int) (Volume(_cubes[k], _momentsRed) / weight);
					lookupGreen[k] = (int) (Volume(_cubes[k], _momentsGreen) / weight);
					lookupBlue[k] = (int) (Volume(_cubes[k], _momentsBlue) / weight);
				}
				else
				{
					lookupRed[k] = 0;
					lookupGreen[k] = 0;
					lookupBlue[k] = 0;
				}
			}

			Span<int> reds = stackalloc int[allowedColorCount + 1];
            Span<int> greens = stackalloc int[allowedColorCount + 1];
            Span<int> blues = stackalloc int[allowedColorCount + 1];
            Span<int> sums = stackalloc int[allowedColorCount + 1];


            var lookup = new Dictionary<Bgra32, byte>();
            for (var y = 0; y < _sourceBitmap.Height; y++)
            {
                var destSpan = _resultBitmap[y];
                var srcSpan = MemoryMarshal.Cast<TPixel, Bgra32>(_sourceBitmap[y]);
                for (var x = 0; x < _sourceBitmap.Width; x++)
                {
                    var color = srcSpan[x];
                    // Check if we already matched the color
                    byte bestMatch;
                    if (!lookup.ContainsKey(color))
                    {
                        // If not we need to find the best match

                        // First get initial match
                        bestMatch = destSpan[x];
                        bestMatch = tag[bestMatch];

                        var bestDistance = 100000000;
                        for (var lookupIndex = 0; lookupIndex < allowedColorCount; lookupIndex++)
                        {
                            var foundRed = lookupRed[lookupIndex];
                            var foundGreen = lookupGreen[lookupIndex];
                            var foundBlue = lookupBlue[lookupIndex];
                            var deltaRed = color.R - foundRed;
                            var deltaGreen = color.G - foundGreen;
                            var deltaBlue = color.B - foundBlue;

                            var distance = deltaRed * deltaRed + deltaGreen * deltaGreen + deltaBlue * deltaBlue;

                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestMatch = (byte) lookupIndex;
                            }
                        }
                        lookup.Add(color, bestMatch);
                    }
                    else
                    {
                        // Already matched, so we just use the lookup
                        bestMatch = lookup[color];
                    }

                    reds[bestMatch] += color.R;
                    greens[bestMatch] += color.G;
                    blues[bestMatch] += color.B;
                    sums[bestMatch]++;

                    destSpan[x] = bestMatch;
                }
            }

			// generates palette
			var imagePalette = _resultBitmap.NativeBitmap.Palette;
			var entries = imagePalette.Entries;
			for (var paletteIndex = 0; paletteIndex < allowedColorCount; paletteIndex++)
			{
				if (sums[paletteIndex] > 0)
				{
					reds[paletteIndex] /= sums[paletteIndex];
					greens[paletteIndex] /= sums[paletteIndex];
					blues[paletteIndex] /= sums[paletteIndex];
				}

				entries[paletteIndex] = Color.FromArgb(255, reds[paletteIndex], greens[paletteIndex], blues[paletteIndex]);
			}
			_resultBitmap.NativeBitmap.Palette = imagePalette;

			// Make sure the bitmap is not disposed, as we return it.
			var tmpBitmap = _resultBitmap;
			_resultBitmap = null;
			return tmpBitmap;
		}

		/// <summary>
		///     Converts the histogram to a series of moments.
		/// </summary>
		private void CalculateMoments()
		{
			Span<long> area = stackalloc long[Sidesize];
            Span<long> areaRed = stackalloc long[Sidesize];
            Span<long> areaGreen = stackalloc long[Sidesize];
            Span<long> areaBlue = stackalloc long[Sidesize];
            var area2 = new float[Sidesize];

			for (var redIndex = 1; redIndex <= Maxsideindex; ++redIndex)
			{
				for (var index = 0; index <= Maxsideindex; ++index)
				{
					area[index] = 0;
					areaRed[index] = 0;
					areaGreen[index] = 0;
					areaBlue[index] = 0;
					area2[index] = 0;
				}

				for (var greenIndex = 1; greenIndex <= Maxsideindex; ++greenIndex)
				{
					long line = 0;
					long lineRed = 0;
					long lineGreen = 0;
					long lineBlue = 0;
					var line2 = 0.0f;

					for (var blueIndex = 1; blueIndex <= Maxsideindex; ++blueIndex)
					{
						line += _weights[redIndex, greenIndex, blueIndex];
						lineRed += _momentsRed[redIndex, greenIndex, blueIndex];
						lineGreen += _momentsGreen[redIndex, greenIndex, blueIndex];
						lineBlue += _momentsBlue[redIndex, greenIndex, blueIndex];
						line2 += _moments[redIndex, greenIndex, blueIndex];

						area[blueIndex] += line;
						areaRed[blueIndex] += lineRed;
						areaGreen[blueIndex] += lineGreen;
						areaBlue[blueIndex] += lineBlue;
						area2[blueIndex] += line2;

						_weights[redIndex, greenIndex, blueIndex] = _weights[redIndex - 1, greenIndex, blueIndex] + area[blueIndex];
						_momentsRed[redIndex, greenIndex, blueIndex] = _momentsRed[redIndex - 1, greenIndex, blueIndex] + areaRed[blueIndex];
						_momentsGreen[redIndex, greenIndex, blueIndex] = _momentsGreen[redIndex - 1, greenIndex, blueIndex] + areaGreen[blueIndex];
						_momentsBlue[redIndex, greenIndex, blueIndex] = _momentsBlue[redIndex - 1, greenIndex, blueIndex] + areaBlue[blueIndex];
						_moments[redIndex, greenIndex, blueIndex] = _moments[redIndex - 1, greenIndex, blueIndex] + area2[blueIndex];
					}
				}
			}
		}

        /// <summary>
        ///     Computes the volume of the cube in a specific moment.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Volume(in WuColorCube cube, long[,,] moment)
		{
			return moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMaximum] -
			       moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] -
			       moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] +
			       moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] -
			       moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] +
			       moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] +
			       moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] -
			       moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum];
		}

        /// <summary>
        ///     Computes the volume of the cube in a specific moment. For the floating-point values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float VolumeFloat(in WuColorCube cube, float[,,] moment)
		{
			return moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMaximum] -
			       moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] -
			       moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] +
			       moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] -
			       moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] +
			       moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] +
			       moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] -
			       moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum];
		}

        /// <summary>
        ///     Splits the cube in given position, and color direction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Top(in WuColorCube cube, int direction, int position, long[,,] moment) =>
            direction switch
            {
                Red => (moment[position, cube.GreenMaximum, cube.BlueMaximum] -
                        moment[position, cube.GreenMaximum, cube.BlueMinimum] -
                        moment[position, cube.GreenMinimum, cube.BlueMaximum] +
                        moment[position, cube.GreenMinimum, cube.BlueMinimum]),
                Green => (moment[cube.RedMaximum, position, cube.BlueMaximum] -
                          moment[cube.RedMaximum, position, cube.BlueMinimum] -
                          moment[cube.RedMinimum, position, cube.BlueMaximum] +
                          moment[cube.RedMinimum, position, cube.BlueMinimum]),
                Blue => (moment[cube.RedMaximum, cube.GreenMaximum, position] -
                         moment[cube.RedMaximum, cube.GreenMinimum, position] -
                         moment[cube.RedMinimum, cube.GreenMaximum, position] +
                         moment[cube.RedMinimum, cube.GreenMinimum, position]),
                _ => 0
            };

        /// <summary>
		///     Splits the cube in a given color direction at its minimum.
		/// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Bottom(in WuColorCube cube, int direction, long[,,] moment) =>
            direction switch
            {
                Red => (-moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] +
                        moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] +
                        moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] -
                        moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]),
                Green => (-moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] +
                          moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] +
                          moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] -
                          moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]),
                Blue => (-moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] +
                         moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] +
                         moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] -
                         moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]),
                _ => 0
            };

        /// <summary>
		///     Calculates statistical variance for a given cube.
		/// </summary>
		private float CalculateVariance(in WuColorCube cube)
		{
			float volumeRed = Volume(cube, _momentsRed);
			float volumeGreen = Volume(cube, _momentsGreen);
			float volumeBlue = Volume(cube, _momentsBlue);
			var volumeMoment = VolumeFloat(cube, _moments);
			float volumeWeight = Volume(cube, _weights);

			var distance = volumeRed * volumeRed + volumeGreen * volumeGreen + volumeBlue * volumeBlue;

			return volumeMoment - distance / volumeWeight;
		}

		/// <summary>
		///     Finds the optimal (maximal) position for the cut.
		/// </summary>
		private float Maximize(in WuColorCube cube, int direction, int first, int last, int[] cut, long wholeRed, long wholeGreen, long wholeBlue, long wholeWeight)
		{
			var bottomRed = Bottom(cube, direction, _momentsRed);
			var bottomGreen = Bottom(cube, direction, _momentsGreen);
			var bottomBlue = Bottom(cube, direction, _momentsBlue);
			var bottomWeight = Bottom(cube, direction, _weights);

			var result = 0.0f;
			cut[0] = -1;

			for (var position = first; position < last; ++position)
			{
				// determines the cube cut at a certain position
				var halfRed = bottomRed + Top(cube, direction, position, _momentsRed);
				var halfGreen = bottomGreen + Top(cube, direction, position, _momentsGreen);
				var halfBlue = bottomBlue + Top(cube, direction, position, _momentsBlue);
				var halfWeight = bottomWeight + Top(cube, direction, position, _weights);

				// the cube cannot be cut at bottom (this would lead to empty cube)
				if (halfWeight != 0)
				{
					var halfDistance = (float) halfRed * halfRed + (float) halfGreen * halfGreen + (float) halfBlue * halfBlue;
					var temp = halfDistance / halfWeight;

					halfRed = wholeRed - halfRed;
					halfGreen = wholeGreen - halfGreen;
					halfBlue = wholeBlue - halfBlue;
					halfWeight = wholeWeight - halfWeight;

					if (halfWeight != 0)
					{
						halfDistance = (float) halfRed * halfRed + (float) halfGreen * halfGreen + (float) halfBlue * halfBlue;
						temp += halfDistance / halfWeight;

						if (temp > result)
						{
							result = temp;
							cut[0] = position;
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		///     Cuts a cube with another one.
		/// </summary>
		private bool Cut(ref WuColorCube first, ref WuColorCube second)
		{
			int direction;

			int[] cutRed = {0};
			int[] cutGreen = {0};
			int[] cutBlue = {0};

			var wholeRed = Volume(first, _momentsRed);
			var wholeGreen = Volume(first, _momentsGreen);
			var wholeBlue = Volume(first, _momentsBlue);
			var wholeWeight = Volume(first, _weights);

			var maxRed = Maximize(first, Red, first.RedMinimum + 1, first.RedMaximum, cutRed, wholeRed, wholeGreen, wholeBlue, wholeWeight);
			var maxGreen = Maximize(first, Green, first.GreenMinimum + 1, first.GreenMaximum, cutGreen, wholeRed, wholeGreen, wholeBlue, wholeWeight);
			var maxBlue = Maximize(first, Blue, first.BlueMinimum + 1, first.BlueMaximum, cutBlue, wholeRed, wholeGreen, wholeBlue, wholeWeight);

			if (maxRed >= maxGreen && maxRed >= maxBlue)
			{
				direction = Red;

				// cannot split empty cube
				if (cutRed[0] < 0)
				{
					return false;
				}
			}
			else
			{
				if (maxGreen >= maxRed && maxGreen >= maxBlue)
				{
					direction = Green;
				}
				else
				{
					direction = Blue;
				}
			}

			second.RedMaximum = first.RedMaximum;
			second.GreenMaximum = first.GreenMaximum;
			second.BlueMaximum = first.BlueMaximum;

			// cuts in a certain direction
			switch (direction)
			{
				case Red:
					second.RedMinimum = first.RedMaximum = cutRed[0];
					second.GreenMinimum = first.GreenMinimum;
					second.BlueMinimum = first.BlueMinimum;
					break;

				case Green:
					second.GreenMinimum = first.GreenMaximum = cutGreen[0];
					second.RedMinimum = first.RedMinimum;
					second.BlueMinimum = first.BlueMinimum;
					break;

				case Blue:
					second.BlueMinimum = first.BlueMaximum = cutBlue[0];
					second.RedMinimum = first.RedMinimum;
					second.GreenMinimum = first.GreenMinimum;
					break;
			}

			// determines the volumes after cut
			first.Volume = (first.RedMaximum - first.RedMinimum) * (first.GreenMaximum - first.GreenMinimum) * (first.BlueMaximum - first.BlueMinimum);
			second.Volume = (second.RedMaximum - second.RedMinimum) * (second.GreenMaximum - second.GreenMinimum) * (second.BlueMaximum - second.BlueMinimum);

			// the cut was successfull
			return true;
		}

		/// <summary>
		///     Marks all the tags with a given label.
		/// </summary>
		private static void Mark(in WuColorCube cube, int label, Span<byte> tag)
		{
			for (var redIndex = cube.RedMinimum + 1; redIndex <= cube.RedMaximum; ++redIndex)
			{
				for (var greenIndex = cube.GreenMinimum + 1; greenIndex <= cube.GreenMaximum; ++greenIndex)
				{
					for (var blueIndex = cube.BlueMinimum + 1; blueIndex <= cube.BlueMaximum; ++blueIndex)
					{
						tag[(redIndex << 10) + (redIndex << 6) + redIndex + (greenIndex << 5) + greenIndex + blueIndex] = (byte) label;
					}
				}
			}
		}
	}
}