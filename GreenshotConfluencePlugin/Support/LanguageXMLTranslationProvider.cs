using GreenshotPlugin.Core;

namespace TranslationByMarkupExtension {
	/// <summary>
	/// 
	/// </summary>
	public class LanguageXMLTranslationProvider : ITranslationProvider {
		#region Private Members

		#endregion

		#region Construction

		#endregion

		#region ITranslationProvider Members

		/// <summary>
		/// See <see cref="ITranslationProvider.Translate" />
		/// </summary>
		public object Translate(string key) {
			if (Language.HasKey("confluence", key)) {
				return Language.GetString("confluence", key);
			}
			return key;
		}

		#endregion
	}
}
