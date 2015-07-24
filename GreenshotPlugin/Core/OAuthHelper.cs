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
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
			RedirectUrl = "http://getgreenshot.org";
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
		public Uri FormattedAuthUrl {
			get {
				return new Uri(AuthUrlPattern.FormatWith(this));
			}
		}

		/// <summary>
		/// The URL to get a Token
		/// </summary>
		public Uri TokenUrl {
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

		// default _browser size
		private Size _browserSize = new Size(864, 587);
		private string _loginTitle = "Authorize Greenshot access";

		#region PublicProperties
		public HttpMethod RequestTokenMethod {
			get;
			set;
		}
		public HttpMethod AccessTokenMethod {
			get;
			set;
		}
		public Uri RequestTokenUrl {
			get;
			set;
		}
		public Uri AuthorizeUrl {
			get;
			set;
		}
		public Uri AccessTokenUrl {
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
			RequestTokenMethod = HttpMethod.Get;
			AccessTokenMethod = HttpMethod.Get;
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
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", key, Uri.EscapeDataString(string.Format("{0}", queryParameters[key])));
				}
			}
			sb.Remove(sb.Length - 1, 1);

			return sb.ToString();
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
				response = Uri.UnescapeDataString(response.Replace("+", " "));
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
		private String GetAccessToken() {
			if (string.IsNullOrEmpty(Token) || (CheckVerifier && string.IsNullOrEmpty(Verifier))) {
				Exception e = new Exception("The request token and verifier were not set");
				throw e;
			}

			IDictionary<string, object> parameters = new Dictionary<string, object>();
			Sign(AccessTokenMethod, AccessTokenUrl, parameters);
			string response = MakeRequest(AccessTokenMethod, AccessTokenUrl, null, parameters, null);
			if (!string.IsNullOrEmpty(response)) {
				response = Uri.UnescapeDataString(response.Replace("+", " "));
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
		private Uri AuthorizationLink {
			get {
				return new Uri(AuthorizeUrl + "?" + OAUTH_TOKEN_KEY + "=" + Token + "&" + OAUTH_CALLBACK_KEY + "=" + Uri.EscapeDataString(CallbackUrl));
			}
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="requestUri">The full url, including the querystring for the signing/request</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HttpMethod method, Uri requestUri, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
			return MakeOAuthRequest(method, requestUri, requestUri, null, parametersToSign, additionalParameters, postData);
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="requestUri">The full url, including the querystring for the signing/request</param>
		/// <param name="headers">Header values</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HttpMethod method, Uri requestUri, IDictionary<string, string> headers, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
			return MakeOAuthRequest(method, requestUri, requestUri, headers, parametersToSign, additionalParameters, postData);
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="signUri">The full url, including the querystring for the signing</param>
		/// <param name="requestUri">The full url, including the querystring for the request</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HttpMethod method, Uri signUri, Uri requestUri, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
			return MakeOAuthRequest(method, signUri, requestUri, null, parametersToSign, additionalParameters, postData);
		}

		/// <summary>
		/// Submit a web request using oAuth.
		/// </summary>
		/// <param name="method">GET or POST</param>
		/// <param name="signUri">The full url, including the querystring for the signing</param>
		/// <param name="requestUri">The full url, including the querystring for the request</param>
		/// <param name="headers">Headers for the request</param>
		/// <param name="parametersToSign">Parameters for the request, which need to be signed</param>
		/// <param name="additionalParameters">Parameters for the request, which do not need to be signed</param>
		/// <param name="postData">Data to post (MemoryStream)</param>
		/// <returns>The web server response.</returns>
		public string MakeOAuthRequest(HttpMethod method, Uri signUri, Uri requestUri, IDictionary<string, string> headers, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters, IBinaryContainer postData) {
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
					Sign(method, signUri, parametersToSign);
					
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
					return MakeRequest(method, requestUri, headers, newParameters, postData);
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
		private void Sign(HttpMethod method, Uri requestUri, IDictionary<string, object> parameters) {
			if (parameters == null) {
				throw new ArgumentNullException("parameters");
			}
			// Build the signature base
			StringBuilder signatureBase = new StringBuilder();

			// Add Method to signature base
			signatureBase.Append(method.ToString()).Append("&");

			// Add normalized URL
			signatureBase.Append(Uri.EscapeDataString(requestUri.Normalize().ToString())).Append("&");

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
			if (CallbackUrl != null && RequestTokenUrl != null && requestUri.Equals(RequestTokenUrl)) {
				parameters.Add(OAUTH_CALLBACK_KEY, CallbackUrl);
			}
			if (!string.IsNullOrEmpty(Verifier)) {
				parameters.Add(OAUTH_VERIFIER_KEY, Verifier);
			}
			if (!string.IsNullOrEmpty(Token)) {
				parameters.Add(OAUTH_TOKEN_KEY, Token);
			}
			signatureBase.Append(Uri.EscapeDataString(GenerateNormalizedParametersString(parameters)));
			LOG.DebugFormat("Signature base: {0}", signatureBase);
			string key = string.Format(CultureInfo.InvariantCulture, "{0}&{1}", Uri.EscapeDataString(_consumerSecret), string.IsNullOrEmpty(TokenSecret) ? string.Empty : Uri.EscapeDataString(TokenSecret));
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
						parameters.Add(OAUTH_SIGNATURE_KEY, Uri.EscapeDataString(base64string));
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
		/// <param name="requestUri"></param>
		/// <param name="headers"></param>
		/// <param name="parameters"></param>
		/// <param name="postData">IBinaryParameter</param>
		/// <returns>Response from server</returns>
		private string MakeRequest(HttpMethod method, Uri requestUri, IDictionary<string, string> headers, IDictionary<string, object> parameters, IBinaryContainer postData) {
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
						authHeader.AppendFormat(CultureInfo.InvariantCulture, "{0}=\"{1}\", ", parameterKey, Uri.EscapeDataString(string.Format("{0}", parameters[parameterKey])));
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

			if (HttpMethod.Get == method || postData != null) {
				if (requestParameters.Count > 0) {
					// Add the parameters to the request
					var query = NetworkHelper.GenerateQueryParameters(requestParameters);
					requestUri = new Uri(string.Format("{0}?{1}", requestUri.AbsoluteUri, query));
				}
			}
			// Create webrequest
			HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(requestUri, method);
			webRequest.ServicePoint.Expect100Continue = false;
			webRequest.UserAgent = _userAgent;

			if (UseHTTPHeadersForAuthorization && authHeader != null) {
				LOG.DebugFormat("Authorization: OAuth {0}", authHeader);
				webRequest.Headers.Add("Authorization: OAuth " + authHeader);
			}

			if (headers != null) {
				foreach(string key in headers.Keys) {
					webRequest.Headers.Add(key, headers[key]);
				}
			}

			if ((HttpMethod.Post == method || HttpMethod.Put == method) && postData == null && requestParameters.Count > 0) {
				if (UseMultipartFormData) {
					NetworkHelper.WriteMultipartFormData(webRequest, requestParameters);
				} else {
					StringBuilder form = new StringBuilder();
					foreach (string parameterKey in requestParameters.Keys) {
						if (parameters[parameterKey] is IBinaryContainer) {
							IBinaryContainer binaryParameter = parameters[parameterKey] as IBinaryContainer;
							form.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", Uri.EscapeDataString(parameterKey), Uri.EscapeDataString(binaryParameter.ToBase64String(Base64FormattingOptions.None)));
						} else {
							form.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", Uri.EscapeDataString(parameterKey), Uri.EscapeDataString(string.Format("{0}", parameters[parameterKey])));
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
				responseData = NetworkHelper.GetResponseAsString(webRequest);
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
	public class LocalServerCodeReceiver : IDisposable {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(LocalServerCodeReceiver));
		private readonly ManualResetEvent _ready = new ManualResetEvent(true);

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

		/// <summary>
		/// HTML code to to return the _browser, default it will try to close the _browser / tab, this won't always work.
		/// You can use CloudServiceName where you want to show the CloudServiceName from your OAuth2 settings
		/// </summary>
		public string ClosePageResponse {
			get {
				return _closePageResponse;
			}
			set {
				_closePageResponse = value;
			}
		}

		private Uri _redirectUri;
		/// <summary>
		/// The URL to redirect to
		/// </summary>
		protected Uri RedirectUri {
			get {
				if (_redirectUri != null) {
					return _redirectUri;
				}

				return _redirectUri = new Uri(string.Format(_loopbackCallback, GetRandomUnusedPort()));
			}
		}

		private string _cloudServiceName;

		private IDictionary<string, string> _returnValues = new Dictionary<string, string>();


		/// <summary>
		/// The OAuth code receiver
		/// </summary>
		/// <param name="authorizationUrl"></param>
		/// <returns>Dictionary with values</returns>
		public async Task<IDictionary<string, string>> ReceiveCodeAsync(OAuth2Settings oauth2Settings, CancellationToken token = default(CancellationToken)) {
			// Set the redirect URL on the settings
			oauth2Settings.RedirectUrl = Uri.EscapeDataString(RedirectUri.AbsoluteUri);
			_cloudServiceName = oauth2Settings.CloudServiceName;
			using (var listener = new HttpListener()) {
				listener.Prefixes.Add(RedirectUri.AbsoluteUri);
				try {
					listener.Start();

					// Get the formatted FormattedAuthUrl
					var authorizationUrl = oauth2Settings.FormattedAuthUrl;
					LOG.DebugFormat("Open a browser with: {0}", authorizationUrl.AbsoluteUri);
					Process.Start(authorizationUrl.AbsoluteUri);

					// Wait to get the authorization code response.
					var context = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
					_ready.Reset();

					while (!token.IsCancellationRequested && !context.AsyncWaitHandle.WaitOne(1)) {
						LOG.Debug("Waiting for response");
						await Task.Delay(1000).ConfigureAwait(false);
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
			var listener = (HttpListener)result.AsyncState;

			//If not listening return immediately as this method is called one last time after Close()
			if (!listener.IsListening) {
				return;
			}

			// Use EndGetContext to complete the asynchronous operation.
			var context = listener.EndGetContext(result);


			// Handle request
			var request = context.Request;
			try {
				var nameValueCollection = request.QueryString;

				// Get response object.
				using (var response = context.Response) {
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

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_ready.Dispose();
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion
	}

	/// <summary>
	/// Code to simplify OAuth 2
	/// </summary>
	public static class OAuth2Helper {
		private const string REFRESH_TOKEN = "refresh_token";
		private const string ACCESS_TOKEN = "access_token";
		private const string CODE = "code";
		private const string CLIENT_ID = "client_id";
		private const string CLIENT_SECRET = "client_secret";
		private const string GRANT_TYPE = "grant_type";
		private const string AUTHORIZATION_CODE = "authorization_code";
		private const string REDIRECT_URI = "redirect_uri";
		private const string EXPIRES_IN = "expires_in";

		/// <summary>
		/// Generate an OAuth 2 Token by using the supplied code
		/// </summary>
		/// <param name="code">Code to get the RefreshToken</param>
		/// <param name="settings">OAuth2Settings to update with the information that was retrieved</param>
		private static async Task GenerateRefreshTokenAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			IDictionary<string, string> data = new Dictionary<string, string>();
			// Use the returned code to get a refresh code
			data.Add(CODE, settings.Code);
			data.Add(CLIENT_ID, settings.ClientId);
			data.Add(REDIRECT_URI, settings.RedirectUrl);
			data.Add(CLIENT_SECRET, settings.ClientSecret);
			data.Add(GRANT_TYPE, AUTHORIZATION_CODE);
			foreach (string key in settings.AdditionalAttributes.Keys) {
				data.Add(key, settings.AdditionalAttributes[key]);
			}

			dynamic refreshTokenResult;
			using (var responseMessage = await settings.TokenUrl.PostFormUrlEncodedAsync(data, token)) {
				refreshTokenResult = await responseMessage.GetAsJsonAsync();
			}

			if (refreshTokenResult.IsDefined("error")) {
				if (refreshTokenResult.IsDefined("error_description")) {
					throw new Exception(string.Format("{0} - {1}", refreshTokenResult.error, refreshTokenResult.error_description));
				} else {
					throw new Exception(refreshTokenResult.error);
				}
			} else {
				// gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
				//  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
				//	"expires_in":3920,
				//	"token_type":"Bearer",
				//	"refresh_token":"1/xEoDL4iW3cxlI7yDbSRFYNG01kVKM2C-259HOF2aQbI"
				settings.AccessToken = (string)refreshTokenResult.access_token;
				settings.RefreshToken = (string)refreshTokenResult.refresh_token;

				if (refreshTokenResult.IsDefined("expires_in")) {
					double expiresIn = refreshTokenResult.expires_in;
					settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds(expiresIn);
				}
				settings.Code = null;
			}
		}

		/// <summary>
		/// Go out and retrieve a new access token via refresh-token with the TokenUrl in the settings
		/// Will upate the access token, refresh token, expire date
		/// </summary>
		/// <param name="settings"></param>
		private static async Task GenerateAccessTokenAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			IDictionary<string, string> data = new Dictionary<string, string>();
			data.Add(REFRESH_TOKEN, settings.RefreshToken);
			data.Add(CLIENT_ID, settings.ClientId);
			data.Add(CLIENT_SECRET, settings.ClientSecret);
			data.Add(GRANT_TYPE, REFRESH_TOKEN);
			foreach (string key in settings.AdditionalAttributes.Keys) {
				data.Add(key, settings.AdditionalAttributes[key]);
			}

			dynamic accessTokenResult;
			using (var responseMessage = await settings.TokenUrl.PostFormUrlEncodedAsync(data, token)) {
				accessTokenResult = await responseMessage.GetAsJsonAsync();
			}

			if (accessTokenResult.IsDefined("error")) {
				var error = (string)accessTokenResult.error;
				if ("invalid_grant" == error) {
					// Refresh token has also expired, we need a new one!
					settings.RefreshToken = null;
					settings.AccessToken = null;
					settings.AccessTokenExpires = DateTimeOffset.MinValue;
					settings.Code = null;
					return;
				} else {
					if (accessTokenResult.IsDefined("error_description")) {
						throw new Exception(string.Format("{0} - {1}", error, accessTokenResult.error_description));
					} else {
						throw new Exception(error);
					}
				}
			} else {
				// gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
				//  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
				//	"expires_in":3920,
				//	"token_type":"Bearer"
				settings.AccessToken = (string)accessTokenResult.access_token;
				if (accessTokenResult.IsDefined("refresh_token")) {
					// Refresh the refresh token :)
					settings.RefreshToken = (string)accessTokenResult.refresh_token;
				}
				if (accessTokenResult.IsDefined("expires_in")) {
					double expiresIn = accessTokenResult.expires_in;
					settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds(expiresIn);
				}
			}
		}

		/// <summary>
		/// Authenticate by using the mode specified in the settings
		/// </summary>
		/// <param name="settings">OAuth2Settings</param>
		/// <param name="token">CancellationToken</param>
		/// <returns>false if it was canceled, true if it worked, exception if not</returns>
		private static async Task<bool> AuthenticateAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			bool completed = true;
			switch (settings.AuthorizeMode) {
				case OAuth2AuthorizeMode.LocalServer:
					completed = await AuthenticateViaLocalServerAsync(settings, token).ConfigureAwait(false);
					break;
				case OAuth2AuthorizeMode.EmbeddedBrowser:
					completed = await AuthenticateViaEmbeddedBrowserAsync(settings, token).ConfigureAwait(false);
					break;
				default:
					throw new NotImplementedException(string.Format("Authorize mode '{0}' is not 'yet' implemented.", settings.AuthorizeMode));
			}
			return completed;
		}

		/// <summary>
		/// Authenticate via an embedded browser
		/// If this works, return the code
		/// </summary>
		/// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
		/// <returns>true if completed, false if canceled</returns>
		private static async Task<bool> AuthenticateViaEmbeddedBrowserAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			if (string.IsNullOrEmpty(settings.CloudServiceName)) {
				throw new ArgumentNullException("CloudServiceName");
			}
			if (settings.BrowserSize == Size.Empty) {
				throw new ArgumentNullException("BrowserSize");
			}
			OAuthLoginForm loginForm = new OAuthLoginForm(string.Format("Authorize {0}", settings.CloudServiceName), settings.BrowserSize, settings.FormattedAuthUrl, settings.RedirectUrl);
			loginForm.ShowDialog();
			if (loginForm.IsOk) {
				string code;
				if (loginForm.CallbackParameters.TryGetValue(CODE, out code) && !string.IsNullOrEmpty(code)) {
					settings.Code = code;
					await GenerateRefreshTokenAsync(settings, token);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Authenticate via a local server by using the LocalServerCodeReceiver
		/// If this works, return the code
		/// </summary>
		/// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
		/// <returns>true if completed</returns>
		private static async Task<bool> AuthenticateViaLocalServerAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			IDictionary<string, string> result;
			using (var codeReceiver = new LocalServerCodeReceiver())
			{
				result = await codeReceiver.ReceiveCodeAsync(settings, token);
			}

			string code;
			if (result.TryGetValue(CODE, out code) && !string.IsNullOrEmpty(code)) {
				settings.Code = code;
				await GenerateRefreshTokenAsync(settings, token);
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
				} else {
					throw new Exception(error);
				}
			}
			return false;
		}

		/// <summary>
		/// Check and authenticate or refresh tokens 
		/// </summary>
		/// <param name="settings">OAuth2Settings</param>
		private static async Task CheckAndAuthenticateOrRefreshAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			// Get Refresh / Access token
			if (string.IsNullOrEmpty(settings.RefreshToken)) {
				if (!await AuthenticateAsync(settings, token).ConfigureAwait(false)) {
					throw new Exception("Authentication cancelled");
				}
			}
			if (settings.IsAccessTokenExpired) {
				await GenerateAccessTokenAsync(settings, token).ConfigureAwait(false);
				// Get Refresh / Access token
				if (string.IsNullOrEmpty(settings.RefreshToken)) {
					if (!await AuthenticateAsync(settings, token).ConfigureAwait(false)) {
						throw new Exception("Authentication cancelled");
					}
					await GenerateAccessTokenAsync(settings, token).ConfigureAwait(false);
				}
			}
			if (settings.IsAccessTokenExpired) {
				throw new Exception("Authentication failed");
			}
		}

		/// <summary>
		/// create HttpClient ready for OAuth 2 access
		/// </summary>
		/// <param name="method">HttpMethod</param>
		/// <param name="uri"></param>
		/// <param name="settings">OAuth2Settings</param>
		/// <returns>HttpClient</returns>
		public static async Task<HttpClient> CreateOAuth2HttpClientAsync(Uri uri, OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			await CheckAndAuthenticateOrRefreshAsync(settings, token).ConfigureAwait(false);

			var httpClient = uri.CreateHttpClient();
			if (!string.IsNullOrEmpty(settings.AccessToken)) {
				httpClient.SetBearer(settings.AccessToken);
			}
			return httpClient;
		}
	}
}
