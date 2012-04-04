/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Greenshot.Interop;

namespace Greenshot.Interop.Office {
	public class PowerpointExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PowerpointExporter));
		private static string version = null;

		public static bool isAfter2003() {
			if (version != null) {
				return !version.StartsWith("11");
			}
			return false;
		}
		/// <summary>
		/// Get the captions of all the open powerpoint presentations
		/// </summary>
		/// <returns></returns>
		public static System.Collections.Generic.List<string> GetPowerpointPresentations() {
			System.Collections.Generic.List<string> presentations = new System.Collections.Generic.List<string>();
			try {
				using (IPowerpointApplication powerpointApplication = COMWrapper.GetInstance<IPowerpointApplication>()) {
					if (powerpointApplication != null) {
						if (version == null) {
							version = powerpointApplication.Version;
						}
						LOG.DebugFormat("Open Presentations: {0}", powerpointApplication.Presentations.Count);
						for (int i = 1; i <= powerpointApplication.Presentations.Count; i++) {
							IPresentation presentation = powerpointApplication.Presentations.item(i);
							if (presentation != null && presentation.ReadOnly != MsoTriState.msoTrue) {
								if (isAfter2003()) {
									if (presentation.Final) {
										continue;
									}
								}
								presentations.Add(presentation.Name);
							}
						}
					}
				}
			} catch (Exception ex) {
				LOG.Warn("Problem retrieving word destinations, ignoring: ", ex);
			}

			return presentations;
		}

		/// <summary>
		/// Export the image from the tmpfile to the presentation with the supplied name
		/// </summary>
		/// <param name="presentationName">Name of the presentation to insert to</param>
		/// <param name="tmpFile">Filename of the image file to insert</param>
		/// <param name="imageSize">Size of the image</param>
		/// <param name="title">A string with the image title</param>
		/// <returns></returns>
		public static bool ExportToPresentation(string presentationName, string tmpFile, Size imageSize, string title) {
			using (IPowerpointApplication powerpointApplication = COMWrapper.GetInstance<IPowerpointApplication>()) {
				if (powerpointApplication != null) {
					LOG.DebugFormat("Open Presentations: {0}", powerpointApplication.Presentations.Count);
					for (int i = 1; i <= powerpointApplication.Presentations.Count; i++) {
						IPresentation presentation = powerpointApplication.Presentations.item(i);
						if (presentation != null && presentation.Name.StartsWith(presentationName)) {
							try {
								AddPictureToPresentation(presentation, tmpFile, imageSize, title);
								return true;
							} catch (Exception e) {
								LOG.Error(e);
							}
						}
					}
				}
			}
			return false;
		}

		private static void AddPictureToPresentation(IPresentation presentation, string tmpFile, Size imageSize, string title) {
			if (presentation != null) {
				//ISlide slide = presentation.Slides.AddSlide( presentation.Slides.Count + 1, PPSlideLayout.ppLayoutPictureWithCaption);
				ISlide slide;
				float left = (presentation.PageSetup.SlideWidth / 2) - (imageSize.Width / 2) ;
				float top = (presentation.PageSetup.SlideHeight / 2) - (imageSize.Height / 2);
				float width = imageSize.Width;
				float height = imageSize.Height;
				bool isLayoutPictureWithCaption = false;
				IShape shapeForCaption = null;
				bool isWidthResized = false;
				bool isHeightResized = false;
				try {
					slide = presentation.Slides.Add(presentation.Slides.Count + 1, (int)PPSlideLayout.ppLayoutPictureWithCaption);
					isLayoutPictureWithCaption = true;
					// Shapes[2] is the image shape on this layout.
					shapeForCaption = slide.Shapes.item(1);
					IShape shapeForLocation = slide.Shapes.item(2);
					if (width > shapeForLocation.Width) {
						width = shapeForLocation.Width;
						isWidthResized = true;
						left = shapeForLocation.Left;
					} else {
						//left = (shapeForLocation.Left + (shapeForLocation.Width / 2)) - (imageSize.Width / 2);
						shapeForLocation.Width = width;
						shapeForLocation.Left = left;
					}
					if (height > shapeForLocation.Height) {
						height = shapeForLocation.Height;
						isHeightResized = true;
						top = shapeForLocation.Top;
					} else {
						top = (shapeForLocation.Top + (shapeForLocation.Height / 2)) - (imageSize.Height / 2);
						shapeForLocation.Height = height;
						//shapeForLocation.Top = top;
					}
					//shapeForLocation.Top = top;
					//LOG.DebugFormat("Shape {0},{1},{2},{3}", shapeForLocation.Left, shapeForLocation.Top, imageSize.Width, imageSize.Height);
				} catch (Exception e) {
					LOG.Error(e);
					slide = presentation.Slides.Add(presentation.Slides.Count + 1, (int)PPSlideLayout.ppLayoutBlank);
				}
				IShape shape = slide.Shapes.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, 0, 0, width, height);
				shape.LockAspectRatio = MsoTriState.msoTrue;
				shape.Left = left;
				shape.Top = top;
				if (!isHeightResized) {
					shape.ScaleHeight(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromMiddle);
				}
				if (!isWidthResized) {
					shape.ScaleWidth(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromMiddle);
				}
				shape.AlternativeText = title;
				if (isLayoutPictureWithCaption && shapeForCaption != null) {
					try {
						// Using try/catch to make sure problems with the text range don't give an exception.
						ITextFrame textFrame = shapeForCaption.TextFrame;
						textFrame.TextRange.Text = title;
					} catch (Exception ex) {
						LOG.Warn("Problem setting the title to a text-range", ex);
					}
				}
				presentation.Application.ActiveWindow.View.GotoSlide(slide.SlideNumber);
				presentation.Application.Activate();
			}
		}

		public static void InsertIntoNewPresentation(string tmpFile, Size imageSize, string title) {
			using (IPowerpointApplication powerpointApplication = COMWrapper.GetOrCreateInstance<IPowerpointApplication>()) {
				if (powerpointApplication != null) {
					powerpointApplication.Visible = true;
					IPresentation presentation = powerpointApplication.Presentations.Add(MsoTriState.msoTrue);
					AddPictureToPresentation(presentation, tmpFile, imageSize, title);
					presentation.Application.Activate();
				}
			}
		}
	}
}
