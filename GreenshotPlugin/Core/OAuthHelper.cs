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

using GreenshotPlugin.Controls;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Provides a predefined set of algorithms that are supported officially by the OAuth 1.x protocol
	/// </summary>
	public enum OAuthSignatureTypes  {
		HMACSHA1,
		PLAINTEXT,
	}
	
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

	/// <summary>
	/// Settings for the OAuth 2 protocol
	/// </summary>
	public class OAuth2Settings {
		public OAuth2Settings() {
			AdditionalAttributes = new Dictionary<string, string>();
			// Create a default state
			State = Guid.NewGuid().ToString();
			AuthorizeMode = OAuth2AuthorizeMode.Unknown;
		}

		public OAuth2AuthorizeMode AuthorizeMode {
			get;
			set;
		}

		/// <summary>
		/// Specify the name of the cloud service, so it can be used in window titles, logs etc
		/// </summary>
		public string CloudServiceName {
			get;
			set;
		}

		/// <summary>
		/// Specify the size of the embedded Browser, if using this
		/// </summary>
		public Size BrowserSize {
			get;
			set;
		}

		/// <summary>
		/// The OAuth 2 client id
		/// </summary>
		public string ClientId {
			get;
			set;
		}

		/// <summary>
		/// The OAuth 2 client secret
		/// </summary>
		public string ClientSecret {
			get;
			set;
		}

		/// <summary>
		/// The OAuth 2 state, this is something that is passed to the server, is not processed but returned back to the client.
		/// e.g. a correlation ID
		/// Default this is filled with a new Guid
		/// </summary>
		public string State {
			get;
			set;
		}

		/// <summary>
		/// The autorization URL where the values of this class can be "injected"
		/// </summary>
		public string AuthUrlPattern {
			get;
			set;
		}

		/// <summary>
		/// Get formatted Auth url (this will call a FormatWith(this) on the AuthUrlPattern
		/// </summary>
		public string FormattedAuthUrl => AuthUrlPattern.FormatWith(this);

		/// <summary>
		/// The URL to get a Token
		/// </summary>
		public string TokenUrl {
			get;
			set;
		}

		/// <summary>
		/// This is the redirect URL, in some implementations this is automatically set (LocalServerCodeReceiver)
		/// In some implementations this could be e.g. urn:ietf:wg:oauth:2.0:oob or urn:ietf:wg:oauth:2.0:oob:auto
		/// </summary>
		public string RedirectUrl {
			get;
			set;
		}

		/// <summary>
		/// Bearer token for accessing OAuth 2 services
		/// </summary>
		public string AccessToken {
			get;
			set;
		}

		/// <summary>
		/// Expire time for the AccessToken, this this time (-60 seconds) is passed a new AccessToken needs to be generated with the RefreshToken
		/// </summary>
		public DateTimeOffset AccessTokenExpires {
			get;
			set;
		}

		/// <summary>
		/// Return true if the access token is expired.
		/// Important "side-effect": if true is returned the AccessToken will be set to null!
		/// </summary>
		public bool IsAccessTokenExpired {
			get {
				bool expired = true;
				if (!string.IsNullOrEmpty(AccessToken)) {
					expired = DateTimeOffset.Now.AddSeconds(60) > AccessTokenExpires;
				}
				// Make sure the token is not usable
				if (expired) {
					AccessToken = null;
				}
				return expired;
			}
		}

		/// <summary>
		/// Token used to get a new Access Token
		/// </summary>
		public string RefreshToken {
			get;
			set;
		}

		/// <summary>
		/// Put anything in here which is needed for the OAuth 2 implementation of this specific service but isn't generic, e.g. for Google there is a "scope"
		/// </summary>
		public IDictionary<string, string> AdditionalAttributes {
			get;
			set;
		}

		/// <summary>
		/// This contains the code returned from the authorization, but only shortly after it was received.
		/// It will be cleared as soon as it was used.
		/// </summary>
		public string Code {
			get;
			set;
		}
	}

	/// <summary>
	/// An OAuth 1 session object
	/// </summary>
	public class OAuthSession {
		private static readonly ILog Log = LogManager.GetLogger(typeof(OAuthSession));
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

		protected static Random random = new Random();

		protected const string UnreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

		private string _userAgent = "Greenshot";
		private IDictionary<string, string> _requestTokenResponseParameters;

		public IDictionary<string, object> RequestTokenParameters { get; } = new Dictionary<string, object>();

		/// <summary>
		/// Parameters of the last called getAccessToken
		/// </summary>
		public IDictionary<string, string> AccessTokenResponseParameters { get; private set; }

		/// <summary>
		/// Parameters of the last called getRequestToken
		/// </summary>
		public IDictionary<string, string> RequestTokenResponseParameters => _requestTokenResponseParameters;

		private readonly string _consumerKey;
		private readonly string _consumerSecret;

		// default _browser size

		#region PublicProperties
		public HTTPMethod RequestTokenMethod {
			get;
			set;
		}
		public HTTPMethod AccessTokenMethod {
			get;
			set;
		}
		public string RequestTokenUrl {
			get;
			set;
		}
		public string AuthorizeUrl {
			get;
			set;
		}
		public string AccessTokenUrl {
			get;
			set;
		}
		public string Token {
			get;
			set;
		}
		public string TokenSecret {
			get;
			set;
		}
		public string Verifier {
			get;
			set;
		}
		public OAuthSignatureTypes SignatureType {
			get;
			set;
		}

		public bool UseMultipartFormData { get; set; }
		public string UserAgent {
			get {
				return _userAgent;
			}
			set {
				_userAgent = value;
			}
		}
		public string CallbackUrl { get; set; } = "http://getgreenshot.org";

		public bool CheckVerifier { get; set; } = true;

		public Size BrowserSize { get; set; } = new Size(864, 587);

		public string LoginTitle { get; set; } = "Authorize Greenshot access";

		public bool UseHttpHeadersForAuthorization { get; set; } = true;

		public bool AutoLogin {
			get;
			set;
		}
		
		#endregion

		/// <summary>
		/// Create an OAuthSession with the consumerKey / consumerSecret
		/// </summary>
		/// <param name="consumerKey">"Public" key for the encoding. When using RSASHA1 this is the path to the private key file</param>
		/// <param name="consumerSecret">"Private" key for the encoding. when usin RSASHA1 this is the password for the private key file</param>
		public OAuthSession(string consumerKey, string consumerSecret) {
			_consumerKey = consumerKey;
			_consumerSecret = consumerSecret;
			UseMultipartFormData = true;
			RequestTokenMethod = HTTPMethod.GET;
			AccessTokenMethod = HTTPMethod.GET;
			SignatureType = OAuthSignatureTypes.HMACSHA1;
			AutoLogin = true;
		}

		/// <summary>
		/// Helper function to compute a hash value
		/// </summary>
		/// <param name="hashAlgorithm">The hashing algorithm used. If that algorithm needs some initialization, like HMAC and its derivatives, they should be initialized prior to passing it to this function</param>
		/// <param name="data">The data to hash</param>
		/// <returns>a Base64 string of the hash value</returns>
		private static string ComputeHash(HashAlgorithm hashAlgorithm, string data) {
			if (hashAlgorithm == null) {
				throw new ArgumentNullException(nameof(hashAlgorithm));
			}

			if (string.IsNullOrEmpty(data)) {
				throw new ArgumentNullException(nameof(data));
			}

			byte[] dataBuffer = Encoding.UTF8.GetBytes(data);
			byte[] hashBytes = hashAlgorithm.ComputeHash(dataBuffer);

			return Convert.ToBase64String(hashBytes);
		}

		/// <summary>
		/// Generate the normalized paramter string
		/// </summary>
		/// <param name="queryParameters">the list of query parameters</param>
		/// <returns>a string with the normalized query parameters</returns>
		private static string GenerateNormalizedParametersString(IDictionary<string, object> queryParameters) {
			if (queryParameters == null || queryParameters.Count == 0) {
				return string.Empty;
			}

			queryParameters = new SortedDictionary<string, object>(queryParameters);

			StringBuilder sb = new StringBuilder();
			foreach (string key in queryParameters.Keys) {
				if (queryParameters[key] is string) {
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", key, UrlEncode3986($"{queryParameters[key]}"));
				}
			}
			sb.Remove(sb.Length - 1, 1);

			return sb.ToString();
		}

		/// <summary>
		/// This is a different Url Encode implementation since the default .NET one outputs the percent encoding in lower case.
		/// While this is not a problem with the percent encoding spec, it is used in upper case throughout OAuth
		/// The resulting string is for UTF-8 encoding!
		/// </summary>
		/// <param name="value">The value to Url encode</param>
		/// <returns>Returns a Url encoded string (unicode) with UTF-8 encoded % values</returns>
		public static string UrlEncode3986(string value) {
			StringBuilder result = new StringBuilder();

			foreach (char symbol in value) {
				if (UnreservedChars.IndexOf(symbol) != -1) {
					result.Append(symbol);
				} else {
					byte[] utf8Bytes = Encoding.UTF8.GetBytes(symbol.ToString());
					foreach(byte utf8Byte in utf8Bytes) {
						result.AppendFormat("%{0:X2}", utf8Byte);
					}
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
		/// <returns>response, this doesn't need to be used!!</returns>
		private string GetRequestToken() {
			IDictionary<string, object> parameters = new Dictionary<string, object>();
			foreach(var value in RequestTokenParameters) {
				parameters.Add(value);
			}
			Sign(RequestTokenMethod, RequestTokenUrl, parameters);
			string response = MakeRequest(RequestTokenMethod, RequestTokenUrl, null, parameters, null);
			if (!string.IsNullOrEmpty(response)) {
				response = NetworkHelper.UrlDecode(response);
				Log.DebugFormat("Request token response: {0}", response);
				_requestTokenResponseParameters = NetworkHelper.ParseQueryString(response);
				string value;
				if (_requestTokenResponseParameters.TryGetValue(OAUTH_TOKEN_KEY, out value)) {
					Token = value;
					TokenSecret = _requestTokenResponseParameters[OAUTH_TOKEN_SECRET_KEY];
				}
			}
			return response;
		}

		/// <summary>
		/// Authorize the token by showing the dialog
		/// </summary>
		/// <param name="requestTokenResponse">Pass the response from the server's request token, so if there is something wrong we can show it.</param>
		/// <returns>The request token.</returns>
		private string GetAuthorizeToken(string requestTokenResponse) {
			if (string.IsNullOrEmpty(Token)) {
				Exception e = new Exception("The request token is not set, service responded with: " + requestTokenResponse);
				throw e;
			}
			Log.DebugFormat("Opening AuthorizationLink: {0}", AuthorizationLink);
			OAuthLoginForm oAuthLoginForm = new OAuthLoginForm(LoginTitle, BrowserSize, AuthorizationLink, CallbackUrl);
			oAuthLoginForm.ShowDialog();
			if (oAuthLoginForm.IsOk) {
				if (oAuthLoginForm.CallbackParameters != null) {
					string tokenValue;
					if (oAuthLoginForm.CallbackParameters.TryGetValue(OAUTH_TOKEN_KEY, out tokenValue)) {
						Token = tokenValue;
					}
					string verifierValue;
					if (oAuthLoginForm.CallbackParameters.TryGetValue(OAUTH_VERIFIER_KEY, out verifierValue)) {
						Verifier = verifierValue;
					}
				}
			}
			if (CheckVerifier) {
				if (!string.IsNullOrEmpty(Verifier)) {
					return Token;
				}
				return null;
			}
			return Token;
		}

		/// <summary>
		/// Get the access token
		/// </summary>
		/// <returns>The access token.</returns>		
		private string GetAccessToken() {
			if (string.IsNullOrEmpty(Token) || (CheckVerifier && string.IsNullOrEmpty(Verifier))) {
				Exception e = new Exception("The request token and verifier were not set");
				throw e;
			}

			IDictionary<string, object> parameters = new Dictionary<string, object>();
			Sign(AccessTokenMethod, AccessTokenUrl, parameters);
			string response = MakeRequest(AccessTokenMethod, AccessTokenUrl, null, parameters, null);
			if (!string.IsNullOrEmpty(response)) {
				response = NetworkHelper.UrlDecode(response);
				Log.DebugFormat("Access token response: {0}", response);
				AccessTokenResponseParameters = NetworkHelper.ParseQueryString(response);
				string tokenValue;
				if (AccessTokenResponseParameters.TryGetValue(OAUTH_TOKEN_KEY, out tokenValue) && tokenValue != null) {
					Token = tokenValue;
				}
				string secretValue;
				if (AccessTokenResponseParameters.TryGetValue(OAUTH_TOKEN_SECRET_KEY, out secretValue) && secretValue != null) {
					TokenSecret = secretValue;
				}
			}

			return Token;
		}

		/// <summary>
		/// This method goes through the whole authorize process, including a Authorization window.
		/// </summary>
		/// <returns>true if the process is completed</returns>
		public bool Authorize() {
			Token = null;
			TokenSecret = null;
			Verifier = null;
			Log.Debug("Creating Token");
			string requestTokenResponse;
			try {
				requestTokenResponse = GetRequestToken();
			} catch (Exception ex) {
				Log.Error(ex);
				throw new NotSupportedException("Service is not available: " + ex.Message);
			}
			if (string.IsNullOrEmpty(GetAuthorizeToken(requestTokenResponse))) {
				Log.Debug("User didn't authenticate!");
				return false;
			}
			try {
				Thread.Sleep(1000);
				return GetAccessToken() != null;
			} catch (Exception ex) {
				Log.Error(ex);
				throw;
			}
		}

		/// <summary>
		/// Get the link to the authorization page for this application.
		/// </summary>
		/// <returns>The url with a valid request token, or a null string.</returns>
		private string AuthorizationLink => AuthorizeUrl + "?" + OAUTH_TOKEN_KEY + "=" + Token + "&" + OAUTH_CALLBACK_KEY + "=" + UrlEncode3986(CallbackUrl);

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="requestUrl">The full url, including the querystring for the signing/request</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HTTPMethod method, string requestUrl, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
			return MakeOAuthRequest(method, requestUrl, requestUrl, null, parametersToSign, additionalParameters, postData);
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="requestUrl">The full url, including the querystring for the signing/request</param>
		/// <param name="headers">Header values</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HTTPMethod method, string requestUrl, IDictionary<string, string> headers, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
			return MakeOAuthRequest(method, requestUrl, requestUrl, headers, parametersToSign, additionalParameters, postData);
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="signUrl">The full url, including the querystring for the signing</param>
		/// <param name="requestUrl">The full url, including the querystring for the request</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HTTPMethod method, string signUrl, string requestUrl, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
			return MakeOAuthRequest(method, signUrl, requestUrl, null, parametersToSign, additionalParameters, postData);
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="signUrl">The full url, including the querystring for the signing</param>
		/// <param name="requestUrl">The full url, including the querystring for the request</param>
		/// <param name="headers">Headers for the request</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HTTPMethod method, string signUrl, string requestUrl, IDictionary<string, string> headers, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
			if (parametersToSign == null) {
				parametersToSign = new Dictionary<string, object>();
			}
			int retries = 2;
			Exception lastException = null;
			while (retries-- > 0) {
				// If we are not trying to get a Authorization or Accestoken, and we don't have a token, create one
				if (string.IsNullOrEmpty(Token)) {
					if (!AutoLogin || !Authorize()) {
						throw new Exception("Not authorized");
					}
				}
				try {
					Sign(method, signUrl, parametersToSign);

					// Join all parameters
					IDictionary<string, object> newParameters = new Dictionary<string, object>();
					foreach (var parameter in parametersToSign) {
						newParameters.Add(parameter);
					}
					if (additionalParameters != null) {
						foreach (var parameter in additionalParameters) {
							newParameters.Add(parameter);
						}
					}
					return MakeRequest(method, requestUrl, headers, newParameters, postData);
				} catch (UnauthorizedAccessException uaEx) {
					lastException = uaEx;
					Token = null;
					TokenSecret = null;
					// Remove oauth keys, so they aren't added double
					List<string> keysToDelete = new List<string>();
					foreach (string parameterKey in parametersToSign.Keys)
					{
						if (parameterKey.StartsWith(OAUTH_PARAMETER_PREFIX))
						{
							keysToDelete.Add(parameterKey);
						}
					}
					foreach (string keyToDelete in keysToDelete)
					{
						parametersToSign.Remove(keyToDelete);
					}
				}
			}
			if (lastException != null) {
				throw lastException;
			}
			throw new Exception("Not authorized");
		}

		/// <summary>
		/// OAuth sign the parameters, meaning all oauth parameters are added to the supplied dictionary.
		/// And additionally a signature is added.
		/// </summary>
		/// <param name="method">Method (POST,PUT,GET)</param>
		/// <param name="requestUrl">Url to call</param>
		/// <param name="parameters">IDictionary of string and string</param>
		private void Sign(HTTPMethod method, string requestUrl, IDictionary<string, object> parameters) {
			if (parameters == null) {
				throw new ArgumentNullException(nameof(parameters));
			}
			// Build the signature base
			StringBuilder signatureBase = new StringBuilder();

			// Add Method to signature base
			signatureBase.Append(method).Append("&");

			// Add normalized URL
			Uri url = new Uri(requestUrl);
			string normalizedUrl = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", url.Scheme, url.Host);
			if (!((url.Scheme == "http" && url.Port == 80) || (url.Scheme == "https" && url.Port == 443))) {
				normalizedUrl += ":" + url.Port;
			}
			normalizedUrl += url.AbsolutePath;
			signatureBase.Append(UrlEncode3986(normalizedUrl)).Append("&");

			// Add normalized parameters
			parameters.Add(OAUTH_VERSION_KEY, OAUTH_VERSION);
			parameters.Add(OAUTH_NONCE_KEY, GenerateNonce());
			parameters.Add(OAUTH_TIMESTAMP_KEY, GenerateTimeStamp());
			switch(SignatureType) {
				case OAuthSignatureTypes.PLAINTEXT:
					parameters.Add(OAUTH_SIGNATURE_METHOD_KEY, PlainTextSignatureType);
					break;
				default:
					parameters.Add(OAUTH_SIGNATURE_METHOD_KEY, HMACSHA1SignatureType);
					break;
			}
			parameters.Add(OAUTH_CONSUMER_KEY_KEY, _consumerKey);
			if (CallbackUrl != null && RequestTokenUrl != null && requestUrl.StartsWith(RequestTokenUrl)) {
				parameters.Add(OAUTH_CALLBACK_KEY, CallbackUrl);
			}
			if (!string.IsNullOrEmpty(Verifier)) {
				parameters.Add(OAUTH_VERIFIER_KEY, Verifier);
			}
			if (!string.IsNullOrEmpty(Token)) {
				parameters.Add(OAUTH_TOKEN_KEY, Token);
			}
			signatureBase.Append(UrlEncode3986(GenerateNormalizedParametersString(parameters)));
			Log.DebugFormat("Signature base: {0}", signatureBase);
			string key = string.Format(CultureInfo.InvariantCulture, "{0}&{1}", UrlEncode3986(_consumerSecret), string.IsNullOrEmpty(TokenSecret) ? string.Empty : UrlEncode3986(TokenSecret));
			switch (SignatureType) {
				case OAuthSignatureTypes.PLAINTEXT:
					parameters.Add(OAUTH_SIGNATURE_KEY, key);
					break;
				default:
					// Generate Signature and add it to the parameters
					HMACSHA1 hmacsha1 = new HMACSHA1 {Key = Encoding.UTF8.GetBytes(key)};
					string signature = ComputeHash(hmacsha1, signatureBase.ToString());
					parameters.Add(OAUTH_SIGNATURE_KEY, signature);
					break;
			}
		}

		/// <summary>
		/// Make the actual OAuth request, all oauth parameters are passed as header (default) and the others are placed in the url or post data.
		/// Any additional parameters added after the Sign call are not in the signature, this could be by design!
		/// </summary>
		/// <param name="method"></param>
		/// <param name="requestUrl"></param>
		/// <param name="headers"></param>
		/// <param name="parameters"></param>
		/// <param name="postData">IBinaryParameter</param>
		/// <returns>Response from server</returns>
		private string MakeRequest(HTTPMethod method, string requestUrl, IDictionary<string, string> headers, IDictionary<string, object> parameters, IBinaryContainer postData) {
			if (parameters == null) {
				throw new ArgumentNullException(nameof(parameters));
			}
			IDictionary<string, object> requestParameters;
			// Add oAuth values as HTTP headers, if this is allowed
			StringBuilder authHeader = null;
			if (UseHttpHeadersForAuthorization) {
				authHeader = new StringBuilder();
				requestParameters = new Dictionary<string, object>();
				foreach (string parameterKey in parameters.Keys) {
					if (parameterKey.StartsWith(OAUTH_PARAMETER_PREFIX)) {
						authHeader.AppendFormat(CultureInfo.InvariantCulture, "{0}=\"{1}\", ", parameterKey, UrlEncode3986($"{parameters[parameterKey]}"));
					} else if (!requestParameters.ContainsKey(parameterKey)) {
						requestParameters.Add(parameterKey, parameters[parameterKey]);
					}
				}
				// Remove trailing comma and space and add it to the headers
				if (authHeader.Length > 0) {
					authHeader.Remove(authHeader.Length - 2, 2);
				}
			} else {
				requestParameters = parameters;
			}

			if (HTTPMethod.GET == method || postData != null) {
				if (requestParameters.Count > 0) {
					// Add the parameters to the request
					requestUrl += "?" + NetworkHelper.GenerateQueryParameters(requestParameters);
				}
			}
			// Create webrequest
			HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(requestUrl, method);
			webRequest.ServicePoint.Expect100Continue = false;
			webRequest.UserAgent = _userAgent;

			if (UseHttpHeadersForAuthorization && authHeader != null) {
				Log.DebugFormat("Authorization: OAuth {0}", authHeader);
				webRequest.Headers.Add("Authorization: OAuth " + authHeader);
			}

			if (headers != null) {
				foreach(string key in headers.Keys) {
					webRequest.Headers.Add(key, headers[key]);
				}
			}

			if ((HTTPMethod.POST == method || HTTPMethod.PUT == method) && postData == null && requestParameters.Count > 0) {
				if (UseMultipartFormData) {
					NetworkHelper.WriteMultipartFormData(webRequest, requestParameters);
				} else {
					StringBuilder form = new StringBuilder();
					foreach (string parameterKey in requestParameters.Keys)
					{
						var binaryParameter = parameters[parameterKey] as IBinaryContainer;
						form.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", UrlEncode3986(parameterKey), binaryParameter != null ? UrlEncode3986(binaryParameter.ToBase64String(Base64FormattingOptions.None)) : UrlEncode3986($"{parameters[parameterKey]}"));
					}
					// Remove trailing &
					if (form.Length > 0) {
						form.Remove(form.Length - 1, 1);
					}
					webRequest.ContentType = "application/x-www-form-urlencoded";
					byte[] data = Encoding.UTF8.GetBytes(form.ToString());
					using (var requestStream = webRequest.GetRequestStream()) {
						requestStream.Write(data, 0, data.Length);
					}
				}
			} else if (postData != null) {
				postData.Upload(webRequest);
			} else {
				webRequest.ContentLength = 0;
			}

			string responseData;
			try {
				responseData = NetworkHelper.GetResponseAsString(webRequest);
				Log.DebugFormat("Response: {0}", responseData);
			} catch (Exception ex) {
				Log.Error("Couldn't retrieve response: ", ex);
				throw;
			}

			return responseData;
		}
	}

	/// <summary>
	/// OAuth 2.0 verification code receiver that runs a local server on a free port
	/// and waits for a call with the authorization verification code.
	/// </summary>
	public class LocalServerCodeReceiver {
		private static readonly ILog Log = LogManager.GetLogger(typeof(LocalServerCodeReceiver));
		private readonly ManualResetEvent _ready = new ManualResetEvent(true);

		/// <summary>
		/// The call back format. Expects one port parameter.
		/// Default: http://localhost:{0}/authorize/
		/// </summary>
		public string LoopbackCallbackUrl { get; set; } = "http://localhost:{0}/authorize/";

		/// <summary>
		/// HTML code to to return the _browser, default it will try to close the _browser / tab, this won't always work.
		/// You can use CloudServiceName where you want to show the CloudServiceName from your OAuth2 settings
		/// </summary>
		public string ClosePageResponse { get; set; } = @"<html>
<head><title>OAuth 2.0 Authentication CloudServiceName</title></head>
<body>
Greenshot received information from CloudServiceName. You can close this browser / tab if it is not closed itself...
<script type='text/javascript'>
	window.setTimeout(function() {
		window.open('', '_self', ''); 
		window.close(); 
	}, 1000);
	if (window.opener) {
		window.opener.checkToken();
	}
</script>
</body>
</html>";

		private string _redirectUri;
		/// <summary>
		/// The URL to redirect to
		/// </summary>
		protected string RedirectUri {
			get {
				if (!string.IsNullOrEmpty(_redirectUri)) {
					return _redirectUri;
				}

				return _redirectUri = string.Format(LoopbackCallbackUrl, GetRandomUnusedPort());
			}
		}

		private string _cloudServiceName;

		private readonly IDictionary<string, string> _returnValues = new Dictionary<string, string>();


		/// <summary>
		/// The OAuth code receiver
		/// </summary>
		/// <param name="oauth2Settings"></param>
		/// <returns>Dictionary with values</returns>
		public IDictionary<string, string> ReceiveCode(OAuth2Settings oauth2Settings) {
			// Set the redirect URL on the settings
			oauth2Settings.RedirectUrl = RedirectUri;
			_cloudServiceName = oauth2Settings.CloudServiceName;
			using (var listener = new HttpListener()) {
				listener.Prefixes.Add(oauth2Settings.RedirectUrl);
				try {
					listener.Start();

					// Get the formatted FormattedAuthUrl
					string authorizationUrl = oauth2Settings.FormattedAuthUrl;
					Log.DebugFormat("Open a browser with: {0}", authorizationUrl);
					Process.Start(authorizationUrl);

					// Wait to get the authorization code response.
					var context = listener.BeginGetContext(ListenerCallback, listener);
					_ready.Reset();

					while (!context.AsyncWaitHandle.WaitOne(1000, true)) {
						Log.Debug("Waiting for response");
					}
				} catch (Exception) {
					// Make sure we can clean up, also if the thead is aborted
					_ready.Set();
					throw;
				} finally {
					_ready.WaitOne();
					listener.Close();
				}
			}
			return _returnValues;
		}

		/// <summary>
		/// Handle a connection async, this allows us to break the waiting
		/// </summary>
		/// <param name="result">IAsyncResult</param>
		private void ListenerCallback(IAsyncResult result) {
			HttpListener listener = (HttpListener)result.AsyncState;

			//If not listening return immediately as this method is called one last time after Close()
			if (!listener.IsListening) {
				return;
			}

			// Use EndGetContext to complete the asynchronous operation.
			HttpListenerContext context = listener.EndGetContext(result);


			// Handle request
			HttpListenerRequest request = context.Request;
			try {
				NameValueCollection nameValueCollection = request.QueryString;

				// Get response object.
				using (HttpListenerResponse response = context.Response) {
					// Write a "close" response.
					byte[] buffer = Encoding.UTF8.GetBytes(ClosePageResponse.Replace("CloudServiceName", _cloudServiceName));
					// Write to response stream.
					response.ContentLength64 = buffer.Length;
					using (var stream = response.OutputStream) {
						stream.Write(buffer, 0, buffer.Length);
					}
				}

				// Create a new response URL with a dictionary that contains all the response query parameters.
				foreach (var name in nameValueCollection.AllKeys) {
					if (!_returnValues.ContainsKey(name)) {
						_returnValues.Add(name, nameValueCollection[name]);
					}
				}
			} catch (Exception) {
				context.Response.OutputStream.Close();
				throw;
			}
			_ready.Set();
		}

		/// <summary>
		/// Returns a random, unused port.
		/// </summary>
		/// <returns>port to use</returns>
		private static int GetRandomUnusedPort() {
			var listener = new TcpListener(IPAddress.Loopback, 0);
			try {
				listener.Start();
				return ((IPEndPoint)listener.LocalEndpoint).Port;
			} finally {
				listener.Stop();
			}
		}
	}

	/// <summary>
	/// Code to simplify OAuth 2
	/// </summary>
	public static class OAuth2Helper {
		private const string RefreshToken = "refresh_token";
		private const string AccessToken = "access_token";
		private const string Code = "code";
		private const string ClientId = "client_id";
		private const string ClientSecret = "client_secret";
		private const string GrantType = "grant_type";
		private const string AuthorizationCode = "authorization_code";
		private const string RedirectUri = "redirect_uri";
		private const string ExpiresIn = "expires_in";

		/// <summary>
		/// Generate an OAuth 2 Token by using the supplied code
		/// </summary>
		/// <param name="settings">OAuth2Settings to update with the information that was retrieved</param>
		public static void GenerateRefreshToken(OAuth2Settings settings) {
			IDictionary<string, object> data = new Dictionary<string, object>();
			// Use the returned code to get a refresh code
			data.Add(Code, settings.Code);
			data.Add(ClientId, settings.ClientId);
			data.Add(RedirectUri, settings.RedirectUrl);
			data.Add(ClientSecret, settings.ClientSecret);
			data.Add(GrantType, AuthorizationCode);
			foreach (string key in settings.AdditionalAttributes.Keys) {
				data.Add(key, settings.AdditionalAttributes[key]);
			}

			HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(settings.TokenUrl, HTTPMethod.POST);
			NetworkHelper.UploadFormUrlEncoded(webRequest, data);
			string accessTokenJsonResult = NetworkHelper.GetResponseAsString(webRequest, true);

			IDictionary<string, object> refreshTokenResult = JSONHelper.JsonDecode(accessTokenJsonResult);
			if (refreshTokenResult.ContainsKey("error"))
			{
				if (refreshTokenResult.ContainsKey("error_description")) {
					throw new Exception($"{refreshTokenResult["error"]} - {refreshTokenResult["error_description"]}");
				}
				throw new Exception((string)refreshTokenResult["error"]);
			}

			// gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
			//  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
			//	"expires_in":3920,
			//	"token_type":"Bearer",
			//	"refresh_token":"1/xEoDL4iW3cxlI7yDbSRFYNG01kVKM2C-259HOF2aQbI"
			if (refreshTokenResult.ContainsKey(AccessToken))
			{
				settings.AccessToken = (string)refreshTokenResult[AccessToken];
			}
			if (refreshTokenResult.ContainsKey(RefreshToken))
			{
				settings.RefreshToken = (string)refreshTokenResult[RefreshToken];
			}
			if (refreshTokenResult.ContainsKey(ExpiresIn))
			{
				object seconds = refreshTokenResult[ExpiresIn];
				if (seconds != null)
				{
					settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds((double)seconds);
				}
			}
			settings.Code = null;
		}

		/// <summary>
		/// Used to update the settings with the callback information
		/// </summary>
		/// <param name="settings">OAuth2Settings</param>
		/// <param name="callbackParameters">IDictionary</param>
		/// <returns>true if the access token is already in the callback</returns>
		private static bool UpdateFromCallback(OAuth2Settings settings, IDictionary<string, string> callbackParameters)
		{
			if (!callbackParameters.ContainsKey(AccessToken))
			{
				return false;
			}
			if (callbackParameters.ContainsKey(RefreshToken))
			{
				// Refresh the refresh token :)
				settings.RefreshToken = callbackParameters[RefreshToken];
			}
			if (callbackParameters.ContainsKey(ExpiresIn))
			{
				var expiresIn = callbackParameters[ExpiresIn];
				settings.AccessTokenExpires = DateTimeOffset.MaxValue;
				if (expiresIn != null)
				{
					double seconds;
					if (double.TryParse(expiresIn, out seconds))
					{
						settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds(seconds);
					}
				}
			}
			settings.AccessToken = callbackParameters[AccessToken];
			return true;
		}

		/// <summary>
		/// Go out and retrieve a new access token via refresh-token with the TokenUrl in the settings
		/// Will upate the access token, refresh token, expire date
		/// </summary>
		/// <param name="settings"></param>
		public static void GenerateAccessToken(OAuth2Settings settings) {
			IDictionary<string, object> data = new Dictionary<string, object>();
			data.Add(RefreshToken, settings.RefreshToken);
			data.Add(ClientId, settings.ClientId);
			data.Add(ClientSecret, settings.ClientSecret);
			data.Add(GrantType, RefreshToken);
			foreach (string key in settings.AdditionalAttributes.Keys) {
				data.Add(key, settings.AdditionalAttributes[key]);
			}

			HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(settings.TokenUrl, HTTPMethod.POST);
			NetworkHelper.UploadFormUrlEncoded(webRequest, data);
			string accessTokenJsonResult = NetworkHelper.GetResponseAsString(webRequest, true);

			// gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
			//  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
			//	"expires_in":3920,
			//	"token_type":"Bearer",

			IDictionary<string, object> accessTokenResult = JSONHelper.JsonDecode(accessTokenJsonResult);
			if (accessTokenResult.ContainsKey("error")) {
				if ("invalid_grant" == (string)accessTokenResult["error"]) {
					// Refresh token has also expired, we need a new one!
					settings.RefreshToken = null;
					settings.AccessToken = null;
					settings.AccessTokenExpires = DateTimeOffset.MinValue;
					settings.Code = null;
					return;
				} else {
					if (accessTokenResult.ContainsKey("error_description")) {
						throw new Exception($"{accessTokenResult["error"]} - {accessTokenResult["error_description"]}");
					} else {
						throw new Exception((string)accessTokenResult["error"]);
					}
				}
			}

			if (accessTokenResult.ContainsKey(AccessToken))
			{
				settings.AccessToken = (string) accessTokenResult[AccessToken];
				settings.AccessTokenExpires = DateTimeOffset.MaxValue;
			}
			if (accessTokenResult.ContainsKey(RefreshToken)) {
				// Refresh the refresh token :)
				settings.RefreshToken = (string)accessTokenResult[RefreshToken];
			}
			if (accessTokenResult.ContainsKey(ExpiresIn))
			{
				object seconds = accessTokenResult[ExpiresIn];
				if (seconds != null)
				{
					settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds((double) seconds);
				}
			}
		}

		/// <summary>
		/// Authenticate by using the mode specified in the settings
		/// </summary>
		/// <param name="settings">OAuth2Settings</param>
		/// <returns>false if it was canceled, true if it worked, exception if not</returns>
		public static bool Authenticate(OAuth2Settings settings) {
			bool completed;
			switch (settings.AuthorizeMode) {
				case OAuth2AuthorizeMode.LocalServer:
					completed = AuthenticateViaLocalServer(settings);
					break;
				case OAuth2AuthorizeMode.EmbeddedBrowser:
					completed = AuthenticateViaEmbeddedBrowser(settings);
					break;
				default:
					throw new NotImplementedException($"Authorize mode '{settings.AuthorizeMode}' is not 'yet' implemented.");
			}
			return completed;
		}

		/// <summary>
		/// Authenticate via an embedded browser
		/// If this works, return the code
		/// </summary>
		/// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
		/// <returns>true if completed, false if canceled</returns>
		private static bool AuthenticateViaEmbeddedBrowser(OAuth2Settings settings) {
			if (string.IsNullOrEmpty(settings.CloudServiceName)) {
				throw new ArgumentNullException(nameof(settings.CloudServiceName));
			}
			if (settings.BrowserSize == Size.Empty) {
				throw new ArgumentNullException(nameof(settings.BrowserSize));
			}
			OAuthLoginForm loginForm = new OAuthLoginForm($"Authorize {settings.CloudServiceName}", settings.BrowserSize, settings.FormattedAuthUrl, settings.RedirectUrl);
			loginForm.ShowDialog();
			if (loginForm.IsOk) {
				string code;
				if (loginForm.CallbackParameters.TryGetValue(Code, out code) && !string.IsNullOrEmpty(code)) {
					settings.Code = code;
					GenerateRefreshToken(settings);
					return true;
				}
				return UpdateFromCallback(settings, loginForm.CallbackParameters);
			}
			return false;
		}

		/// <summary>
		/// Authenticate via a local server by using the LocalServerCodeReceiver
		/// If this works, return the code
		/// </summary>
		/// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
		/// <returns>true if completed</returns>
		private static bool AuthenticateViaLocalServer(OAuth2Settings settings) {
			var codeReceiver = new LocalServerCodeReceiver();
			IDictionary<string, string> result = codeReceiver.ReceiveCode(settings);

			string code;
			if (result.TryGetValue(Code, out code) && !string.IsNullOrEmpty(code)) {
				settings.Code = code;
				GenerateRefreshToken(settings);
				return true;
			}
			string error;
			if (result.TryGetValue("error", out error)) {
				string errorDescription;
				if (result.TryGetValue("error_description", out errorDescription)) {
					throw new Exception(errorDescription);
				}
				if ("access_denied" == error) {
					throw new UnauthorizedAccessException("Access denied");
				}
				throw new Exception(error);
			}
			return false;
		}

		/// <summary>
		/// Simple helper to add the Authorization Bearer header
		/// </summary>
		/// <param name="webRequest">WebRequest</param>
		/// <param name="settings">OAuth2Settings</param>
		public static void AddOAuth2Credentials(HttpWebRequest webRequest, OAuth2Settings settings) {
			if (!string.IsNullOrEmpty(settings.AccessToken)) {
				webRequest.Headers.Add("Authorization", "Bearer " + settings.AccessToken);
			}
		}

		/// <summary>
		/// Check and authenticate or refresh tokens 
		/// </summary>
		/// <param name="settings">OAuth2Settings</param>
		public static void CheckAndAuthenticateOrRefresh(OAuth2Settings settings) {
			// Get Refresh / Access token
			if (string.IsNullOrEmpty(settings.RefreshToken)) {
				if (!Authenticate(settings)) {
					throw new Exception("Authentication cancelled");
				}
			}
			if (settings.IsAccessTokenExpired) {
				GenerateAccessToken(settings);
				// Get Refresh / Access token
				if (string.IsNullOrEmpty(settings.RefreshToken)) {
					if (!Authenticate(settings)) {
						throw new Exception("Authentication cancelled");
					}
					GenerateAccessToken(settings);
				}
			}
			if (settings.IsAccessTokenExpired) {
				throw new Exception("Authentication failed");
			}
		}

		/// <summary>
		/// CreateWebRequest ready for OAuth 2 access
		/// </summary>
		/// <param name="method">HTTPMethod</param>
		/// <param name="url"></param>
		/// <param name="settings">OAuth2Settings</param>
		/// <returns>HttpWebRequest</returns>
		public static HttpWebRequest CreateOAuth2WebRequest(HTTPMethod method, string url, OAuth2Settings settings) {
			CheckAndAuthenticateOrRefresh(settings);

			HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(url, method);
			AddOAuth2Credentials(webRequest, settings);
			return webRequest;
		}
	}
}
