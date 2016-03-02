/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Greenshot.Addon.Core
{
	internal class WuColorCube
	{
		/// <summary>
		/// Gets or sets the red minimum.
		/// </summary>
		/// <value>The red minimum.</value>
		public int RedMinimum
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the red maximum.
		/// </summary>
		/// <value>The red maximum.</value>
		public int RedMaximum
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the green minimum.
		/// </summary>
		/// <value>The green minimum.</value>
		public int GreenMinimum
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the green maximum.
		/// </summary>
		/// <value>The green maximum.</value>
		public int GreenMaximum
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the blue minimum.
		/// </summary>
		/// <value>The blue minimum.</value>
		public int BlueMinimum
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the blue maximum.
		/// </summary>
		/// <value>The blue maximum.</value>
		public int BlueMaximum
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the cube volume.
		/// </summary>
		/// <value>The volume.</value>
		public int Volume
		{
			get;
			set;
		}
	}

	public class WuQuantizer : IDisposable
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(WuQuantizer));

		private const int MaxColor = 512;
		private const int Red = 2;
		private const int Green = 1;
		private const int Blue = 0;
		private const int SideSize = 33;
		private const int MaxSideIndex = 32;
		private const int MaxVolume = SideSize*SideSize*SideSize;

		// To count the colors
		private readonly int _colorCount;

		private int[] _reds;
		private int[] _greens;
		private int[] _blues;
		private int[] _sums;

		private readonly long[,,] _weights;
		private readonly long[,,] _momentsRed;
		private readonly long[,,] _momentsGreen;
		private readonly long[,,] _momentsBlue;
		private readonly float[,,] _moments;

		private byte[] _tag;

		private readonly WuColorCube[] _cubes;
		private readonly Bitmap _sourceBitmap;
		private Bitmap _resultBitmap;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_resultBitmap != null)
				{
					_resultBitmap.Dispose();
					_resultBitmap = null;
				}
			}
		}

		public WuQuantizer(Bitmap sourceBitmap)
		{
			_sourceBitmap = sourceBitmap;
			// Make sure the color count variables are reset
			var bitArray = new BitArray((int) Math.Pow(2, 24));
			_colorCount = 0;

			// creates all the _cubes
			_cubes = new WuColorCube[MaxColor];

			// initializes all the _cubes
			for (int cubeIndex = 0; cubeIndex < MaxColor; cubeIndex++)
			{
				_cubes[cubeIndex] = new WuColorCube();
			}

			// resets the reference minimums
			_cubes[0].RedMinimum = 0;
			_cubes[0].GreenMinimum = 0;
			_cubes[0].BlueMinimum = 0;

			// resets the reference maximums
			_cubes[0].RedMaximum = MaxSideIndex;
			_cubes[0].GreenMaximum = MaxSideIndex;
			_cubes[0].BlueMaximum = MaxSideIndex;

			_weights = new long[SideSize, SideSize, SideSize];
			_momentsRed = new long[SideSize, SideSize, SideSize];
			_momentsGreen = new long[SideSize, SideSize, SideSize];
			_momentsBlue = new long[SideSize, SideSize, SideSize];
			_moments = new float[SideSize, SideSize, SideSize];

			int[] table = new int[256];

			for (int tableIndex = 0; tableIndex < 256; ++tableIndex)
			{
				table[tableIndex] = tableIndex*tableIndex;
			}

			// Use a bitmap to store the initial match, which is just as good as an array and saves us 2x the storage
			using (var sourceFastBitmap = FastBitmap.Create(sourceBitmap))
			{
				var sourceFastBitmapWithBlend = sourceFastBitmap as IFastBitmapWithBlend;
				sourceFastBitmap.Lock();
				using (var destinationFastBitmap = FastBitmap.CreateEmpty(sourceBitmap.Size, PixelFormat.Format8bppIndexed, Color.White) as FastChunkyBitmap)
				{
					destinationFastBitmap.Lock();
					for (int y = 0; y < sourceFastBitmap.Height; y++)
					{
						for (int x = 0; x < sourceFastBitmap.Width; x++)
						{
							Color color;
							if (sourceFastBitmapWithBlend == null)
							{
								color = sourceFastBitmap.GetColorAt(x, y);
							}
							else
							{
								color = sourceFastBitmapWithBlend.GetBlendedColorAt(x, y);
							}
							// To count the colors
							int index = color.ToArgb() & 0x00ffffff;
							// Check if we already have this color
							if (!bitArray.Get(index))
							{
								// If not, add 1 to the single colors
								_colorCount++;
								bitArray.Set(index, true);
							}

							int indexRed = (color.R >> 3) + 1;
							int indexGreen = (color.G >> 3) + 1;
							int indexBlue = (color.B >> 3) + 1;

							_weights[indexRed, indexGreen, indexBlue]++;
							_momentsRed[indexRed, indexGreen, indexBlue] += color.R;
							_momentsGreen[indexRed, indexGreen, indexBlue] += color.G;
							_momentsBlue[indexRed, indexGreen, indexBlue] += color.B;
							_moments[indexRed, indexGreen, indexBlue] += table[color.R] + table[color.G] + table[color.B];

							// Store the initial "match"
							int paletteIndex = (indexRed << 10) + (indexRed << 6) + indexRed + (indexGreen << 5) + indexGreen + indexBlue;
							destinationFastBitmap.SetColorIndexAt(x, y, (byte) (paletteIndex & 0xff));
						}
					}
					_resultBitmap = destinationFastBitmap.UnlockAndReturnBitmap();
				}
			}
		}

		public int GetColorCount()
		{
			return _colorCount;
		}

		/// <summary>
		/// Reindex the 24/32 BPP (A)RGB image to a 8BPP
		/// </summary>
		/// <returns>Bitmap</returns>
		public Bitmap SimpleReindex()
		{
			var colors = new List<Color>();
			var lookup = new Dictionary<Color, byte>();
			using (var bbbDest = FastBitmap.Create(_resultBitmap) as FastChunkyBitmap)
			{
				bbbDest.Lock();
				using (var bbbSrc = FastBitmap.Create(_sourceBitmap))
				{
					var bbbSrcBlend = bbbSrc as IFastBitmapWithBlend;

					bbbSrc.Lock();
					for (int y = 0; y < bbbSrc.Height; y++)
					{
						for (int x = 0; x < bbbSrc.Width; x++)
						{
							Color color;
							if (bbbSrcBlend != null)
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
			var imagePalette = _resultBitmap.Palette;
			var entries = imagePalette.Entries;
			for (int paletteIndex = 0; paletteIndex < 256; paletteIndex++)
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
			_resultBitmap.Palette = imagePalette;

			// Make sure the bitmap is not disposed, as we return it.
			var tmpBitmap = _resultBitmap;
			_resultBitmap = null;
			return tmpBitmap;
		}

		/// <summary>
		/// Get the image
		/// </summary>
		public Bitmap GetQuantizedImage(int allowedColorCount)
		{
			if (allowedColorCount > 256)
			{
				throw new ArgumentOutOfRangeException(nameof(allowedColorCount), allowedColorCount, "Quantizing muss be done to get less than 256 colors");
			}
			if (_colorCount < allowedColorCount)
			{
				// Simple logic to reduce to 8 bit
				Log.Information("Colors in the image are already less as whished for, using simple copy to indexed image, no quantizing needed!");
				return SimpleReindex();
			}
			// preprocess the colors
			CalculateMoments();
			Log.Information("Calculated the _moments...");
			int next = 0;
			var volumeVariance = new float[MaxColor];

			// processes the _cubes
			for (int cubeIndex = 1; cubeIndex < allowedColorCount; ++cubeIndex)
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
				float temp = volumeVariance[0];

				for (int index = 1; index <= cubeIndex; ++index)
				{
					if (volumeVariance[index] > temp)
					{
						temp = volumeVariance[index];
						next = index;
					}
				}

				if (temp <= 0.0)
				{
					allowedColorCount = cubeIndex + 1;
					break;
				}
			}

			int[] lookupRed = new int[MaxColor];
			int[] lookupGreen = new int[MaxColor];
			int[] lookupBlue = new int[MaxColor];

			_tag = new byte[MaxVolume];

			// precalculates lookup tables
			for (int k = 0; k < allowedColorCount; ++k)
			{
				Mark(_cubes[k], k, _tag);

				long weight = Volume(_cubes[k], _weights);

				if (weight > 0)
				{
					lookupRed[k] = (int) (Volume(_cubes[k], _momentsRed)/weight);
					lookupGreen[k] = (int) (Volume(_cubes[k], _momentsGreen)/weight);
					lookupBlue[k] = (int) (Volume(_cubes[k], _momentsBlue)/weight);
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

			Log.Information("Starting bitmap reconstruction...");

			using (FastChunkyBitmap dest = FastBitmap.Create(_resultBitmap) as FastChunkyBitmap)
			{
				using (IFastBitmap src = FastBitmap.Create(_sourceBitmap))
				{
					var srcBlend = src as IFastBitmapWithBlend;
					var lookup = new Dictionary<Color, byte>();
					for (int y = 0; y < src.Height; y++)
					{
						for (int x = 0; x < src.Width; x++)
						{
							Color color;
							if (srcBlend != null)
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

								int bestDistance = 100000000;
								for (int lookupIndex = 0; lookupIndex < allowedColorCount; lookupIndex++)
								{
									int foundRed = lookupRed[lookupIndex];
									int foundGreen = lookupGreen[lookupIndex];
									int foundBlue = lookupBlue[lookupIndex];
									int deltaRed = color.R - foundRed;
									int deltaGreen = color.G - foundGreen;
									int deltaBlue = color.B - foundBlue;

									int distance = deltaRed*deltaRed + deltaGreen*deltaGreen + deltaBlue*deltaBlue;

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
			var imagePalette = _resultBitmap.Palette;
			var entries = imagePalette.Entries;
			for (int paletteIndex = 0; paletteIndex < allowedColorCount; paletteIndex++)
			{
				if (_sums[paletteIndex] > 0)
				{
					_reds[paletteIndex] /= _sums[paletteIndex];
					_greens[paletteIndex] /= _sums[paletteIndex];
					_blues[paletteIndex] /= _sums[paletteIndex];
				}

				entries[paletteIndex] = Color.FromArgb(255, _reds[paletteIndex], _greens[paletteIndex], _blues[paletteIndex]);
			}
			_resultBitmap.Palette = imagePalette;

			// Make sure the bitmap is not disposed, as we return it.
			var tmpBitmap = _resultBitmap;
			_resultBitmap = null;
			return tmpBitmap;
		}

		/// <summary>
		/// Converts the histogram to a series of _moments.
		/// </summary>
		private void CalculateMoments()
		{
			long[] area = new long[SideSize];
			long[] areaRed = new long[SideSize];
			long[] areaGreen = new long[SideSize];
			long[] areaBlue = new long[SideSize];
			float[] area2 = new float[SideSize];

			for (int redIndex = 1; redIndex <= MaxSideIndex; ++redIndex)
			{
				for (int index = 0; index <= MaxSideIndex; ++index)
				{
					area[index] = 0;
					areaRed[index] = 0;
					areaGreen[index] = 0;
					areaBlue[index] = 0;
					area2[index] = 0;
				}

				for (int greenIndex = 1; greenIndex <= MaxSideIndex; ++greenIndex)
				{
					long line = 0;
					long lineRed = 0;
					long lineGreen = 0;
					long lineBlue = 0;
					float line2 = 0.0f;

					for (int blueIndex = 1; blueIndex <= MaxSideIndex; ++blueIndex)
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
		/// Computes the volume of the cube in a specific moment.
		/// </summary>
		private static long Volume(WuColorCube cube, long[,,] moment)
		{
			return moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMaximum] - moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] - moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] + moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] - moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] + moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] + moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] - moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum];
		}

		/// <summary>
		/// Computes the volume of the cube in a specific moment. For the floating-point values.
		/// </summary>
		private static float VolumeFloat(WuColorCube cube, float[,,] moment)
		{
			return moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMaximum] - moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] - moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] + moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] - moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] + moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] + moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] - moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum];
		}

		/// <summary>
		/// Splits the cube in given position, and color direction.
		/// </summary>
		private static long Top(WuColorCube cube, int direction, int position, long[,,] moment)
		{
			switch (direction)
			{
				case Red:
					return (moment[position, cube.GreenMaximum, cube.BlueMaximum] - moment[position, cube.GreenMaximum, cube.BlueMinimum] - moment[position, cube.GreenMinimum, cube.BlueMaximum] + moment[position, cube.GreenMinimum, cube.BlueMinimum]);

				case Green:
					return (moment[cube.RedMaximum, position, cube.BlueMaximum] - moment[cube.RedMaximum, position, cube.BlueMinimum] - moment[cube.RedMinimum, position, cube.BlueMaximum] + moment[cube.RedMinimum, position, cube.BlueMinimum]);

				case Blue:
					return (moment[cube.RedMaximum, cube.GreenMaximum, position] - moment[cube.RedMaximum, cube.GreenMinimum, position] - moment[cube.RedMinimum, cube.GreenMaximum, position] + moment[cube.RedMinimum, cube.GreenMinimum, position]);

				default:
					return 0;
			}
		}

		/// <summary>
		/// Splits the cube in a given color direction at its minimum.
		/// </summary>
		private static long Bottom(WuColorCube cube, int direction, long[,,] moment)
		{
			switch (direction)
			{
				case Red:
					return (-moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] + moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] + moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] - moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]);

				case Green:
					return (-moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] + moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] + moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] - moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]);

				case Blue:
					return (-moment[cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] + moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] + moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] - moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]);
				default:
					return 0;
			}
		}

		/// <summary>
		/// Calculates statistical variance for a given cube.
		/// </summary>
		private float CalculateVariance(WuColorCube cube)
		{
			float volumeRed = Volume(cube, _momentsRed);
			float volumeGreen = Volume(cube, _momentsGreen);
			float volumeBlue = Volume(cube, _momentsBlue);
			float volumeMoment = VolumeFloat(cube, _moments);
			float volumeWeight = Volume(cube, _weights);

			float distance = volumeRed*volumeRed + volumeGreen*volumeGreen + volumeBlue*volumeBlue;

			return volumeMoment - (distance/volumeWeight);
		}

		/// <summary>
		///	Finds the optimal (maximal) position for the cut.
		/// </summary>
		private float Maximize(WuColorCube cube, int direction, int first, int last, int[] cut, long wholeRed, long wholeGreen, long wholeBlue, long wholeWeight)
		{
			long bottomRed = Bottom(cube, direction, _momentsRed);
			long bottomGreen = Bottom(cube, direction, _momentsGreen);
			long bottomBlue = Bottom(cube, direction, _momentsBlue);
			long bottomWeight = Bottom(cube, direction, _weights);

			float result = 0.0f;
			cut[0] = -1;

			for (int position = first; position < last; ++position)
			{
				// determines the cube cut at a certain position
				long halfRed = bottomRed + Top(cube, direction, position, _momentsRed);
				long halfGreen = bottomGreen + Top(cube, direction, position, _momentsGreen);
				long halfBlue = bottomBlue + Top(cube, direction, position, _momentsBlue);
				long halfWeight = bottomWeight + Top(cube, direction, position, _weights);

				// the cube cannot be cut at bottom (this would lead to empty cube)
				if (halfWeight != 0)
				{
					float halfDistance = halfRed*halfRed + halfGreen*halfGreen + halfBlue*halfBlue;
					float temp = halfDistance/halfWeight;

					halfRed = wholeRed - halfRed;
					halfGreen = wholeGreen - halfGreen;
					halfBlue = wholeBlue - halfBlue;
					halfWeight = wholeWeight - halfWeight;

					if (halfWeight != 0)
					{
						halfDistance = halfRed*halfRed + halfGreen*halfGreen + halfBlue*halfBlue;
						temp += halfDistance/halfWeight;

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
		/// Cuts a cube with another one.
		/// </summary>
		private Boolean Cut(WuColorCube first, WuColorCube second)
		{
			int direction;

			int[] cutRed =
			{
				0
			};
			int[] cutGreen =
			{
				0
			};
			int[] cutBlue =
			{
				0
			};

			long wholeRed = Volume(first, _momentsRed);
			long wholeGreen = Volume(first, _momentsGreen);
			long wholeBlue = Volume(first, _momentsBlue);
			long wholeWeight = Volume(first, _weights);

			float maxRed = Maximize(first, Red, first.RedMinimum + 1, first.RedMaximum, cutRed, wholeRed, wholeGreen, wholeBlue, wholeWeight);
			float maxGreen = Maximize(first, Green, first.GreenMinimum + 1, first.GreenMaximum, cutGreen, wholeRed, wholeGreen, wholeBlue, wholeWeight);
			float maxBlue = Maximize(first, Blue, first.BlueMinimum + 1, first.BlueMaximum, cutBlue, wholeRed, wholeGreen, wholeBlue, wholeWeight);

			if ((maxRed >= maxGreen) && (maxRed >= maxBlue))
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
				if ((maxGreen >= maxRed) && (maxGreen >= maxBlue))
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
			first.Volume = (first.RedMaximum - first.RedMinimum)*(first.GreenMaximum - first.GreenMinimum)*(first.BlueMaximum - first.BlueMinimum);
			second.Volume = (second.RedMaximum - second.RedMinimum)*(second.GreenMaximum - second.GreenMinimum)*(second.BlueMaximum - second.BlueMinimum);

			// the cut was successfull
			return true;
		}

		/// <summary>
		/// Marks all the tags with a given label.
		/// </summary>
		private void Mark(WuColorCube cube, int label, byte[] tag)
		{
			for (int redIndex = cube.RedMinimum + 1; redIndex <= cube.RedMaximum; ++redIndex)
			{
				for (int greenIndex = cube.GreenMinimum + 1; greenIndex <= cube.GreenMaximum; ++greenIndex)
				{
					for (int blueIndex = cube.BlueMinimum + 1; blueIndex <= cube.BlueMaximum; ++blueIndex)
					{
						tag[(redIndex << 10) + (redIndex << 6) + redIndex + (greenIndex << 5) + greenIndex + blueIndex] = (byte) label;
					}
				}
			}
		}
	}
}