/// <summary>
/// Parts of this class were taken from BlurEffect.cs of Paint.NET 3.0.1, 
/// which was released under MIT license.
/// http://www.getpaint.net
/// Some of this code has been adapted for integration with Greenshot.
/// See Paint.NET copyright notice below.
/// </summary>

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Drawing.Fields;
using Greenshot.Drawing.Filters;

namespace Greenshot.Drawing.Filters {
    [Serializable()] 
	public class BlurFilter : AbstractFilter {
		public double previewQuality;
		public double PreviewQuality {
			get { return previewQuality; }
			set { previewQuality = value; OnPropertyChanged("PreviewQuality"); }
		}
		
		public BlurFilter(DrawableContainer parent) : base(parent) {
			AddField(FieldFactory.CreateField(FieldType.BLUR_RADIUS, GetType()));
			AddField(FieldFactory.CreateField(FieldType.PREVIEW_QUALITY));
		}

        public static int[] CreateGaussianBlurRow(int amount) {
            int size = 1 + (amount * 2);
            int[] weights = new int[size];

            for (int i = 0; i <= amount; ++i)
            {
                // 1 + aa - aa + 2ai - ii
                weights[i] = 16 * (i + 1);
                weights[weights.Length - i - 1] = weights[i];
            }

            return weights;
        }
    
        public unsafe override void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle rect, RenderMode renderMode) {
        	applyRect = IntersectRectangle(applyBitmap.Size, rect);

        	if (applyRect.Height <= 0 || applyRect.Width <= 0) {
				return;
			}
        	
        	int blurRadius = GetFieldValueAsInt(FieldType.BLUR_RADIUS);
        	double previewQuality = GetFieldValueAsDouble(FieldType.PREVIEW_QUALITY);

        	// do nothing when nothing can be done!
			if (blurRadius < 1) {
				return;
			}

        	using (BitmapBuffer bbbDest = new BitmapBuffer(applyBitmap, applyRect)) {
        		bbbDest.Lock();
        		using (BitmapBuffer bbbSrc = new BitmapBuffer(applyBitmap, applyRect)) {
        			bbbSrc.Lock();
		        	Random rand = new Random();

		        	int r = blurRadius;
		            int[] w = CreateGaussianBlurRow(r);
		            int wlen = w.Length;

		            for (int y = 0; y < applyRect.Height; ++y) {
		                long* waSums = stackalloc long[wlen];
		                long* wcSums = stackalloc long[wlen];
		                long* aSums = stackalloc long[wlen];
		                long* bSums = stackalloc long[wlen];
		                long* gSums = stackalloc long[wlen];
		                long* rSums = stackalloc long[wlen];
		                long waSum = 0;
		                long wcSum = 0;
		                long aSum = 0;
		                long bSum = 0;
		                long gSum = 0;
		                long rSum = 0;
		
		                for (int wx = 0; wx < wlen; ++wx) {
		                    int srcX = wx - r;
		                    waSums[wx] = 0;
		                    wcSums[wx] = 0;
		                    aSums[wx] = 0;
		                    bSums[wx] = 0;
		                    gSums[wx] = 0;
		                    rSums[wx] = 0;
		
		                    if (srcX >= 0 && srcX < bbbDest.Width) {
		                        for (int wy = 0; wy < wlen; ++wy) {
		                            int srcY = y + wy - r;
		
		                            if (srcY >= 0 && srcY < bbbDest.Height) {
		                                int[] colors = bbbSrc.GetColorArrayAt(srcX, srcY);
		                                int wp = w[wy];
		
		                                waSums[wx] += wp;
		                                wp *= colors[0] + (colors[0] >> 7);
		                                wcSums[wx] += wp;
		                                wp >>= 8;
		
		                                aSums[wx] += wp * colors[0];
		                                bSums[wx] += wp * colors[3];
		                                gSums[wx] += wp * colors[2];
		                                rSums[wx] += wp * colors[1];
		                            }
		                        }
		
		                        int wwx = w[wx];
		                        waSum += wwx * waSums[wx];
		                        wcSum += wwx * wcSums[wx];
		                        aSum += wwx * aSums[wx];
		                        bSum += wwx * bSums[wx];
		                        gSum += wwx * gSums[wx];
		                        rSum += wwx * rSums[wx];
		                    }
		                }
		
		                wcSum >>= 8;
		
		                if (waSum == 0 || wcSum == 0) {
		                   SetColorAt(bbbDest, 0, y, new int[]{0,0,0,0});
		                } else {
		                    int alpha = (int)(aSum / waSum);
		                    int blue = (int)(bSum / wcSum);
		                    int green = (int)(gSum / wcSum);
		                    int red = (int)(rSum / wcSum);
		
		                    SetColorAt(bbbDest, 0, y, new int[]{alpha, red, green, blue});
		                }
		
						for (int x = 1; x < applyRect.Width; ++x) {
							for (int i = 0; i < wlen - 1; ++i) {
								waSums[i] = waSums[i + 1];
								wcSums[i] = wcSums[i + 1];
								aSums[i] = aSums[i + 1];
								bSums[i] = bSums[i + 1];
								gSums[i] = gSums[i + 1];
								rSums[i] = rSums[i + 1];
							}
	
							waSum = 0;
							wcSum = 0;
							aSum = 0;
							bSum = 0;
							gSum = 0;
							rSum = 0;
	
							int wx;
							for (wx = 0; wx < wlen - 1; ++wx) {
								long wwx = (long)w[wx];
								waSum += wwx * waSums[wx];
								wcSum += wwx * wcSums[wx];
								aSum += wwx * aSums[wx];
								bSum += wwx * bSums[wx];
								gSum += wwx * gSums[wx];
								rSum += wwx * rSums[wx];
							}
		
		                    wx = wlen - 1;
		
		                    waSums[wx] = 0;
		                    wcSums[wx] = 0;
		                    aSums[wx] = 0;
		                    bSums[wx] = 0;
		                    gSums[wx] = 0;
		                    rSums[wx] = 0;
		
							int srcX = x + wx - r;
		
							if (srcX >= 0 && srcX < applyRect.Width) {
								for (int wy = 0; wy < wlen; ++wy) {
									int srcY = y + wy - r;
									// only when in EDIT mode, ignore some pixels depending on preview quality
									if ((renderMode==RenderMode.EXPORT || rand.NextDouble()<previewQuality) && srcY >= 0 && srcY < applyRect.Height) {
										int[] colors = bbbSrc.GetColorArrayAt(srcX, srcY);
										int wp = w[wy];
										
										waSums[wx] += wp;
										wp *= colors[0] + (colors[0] >> 7);
										wcSums[wx] += wp;
										wp >>= 8;
										
										aSums[wx] += wp * (long)colors[0];
										bSums[wx] += wp * (long)colors[3];
										gSums[wx] += wp * (long)colors[2];
										rSums[wx] += wp * (long)colors[1];
									}
								}
								
								int wr = w[wx];
								waSum += (long)wr * waSums[wx];
								wcSum += (long)wr * wcSums[wx];
								aSum += (long)wr * aSums[wx];
								bSum += (long)wr * bSums[wx];
								gSum += (long)wr * gSums[wx];
								rSum += (long)wr * rSums[wx];
							}
	
							wcSum >>= 8;
		
							if (waSum == 0 || wcSum == 0) {
								SetColorAt(bbbDest, x, y, new int[]{0,0,0,0});
							} else {
								int alpha = (int)(aSum / waSum);
								int blue = (int)(bSum / wcSum);
								int green = (int)(gSum / wcSum);
								int red = (int)(rSum / wcSum);
		
								SetColorAt(bbbDest, x, y, new int[]{alpha, red, green, blue});
							}
						}
					}
				}
	            bbbDest.DrawTo(graphics, applyRect.Location);
        	}
		}
        
        private void SetColorAt(BitmapBuffer bbb, int x, int y, int[] colors) {
        	if(parent.Contains(applyRect.Left+x, applyRect.Top+y) ^ Invert) {
        		bbb.SetColorArrayAt(x, y, colors);
        	}
        }
    }
}
