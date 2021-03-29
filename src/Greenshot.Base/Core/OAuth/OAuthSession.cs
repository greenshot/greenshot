/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Greenshot.Base.Controls;
using log4net;

namespace Greenshot.Base.Core.OAuth
{
    /// <summary>
    /// An OAuth 1 session object
    /// </summary>
    public class OAuthSession
    {
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

        public HTTPMethod RequestTokenMethod { get; set; }
        public HTTPMethod AccessTokenMethod { get; set; }
        public string RequestTokenUrl { get; set; }
        public string AuthorizeUrl { get; set; }
        public string AccessTokenUrl { get; set; }
        public string Token { get; set; }
        public string TokenSecret { get; set; }
        public string Verifier { get; set; }
        public OAuthSignatureTypes SignatureType { get; set; }

        public bool UseMultipartFormData { get; set; }

        public string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }

        public string CallbackUrl { get; set; } = "https://getgreenshot.org";

        public bool CheckVerifier { get; set; } = true;

        public Size BrowserSize { get; set; } = new Size(864, 587);

        public string LoginTitle { get; set; } = "Authorize Greenshot access";

        public bool UseHttpHeadersForAuthorization { get; set; } = true;

        public bool AutoLogin { get; set; }

        /// <summary>
        /// Create an OAuthSession with the consumerKey / consumerSecret
        /// </summary>
        /// <param name="consumerKey">"Public" key for the encoding. When using RSASHA1 this is the path to the private key file</param>
        /// <param name="consumerSecret">"Private" key for the encoding. when usin RSASHA1 this is the password for the private key file</param>
        public OAuthSession(string consumerKey, string consumerSecret)
        {
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

            StringBuilder sb = new StringBuilder();
            foreach (string key in queryParameters.Keys)
            {
                if (queryParameters[key] is string)
                {
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
        public static string UrlEncode3986(string value)
        {
            StringBuilder result = new StringBuilder();

            foreach (char symbol in value)
            {
                if (UnreservedChars.IndexOf(symbol) != -1)
                {
                    result.Append(symbol);
                }
                else
                {
                    byte[] utf8Bytes = Encoding.UTF8.GetBytes(symbol.ToString());
                    foreach (byte utf8Byte in utf8Bytes)
                    {
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
        public static string GenerateTimeStamp()
        {
            // Default implementation of UNIX time of the current UTC time
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        /// <summary>
        /// Generate a nonce
        /// </summary>
        /// <returns></returns>
        public static string GenerateNonce()
        {
            // Just a simple implementation of a random number between 123400 and 9999999
            return random.Next(123400, 9999999).ToString();
        }

        /// <summary>
        /// Get the request token using the consumer key and secret.  Also initializes tokensecret
        /// </summary>
        /// <returns>response, this doesn't need to be used!!</returns>
        private string GetRequestToken()
        {
            IDictionary<string, object> parameters = new Dictionary<string, object>();
            foreach (var value in RequestTokenParameters)
            {
                parameters.Add(value);
            }

            Sign(RequestTokenMethod, RequestTokenUrl, parameters);
            string response = MakeRequest(RequestTokenMethod, RequestTokenUrl, null, parameters, null);
            if (!string.IsNullOrEmpty(response))
            {
                response = NetworkHelper.UrlDecode(response);
                Log.DebugFormat("Request token response: {0}", response);
                _requestTokenResponseParameters = NetworkHelper.ParseQueryString(response);
                if (_requestTokenResponseParameters.TryGetValue(OAUTH_TOKEN_KEY, out var value))
                {
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
        private string GetAuthorizeToken(string requestTokenResponse)
        {
            if (string.IsNullOrEmpty(Token))
            {
                Exception e = new Exception("The request token is not set, service responded with: " + requestTokenResponse);
                throw e;
            }

            Log.DebugFormat("Opening AuthorizationLink: {0}", AuthorizationLink);
            OAuthLoginForm oAuthLoginForm = new OAuthLoginForm(LoginTitle, BrowserSize, AuthorizationLink, CallbackUrl);
            oAuthLoginForm.ShowDialog();
            if (oAuthLoginForm.IsOk)
            {
                if (oAuthLoginForm.CallbackParameters != null)
                {
                    if (oAuthLoginForm.CallbackParameters.TryGetValue(OAUTH_TOKEN_KEY, out var tokenValue))
                    {
                        Token = tokenValue;
                    }

                    if (oAuthLoginForm.CallbackParameters.TryGetValue(OAUTH_VERIFIER_KEY, out var verifierValue))
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
        private string GetAccessToken()
        {
            if (string.IsNullOrEmpty(Token) || (CheckVerifier && string.IsNullOrEmpty(Verifier)))
            {
                Exception e = new Exception("The request token and verifier were not set");
                throw e;
            }

            IDictionary<string, object> parameters = new Dictionary<string, object>();
            Sign(AccessTokenMethod, AccessTokenUrl, parameters);
            string response = MakeRequest(AccessTokenMethod, AccessTokenUrl, null, parameters, null);
            if (!string.IsNullOrEmpty(response))
            {
                response = NetworkHelper.UrlDecode(response);
                Log.DebugFormat("Access token response: {0}", response);
                AccessTokenResponseParameters = NetworkHelper.ParseQueryString(response);
                if (AccessTokenResponseParameters.TryGetValue(OAUTH_TOKEN_KEY, out var tokenValue) && tokenValue != null)
                {
                    Token = tokenValue;
                }

                if (AccessTokenResponseParameters.TryGetValue(OAUTH_TOKEN_SECRET_KEY, out var secretValue) && secretValue != null)
                {
                    TokenSecret = secretValue;
                }
            }

            return Token;
        }

        /// <summary>
        /// This method goes through the whole authorize process, including a Authorization window.
        /// </summary>
        /// <returns>true if the process is completed</returns>
        public bool Authorize()
        {
            Token = null;
            TokenSecret = null;
            Verifier = null;
            Log.Debug("Creating Token");
            string requestTokenResponse;
            try
            {
                requestTokenResponse = GetRequestToken();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw new NotSupportedException("Service is not available: " + ex.Message);
            }

            if (string.IsNullOrEmpty(GetAuthorizeToken(requestTokenResponse)))
            {
                Log.Debug("User didn't authenticate!");
                return false;
            }

            try
            {
                Thread.Sleep(1000);
                return GetAccessToken() != null;
            }
            catch (Exception ex)
            {
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
        public string MakeOAuthRequest(HTTPMethod method, string requestUrl, IDictionary<string, object> parametersToSign, IDictionary<string, object> additionalParameters,
            IBinaryContainer postData)
        {
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
        public string MakeOAuthRequest(HTTPMethod method, string requestUrl, IDictionary<string, string> headers, IDictionary<string, object> parametersToSign,
            IDictionary<string, object> additionalParameters, IBinaryContainer postData)
        {
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
        public string MakeOAuthRequest(HTTPMethod method, string signUrl, string requestUrl, IDictionary<string, object> parametersToSign,
            IDictionary<string, object> additionalParameters, IBinaryContainer postData)
        {
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
        public string MakeOAuthRequest(HTTPMethod method, string signUrl, string requestUrl, IDictionary<string, string> headers, IDictionary<string, object> parametersToSign,
            IDictionary<string, object> additionalParameters, IBinaryContainer postData)
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
                    if (!AutoLogin || !Authorize())
                    {
                        throw new Exception("Not authorized");
                    }
                }

                try
                {
                    Sign(method, signUrl, parametersToSign);

                    // Join all parameters
                    IDictionary<string, object> newParameters = new Dictionary<string, object>();
                    foreach (var parameter in parametersToSign)
                    {
                        newParameters.Add(parameter);
                    }

                    if (additionalParameters != null)
                    {
                        foreach (var parameter in additionalParameters)
                        {
                            newParameters.Add(parameter);
                        }
                    }

                    return MakeRequest(method, requestUrl, headers, newParameters, postData);
                }
                catch (UnauthorizedAccessException uaEx)
                {
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
        /// <param name="requestUrl">Url to call</param>
        /// <param name="parameters">IDictionary of string and string</param>
        private void Sign(HTTPMethod method, string requestUrl, IDictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            // Build the signature base
            StringBuilder signatureBase = new StringBuilder();

            // Add Method to signature base
            signatureBase.Append(method).Append("&");

            // Add normalized URL
            Uri url = new Uri(requestUrl);
            string normalizedUrl = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", url.Scheme, url.Host);
            if (!((url.Scheme == "http" && url.Port == 80) || (url.Scheme == "https" && url.Port == 443)))
            {
                normalizedUrl += ":" + url.Port;
            }

            normalizedUrl += url.AbsolutePath;
            signatureBase.Append(UrlEncode3986(normalizedUrl)).Append("&");

            // Add normalized parameters
            parameters.Add(OAUTH_VERSION_KEY, OAUTH_VERSION);
            parameters.Add(OAUTH_NONCE_KEY, GenerateNonce());
            parameters.Add(OAUTH_TIMESTAMP_KEY, GenerateTimeStamp());
            switch (SignatureType)
            {
                case OAuthSignatureTypes.PLAINTEXT:
                    parameters.Add(OAUTH_SIGNATURE_METHOD_KEY, PlainTextSignatureType);
                    break;
                default:
                    parameters.Add(OAUTH_SIGNATURE_METHOD_KEY, HMACSHA1SignatureType);
                    break;
            }

            parameters.Add(OAUTH_CONSUMER_KEY_KEY, _consumerKey);
            if (CallbackUrl != null && RequestTokenUrl != null && requestUrl.StartsWith(RequestTokenUrl))
            {
                parameters.Add(OAUTH_CALLBACK_KEY, CallbackUrl);
            }

            if (!string.IsNullOrEmpty(Verifier))
            {
                parameters.Add(OAUTH_VERIFIER_KEY, Verifier);
            }

            if (!string.IsNullOrEmpty(Token))
            {
                parameters.Add(OAUTH_TOKEN_KEY, Token);
            }

            signatureBase.Append(UrlEncode3986(GenerateNormalizedParametersString(parameters)));
            Log.DebugFormat("Signature base: {0}", signatureBase);
            string key = string.Format(CultureInfo.InvariantCulture, "{0}&{1}", UrlEncode3986(_consumerSecret),
                string.IsNullOrEmpty(TokenSecret) ? string.Empty : UrlEncode3986(TokenSecret));
            switch (SignatureType)
            {
                case OAuthSignatureTypes.PLAINTEXT:
                    parameters.Add(OAUTH_SIGNATURE_KEY, key);
                    break;
                default:
                    // Generate Signature and add it to the parameters
                    HMACSHA1 hmacsha1 = new HMACSHA1
                    {
                        Key = Encoding.UTF8.GetBytes(key)
                    };
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
        private string MakeRequest(HTTPMethod method, string requestUrl, IDictionary<string, string> headers, IDictionary<string, object> parameters, IBinaryContainer postData)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            IDictionary<string, object> requestParameters;
            // Add oAuth values as HTTP headers, if this is allowed
            StringBuilder authHeader = null;
            if (UseHttpHeadersForAuthorization)
            {
                authHeader = new StringBuilder();
                requestParameters = new Dictionary<string, object>();
                foreach (string parameterKey in parameters.Keys)
                {
                    if (parameterKey.StartsWith(OAUTH_PARAMETER_PREFIX))
                    {
                        authHeader.AppendFormat(CultureInfo.InvariantCulture, "{0}=\"{1}\", ", parameterKey, UrlEncode3986($"{parameters[parameterKey]}"));
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
            }
            else
            {
                requestParameters = parameters;
            }

            if (HTTPMethod.GET == method || postData != null)
            {
                if (requestParameters.Count > 0)
                {
                    // Add the parameters to the request
                    requestUrl += "?" + NetworkHelper.GenerateQueryParameters(requestParameters);
                }
            }

            // Create webrequest
            HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(requestUrl, method);
            webRequest.ServicePoint.Expect100Continue = false;
            webRequest.UserAgent = _userAgent;

            if (UseHttpHeadersForAuthorization && authHeader != null)
            {
                Log.DebugFormat("Authorization: OAuth {0}", authHeader);
                webRequest.Headers.Add("Authorization: OAuth " + authHeader);
            }

            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    webRequest.Headers.Add(key, headers[key]);
                }
            }

            if ((HTTPMethod.POST == method || HTTPMethod.PUT == method) && postData == null && requestParameters.Count > 0)
            {
                if (UseMultipartFormData)
                {
                    NetworkHelper.WriteMultipartFormData(webRequest, requestParameters);
                }
                else
                {
                    StringBuilder form = new StringBuilder();
                    foreach (string parameterKey in requestParameters.Keys)
                    {
                        var binaryParameter = parameters[parameterKey] as IBinaryContainer;
                        form.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", UrlEncode3986(parameterKey),
                            binaryParameter != null ? UrlEncode3986(binaryParameter.ToBase64String(Base64FormattingOptions.None)) : UrlEncode3986($"{parameters[parameterKey]}"));
                    }

                    // Remove trailing &
                    if (form.Length > 0)
                    {
                        form.Remove(form.Length - 1, 1);
                    }

                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    byte[] data = Encoding.UTF8.GetBytes(form.ToString());
                    using var requestStream = webRequest.GetRequestStream();
                    requestStream.Write(data, 0, data.Length);
                }
            }
            else if (postData != null)
            {
                postData.Upload(webRequest);
            }
            else
            {
                webRequest.ContentLength = 0;
            }

            string responseData;
            try
            {
                responseData = NetworkHelper.GetResponseAsString(webRequest);
                Log.DebugFormat("Response: {0}", responseData);
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't retrieve response: ", ex);
                throw;
            }

            return responseData;
        }
    }
}