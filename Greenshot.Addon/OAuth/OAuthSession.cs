/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Factory;
using Greenshot.Addon.Controls;

namespace Greenshot.Addon.OAuth
{
	/// <summary>
	/// An OAuth 1 session object
	/// </summary>
	public class OAuthSession
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(OAuthSession));
		private const string OauthVersion = "1.0";
		private const string OauthParameterPrefix = "oauth_";

		//
		// List of know and used oauth parameters' names
		//		
		private const string OauthConsumerKeyKey = "oauth_consumer_key";
		private const string OauthCallbackKey = "oauth_callback";
		private const string OauthVersionKey = "oauth_version";
		private const string OauthSignatureMethodKey = "oauth_signature_method";
		private const string OauthTimestampKey = "oauth_timestamp";
		private const string OauthNonceKey = "oauth_nonce";
		private const string OauthTokenKey = "oauth_token";
		private const string OauthVerifierKey = "oauth_verifier";
		private const string OauthTokenSecretKey = "oauth_token_secret";
		private const string OauthSignatureKey = "oauth_signature";

		private const string HmacSha1SignatureType = "HMAC-SHA1";
		private const string PlainTextSignatureType = "PLAINTEXT";
		private const string RsaSha1SignatureType = "RSA-SHA1";

		private static readonly Random RandomForNonce = new Random();

		/// <summary>
		/// Parameters of the last called getAccessToken
		/// </summary>
		public IDictionary<string, string> AccessTokenResponseParameters { get; private set; }

		/// <summary>
		/// Parameters of the last called getRequestToken
		/// </summary>
		public IDictionary<string, string> RequestTokenResponseParameters { get; private set; }

		private readonly string _consumerKey;
		private readonly string _consumerSecret;

		// default _browser size

		#region PublicProperties

		public HttpMethod RequestTokenMethod
		{
			get;
			set;
		}

		public HttpMethod AccessTokenMethod
		{
			get;
			set;
		}

		public Uri RequestTokenUrl
		{
			get;
			set;
		}

		public Uri AuthorizeUrl
		{
			get;
			set;
		}

		public Uri AccessTokenUrl
		{
			get;
			set;
		}

		public string Token
		{
			get;
			set;
		}

		public string TokenSecret
		{
			get;
			set;
		}

		public string Verifier
		{
			get;
			set;
		}

		public OAuthSignatureTypes SignatureType
		{
			get;
			set;
		}

		public string CallbackUrl
		{
			get;
			set;
		} = "http://getgreenshot.org";

		public bool CheckVerifier
		{
			get;
			set;
		} = true;

		public Size BrowserSize
		{
			get;
			set;
		} = new Size(864, 587);

		public string LoginTitle
		{
			get;
			set;
		} = "Authorize Greenshot access";

		public bool AutoLogin
		{
			get;
			set;
		}

		#endregion

		/// <summary>
		/// Create an OAuthSession with the consumerKey / consumerSecret
		/// </summary>
		/// <param name="consumerKey">"Public" key for the encoding. When using RSASHA1 this is the path to the private key file</param>
		/// <param name="consumerSecret">"Private" key for the encoding. when usin RSASHA1 this is the password for the private key file</param>
		public OAuthSession(string consumerKey, string consumerSecret)
		{
			_consumerKey = consumerKey;
			_consumerSecret = consumerSecret;
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
		private static string ComputeHash(HashAlgorithm hashAlgorithm, string data)
		{
			if (hashAlgorithm == null)
			{
				throw new ArgumentNullException(nameof(hashAlgorithm));
			}

			if (string.IsNullOrEmpty(data))
			{
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
		private static string GenerateNormalizedParametersString(IDictionary<string, object> queryParameters)
		{
			if (queryParameters == null || queryParameters.Count == 0)
			{
				return string.Empty;
			}

			queryParameters = new SortedDictionary<string, object>(queryParameters);

			var sb = new StringBuilder();
			foreach (var key in queryParameters.Keys)
			{
				if (queryParameters[key] is string)
				{
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", key, Uri.EscapeDataString($"{queryParameters[key]}"));
				}
			}
			sb.Remove(sb.Length - 1, 1);

			return sb.ToString();
		}

		/// <summary>
		/// Generate the timestamp for the signature		
		/// </summary>
		/// <returns></returns>
		public static string GenerateTimeStamp()
		{
			// Default implementation of UNIX time of the current UTC time
			TimeSpan timespan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return Convert.ToInt64(timespan.TotalSeconds).ToString();
		}

		/// <summary>
		/// Generate a nonce
		/// </summary>
		/// <returns></returns>
		public static string GenerateNonce()
		{
			// Just a simple implementation of a random number between 123400 and 9999999
			return RandomForNonce.Next(123400, 9999999).ToString();
		}

		/// <summary>
		/// Get the request token using the consumer key and secret.  Also initializes tokensecret
		/// </summary>
		private async Task GetRequestTokenAsync()
		{
			IDictionary<string, object> parameters = new Dictionary<string, object>();
			Sign(RequestTokenMethod, RequestTokenUrl, parameters);
			string response = await MakeRequest(RequestTokenMethod, RequestTokenUrl, null, parameters, null).ConfigureAwait(false);
			if (!string.IsNullOrEmpty(response))
			{
				var uriBuilder = new UriBuilder("http://getgreenshot.org")
				{
					Query = Uri.UnescapeDataString(response.Replace("+", " "))
				};
				Log.Debug("Request token response: {0}", response);
				RequestTokenResponseParameters = uriBuilder.Uri.QueryToDictionary();
				string value;
				if (RequestTokenResponseParameters.TryGetValue(OauthTokenKey, out value))
				{
					Token = value;
					TokenSecret = RequestTokenResponseParameters[OauthTokenSecretKey];
				}
			}
		}

		/// <summary>
		/// Authorize the token by showing the dialog
		/// </summary>
		/// <returns>The request token.</returns>
		private string GetAuthorizeToken()
		{
			if (string.IsNullOrEmpty(Token))
			{
				throw new Exception("The request token is not set");
			}
			Log.Debug("Opening AuthorizationLink: {0}", AuthorizationLink);
			var oAuthLoginForm = new OAuthLoginForm(LoginTitle, BrowserSize, AuthorizationLink, CallbackUrl);
			oAuthLoginForm.ShowDialog();
			if (oAuthLoginForm.IsOk)
			{
				if (oAuthLoginForm.CallbackParameters != null)
				{
					string tokenValue;
					if (oAuthLoginForm.CallbackParameters.TryGetValue(OauthTokenKey, out tokenValue))
					{
						Token = tokenValue;
					}
					string verifierValue;
					if (oAuthLoginForm.CallbackParameters.TryGetValue(OauthVerifierKey, out verifierValue))
					{
						Verifier = verifierValue;
					}
				}
			}
			if (CheckVerifier)
			{
				if (!string.IsNullOrEmpty(Verifier))
				{
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
		private async Task<string> GetAccessTokenAsync()
		{
			if (string.IsNullOrEmpty(Token) || (CheckVerifier && string.IsNullOrEmpty(Verifier)))
			{
				throw new Exception("The request token and verifier were not set");
			}

			IDictionary<string, object> parameters = new Dictionary<string, object>();
			Sign(AccessTokenMethod, AccessTokenUrl, parameters);
			string response = await MakeRequest(AccessTokenMethod, AccessTokenUrl, null, parameters, null).ConfigureAwait(false);
			if (!string.IsNullOrEmpty(response))
			{
				var uriBuilder = new UriBuilder("http://getgreenshot.org")
				{
					Query = Uri.UnescapeDataString(response.Replace("+", " "))
				};
				AccessTokenResponseParameters = uriBuilder.Uri.QueryToDictionary();
				string tokenValue;
				if (AccessTokenResponseParameters.TryGetValue(OauthTokenKey, out tokenValue) && tokenValue != null)
				{
					Token = tokenValue;
				}
				string secretValue;
				if (AccessTokenResponseParameters.TryGetValue(OauthTokenSecretKey, out secretValue) && secretValue != null)
				{
					TokenSecret = secretValue;
				}
			}

			return Token;
		}

		/// <summary>
		/// Helper method to call a method (function) on a STA Thread.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="func"></param>
		/// <returns>Task</returns>
		private static Task<T> StartStaTask<T>(Func<T> func)
		{
			var tcs = new TaskCompletionSource<T>();
			Thread thread = new Thread(() =>
			{
				try
				{
					tcs.SetResult(func());
				}
				catch (Exception e)
				{
					tcs.SetException(e);
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			return tcs.Task;
		}

		/// <summary>
		/// This method goes through the whole authorize process, including a Authorization window.
		/// </summary>
		/// <returns>true if the process is completed</returns>
		public async Task<bool> AuthorizeAsync()
		{
			Token = null;
			TokenSecret = null;
			Verifier = null;
			Log.Debug("Creating Token");
			try
			{
				await GetRequestTokenAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Retrieving an Oauth request token failed");
				throw new NotSupportedException("Service is not available: " + ex.Message);
			}
			// Run the WebBrowser on a STA thread!
			var token = await StartStaTask(GetAuthorizeToken);
			if (string.IsNullOrEmpty(token))
			{
				Log.Debug("User didn't authenticate!");
				return false;
			}
			try
			{
				await Task.Delay(1000).ConfigureAwait(false);
				return await GetAccessTokenAsync() != null;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Retrieving an OAuth access token failed");
				throw;
			}
		}

		/// <summary>
		/// Get the link to the authorization page for this application.
		/// </summary>
		/// <returns>The url with a valid request token, or a null string.</returns>
		private Uri AuthorizationLink
		{
			get
			{
				return new Uri(AuthorizeUrl + "?" + OauthTokenKey + "=" + Token + "&" + OauthCallbackKey + "=" + Uri.EscapeDataString(CallbackUrl));
			}
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
		/// <param name="content">Data to post (HttpContent)</param>
		/// <returns>The web server response.</returns>
		public async Task<string> MakeOAuthRequest(HttpMethod method, Uri signUri, Uri requestUri, IDictionary<string, string> headers = null, IDictionary<string, object> parametersToSign = null, IDictionary<string, object> additionalParameters = null, HttpContent content = null)
		{
			if (parametersToSign == null)
			{
				parametersToSign = new Dictionary<string, object>();
			}
			int retries = 2;
			Exception lastException = null;
			while (retries-- > 0)
			{
				// If we are not trying to get a Authorization or Accestoken, and we don't have a token, create one
				if (string.IsNullOrEmpty(Token))
				{
					if (!AutoLogin || !await AuthorizeAsync().ConfigureAwait(false))
					{
						throw new Exception("Not authorized");
					}
				}
				try
				{
					Sign(method, signUri, parametersToSign);

					// Join all parameters
					IDictionary<string, object> newParameters = new Dictionary<string, object>(parametersToSign);
					if (additionalParameters != null)
					{
						foreach (var parameter in additionalParameters)
						{
							newParameters.Add(parameter);
						}
					}
					return await MakeRequest(method, requestUri, headers, newParameters, content).ConfigureAwait(false);
				}
				catch (WebException wEx)
				{
					lastException = wEx;
					var response = wEx.Response as HttpWebResponse;
					if (response != null && response.StatusCode == HttpStatusCode.Unauthorized)
					{
						Token = null;
						TokenSecret = null;
						// Remove oauth keys, so they aren't added double
						var keysToDelete = parametersToSign.Keys.Where(parameterKey => parameterKey.StartsWith(OauthParameterPrefix)).ToList();
						foreach (string keyToDelete in keysToDelete)
						{
							parametersToSign.Remove(keyToDelete);
						}
						continue;
					}
					throw;
				}
			}
			if (lastException != null)
			{
				throw lastException;
			}
			throw new Exception("Not authorized");
		}

		/// <summary>
		/// OAuth sign the parameters, meaning all oauth parameters are added to the supplied dictionary.
		/// And additionally a signature is added.
		/// </summary>
		/// <param name="method">Method (POST,PUT,GET)</param>
		/// <param name="requestUri">Url to call</param>
		/// <param name="parameters">IDictionar&lt;string, string&gt;</param>
		private void Sign(HttpMethod method, Uri requestUri, IDictionary<string, object> parameters)
		{
			if (parameters == null)
			{
				throw new ArgumentNullException(nameof(parameters));
			}
			// Build the signature base
			var signatureBase = new StringBuilder();

			// Add Method to signature base
			signatureBase.Append(method).Append("&");

			// Add normalized URL (this automatically happens when using "AbsoluteUri")
			signatureBase.Append(Uri.EscapeDataString(requestUri.AbsoluteUri)).Append("&");

			// Add normalized parameters
			parameters.Add(OauthVersionKey, OauthVersion);
			parameters.Add(OauthNonceKey, GenerateNonce());
			parameters.Add(OauthTimestampKey, GenerateTimeStamp());
			switch (SignatureType)
			{
				case OAuthSignatureTypes.RSASHA1:
					parameters.Add(OauthSignatureMethodKey, RsaSha1SignatureType);
					break;
				case OAuthSignatureTypes.PLAINTEXT:
					parameters.Add(OauthSignatureMethodKey, PlainTextSignatureType);
					break;
				default:
					parameters.Add(OauthSignatureMethodKey, HmacSha1SignatureType);
					break;
			}
			parameters.Add(OauthConsumerKeyKey, _consumerKey);
			if (CallbackUrl != null && RequestTokenUrl != null && requestUri.Equals(RequestTokenUrl))
			{
				parameters.Add(OauthCallbackKey, CallbackUrl);
			}
			if (!string.IsNullOrEmpty(Verifier))
			{
				parameters.Add(OauthVerifierKey, Verifier);
			}
			if (!string.IsNullOrEmpty(Token))
			{
				parameters.Add(OauthTokenKey, Token);
			}
			signatureBase.Append(Uri.EscapeDataString(GenerateNormalizedParametersString(parameters)));
			Log.Debug("Signature base: {0}", signatureBase);
			string key = string.Format(CultureInfo.InvariantCulture, "{0}&{1}", Uri.EscapeDataString(_consumerSecret), string.IsNullOrEmpty(TokenSecret) ? string.Empty : Uri.EscapeDataString(TokenSecret));
			switch (SignatureType)
			{
				case OAuthSignatureTypes.RSASHA1:
					// Code comes from here: http://www.dotnetfunda.com/articles/article1932-rest-service-call-using-oauth-10-authorization-with-rsa-sha1.aspx
					// Read the .P12 file to read Private/Public key Certificate
					string certFilePath = _consumerKey; // The .P12 certificate file path Example: "C:/mycertificate/MCOpenAPI.p12
					string password = _consumerSecret; // password to read certificate .p12 file
					// Read the Certification from .P12 file.
					var cert = new X509Certificate2(certFilePath, password);
					// Retrieve the Private key from Certificate.
					var rsaCrypt = (RSACryptoServiceProvider) cert.PrivateKey;
					// Create a RSA-SHA1 Hash object
					using (var shaHashObject = new SHA1Managed())
					{
						// Create Byte Array of Signature base string
						byte[] data = Encoding.ASCII.GetBytes(signatureBase.ToString());
						// Create Hashmap of Signature base string
						byte[] hash = shaHashObject.ComputeHash(data);
						// Create Sign Hash of base string
						// NOTE - 'SignHash' gives correct data. Don't use SignData method
						byte[] rsaSignature = rsaCrypt.SignHash(hash, CryptoConfig.MapNameToOID("SHA1"));
						// Convert to Base64 string
						string base64String = Convert.ToBase64String(rsaSignature);
						// Return the Encoded UTF8 string
						parameters.Add(OauthSignatureKey, Uri.EscapeDataString(base64String));
					}
					break;
				case OAuthSignatureTypes.PLAINTEXT:
					parameters.Add(OauthSignatureKey, key);
					break;
				default:
					// Generate Signature and add it to the parameters
					var hmacsha1 = new HMACSHA1 {Key = Encoding.UTF8.GetBytes(key)};
					string signature = ComputeHash(hmacsha1, signatureBase.ToString());
					parameters.Add(OauthSignatureKey, signature);
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
		/// <param name="content">HttpContent</param>
		/// <param name="token">CancellationToken</param>
		/// <returns>Response from server</returns>
		private async Task<string> MakeRequest(HttpMethod method, Uri requestUri, IDictionary<string, string> headers, IDictionary<string, object> parameters, HttpContent content, CancellationToken token = default(CancellationToken))
		{
			if (parameters == null)
			{
				throw new ArgumentNullException(nameof(parameters));
			}
			// Add oAuth values as HTTP headers
			var authHeader = new StringBuilder();
			IDictionary<string, object> requestParameters = new SortedDictionary<string, object>();
			foreach (string parameterKey in parameters.Keys)
			{
				if (parameterKey.StartsWith(OauthParameterPrefix))
				{
					authHeader.AppendFormat(CultureInfo.InvariantCulture, "{0}=\"{1}\", ", parameterKey, Uri.EscapeDataString(string.Format("{0}", parameters[parameterKey])));
				}
				else if (!requestParameters.ContainsKey(parameterKey))
				{
					requestParameters.Add(parameterKey, parameters[parameterKey]);
				}
			}
			// Remove trailing comma and space and add it to the headers
			if (authHeader.Length > 0)
			{
				authHeader.Remove(authHeader.Length - 2, 2);
			}

			if (HttpMethod.Get == method || content != null)
			{
				if (requestParameters.Count > 0)
				{
					// Add the parameters to the request
					requestUri = requestUri.ExtendQuery(requestParameters);
				}
			}
			string responseData;
			using (var httpClient = HttpClientFactory.Create())
			{
				httpClient.DefaultRequestHeaders.ExpectContinue = false;
				// TODO: Auth headers could be passed/stored different, maybe only one httpclient pro session?
				httpClient.SetAuthorization("OAuth", authHeader.ToString());
				httpClient.AddDefaultRequestHeader("User-Agent", "Greenshot");
				if (headers != null)
				{
					foreach (string key in headers.Keys)
					{
						httpClient.AddDefaultRequestHeader(key, headers[key]);
					}
				}

				HttpResponseMessage responseMessage;
				if (content != null)
				{
					responseMessage = await httpClient.PostAsync(requestUri, content, token).ConfigureAwait(false);
				}
				else if ((HttpMethod.Post == method || HttpMethod.Put == method) && requestParameters.Count > 0)
				{
					var multipartFormDataContent = new MultipartFormDataContent();
					foreach (var key in requestParameters.Keys)
					{
						var requestObject = requestParameters[key];
						var formattedKey = $"\"{key}\"";
						if (requestObject is HttpContent)
						{
							multipartFormDataContent.Add(requestObject as HttpContent, formattedKey);
						}
						else
						{
							multipartFormDataContent.Add(new StringContent(requestObject as string), formattedKey);
						}
					}
					responseMessage = await httpClient.PostAsync(requestUri, multipartFormDataContent, token).ConfigureAwait(false);
				}
				else
				{
					if (HttpMethod.Post == method)
					{
						responseMessage = await httpClient.PostAsync(requestUri, null, token).ConfigureAwait(false);
					}
					else
					{
						responseMessage = await httpClient.GetAsync(requestUri, token).ConfigureAwait(false);
					}
				}

				try
				{
					responseData = await responseMessage.GetAsAsync<string>(token: token).ConfigureAwait(false);
					Log.Debug("Response: {0}", responseData);
				}
				catch (Exception ex)
				{
					Log.Error("Couldn't retrieve response: ", ex);
					throw;
				}
			}

			return responseData;
		}
	}
}