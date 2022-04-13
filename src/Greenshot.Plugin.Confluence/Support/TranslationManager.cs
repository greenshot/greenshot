using System;

namespace Greenshot.Plugin.Confluence.Support
{
    public class TranslationManager
    {
        private static TranslationManager _translationManager;

        public event EventHandler LanguageChanged;

        public static TranslationManager Instance => _translationManager ??= new TranslationManager();

        public ITranslationProvider TranslationProvider { get; set; }

        public object Translate(string key)
        {
            object translatedValue = TranslationProvider?.Translate(key);
            if (translatedValue != null)
            {
                return translatedValue;
            }

            return $"!{key}!";
        }
    }
}