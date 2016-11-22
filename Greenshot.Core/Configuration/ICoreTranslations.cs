using System.ComponentModel;
using Dapplo.Config.Language;

namespace Greenshot.Core.Configuration
{
	/// <summary>
	/// Translations for the core
	/// </summary>
	public interface ICoreTranslations : ILanguagePart
	{
		[DefaultValue("Error")]
		string Error { get; }

		string ClipboardError { get; }

		string ClipboardInuse { get; }

		string ErrorNowriteaccess { get; }

		string ErrorOpenfile { get; }
	}
}