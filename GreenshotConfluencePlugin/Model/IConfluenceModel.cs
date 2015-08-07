using Dapplo.Config.Extension;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenshotConfluencePlugin.Model {
	/// <summary>
	/// Container for the Confluence data
	/// </summary>
	public interface IConfluenceModel : INotifyPropertyChanged, IDefaultValue {
		IDictionary<string, Space> Spaces {
			get;
			set;
		}
		IDictionary<long, Content> ContentCachedById {
			get;
			set;
		}

		IDictionary<string, Content> ContentCachedBySpaceAndTitle {
			get;
			set;
		}
	}
}
