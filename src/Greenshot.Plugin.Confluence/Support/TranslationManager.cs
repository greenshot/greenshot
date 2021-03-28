using System;

namespace Greenshot.Plugin.Confluence.Support {
	public class TranslationManager {
		private static TranslationManager _translationManager;

		public event EventHandler LanguageChanged;

		/*public CultureInfo CurrentLanguage {
			get { return Thread.CurrentThread.CurrentUICulture; }
			set {
				if( value != Thread.CurrentThread.CurrentUICulture) {
					Thread.CurrentThread.CurrentUICulture = value;
					OnLanguageChanged();
				}
			}
		}

		public IEnumerable<CultureInfo> Languages {
			get {
			   if( TranslationProvider != null) {
				   return TranslationProvider.Languages;
			   }
			   return Enumerable.Empty<CultureInfo>();
			}
		}*/

		public static TranslationManager Instance => _translationManager ??= new TranslationManager();

		public ITranslationProvider TranslationProvider { get; set; }

        public object Translate(string key) {
			object translatedValue = TranslationProvider?.Translate(key);
			if( translatedValue != null) {
				return translatedValue;
			}
			return $"!{key}!";
		}
	}
}
