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

using GreenshotPlugin.Controls;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Provides a predefined set of algorithms that are supported officially by the OAuth 1.x protocol
	/// </summary>
	public enum OAuthSignatureTypes  {
		HMACSHA1,
		PLAINTEXT,
		RSASHA1
	}
	
	/// <summary>
	/// Used HTTP Method, this is for the OAuth 1.x protocol
	/// </summary>
	public enum HTTPMethod { GET, POST, PUT, DELETE };

	/// <summary>
	/// Settings for the OAuth 2 protocol
	/// </summary>
	public class OAuth2Settings {
		public OAuth2Settings() {
			AdditionalAttributes = new Dictionary<string, string>();
			// Create a default state
			State = Guid.NewGuid().ToString();
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
		public string FormattedAuthUrl {
			get {
				return AuthUrlPattern.FormatWith(this);
			}
		}

		/// <summary>
		/// The URL to get a Token
		/// </summary>
		public string TokenUrlPattern {
			get;
			set;
		}

		/// <summary>
		/// Get formatted Token url (this will call a FormatWith(this) on the TokenUrlPattern
		/// </summary>
		public string FormattedTokenUrl {
			get {
				return TokenUrlPattern.FormatWith(this);
			}
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
				if (!string.IsNullOrEmpty(AccessToken) && AccessTokenExpires != null) {
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
	}

	/// <summary>
	/// An OAuth 1 session object
	/// </summary>
	public class OAuthSession {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(OAuthSession));
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
		private bool _checkVerifier = true;
		private bool _useHttpHeadersForAuthorization = true;
		private IDictionary<string, string> _accessTokenResponseParameters;
		private IDictionary<string, string> _requestTokenResponseParameters;
		private readonly IDictionary<string, object> _requestTokenParameters = new Dictionary<string, object>();
		
		public IDictionary<string, object> RequestTokenParameters {
			get { return _requestTokenParameters; }
		}

		/// <summary>
		/// Parameters of the last called getAccessToken
		/// </summary>
		public IDictionary<string, string> AccessTokenResponseParameters {
			get {
				return _accessTokenResponseParameters;
			}
		}
		/// <summary>
		/// Parameters of the last called getRequestToken
		/// </summary>
		public IDictionary<string, string> RequestTokenResponseParameters {
			get {
				return _requestTokenResponseParameters;
			}
		}
		private readonly string _consumerKey;
		private readonly string _consumerSecret;

		// default browser size
		private Size _browserSize = new Size(864, 587);
		private string _loginTitle = "Authorize Greenshot access";

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
		public string CallbackUrl {
			get {
				return _callbackUrl;
			}
			set {
				_callbackUrl = value;
			}
		}
		public bool CheckVerifier {
			get {
				return _checkVerifier;
			}
			set {
				_checkVerifier = value;
			}
		}

		public Size BrowserSize {
			get {
				return _browserSize;
			}
			set {
				_browserSize = value;
			}
		}

		public string LoginTitle {
			get {
				return _loginTitle;
			}
			set {
				_loginTitle = value;
			}
		}
		public bool UseHTTPHeadersForAuthorization {
			get {
				return _useHttpHeadersForAuthorization;
			}
			set {
				_useHttpHeadersForAuthorization = value;
			}
		}

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
				throw new ArgumentNullException("hashAlgorithm");
			}

			if (string.IsNullOrEmpty(data)) {
				throw new ArgumentNullException("data");
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
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", key, UrlEncode3986(string.Format("{0}",queryParameters[key])));
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
				if (UNRESERVED_CHARS.IndexOf(symbol) != -1) {
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
		private void GetRequestToken() {
			IDictionary<string, object> parameters = new Dictionary<string, object>();
			foreach(var value in _requestTokenParameters) {
				parameters.Add(value);
			}
			Sign(RequestTokenMethod, RequestTokenUrl, parameters);
			string response = MakeRequest(RequestTokenMethod, RequestTokenUrl, null, parameters, null);
			if (!string.IsNullOrEmpty(response)) {
				response = NetworkHelper.UrlDecode(response);
				LOG.DebugFormat("Request token response: {0}", response);
				_requestTokenResponseParameters = NetworkHelper.ParseQueryString(response);
				string value;
				if (_requestTokenResponseParameters.TryGetValue(OAUTH_TOKEN_KEY, out value)) {
					Token = value;
					TokenSecret = _requestTokenResponseParameters[OAUTH_TOKEN_SECRET_KEY];
				}
			}
		}

		/// <summary>
		/// Authorize the token by showing the dialog
		/// </summary>
		/// <returns>The request token.</returns>
		private String GetAuthorizeToken() {
			if (string.IsNullOrEmpty(Token)) {
				Exception e = new Exception("The request token is not set");
				throw e;
			}
			LOG.DebugFormat("Opening AuthorizationLink: {0}", AuthorizationLink);
			OAuthLoginForm oAuthLoginForm = new OAuthLoginForm(LoginTitle, BrowserSize, AuthorizationLink, CallbackUrl);
			oAuthLoginForm.ShowDialog();
			if (oAuthLoginForm.isOk) {
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
		private String GetAccessToken() {
			if (string.IsNullOrEmpty(Token) || (CheckVerifier && string.IsNullOrEmpty(Verifier))) {
				Exception e = new Exception("The request token and verifier were not set");
				throw e;
			}

			IDictionary<string, object> parameters = new Dictionary<string, object>();
			Sign(AccessTokenMethod, AccessTokenUrl, parameters);
			string response = MakeRequest(AccessTokenMethod, AccessTokenUrl, null, parameters, null);
			if (!string.IsNullOrEmpty(response)) {
				response = NetworkHelper.UrlDecode(response);
				LOG.DebugFormat("Access token response: {0}", response);
				_accessTokenResponseParameters = NetworkHelper.ParseQueryString(response);
				string tokenValue;
				if (_accessTokenResponseParameters.TryGetValue(OAUTH_TOKEN_KEY, out tokenValue) && tokenValue != null) {
					Token = tokenValue;
				}
				string secretValue;
				if (_accessTokenResponseParameters.TryGetValue(OAUTH_TOKEN_SECRET_KEY, out secretValue) && secretValue != null) {
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
			LOG.Debug("Creating Token");
			try {
				GetRequestToken();
			} catch (Exception ex) {
				LOG.Error(ex);
				throw new NotSupportedException("Service is not available: " + ex.Message);
			}
			if (string.IsNullOrEmpty(GetAuthorizeToken())) {
				LOG.Debug("User didn't authenticate!");
				return false;
			}
			try {
				Thread.Sleep(1000);
				return GetAccessToken() != null;
			} catch (Exception ex) {
				LOG.Error(ex);
				throw;
			}
		}

		/// <summary>
		/// Get the link to the authorization page for this application.
		/// </summary>
		/// <returns>The url with a valid request token, or a null string.</returns>
		private string AuthorizationLink {
			get {
				return AuthorizeUrl + "?" + OAUTH_TOKEN_KEY + "=" + Token + "&" + OAUTH_CALLBACK_KEY + "=" + UrlEncode3986(CallbackUrl);
			}
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="requestURL">The full url, including the querystring for the signing/request</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HTTPMethod method, string requestURL, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
			return MakeOAuthRequest(method, requestURL, requestURL, null, parametersToSign, additionalParameters, postData);
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="requestURL">The full url, including the querystring for the signing/request</param>
		/// <param name="headers">Header values</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HTTPMethod method, string requestURL, IDictionary<string, string> headers, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
			return MakeOAuthRequest(method, requestURL, requestURL, headers, parametersToSign, additionalParameters, postData);
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="signUrl">The full url, including the querystring for the signing</param>
		/// <param name="requestURL">The full url, including the querystring for the request</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HTTPMethod method, string signUrl, string requestURL, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
			return MakeOAuthRequest(method, signUrl, requestURL, null, parametersToSign, additionalParameters, postData);
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="signUrl">The full url, including the querystring for the signing</param>
		/// <param name="requestURL">The full url, including the querystring for the request</param>
		/// <param name="headers">Headers for the request</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HTTPMethod method, string signUrl, string requestURL, IDictionary<string, string> headers, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
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
					foreach(var parameter in parametersToSign) {
						newParameters.Add(parameter);
					}
					if (additionalParameters != null) {
						foreach(var parameter in additionalParameters) {
							newParameters.Add(parameter);
						}
					}
					return MakeRequest(method, requestURL, headers, newParameters, postData);
				} catch (WebException wEx) {
					lastException = wEx;
					if (wEx.Response != null) {
						HttpWebResponse response = wEx.Response as HttpWebResponse;
						if (response != null && response.StatusCode == HttpStatusCode.Unauthorized) {
							Token = null;
							TokenSecret = null;
							// Remove oauth keys, so they aren't added double
							List<string> keysToDelete = new List<string>();
							foreach (string parameterKey in parametersToSign.Keys) {
								if (parameterKey.StartsWith(OAUTH_PARAMETER_PREFIX)) {
									keysToDelete.Add(parameterKey);
								}
							}
							foreach(string keyToDelete in keysToDelete) {
								parametersToSign.Remove(keyToDelete);
							}
							continue;
						}
					}
					throw;
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
		/// <param name="requestURL">Url to call</param>
		/// <param name="parameters">IDictionary<string, string></param>
		private void Sign(HTTPMethod method, string requestURL, IDictionary<string, object> parameters) {
			if (parameters == null) {
				throw new ArgumentNullException("parameters");
			}
			// Build the signature base
			StringBuilder signatureBase = new StringBuilder();

			// Add Method to signature base
			signatureBase.Append(method.ToString()).Append("&");

			// Add normalized URL
			Uri url = new Uri(requestURL);
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
				case OAuthSignatureTypes.RSASHA1:
					parameters.Add(OAUTH_SIGNATURE_METHOD_KEY, RSASHA1SignatureType);
					break;
				case OAuthSignatureTypes.PLAINTEXT:
					parameters.Add(OAUTH_SIGNATURE_METHOD_KEY, PlainTextSignatureType);
					break;
				case OAuthSignatureTypes.HMACSHA1:
				default:
					parameters.Add(OAUTH_SIGNATURE_METHOD_KEY, HMACSHA1SignatureType);
					break;
			}
			parameters.Add(OAUTH_CONSUMER_KEY_KEY, _consumerKey);
			if (CallbackUrl != null && RequestTokenUrl != null && requestURL.StartsWith(RequestTokenUrl)) {
				parameters.Add(OAUTH_CALLBACK_KEY, CallbackUrl);
			}
			if (!string.IsNullOrEmpty(Verifier)) {
				parameters.Add(OAUTH_VERIFIER_KEY, Verifier);
			}
			if (!string.IsNullOrEmpty(Token)) {
				parameters.Add(OAUTH_TOKEN_KEY, Token);
			}
			signatureBase.Append(UrlEncode3986(GenerateNormalizedParametersString(parameters)));
			LOG.DebugFormat("Signature base: {0}", signatureBase);
			string key = string.Format(CultureInfo.InvariantCulture, "{0}&{1}", UrlEncode3986(_consumerSecret), string.IsNullOrEmpty(TokenSecret) ? string.Empty : UrlEncode3986(TokenSecret));
			switch (SignatureType) {
				case OAuthSignatureTypes.RSASHA1:
					// Code comes from here: http://www.dotnetfunda.com/articles/article1932-rest-service-call-using-oauth-10-authorization-with-rsa-sha1.aspx
					// Read the .P12 file to read Private/Public key Certificate
					string certFilePath = _consumerKey; // The .P12 certificate file path Example: "C:/mycertificate/MCOpenAPI.p12
					string password = _consumerSecret; // password to read certificate .p12 file
					// Read the Certification from .P12 file.
					X509Certificate2 cert = new X509Certificate2(certFilePath.ToString(), password);
					// Retrieve the Private key from Certificate.
					RSACryptoServiceProvider RSAcrypt = (RSACryptoServiceProvider)cert.PrivateKey;
					// Create a RSA-SHA1 Hash object
					using (SHA1Managed shaHASHObject = new SHA1Managed()) {
						// Create Byte Array of Signature base string
						byte[] data = Encoding.ASCII.GetBytes(signatureBase.ToString());
						// Create Hashmap of Signature base string
						byte[] hash = shaHASHObject.ComputeHash(data);
						// Create Sign Hash of base string
						// NOTE - 'SignHash' gives correct data. Don't use SignData method
						byte[] rsaSignature = RSAcrypt.SignHash(hash, CryptoConfig.MapNameToOID("SHA1"));
						// Convert to Base64 string
						string base64string = Convert.ToBase64String(rsaSignature);
						// Return the Encoded UTF8 string
						parameters.Add(OAUTH_SIGNATURE_KEY, UrlEncode3986(base64string));
					}
					break;
				case OAuthSignatureTypes.PLAINTEXT:
					parameters.Add(OAUTH_SIGNATURE_KEY, key);
					break;
				case OAuthSignatureTypes.HMACSHA1:
				default:
					// Generate Signature and add it to the parameters
					HMACSHA1 hmacsha1 = new HMACSHA1();
					hmacsha1.Key = Encoding.UTF8.GetBytes(key);
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
		/// <param name="requestURL"></param>
		/// <param name="headers"></param>
		/// <param name="parameters"></param>
		/// <param name="postData">IBinaryParameter</param>
		/// <returns>Response from server</returns>
		private string MakeRequest(HTTPMethod method, string requestURL, IDictionary<string, string> headers, IDictionary<string, object> parameters, IBinaryContainer postData) {
			if (parameters == null) {
				throw new ArgumentNullException("parameters");
			}
			IDictionary<string, object> requestParameters;
			// Add oAuth values as HTTP headers, if this is allowed
			StringBuilder authHeader = null;
			if (UseHTTPHeadersForAuthorization) {
				authHeader = new StringBuilder();
				requestParameters = new Dictionary<string, object>();
				foreach (string parameterKey in parameters.Keys) {
					if (parameterKey.StartsWith(OAUTH_PARAMETER_PREFIX)) {
						authHeader.AppendFormat(CultureInfo.InvariantCulture, "{0}=\"{1}\", ", parameterKey, UrlEncode3986(string.Format("{0}",parameters[parameterKey])));
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
					requestURL += "?" + NetworkHelper.GenerateQueryParameters(requestParameters);
				}
			}
			// Create webrequest
			HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(requestURL);
			webRequest.Method = method.ToString();
			webRequest.ServicePoint.Expect100Continue = false;
			webRequest.UserAgent = _userAgent;
			webRequest.Timeout = 100000;

			if (UseHTTPHeadersForAuthorization && authHeader != null) {
				LOG.DebugFormat("Authorization: OAuth {0}", authHeader);
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
					foreach (string parameterKey in requestParameters.Keys) {
						if (parameters[parameterKey] is IBinaryContainer) {
							IBinaryContainer binaryParameter = parameters[parameterKey] as IBinaryContainer;
							form.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", UrlEncode3986(parameterKey), UrlEncode3986(binaryParameter.ToBase64String(Base64FormattingOptions.None)));
						} else {
							form.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", UrlEncode3986(parameterKey), UrlEncode3986(string.Format("{0}",parameters[parameterKey])));
						}
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
				responseData = NetworkHelper.GetResponse(webRequest);
				LOG.DebugFormat("Response: {0}", responseData);
			} catch (Exception ex) {
				LOG.Error("Couldn't retrieve response: ", ex);
				throw;
			} finally {
				webRequest = null;
			}

			return responseData;
		}
	}

	/// <summary>
	/// OAuth 2.0 verification code receiver that runs a local server on a free port
	/// and waits for a call with the authorization verification code.
	/// </summary>
	public class LocalServerCodeReceiver {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(LocalServerCodeReceiver));

		private string _loopbackCallback = "http://localhost:{0}/authorize/";
		/// <summary>
		/// The call back format. Expects one port parameter.
		/// Default: http://localhost:{0}/authorize/
		/// </summary>
		public string LoopbackCallbackUrl {
			get {
				return _loopbackCallback;
			}
			set {
				_loopbackCallback = value;
			}
		}

		private string _closePageResponse =
@"<html>
<head><title>OAuth 2.0 Authentication Token Received</title></head>
<body>
Greenshot received verification code. You can close this browser / tab if it is not closed itself...
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

		/// <summary>
		/// HTML code to to return the browser, default it will try to close the browser / tab, this won't always work.
		/// </summary>
		public string ClosePageResponse {
			get {
				return _closePageResponse;
			}
			set {
				_closePageResponse = value;
			}
		}

		private string _redirectUri;
		/// <summary>
		/// The URL to redirect to
		/// </summary>
		protected string RedirectUri {
			get {
				if (!string.IsNullOrEmpty(_redirectUri)) {
					return _redirectUri;
				}

				return _redirectUri = string.Format(_loopbackCallback, GetRandomUnusedPort());
			}
		}

		/// <summary>
		/// The OAuth code receiver
		/// </summary>
		/// <param name="authorizationUrl"></param>
		/// <returns>Dictionary with values</returns>
		public IDictionary<string, string> ReceiveCode(OAuth2Settings oauth2Settings) {
			// Set the redirect URL on the settings
			oauth2Settings.RedirectUrl = RedirectUri;
			using (var listener = new HttpListener()) {
				listener.Prefixes.Add(oauth2Settings.RedirectUrl);
				try {
					listener.Start();

					// Get the formatted FormattedAuthUrl
					string authorizationUrl = oauth2Settings.FormattedAuthUrl;
					LOG.DebugFormat("Open a browser with: {0}", authorizationUrl);
					Process.Start(authorizationUrl);

					// Wait to get the authorization code response.
					var context = listener.GetContext();
					try {
						NameValueCollection nameValueCollection = context.Request.QueryString;

						// Write a "close" response.
						using (var writer = new StreamWriter(context.Response.OutputStream)) {
							writer.WriteLine(ClosePageResponse);
							writer.Flush();
						}

						// Create a new response URL with a dictionary that contains all the response query parameters.
						IDictionary<string, string> returnValues = new Dictionary<string, string>();
						foreach (var name in nameValueCollection.AllKeys) {
							if (!returnValues.ContainsKey(name)) {
								returnValues.Add(name, nameValueCollection[name]);
							}
						}
						return returnValues;
					} finally {
						context.Response.OutputStream.Close();
					}
				} finally {
					listener.Close();
				}
			}
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
		/// <summary>
		/// Upload parameters by post
		/// </summary>
		/// <param name="url"></param>
		/// <param name="parameters">Form-Url-Parameters</param>
		/// <param name="settings">OAuth2Settings</param>
		/// <returns>response</returns>
		public static string HttpPost(string url, IDictionary<string, object> parameters, OAuth2Settings settings) {
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
			webRequest.Method = "POST";
			webRequest.KeepAlive = true;
			webRequest.Credentials = CredentialCache.DefaultCredentials;

			AddOAuth2Credentials(webRequest, settings);
			return NetworkHelper.UploadFormUrlEncoded(webRequest, parameters);
		}

		/// <summary>
		/// Generate an OAuth 2 Token by using the supplied code
		/// </summary>
		/// <param name="code">Code to get the RefreshToken</param>
		/// <param name="settings">OAuth2Settings to update with the information that was retrieved</param>
		public static void GenerateRefreshToken(string code, OAuth2Settings settings) {
			// Use the returned code to get a refresh code
			IDictionary<string, object> data = new Dictionary<string, object>();
			data.Add("code", code);
			data.Add("client_id", settings.ClientId);
			data.Add("redirect_uri", settings.RedirectUrl);
			data.Add("client_secret", settings.ClientSecret);
			data.Add("grant_type", "authorization_code");

			string accessTokenJsonResult = HttpPost(settings.FormattedTokenUrl, data, settings);
			IDictionary<string, object> refreshTokenResult = JSONHelper.JsonDecode(accessTokenJsonResult);
			// gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
			//  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
			//	"expires_in":3920,
			//	"token_type":"Bearer",
			//	"refresh_token":"1/xEoDL4iW3cxlI7yDbSRFYNG01kVKM2C-259HOF2aQbI"
			settings.AccessToken = (string)refreshTokenResult["access_token"] as string;
			settings.RefreshToken = (string)refreshTokenResult["refresh_token"] as string;

			object seconds = refreshTokenResult["expires_in"];
			if (seconds != null) {
				settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds((double)seconds);
			}
		}

		/// <summary>
		/// Go out and retrieve a new access token via refresh-token with the TokenUrl in the settings
		/// Will upate the access token, refresh token, expire date
		/// </summary>
		/// <param name="settings"></param>
		public static void GenerateAccessToken(OAuth2Settings settings) {
			IDictionary<string, object> data = new Dictionary<string, object>();
			data.Add("refresh_token", settings.RefreshToken);
			data.Add("client_id", settings.ClientId);
			data.Add("client_secret", settings.ClientSecret);
			data.Add("grant_type", "refresh_token");

			string accessTokenJsonResult = HttpPost(settings.FormattedTokenUrl, data, settings);
			// gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
			//  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
			//	"expires_in":3920,
			//	"token_type":"Bearer",

			IDictionary<string, object> accessTokenResult = JSONHelper.JsonDecode(accessTokenJsonResult);
			settings.AccessToken = (string)accessTokenResult["access_token"] as string;
			object seconds = accessTokenResult["expires_in"];
			if (seconds != null) {
				settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds((double)seconds);
			}
		}

		/// <summary>
		/// Authenticate via a local server by using the LocalServerCodeReceiver
		/// If this works, immediately generate a refresh token afterwards, otherwise this throws an exception
		/// </summary>
		/// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
		public static void AuthenticateViaLocalServer(OAuth2Settings settings) {
			var codeReceiver = new LocalServerCodeReceiver();
			IDictionary<string, string> result = codeReceiver.ReceiveCode(settings);

			string code;
			if (result.TryGetValue("code", out code)) {
				GenerateRefreshToken(code, settings);
			}
			string error;
			if (result.TryGetValue("error", out error)) {
				if ("access_denied" == error) {
					throw new UnauthorizedAccessException("Access denied");
				} else {
					throw new Exception(error);
				}
			}
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
	}
}
