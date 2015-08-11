/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

namespace GreenshotPlugin.OAuth {
	/// <summary>
	/// Specify the autorize mode that is used to get the token from the cloud service.
	/// </summary>
	public enum OAuth2AuthorizeMode {
		Unknown,		// Will give an exception, caller needs to specify another value
		LocalServer,	// Will specify a redirect URL to http://localhost:port/authorize, while having a HttpListener
		MonitorTitle,	// Not implemented yet: Will monitor for title changes
		Pin,			// Not implemented yet: Will ask the user to enter the shown PIN
		EmbeddedBrowser // Will open into an embedded _browser (OAuthLoginForm), and catch the redirect
	}
}
