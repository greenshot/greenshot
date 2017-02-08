/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using Greenshot.IniFile;
using Greenshot.Interop;
using Greenshot.Interop.Office;

namespace GreenshotOfficePlugin.OfficeExport {
	public class PowerpointExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PowerpointExporter));
		private static Version _powerpointVersion;
		private static readonly OfficeConfiguration officeConfiguration = IniConfig.GetIniSection<OfficeConfiguration>();

		private static bool IsAfter2003() {
			return _powerpointVersion.Major > (int)OfficeVersion.OFFICE_2003;
		}

		/// <summary>
		/// Get the captions of all the open powerpoint presentations
		/// </summary>
		/// <returns></returns>
		public static List<string> GetPowerpointPresentations() {
			List<string> foundPresentations = new List<string>();
			try {
				using (IPowerpointApplication powerpointApplication = GetPowerpointApplication()) {
					if (powerpointApplication == null) {
						return foundPresentations;
					}

					using (IPresentations presentations = powerpointApplication.Presentations) {
						LOG.DebugFormat("Open Presentations: {0}", presentations.Count);
						for (int i = 1; i <= presentations.Count; i++) {
							using (IPresentation presentation = presentations.item(i)) {
								if (presentation == null) {
									continue;
								}
								if (presentation.ReadOnly == MsoTriState.msoTrue) {
									continue;
								}
								if (IsAfter2003()) {
									if (presentation.Final) {
										continue;
									}
								}
								foundPresentations.Add(presentation.Name);
							}
						}
					}
				}
			} catch (Exception ex) {
				LOG.Warn("Problem retrieving word destinations, ignoring: ", ex);
			}
			foundPresentations.Sort();
			return foundPresentations;
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
			using (IPowerpointApplication powerpointApplication = GetPowerpointApplication()) {
				if (powerpointApplication == null) {
					return false;
				}
				using (IPresentations presentations = powerpointApplication.Presentations) {
					LOG.DebugFormat("Open Presentations: {0}", presentations.Count);
					for (int i = 1; i <= presentations.Count; i++) {
						using (IPresentation presentation = presentations.item(i)) {
							if (presentation == null) {
								continue;
							}
							if (!presentation.Name.StartsWith(presentationName)) {
								continue;
							}
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

		/// <summary>
		/// Internal method to add a picture to a presentation
		/// </summary>
		/// <param name="presentation"></param>
		/// <param name="tmpFile"></param>
		/// <param name="imageSize"></param>
		/// <param name="title"></param>
		private static void AddPictureToPresentation(IPresentation presentation, string tmpFile, Size imageSize, string title) {
			if (presentation != null) {
				//ISlide slide = presentation.Slides.AddSlide( presentation.Slides.Count + 1, PPSlideLayout.ppLayoutPictureWithCaption);
				ISlide slide;
				float left = (presentation.PageSetup.SlideWidth / 2) - (imageSize.Width / 2f);
				float top = (presentation.PageSetup.SlideHeight / 2) - (imageSize.Height / 2f);
				float width = imageSize.Width;
				float height = imageSize.Height;
				IShape shapeForCaption = null;
				bool hasScaledWidth = false;
				bool hasScaledHeight = false;
				try {
					slide = presentation.Slides.Add(presentation.Slides.Count + 1, (int)officeConfiguration.PowerpointSlideLayout);
					// Shapes[2] is the image shape on this layout.
					shapeForCaption = slide.Shapes.item(1);
					IShape shapeForLocation = slide.Shapes.item(2);

					if (width > shapeForLocation.Width) {
						width = shapeForLocation.Width;
						left = shapeForLocation.Left;
						hasScaledWidth = true;
					} else {
						shapeForLocation.Left = left;
					}
					shapeForLocation.Width = imageSize.Width;

					if (height > shapeForLocation.Height) {
						height = shapeForLocation.Height;
						top = shapeForLocation.Top;
						hasScaledHeight = true;
					} else {
						top = (shapeForLocation.Top + (shapeForLocation.Height / 2)) - (imageSize.Height / 2f);
					}
					shapeForLocation.Height = imageSize.Height;
				} catch (Exception e) {
					LOG.Error(e);
					slide = presentation.Slides.Add(presentation.Slides.Count + 1, (int)PPSlideLayout.ppLayoutBlank);
				}
				IShape shape = slide.Shapes.AddPicture(tmpFile, MsoTriState.msoFalse, MsoTriState.msoTrue, 0, 0, width, height);
				if (officeConfiguration.PowerpointLockAspectRatio) {
					shape.LockAspectRatio = MsoTriState.msoTrue;
				} else {
					shape.LockAspectRatio = MsoTriState.msoFalse;
				}
				shape.ScaleHeight(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromMiddle);
				shape.ScaleWidth(1, MsoTriState.msoTrue, MsoScaleFrom.msoScaleFromMiddle);
				if (hasScaledWidth) {
					shape.Width = width;
				}
				if (hasScaledHeight) {
					shape.Height = height;
				}
				shape.Left = left;
				shape.Top = top;
				shape.AlternativeText = title;
				if (shapeForCaption != null) {
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

		/// <summary>
		/// Insert a capture into a new presentation
		/// </summary>
		/// <param name="tmpFile"></param>
		/// <param name="imageSize"></param>
		/// <param name="title"></param>
		/// <returns></returns>
		public static bool InsertIntoNewPresentation(string tmpFile, Size imageSize, string title) {
			bool isPictureAdded = false;
			using (IPowerpointApplication powerpointApplication = GetOrCreatePowerpointApplication()) {
				if (powerpointApplication != null) {
					powerpointApplication.Visible = true;
					using (IPresentations presentations = powerpointApplication.Presentations) {
						using (IPresentation presentation = presentations.Add(MsoTriState.msoTrue)) {
							try {
								AddPictureToPresentation(presentation, tmpFile, imageSize, title);
								isPictureAdded = true;
							} catch (Exception e) {
								LOG.Error(e);
							}
						}
					}
				}
			}
			return isPictureAdded;
		}

		/// <summary>
		/// Call this to get the running powerpoint application, returns null if there isn't any.
		/// </summary>
		/// <returns>IPowerpointApplication or null</returns>
		private static IPowerpointApplication GetPowerpointApplication() {
			IPowerpointApplication powerpointApplication = COMWrapper.GetInstance<IPowerpointApplication>();
			InitializeVariables(powerpointApplication);
			return powerpointApplication;
		}

		/// <summary>
		/// Call this to get the running powerpoint application, or create a new instance
		/// </summary>
		/// <returns>IPowerpointApplication</returns>
		private static IPowerpointApplication GetOrCreatePowerpointApplication() {
			IPowerpointApplication powerpointApplication = COMWrapper.GetOrCreateInstance<IPowerpointApplication>();
			InitializeVariables(powerpointApplication);
			return powerpointApplication;
		}

		/// <summary>
		/// Initialize static outlook variables like version and currentuser
		/// </summary>
		/// <param name="powerpointApplication">IPowerpointApplication</param>
		private static void InitializeVariables(IPowerpointApplication powerpointApplication) {
			if (powerpointApplication == null || _powerpointVersion != null) {
				return;
			}
			try {
				_powerpointVersion = new Version(powerpointApplication.Version);
				LOG.InfoFormat("Using Powerpoint {0}", _powerpointVersion);
			} catch (Exception exVersion) {
				LOG.Error(exVersion);
				LOG.Warn("Assuming Powerpoint version 1997.");
				_powerpointVersion = new Version((int)OfficeVersion.OFFICE_97, 0, 0, 0);
			}
		}

	}
}
