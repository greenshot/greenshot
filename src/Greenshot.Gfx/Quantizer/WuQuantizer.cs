// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Imaging;
using Dapplo.Log;
using Greenshot.Gfx.FastBitmap;

namespace Greenshot.Gfx.Quantizer
{
	/// <summary>
	///     Implementation of the WuQuantizer algorithm
	/// </summary>
	public class WuQuantizer : IDisposable
	{
		private const int Maxcolor = 512;
		private const int Red = 2;
		private const int Green = 1;
		private const int Blue = 0;
		private const int Sidesize = 33;
		private const int Maxsideindex = 32;
		private const int Maxvolume = Sidesize * Sidesize * Sidesize;
		private static readonly LogSource Log = new LogSource();

		// To count the colors
		private readonly int _colorCount;

		private readonly WuColorCube[] _cubes;
		private readonly float[,,] _moments;
		private readonly long[,,] _momentsBlue;
		private readonly long[,,] _momentsGreen;
		private readonly long[,,] _momentsRed;
		private readonly IBitmapWithNativeSupport _sourceBitmap;

		private readonly long[,,] _weights;
		private int[] _blues;
		private int[] _greens;

		private int[] _reds;
		private IBitmapWithNativeSupport _resultBitmap;
		private int[] _sums;

		private byte[] _tag;

        /// <summary>
        /// The constructor for the WuQauntizer
        /// </summary>
        /// <param name="sourceBitmap"></param>
		public WuQuantizer(IBitmapWithNativeSupport sourceBitmap)
		{
			_sourceBitmap = sourceBitmap;
			// Make sure the color count variables are reset
			var bitArray = new BitArray((int) Math.Pow(2, 24));
			_colorCount = 0;

			// creates all the cubes
			_cubes = new WuColorCube[Maxcolor];

			// initializes all the cubes
			for (var cubeIndex = 0; cubeIndex < Maxcolor; cubeIndex++)
			{
				_cubes[cubeIndex] = new WuColorCube();
			}

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

			var table = new int[256];

			for (var tableIndex = 0; tableIndex < 256; ++tableIndex)
			{
				table[tableIndex] = tableIndex * tableIndex;
			}

			// Use a bitmap to store the initial match, which is just as good as an array and saves us 2x the storage
			using (var sourceFastBitmap = FastBitmapFactory.Create(sourceBitmap))
			{
                sourceFastBitmap.Lock();
                using (var destinationFastBitmap = FastBitmapFactory.CreateEmpty(sourceBitmap.Size, PixelFormat.Format8bppIndexed, Color.White) as FastChunkyBitmap)
				{
					for (var y = 0; y < sourceFastBitmap.Height; y++)
					{
						for (var x = 0; x < sourceFastBitmap.Width; x++)
						{
							Color color;
							if (!(sourceFastBitmap is IFastBitmapWithBlend sourceFastBitmapWithBlend))
							{
								color = sourceFastBitmap.GetColorAt(x, y);
							}
							else
							{
								color = sourceFastBitmapWithBlend.GetBlendedColorAt(x, y);
							}
							// To count the colors
							var index = color.ToArgb() & 0x00ffffff;
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
							destinationFastBitmap.SetColorIndexAt(x, y, (byte) (paletteIndex & 0xff));
						}
					}
					_resultBitmap = destinationFastBitmap.UnlockAndReturnBitmap();
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
			var colors = new List<Color>();
			var lookup = new Dictionary<Color, byte>();
			using (var bbbDest = FastBitmapFactory.Create(_resultBitmap) as FastChunkyBitmap)
			{
				bbbDest.Lock();
				using (var bbbSrc = FastBitmapFactory.Create(_sourceBitmap))
				{
                    bbbSrc.Lock();
                    for (var y = 0; y < bbbSrc.Height; y++)
					{
						for (var x = 0; x < bbbSrc.Width; x++)
						{
							Color color;
							if (bbbSrc is IFastBitmapWithBlend bbbSrcBlend)
							{
								color = bbbSrcBlend.GetBlendedColorAt(x, y);
							}
							else
							{
								color = bbbSrc.GetColorAt(x, y);
							}
							byte index;
							if (lookup.ContainsKey(color))
							{
								index = lookup[color];
							}
							else
							{
								colors.Add(color);
								index = (byte) (colors.Count - 1);
								lookup.Add(color, index);
							}
							bbbDest.SetColorIndexAt(x, y, index);
						}
					}
				}
			}

			// generates palette
			var imagePalette = _resultBitmap.NativeBitmap.Palette;
			var entries = imagePalette.Entries;
			for (var paletteIndex = 0; paletteIndex < 256; paletteIndex++)
			{
				if (paletteIndex < _colorCount)
				{
					entries[paletteIndex] = colors[paletteIndex];
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
				Log.Info().WriteLine("Colors in the image are already less as whished for, using simple copy to indexed image, no quantizing needed!");
				return SimpleReindex();
			}
			// preprocess the colors
			CalculateMoments();
			Log.Info().WriteLine("Calculated the moments...");
			var next = 0;
			var volumeVariance = new float[Maxcolor];

			// processes the cubes
			for (var cubeIndex = 1; cubeIndex < allowedColorCount; ++cubeIndex)
			{
				// if cut is possible; make it
				if (Cut(_cubes[next], _cubes[cubeIndex]))
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

			var lookupRed = new int[Maxcolor];
			var lookupGreen = new int[Maxcolor];
			var lookupBlue = new int[Maxcolor];

			_tag = new byte[Maxvolume];

			// pre-calculates lookup tables
			for (var k = 0; k < allowedColorCount; ++k)
			{
				Mark(_cubes[k], k, _tag);

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

			_reds = new int[allowedColorCount + 1];
			_greens = new int[allowedColorCount + 1];
			_blues = new int[allowedColorCount + 1];
			_sums = new int[allowedColorCount + 1];

			Log.Info().WriteLine("Starting bitmap reconstruction...");

			using (var dest = FastBitmapFactory.Create(_resultBitmap) as FastChunkyBitmap)
			{
				using (var src = FastBitmapFactory.Create(_sourceBitmap))
				{
                    var lookup = new Dictionary<Color, byte>();
                    for (var y = 0; y < src.Height; y++)
					{
						for (var x = 0; x < src.Width; x++)
						{
							Color color;
							if (src is IFastBitmapWithBlend srcBlend)
							{
								// WithoutAlpha, this makes it possible to ignore the alpha
								color = srcBlend.GetBlendedColorAt(x, y);
							}
							else
							{
								color = src.GetColorAt(x, y);
							}

							// Check if we already matched the color
							byte bestMatch;
							if (!lookup.ContainsKey(color))
							{
								// If not we need to find the best match

								// First get initial match
								bestMatch = dest.GetColorIndexAt(x, y);
								bestMatch = _tag[bestMatch];

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

							_reds[bestMatch] += color.R;
							_greens[bestMatch] += color.G;
							_blues[bestMatch] += color.B;
							_sums[bestMatch]++;

							dest.SetColorIndexAt(x, y, bestMatch);
						}
					}
				}
			}


			// generates palette
			var imagePalette = _resultBitmap.NativeBitmap.Palette;
			var entries = imagePalette.Entries;
			for (var paletteIndex = 0; paletteIndex < allowedColorCount; paletteIndex++)
			{
				if (_sums[paletteIndex] > 0)
				{
					_reds[paletteIndex] /= _sums[paletteIndex];
					_greens[paletteIndex] /= _sums[paletteIndex];
					_blues[paletteIndex] /= _sums[paletteIndex];
				}

				entries[paletteIndex] = Color.FromArgb(255, _reds[paletteIndex], _greens[paletteIndex], _blues[paletteIndex]);
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
			var area = new long[Sidesize];
			var areaRed = new long[Sidesize];
			var areaGreen = new long[Sidesize];
			var areaBlue = new long[Sidesize];
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
		private static long Volume(WuColorCube cube, long[,,] moment)
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
		private static float VolumeFloat(WuColorCube cube, float[,,] moment)
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
		private static long Top(WuColorCube cube, int direction, int position, long[,,] moment)
		{
			switch (direction)
			{
				case Red:
					return moment[position, cube.GreenMaximum, cube.BlueMaximum] -
					       moment[position, cube.GreenMaximum, cube.BlueMinimum] -
					       moment[position, cube.GreenMinimum, cube.BlueMaximum] +
					       moment[position, cube.GreenMinimum, cube.BlueMinimum];

				case Green:
					return moment[cube.RedMaximum, position, cube.BlueMaximum] -
					       moment[cube.RedMaximum, position, cube.BlueMinimum] -
					       moment[cube.RedMinimum, position, cube.BlueMaximum] +
					       moment[cube.RedMinimum, position, cube.BlueMinimum];

				case Blue:
					return moment[cube.RedMaximum, cube.GreenMaximum, position] -
					       moment[cube.RedMaximum, cube.GreenMinimum, position] -
					       moment[cube.RedMinimum, cube.GreenMaximum, position] +
					       moment[cube.RedMinimum, cube.GreenMinimum, position];

				default:
					return 0;
			}
		}

		/// <summary>
		///     Splits the cube in a given color direction at its minimum.
		/// </summary>
		private static long Bottom(WuColorCube cube, int direction, long[,,] moment)
		{
			switch (direction)
			{
				case Red:
					return -moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] +
					       moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] +
					       moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] -
					       moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum];

				case Green:
					return -moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] +
					       moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] +
					       moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] -
					       moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum];

				case Blue:
					return -moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] +
					       moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] +
					       moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] -
					       moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum];
				default:
					return 0;
			}
		}

		/// <summary>
		///     Calculates statistical variance for a given cube.
		/// </summary>
		private float CalculateVariance(WuColorCube cube)
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
		private float Maximize(WuColorCube cube, int direction, int first, int last, int[] cut, long wholeRed, long wholeGreen, long wholeBlue, long wholeWeight)
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
		private bool Cut(WuColorCube first, WuColorCube second)
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
		private static void Mark(WuColorCube cube, int label, byte[] tag)
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