using Greenshot.Base.Core;

namespace Greenshot.Plugin.Confluence.Support
{
    /// <summary>
    ///
    /// </summary>
    public class LanguageXMLTranslationProvider : ITranslationProvider
    {
        /// <summary>
        /// See <see cref="ITranslationProvider.Translate" />
        /// </summary>
        public object Translate(string key) => Language.HasKey("confluence", key) ? Language.GetString("confluence", key) : (object)key;
    }
}