using Dapplo.Config.Language;

namespace Greenshot.CaptureCore.Configuration
{
	/// <summary>
	/// Translations for the core
	/// </summary>
	public interface ICaptureTranslations : ILanguagePart
	{

		string ContextmenuCaptureArea { get; }

		string ContextmenuCaptureClipboard { get; }

		string ContextmenuCaptureFullScreen { get; }

		string ContextmenuCaptureFullScreenAll { get; }

		string ContextmenuCaptureFullScreenBottom { get; }

		string ContextmenuCaptureFullScreenLeft { get; }

		string ContextmenuCaptureFullScreenRight { get; }

		string ContextmenuCaptureFullScreenTop { get; }

		string ContextmenuCaptureie { get; }

		string ContextmenuCaptureLastRegion { get; }

		string ContextmenuCaptureWindow { get; }

		string WaitIeCapture { get; }
	}
}