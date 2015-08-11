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
using GreenshotPlugin.Core;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace GreenshotPlugin.OAuth {
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
			foreach (var value in _requestTokenParameters) {
				parameters.Add(value);
			}
			Sign(RequestTokenMethod, RequestTokenUrl, parameters);
			string response = MakeRequest(RequestTokenMethod, RequestTokenUrl, null, parameters, null);
			if (!string.IsNullOrEmpty(response)) {
				response = Uri.UnescapeDataString(response.Replace("+", " "));
				LOG.DebugFormat("Request token response: {0}", response);
				_requestTokenResponseParameters = HttpClientHelper.ParseQueryString(response);
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
				_accessTokenResponseParameters = HttpClientHelper.ParseQueryString(response);
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
					foreach (var parameter in parametersToSign) {
						newParameters.Add(parameter);
					}
					if (additionalParameters != null) {
						foreach (var parameter in additionalParameters) {
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
							foreach (string keyToDelete in keysToDelete) {
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
			switch (SignatureType) {
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
					requestUri = requestUri.ExtendQuery(requestParameters);
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
				foreach (string key in headers.Keys) {
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
}
