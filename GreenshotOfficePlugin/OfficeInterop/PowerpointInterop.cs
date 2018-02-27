#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System.Collections;
using GreenshotPlugin.Interop;

#endregion

namespace GreenshotOfficePlugin.OfficeInterop
{
	// See http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.application_members.aspx
	[ComProgId("Powerpoint.Application")]
	public interface IPowerpointApplication : IComCommon
	{
		IPresentation ActivePresentation { get; }
		IPresentations Presentations { get; }
		bool Visible { get; set; }
		IPowerpointWindow ActiveWindow { get; }
		string Version { get; }
		void Activate();
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.slides_members.aspx
	public interface ISlides : IComCommon
	{
		int Count { get; }
		ISlide Add(int Index, int layout);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.documentwindow.view.aspx
	public interface IPowerpointWindow : IComCommon
	{
		IPowerpointView View { get; }
		void Activate();
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.view_members.aspx
	public interface IPowerpointView : IComCommon
	{
		IZoom Zoom { get; }
		void GotoSlide(int index);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.presentation_members.aspx
	public interface IPresentation : IComCommon
	{
		string Name { get; }
		ISlides Slides { get; }
		IPowerpointApplication Application { get; }
		MsoTriState ReadOnly { get; }
		bool Final { get; set; }
		IPageSetup PageSetup { get; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.presentations_members.aspx
	public interface IPresentations : IComCommon, IComCollection
	{
		IPresentation Add(MsoTriState WithWindow);
		IPresentation item(int index);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.pagesetup_members.aspx
	public interface IPageSetup : IComCommon, IComCollection
	{
		float SlideWidth { get; set; }
		float SlideHeight { get; set; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.slide_members.aspx
	public interface ISlide : IComCommon
	{
		IShapes Shapes { get; }
		int SlideNumber { get; }
		void Select();
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.shapes_members.aspx
	public interface IShapes : IComCommon, IEnumerable
	{
		int Count { get; }
		IShape item(int index);
		IShape AddPicture(string FileName, MsoTriState LinkToFile, MsoTriState SaveWithDocument, float Left, float Top, float Width, float Height);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.shape_members.aspx
	public interface IShape : IComCommon
	{
		float Left { get; set; }
		float Top { get; set; }
		float Width { get; set; }
		float Height { get; set; }
		ITextFrame TextFrame { get; }
		string AlternativeText { get; set; }
		MsoTriState LockAspectRatio { get; set; }
		void ScaleWidth(float Factor, MsoTriState RelativeToOriginalSize, MsoScaleFrom fScale);
		void ScaleHeight(float Factor, MsoTriState RelativeToOriginalSize, MsoScaleFrom fScale);
	}

	public interface ITextFrame : IComCommon
	{
		ITextRange TextRange { get; }
		MsoTriState HasText { get; }
	}

	public interface ITextRange : IComCommon
	{
		string Text { get; set; }
	}

	public enum PPSlideLayout
	{
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