using System.Collections.Generic;
using System.ComponentModel;

namespace Greenshot.Addon.Interfaces.Drawing
{
	/// <summary>
	///     All elements that have "fields" that need to be bound in the editor must implement this interface
	/// </summary>
	public interface IFieldHolder : INotifyPropertyChanged
	{
		IDictionary<FieldTypes, FieldAttribute> FieldAttributes { get; }

		ElementFlag Flag { get; }

		void Invalidate();
	}
}