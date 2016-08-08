using System.ComponentModel.Composition;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.CaliburnMicro.Menu;
using Dapplo.Utils;
using Greenshot.Addon.Box.Forms;
using MahApps.Metro.IconPacks;

namespace Greenshot.Addon.Box
{
	/// <summary>
	/// This will add an extry for the exit to the context menu
	/// </summary>
	[Export("systray", typeof(IMenuItem))]
	public sealed class BoxMenuItem : MenuItem, IPartImportsSatisfiedNotification
	{

		[Import]
		private IBoxLanguage BoxLanguage
		{
			get;
			set;
		}

		[Import]
		private IBoxConfiguration BoxConfiguration
		{
			get;
			set;
		}

		public void OnImportsSatisfied()
		{
			Id = "Box";
			UiContext.RunOn(() =>
			{
				// automatically update the DisplayName
				this.BindDisplayName(BoxLanguage, nameof(IBoxLanguage.SettingsTitle));
				Icon = new PackIconMaterial
				{
					Kind = PackIconMaterialKind.Box
				};
			});
		}

		public override void Click(IMenuItem clickedItem)
		{
			new SettingsForm(BoxConfiguration).ShowDialog();
		}
	}
}
