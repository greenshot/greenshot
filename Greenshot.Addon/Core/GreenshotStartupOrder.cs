namespace Greenshot.Addon.Core
{
	public enum GreenshotStartupOrder
	{
		/// <summary>
		///     This is the order which the CaliburnMicroBootstrapper uses, if you depend on this take a higher order!
		/// </summary>
		Bootstrapper = 100,

		/// <summary>
		///     This is the order for opening the TrayIcons, IF Dapplo.CaliburnMicro.NotifyIconWpf is used
		/// </summary>
		TrayIcons = 200,

		/// <summary>
		/// The order for addons
		/// </summary>
		Addon = 300
	}
}
