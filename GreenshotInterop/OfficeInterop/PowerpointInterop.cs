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
using System.Collections;
using System.Drawing;
using System.Runtime.Remoting.Messaging;
using Greenshot.Interop;

namespace Greenshot.Interop.Office {
	// See http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.application_members.aspx
	[ComProgId("Powerpoint.Application")]
	public interface IPowerpointApplication : Common {
		IPresentation ActivePresentation { get; }
		IPresentations Presentations { get; }
		bool Visible { get; set; }
		void Activate();
		IPowerpointWindow ActiveWindow { get; }
		string Version { get; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.slides_members.aspx
	public interface ISlides : Common {
		int Count { get; }
		ISlide Add(int Index, int layout);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.documentwindow.view.aspx
	public interface IPowerpointWindow : Common {
		void Activate();
		IPowerpointView View { get; }
	}
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.view_members.aspx
	public interface IPowerpointView : Common {
		IZoom Zoom { get; }
		void GotoSlide(int index);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.presentation_members.aspx
	public interface IPresentation : Common {
		string Name { get; }
		ISlides Slides { get; }
		IPowerpointApplication Application { get; }
		MsoTriState ReadOnly { get; }
		bool Final { get; set; }
		IPageSetup PageSetup { get; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.presentations_members.aspx
	public interface IPresentations : Common, Collection {
		IPresentation Add(MsoTriState WithWindow);
		IPresentation item(int index);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.pagesetup_members.aspx
	public interface IPageSetup : Common, Collection {
		float SlideWidth { get; set; }
		float SlideHeight { get; set; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.slide_members.aspx
	public interface ISlide : Common {
		IShapes Shapes { get; }
		void Select();
		int SlideNumber { get; }

	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.shapes_members.aspx
	public interface IShapes : Common, IEnumerable {
		int Count { get; }
		IShape item(int index);
		IShape AddPicture(string FileName, MsoTriState LinkToFile, MsoTriState SaveWithDocument, float Left, float Top, float Width, float Height);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.shape_members.aspx
	public interface IShape : Common {
		float Left { get; set; }
		float Top { get; set; }
		float Width { get; set; }
		float Height { get; set; }
		ITextFrame TextFrame { get; }
		void ScaleWidth(float Factor, MsoTriState RelativeToOriginalSize, MsoScaleFrom fScale);
		void ScaleHeight(float Factor, MsoTriState RelativeToOriginalSize, MsoScaleFrom fScale);
		string AlternativeText { get; set; }
		MsoTriState LockAspectRatio { get; set; }
	}

	public interface ITextFrame : Common {
		ITextRange TextRange { get; }
		MsoTriState HasText { get; }
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
}
