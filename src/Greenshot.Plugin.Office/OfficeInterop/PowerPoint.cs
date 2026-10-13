// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: https://getgreenshot.org/
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
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Runtime.InteropServices;

namespace Greenshot.Plugin.Office.OfficeInterop
{
    // The members of PowerPoint we use, see OfficeApplication for how this works

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointApplication
    {
        IPowerPointPresentations Presentations { get; }
        IPowerPointDocumentWindow ActiveWindow { get; }
        MsoTriState Visible { set; }
        void Activate();
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointPresentations
    {
        int Count { get; }
        IPowerPointPresentation Item(object index);
        IPowerPointPresentation Add();
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointPresentation
    {
        IPowerPointApplication Application { get; }
        string Name { get; }
        int ReadOnly { get; }
        bool Final { get; }
        IPowerPointPageSetup PageSetup { get; }
        IPowerPointSlides Slides { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointPageSetup
    {
        float SlideWidth { get; }
        float SlideHeight { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointSlides
    {
        int Count { get; }
        IPowerPointSlide Add(int index, PpSlideLayout layout);
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointSlide
    {
        IPowerPointShapes Shapes { get; }
        int SlideNumber { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointShapes
    {
        IPowerPointShape Item(object index);
        IPowerPointShape AddPicture(string fileName, MsoTriState linkToFile, MsoTriState saveWithDocument, float left, float top, float width, float height);
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointShape
    {
        float Left { get; set; }
        float Top { get; set; }
        float Width { get; set; }
        float Height { get; set; }
        MsoTriState LockAspectRatio { set; }
        string AlternativeText { get; set; }
        IPowerPointTextFrame TextFrame { get; }
        void ScaleHeight(float factor, MsoTriState relativeToOriginalSize, MsoScaleFrom scale);
        void ScaleWidth(float factor, MsoTriState relativeToOriginalSize, MsoScaleFrom scale);
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointTextFrame
    {
        IPowerPointTextRange TextRange { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointTextRange
    {
        string Text { get; set; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointDocumentWindow
    {
        IPowerPointView View { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IPowerPointView
    {
        void GotoSlide(int index);
    }

    /// <summary>
    /// PpSlideLayout, all values: the slide layout is a setting, stored by name
    /// </summary>
    public enum PpSlideLayout
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
