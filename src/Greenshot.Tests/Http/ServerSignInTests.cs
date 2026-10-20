/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Text;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Http;
using Xunit;

namespace Greenshot.Tests.Http
{
    /// <summary>
    /// The credentials, their storage in the Windows Credential Manager and the sign-in without the window.
    /// Every test uses its own target, which is removed again.
    /// </summary>
    public class ServerSignInTests
    {
        public ServerSignInTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static string NewTarget() => $"https://greenshot-test-{Guid.NewGuid():N}.example.com";

        [Fact]
        public void ServerCredentials_UserAndPassword_Basic_TokenOnly_Bearer()
        {
            var basic = new ServerCredentials("user@example.com", "secret").CreateAuthorization();
            Assert.Equal("Basic", basic.Scheme);
            Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("user@example.com:secret")), basic.Parameter);

            var token = new ServerCredentials(" ", "pat-123");
            Assert.True(token.IsToken);
            Assert.Equal("Bearer", token.CreateAuthorization().Scheme);
            Assert.Equal("pat-123", token.CreateAuthorization().Parameter);
        }

        [Fact]
        public void ServerCredentials_ToString_NeverTheSecret()
        {
            Assert.DoesNotContain("secret", new ServerCredentials("user", "secret").ToString());
            Assert.DoesNotContain("pat-123", new ServerCredentials(null, "pat-123").ToString());
        }

        [Fact]
        public void CredentialStore_WriteReadDelete()
        {
            string target = NewTarget();
            try
            {
                Assert.Null(CredentialStore.Read(target));

                Assert.True(CredentialStore.Write(target, new ServerCredentials("user@example.com", "ätoken")));
                var stored = CredentialStore.Read(target);
                Assert.Equal("user@example.com", stored.UserName);
                Assert.Equal("ätoken", stored.Secret);

                // A token replaces the user and password
                Assert.True(CredentialStore.Write(target, new ServerCredentials(null, "pat-123")));
                stored = CredentialStore.Read(target);
                Assert.True(stored.IsToken);
                Assert.Equal("pat-123", stored.Secret);

                Assert.True(CredentialStore.Delete(target));
                Assert.Null(CredentialStore.Read(target));
                // Nothing to delete is fine too
                Assert.True(CredentialStore.Delete(target));
            }
            finally
            {
                CredentialStore.Delete(target);
            }
        }

        [Fact]
        public async Task EnsureSignedInAsync_StoredCredentials_SignsInWithoutTheWindow_SignOutRemovesThem()
        {
            string target = NewTarget();
            try
            {
                CredentialStore.Write(target, new ServerCredentials("user@example.com", "token"));
                ServerCredentials used = null;
                int changes = 0;
                var signIn = new ServerSignIn("Test", () => target, (url, credentials, _) =>
                {
                    used = credentials;
                    return Task.FromResult("Robin");
                });
                signIn.Changed += (_, _) => changes++;

                Assert.True(await signIn.EnsureSignedInAsync(TestContext.Current.CancellationToken));
                Assert.True(signIn.IsSignedIn);
                Assert.Equal("Robin", signIn.Account);
                Assert.Equal("user@example.com", used.UserName);

                // Signed in: no second check
                used = null;
                Assert.True(await signIn.EnsureSignedInAsync(TestContext.Current.CancellationToken));
                Assert.Null(used);

                // After a recheck the credentials of the session are checked again
                signIn.Recheck();
                Assert.True(await signIn.EnsureSignedInAsync(TestContext.Current.CancellationToken));
                Assert.Equal("token", used.Secret);

                signIn.SignOut();
                Assert.False(signIn.IsSignedIn);
                Assert.Null(signIn.Account);
                Assert.Null(CredentialStore.Read(target));
                Assert.Equal(3, changes);
            }
            finally
            {
                CredentialStore.Delete(target);
            }
        }

        [Fact]
        public async Task IsSignedIn_AnotherUrl_False()
        {
            string target = NewTarget();
            string url = target;
            try
            {
                CredentialStore.Write(target, new ServerCredentials("user@example.com", "token"));
                var signIn = new ServerSignIn("Test", () => url, (_, _, _) => Task.FromResult("Robin"));
                Assert.True(await signIn.EnsureSignedInAsync(TestContext.Current.CancellationToken));

                url = NewTarget();

                Assert.False(signIn.IsSignedIn);
                Assert.Null(signIn.Account);
            }
            finally
            {
                CredentialStore.Delete(target);
            }
        }

        [Fact]
        public async Task TestAsync_RejectedCredentials_SignsOutOfTheSession_KeepsTheStoredOnes()
        {
            string target = NewTarget();
            try
            {
                CredentialStore.Write(target, new ServerCredentials("user@example.com", "token"));
                bool reject = false;
                var signIn = new ServerSignIn("Test", () => target, (_, _, _) =>
                    reject ? Task.FromException<string>(new UnauthorizedAccessException("401")) : Task.FromResult("Robin"));
                Assert.True(await signIn.EnsureSignedInAsync(TestContext.Current.CancellationToken));

                reject = true;

                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => signIn.TestAsync(TestContext.Current.CancellationToken));
                Assert.False(signIn.IsSignedIn);
                Assert.NotNull(CredentialStore.Read(target));
            }
            finally
            {
                CredentialStore.Delete(target);
            }
        }
    }
}
