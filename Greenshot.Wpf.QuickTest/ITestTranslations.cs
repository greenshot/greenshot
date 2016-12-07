using Dapplo.Language;
using Greenshot.Core.Configuration;

namespace Greenshot.Wpf.QuickTest
{
	/// <summary>
	/// This actually only makes sure the ICoreTranslations is available...
	/// </summary>
	public interface ITestTranslations : ILanguage, ICoreTranslations
	{
	}
}
