/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class NetworkHelperSecurityTests
    {
        private static readonly X509Certificate2 SampleCert;

        static NetworkHelperSecurityTests()
        {
            using (var rsa = RSA.Create(2048))
            {
                var req = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                SampleCert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(10));
            }
        }

        private readonly ICoreConfiguration _config;

        public NetworkHelperSecurityTests()
        {
            TestEnvironment.EnsureInitialized();
            _config = IniConfigRegistry.GetSection<ICoreConfiguration>();
        }

        [Fact]
        public void ValidateServerCertificate_NoError_ReturnsTrue()
        {
            bool result = NetworkHelper.ValidateServerCertificate(
                "https://api.imgur.com",
                SampleCert,
                null,
                SslPolicyErrors.None);

            Assert.True(result);
        }

        [Fact]
        public void ValidateServerCertificate_NullCertificate_ReturnsFalse()
        {
            bool result = NetworkHelper.ValidateServerCertificate(
                "https://api.imgur.com",
                null,
                null,
                SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.False(result);
        }

        [Fact]
        public void ValidateServerCertificate_UntrustedCert_NoExceptionsConfigured_ReturnsFalse()
        {
            _config.AllowedUntrustedCertificateHosts = new List<string>();
            _config.AllowedCertificateThumbprints = new List<string>();

            bool result = NetworkHelper.ValidateServerCertificate(
                "https://api.imgur.com",
                SampleCert,
                null,
                SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.False(result);
        }

        [Fact]
        public void ValidateServerCertificate_UntrustedCert_MatchingHostException_ReturnsTrue()
        {
            _config.AllowedUntrustedCertificateHosts = new List<string> { "jira.mycompany.local" };
            _config.AllowedCertificateThumbprints = new List<string>();

            bool result = NetworkHelper.ValidateServerCertificate(
                "https://jira.mycompany.local/rest/api",
                SampleCert,
                null,
                SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.True(result);
        }

        [Fact]
        public void ValidateServerCertificate_UntrustedCert_NonMatchingHostException_ReturnsFalse()
        {
            _config.AllowedUntrustedCertificateHosts = new List<string> { "jira.mycompany.local" };
            _config.AllowedCertificateThumbprints = new List<string>();

            bool result = NetworkHelper.ValidateServerCertificate(
                "https://api.imgur.com/oauth2/token",
                SampleCert,
                null,
                SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.False(result);
        }

        [Fact]
        public void ValidateServerCertificate_UntrustedCert_WildcardHostMatch_ReturnsTrue()
        {
            _config.AllowedUntrustedCertificateHosts = new List<string> { "*.corp.internal" };
            _config.AllowedCertificateThumbprints = new List<string>();

            bool result = NetworkHelper.ValidateServerCertificate(
                "https://confluence.corp.internal:8443",
                SampleCert,
                null,
                SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.True(result);
        }

        [Fact]
        public void ValidateServerCertificate_UntrustedCert_MatchingThumbprint_ReturnsTrue()
        {
            string thumbprint = SampleCert.Thumbprint;

            _config.AllowedUntrustedCertificateHosts = new List<string>();
            _config.AllowedCertificateThumbprints = new List<string> { thumbprint };

            bool result = NetworkHelper.ValidateServerCertificate(
                "https://untrusted-host.example.com",
                SampleCert,
                null,
                SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.True(result);
        }

        [Fact]
        public void ValidateServerCertificate_UntrustedCert_NonMatchingThumbprint_ReturnsFalse()
        {
            _config.AllowedUntrustedCertificateHosts = new List<string>();
            _config.AllowedCertificateThumbprints = new List<string> { "00112233445566778899AABBCCDDEEFF00112233" };

            bool result = NetworkHelper.ValidateServerCertificate(
                "https://untrusted-host.example.com",
                SampleCert,
                null,
                SslPolicyErrors.RemoteCertificateChainErrors);

            Assert.False(result);
        }

        [Theory]
        [InlineData("jira.corp.internal", "jira.corp.internal", true)]
        [InlineData("JIRA.CORP.INTERNAL", "jira.corp.internal", true)]
        [InlineData("jira.corp.internal:8443", "jira.corp.internal", true)]
        [InlineData("jira.corp.internal", "jira.corp.internal:8443", true)]
        [InlineData("jira.corp.internal", "*.corp.internal", true)]
        [InlineData("sub.jira.corp.internal", "*.corp.internal", true)]
        [InlineData("jira.other.internal", "*.corp.internal", false)]
        [InlineData("api.imgur.com", "jira.corp.internal", false)]
        [InlineData("", "jira.corp.internal", false)]
        [InlineData("jira.corp.internal", "", false)]
        [InlineData(null, "jira.corp.internal", false)]
        [InlineData("jira.corp.internal", null, false)]
        public void IsHostMatch_HandlesPatternsCorrectly(string host, string pattern, bool expected)
        {
            bool actual = NetworkHelper.IsHostMatch(host, pattern);
            Assert.Equal(expected, actual);
        }
    }
}
