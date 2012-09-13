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
	public class OAuthHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OAuthHelper));
		/// <summary>
		/// Provides a predefined set of algorithms that are supported officially by the protocol
		/// </summary>
		protected enum SignatureTypes {
			HMACSHA1,
			PLAINTEXT,
			RSASHA1
		}

		/// <summary>
		/// Provides an internal structure to sort the query parameter
		/// </summary>
		protected class QueryParameter {
			private string name = null;
			private string value = null;

			public QueryParameter(string name, string value)
			{
				this.name = name;
				this.value = value;
			}

			public string Name
			{
				get { return name; }
			}

			public string Value
			{
				get { return value; }
			}
		}

		/// <summary>
		/// Comparer class used to perform the sorting of the query parameters
		/// </summary>
		protected class QueryParameterComparer : IComparer<QueryParameter> {

			#region IComparer<QueryParameter> Members

			public int Compare(QueryParameter x, QueryParameter y) {
				if (x.Name == y.Name) {
					return string.Compare(x.Value, y.Value);
				} else {
					return string.Compare(x.Name, y.Name);
				}
			}

			#endregion
		}

		protected const string OAuthVersion = "1.0";
		protected const string OAuthParameterPrefix = "oauth_";

		//
		// List of know and used oauth parameters' names
		//		
		protected const string OAuthConsumerKeyKey = "oauth_consumer_key";
		protected const string OAuthCallbackKey = "oauth_callback";
		protected const string OAuthVersionKey = "oauth_version";
		protected const string OAuthSignatureMethodKey = "oauth_signature_method";
		protected const string OAuthSignatureKey = "oauth_signature";
		protected const string OAuthTimestampKey = "oauth_timestamp";
		protected const string OAuthNonceKey = "oauth_nonce";
		protected const string OAuthTokenKey = "oauth_token";
		protected const string oAauthVerifier = "oauth_verifier";
		protected const string OAuthTokenSecretKey = "oauth_token_secret";

		protected const string HMACSHA1SignatureType = "HMAC-SHA1";
		protected const string PlainTextSignatureType = "PLAINTEXT";
		protected const string RSASHA1SignatureType = "RSA-SHA1";

		public enum Method { GET, POST, PUT, DELETE };
		private string _userAgent = "Greenshot";
		private string _callbackUrl = "http://getgreenshot.org";
		private bool checkVerifier = true;
		// default browser size
		private int _browserWidth = 864;
		private int _browserHeight = 587;
		
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
		#endregion
 
		protected Random random = new Random();

		private string oauth_verifier;
		public string Verifier { get { return oauth_verifier; } set { oauth_verifier = value; } }


		protected const string unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

		/// <summary>
		/// Helper function to compute a hash value
		/// </summary>
		/// <param name="hashAlgorithm">The hashing algorithm used. If that algorithm needs some initialization, like HMAC and its derivatives, they should be initialized prior to passing it to this function</param>
		/// <param name="data">The data to hash</param>
		/// <returns>a Base64 string of the hash value</returns>
		private string ComputeHash(HashAlgorithm hashAlgorithm, string data) {
			if (hashAlgorithm == null) {
				throw new ArgumentNullException("hashAlgorithm");
			}

			if (string.IsNullOrEmpty(data)) {
				throw new ArgumentNullException("data");
			}

			byte[] dataBuffer = System.Text.Encoding.ASCII.GetBytes(data);
			byte[] hashBytes = hashAlgorithm.ComputeHash(dataBuffer);

			return Convert.ToBase64String(hashBytes);
		}

		/// <summary>
		/// Internal function to cut out all non oauth query string parameters (all parameters not begining with "oauth_")
		/// </summary>
		/// <param name="parameters">The query string part of the Url</param>
		/// <returns>A list of QueryParameter each containing the parameter name and value</returns>
		private List<QueryParameter> GetQueryParameters(string parameters) {
			if (parameters.StartsWith("?")) {
				parameters = parameters.Remove(0, 1);
			}

			List<QueryParameter> result = new List<QueryParameter>();

			if (!string.IsNullOrEmpty(parameters)) {
				string[] p = parameters.Split('&');
				foreach (string s in p) {
					if (!string.IsNullOrEmpty(s) && !s.StartsWith(OAuthParameterPrefix)) {
						if (s.IndexOf('=') > -1) {
							string[] temp = s.Split('=');
							result.Add(new QueryParameter(temp[0], temp[1]));
						} else {
							result.Add(new QueryParameter(s, string.Empty));
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		/// A wrapper around the EscapeDataString, as the limit is 32766 characters
		/// See: http://msdn.microsoft.com/en-us/library/system.uri.escapedatastring%28v=vs.110%29.aspx
		/// </summary>
		/// <param name="dataString"></param>
		/// <returns>escaped data string</returns>
		private static StringBuilder EscapeDataStringToStringBuilder(string dataString) {
			StringBuilder result = new StringBuilder();
			int currentLocation = 0;
			while (currentLocation < dataString.Length) {
				string process = dataString.Substring(currentLocation, Math.Min(16384, dataString.Length - currentLocation));
				result.Append(Uri.EscapeDataString(process));
				currentLocation = currentLocation + 16384;
			}
			return result;
		}

		/// <summary>
		/// This is a different Url Encode implementation since the default .NET one outputs the percent encoding in lower case.
		/// While this is not a problem with the percent encoding spec, it is used in upper case throughout OAuth
		/// </summary>
		/// <param name="value">The value to Url encode</param>
		/// <returns>Returns a Url encoded string</returns>
		public static string UrlEncode3986(string text) {
			string[] UriRfc3986CharsToEscape = new[] { "!", "*", "'", "(", ")" };
			LOG.DebugFormat("Text size {0}", text.Length);
			StringBuilder escaped = EscapeDataStringToStringBuilder(text);

			for (int i = 0; i < UriRfc3986CharsToEscape.Length; i++) {
				escaped.Replace(UriRfc3986CharsToEscape[i], Uri.HexEscape(UriRfc3986CharsToEscape[i][0]));
			}
			return escaped.ToString();
		}
		/// <summary>
		/// Normalizes the request parameters according to the spec
		/// </summary>
		/// <param name="parameters">The list of parameters already sorted</param>
		/// <returns>a string representing the normalized parameters</returns>
		protected string NormalizeRequestParameters(IList<QueryParameter> parameters) {
			StringBuilder sb = new StringBuilder();
			QueryParameter p = null;
			for (int i = 0; i < parameters.Count; i++) {
				p = parameters[i];
				sb.AppendFormat("{0}={1}", p.Name, p.Value);

				if (i < parameters.Count - 1) {
					sb.Append("&");
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Generate the signature base that is used to produce the signature
		/// </summary>
		/// <param name="url">The full url that needs to be signed including its non OAuth url parameters</param>
		/// <param name="consumerKey">The consumer key</param>		
		/// <param name="token">The token, if available. If not available pass null or an empty string</param>
		/// <param name="tokenSecret">The token secret, if available. If not available pass null or an empty string</param>
		/// <param name="httpMethod">The http method used. Must be a valid HTTP method verb (POST,GET,PUT, etc)</param>
		/// <param name="signatureType">The signature type. To use the default values use <see cref="OAuthBase.SignatureTypes">OAuthBase.SignatureTypes</see>.</param>
		/// <returns>The signature base</returns>
		protected string GenerateSignatureBase(Uri url, string consumerKey, string token, string tokenSecret, string httpMethod, string timeStamp, string nonce, string callback, string signatureType, out string normalizedUrl, out string normalizedRequestParameters) {
			if (token == null) {
				token = string.Empty;
			}

			if (tokenSecret == null) {
				tokenSecret = string.Empty;
			}

			if (string.IsNullOrEmpty(consumerKey)) {
				throw new ArgumentNullException("consumerKey");
			}

			if (string.IsNullOrEmpty(httpMethod)) {
				throw new ArgumentNullException("httpMethod");
			}

			if (string.IsNullOrEmpty(signatureType)) {
				throw new ArgumentNullException("signatureType");
			}

			normalizedUrl = null;
			normalizedRequestParameters = null;

			List<QueryParameter> parameters = GetQueryParameters(url.Query);
			parameters.Add(new QueryParameter(OAuthVersionKey, OAuthVersion));
			parameters.Add(new QueryParameter(OAuthNonceKey, nonce));
			parameters.Add(new QueryParameter(OAuthTimestampKey, timeStamp));
			parameters.Add(new QueryParameter(OAuthSignatureMethodKey, signatureType));
			parameters.Add(new QueryParameter(OAuthConsumerKeyKey, consumerKey));

			//TODO: Make this less of a hack
			if (!string.IsNullOrEmpty(callback)) {
				parameters.Add(new QueryParameter(OAuthCallbackKey, UrlEncode3986(callback)));
			}

			if (!string.IsNullOrEmpty(token)) {
				parameters.Add(new QueryParameter(OAuthTokenKey, token));
			}

			if (!string.IsNullOrEmpty(oauth_verifier)) {
				parameters.Add(new QueryParameter(oAauthVerifier, oauth_verifier));
			}


			parameters.Sort(new QueryParameterComparer());


			normalizedUrl = string.Format("{0}://{1}", url.Scheme, url.Host);
			if (!((url.Scheme == "http" && url.Port == 80) || (url.Scheme == "https" && url.Port == 443))) {
				normalizedUrl += ":" + url.Port;
			}
			normalizedUrl += url.AbsolutePath;
			normalizedRequestParameters = NormalizeRequestParameters(parameters);

			StringBuilder signatureBase = new StringBuilder();
			signatureBase.AppendFormat("{0}&", httpMethod.ToUpper());
			signatureBase.AppendFormat("{0}&", UrlEncode3986(normalizedUrl));
			signatureBase.AppendFormat("{0}", UrlEncode3986(normalizedRequestParameters));

			return signatureBase.ToString();
		}

		/// <summary>
		/// Generate the signature value based on the given signature base and hash algorithm
		/// </summary>
		/// <param name="signatureBase">The signature based as produced by the GenerateSignatureBase method or by any other means</param>
		/// <param name="hash">The hash algorithm used to perform the hashing. If the hashing algorithm requires initialization or a key it should be set prior to calling this method</param>
		/// <returns>A base64 string of the hash value</returns>
		protected string GenerateSignatureUsingHash(string signatureBase, HashAlgorithm hash) {
			return ComputeHash(hash, signatureBase);
		}

		/// <summary>
		/// Generates a signature using the HMAC-SHA1 algorithm
		/// </summary>		
		/// <param name="url">The full url that needs to be signed including its non OAuth url parameters</param>
		/// <param name="consumerKey">The consumer key</param>
		/// <param name="consumerSecret">The consumer seceret</param>
		/// <param name="token">The token, if available. If not available pass null or an empty string</param>
		/// <param name="tokenSecret">The token secret, if available. If not available pass null or an empty string</param>
		/// <param name="httpMethod">The http method used. Must be a valid HTTP method verb (POST,GET,PUT, etc)</param>
		/// <returns>A base64 string of the hash value</returns>
		protected string GenerateSignature(Uri url, string consumerKey, string consumerSecret, string token, string tokenSecret, string httpMethod, string timeStamp, string nonce, string callback, out string normalizedUrl, out string normalizedRequestParameters) {
			return GenerateSignature(url, consumerKey, consumerSecret, token, tokenSecret, httpMethod, timeStamp, nonce, callback, SignatureTypes.HMACSHA1, out normalizedUrl, out normalizedRequestParameters);
		}

		/// <summary>
		/// Generates a signature using the specified signatureType 
		/// </summary>		
		/// <param name="url">The full url that needs to be signed including its non OAuth url parameters</param>
		/// <param name="consumerKey">The consumer key</param>
		/// <param name="consumerSecret">The consumer seceret</param>
		/// <param name="token">The token, if available. If not available pass null or an empty string</param>
		/// <param name="tokenSecret">The token secret, if available. If not available pass null or an empty string</param>
		/// <param name="httpMethod">The http method used. Must be a valid HTTP method verb (POST,GET,PUT, etc)</param>
		/// <param name="signatureType">The type of signature to use</param>
		/// <returns>A base64 string of the hash value</returns>
		protected string GenerateSignature(Uri url, string consumerKey, string consumerSecret, string token, string tokenSecret, string httpMethod, string timeStamp, string nonce, string callback, SignatureTypes signatureType, out string normalizedUrl, out string normalizedRequestParameters) {
			normalizedUrl = null;
			normalizedRequestParameters = null;

			switch (signatureType) {
				case SignatureTypes.PLAINTEXT:
					return NetworkHelper.UrlEncode(string.Format("{0}&{1}", consumerSecret, tokenSecret));
				case SignatureTypes.HMACSHA1:
					string signatureBase = GenerateSignatureBase(url, consumerKey, token, tokenSecret, httpMethod, timeStamp, nonce, callback, HMACSHA1SignatureType, out normalizedUrl, out normalizedRequestParameters);
					HMACSHA1 hmacsha1 = new HMACSHA1();
					hmacsha1.Key = Encoding.ASCII.GetBytes(string.Format("{0}&{1}", UrlEncode3986(consumerSecret), string.IsNullOrEmpty(tokenSecret) ? "" : UrlEncode3986(tokenSecret)));

					return GenerateSignatureUsingHash(signatureBase, hmacsha1);
				case SignatureTypes.RSASHA1:
					throw new NotImplementedException();
				default:
					throw new ArgumentException("Unknown signature type", "signatureType");
			}
		}

		/// <summary>
		/// Generate the timestamp for the signature		
		/// </summary>
		/// <returns></returns>

		protected virtual string GenerateTimeStamp() {
			// Default implementation of UNIX time of the current UTC time
			TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return Convert.ToInt64(ts.TotalSeconds).ToString();
		}

		/// <summary>
		/// Generate a nonce
		/// </summary>
		/// <returns></returns>
		protected virtual string GenerateNonce() {
			// Just a simple implementation of a random number between 123400 and 9999999
			return random.Next(123400, 9999999).ToString();
		}

		/// <summary>
		/// Get the request token using the consumer key and secret.  Also initializes tokensecret
		/// </summary>
		/// <returns>The request token.</returns>
		public String getRequestToken() {
			string ret = null;
			string response = oAuthWebRequest(Method.POST, RequestTokenUrl, String.Empty);
			if (response.Length > 0) {
				NameValueCollection qs = NetworkHelper.ParseQueryString(response);
				if (qs["oauth_token"] != null) {
					this.Token = qs["oauth_token"];
					this.TokenSecret = qs["oauth_token_secret"];
					ret = this.Token;
				}
			}
			return ret;		
		}

		/// <summary>
		/// Authorize the token by showing the dialog
		/// </summary>
		/// <returns>The request token.</returns>
		public String authorizeToken(string browserTitle) {
			if (string.IsNullOrEmpty(Token)) {
				Exception e = new Exception("The request token is not set");
				throw e;
			}
			LOG.DebugFormat("Opening browser for URL: {0}", AuthorizationLink);
			OAuthLoginForm oAuthLoginForm = new OAuthLoginForm(this, browserTitle, BrowserWidth, BrowserHeight);
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
		public String getAccessToken() {
			if (string.IsNullOrEmpty(Token) || (CheckVerifier && string.IsNullOrEmpty(Verifier))) {
				Exception e = new Exception("The request token and verifier were not set");
				throw e;
			}

			string response = oAuthWebRequest(Method.POST, AccessTokenUrl, string.Empty);

			if (response.Length > 0) {
				NameValueCollection qs = NetworkHelper.ParseQueryString(response);
				if (qs["oauth_token"] != null) {
					this.Token = qs["oauth_token"];
				}
				if (qs["oauth_token_secret"] != null) {
					this.TokenSecret = qs["oauth_token_secret"];
				}
			}

			return Token;		
		}

		/// <summary>
		/// Get the link to the authorization page for this application.
		/// </summary>
		/// <returns>The url with a valid request token, or a null string.</returns>
		public string AuthorizationLink {
			get {
				return AuthorizeUrl + "?oauth_token=" + this.Token;
			}
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="url">The full url, including the querystring.</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string oAuthWebRequest(Method method, string url, MemoryStream postData) {
			string outUrl = "";
			string querystring = "";

			Uri uri = new Uri(url);

			string nonce = this.GenerateNonce();
			string timeStamp = this.GenerateTimeStamp();

			string callback = "";
			if (url.ToString().Contains(RequestTokenUrl)) {
				callback = CallbackUrl;
			}

			//Generate Signature
			string sig = this.GenerateSignature(uri,
				this.ConsumerKey,
				this.ConsumerSecret,
				this.Token,
				this.TokenSecret,
				method.ToString(),
				timeStamp,
				nonce,
				callback,
				out outUrl,
				out querystring);

			if (querystring.Length > 0) {
				querystring += "&";
			}
			querystring += "oauth_signature=" + NetworkHelper.UrlEncode(sig);

			if (querystring.Length > 0) {
				outUrl += "?";
			}

			return WebRequest(method, outUrl + querystring, postData);
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="url">The full url, including the querystring.</param>
		/// <param name="postData">Data to post (querystring format)</param>
		/// <returns>The web server response.</returns>
		public string oAuthWebRequest(Method method, string url, string postData) {
			string outUrl = "";
			string querystring = "";
			string ret = "";

			//Setup postData for signing.
			//Add the postData to the querystring.
			if (method == Method.POST) {
				if (postData.Length > 0) {
					//Decode the parameters and re-encode using the oAuth UrlEncode method.
					NameValueCollection qs = NetworkHelper.ParseQueryString(postData);
					postData = "";
					foreach (string key in qs.AllKeys) {
						if (postData.Length > 0) {
							postData += "&";
						}
						qs[key] = NetworkHelper.UrlDecode(qs[key]);
						qs[key] = UrlEncode3986(qs[key]);
						postData += key + "=" + qs[key];

					}
					if (url.IndexOf("?") > 0) {
						url += "&";
					} else {
						url += "?";
					}
					url += postData;
				}
			}

			Uri uri = new Uri(url);

			string nonce = this.GenerateNonce();
			string timeStamp = this.GenerateTimeStamp();
			
			string callback = "";
			if (url.ToString().Contains(RequestTokenUrl)) {
				callback = CallbackUrl;
			}

			//Generate Signature
			string sig = this.GenerateSignature(uri,
				this.ConsumerKey,
				this.ConsumerSecret,
				this.Token,
				this.TokenSecret,
				method.ToString(),
				timeStamp,
				nonce,
				callback,
				out outUrl,
				out querystring);

			if (querystring.Length > 0) {
				querystring += "&";
			}
			querystring += "oauth_signature=" + NetworkHelper.UrlEncode(sig);

			//Convert the querystring to postData
			if (method == Method.POST) {
				postData = querystring;
				querystring = "";
			}

			if (querystring.Length > 0) {
				outUrl += "?";
			}

			if (method == Method.POST || method == Method.GET) {
				ret = WebRequest(method, outUrl + querystring, postData);
			}
				
			return ret;
		}

		/// <summary>
		/// Web Request Wrapper
		/// </summary>
		/// <param name="method">Http Method</param>
		/// <param name="url">Full url to the web resource</param>
		/// <param name="postData">Data to post </param>
		/// <returns>The web server response.</returns>
		protected string WebRequest(Method method, string url, MemoryStream postData) {
			HttpWebRequest webRequest = null;
			string responseData = "";

			webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
			webRequest.Method = method.ToString();
			webRequest.ServicePoint.Expect100Continue = false;
			webRequest.UserAgent = _userAgent;
			webRequest.Timeout = 20000;
			webRequest.ContentLength = postData.Length;
			using (var requestStream = webRequest.GetRequestStream()) {
				requestStream.Write(postData.GetBuffer(), 0, (int)postData.Length);
			}

			responseData = WebResponseGet(webRequest);

			webRequest = null;

			return responseData;
		}

		/// <summary>
		/// Web Request Wrapper
		/// </summary>
		/// <param name="method">Http Method</param>
		/// <param name="url">Full url to the web resource</param>
		/// <param name="postData">Data to post in querystring format</param>
		/// <returns>The web server response.</returns>
		protected string WebRequest(Method method, string url, string postData) {
			HttpWebRequest webRequest = null;
			StreamWriter requestWriter = null;
			string responseData = "";

			webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
			webRequest.Method = method.ToString();
			webRequest.ServicePoint.Expect100Continue = false;
			webRequest.UserAgent  = _userAgent;
			webRequest.Timeout = 20000;

			if (method == Method.POST) {
				webRequest.ContentType = "application/x-www-form-urlencoded";

				requestWriter = new StreamWriter(webRequest.GetRequestStream());
				try {
					requestWriter.Write(postData);
				} catch {
					throw;
				} finally {
					requestWriter.Close();
					requestWriter = null;
				}
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
