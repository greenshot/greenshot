using System;
using System.Collections.Generic;
using System.Globalization;

using GreenshotConfluencePlugin;
using GreenshotPlugin.Core;

namespace TranslationByMarkupExtension {
    /// <summary>
    /// 
    /// </summary>
    public class LanguageXMLTranslationProvider : ITranslationProvider {
        #region Private Members

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ResxTranslationProvider"/> class.
        /// </summary>
        /// <param name="baseName">Name of the base.</param>
        /// <param name="assembly">The assembly.</param>
        public LanguageXMLTranslationProvider() {
        }

        #endregion

        #region ITranslationProvider Members

        /// <summary>
        /// See <see cref="ITranslationProvider.Translate" />
        /// </summary>
        public object Translate(string key) {
        	if (Language.hasKey(key)) {
				return Language.GetString(key);
        	}
            return key;
        }

        #endregion

        #region ITranslationProvider Members

        /// <summary>
        /// See <see cref="ITranslationProvider.AvailableLanguages" />
        /// </summary>
        public IEnumerable<CultureInfo> Languages {
            get {
				foreach (LanguageFile supportedLanguage in Language.SupportedLanguages) {
            		yield return new CultureInfo(supportedLanguage.Ietf);
            	}
            }
        }

        #endregion
    }
}
