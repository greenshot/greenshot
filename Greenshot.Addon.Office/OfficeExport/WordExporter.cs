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

using System;
using System.Collections.Generic;
using Dapplo.Ini;
using Dapplo.Log;
using Dapplo.Windows.Com;
using Dapplo.Windows.Desktop;
using Greenshot.Addon.Office.OfficeInterop;

#endregion

namespace Greenshot.Addon.Office.OfficeExport
{
	/// <summary>
	/// This is responsible for exporting to word
	/// </summary>
	public static class WordExporter
	{
		private static readonly LogSource Log = new LogSource();
		private static Version _wordVersion;
		private static readonly IOfficeConfiguration OfficeConfig = IniConfig.Current.Get<IOfficeConfiguration>();

		/// <summary>
		///     Check if the used version is higher than Office 2003
		/// </summary>
		/// <returns></returns>
		private static bool IsAfter2003()
		{
			return _wordVersion.Major > (int) OfficeVersions.Office2003;
		}

		/// <summary>
		///     Insert the bitmap stored under the tempfile path into the word document with the supplied caption
		/// </summary>
		/// <param name="wordCaption">string</param>
		/// <param name="tmpFile">string</param>
		/// <returns>true if it worked</returns>
		public static bool InsertIntoExistingDocument(string wordCaption, string tmpFile)
		{
			using (var wordApplication = GetWordApplication())
			{
				if (wordApplication == null)
				{
					return false;
				}
				using (var documents = wordApplication.Documents)
				{
					for (var i = 1; i <= documents.Count; i++)
					{
						using (var wordDocument = documents.item(i))
						{
							using (var activeWindow = wordDocument.ActiveWindow)
							{
								if (activeWindow.Caption.StartsWith(wordCaption))
								{
									return InsertIntoExistingDocument(wordApplication, wordDocument, tmpFile, null, null);
								}
							}
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		///     Internal method for the insert
		/// </summary>
		/// <param name="wordApplication"></param>
		/// <param name="wordDocument"></param>
		/// <param name="tmpFile"></param>
		/// <param name="address">link for the image</param>
		/// <param name="tooltip">tooltip of the image</param>
		/// <returns></returns>
		internal static bool InsertIntoExistingDocument(IWordApplication wordApplication, IWordDocument wordDocument, string tmpFile, string address, string tooltip)
		{
			// Bug #1517: image will be inserted into that document, where the focus was last. It will not inserted into the chosen one.
			// Solution: Make sure the selected document is active, otherwise the insert will be made in a different document!
			try
			{
				wordDocument.Activate();
			}
			catch
			{
				// ignored
			}
			using (var selection = wordApplication.Selection)
			{
				if (selection == null)
				{
					Log.Info().WriteLine("No selection to insert {0} into found.", tmpFile);
					return false;
				}
				// Add Picture
				using (var shape = AddPictureToSelection(selection, tmpFile))
				{
					if (!string.IsNullOrEmpty(address))
					{
						var screentip = Type.Missing;
						if (!string.IsNullOrEmpty(tooltip))
						{
							screentip = tooltip;
						}
						try
						{
							using (var hyperlinks = wordDocument.Hyperlinks)
							{
								hyperlinks.Add(shape, screentip, Type.Missing, screentip, Type.Missing, Type.Missing);
							}
						}
						catch (Exception e)
						{
							Log.Warn().WriteLine("Couldn't add hyperlink for image: {0}", e.Message);
						}
					}
				}
				try
				{
					using (var activeWindow = wordDocument.ActiveWindow)
					{
						activeWindow.Activate();
						using (var activePane = activeWindow.ActivePane)
						{
							using (var view = activePane.View)
							{
								view.Zoom.Percentage = 100;
							}
						}
					}
				}
				catch (Exception e)
				{
					Log.Warn().WriteLine("Couldn't set zoom to 100, error: {0}", e.InnerException?.Message ?? e.Message);
				}
				try
				{
					wordApplication.Activate();
				}
				catch
				{
					// ignored
				}
				try
				{
					using (var activeWindow = wordDocument.ActiveWindow)
					{
						activeWindow.Activate();
						var hWnd = activeWindow.Hwnd;
						if (hWnd > 0)
						{
							// TODO: Await?
							InteropWindowFactory.CreateFor(new IntPtr(hWnd)).ToForegroundAsync();
						}
					}
				}
				catch
				{
					// ignored
				}
				return true;
			}
		}

		/// <summary>
		///     Helper method to add the file as image to the selection
		/// </summary>
		/// <param name="selection"></param>
		/// <param name="tmpFile"></param>
		/// <returns></returns>
		private static IInlineShape AddPictureToSelection(ISelection selection, string tmpFile)
		{
			using (var shapes = selection.InlineShapes)
			{
				var shape = shapes.AddPicture(tmpFile, false, true, Type.Missing);
				// Lock aspect ratio
				if (OfficeConfig.WordLockAspectRatio)
				{
					shape.LockAspectRatio = MsoTriState.msoTrue;
				}
				selection.InsertAfter("\r\n");
				selection.MoveDown(WdUnits.wdLine, 1, Type.Missing);
				return shape;
			}
		}

		public static void InsertIntoNewDocument(string tmpFile, string address, string tooltip)
		{
			using (var wordApplication = GetOrCreateWordApplication())
			{
				if (wordApplication == null)
				{
					return;
				}
				wordApplication.Visible = true;
				wordApplication.Activate();
				// Create new Document
				object template = string.Empty;
				object newTemplate = false;
				object documentType = 0;
				object documentVisible = true;
				using (var documents = wordApplication.Documents)
				{
					using (var wordDocument = documents.Add(ref template, ref newTemplate, ref documentType, ref documentVisible))
					{
						using (var selection = wordApplication.Selection)
						{
							// Add Picture
							using (var shape = AddPictureToSelection(selection, tmpFile))
							{
								if (!string.IsNullOrEmpty(address))
								{
									var screentip = Type.Missing;
									if (!string.IsNullOrEmpty(tooltip))
									{
										screentip = tooltip;
									}
									try
									{
										using (var hyperlinks = wordDocument.Hyperlinks)
										{
											hyperlinks.Add(shape, screentip, Type.Missing, screentip, Type.Missing, Type.Missing);
										}
									}
									catch (Exception e)
									{
										Log.Warn().WriteLine("Couldn't add hyperlink for image: {0}", e.Message);
									}
								}
							}
						}
						try
						{
							wordDocument.Activate();
						}
						catch
						{
							// ignored
						}
						try
						{
							using (var activeWindow = wordDocument.ActiveWindow)
							{
								activeWindow.Activate();
								var hWnd = activeWindow.Hwnd;
								if (hWnd > 0)
								{
									// TODO: Await?
									InteropWindowFactory.CreateFor(new IntPtr(hWnd)).ToForegroundAsync();
								}
							}
						}
						catch
						{
							// ignored
						}
					}
				}
			}
		}

		/// <summary>
		///     Get the captions of all the open word documents
		/// </summary>
		/// <returns></returns>
		public static List<string> GetWordDocuments()
		{
			var openDocuments = new List<string>();
			try
			{
				using (var wordApplication = GetWordApplication())
				{
					if (wordApplication == null)
					{
						return openDocuments;
					}
					using (var documents = wordApplication.Documents)
					{
						for (var i = 1; i <= documents.Count; i++)
						{
							using (var document = documents.item(i))
							{
								if (document.ReadOnly)
								{
									continue;
								}

							    if (IsAfter2003() && document.Final)
							    {
							        continue;
							    }

							    using (var activeWindow = document.ActiveWindow)
								{
									openDocuments.Add(activeWindow.Caption);
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Warn().WriteLine(ex, "Problem retrieving word destinations, ignoring: ");
			}
			openDocuments.Sort();
			return openDocuments;
		}

		/// <summary>
		///     Call this to get the running outlook application, returns null if there isn't any.
		/// </summary>
		/// <returns>IWordApplication or null</returns>
		private static IWordApplication GetWordApplication()
		{
			var wordApplication = ComWrapper.GetInstance<IWordApplication>();
			InitializeVariables(wordApplication);
			return wordApplication;
		}

		/// <summary>
		///     Call this to get the running word application, or create a new instance
		/// </summary>
		/// <returns>IWordApplication</returns>
		private static IWordApplication GetOrCreateWordApplication()
		{
			var wordApplication = ComWrapper.GetOrCreateInstance<IWordApplication>();
			InitializeVariables(wordApplication);
			return wordApplication;
		}

		/// <summary>
		///     Initialize static outlook variables like version and currentuser
		/// </summary>
		/// <param name="wordApplication"></param>
		private static void InitializeVariables(IWordApplication wordApplication)
		{
			if (wordApplication == null || _wordVersion != null)
			{
				return;
			}
			try
			{
				_wordVersion = new Version(wordApplication.Version);
				Log.Info().WriteLine("Using Word {0}", _wordVersion);
			}
			catch (Exception exVersion)
			{
				Log.Error().WriteLine(exVersion);
				Log.Warn().WriteLine("Assuming Word version 1997.");
				_wordVersion = new Version((int) OfficeVersions.Office97, 0, 0, 0);
			}
		}
	}
}