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
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using GreenshotPlugin.Controls;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Provides a predefined set of algorithms that are supported officially by the protocol
	/// </summary>
	public enum OAuthSignatureTypes  {
		HMACSHA1,
		PLAINTEXT,
		RSASHA1
	}
		
	public enum HTTPMethod { GET, POST, PUT, DELETE };

	public class OAuthSession {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OAuthSession));
		protected const string OAUTH_VERSION = "1.0";
		protected const string OAUTH_PARAMETER_PREFIX = "oauth_";

		//
		// List of know and used oauth parameters' names
		//		
		protected const string OAUTH_CONSUMER_KEY_KEY = "oauth_consumer_key";
		protected const string OAUTH_CALLBACK_KEY = "oauth_callback";
		protected const string OAUTH_VERSION_KEY = "oauth_version";
		protected const string OAUTH_SIGNATURE_METHOD_KEY = "oauth_signature_method";
		protected const string OAUTH_TIMESTAMP_KEY = "oauth_timestamp";
		protected const string OAUTH_NONCE_KEY = "oauth_nonce";
		protected const string OAUTH_TOKEN_KEY = "oauth_token";
		protected const string OAUTH_VERIFIER_KEY = "oauth_verifier";
		protected const string OAUTH_TOKEN_SECRET_KEY = "oauth_token_secret";
		protected const string OAUTH_SIGNATURE_KEY = "oauth_signature";

		protected const string HMACSHA1SignatureType = "HMAC-SHA1";
		protected const string PlainTextSignatureType = "PLAINTEXT";
		protected const string RSASHA1SignatureType = "RSA-SHA1";


		protected static Random random = new Random();

		protected const string UNRESERVED_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

		private string _userAgent = "Greenshot";
		private string _callbackUrl = "http://getgreenshot.org";
		private bool checkVerifier = true;
		private bool useHTTPHeadersForAuthorization = true;
		private bool useAuthorization = true;

		// default browser size
		private int _browserWidth = 864;
		private int _browserHeight = 587;
		private string loginTitle = "Authorize Greenshot access";

		#region PublicPropertiies
		public string ConsumerKey { get; set; }
		public string ConsumerSecret { get; set; }
		public string UserAgent { get { return _userAgent; } set { _userAgent = value; } }
		public string RequestTokenUrl { get; set; }
		public string AuthorizeUrl { get; set; }
		public string AccessTokenUrl { get; set; }
		public string CallbackUrl { get { return _callbackUrl;} set { _callbackUrl = value; } }
		public bool CheckVerifier {
			get {
				return checkVerifier;
			}
			set {
				checkVerifier = value;
			}
		}

		public int BrowserWidth {
			get {
				return _browserWidth;
			}
			set {
				_browserWidth = value;
			}
		}
		public int BrowserHeight {
			get {
				return _browserHeight;
			}
			set {
				_browserHeight = value;
			}
		}
		public string Token {
			get;
			set;
		}
		public string TokenSecret { get; set; }
		public string LoginTitle {
			get {
				return loginTitle;
			}
			set {
				loginTitle = value;
			}
		}
		public string Verifier {
			get;
			set;
		}
		public bool UseHTTPHeadersForAuthorization {
			get {
				return useHTTPHeadersForAuthorization;
			}
			set {
				useHTTPHeadersForAuthorization = value;
			}
		}
		public bool UseAuthorization {
			get {
				return useAuthorization;
			}
			set {
				useAuthorization = value;
			}
		}
		
		#endregion

		/// <summary>
		/// Helper function to compute a hash value
		/// </summary>
		/// <param name="hashAlgorithm">The hashing algorithm used. If that algorithm needs some initialization, like HMAC and its derivatives, they should be initialized prior to passing it to this function</param>
		/// <param name="data">The data to hash</param>
		/// <returns>a Base64 string of the hash value</returns>
		private static string ComputeHash(HashAlgorithm hashAlgorithm, string data) {
			if (hashAlgorithm == null) {
				throw new ArgumentNullException("hashAlgorithm");
			}

			if (string.IsNullOrEmpty(data)) {
				throw new ArgumentNullException("data");
			}

			byte[] dataBuffer = System.Text.Encoding.UTF8.GetBytes(data);
			byte[] hashBytes = hashAlgorithm.ComputeHash(dataBuffer);

			return Convert.ToBase64String(hashBytes);
		}

		/// <summary>
		/// Generate the normalized paramter string
		/// </summary>
		/// <param name="queryParameters">the list of query parameters</param>
		/// <returns>a string with the normalized query parameters</returns>
		private static string GenerateNormalizedParametersString(IDictionary<string, string> queryParameters) {
			if (queryParameters == null || queryParameters.Count == 0) {
				return string.Empty;
			}

			queryParameters = new SortedDictionary<string, string>(queryParameters);

			StringBuilder sb = new StringBuilder();
			foreach (string key in queryParameters.Keys) {
				sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}={1}&", key, UrlEncode3986(queryParameters[key]));
			}
			sb.Remove(sb.Length - 1, 1);

			return sb.ToString();
		}

		/// <summary>
		/// This is a different Url Encode implementation since the default .NET one outputs the percent encoding in lower case.
		/// While this is not a problem with the percent encoding spec, it is used in upper case throughout OAuth
		/// </summary>
		/// <param name="value">The value to Url encode</param>
		/// <returns>Returns a Url encoded string</returns>
		/// <remarks>This will cause an ignorable CA1055 warning in code analysis.</remarks>
		private static string UrlEncode3986(string value) {
			StringBuilder result = new StringBuilder();

			foreach (char symbol in value) {
				if (UNRESERVED_CHARS.IndexOf(symbol) != -1) {
					result.Append(symbol);
				} else {
					result.Append('%' + String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:X2}", (int)symbol));
				}
			}

			return result.ToString();
		}

		/// <summary>
		/// Generate the timestamp for the signature		
		/// </summary>
		/// <returns></returns>
		public static string GenerateTimeStamp() {
			// Default implementation of UNIX time of the current UTC time
			TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return Convert.ToInt64(ts.TotalSeconds).ToString();
		}

		/// <summary>
		/// Generate a nonce
		/// </summary>
		/// <returns></returns>
		public static string GenerateNonce() {
			// Just a simple implementation of a random number between 123400 and 9999999
			return random.Next(123400, 9999999).ToString();
		}
		/// <summary>
		/// Get the request token using the consumer key and secret.  Also initializes tokensecret
		/// </summary>
		/// <returns>The request token.</returns>
		private String getRequestToken() {
			string ret = null;
			string response = oAuthWebRequestNoCheck(HTTPMethod.POST, RequestTokenUrl, null);
			if (response.Length > 0) {
				IDictionary<string, string> qs = NetworkHelper.ParseQueryString(response);
				if (qs.ContainsKey(OAUTH_TOKEN_KEY)) {
					this.Token = qs[OAUTH_TOKEN_KEY];
					this.TokenSecret = qs[OAUTH_TOKEN_SECRET_KEY];
					ret = this.Token;
				}
			}
			return ret;		
		}

		/// <summary>
		/// Authorize the token by showing the dialog
		/// </summary>
		/// <returns>The request token.</returns>
		private String authorizeToken() {
			if (string.IsNullOrEmpty(Token)) {
				Exception e = new Exception("The request token is not set");
				throw e;
			}
			LOG.DebugFormat("Opening AuthorizationLink: {0}", AuthorizationLink);
			OAuthLoginForm oAuthLoginForm = new OAuthLoginForm(this, LoginTitle, BrowserWidth, BrowserHeight);
			oAuthLoginForm.ShowDialog();
			Token = oAuthLoginForm.Token;
			if (CheckVerifier) {
				Verifier = oAuthLoginForm.Verifier;
				if (!string.IsNullOrEmpty(Verifier)) {
					return Token;
				} else {
					return null;
				}
			} else {
				return Token;
			}
		}

		/// <summary>
		/// Get the access token
		/// </summary>
		/// <returns>The access token.</returns>		
		private String getAccessToken() {
			if (string.IsNullOrEmpty(Token) || (CheckVerifier && string.IsNullOrEmpty(Verifier))) {
				Exception e = new Exception("The request token and verifier were not set");
				throw e;
			}

			string response = oAuthWebRequestNoCheck(HTTPMethod.POST, AccessTokenUrl, null);

			if (response.Length > 0) {
				IDictionary<string, string> qs = NetworkHelper.ParseQueryString(response);
				if (qs.ContainsKey(OAUTH_TOKEN_KEY) && qs[OAUTH_TOKEN_KEY] != null) {
					this.Token = qs[OAUTH_TOKEN_KEY];
				}
				if (qs.ContainsKey(OAUTH_TOKEN_SECRET_KEY) && qs[OAUTH_TOKEN_SECRET_KEY] != null) {
					this.TokenSecret = qs[OAUTH_TOKEN_SECRET_KEY];
				}
			}

			return Token;		
		}

		/// <summary>
		/// This method goes through the whole authorize process, including a Authorization window.
		/// </summary>
		/// <returns>true if the process is completed</returns>
		private bool authorize() {
			LOG.Debug("Creating Token");
			try {
				getRequestToken();
			} catch (Exception ex) {
				LOG.Error(ex);
				throw new NotSupportedException("Service is not available: " + ex.Message);
			}
			if (UseAuthorization && string.IsNullOrEmpty(authorizeToken())) {
				LOG.Debug("User didn't authenticate!");
				return false;
			}
			return getAccessToken() != null;
		}

		/// <summary>
		/// Get the link to the authorization page for this application.
		/// </summary>
		/// <returns>The url with a valid request token, or a null string.</returns>
		public string AuthorizationLink {
			get {
				return AuthorizeUrl + "?" + OAUTH_TOKEN_KEY + "=" + this.Token + "&" + OAUTH_CALLBACK_KEY + "=" + UrlEncode3986(CallbackUrl);
			}
		}

		/// <summary>
		/// Wrapper
		/// </summary>
		/// <param name="method"></param>
		/// <param name="requestURL"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public string oAuthWebRequest(HTTPMethod method, string requestURL, IDictionary<string, string> parameters) {
			return oAuthWebRequest(method, requestURL, parameters, null, null);
		}
		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="requestURL">The full url, including the querystring.</param>
		/// <param name="parameters">Parameters for the request</param>
		/// <param name="contentType">contenttype for the postdata</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string oAuthWebRequest(HTTPMethod method, string requestURL, IDictionary<string, string> parameters, string contentType, MemoryStream postData) {
			// If we are not trying to get a Authorization or Accestoken, and we don't have a token, create one
			if (string.IsNullOrEmpty(Token)) {
				if (!authorize()) {
					throw new Exception("Not authorized");
				}
			}
			return oAuthWebRequestNoCheck(method, requestURL, parameters, contentType, postData);
		}

		public string oAuthWebRequestNoCheck(HTTPMethod method, string requestURL, IDictionary<string, string> parameters) {
			return oAuthWebRequestNoCheck(method, requestURL, parameters, null, null);
		}

		private string oAuthWebRequestNoCheck(HTTPMethod method, string requestURL, IDictionary<string, string> parameters, string contentType, MemoryStream postData) {
			// Build the signature base
			StringBuilder signatureBase = new StringBuilder();

			// Add Method to signature base
			signatureBase.Append(method.ToString()).Append("&");

			// Add normalized URL
			Uri url = new Uri(requestURL);
			string normalizedUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}://{1}", url.Scheme, url.Host);
			if (!((url.Scheme == "http" && url.Port == 80) || (url.Scheme == "https" && url.Port == 443))) {
				normalizedUrl += ":" + url.Port;
			}
			normalizedUrl += url.AbsolutePath;
			signatureBase.Append(UrlEncode3986(normalizedUrl)).Append("&");

			// Add normalized parameters
			if (parameters == null) {
				parameters = new Dictionary<string, string>();
			}

			parameters.Add(OAUTH_VERSION_KEY, OAUTH_VERSION);
			parameters.Add(OAUTH_NONCE_KEY, GenerateNonce());
			parameters.Add(OAUTH_TIMESTAMP_KEY, GenerateTimeStamp());
			parameters.Add(OAUTH_SIGNATURE_METHOD_KEY, HMACSHA1SignatureType);
			parameters.Add(OAUTH_CONSUMER_KEY_KEY, ConsumerKey);
			if (CallbackUrl != null && RequestTokenUrl != null && requestURL.ToString().StartsWith(RequestTokenUrl)) {
				parameters.Add(OAUTH_CALLBACK_KEY, CallbackUrl);
			}

			if (!string.IsNullOrEmpty(Token)) {
				parameters.Add(OAUTH_TOKEN_KEY, Token);
			}
			signatureBase.Append(UrlEncode3986(GenerateNormalizedParametersString(parameters)));
			LOG.DebugFormat("Signature base: {0}", signatureBase);
			// Generate Signature and add it to the parameters
			HMACSHA1 hmacsha1 = new HMACSHA1();
			hmacsha1.Key = Encoding.UTF8.GetBytes(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}&{1}", UrlEncode3986(ConsumerSecret), string.IsNullOrEmpty(TokenSecret) ? string.Empty : UrlEncode3986(TokenSecret)));
			string signature = ComputeHash(hmacsha1, signatureBase.ToString());
			parameters.Add(OAUTH_SIGNATURE_KEY, signature);
			LOG.DebugFormat("Signature: {0}", signature);

			IDictionary<string, string> requestParameters = null;
			// Add oAuth values as HTTP headers, if this is allowed
			StringBuilder authHeader = null;
			if (HTTPMethod.POST == method && UseHTTPHeadersForAuthorization) {
				authHeader = new StringBuilder();
				requestParameters = new Dictionary<string, string>();
				foreach (string parameterKey in parameters.Keys) {
					if (parameterKey.StartsWith(OAUTH_PARAMETER_PREFIX)) {
						authHeader.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}=\"{1}\", ", parameterKey, UrlEncode3986(parameters[parameterKey]));
					} else if (!requestParameters.ContainsKey(parameterKey)) {
						LOG.DebugFormat("Request parameter: {0}={1}", parameterKey, parameters[parameterKey]);
						requestParameters.Add(parameterKey, parameters[parameterKey]);
					}
				}
				// Remove trailing comma and space and add it to the headers
				if (authHeader.Length > 0) {
					authHeader.Remove(authHeader.Length - 2, 2);
				}
			} else if (HTTPMethod.GET == method) {
				if (parameters.Count > 0) {
					// Add the parameters to the request
					requestURL += "?" + NetworkHelper.GenerateQueryParameters(parameters);
				}
			} else {
				requestParameters = parameters;
			}

			// Create webrequest
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(requestURL);
			webRequest.Method = method.ToString();
			webRequest.ServicePoint.Expect100Continue = false;
			webRequest.UserAgent = _userAgent;
			webRequest.Timeout = 20000;

			if (UseHTTPHeadersForAuthorization && authHeader != null) {
				LOG.DebugFormat("Authorization: OAuth {0}", authHeader.ToString());
				webRequest.Headers.Add("Authorization: OAuth " + authHeader.ToString());
			}

			if (HTTPMethod.POST == method && postData == null && requestParameters != null && requestParameters.Count > 0) {
				StringBuilder form = new StringBuilder();
				foreach (string parameterKey in requestParameters.Keys) {
					form.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}={1}&", UrlEncode3986(parameterKey), UrlEncode3986(parameters[parameterKey]));
				}
				// Remove trailing &
				if (form.Length > 0) {
					form.Remove(form.Length - 1, 1);
				}
				LOG.DebugFormat("Form data: {0}", form.ToString());
				webRequest.ContentType = "application/x-www-form-urlencoded";
				byte[] data = Encoding.UTF8.GetBytes(form.ToString());
				using (var requestStream = webRequest.GetRequestStream()) {
					requestStream.Write(data, 0, data.Length);
				}
			} else {
				webRequest.ContentType = contentType;
				if (postData != null) {
					using (var requestStream = webRequest.GetRequestStream()) {
						requestStream.Write(postData.GetBuffer(), 0, (int)postData.Length);
					}
				}
			}

			string responseData = WebResponseGet(webRequest);
			LOG.DebugFormat("Response: {0}", responseData);

			webRequest = null;

			return responseData;
		}

		/// <summary>
		/// Web Request Wrapper
		/// </summary>
		/// <param name="method">Http Method</param>
		/// <param name="url">Full url to the web resource</param>
		/// <param name="postData">Data to post </param>
		/// <returns>The web server response.</returns>
		protected string WebRequest(HTTPMethod method, string url, string contentType, MemoryStream postData) {
			HttpWebRequest webRequest = null;
			string responseData = "";

			webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
			webRequest.Method = method.ToString();
			webRequest.ServicePoint.Expect100Continue = false;
			webRequest.UserAgent = _userAgent;
			webRequest.Timeout = 20000;
			webRequest.ContentLength = postData.Length;
			if (method == HTTPMethod.POST) {
				webRequest.ContentType = contentType;
			}

			using (var requestStream = webRequest.GetRequestStream()) {
				requestStream.Write(postData.GetBuffer(), 0, (int)postData.Length);
			}

			responseData = WebResponseGet(webRequest);

			webRequest = null;

			return responseData;
		}

		/// <summary>
		/// Process the web response.
		/// </summary>
		/// <param name="webRequest">The request object.</param>
		/// <returns>The response data.</returns>
		protected string WebResponseGet(HttpWebRequest webRequest) {
			StreamReader responseReader = null;
			string responseData = "";

			try {
				responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
				responseData = responseReader.ReadToEnd();
			} catch (Exception e) {
				throw e;
			} finally {
				webRequest.GetResponse().GetResponseStream().Close();
				responseReader.Close();
				responseReader = null;
			}

			return responseData;
		}
	}

}
