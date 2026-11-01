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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Recipes;
using Greenshot.Recipes.Approval;
using Greenshot.Recipes.Catalog;
using Greenshot.Recipes.Pipeline;
using Greenshot.Tests.Http;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// The recipe catalog: only a recipe in a correctly signed index, with the hash the index names, reaches the approval window,
    /// and only what the user approves is saved.
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class RecipeCatalogTests
    {
        /// <summary>
        /// Made with openssl: <c>openssl ecparam -name prime256v1 -genkey</c>, the data signed with <c>openssl dgst -sha256 -sign</c>
        /// </summary>
        private const string OpensslPublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAETR1c+E6Gx2yXbRneFw5F+Ea7AQSTbUkLXIGQxKVciWfvICZysNE/N4Bed9fh2DvXIGlDVJjvZE6aaO3e1PS8Mw==";
        private const string OpensslData = "{\"indexVersion\":1,\"recipes\":[]}\n";
        private const string OpensslSignature = "MEUCIQCQcSZc4G47fUZPBXKowxzlpNYejTp+ZF7j0fEwQsGkWQIgTEfp7/8mN0QHKJmOUCdYCrLkvBJ9zU9jDJP9NCB5xsU=";

        public RecipeCatalogTests()
        {
            TestEnvironment.EnsureInitialized();
            _ = CapturePipeline.Instance;
        }

        [Fact]
        public void Signature_FromOpenssl_IsVerified()
        {
            byte[] data = Encoding.UTF8.GetBytes(OpensslData);
            byte[] signature = Convert.FromBase64String(OpensslSignature);

            Assert.True(RecipeCatalogSignature.Verify(data, signature, new[] { OpensslPublicKey }));
            // Another key, changed data or a changed signature don't verify
            Assert.False(RecipeCatalogSignature.Verify(data, signature, new[] { TestKey.Create().PublicKey }));
            Assert.False(RecipeCatalogSignature.Verify(Encoding.UTF8.GetBytes(OpensslData + " "), signature, new[] { OpensslPublicKey }));
            signature[signature.Length - 1] ^= 1;
            Assert.False(RecipeCatalogSignature.Verify(data, signature, new[] { OpensslPublicKey }));
        }

        [Fact]
        public void Signature_WithAnyTrustedKey_IsVerified()
        {
            var current = TestKey.Create();
            var spare = TestKey.Create();
            byte[] data = Encoding.UTF8.GetBytes("index");

            Assert.True(RecipeCatalogSignature.Verify(data, spare.Sign(data), new[] { current.PublicKey, spare.PublicKey }));
            Assert.False(RecipeCatalogSignature.Verify(data, spare.Sign(data), new[] { current.PublicKey }));
            Assert.False(RecipeCatalogSignature.Verify(data, spare.Sign(data), Array.Empty<string>()));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not base64")]
        // An RSA key
        [InlineData("MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAMm+LEU/u5gn4hcwSgdAvaE7J2RHlDMvVr9ECAkPqZJ1RsQbm9clCGoBPjVcFbgyG8UfaS0w7JwqSqqE7yEtkAUCAwEAAQ==")]
        public void ReadPublicKey_OnlyTakesP256Keys(string key)
        {
            Assert.Null(RecipeCatalogSignature.ReadPublicKey(key));
        }

        [Fact]
        public void DerToIeeeP1363_RejectsWhatIsNoSignature()
        {
            Assert.Null(RecipeCatalogSignature.DerToIeeeP1363(new byte[0]));
            Assert.Null(RecipeCatalogSignature.DerToIeeeP1363(new byte[] { 0x30, 0x03, 0x02, 0x01, 0x01 }));
            // Something after the sequence
            byte[] valid = Convert.FromBase64String(OpensslSignature);
            Assert.NotNull(RecipeCatalogSignature.DerToIeeeP1363(valid));
            Assert.Null(RecipeCatalogSignature.DerToIeeeP1363(valid.Concat(new byte[] { 0 }).ToArray()));
        }

        [Fact]
        public void CatalogKeys_AreP256Keys()
        {
            Assert.NotEmpty(RecipeCatalogSignature.CatalogKeys);
            Assert.All(RecipeCatalogSignature.CatalogKeys, key => Assert.NotNull(RecipeCatalogSignature.ReadPublicKey(key)));
        }

        [Fact]
        public async Task Client_ReadsASignedIndex_AndRefusesAnUnsignedOne()
        {
            var key = TestKey.Create();
            var catalog = new FakeCatalog(key, CreateCatalogRecipe("signed_index"));
            var index = await catalog.Client(key).GetIndexAsync(CancellationToken.None);
            Assert.Equal("signed_index", Assert.Single(index.Recipes).Id);

            var exception = await Assert.ThrowsAsync<RecipeCatalogException>(() => catalog.Client(TestKey.Create()).GetIndexAsync(CancellationToken.None));
            Assert.Equal(Greenshot.Base.Languages.Texts.Recipe.CatalogNotSigned, exception.Message);
        }

        [Fact]
        public async Task Client_RefusesARecipeFileWithAnotherHash_OrFromAnotherPlace()
        {
            var key = TestKey.Create();
            var catalog = new FakeCatalog(key, CreateCatalogRecipe("hashed_recipe"));
            var client = catalog.Client(key);
            var entry = RecipeCatalogClient.Find(await client.GetIndexAsync(CancellationToken.None), "hashed_recipe");
            Assert.Equal(catalog.RecipeBytes, await client.GetRecipeFileAsync(entry, CancellationToken.None));

            catalog.RecipeBytes = catalog.RecipeBytes.Concat(Encoding.UTF8.GetBytes(" ")).ToArray();
            await Assert.ThrowsAsync<RecipeCatalogException>(() => client.GetRecipeFileAsync(entry, CancellationToken.None));

            entry.Url = "https://example.com/hashed_recipe.gsrecipe.json";
            await Assert.ThrowsAsync<RecipeCatalogException>(() => client.GetRecipeFileAsync(entry, CancellationToken.None));
        }

        [Theory]
        [InlineData("1.4.0", "1.4", true)]
        [InlineData("1.4.0.0", "1.4.0", true)]
        [InlineData("1.4.12", "1.4.2", true)]
        [InlineData("2", "1.4.0", false)]
        public void TryParseVersion_ComparesPartsAsNumbers(string a, string b, bool parsed)
        {
            Assert.Equal(parsed, RecipeCatalogInstaller.TryParseVersion(a, out var first) && RecipeCatalogInstaller.TryParseVersion(b, out var second) && first >= second);
        }

        [Fact]
        public async Task Install_Approved_IsSaved_FromTheCatalog_WithTheOptionValues()
        {
            const string id = "catalog_installed";
            await WithCatalogAsync(CreateCatalogRecipe(id), (request, cancellationToken) =>
            {
                Assert.True(request.IsFromCatalog);
                Assert.True(request.EditOptions);
                Assert.Equal("1.2.0", request.CatalogEntry.Version);
                var approval = new RecipeApproval { ApprovedTriggers = new List<string>() };
                return Task.FromResult(new ApprovalResult(approval, false) { OptionValues = new Dictionary<string, object> { ["border"] = 9 } });
            }, async (catalog, directory, messages) =>
            {
                try
                {
                    Assert.Equal(RecipeCatalogInstallOutcome.Installed, await RecipeCatalogInstaller.InstallAsync(id));
                    Assert.Empty(messages);

                    string file = Path.Combine(directory, id + RecipeSerializer.RecipeFileExtension);
                    // Exactly the bytes of the catalog, which were approved
                    Assert.Equal(catalog.RecipeBytes, File.ReadAllBytes(file));
                    var record = RecipeTrustStore.GetTrustRecord(file);
                    Assert.True(record.IsFromCatalog);
                    Assert.Equal($"catalog:{id}@1.2.0", record.Origin);

                    var loaded = RecipeManager.Instance.GetRecipeById(id);
                    Assert.NotNull(loaded);
                    Assert.Equal(9, Convert.ToInt32(RecipeOptionStore.GetValue(loaded, loaded.Options.Single())));

                    // The same version again: nothing to approve
                    Assert.Equal(RecipeCatalogInstallOutcome.Refused, await RecipeCatalogInstaller.InstallAsync(id));
                    Assert.Single(messages);
                }
                finally
                {
                    RecipeManager.Instance.UnregisterRecipe(id);
                    RecipeOptionStore.Reset(id);
                }
            });
        }

        [Fact]
        public async Task Install_Rejected_SavesNothing()
        {
            const string id = "catalog_rejected";
            await WithCatalogAsync(CreateCatalogRecipe(id), (request, cancellationToken) => Task.FromResult<ApprovalResult>(null), async (catalog, directory, messages) =>
            {
                Assert.Equal(RecipeCatalogInstallOutcome.Rejected, await RecipeCatalogInstaller.InstallAsync(id));
                Assert.False(File.Exists(Path.Combine(directory, id + RecipeSerializer.RecipeFileExtension)));
                Assert.Null(RecipeManager.Instance.GetRecipeById(id));
            });
        }

        [Fact]
        public async Task Install_WhenTheAdministratorSwitchedItOff_ShowsNothingButTheReason()
        {
            const string id = "catalog_switched_off";
            bool shown = false;
            await WithCatalogAsync(CreateCatalogRecipe(id), (request, cancellationToken) =>
            {
                shown = true;
                return Task.FromResult<ApprovalResult>(null);
            }, async (catalog, directory, messages) =>
            {
                RecipeCatalogInstaller.ConfigurationProvider = () => new RecipeConfigurationImpl { Enabled = true, AllowCatalogInstall = false };
                Assert.Equal(RecipeCatalogInstallOutcome.Refused, await RecipeCatalogInstaller.InstallAsync(id));
                Assert.False(shown);
                Assert.Equal(Greenshot.Base.Languages.Texts.Recipe.CatalogNotAllowed, Assert.Single(messages));
                Assert.Empty(catalog.Handler.Requests);
            });
        }

        [Theory]
        [InlineData("not_in_the_catalog")]
        [InlineData("../evil")]
        [InlineData("")]
        public async Task Install_OfAnUnknownId_ShowsNothingButTheReason(string id)
        {
            await WithCatalogAsync(CreateCatalogRecipe("catalog_known"), (request, cancellationToken) => throw new InvalidOperationException("No window for an unknown recipe"),
                async (catalog, directory, messages) =>
                {
                    Assert.Equal(RecipeCatalogInstallOutcome.Refused, await RecipeCatalogInstaller.InstallAsync(id));
                    Assert.Single(messages);
                });
        }

        [Fact]
        public async Task Install_DoesNotReplaceARecipeWhichIsNotFromTheCatalog()
        {
            const string id = "catalog_taken";
            RecipeManager.Instance.RegisterRecipe(RecipeApprovalTests.CreateRecipe(id));
            try
            {
                await WithCatalogAsync(CreateCatalogRecipe(id), (request, cancellationToken) => throw new InvalidOperationException("No window for a taken id"),
                    async (catalog, directory, messages) =>
                    {
                        Assert.Equal(RecipeCatalogInstallOutcome.Refused, await RecipeCatalogInstaller.InstallAsync(id));
                        Assert.Contains(id, Assert.Single(messages));
                    });
            }
            finally
            {
                RecipeManager.Instance.UnregisterRecipe(id);
            }
        }

        [Fact]
        public async Task Install_NeedingANewerGreenshot_ShowsNothingButTheReason()
        {
            const string id = "catalog_too_new";
            await WithCatalogAsync(CreateCatalogRecipe(id), (request, cancellationToken) => throw new InvalidOperationException("No window for a recipe this Greenshot can't use"),
                async (catalog, directory, messages) =>
                {
                    RecipeCatalogInstaller.GreenshotVersionProvider = () => new Version(1, 3, 9, 0);
                    Assert.Equal(RecipeCatalogInstallOutcome.Refused, await RecipeCatalogInstaller.InstallAsync(id));
                    Assert.Contains("1.4.0", Assert.Single(messages));
                });
        }

        [Fact]
        public async Task Install_WhileOneWaitsForTheUser_IsDropped()
        {
            const string id = "catalog_busy";
            var windowOpen = new TaskCompletionSource<bool>();
            var closeWindow = new TaskCompletionSource<ApprovalResult>();
            await WithCatalogAsync(CreateCatalogRecipe(id), (request, cancellationToken) =>
            {
                windowOpen.TrySetResult(true);
                return closeWindow.Task;
            }, async (catalog, directory, messages) =>
            {
                var first = RecipeCatalogInstaller.InstallAsync(id);
                await windowOpen.Task;
                Assert.Equal(RecipeCatalogInstallOutcome.Busy, await RecipeCatalogInstaller.InstallAsync(id));
                closeWindow.SetResult(null);
                Assert.Equal(RecipeCatalogInstallOutcome.Rejected, await first);
            });
        }

        [Fact]
        public void InstallRecipe_IsAllowedFromAWebPage()
        {
            Assert.True(Greenshot.Ipc.IpcSecurityDispatcher.IsCommandAllowedForSource("INSTALL_RECIPE", "url_scheme"));
        }

        private static CaptureRecipe CreateCatalogRecipe(string id)
        {
            var recipe = RecipeApprovalTests.CreateRecipe(id)
                .AddNode(new RecipeNodeConfig { Id = "clip", StepType = "Clipboard" });
            recipe.Flow.AddTransition("source", "clip");
            recipe.Options = new List<RecipeOption>
            {
                new RecipeOption { Key = "border", Type = ContractDataType.Integer, DefaultValue = 4, Min = 0, Max = 50, Label = "Border" }
            };
            return recipe;
        }

        /// <summary>
        /// The installer with a fake catalog signed with a test key, a temporary directory and trust store, the approval window replaced
        /// and the messages to the user collected
        /// </summary>
        private static async Task WithCatalogAsync(CaptureRecipe recipe, Func<RecipeApprovalRequest, CancellationToken, Task<ApprovalResult>> prompt,
            Func<FakeCatalog, string, List<string>, Task> test)
        {
            string directory = Path.Combine(Path.GetTempPath(), "GreenshotCatalog_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var key = TestKey.Create();
            var catalog = new FakeCatalog(key, recipe);
            var messages = new List<string>();
            var previousClient = RecipeCatalogInstaller.ClientFactory;
            var previousPrompt = RecipeCatalogInstaller.ApprovalPrompt;
            var previousMessage = RecipeCatalogInstaller.ShowMessage;
            var previousDirectory = RecipeCatalogInstaller.DirectoryProvider;
            var previousConfiguration = RecipeCatalogInstaller.ConfigurationProvider;
            var previousVersion = RecipeCatalogInstaller.GreenshotVersionProvider;
            try
            {
                RecipeTrustStore.UseStoreFile(Path.Combine(directory, "recipe_trust.dat"));
                RecipeCatalogInstaller.ClientFactory = () => catalog.Client(key);
                RecipeCatalogInstaller.ApprovalPrompt = prompt;
                RecipeCatalogInstaller.ShowMessage = (text, cancellationToken) =>
                {
                    lock (messages)
                    {
                        messages.Add(text);
                    }
                    return Task.CompletedTask;
                };
                RecipeCatalogInstaller.DirectoryProvider = () => Path.Combine(directory, "Catalog");
                RecipeCatalogInstaller.ConfigurationProvider = () => new RecipeConfigurationImpl { Enabled = true, AllowCatalogInstall = true };
                RecipeCatalogInstaller.GreenshotVersionProvider = () => new Version(1, 4, 0, 0);
                await test(catalog, Path.Combine(directory, "Catalog"), messages);
            }
            finally
            {
                RecipeCatalogInstaller.ClientFactory = previousClient;
                RecipeCatalogInstaller.ApprovalPrompt = previousPrompt;
                RecipeCatalogInstaller.ShowMessage = previousMessage;
                RecipeCatalogInstaller.DirectoryProvider = previousDirectory;
                RecipeCatalogInstaller.ConfigurationProvider = previousConfiguration;
                RecipeCatalogInstaller.GreenshotVersionProvider = previousVersion;
                RecipeTrustStore.UseStoreFile(null);
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // A file still open is removed with the temporary directory later
                }
            }
        }

        /// <summary>
        /// The catalog as GitHub serves it: index.json, its signature and the recipe file
        /// </summary>
        private sealed class FakeCatalog
        {
            private const string RecipeUrl = RecipeCatalogClient.RecipeUrlPrefix + "0123456789abcdef/recipes/{0}/recipe.gsrecipe.json";
            private readonly TestKey _key;
            private readonly string _recipeId;

            public FakeCatalog(TestKey key, CaptureRecipe recipe)
            {
                _key = key;
                _recipeId = recipe.Id;
                RecipeBytes = new UTF8Encoding(false).GetBytes(RecipeSerializer.Serialize(recipe));
                Handler = new FakeHttpHandler(Respond);
            }

            public byte[] RecipeBytes { get; set; }

            public FakeHttpHandler Handler { get; }

            public RecipeCatalogClient Client(TestKey trusted) => new RecipeCatalogClient(new HttpService(Handler, TimeSpan.FromSeconds(10)), new[] { trusted.PublicKey });

            private byte[] Index()
            {
                // The hash of the recipe as it was published, later changes of RecipeBytes are what an attacker serves
                string json = "{\"indexVersion\":1,\"repository\":\"greenshot/greenshot-recipes\",\"commit\":\"0123456789abcdef\",\"recipes\":[{" +
                              $"\"id\":\"{_recipeId}\",\"version\":\"1.2.0\",\"name\":\"{_recipeId}\",\"summary\":\"A test\",\"kind\":\"recipe\"," +
                              "\"authors\":[{\"name\":\"Test\",\"github\":\"test\"}],\"license\":\"MIT\",\"minGreenshot\":\"1.4.0\",\"tags\":[]," +
                              $"\"sha256\":\"{_publishedHash ??= RecipeTrustStore.ComputeSha256(RecipeBytes)}\",\"url\":\"{string.Format(RecipeUrl, _recipeId)}\"}}]}}\n";
                return Encoding.UTF8.GetBytes(json);
            }

            private string _publishedHash;

            private HttpResponseMessage Respond(HttpRequestMessage request)
            {
                string url = request.RequestUri.AbsoluteUri;
                if (url == RecipeCatalogClient.IndexUrl)
                {
                    return FakeHttpHandler.Bytes(Index());
                }
                if (url == RecipeCatalogClient.SignatureUrl)
                {
                    return FakeHttpHandler.Bytes(_key.Sign(Index()));
                }
                if (url == string.Format(RecipeUrl, _recipeId))
                {
                    return FakeHttpHandler.Bytes(RecipeBytes);
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// A P-256 key made for a test, signing like openssl does (DER)
        /// </summary>
        private sealed class TestKey
        {
            private static readonly byte[] SpkiPrefix =
            {
                0x30, 0x59, 0x30, 0x13, 0x06, 0x07, 0x2a, 0x86, 0x48, 0xce, 0x3d, 0x02, 0x01, 0x06, 0x08, 0x2a,
                0x86, 0x48, 0xce, 0x3d, 0x03, 0x01, 0x07, 0x03, 0x42, 0x00, 0x04
            };

            private ECParameters _parameters;

            public string PublicKey { get; private set; }

            public static TestKey Create()
            {
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var parameters = ecdsa.ExportParameters(true);
                return new TestKey
                {
                    _parameters = parameters,
                    PublicKey = Convert.ToBase64String(SpkiPrefix.Concat(parameters.Q.X).Concat(parameters.Q.Y).ToArray())
                };
            }

            public byte[] Sign(byte[] data)
            {
                using var ecdsa = ECDsa.Create(_parameters);
                byte[] signature = ecdsa.SignData(data, HashAlgorithmName.SHA256);
                return ToDer(signature.Take(32).ToArray(), signature.Skip(32).ToArray());
            }

            private static byte[] ToDer(byte[] r, byte[] s)
            {
                byte[] Integer(byte[] value)
                {
                    var trimmed = value.SkipWhile(b => b == 0).ToList();
                    if (trimmed.Count == 0 || trimmed[0] >= 0x80)
                    {
                        trimmed.Insert(0, 0);
                    }
                    return new byte[] { 0x02, (byte)trimmed.Count }.Concat(trimmed).ToArray();
                }
                var body = Integer(r).Concat(Integer(s)).ToArray();
                return new byte[] { 0x30, (byte)body.Length }.Concat(body).ToArray();
            }
        }
    }
}
