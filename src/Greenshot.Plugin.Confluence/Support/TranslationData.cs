using System;
using System.ComponentModel;
using System.Windows;

namespace Greenshot.Plugin.Confluence.Support {
	public class TranslationData : IWeakEventListener, INotifyPropertyChanged {
        private readonly string _key;

        /// <summary>
		/// Initializes a new instance of the <see cref="TranslationData"/> class.
		/// </summary>
		/// <param name="key">The key.</param>
		public TranslationData( string key) {
			_key = key;
			LanguageChangedEventManager.AddListener(TranslationManager.Instance, this);
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="TranslationData"/> is reclaimed by garbage collection.
		/// </summary>
		~TranslationData() {
			LanguageChangedEventManager.RemoveListener(TranslationManager.Instance, this);
		}

		public object Value => TranslationManager.Instance.Translate(_key);

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(LanguageChangedEventManager))
			{
				OnLanguageChanged(sender, e);
				return true;
			}
			return false;
		}

		private void OnLanguageChanged(object sender, EventArgs e)
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs("Value"));
		}

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
