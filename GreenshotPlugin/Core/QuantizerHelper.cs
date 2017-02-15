#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using log4net;

#endregion

namespace GreenshotPlugin.Core
{
	internal class WuColorCube
	{
		/// <summary>
		///     Gets or sets the red minimum.
		/// </summary>
		/// <value>The red minimum.</value>
		public int RedMinimum { get; set; }

		/// <summary>
		///     Gets or sets the red maximum.
		/// </summary>
		/// <value>The red maximum.</value>
		public int RedMaximum { get; set; }

		/// <summary>
		///     Gets or sets the green minimum.
		/// </summary>
		/// <value>The green minimum.</value>
		public int GreenMinimum { get; set; }

		/// <summary>
		///     Gets or sets the green maximum.
		/// </summary>
		/// <value>The green maximum.</value>
		public int GreenMaximum { get; set; }

		/// <summary>
		///     Gets or sets the blue minimum.
		/// </summary>
		/// <value>The blue minimum.</value>
		public int BlueMinimum { get; set; }

		/// <summary>
		///     Gets or sets the blue maximum.
		/// </summary>
		/// <value>The blue maximum.</value>
		public int BlueMaximum { get; set; }

		/// <summary>
		///     Gets or sets the cube volume.
		/// </summary>
		/// <value>The volume.</value>
		public int Volume { get; set; }
	}

	public class WuQuantizer : IDisposable
	{
		private const int MAXCOLOR = 512;
		private const int RED = 2;
		private const int GREEN = 1;
		private const int BLUE = 0;
		private const int SIDESIZE = 33;
		private const int MAXSIDEINDEX = 32;
		private const int MAXVOLUME = SIDESIZE * SIDESIZE * SIDESIZE;
		private static readonly ILog LOG = LogManager.GetLogger(typeof(WuQuantizer));

		// To count the colors
		private readonly int colorCount;

		private readonly WuColorCube[] cubes;
		private readonly float[,,] moments;
		private readonly long[,,] momentsBlue;
		private readonly long[,,] momentsGreen;
		private readonly long[,,] momentsRed;
		private readonly Bitmap sourceBitmap;

		private readonly long[,,] weights;
		private int[] blues;
		private int[] greens;

		private int[] reds;
		private Bitmap resultBitmap;
		private int[] sums;

		private byte[] tag;

		/// <summary>
		///     See <see cref="IColorQuantizer.Prepare" /> for more details.
		/// </summary>
		public WuQuantizer(Bitmap sourceBitmap)
		{
			this.sourceBitmap = sourceBitmap;
			// Make sure the color count variables are reset
			var bitArray = new BitArray((int) Math.Pow(2, 24));
			colorCount = 0;

			// creates all the cubes
			cubes = new WuColorCube[MAXCOLOR];

			// initializes all the cubes
			for (var cubeIndex = 0; cubeIndex < MAXCOLOR; cubeIndex++)
			{
				cubes[cubeIndex] = new WuColorCube();
			}

			// resets the reference minimums
			cubes[0].RedMinimum = 0;
			cubes[0].GreenMinimum = 0;
			cubes[0].BlueMinimum = 0;

			// resets the reference maximums
			cubes[0].RedMaximum = MAXSIDEINDEX;
			cubes[0].GreenMaximum = MAXSIDEINDEX;
			cubes[0].BlueMaximum = MAXSIDEINDEX;

			weights = new long[SIDESIZE, SIDESIZE, SIDESIZE];
			momentsRed = new long[SIDESIZE, SIDESIZE, SIDESIZE];
			momentsGreen = new long[SIDESIZE, SIDESIZE, SIDESIZE];
			momentsBlue = new long[SIDESIZE, SIDESIZE, SIDESIZE];
			moments = new float[SIDESIZE, SIDESIZE, SIDESIZE];

			var table = new int[256];

			for (var tableIndex = 0; tableIndex < 256; ++tableIndex)
			{
				table[tableIndex] = tableIndex * tableIndex;
			}

			// Use a bitmap to store the initial match, which is just as good as an array and saves us 2x the storage
			using (var sourceFastBitmap = FastBitmap.Create(sourceBitmap))
			{
				var sourceFastBitmapWithBlend = sourceFastBitmap as IFastBitmapWithBlend;
				sourceFastBitmap.Lock();
				using (var destinationFastBitmap = FastBitmap.CreateEmpty(sourceBitmap.Size, PixelFormat.Format8bppIndexed, Color.White) as FastChunkyBitmap)
				{
					destinationFastBitmap.Lock();
					for (var y = 0; y < sourceFastBitmap.Height; y++)
					{
						for (var x = 0; x < sourceFastBitmap.Width; x++)
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
							var index = color.ToArgb() & 0x00ffffff;
							// Check if we already have this color
							if (!bitArray.Get(index))
							{
								// If not, add 1 to the single colors
								colorCount++;
								bitArray.Set(index, true);
							}

							var indexRed = (color.R >> 3) + 1;
							var indexGreen = (color.G >> 3) + 1;
							var indexBlue = (color.B >> 3) + 1;

							weights[indexRed, indexGreen, indexBlue]++;
							momentsRed[indexRed, indexGreen, indexBlue] += color.R;
							momentsGreen[indexRed, indexGreen, indexBlue] += color.G;
							momentsBlue[indexRed, indexGreen, indexBlue] += color.B;
							moments[indexRed, indexGreen, indexBlue] += table[color.R] + table[color.G] + table[color.B];

							// Store the initial "match"
							var paletteIndex = (indexRed << 10) + (indexRed << 6) + indexRed + (indexGreen << 5) + indexGreen + indexBlue;
							destinationFastBitmap.SetColorIndexAt(x, y, (byte) (paletteIndex & 0xff));
						}
					}
					resultBitmap = destinationFastBitmap.UnlockAndReturnBitmap();
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (resultBitmap != null)
				{
					resultBitmap.Dispose();
					resultBitmap = null;
				}
			}
		}

		/// <summary>
		///     See <see cref="IColorQuantizer.Prepare" /> for more details.
		/// </summary>
		public int GetColorCount()
		{
			return colorCount;
		}

		/// <summary>
		///     Reindex the 24/32 BPP (A)RGB image to a 8BPP
		/// </summary>
		/// <returns>Bitmap</returns>
		public Bitmap SimpleReindex()
		{
			var colors = new List<Color>();
			var lookup = new Dictionary<Color, byte>();
			using (var bbbDest = FastBitmap.Create(resultBitmap) as FastChunkyBitmap)
			{
				bbbDest.Lock();
				using (var bbbSrc = FastBitmap.Create(sourceBitmap))
				{
					var bbbSrcBlend = bbbSrc as IFastBitmapWithBlend;

					bbbSrc.Lock();
					byte index;
					for (var y = 0; y < bbbSrc.Height; y++)
					{
						for (var x = 0; x < bbbSrc.Width; x++)
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
			var imagePalette = resultBitmap.Palette;
			var entries = imagePalette.Entries;
			for (var paletteIndex = 0; paletteIndex < 256; paletteIndex++)
			{
				if (paletteIndex < colorCount)
				{
					entries[paletteIndex] = colors[paletteIndex];
				}
				else
				{
					entries[paletteIndex] = Color.Black;
				}
			}
			resultBitmap.Palette = imagePalette;

			// Make sure the bitmap is not disposed, as we return it.
			var tmpBitmap = resultBitmap;
			resultBitmap = null;
			return tmpBitmap;
		}

		/// <summary>
		///     Get the image
		/// </summary>
		public Bitmap GetQuantizedImage(int allowedColorCount)
		{
			if (allowedColorCount > 256)
			{
				throw new ArgumentOutOfRangeException(nameof(allowedColorCount), "Quantizing muss be done to get less than 256 colors");
			}
			if (colorCount < allowedColorCount)
			{
				// Simple logic to reduce to 8 bit
				LOG.Info("Colors in the image are already less as whished for, using simple copy to indexed image, no quantizing needed!");
				return SimpleReindex();
			}
			// preprocess the colors
			CalculateMoments();
			LOG.Info("Calculated the moments...");
			var next = 0;
			var volumeVariance = new float[MAXCOLOR];

			// processes the cubes
			for (var cubeIndex = 1; cubeIndex < allowedColorCount; ++cubeIndex)
			{
				// if cut is possible; make it
				if (Cut(cubes[next], cubes[cubeIndex]))
				{
					volumeVariance[next] = cubes[next].Volume > 1 ? CalculateVariance(cubes[next]) : 0.0f;
					volumeVariance[cubeIndex] = cubes[cubeIndex].Volume > 1 ? CalculateVariance(cubes[cubeIndex]) : 0.0f;
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

			var lookupRed = new int[MAXCOLOR];
			var lookupGreen = new int[MAXCOLOR];
			var lookupBlue = new int[MAXCOLOR];

			tag = new byte[MAXVOLUME];

			// precalculates lookup tables
			for (var k = 0; k < allowedColorCount; ++k)
			{
				Mark(cubes[k], k, tag);

				var weight = Volume(cubes[k], weights);

				if (weight > 0)
				{
					lookupRed[k] = (int) (Volume(cubes[k], momentsRed) / weight);
					lookupGreen[k] = (int) (Volume(cubes[k], momentsGreen) / weight);
					lookupBlue[k] = (int) (Volume(cubes[k], momentsBlue) / weight);
				}
				else
				{
					lookupRed[k] = 0;
					lookupGreen[k] = 0;
					lookupBlue[k] = 0;
				}
			}

			reds = new int[allowedColorCount + 1];
			greens = new int[allowedColorCount + 1];
			blues = new int[allowedColorCount + 1];
			sums = new int[allowedColorCount + 1];

			LOG.Info("Starting bitmap reconstruction...");

			using (var dest = FastBitmap.Create(resultBitmap) as FastChunkyBitmap)
			{
				using (var src = FastBitmap.Create(sourceBitmap))
				{
					var srcBlend = src as IFastBitmapWithBlend;
					var lookup = new Dictionary<Color, byte>();
					for (var y = 0; y < src.Height; y++)
					{
						for (var x = 0; x < src.Width; x++)
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

							dest.SetColorIndexAt(x, y, bestMatch);
						}
					}
				}
			}


			// generates palette
			var imagePalette = resultBitmap.Palette;
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
			resultBitmap.Palette = imagePalette;

			// Make sure the bitmap is not disposed, as we return it.
			var tmpBitmap = resultBitmap;
			resultBitmap = null;
			return tmpBitmap;
		}

		/// <summary>
		///     Converts the histogram to a series of moments.
		/// </summary>
		private void CalculateMoments()
		{
			var area = new long[SIDESIZE];
			var areaRed = new long[SIDESIZE];
			var areaGreen = new long[SIDESIZE];
			var areaBlue = new long[SIDESIZE];
			var area2 = new float[SIDESIZE];

			for (var redIndex = 1; redIndex <= MAXSIDEINDEX; ++redIndex)
			{
				for (var index = 0; index <= MAXSIDEINDEX; ++index)
				{
					area[index] = 0;
					areaRed[index] = 0;
					areaGreen[index] = 0;
					areaBlue[index] = 0;
					area2[index] = 0;
				}

				for (var greenIndex = 1; greenIndex <= MAXSIDEINDEX; ++greenIndex)
				{
					long line = 0;
					long lineRed = 0;
					long lineGreen = 0;
					long lineBlue = 0;
					var line2 = 0.0f;

					for (var blueIndex = 1; blueIndex <= MAXSIDEINDEX; ++blueIndex)
					{
						line += weights[redIndex, greenIndex, blueIndex];
						lineRed += momentsRed[redIndex, greenIndex, blueIndex];
						lineGreen += momentsGreen[redIndex, greenIndex, blueIndex];
						lineBlue += momentsBlue[redIndex, greenIndex, blueIndex];
						line2 += moments[redIndex, greenIndex, blueIndex];

						area[blueIndex] += line;
						areaRed[blueIndex] += lineRed;
						areaGreen[blueIndex] += lineGreen;
						areaBlue[blueIndex] += lineBlue;
						area2[blueIndex] += line2;

						weights[redIndex, greenIndex, blueIndex] = weights[redIndex - 1, greenIndex, blueIndex] + area[blueIndex];
						momentsRed[redIndex, greenIndex, blueIndex] = momentsRed[redIndex - 1, greenIndex, blueIndex] + areaRed[blueIndex];
						momentsGreen[redIndex, greenIndex, blueIndex] = momentsGreen[redIndex - 1, greenIndex, blueIndex] + areaGreen[blueIndex];
						momentsBlue[redIndex, greenIndex, blueIndex] = momentsBlue[redIndex - 1, greenIndex, blueIndex] + areaBlue[blueIndex];
						moments[redIndex, greenIndex, blueIndex] = moments[redIndex - 1, greenIndex, blueIndex] + area2[blueIndex];
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
				case RED:
					return moment[position, cube.GreenMaximum, cube.BlueMaximum] -
							moment[position, cube.GreenMaximum, cube.BlueMinimum] -
							moment[position, cube.GreenMinimum, cube.BlueMaximum] +
							moment[position, cube.GreenMinimum, cube.BlueMinimum];

				case GREEN:
					return moment[cube.RedMaximum, position, cube.BlueMaximum] -
							moment[cube.RedMaximum, position, cube.BlueMinimum] -
							moment[cube.RedMinimum, position, cube.BlueMaximum] +
							moment[cube.RedMinimum, position, cube.BlueMinimum];

				case BLUE:
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
				case RED:
					return -moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] +
							moment[cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] +
							moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] -
							moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum];

				case GREEN:
					return -moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] +
							moment[cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] +
							moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] -
							moment[cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum];

				case BLUE:
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
			float volumeRed = Volume(cube, momentsRed);
			float volumeGreen = Volume(cube, momentsGreen);
			float volumeBlue = Volume(cube, momentsBlue);
			var volumeMoment = VolumeFloat(cube, moments);
			float volumeWeight = Volume(cube, weights);

			var distance = volumeRed * volumeRed + volumeGreen * volumeGreen + volumeBlue * volumeBlue;

			return volumeMoment - distance / volumeWeight;
		}

		/// <summary>
		///     Finds the optimal (maximal) position for the cut.
		/// </summary>
		private float Maximize(WuColorCube cube, int direction, int first, int last, int[] cut, long wholeRed, long wholeGreen, long wholeBlue, long wholeWeight)
		{
			var bottomRed = Bottom(cube, direction, momentsRed);
			var bottomGreen = Bottom(cube, direction, momentsGreen);
			var bottomBlue = Bottom(cube, direction, momentsBlue);
			var bottomWeight = Bottom(cube, direction, weights);

			var result = 0.0f;
			cut[0] = -1;

			for (var position = first; position < last; ++position)
			{
				// determines the cube cut at a certain position
				var halfRed = bottomRed + Top(cube, direction, position, momentsRed);
				var halfGreen = bottomGreen + Top(cube, direction, position, momentsGreen);
				var halfBlue = bottomBlue + Top(cube, direction, position, momentsBlue);
				var halfWeight = bottomWeight + Top(cube, direction, position, weights);

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

			var wholeRed = Volume(first, momentsRed);
			var wholeGreen = Volume(first, momentsGreen);
			var wholeBlue = Volume(first, momentsBlue);
			var wholeWeight = Volume(first, weights);

			var maxRed = Maximize(first, RED, first.RedMinimum + 1, first.RedMaximum, cutRed, wholeRed, wholeGreen, wholeBlue, wholeWeight);
			var maxGreen = Maximize(first, GREEN, first.GreenMinimum + 1, first.GreenMaximum, cutGreen, wholeRed, wholeGreen, wholeBlue, wholeWeight);
			var maxBlue = Maximize(first, BLUE, first.BlueMinimum + 1, first.BlueMaximum, cutBlue, wholeRed, wholeGreen, wholeBlue, wholeWeight);

			if (maxRed >= maxGreen && maxRed >= maxBlue)
			{
				direction = RED;

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
					direction = GREEN;
				}
				else
				{
					direction = BLUE;
				}
			}

			second.RedMaximum = first.RedMaximum;
			second.GreenMaximum = first.GreenMaximum;
			second.BlueMaximum = first.BlueMaximum;

			// cuts in a certain direction
			switch (direction)
			{
				case RED:
					second.RedMinimum = first.RedMaximum = cutRed[0];
					second.GreenMinimum = first.GreenMinimum;
					second.BlueMinimum = first.BlueMinimum;
					break;

				case GREEN:
					second.GreenMinimum = first.GreenMaximum = cutGreen[0];
					second.RedMinimum = first.RedMinimum;
					second.BlueMinimum = first.BlueMinimum;
					break;

				case BLUE:
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
		private void Mark(WuColorCube cube, int label, byte[] tag)
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