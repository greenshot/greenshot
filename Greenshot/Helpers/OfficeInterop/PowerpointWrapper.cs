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
using System.Runtime.Remoting.Messaging;
using Greenshot.Interop;
using Greenshot.Plugin;

namespace Greenshot.Helpers.OfficeInterop {
	// See http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.application_members.aspx
	[ComProgId("Powerpoint.Application")]
	public interface IPowerpointApplication : Common {
		IPresentation ActivePresentation { get; }
		IPresentations Presentations {get;}
		bool Visible {get; set;}
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.slides_members.aspx
	public interface ISlides : Common {
		int Count {get;}
		ISlide Add(int Index, int layout);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.presentation_members.aspx
	public interface IPresentation : Common {
		string Name { get; }
		ISlides Slides{ get;}
	}
	
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.presentations_members.aspx
	public interface IPresentations : Common, Collection {
		IPresentation Add(MsoTriState WithWindow);
		IPresentation item(int index);
	}
	
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.slide_members.aspx
	public interface ISlide : Common {
		IShapes Shapes {get;}
		void Select();
		int SlideNumber { get; }
	}
	
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.shapes_members.aspx
	public interface IShapes : Common, IEnumerable {
		int Count {get;}
		IShape item(int index);
		IShape AddPicture(string FileName, MsoTriState LinkToFile, MsoTriState SaveWithDocument, float Left, float Top, float Width, float Height);
	}
	
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.shape_members.aspx
	public interface IShape : Common {
		float Left { get; set;}
		float Top { get; set;}
		float Width { get; set;}
		float Height { get; set;}
		ITextFrame TextFrame { get; }
		void ScaleWidth(float Factor, MsoTriState RelativeToOriginalSize, MsoScaleFrom fScale);
		void ScaleHeight(float Factor, MsoTriState RelativeToOriginalSize, MsoScaleFrom fScale);
	}
	
	public interface ITextFrame : Common {
		ITextRange TextRange {get;}
	}
	public interface ITextRange : Common {
		string Text { get; set; }
	}

	public enum PPSlideLayout : int {
		ppLayoutMixed = -2,
		ppLayoutTitle = 1,
		ppLayoutText = 2,
		ppLayoutTwoColumnText = 3,
		ppLayoutTable = 4,
		ppLayoutTextAndChart = 5,
		ppLayoutChartAndText = 6,
		ppLayoutOrgchart = 7,
		ppLayoutChart = 8,
		ppLayoutTextAndClipart = 9,
		ppLayoutClipartAndText = 10,
		ppLayoutTitleOnly = 11,
		ppLayoutBlank = 12,
		ppLayoutTextAndObject = 13,
		ppLayoutObjectAndText = 14,
		ppLayoutLargeObject = 15,
		ppLayoutObject = 16,
		ppLayoutTextAndMediaClip = 17,
		ppLayoutMediaClipAndText = 18,
		ppLayoutObjectOverText = 19,
		ppLayoutTextOverObject = 20,
		ppLayoutTextAndTwoObjects = 21,
		ppLayoutTwoObjectsAndText = 22,
		ppLayoutTwoObjectsOverText = 23,
		ppLayoutFourObjects = 24,
		ppLayoutVerticalText = 25,
		ppLayoutClipArtAndVerticalText = 26,
		ppLayoutVerticalTitleAndText = 27,
		ppLayoutVerticalTitleAndTextOverChart = 28,
		ppLayoutTwoObjects = 29,
		ppLayoutObjectAndTwoObjects = 30,
		ppLayoutTwoObjectsAndObject = 31,
		ppLayoutCustom = 32,
		ppLayoutSectionHeader = 33,
		ppLayoutComparison = 34,
		ppLayoutContentWithCaption = 35,
		ppLayoutPictureWithCaption = 36
	}

	public class PowerpointExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PowerpointExporter));

		private static IPowerpointApplication GetOrCreatePowerpointApplication() {
			return (IPowerpointApplication)COMWrapper.GetOrCreateInstance(typeof(IPowerpointApplication));
		}

		private static IPowerpointApplication GetPowerpointApplication() {
			return (IPowerpointApplication)COMWrapper.GetInstance(typeof(IPowerpointApplication));
		}

		/// <summary>
		/// Get the captions of all the open powerpoint presentations
		/// </summary>
		/// <returns></returns>
		public static System.Collections.Generic.List<string> GetPowerpointPresentations() {
			System.Collections.Generic.List<string> presentations = new System.Collections.Generic.List<string>();
			try {
				using( IPowerpointApplication powerpointApplication = GetPowerpointApplication() ) {
					if (powerpointApplication != null) {
						LOG.DebugFormat("Open Presentations: {0}", powerpointApplication.Presentations.Count);
						for(int i = 1; i <= powerpointApplication.Presentations.Count ; i ++) {
							IPresentation presentation = powerpointApplication.Presentations.item(i);
							if (presentation != null) {
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
		/// <param name="presentationName"></param>
		/// <param name="tmpFile"></param>
		/// <param name="imageSize"></param>
		/// <param name="captureDetails"></param>
		/// <returns></returns>
		public static bool ExportToPresentation(string presentationName, string tmpFile, Size imageSize, ICaptureDetails captureDetails) {
			using( IPowerpointApplication powerpointApplication = GetPowerpointApplication() ) {
				if (powerpointApplication != null) {
					LOG.DebugFormat("Open Presentations: {0}", powerpointApplication.Presentations.Count);
					for(int i = 1; i <= powerpointApplication.Presentations.Count ; i ++) {
						IPresentation presentation = powerpointApplication.Presentations.item(i);
						if (presentation != null && presentation.Name.StartsWith(presentationName)) {
							try {
								AddPictureToPresentation(presentation, tmpFile, imageSize, captureDetails);
								return true;
							} catch (Exception e){
								LOG.Error(e);
							}
						}
					}
				}
			}
			return false;
		}

		private static void AddPictureToPresentation(IPresentation presentation, string tmpFile, Size imageSize, ICaptureDetails captureDetails) {
			if (presentation != null) {
				//ISlide slide = presentation.Slides.AddSlide( presentation.Slides.Count + 1, PPSlideLayout.ppLayoutPictureWithCaption);
				ISlide slide;
				float left = 0;
				float top = 0;
				bool isLayoutPictureWithCaption = false;
				try {
					slide = presentation.Slides.Add( presentation.Slides.Count + 1,  (int)PPSlideLayout.ppLayoutPictureWithCaption);
					isLayoutPictureWithCaption = true;
					// Shapes[2] is the image shape on this layout.
					IShape shapeForLocation = slide.Shapes.item(2);
					shapeForLocation.Width = imageSize.Width;
					shapeForLocation.Height = imageSize.Height;
					left = shapeForLocation.Left;
					top = shapeForLocation.Top;
					LOG.DebugFormat("Shape {0},{1},{2},{3}", shapeForLocation.Left, shapeForLocation.Top, imageSize.Width, imageSize.Height);
				} catch (Exception e) {
					LOG.Error(e);
					slide = presentation.Slides.Add( presentation.Slides.Count + 1,  (int)PPSlideLayout.ppLayoutBlank);
				}
				IShape shape = slide.Shapes.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, left, top, imageSize.Width, imageSize.Height);
				shape.Width = imageSize.Width;
				shape.Height = imageSize.Height;
				shape.ScaleWidth(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromMiddle);
				shape.ScaleHeight(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromMiddle);
				if (isLayoutPictureWithCaption) {
					try {
						// Using try/catch to make sure problems with the text range don't give an exception.
						shape.TextFrame.TextRange.Text = captureDetails.Title;
					} catch {};
				}
			}
		}

		public static void InsertIntoNewPresentation(string tmpFile, Size imageSize, ICaptureDetails captureDetails) {
			using( IPowerpointApplication powerpointApplication = GetOrCreatePowerpointApplication() ) {
				if (powerpointApplication != null) {
					powerpointApplication.Visible = true;
					IPresentation presentation = powerpointApplication.Presentations.Add(MsoTriState.msoTrue);
					AddPictureToPresentation(presentation, tmpFile, imageSize, captureDetails);
				}
			}
		}
	}
}
