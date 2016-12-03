using System.Collections.Generic;

namespace Greenshot.Core.Interfaces
{
	/// <summary>
	/// Base interface for composible things
	/// </summary>
	/// <typeparam name="TComposableItem">Type which is composed</typeparam>
	public interface IAmComposable<TComposableItem>
	{
		/// <summary>
		/// The items, these are readonly
		/// </summary>
		IEnumerable<TComposableItem> Items { get; }

		/// <summary>
		/// Add a new item
		/// </summary>
		/// <param name="item">TComposableItem to add</param>
		/// <returns>this to allow fluent calls</returns>
		IAmComposable<TComposableItem> Add(TComposableItem item);
	}
}
