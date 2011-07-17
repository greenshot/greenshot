/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Greenshot.Helpers {
	/// <summary>
	/// Quantizer is a method to reduce the colors in a bitmap.
	/// Currently there is only 1 implementation: OctreeQuantizer
	/// </summary>
	public unsafe abstract class Quantizer {
		/// <summary>
		/// Construct the quantizer
		/// </summary>
		/// <param name="singlePass">If true, the quantization only needs to loop through the source pixels once</param>
		/// <remarks>
		/// If you construct this class with a true value for singlePass, then the code will, when quantizing your image,
		/// only call the 'QuantizeImage' function. If two passes are required, the code will call 'InitialQuantizeImage'
		/// and then 'QuantizeImage'.
		/// </remarks>
		public Quantizer (bool singlePass) {
			_singlePass = singlePass ;
		}

		/// <summary>
		/// Quantize an image and return the resulting output bitmap
		/// </summary>
		/// <param name="source">The image to quantize</param>
		/// <returns>A quantized version of the image</returns>
		public Bitmap Quantize(Image source) {
			// Get the size of the source image
			int	height = source.Height ;
			int width = source.Width ;

			// And construct a rectangle from these dimensions
			Rectangle bounds = new Rectangle(0, 0, width, height) ;

			// First off take a 32bpp copy of the image
			Bitmap copy = new Bitmap ( width , height , PixelFormat.Format32bppArgb ) ;

			// And construct an 8bpp version
			Bitmap output = new Bitmap ( width , height , PixelFormat.Format8bppIndexed ) ;

			// Now lock the bitmap into memory
			using (Graphics g = Graphics.FromImage(copy)) {
				g.PageUnit = GraphicsUnit.Pixel ;

				// Draw the source image onto the copy bitmap,
				// which will effect a widening as appropriate.
				g.DrawImage(source, bounds ) ;
			}

			// Define a pointer to the bitmap data
			BitmapData sourceData = null ;

			try {
				// Get the source image bits and lock into memory
				sourceData = copy.LockBits ( bounds , ImageLockMode.ReadOnly , PixelFormat.Format32bppArgb ) ;

				// Call the FirstPass function if not a single pass algorithm.
				// For something like an octree quantizer, this will run through
				// all image pixels, build a data structure, and create a palette.
				if ( !_singlePass )
					FirstPass ( sourceData , width , height ) ;

				// Then set the color palette on the output bitmap. I'm passing in the current palette 
				// as there's no way to construct a new, empty palette.
				output.Palette = this.GetPalette ( output.Palette ) ;

				// Then call the second pass which actually does the conversion
				SecondPass ( sourceData , output , width , height , bounds ) ;
			} finally {
				// Ensure that the bits are unlocked
				copy.UnlockBits ( sourceData );
				copy.Dispose();
			}

			// Last but not least, return the output bitmap
			return output;
		}

		/// <summary>
		/// Execute the first pass through the pixels in the image
		/// </summary>
		/// <param name="sourceData">The source data</param>
		/// <param name="width">The width in pixels of the image</param>
		/// <param name="height">The height in pixels of the image</param>
		protected virtual void FirstPass ( BitmapData sourceData , int width , int height ) {
			// Define the source data pointers. The source row is a byte to
			// keep addition of the stride value easier (as this is in bytes)
			byte*	pSourceRow = (byte*)sourceData.Scan0.ToPointer ( ) ;
			Int32*	pSourcePixel ;

			// Loop through each row
			for ( int row = 0 ; row < height ; row++ ) {
				// Set the source pixel to the first pixel in this row
				pSourcePixel = (Int32*) pSourceRow ;

				// And loop through each column
				for ( int col = 0 ; col < width ; col++ , pSourcePixel++ ) {
					// Now I have the pixel, call the FirstPassQuantize function...
					InitialQuantizePixel ( (Color32*)pSourcePixel ) ;
				}

				// Add the stride to the source row
				pSourceRow += sourceData.Stride ;
			}
		}

		/// <summary>
		/// Execute a second pass through the bitmap
		/// </summary>
		/// <param name="sourceData">The source bitmap, locked into memory</param>
		/// <param name="output">The output bitmap</param>
		/// <param name="width">The width in pixels of the image</param>
		/// <param name="height">The height in pixels of the image</param>
		/// <param name="bounds">The bounding rectangle</param>
		protected virtual void SecondPass ( BitmapData sourceData , Bitmap output , int width , int height , Rectangle bounds ) {
			BitmapData	outputData = null ;

			try {
				// Lock the output bitmap into memory
				outputData = output.LockBits ( bounds , ImageLockMode.WriteOnly , PixelFormat.Format8bppIndexed ) ;

				// Define the source data pointers. The source row is a byte to
				// keep addition of the stride value easier (as this is in bytes)
				byte*	pSourceRow = (byte*)sourceData.Scan0.ToPointer ( ) ;
				Int32*	pSourcePixel = (Int32*)pSourceRow ;
				Int32*	pPreviousPixel = pSourcePixel ;

				// Now define the destination data pointers
				byte*	pDestinationRow = (byte*) outputData.Scan0.ToPointer();
				byte*	pDestinationPixel = pDestinationRow ;

				// And convert the first pixel, so that I have values going into the loop
				byte	pixelValue = QuantizePixel ( (Color32*)pSourcePixel ) ;

				// Assign the value of the first pixel
				*pDestinationPixel = pixelValue ;

				// Loop through each row
				for ( int row = 0 ; row < height ; row++ ) {
					// Set the source pixel to the first pixel in this row
					pSourcePixel = (Int32*) pSourceRow ;

					// And set the destination pixel pointer to the first pixel in the row
					pDestinationPixel = pDestinationRow ;

					// Loop through each pixel on this scan line
					for ( int col = 0 ; col < width ; col++ , pSourcePixel++ , pDestinationPixel++ ) {
						// Check if this is the same as the last pixel. If so use that value
						// rather than calculating it again. This is an inexpensive optimisation.
						if ( *pPreviousPixel != *pSourcePixel ) {
							// Quantize the pixel
							pixelValue = QuantizePixel ( (Color32*)pSourcePixel ) ;

							// And setup the previous pointer
							pPreviousPixel = pSourcePixel ;
						}

						// And set the pixel in the output
						*pDestinationPixel = pixelValue ;
					}

					// Add the stride to the source row
					pSourceRow += sourceData.Stride ;

					// And to the destination row
					pDestinationRow += outputData.Stride ;
				}
			} finally {
				// Ensure that I unlock the output bits
				output.UnlockBits ( outputData ) ;
			}
		}

		/// <summary>
		/// Override this to process the pixel in the first pass of the algorithm
		/// </summary>
		/// <param name="pixel">The pixel to quantize</param>
		/// <remarks>
		/// This function need only be overridden if your quantize algorithm needs two passes,
		/// such as an Octree quantizer.
		/// </remarks>
		protected virtual void InitialQuantizePixel ( Color32* pixel ) {
		}

		/// <summary>
		/// Override this to process the pixel in the second pass of the algorithm
		/// </summary>
		/// <param name="pixel">The pixel to quantize</param>
		/// <returns>The quantized value</returns>
		protected abstract byte QuantizePixel ( Color32* pixel ) ;

		/// <summary>
		/// Retrieve the palette for the quantized image
		/// </summary>
		/// <param name="original">Any old palette, this is overrwritten</param>
		/// <returns>The new color palette</returns>
		protected abstract ColorPalette GetPalette ( ColorPalette original ) ;

		/// <summary>
		/// Flag used to indicate whether a single pass or two passes are needed for quantization.
		/// </summary>
		private bool	_singlePass ;

		/// <summary>
		/// Struct that defines a 32 bpp colour
		/// </summary>
		/// <remarks>
		/// This struct is used to read data from a 32 bits per pixel image
		/// in memory, and is ordered in this manner as this is the way that
		/// the data is layed out in memory
		/// </remarks>
		[StructLayout(LayoutKind.Explicit)]
		public struct Color32 {
			/// <summary>
			/// Holds the blue component of the colour
			/// </summary>
			[FieldOffset(0)]
			public byte Blue ;
			/// <summary>
			/// Holds the green component of the colour
			/// </summary>
			[FieldOffset(1)]
			public byte Green ;
			/// <summary>
			/// Holds the red component of the colour
			/// </summary>
			[FieldOffset(2)]
			public byte Red ;
			/// <summary>
			/// Holds the alpha component of the colour
			/// </summary>
			[FieldOffset(3)]
			public byte Alpha ;

			/// <summary>
			/// Permits the color32 to be treated as an int32
			/// </summary>
			[FieldOffset(0)]
			public int ARGB ;

			/// <summary>
			/// Return the color for this Color32 object
			/// </summary>
			public Color Color {
				get	{ return Color.FromArgb ( Alpha , Red , Green , Blue ) ; }
			}
		}
	}

	/// <summary>
	/// Quantize using an Octree
	/// </summary>
	public unsafe class OctreeQuantizer : Quantizer {
		/// <summary>
		/// Construct the octree quantizer
		/// </summary>
		/// <remarks>
		/// The Octree quantizer is a two pass algorithm. The initial pass sets up the octree,
		/// the second pass quantizes a color based on the nodes in the tree
		/// </remarks>
		/// <param name="maxColors">The maximum number of colors to return</param>
		/// <param name="maxColorBits">The number of significant bits</param>
		public OctreeQuantizer ( int maxColors , int maxColorBits ) : base ( false ) {
			if ( maxColors > 255 ) {
				throw new ArgumentOutOfRangeException ( "maxColors" , maxColors , "The number of colors should be less than 256" ) ;
			}

			if ( ( maxColorBits < 1 ) | ( maxColorBits > 8 ) ) {
				throw new ArgumentOutOfRangeException ( "maxColorBits" , maxColorBits , "This should be between 1 and 8" ) ;
			}

			// Construct the octree
			_octree = new Octree ( maxColorBits  ) ;

			_maxColors = maxColors ;
		}

		/// <summary>
		/// Process the pixel in the first pass of the algorithm
		/// </summary>
		/// <param name="pixel">The pixel to quantize</param>
		/// <remarks>
		/// This function need only be overridden if your quantize algorithm needs two passes,
		/// such as an Octree quantizer.
		/// </remarks>
		protected override void InitialQuantizePixel ( Color32* pixel ) {
			// Add the color to the octree
			_octree.AddColor ( pixel ) ;
		}

		/// <summary>
		/// Override this to process the pixel in the second pass of the algorithm
		/// </summary>
		/// <param name="pixel">The pixel to quantize</param>
		/// <returns>The quantized value</returns>
		protected override byte QuantizePixel ( Color32* pixel ) {
			byte	paletteIndex = (byte)_maxColors ;	// The color at [_maxColors] is set to transparent

			// Get the palette index if this non-transparent
			if ( pixel->Alpha > 0 ) {
				paletteIndex = (byte)_octree.GetPaletteIndex ( pixel ) ;
			}

			return paletteIndex ;
		}

		/// <summary>
		/// Retrieve the palette for the quantized image
		/// </summary>
		/// <param name="original">Any old palette, this is overrwritten</param>
		/// <returns>The new color palette</returns>
		protected override ColorPalette GetPalette ( ColorPalette original ) {
			// First off convert the octree to _maxColors colors
			ArrayList	palette = _octree.Palletize ( _maxColors - 1 ) ;

			// Then convert the palette based on those colors
			for ( int index = 0 ; index < palette.Count ; index++ )
				original.Entries[index] = (Color)palette[index] ;

			// Add the transparent color
			original.Entries[_maxColors] = Color.FromArgb ( 0 , 0 , 0 , 0 ) ;

			return original ;
		}

		/// <summary>
		/// Stores the tree
		/// </summary>
		private	Octree _octree ;

		/// <summary>
		/// Maximum allowed color depth
		/// </summary>
		private int	_maxColors ;

		/// <summary>
		/// Class which does the actual quantization
		/// </summary>
		private class Octree {
			/// <summary>
			/// Construct the octree
			/// </summary>
			/// <param name="maxColorBits">The maximum number of significant bits in the image</param>
			public Octree ( int maxColorBits ) {
				_maxColorBits = maxColorBits ;
				_leafCount = 0 ;
				_reducibleNodes = new OctreeNode[9] ;
				_root = new OctreeNode ( 0 , _maxColorBits , this ) ; 
				_previousColor = 0 ;
				_previousNode = null ;
			}

			/// <summary>
			/// Add a given color value to the octree
			/// </summary>
			/// <param name="pixel"></param>
			public void AddColor ( Color32* pixel ) {
				// Check if this request is for the same color as the last
				if ( _previousColor == pixel->ARGB ) {
					// If so, check if I have a previous node setup. This will only ocurr if the first color in the image
					// happens to be black, with an alpha component of zero.
					if ( null == _previousNode ) {
						_previousColor = pixel->ARGB ;
						_root.AddColor ( pixel , _maxColorBits , 0 , this ) ;
					} else {
						// Just update the previous node
						_previousNode.Increment ( pixel ) ;
					}
				} else {
					_previousColor = pixel->ARGB ;
					_root.AddColor ( pixel , _maxColorBits , 0 , this ) ;
				}
			}

			/// <summary>
			/// Reduce the depth of the tree
			/// </summary>
			public void Reduce ( ) {
				int	index ;

				// Find the deepest level containing at least one reducible node
				for ( index = _maxColorBits - 1 ; ( index > 0 ) && ( null == _reducibleNodes[index] ) ; index-- ) ;

				// Reduce the node most recently added to the list at level 'index'
				OctreeNode	node = _reducibleNodes[index] ;
				_reducibleNodes[index] = node.NextReducible ;

				// Decrement the leaf count after reducing the node
				_leafCount -= node.Reduce ( ) ;

				// And just in case I've reduced the last color to be added, and the next color to
				// be added is the same, invalidate the previousNode...
				_previousNode = null ;
			}

			/// <summary>
			/// Get/Set the number of leaves in the tree
			/// </summary>
			public int Leaves {
				get { return _leafCount ; }
				set { _leafCount = value ; }
			}

			/// <summary>
			/// Return the array of reducible nodes
			/// </summary>
			protected OctreeNode[] ReducibleNodes {
				get { return _reducibleNodes ; }
			}

			/// <summary>
			/// Keep track of the previous node that was quantized
			/// </summary>
			/// <param name="node">The node last quantized</param>
			protected void TrackPrevious ( OctreeNode node ) {
				_previousNode = node ;
			}

			/// <summary>
			/// Convert the nodes in the octree to a palette with a maximum of colorCount colors
			/// </summary>
			/// <param name="colorCount">The maximum number of colors</param>
			/// <returns>An arraylist with the palettized colors</returns>
			public ArrayList Palletize ( int colorCount ) {
				while ( Leaves > colorCount ) {
					Reduce();
				}

				// Now palettize the nodes
				ArrayList	palette = new ArrayList ( Leaves ) ;
				int			paletteIndex = 0 ;
				_root.ConstructPalette ( palette , ref paletteIndex ) ;

				// And return the palette
				return palette ;
			}

			/// <summary>
			/// Get the palette index for the passed color
			/// </summary>
			/// <param name="pixel"></param>
			/// <returns></returns>
			public int GetPaletteIndex ( Color32* pixel ) {
				return _root.GetPaletteIndex ( pixel , 0 ) ;
			}

			/// <summary>
			/// Mask used when getting the appropriate pixels for a given node
			/// </summary>
			private static int[] mask = new int[8] { 0x80 , 0x40 , 0x20 , 0x10 , 0x08 , 0x04 , 0x02 , 0x01 } ;

			/// <summary>
			/// The root of the octree
			/// </summary>
			private	OctreeNode _root ;

			/// <summary>
			/// Number of leaves in the tree
			/// </summary>
			private int _leafCount ;

			/// <summary>
			/// Array of reducible nodes
			/// </summary>
			private OctreeNode[] _reducibleNodes ;

			/// <summary>
			/// Maximum number of significant bits in the image
			/// </summary>
			private int _maxColorBits ;

			/// <summary>
			/// Store the last node quantized
			/// </summary>
			private OctreeNode _previousNode ;

			/// <summary>
			/// Cache the previous color quantized
			/// </summary>
			private int _previousColor ;

			/// <summary>
			/// Class which encapsulates each node in the tree
			/// </summary>
			protected class OctreeNode {
				/// <summary>
				/// Construct the node
				/// </summary>
				/// <param name="level">The level in the tree = 0 - 7</param>
				/// <param name="colorBits">The number of significant color bits in the image</param>
				/// <param name="octree">The tree to which this node belongs</param>
				public OctreeNode ( int level , int colorBits , Octree octree ) {
					// Construct the new node
					_leaf = ( level == colorBits ) ;

					_red = _green = _blue = 0 ;
					_pixelCount = 0 ;

					// If a leaf, increment the leaf count
					if ( _leaf ) {
						octree.Leaves++ ;
						_nextReducible = null ;
						_children = null ; 
					} else {
						// Otherwise add this to the reducible nodes
						_nextReducible = octree.ReducibleNodes[level] ;
						octree.ReducibleNodes[level] = this ;
						_children = new OctreeNode[8] ;
					}
				}

				/// <summary>
				/// Add a color into the tree
				/// </summary>
				/// <param name="pixel">The color</param>
				/// <param name="colorBits">The number of significant color bits</param>
				/// <param name="level">The level in the tree</param>
				/// <param name="octree">The tree to which this node belongs</param>
				public void AddColor ( Color32* pixel , int colorBits , int level , Octree octree ) {
					// Update the color information if this is a leaf
					if ( _leaf ) {
						Increment ( pixel ) ;
						// Setup the previous node
						octree.TrackPrevious ( this ) ;
					} else {
						// Go to the next level down in the tree
						int	shift = 7 - level ;
						int index = ( ( pixel->Red & mask[level] ) >> ( shift - 2 ) ) |
									( ( pixel->Green & mask[level] ) >> ( shift - 1 ) ) |
									( ( pixel->Blue & mask[level] ) >> ( shift ) ) ;

						OctreeNode	child = _children[index] ;

						if ( null == child ) {
							// Create a new child node & store in the array
							child = new OctreeNode ( level + 1 , colorBits , octree ) ; 
							_children[index] = child ;
						}

						// Add the color to the child node
						child.AddColor ( pixel , colorBits , level + 1 , octree ) ;
					}

				}

				/// <summary>
				/// Get/Set the next reducible node
				/// </summary>
				public OctreeNode NextReducible {
					get { return _nextReducible ; }
					set { _nextReducible = value ; }
				}

				/// <summary>
				/// Return the child nodes
				/// </summary>
				public OctreeNode[] Children {
					get { return _children ; }
				}

				/// <summary>
				/// Reduce this node by removing all of its children
				/// </summary>
				/// <returns>The number of leaves removed</returns>
				public int Reduce ( ) {
					_red = _green = _blue = 0 ;
					int	children = 0 ;

					// Loop through all children and add their information to this node
					for ( int index = 0 ; index < 8 ; index++ ) {
						if ( null != _children[index] ) {
							_red += _children[index]._red ;
							_green += _children[index]._green ;
							_blue += _children[index]._blue ;
							_pixelCount += _children[index]._pixelCount ;
							++children ;
							_children[index] = null ;
						}
					}

					// Now change this to a leaf node
					_leaf = true ;

					// Return the number of nodes to decrement the leaf count by
					return ( children - 1 ) ;
				}

				/// <summary>
				/// Traverse the tree, building up the color palette
				/// </summary>
				/// <param name="palette">The palette</param>
				/// <param name="paletteIndex">The current palette index</param>
				public void ConstructPalette ( ArrayList palette , ref int paletteIndex ) {
					if ( _leaf ) {
						// Consume the next palette index
						_paletteIndex = paletteIndex++ ;

						// And set the color of the palette entry
						palette.Add ( Color.FromArgb ( _red / _pixelCount , _green / _pixelCount , _blue / _pixelCount ) ) ;
					} else {
						// Loop through children looking for leaves
						for ( int index = 0 ; index < 8 ; index++ ) {
							if ( null != _children[index] ) {
								_children[index].ConstructPalette ( palette , ref paletteIndex ) ;
							}
						}
					}
				}

				/// <summary>
				/// Return the palette index for the passed color
				/// </summary>
				public int GetPaletteIndex ( Color32* pixel , int level ) {
					int	paletteIndex = _paletteIndex ;

					if ( !_leaf ) {
						int	shift = 7 - level ;
						int index = ( ( pixel->Red & mask[level] ) >> ( shift - 2 ) ) |
									( ( pixel->Green & mask[level] ) >> ( shift - 1 ) ) |
									( ( pixel->Blue & mask[level] ) >> ( shift ) ) ;

						if ( null != _children[index] ) {
							paletteIndex = _children[index].GetPaletteIndex ( pixel , level + 1 ) ;
						} else {
							throw new Exception ( "Didn't expect this!" ) ;
						}
					}

					return paletteIndex ;
				}

				/// <summary>
				/// Increment the pixel count and add to the color information
				/// </summary>
				public void Increment ( Color32* pixel ) {
					_pixelCount++ ;
					_red += pixel->Red ;
					_green += pixel->Green ;
					_blue += pixel->Blue ;
				}

				/// <summary>
				/// Flag indicating that this is a leaf node
				/// </summary>
				private	bool _leaf ;

				/// <summary>
				/// Number of pixels in this node
				/// </summary>
				private	int _pixelCount ;

				/// <summary>
				/// Red component
				/// </summary>
				private	int _red ;

				/// <summary>
				/// Green Component
				/// </summary>
				private	int _green ;

				/// <summary>
				/// Blue component
				/// </summary>
				private int _blue ;

				/// <summary>
				/// Pointers to any child nodes
				/// </summary>
				private OctreeNode[] _children ;

				/// <summary>
				/// Pointer to next reducible node
				/// </summary>
				private OctreeNode _nextReducible ;

				/// <summary>
				/// The index of this node in the palette
				/// </summary>
				private	int _paletteIndex ;
			}
		}
	}
}
