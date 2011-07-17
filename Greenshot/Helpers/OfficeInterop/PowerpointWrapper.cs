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
		ISlide Add(int Index, PPSlideLayout Layout);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.presentation_members.aspx
	public interface IPresentation : Common {
		ISlides Slides{ get;}
	}
	
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.presentations_members.aspx
	public interface IPresentations : Common {
		int Count {get;}
		IPresentation Add(MsoTriState WithWindow);
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

		private static IPowerpointApplication PowerpointApplication() {
			return (IPowerpointApplication)COMWrapper.GetOrCreateInstance(typeof(IPowerpointApplication));
		}

		private static void AddPictureToPresentation(IPresentation presentation, string tmpFile, Image image, ICaptureDetails captureDetails) {
			if (presentation != null) {
				//ISlide slide = presentation.Slides.AddSlide( presentation.Slides.Count + 1, PPSlideLayout.ppLayoutPictureWithCaption);
				LOG.DebugFormat("Slides before {0}", presentation.Slides.Count);
				ISlide slide = presentation.Slides.Add( presentation.Slides.Count + 1,  PPSlideLayout.ppLayoutPictureWithCaption);
				LOG.DebugFormat("Slides after {0}", presentation.Slides.Count);
				// Shapes[2] is the image shape on this layout.
				IShape shapeForLocation = slide.Shapes.item(2);
				shapeForLocation.Width = image.Width;
				shapeForLocation.Height = image.Height;
				LOG.DebugFormat("Shape {0},{1},{2},{3}", shapeForLocation.Left, shapeForLocation.Top, image.Width, image.Height);
				IShape shape = slide.Shapes.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, shapeForLocation.Left, shapeForLocation.Top, image.Width, image.Height);
				shape.Width = image.Width;
				shape.Height = image.Height;
				shape.ScaleWidth(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromMiddle);
				shape.ScaleHeight(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromMiddle);
				slide.Shapes.item(1).TextFrame.TextRange.Text = captureDetails.Title;
			}
		}

		private static void InsertIntoNewPresentation(IPowerpointApplication powerpointApplication, string tmpFile, Image image, ICaptureDetails captureDetails) {
			LOG.Debug("No Presentation, creating a new Presentation");
			IPresentation presentation = powerpointApplication.Presentations.Add(MsoTriState.msoTrue);
			AddPictureToPresentation(presentation, tmpFile, image, captureDetails);
		}

		public static void ExportToPowerpoint(string tmpFile, Image image, ICaptureDetails captureDetails) {
			using( IPowerpointApplication powerpointApplication = PowerpointApplication() ) {
				if (powerpointApplication != null) {
					LOG.DebugFormat("Open Presentations: {0}", powerpointApplication.Presentations.Count);
					if (powerpointApplication.Presentations.Count > 0) {
						if (powerpointApplication.ActivePresentation != null) {
							LOG.Debug("Presentation found!");
							AddPictureToPresentation(powerpointApplication.ActivePresentation, tmpFile, image, captureDetails);
						}
					} else {
						InsertIntoNewPresentation(powerpointApplication, tmpFile, image, captureDetails);
					}
					powerpointApplication.Visible = true;
				}
			}
		}
	}
}
