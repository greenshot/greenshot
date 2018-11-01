using System;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Dapplo.Config.Ini;
using Dapplo.HttpExtensions;
using Greenshot.Addons.Core;

namespace Greenshot.Addons.Config.Impl
{
    internal class HttpConfigurationImpl : IniSectionBase<IHttpConfiguration>, IHttpConfiguration
    {
        #region Implementation of IHttpSettings

        public IHttpSettings ShallowClone()
        {
            throw new NotImplementedException();
        }

        public bool AllowAutoRedirect { get; set; }
        public ICredentials Credentials { get; set; }
        public ClientCertificateOption ClientCertificateOptions { get; set; }
        public DecompressionMethods DefaultDecompressionMethods { get; set; }
        public string DefaultUserAgent { get; set; }
        public bool Expect100Continue { get; set; }
        public int MaxAutomaticRedirections { get; set; }
        public long MaxResponseContentBufferSize { get; set; }
        public bool PreAuthenticate { get; set; }
        public TimeSpan RequestTimeout { get; set; }
        public bool UseCookies { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public bool AllowPipelining { get; set; }
        public AuthenticationLevel AuthenticationLevel { get; set; }
        public X509CertificateCollection ClientCertificates { get; set; }
        public TimeSpan ContinueTimeout { get; set; }
        public TokenImpersonationLevel ImpersonationLevel { get; set; }
        public long MaxRequestContentBufferSize { get; set; }
        public int MaxResponseHeadersLength { get; set; }
        public int ReadWriteTimeout { get; set; }
        public RequestCacheLevel RequestCacheLevel { get; set; }
        public bool UseProxy { get; set; }
        public bool IgnoreSslCertificateErrors { get; set; }
        public string[] ProxyBypassList { get; set; }
        public bool ProxyBypassOnLocal { get; set; }
        public ICredentials ProxyCredentials { get; set; }
        public Uri ProxyUri { get; set; }
        public bool UseDefaultCredentialsForProxy { get; set; }
        public bool UseDefaultProxy { get; set; }

        #endregion
    }
}
