// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
        public AuthenticationLevel AuthenticationLevel { get; set; }
        public X509CertificateCollection ClientCertificates { get; set; }
        public int MaxConnectionsPerServer { get; set; }
        public Uri ProxyUri { get; set; }
        public string[] ProxyBypassList { get; set; }
        public bool ProxyBypassOnLocal { get; set; }
        public ICredentials ProxyCredentials { get; set; }
        public long MaxRequestContentBufferSize { get; set; }
        public int MaxResponseHeadersLength { get; set; }
        public bool UseProxy { get; set; }
        public bool IgnoreSslCertificateErrors { get; set; }
        public bool UseDefaultCredentialsForProxy { get; set; }
        public bool UseDefaultProxy { get; set; }
        public TimeSpan ContinueTimeout { get; set; }
        public bool AllowPipelining { get; set; }

        public TokenImpersonationLevel ImpersonationLevel { get; set; }
        public RequestCacheLevel RequestCacheLevel { get; set; }
        public int ReadWriteTimeout { get; set; }

        IHttpSettings IHttpSettings.ShallowClone()
        {
            return ShallowClone() as IHttpSettings;
        }
    }
}
