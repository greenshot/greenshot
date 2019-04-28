// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Drawing;
using Dapplo.Log;
using Dapplo.Windows.Com;
using Greenshot.Addon.Office.Configuration;
using Greenshot.Addon.Office.OfficeInterop;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using Shape = Microsoft.Office.Interop.PowerPoint.Shape;

namespace Greenshot.Addon.Office.OfficeExport
{
    /// <summary>
    /// Export logic for powerpoint
    /// </summary>
    public class PowerpointExporter
    {
        private static readonly LogSource Log = new LogSource();
        private readonly IOfficeConfiguration _officeConfiguration;
        private Version _powerpointVersion;

        /// <summary>
        /// Constructor used for dependency injection
        /// </summary>
        /// <param name="officeConfiguration"></param>
        public PowerpointExporter(IOfficeConfiguration officeConfiguration)
        {
            _officeConfiguration = officeConfiguration;
        }

        /// <summary>
        ///     Internal method to add a picture to a presentation
        /// </summary>
        /// <param name="presentation"></param>
        /// <param name="tmpFile"></param>
        /// <param name="imageSize"></param>
        /// <param name="title"></param>
        private void AddPictureToPresentation(IDisposableCom<Presentation> presentation, string tmpFile, Size imageSize, string title)
        {
            if (presentation != null)
            {
                //ISlide slide = presentation.Slides.AddSlide( presentation.Slides.Count + 1, PPSlideLayout.ppLayoutPictureWithCaption);
                IDisposableCom<Slide> slide = null;
                try
                {
                    float left, top;
                    using (var pageSetup = DisposableCom.Create(presentation.ComObject.PageSetup))
                    {
                        left = pageSetup.ComObject.SlideWidth / 2 - imageSize.Width / 2f;
                        top = pageSetup.ComObject.SlideHeight / 2 - imageSize.Height / 2f;
                    }
                    float width = imageSize.Width;
                    float height = imageSize.Height;
                    IDisposableCom<Shape> shapeForCaption = null;
                    bool hasScaledWidth = false;
                    bool hasScaledHeight = false;
                    try
                    {
                        using (var slides = DisposableCom.Create(presentation.ComObject.Slides))
                        {
                            slide = DisposableCom.Create(slides.ComObject.Add(slides.ComObject.Count + 1, _officeConfiguration.PowerpointSlideLayout));
                        }

                        using (var shapes = DisposableCom.Create(slide.ComObject.Shapes))
                        {
                            using (var shapeForLocation = DisposableCom.Create(shapes.ComObject[2]))
                            {
                                // Shapes[2] is the image shape on this layout.
                                shapeForCaption = DisposableCom.Create(shapes.ComObject[1]);
                                if (width > shapeForLocation.ComObject.Width)
                                {
                                    width = shapeForLocation.ComObject.Width;
                                    left = shapeForLocation.ComObject.Left;
                                    hasScaledWidth = true;
                                }
                                else
                                {
                                    shapeForLocation.ComObject.Left = left;
                                }
                                shapeForLocation.ComObject.Width = imageSize.Width;

                                if (height > shapeForLocation.ComObject.Height)
                                {
                                    height = shapeForLocation.ComObject.Height;
                                    top = shapeForLocation.ComObject.Top;
                                    hasScaledHeight = true;
                                }
                                else
                                {
                                    top = shapeForLocation.ComObject.Top + shapeForLocation.ComObject.Height / 2 - imageSize.Height / 2f;
                                }
                                shapeForLocation.ComObject.Height = imageSize.Height;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error().WriteLine(e, "Powerpoint shape creating failed");
                        using (var slides = DisposableCom.Create(presentation.ComObject.Slides))
                        {
                            slide = DisposableCom.Create(slides.ComObject.Add(slides.ComObject.Count + 1, PpSlideLayout.ppLayoutBlank));
                        }
                    }
                    using (var shapes = DisposableCom.Create(slide.ComObject.Shapes))
                    {
                        using (var shape = DisposableCom.Create(shapes.ComObject.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, 0, 0, width, height)))
                        {
                            if (_officeConfiguration.PowerpointLockAspectRatio)
                            {
                                shape.ComObject.LockAspectRatio = MsoTriState.msoTrue;
                            }
                            else
                            {
                                shape.ComObject.LockAspectRatio = MsoTriState.msoFalse;
                            }
                            shape.ComObject.ScaleHeight(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromMiddle);
                            shape.ComObject.ScaleWidth(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromMiddle);
                            if (hasScaledWidth)
                            {
                                shape.ComObject.Width = width;
                            }
                            if (hasScaledHeight)
                            {
                                shape.ComObject.Height = height;
                            }
                            shape.ComObject.Left = left;
                            shape.ComObject.Top = top;
                            shape.ComObject.AlternativeText = title;
                        }
                    }
                    if (shapeForCaption != null)
                    {
                        try
                        {
                            using (shapeForCaption)
                            {
                                // Using try/catch to make sure problems with the text range don't give an exception.
                                using (var textFrame = DisposableCom.Create(shapeForCaption.ComObject.TextFrame))
                                {
                                    using (var textRange = DisposableCom.Create(textFrame.ComObject.TextRange))
                                    {
                                        textRange.ComObject.Text = title;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warn().WriteLine(ex, "Problem setting the title to a text-range");
                        }
                    }
                    // Activate/Goto the slide
                    try
                    {
                        using (var application = DisposableCom.Create(presentation.ComObject.Application))
                        {
                            using (var activeWindow = DisposableCom.Create(application.ComObject.ActiveWindow))
                            {
                                using (var view = DisposableCom.Create(activeWindow.ComObject.View))
                                {
                                    view.ComObject.GotoSlide(slide.ComObject.SlideNumber);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn().WriteLine(ex, "Problem going to the slide");
                    }
                }
                finally
                {
                    slide?.Dispose();
                }
            }
        }

        /// <summary>
        ///     Export the image from the tmpfile to the presentation with the supplied name
        /// </summary>
        /// <param name="presentationName">Name of the presentation to insert to</param>
        /// <param name="tmpFile">Filename of the image file to insert</param>
        /// <param name="imageSize">Size of the image</param>
        /// <param name="title">A string with the image title</param>
        /// <returns></returns>
        public bool ExportToPresentation(string presentationName, string tmpFile, Size imageSize, string title)
        {
            using (var powerpointApplication = GetPowerPointApplication())
            {
                if (powerpointApplication == null)
                {
                    return false;
                }
                using (var presentations = DisposableCom.Create(powerpointApplication.ComObject.Presentations))
                {
                    Log.Debug().WriteLine("Open Presentations: {0}", presentations.ComObject.Count);
                    for (int i = 1; i <= presentations.ComObject.Count; i++)
                    {
                        using (var presentation = DisposableCom.Create(presentations.ComObject[i]))
                        {
                            if (presentation == null)
                            {
                                continue;
                            }
                            if (!presentation.ComObject.Name.StartsWith(presentationName))
                            {
                                continue;
                            }
                            try
                            {
                                AddPictureToPresentation(presentation, tmpFile, imageSize, title);
                                return true;
                            }
                            catch (Exception e)
                            {
                                Log.Error().WriteLine(e, "Adding picture to powerpoint failed");
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///     Call this to get the running PowerPoint application, or create a new instance
        /// </summary>
        /// <returns>ComDisposable for PowerPoint.Application</returns>
        private IDisposableCom<Application> GetOrCreatePowerPointApplication()
        {
            var powerPointApplication = GetPowerPointApplication();
            if (powerPointApplication == null)
            {
                powerPointApplication = DisposableCom.Create(new Application());
            }
            InitializeVariables(powerPointApplication);
            return powerPointApplication;
        }

        /// <summary>
        ///     Call this to get the running PowerPoint application, returns null if there isn't any.
        /// </summary>
        /// <returns>ComDisposable for PowerPoint.Application or null</returns>
        private IDisposableCom<Application> GetPowerPointApplication()
        {
            IDisposableCom<Application> powerPointApplication;
            try
            {
                powerPointApplication = OleAut32Api.GetActiveObject<Application>("PowerPoint.Application");
            }
            catch (Exception)
            {
                // Ignore, probably no PowerPoint running
                return null;
            }
            if (powerPointApplication.ComObject != null)
            {
                InitializeVariables(powerPointApplication);
            }
            return powerPointApplication;
        }

        /// <summary>
        ///     Get the captions of all the open powerpoint presentations
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetPowerpointPresentations()
        {
            using (var powerpointApplication = GetPowerPointApplication())
            {
                if (powerpointApplication == null)
                {
                    yield break;
                }

                using (var presentations = DisposableCom.Create(powerpointApplication.ComObject.Presentations))
                {
                    Log.Debug().WriteLine("Open Presentations: {0}", presentations.ComObject.Count);
                    for (int i = 1; i <= presentations.ComObject.Count; i++)
                    {
                        using (var presentation = DisposableCom.Create(presentations.ComObject[i]))
                        {
                            if (presentation == null)
                            {
                                continue;
                            }
                            if (presentation.ComObject.ReadOnly == MsoTriState.msoTrue)
                            {
                                continue;
                            }
                            if (IsAfter2003())
                            {
                                if (presentation.ComObject.Final)
                                {
                                    continue;
                                }
                            }
                            yield return presentation.ComObject.Name;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Initialize static powerpoint variables like version
        /// </summary>
        /// <param name="powerpointApplication">IPowerpointApplication</param>
        private void InitializeVariables(IDisposableCom<Application> powerpointApplication)
        {
            if ((powerpointApplication == null) || (powerpointApplication.ComObject == null) || (_powerpointVersion != null))
            {
                return;
            }
            if (!Version.TryParse(powerpointApplication.ComObject.Version, out _powerpointVersion))
            {
                Log.Warn().WriteLine("Assuming Powerpoint version 1997.");
                _powerpointVersion = new Version((int)OfficeVersions.Office97, 0, 0, 0);
            }
        }

        /// <summary>
        ///     Insert a capture into a new presentation
        /// </summary>
        /// <param name="tmpFile"></param>
        /// <param name="imageSize"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public bool InsertIntoNewPresentation(string tmpFile, Size imageSize, string title)
        {
            bool isPictureAdded = false;
            using (var powerpointApplication = GetOrCreatePowerPointApplication())
            {
                if (powerpointApplication != null)
                {
                    powerpointApplication.ComObject.Activate();
                    powerpointApplication.ComObject.Visible = MsoTriState.msoTrue;
                    using (var presentations = DisposableCom.Create(powerpointApplication.ComObject.Presentations))
                    {
                        using (var presentation = DisposableCom.Create(presentations.ComObject.Add()))
                        {
                            try
                            {
                                AddPictureToPresentation(presentation, tmpFile, imageSize, title);
                                isPictureAdded = true;
                            }
                            catch (Exception e)
                            {
                                Log.Error().WriteLine(e, "Powerpoint add picture to presentation failed");
                            }
                        }
                    }
                }
            }
            return isPictureAdded;
        }

        private bool IsAfter2003()
        {
            return _powerpointVersion.Major > (int)OfficeVersions.Office2003;
        }
    }

}