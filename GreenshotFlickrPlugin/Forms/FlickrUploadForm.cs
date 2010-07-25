/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using FlickrNet;

namespace GreenshotFlickrPlugin {
	/// <summary>
	/// Description of FlickrUploadForm.
	/// </summary>
	public partial class FlickrUploadForm : Form {
		// Store the Frob in a private variable
		private string tempFrob;
		private string ApiKey = "f967e5148945cb3c4e149cc5be97796a";
		private string SharedSecret = "4180a21a1d2f8666";
		private Flickr flickr;
		
		public FlickrUploadForm() {
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			// Create Flickr instance
			flickr = new Flickr(ApiKey, SharedSecret);
		}

		protected void AuthMeButton_Click(object sender, EventArgs e) {
			// Get Frob    
			tempFrob = flickr.AuthGetFrob();
			// Calculate the URL at Flickr to redirect the user to
			string flickrUrl = flickr.AuthCalcUrl(tempFrob, AuthLevel.Write);
			// The following line will load the URL in the users default browser.
			System.Diagnostics.Process.Start(flickrUrl);
		}

		protected void CompleteAuthButton_Click(object sender, EventArgs e) {
			try {
				// use the temporary Frob to get the authentication
				Auth auth = flickr.AuthGetToken(tempFrob);
				// Store this Token for later usage, 
				// or set your Flickr instance to use it.
				Console.WriteLine("User authenticated successfully");
				Console.WriteLine("Authentication token is " + auth.Token); 
				flickr.AuthToken = auth.Token;
				Console.WriteLine("User id is " + auth.User);
			} catch(FlickrException ex) {
				// If user did not authenticat your application 
				// then a FlickrException will be thrown.
				Console.WriteLine("User did not authenticate you");
				Console.WriteLine(ex.ToString());
			}
		}

		public void upload(Stream buffer) {
			string file = "test.png";
			string title = "Test Photo";
			string description = "This is the description of the photo";
			string tags = "tag1,tag2,tag3";
			string photoId = flickr.UploadPicture(buffer, file, title, description, tags, false, false, false, ContentType.Screenshot, SafetyLevel.Restricted, HiddenFromSearch.Hidden);
		
			flickr.PhotosSetMeta(photoId, "New Title", "New Description");
			// Get list of users sets
			PhotosetCollection sets = flickr.PhotosetsGetList();
			// Get the first set in the collection
			Photoset set = sets[0];
			// Add the photo to that set
			flickr.PhotosetsAddPhoto(set.PhotosetId, photoId);
		}

		void OkButtonClick(object sender, EventArgs e) {
			DialogResult = DialogResult.OK;
		}
		
		void CancelButtonClick(object sender, EventArgs e) {
			DialogResult = DialogResult.Cancel;
		}
	}
}
