using System;
using System.Linq;
using Greenshot.Base.Core.FileFormat;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class FileFormatRegistryTests
    {
        [Fact]
        public void Definition_NormalizesIdsExtensionsAndMimeTypes()
        {
            var definition = CreateFormat(" WebP ", new[] { ".WEBP", "webp" }, "webp", "Image/WebP", new[] { "IMAGE/X-WEBP" });

            Assert.Equal("webp", definition.Id);
            Assert.Equal(new[] { "webp" }, definition.Extensions);
            Assert.Equal("image/webp", definition.MimeType);
            Assert.Equal(new[] { "image/x-webp" }, definition.MimeTypeAliases);
        }

        [Fact]
        public void Registry_ResolvesIdsExtensionsAndMimeAliasesCaseInsensitively()
        {
            var registry = new FileFormatRegistry();
            registry.Register(CreateFormat("webp", new[] { "webp" }, "webp", "image/webp", new[] { "image/x-webp" }));

            Assert.True(registry.TryGet("WEBP", out var byId));
            Assert.Equal("webp", byId.Id);
            Assert.Equal("webp", registry.GetByExtension(".WEBP").Id);
            Assert.Equal("webp", registry.GetByMimeType("IMAGE/X-WEBP; charset=utf-8").Id);
        }

        [Fact]
        public void RegisterIfMissing_DoesNotReplaceExistingMetadata()
        {
            var registry = new FileFormatRegistry();
            var definition = CreateFormat("webp", new[] { "webp" }, "webp", "image/webp", null);
            registry.Register(definition);

            Assert.False(registry.RegisterIfMissing(CreateFormat("WEBP", new[] { ".WEBP" }, "webp", "image/webp", null)));
            Assert.Throws<InvalidOperationException>(() => registry.RegisterIfMissing(
                CreateFormat("webp", new[] { "webp", "webpx" }, "webp", "image/webp", null)));
            Assert.Single(registry.Formats);
            Assert.Null(registry.GetByExtension("webpx"));
        }

        [Fact]
        public void Register_RejectsExtensionAndMimeConflicts()
        {
            var registry = new FileFormatRegistry();
            registry.Register(CreateFormat("webp", new[] { "webp" }, "webp", "image/webp", null));

            Assert.Throws<InvalidOperationException>(() => registry.Register(
                CreateFormat("other", new[] { "webp" }, "webp", "image/x-other", null)));
            Assert.Throws<InvalidOperationException>(() => registry.Register(
                CreateFormat("other", new[] { "other" }, "other", "image/webp", null)));
        }

        [Fact]
        public void Definition_RejectsMimeParametersAndUnregisteredPreferredExtensions()
        {
            Assert.Throws<ArgumentException>(() => CreateFormat("webp", new[] { "webp" }, "webp", "image/webp; charset=utf-8", null));
            Assert.Throws<ArgumentException>(() => CreateFormat("webp", new[] { "webp" }, "webpx", "image/webp", null));
        }

        [Fact]
        public void WellKnownFileFormats_IsFormat_ValidatesKnownFormatAndComparesCaseInsensitively()
        {
            Assert.True(WellKnownFileFormats.IsEqualFormat(WellKnownFileFormats.Png, "PNG"));
            Assert.False(WellKnownFileFormats.IsEqualFormat("webp", "webp"));
        }

        [Fact]
        public void DisplayNameExtensions_AppendPreferredAndSupportedExtensions()
        {
            var format = CreateFormat("webp", new[] { "webp", "webpx" }, "webp", "image/webp", null);

            Assert.Equal("WebP image (.webp)", format.GetDisplayNameWithPreferredExtension());
            Assert.Equal("WebP image (.webp, .webpx)", format.GetDisplayNameWithSupportedExtensions());
        }

        private static FileFormatDefinition CreateFormat(string id, string[] extensions, string preferredExtension, string mimeType, string[] mimeAliases)
        {
            return new FileFormatDefinition(id, extensions, preferredExtension, mimeType, mimeAliases, "FileFormat." + id.ToLowerInvariant(), "WebP image");
        }
    }
}
