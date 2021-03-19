﻿using GreenshotPlugin.Core;

namespace GreenshotConfluencePlugin.Support {
	/// <summary>
	/// 
	/// </summary>
	public class LanguageXMLTranslationProvider : ITranslationProvider {
        /// <summary>
		/// See <see cref="ITranslationProvider.Translate" />
		/// </summary>
		public object Translate(string key) {
			if (Language.HasKey("confluence", key)) {
				return Language.GetString("confluence", key);
			}
			return key;
		}
    }
}
